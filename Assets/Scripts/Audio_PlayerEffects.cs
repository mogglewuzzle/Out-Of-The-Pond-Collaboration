using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Player Effects")]
public class Audio_PlayerEffects : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource movementLoopSource;

    [Header("Movement Loops")]
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip runLoopClip;
    [Tooltip("Playback speed for the normal walking loop. 1 is original speed.")]
    [SerializeField] private float walkLoopPitch = 1f;
    [Tooltip("Playback speed for the sprint/running loop. 1 is original speed, 1.2 is 20% faster.")]
    [SerializeField] private float runLoopPitch = 1f;
    [SerializeField] private float minMoveInput = 0.1f;
    [SerializeField] private bool requireGroundedForMovementLoops = true;

    [Header("Input Sounds")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip sprintPressedClip;
    [Tooltip("Playback speed for the one-shot sprint button sound. 1 is original speed.")]
    [SerializeField] private float sprintPressedPitch = 1f;
    [SerializeField] private AudioClip tonguePressedClip;
    [SerializeField] private AudioClip interactPressedClip;
    [SerializeField] private AudioClip pickupPressedClip;
    [SerializeField] private AudioClip throwPressedClip;
    [SerializeField] private AudioClip consumePressedClip;

    [Header("Player Lookup")]
    [Tooltip("If enabled, this manager can live outside the player and will find the first object tagged Player.")]
    [SerializeField] private bool autoFindPlayerByTag = true;
    [Tooltip("If enabled, falls back to finding the first PlayerInputHandler when the Player tag finds the wrong child object or no tagged object exists.")]
    [SerializeField] private bool autoFindPlayerByInputHandler = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float playerSearchInterval = 0.5f;
    [Tooltip("Runtime display only. Shows the player object this audio manager is currently reading input from.")]
    [SerializeField] private GameObject foundPlayer;

    private PlayerInputHandler input;
    private PlayerMovementController movement;
    private PlayerSprintController sprint;
    private float nextPlayerSearchTime;

    private bool previousSprintHeld;
    private bool previousTongueHeld;

    private void Awake()
    {
        EnsureSources();
        FindPlayerComponents();
    }

    private void Update()
    {
        RefreshPlayerIfNeeded();

        if (input == null)
        {
            foundPlayer = null;
            StopMovementLoop();
            return;
        }

        PlayPressedInputSounds();
        UpdateMovementLoop();
    }

    public void PlayJump()
    {
        PlayOneShot(jumpClip);
    }

    public void PlayTongue()
    {
        PlayOneShot(tonguePressedClip);
    }

    private void PlayPressedInputSounds()
    {
        if (input.JumpPressed)
            PlayOneShot(jumpClip);

        if (input.SprintHeld && !previousSprintHeld)
            PlayOneShot(sprintPressedClip, sprintPressedPitch);

        if (input.TongueThrowHeld && !previousTongueHeld)
            PlayOneShot(tonguePressedClip);

        if (input.InteractPressed)
            PlayOneShot(interactPressedClip);

        if (input.PickUpPressed)
            PlayOneShot(pickupPressedClip);

        if (input.ThrowObjectReleased)
            PlayOneShot(throwPressedClip);

        if (input.ConsumePressed)
            PlayOneShot(consumePressedClip);

        previousSprintHeld = input.SprintHeld;
        previousTongueHeld = input.TongueThrowHeld;
    }

    private void UpdateMovementLoop()
    {
        if (movementLoopSource == null)
            return;

        bool hasMoveInput = input.MoveInput.sqrMagnitude >= minMoveInput * minMoveInput;
        bool grounded = movement == null || movement.IsGroundedCached;
        bool canPlayMovement = hasMoveInput && (!requireGroundedForMovementLoops || grounded);

        if (!canPlayMovement)
        {
            StopMovementLoop();
            return;
        }

        bool isSprinting = sprint != null && sprint.IsSprinting;
        AudioClip targetClip = isSprinting ? runLoopClip : walkLoopClip;
        float targetPitch = isSprinting ? runLoopPitch : walkLoopPitch;
        PlayMovementLoop(targetClip, targetPitch);
    }

    private void PlayMovementLoop(AudioClip clip, float pitch)
    {
        if (clip == null)
        {
            StopMovementLoop();
            return;
        }

        movementLoopSource.pitch = Mathf.Max(0.01f, pitch);

        if (movementLoopSource.clip == clip && movementLoopSource.isPlaying)
            return;

        movementLoopSource.clip = clip;
        movementLoopSource.loop = true;
        movementLoopSource.Play();
    }

    private void StopMovementLoop()
    {
        if (movementLoopSource != null && movementLoopSource.isPlaying)
            movementLoopSource.Stop();
    }

    private void PlayOneShot(AudioClip clip)
    {
        PlayOneShot(clip, 1f);
    }

    private void PlayOneShot(AudioClip clip, float pitch)
    {
        if (oneShotSource == null || clip == null)
            return;

        oneShotSource.pitch = Mathf.Max(0.01f, pitch);
        oneShotSource.PlayOneShot(clip);
    }

    private void EnsureSources()
    {
        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        if (movementLoopSource == null)
            movementLoopSource = gameObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        movementLoopSource.playOnAwake = false;
        movementLoopSource.loop = true;
    }

    private void RefreshPlayerIfNeeded()
    {
        if (input != null)
            return;

        if ((!autoFindPlayerByTag && !autoFindPlayerByInputHandler) || Time.unscaledTime < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.unscaledTime + Mathf.Max(0.1f, playerSearchInterval);
        FindPlayerComponents();
    }

    private void FindPlayerComponents()
    {
        PlayerInputHandler localInput = GetComponent<PlayerInputHandler>();
        if (localInput != null)
        {
            AssignPlayer(localInput.gameObject);
            return;
        }

        if (autoFindPlayerByTag && !string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (TryAssignPlayer(playerObject))
                return;
        }

        if (!autoFindPlayerByInputHandler)
            return;

        PlayerInputHandler foundInput = FindFirstObjectByType<PlayerInputHandler>();
        if (foundInput != null)
            AssignPlayer(foundInput.gameObject);
    }

    private bool TryAssignPlayer(GameObject playerObject)
    {
        if (playerObject == null)
            return false;

        PlayerInputHandler foundInput = playerObject.GetComponent<PlayerInputHandler>();
        if (foundInput == null)
            foundInput = playerObject.GetComponentInParent<PlayerInputHandler>();

        if (foundInput == null)
            foundInput = playerObject.GetComponentInChildren<PlayerInputHandler>();

        if (foundInput == null)
            return false;

        AssignPlayer(foundInput.gameObject);
        return true;
    }

    private void AssignPlayer(GameObject playerObject)
    {
        foundPlayer = playerObject;
        input = foundPlayer.GetComponent<PlayerInputHandler>();
        movement = foundPlayer.GetComponent<PlayerMovementController>();
        sprint = foundPlayer.GetComponent<PlayerSprintController>();
        previousSprintHeld = input != null && input.SprintHeld;
        previousTongueHeld = input != null && input.TongueThrowHeld;
    }
}
