using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Aim Pitch (Look Up/Down)")]
    [SerializeField] private Transform aimPivot;
    [SerializeField] private float pitchSpeed = 120f;
    [SerializeField, Range(0f, 90f)] private float pitchMax = 60f;
    [SerializeField, Range(0f, 90f)] private float pitchMin = 40f;
    [SerializeField] private bool invertPitch = false;
    [SerializeField, Range(0.1f, 1f)] private float aimPitchMultiplier = 0.5f;

    [Header("Free Look Pitch (Not Aiming)")]
    [SerializeField] private bool enableFreeLookPitch = true;
    [SerializeField] private Transform freeLookPivot;
    [SerializeField] private float freeLookPitchSpeed = 120f;
    [SerializeField, Range(0.1f, 1f)] private float freeLookPitchMultiplier = 1f;
    [SerializeField, Range(0f, 90f)] private float freeLookPitchMax = 60f;
    [SerializeField, Range(0f, 90f)] private float freeLookPitchMin = 40f;
    [SerializeField] private bool invertFreeLookPitch = false;

    [Header("Free Look Recenter")]
    [SerializeField] private bool freeLookPitchRecenter = false;
    [SerializeField] private float freeLookRecenterSpeed = 90f;
    [SerializeField] private float recenterDelay = 0.5f;

    [Header("References")]
    [SerializeField] private PlayerMovementController movement;

    [Header("Recenter Curve")]
    [SerializeField] private float recenterCurvePower = 2f;

    [Header("Stick Filtering")]
    [SerializeField, Range(0f, 0.5f)] private float stickDeadzone = 0.15f;
    [SerializeField, Range(1f, 3f)] private float stickCurve = 2f;

    private PlayerInputHandler input;
    private PlayerAimController aim;
    private PlayerFreeCameraController freeCamera;

    private float currentPitch;
    private float currentFreeLookPitch;
    private bool wasAiming;

    private float timeSinceLookInput;
    private bool isRecentering;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        aim = GetComponent<PlayerAimController>();
        freeCamera = GetComponent<PlayerFreeCameraController>();
    }

    private void FixedUpdate()
    {
        HandleAimModeTransitions();

        if (freeCamera != null && freeCamera.IsActive)
            return;

        if (aim != null && aim.IsAiming)
            UpdateAimPitch();
        else if (enableFreeLookPitch)
            UpdateFreeLookPitch();
    }

    private void HandleAimModeTransitions()
    {
        bool isAiming = aim != null && aim.IsAiming;

        if (isAiming && !wasAiming)
        {
            currentPitch = currentFreeLookPitch;

            if (aimPivot != null)
                aimPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
        else if (!isAiming && wasAiming)
        {
            currentFreeLookPitch = currentPitch;

            if (freeLookPivot != null)
                freeLookPivot.localRotation = Quaternion.Euler(currentFreeLookPitch, 0f, 0f);
        }

        wasAiming = isAiming;
    }

    private void UpdateAimPitch()
    {
        if (aimPivot == null)
            return;

        float rawY = Mathf.Abs(input.LookInput.y);
        if (rawY < stickDeadzone)
            return;

        float remapped = Mathf.Clamp01((rawY - stickDeadzone) / (1f - stickDeadzone));
        float curved = Mathf.Sign(input.LookInput.y) * Mathf.Pow(remapped, stickCurve);
        float direction = invertPitch ? 1f : -1f;

        currentPitch += curved * pitchSpeed * aimPitchMultiplier * direction * Time.fixedDeltaTime;
        currentPitch = Mathf.Clamp(currentPitch, -pitchMax, pitchMin);

        aimPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void UpdateFreeLookPitch()
    {
        if (freeLookPivot == null)
            return;

        float rawY = Mathf.Abs(input.LookInput.y);
        bool hasLookInput = rawY >= stickDeadzone;
        bool isGrounded = movement != null && movement.IsGroundedCached;

        if (hasLookInput || !isGrounded)
        {
            isRecentering = false;
            timeSinceLookInput = 0f;
        }
        else
        {
            timeSinceLookInput += Time.fixedDeltaTime;

            if (freeLookPitchRecenter &&
                isGrounded &&
                timeSinceLookInput >= recenterDelay)
            {
                isRecentering = true;
            }
        }

        if (isRecentering)
        {
            float distance = Mathf.Abs(currentFreeLookPitch);
            float directionSign = Mathf.Sign(currentFreeLookPitch);

            float speed = freeLookRecenterSpeed *
                          Mathf.Pow(distance / freeLookPitchMax, recenterCurvePower);

            currentFreeLookPitch -= directionSign * speed * Time.fixedDeltaTime;

            if (Mathf.Abs(currentFreeLookPitch) < 0.01f)
                currentFreeLookPitch = 0f;

            freeLookPivot.localRotation = Quaternion.Euler(currentFreeLookPitch, 0f, 0f);
            return;
        }

        if (!hasLookInput)
            return;

        float remapped = Mathf.Clamp01((rawY - stickDeadzone) / (1f - stickDeadzone));
        float curved = Mathf.Sign(input.LookInput.y) * Mathf.Pow(remapped, stickCurve);
        float direction = invertFreeLookPitch ? 1f : -1f;

        currentFreeLookPitch += curved * freeLookPitchSpeed * freeLookPitchMultiplier * direction * Time.fixedDeltaTime;
        currentFreeLookPitch = Mathf.Clamp(currentFreeLookPitch, -freeLookPitchMax, freeLookPitchMin);

        freeLookPivot.localRotation = Quaternion.Euler(currentFreeLookPitch, 0f, 0f);
    }
}
