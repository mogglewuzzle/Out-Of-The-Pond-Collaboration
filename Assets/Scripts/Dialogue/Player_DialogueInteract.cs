using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Player Dialogue Interact")]
public class Player_DialogueInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Point on the player where the dialogue cast starts. The cast fires along this transform's forward direction. If empty, the player's transform is used.")]
    [SerializeField] private Transform interactOrigin;

    [Header("Input")]
    [Tooltip("Optional. If assigned, this action starts dialogue. If empty, the script uses PlayerInputHandler.InteractPressed.")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Detection")]
    [Tooltip("How far forward from the start point the dialogue check can reach.")]
    [SerializeField] private float interactRange = 3f;
    [Tooltip("Radius of the invisible cast. Larger values make it easier to start dialogue without aiming exactly at the collider.")]
    [SerializeField] private float castRadius = 0.2f;
    [Tooltip("Layers the dialogue check is allowed to hit. NPC colliders must be on one of these layers.")]
    [SerializeField] private LayerMask interactMask = ~0;
    [Tooltip("Tag required on the NPC or one of its parent objects before dialogue can start.")]
    [SerializeField] private string dialogueCharacterTag = "DialogueCharacter";
    [Tooltip("When enabled, trigger colliders can start dialogue too.")]
    [SerializeField] private bool hitTriggerColliders = true;
    [Tooltip("Prints why dialogue did or did not start when Interact is pressed.")]
    [SerializeField] private bool debugLogs;

    private PlayerInputHandler input;
    private bool interactActionEnabledByThisScript;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        EnableInteractAction();
    }

    private void OnDisable()
    {
        DisableInteractAction();
    }

    private void Update()
    {
        if (!WasInteractPressedThisFrame())
            return;

        if (Dialogue_Manager.Instance != null && Dialogue_Manager.Instance.IsDialogueActive)
            return;

        Dialogue_Source source = FindDialogueSource();
        if (source == null)
        {
            LogDebug("No dialogue source found from interact cast.");
            return;
        }

        LogDebug($"Starting dialogue from {source.name}.");
        source.StartDialogue();
    }

    private bool WasInteractPressedThisFrame()
    {
        InputAction action = interactAction != null ? interactAction.action : null;
        if (action != null)
        {
            EnableInteractAction();
            return action.WasPressedThisFrame();
        }

        return input != null && input.InteractPressed;
    }

    private Dialogue_Source FindDialogueSource()
    {
        Ray ray = GetInteractionRay();

        QueryTriggerInteraction triggerInteraction = hitTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        if (Physics.SphereCast(ray, castRadius, out RaycastHit hit, interactRange, interactMask, triggerInteraction))
        {
            LogDebug($"Interact cast hit {hit.collider.name} on {hit.collider.gameObject.layer}.");

            Dialogue_Source source = GetDialogueSourceFromCollider(hit.collider);
            if (source != null)
                return source;

            LogDebug($"Hit {hit.collider.name}, but it was not linked to a dialogue source.");
        }
        else
        {
            LogDebug("Interact cast did not hit anything.");
        }

        return null;
    }

    private Ray GetInteractionRay()
    {
        Transform origin = interactOrigin != null ? interactOrigin : transform;
        return new Ray(origin.position, origin.forward);
    }

    private Dialogue_Source GetDialogueSourceFromCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        GameObject dialogueCharacter = GetDialogueCharacterFromCollider(hitCollider);
        if (dialogueCharacter == null)
        {
            LogDebug($"Hit {hitCollider.name}, but no parent has tag {dialogueCharacterTag}.");
            return null;
        }

        Dialogue_Source source = hitCollider.GetComponentInParent<Dialogue_Source>();
        if (source != null && source.BelongsTo(dialogueCharacter))
            return source;

        Dialogue_Source[] dialogueSources = FindObjectsByType<Dialogue_Source>(FindObjectsSortMode.None);
        for (int i = 0; i < dialogueSources.Length; i++)
        {
            if (dialogueSources[i] != null && dialogueSources[i].BelongsTo(dialogueCharacter))
                return dialogueSources[i];
        }

        LogDebug($"Found dialogue character {dialogueCharacter.name}, but no Dialogue_Source references it.");

        return null;
    }

    private GameObject GetDialogueCharacterFromCollider(Collider hitCollider)
    {
        Transform current = hitCollider.transform;

        while (current != null)
        {
            if (current.CompareTag(dialogueCharacterTag))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private void LogDebug(string message)
    {
        if (debugLogs)
            Debug.Log($"[{nameof(Player_DialogueInteract)}] {message}", this);
    }

    private void EnableInteractAction()
    {
        InputAction action = interactAction != null ? interactAction.action : null;
        if (action == null || action.enabled)
            return;

        action.Enable();
        interactActionEnabledByThisScript = true;
    }

    private void DisableInteractAction()
    {
        InputAction action = interactAction != null ? interactAction.action : null;
        if (action == null || !interactActionEnabledByThisScript)
            return;

        action.Disable();
        interactActionEnabledByThisScript = false;
    }

    private void OnDrawGizmosSelected()
    {
        Ray ray = GetInteractionRay();
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(ray.origin, ray.direction * interactRange);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * interactRange, castRadius);
    }
}
