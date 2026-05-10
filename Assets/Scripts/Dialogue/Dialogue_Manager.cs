using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Manager")]
public class Dialogue_Manager : MonoBehaviour
{
    public static Dialogue_Manager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Dialogue_UI dialogueUI;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject gameplayCrosshair;

    [Header("Player Dialogue Camera")]
    [SerializeField] private CinemachineCamera playerDialogueCamera;
    [SerializeField] private int playerDialogueCameraPriority = 60;

    [Header("NPC Dialogue Camera")]
    [SerializeField] private CinemachineCamera npcDialogueCamera;
    [SerializeField] private int npcDialogueCameraPriority = 55;
    [Tooltip("If enabled, the NPC dialogue camera priority is only raised after the player has spoken. If disabled, it is raised as soon as dialogue starts.")]
    [SerializeField] private bool useNpcCameraOnlyAfterPlayerSpeaks;
    [Tooltip("If enabled, the NPC dialogue camera's Tracking Target is set to the NPC currently being spoken to.")]
    [SerializeField] private bool autoSetNpcCameraTrackingTarget = true;
    [Tooltip("Optional. If set, the NPC dialogue camera targets the first object with this tag found on the speaker or its children. Leave empty to target the speaker object directly.")]
    [SerializeField] private string npcCameraTargetTag;
    [Tooltip("If enabled, the NPC dialogue camera's Tracking Target is restored when dialogue ends. Disable this for a dedicated dialogue camera that can stay on the last NPC.")]
    [SerializeField] private bool restoreNpcCameraTrackingTargetOnEnd = true;

    [Header("Player Response")]
    [SerializeField] private string playerSpeakerName = "Player";

    [Header("Player Lock")]
    [SerializeField] private bool lockPlayerDuringDialogue = true;
    [SerializeField] private bool showCursorDuringDialogue = true;

    [Header("Player Dialogue Position")]
    [SerializeField] private bool movePlayerToDialoguePosition = true;
    [SerializeField] private float playerDistanceFromSpeaker = 2f;
    [SerializeField] private float playerSideOffset = 0f;
    [SerializeField] private float playerRepositionSpeed = 2f;
    [SerializeField] private bool rotatePlayerTowardSpeaker = true;
    [SerializeField] private float playerRotationSpeed = 540f;

    [Header("Dialogue Submit")]
    [Tooltip("When enabled, the player's Interact input activates the currently selected dialogue button.")]
    [SerializeField] private bool useInteractToSubmitSelectedButton = true;

    [Header("Dialogue Cancel")]
    [SerializeField] private bool useCancelInputToEndDialogue = true;
    [SerializeField] private InputActionReference cancelDialogueAction;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueEnded;

    public Dialogue_Node CurrentNode { get; private set; }
    public GameObject CurrentSpeakerObject { get; private set; }
    public bool IsDialogueActive => CurrentNode != null || pendingNodeAfterPlayerResponse != null || showingPlayerResponse;

    private Dialogue_Source currentDialogueSource;
    private Dialogue_Node pendingNodeAfterPlayerResponse;
    private Behaviour[] playerBehavioursToDisable;
    private bool[] playerBehaviourPreviousStates;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private bool playerLocked;
    private PlayerInputHandler playerInput;
    private int ignoreInteractSubmitFrame = -1;
    private int ignoreCancelInputFrame = -1;
    private bool showEndDialogueButton;
    private string endDialogueButtonText;
    private int playerDialogueCameraPreviousPriority;
    private bool playerDialogueCameraPriorityChanged;
    private int npcDialogueCameraPreviousPriority;
    private bool npcDialogueCameraPriorityChanged;
    private Transform npcDialogueCameraPreviousTrackingTarget;
    private bool npcDialogueCameraTrackingTargetChanged;
    private bool playerHasSpokenThisDialogue;
    private Coroutine playerRepositionRoutine;
    private bool cancelDialogueActionEnabledByManager;
    private bool endAfterPlayerResponse;
    private bool showingPlayerResponse;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple Dialogue_Manager objects exist in the scene.", this);
            return;
        }

        Instance = this;

        if (dialogueUI == null)
            dialogueUI = FindFirstObjectByType<Dialogue_UI>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        EnableCancelDialogueAction();
    }

    private void OnDisable()
    {
        DisableCancelDialogueAction();
    }

    private void Update()
    {
        if (ShouldCancelDialogueFromInput())
        {
            EndDialogue();
            return;
        }

        if (!useInteractToSubmitSelectedButton || !IsDialogueActive)
            return;

        if (Time.frameCount == ignoreInteractSubmitFrame)
            return;

        CachePlayerInput();

        if (playerInput == null || !playerInput.InteractPressed || dialogueUI == null)
            return;

        if (dialogueUI.IsTyping && dialogueUI.AllowInteractToSpeedUpTyping)
        {
            dialogueUI.SpeedUpTypingOnce();
            return;
        }

        if (dialogueUI.CanContinuePlayerLineWithInteract)
        {
            dialogueUI.ContinuePlayerLineFromInteract();
            return;
        }

        dialogueUI.SubmitSelectedButton();
    }

    public void StartDialogue(
        Dialogue_Node startingNode,
        GameObject speakerObject = null,
        bool showEndButton = true,
        string endButtonText = "Goodbye")
    {
        StartDialogue(null, startingNode, speakerObject, showEndButton, endButtonText);
    }

    public void StartDialogue(
        Dialogue_Source dialogueSource,
        Dialogue_Node startingNode,
        GameObject speakerObject = null,
        bool showEndButton = true,
        string endButtonText = "Goodbye")
    {
        if (startingNode == null)
        {
            Debug.LogWarning("Tried to start dialogue without a starting node.", this);
            return;
        }

        currentDialogueSource = dialogueSource;
        CurrentSpeakerObject = speakerObject;
        showEndDialogueButton = showEndButton;
        endDialogueButtonText = endButtonText;
        playerHasSpokenThisDialogue = false;
        endAfterPlayerResponse = false;
        showingPlayerResponse = false;

        ignoreInteractSubmitFrame = Time.frameCount;
        ignoreCancelInputFrame = Time.frameCount;
        LockPlayer();
        BeginPlayerDialogueReposition(speakerObject);
        ApplyNpcCameraTrackingTarget(speakerObject);

        if (!useNpcCameraOnlyAfterPlayerSpeaks)
            ApplyNpcDialogueCameraPriority();

        onDialogueStarted?.Invoke();
        ShowNode(startingNode);
    }

    public void Choose(DialogueChoice choice)
    {
        if (choice == null)
        {
            EndDialogue();
            return;
        }

        choice.MarkChosen();
        choice.RunLinkedDialogueEvent(this);
        endAfterPlayerResponse = choice.EndsConversation || choice.FinalChoice;

        if (choice.FinalChoice)
        {
            if (currentDialogueSource != null)
                currentDialogueSource.MarkDialogueCompleted();
        }

        ShowPlayerResponse(choice);
    }

    public void EndDialogue()
    {
        CurrentNode = null;
        CurrentSpeakerObject = null;
        currentDialogueSource = null;
        pendingNodeAfterPlayerResponse = null;
        endAfterPlayerResponse = false;
        showingPlayerResponse = false;

        RestorePlayerDialogueCameraPriority();
        RestoreNpcDialogueCameraPriority();
        if (restoreNpcCameraTrackingTargetOnEnd)
            RestoreNpcCameraTrackingTarget();

        if (dialogueUI != null)
            dialogueUI.Hide();

        StopPlayerDialogueReposition();
        UnlockPlayer();
        onDialogueEnded?.Invoke();
    }

    private void ShowNode(Dialogue_Node node)
    {
        if (node == null)
        {
            EndDialogue();
            return;
        }

        CurrentNode = node;

        if (dialogueUI != null)
        {
            RestorePlayerDialogueCameraPriority();
            if (!useNpcCameraOnlyAfterPlayerSpeaks || playerHasSpokenThisDialogue)
                ApplyNpcDialogueCameraPriority();

            dialogueUI.ShowNode(node, GetCurrentSpeakerName(), Choose, showEndDialogueButton, endDialogueButtonText, EndDialogue);
        }
        else
        {
            RestorePlayerDialogueCameraPriority();
            if (!useNpcCameraOnlyAfterPlayerSpeaks || playerHasSpokenThisDialogue)
                ApplyNpcDialogueCameraPriority();

            Debug.Log($"{GetCurrentSpeakerName()}: {node.DialogueText}", node);
        }
    }

    private string GetCurrentSpeakerName()
    {
        if (currentDialogueSource != null)
            return currentDialogueSource.SpeakerName;

        if (CurrentSpeakerObject != null)
            return CurrentSpeakerObject.name;

        return string.Empty;
    }

    private void ShowPlayerResponse(DialogueChoice choice)
    {
        CurrentNode = null;
        pendingNodeAfterPlayerResponse = choice.NextNode;
        showingPlayerResponse = true;
        playerHasSpokenThisDialogue = true;
        RestoreNpcDialogueCameraPriority();
        ApplyPlayerDialogueCameraPriority();

        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(
                playerSpeakerName,
                choice.PlayerResponseText,
                ContinueAfterPlayerResponse);
        }
        else
        {
            Debug.Log($"{playerSpeakerName}: {choice.PlayerResponseText}", this);
            ContinueAfterPlayerResponse();
        }
    }

    private void ContinueAfterPlayerResponse()
    {
        showingPlayerResponse = false;

        if (endAfterPlayerResponse)
        {
            EndDialogue();
            return;
        }

        Dialogue_Node nextNode = pendingNodeAfterPlayerResponse;
        pendingNodeAfterPlayerResponse = null;
        RestorePlayerDialogueCameraPriority();

        ShowNode(nextNode);
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringDialogue || playerLocked)
            return;

        playerLocked = true;
        CachePlayerBehaviours();

        if (playerBehavioursToDisable != null)
        {
            playerBehaviourPreviousStates = new bool[playerBehavioursToDisable.Length];

            for (int i = 0; i < playerBehavioursToDisable.Length; i++)
            {
                Behaviour behaviour = playerBehavioursToDisable[i];
                if (behaviour == null)
                    continue;

                playerBehaviourPreviousStates[i] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }

        if (gameplayCrosshair != null)
            gameplayCrosshair.SetActive(false);

        if (showCursorDuringDialogue)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UnlockPlayer()
    {
        if (!playerLocked)
            return;

        playerLocked = false;

        if (playerBehavioursToDisable != null && playerBehaviourPreviousStates != null)
        {
            for (int i = 0; i < playerBehavioursToDisable.Length; i++)
            {
                Behaviour behaviour = playerBehavioursToDisable[i];
                if (behaviour == null)
                    continue;

                behaviour.enabled = playerBehaviourPreviousStates[i];
            }
        }

        if (gameplayCrosshair != null)
            gameplayCrosshair.SetActive(true);

        if (showCursorDuringDialogue)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void BeginPlayerDialogueReposition(GameObject speakerObject)
    {
        StopPlayerDialogueReposition();

        if (!movePlayerToDialoguePosition || playerObject == null || speakerObject == null)
            return;

        if (speakerObject.GetComponentInParent<Dialogue_NoPlayerReposition>() != null)
            return;

        playerRepositionRoutine = StartCoroutine(MovePlayerToDialoguePosition(speakerObject.transform));
    }

    private IEnumerator MovePlayerToDialoguePosition(Transform speakerTransform)
    {
        if (speakerTransform == null)
        {
            playerRepositionRoutine = null;
            yield break;
        }

        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
        Transform playerTransform = playerObject.transform;

        Vector3 targetPosition = GetPlayerDialogueTargetPosition(speakerTransform, playerTransform.position.y);

        while (Vector3.Distance(playerTransform.position, targetPosition) > 0.01f)
        {
            if (speakerTransform == null)
            {
                playerRepositionRoutine = null;
                yield break;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition,
                playerRepositionSpeed * Time.fixedDeltaTime);

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = new Vector3(0f, playerRigidbody.linearVelocity.y, 0f);
                playerRigidbody.MovePosition(nextPosition);
            }
            else
            {
                playerTransform.position = nextPosition;
            }

            if (rotatePlayerTowardSpeaker)
                RotatePlayerTowardSpeaker(playerRigidbody, playerTransform, speakerTransform.position);

            yield return new WaitForFixedUpdate();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = new Vector3(0f, playerRigidbody.linearVelocity.y, 0f);
            playerRigidbody.MovePosition(targetPosition);
        }
        else
        {
            playerTransform.position = targetPosition;
        }

        if (rotatePlayerTowardSpeaker)
            RotatePlayerTowardSpeaker(playerRigidbody, playerTransform, speakerTransform.position);

        playerRepositionRoutine = null;
    }

    private Vector3 GetPlayerDialogueTargetPosition(Transform speakerTransform, float playerHeight)
    {
        Vector3 speakerForward = speakerTransform.forward;
        speakerForward.y = 0f;

        if (speakerForward.sqrMagnitude <= 0.0001f)
            speakerForward = Vector3.forward;

        speakerForward.Normalize();

        Vector3 speakerRight = speakerTransform.right;
        speakerRight.y = 0f;

        if (speakerRight.sqrMagnitude <= 0.0001f)
            speakerRight = Vector3.right;

        speakerRight.Normalize();

        Vector3 targetPosition =
            speakerTransform.position +
            speakerForward * playerDistanceFromSpeaker +
            speakerRight * playerSideOffset;

        targetPosition.y = playerHeight;
        return targetPosition;
    }

    private void RotatePlayerTowardSpeaker(Rigidbody playerRigidbody, Transform playerTransform, Vector3 speakerPosition)
    {
        Vector3 directionToSpeaker = speakerPosition - playerTransform.position;
        directionToSpeaker.y = 0f;

        if (directionToSpeaker.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToSpeaker.normalized, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            playerTransform.rotation,
            targetRotation,
            playerRotationSpeed * Time.fixedDeltaTime);

        if (playerRigidbody != null)
            playerRigidbody.MoveRotation(nextRotation);
        else
            playerTransform.rotation = nextRotation;
    }

    private void StopPlayerDialogueReposition()
    {
        if (playerRepositionRoutine == null)
            return;

        StopCoroutine(playerRepositionRoutine);
        playerRepositionRoutine = null;
    }

    private void OnValidate()
    {
        playerDistanceFromSpeaker = Mathf.Max(0f, playerDistanceFromSpeaker);
        playerRepositionSpeed = Mathf.Max(0.01f, playerRepositionSpeed);
        playerRotationSpeed = Mathf.Max(0f, playerRotationSpeed);
    }

    private bool ShouldCancelDialogueFromInput()
    {
        if (!useCancelInputToEndDialogue || !IsDialogueActive)
            return false;

        if (Time.frameCount == ignoreCancelInputFrame)
            return false;

        EnableCancelDialogueAction();

        InputAction cancelAction = cancelDialogueAction != null ? cancelDialogueAction.action : null;
        return cancelAction != null && cancelAction.WasPressedThisFrame();
    }

    private void EnableCancelDialogueAction()
    {
        if (!useCancelInputToEndDialogue)
            return;

        InputAction cancelAction = cancelDialogueAction != null ? cancelDialogueAction.action : null;
        if (cancelAction == null)
            return;

        if (cancelAction.enabled)
            return;

        cancelAction.Enable();
        cancelDialogueActionEnabledByManager = true;
    }

    private void DisableCancelDialogueAction()
    {
        InputAction cancelAction = cancelDialogueAction != null ? cancelDialogueAction.action : null;
        if (cancelAction == null || !cancelDialogueActionEnabledByManager)
            return;

        cancelAction.Disable();
        cancelDialogueActionEnabledByManager = false;
    }

    private void CachePlayerBehaviours()
    {
        if (playerBehavioursToDisable != null || playerObject == null)
            return;

        playerBehavioursToDisable = new Behaviour[]
        {
            playerObject.GetComponent<PlayerMovementController>(),
            playerObject.GetComponent<PlayerRotationController>(),
            playerObject.GetComponent<PlayerCameraController>(),
            playerObject.GetComponent<PlayerFreeCameraController>(),
            playerObject.GetComponent<PlayerAimController>(),
            playerObject.GetComponent<PlayerSprintController>(),
            playerObject.GetComponent<PlayerJumpController>(),
            playerObject.GetComponent<PlayerTongueProjection>(),
            playerObject.GetComponent<Player_TongueAttract>(),
            playerObject.GetComponent<Player_TongueGrab>(),
            playerObject.GetComponent<Player_TongueSwing>(),
            playerObject.GetComponent<Player_PickupControl>(),
            playerObject.GetComponent<Player_ThrowObjectControl>()
        };
    }

    private void CachePlayerInput()
    {
        if (playerInput != null || playerObject == null)
            return;

        playerInput = playerObject.GetComponent<PlayerInputHandler>();
    }

    private void ApplyPlayerDialogueCameraPriority()
    {
        if (playerDialogueCamera == null || playerDialogueCameraPriorityChanged)
            return;

        playerDialogueCameraPreviousPriority = playerDialogueCamera.Priority;
        playerDialogueCamera.Priority = playerDialogueCameraPriority;
        playerDialogueCameraPriorityChanged = true;
    }

    private void RestorePlayerDialogueCameraPriority()
    {
        if (playerDialogueCamera == null || !playerDialogueCameraPriorityChanged)
            return;

        playerDialogueCamera.Priority = playerDialogueCameraPreviousPriority;
        playerDialogueCameraPriorityChanged = false;
    }

    private void ApplyNpcDialogueCameraPriority()
    {
        if (npcDialogueCamera == null || npcDialogueCameraPriorityChanged)
            return;

        npcDialogueCameraPreviousPriority = npcDialogueCamera.Priority;
        npcDialogueCamera.Priority = npcDialogueCameraPriority;
        npcDialogueCameraPriorityChanged = true;
    }

    private void RestoreNpcDialogueCameraPriority()
    {
        if (npcDialogueCamera == null || !npcDialogueCameraPriorityChanged)
            return;

        npcDialogueCamera.Priority = npcDialogueCameraPreviousPriority;
        npcDialogueCameraPriorityChanged = false;
    }

    private void ApplyNpcCameraTrackingTarget(GameObject dialogueCharacter)
    {
        if (!autoSetNpcCameraTrackingTarget || npcDialogueCamera == null || dialogueCharacter == null)
            return;

        if (!npcDialogueCameraTrackingTargetChanged || !restoreNpcCameraTrackingTargetOnEnd)
        {
            npcDialogueCameraPreviousTrackingTarget = npcDialogueCamera.Target.TrackingTarget;
            npcDialogueCameraTrackingTargetChanged = true;
        }

        npcDialogueCamera.Target.TrackingTarget = GetNpcCameraTrackingTarget(dialogueCharacter);
    }

    private Transform GetNpcCameraTrackingTarget(GameObject dialogueCharacter)
    {
        if (dialogueCharacter == null)
            return null;

        if (string.IsNullOrWhiteSpace(npcCameraTargetTag))
            return dialogueCharacter.transform;

        Transform taggedTarget = FindTaggedCameraTarget(dialogueCharacter.transform, npcCameraTargetTag.Trim());
        return taggedTarget != null ? taggedTarget : dialogueCharacter.transform;
    }

    private Transform FindTaggedCameraTarget(Transform speakerTransform, string targetTag)
    {
        if (speakerTransform == null || string.IsNullOrWhiteSpace(targetTag))
            return null;

        if (HasTag(speakerTransform.gameObject, targetTag))
            return speakerTransform;

        return FindTaggedChild(speakerTransform, targetTag);
    }

    private Transform FindTaggedChild(Transform root, string targetTag)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (HasTag(child.gameObject, targetTag))
                return child;

            Transform taggedChild = FindTaggedChild(child, targetTag);
            if (taggedChild != null)
                return taggedChild;
        }

        return null;
    }

    private bool HasTag(GameObject targetObject, string targetTag)
    {
        try
        {
            return targetObject != null && targetObject.CompareTag(targetTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"NPC camera target tag '{targetTag}' is not defined in Project Settings > Tags and Layers.", this);
            return false;
        }
    }

    private void RestoreNpcCameraTrackingTarget()
    {
        if (npcDialogueCamera == null || !npcDialogueCameraTrackingTargetChanged)
            return;

        npcDialogueCamera.Target.TrackingTarget = npcDialogueCameraPreviousTrackingTarget;
        npcDialogueCameraPreviousTrackingTarget = null;
        npcDialogueCameraTrackingTargetChanged = false;
    }
}
