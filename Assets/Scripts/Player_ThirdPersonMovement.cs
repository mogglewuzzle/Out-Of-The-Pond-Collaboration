using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerThirdPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotation — Right Stick (Rate Control)")]
    [Tooltip("Max rotation speed in degrees per second at full stick deflection.")]
    [SerializeField] private float rotationSpeed = 360f;

    [Tooltip("Deadzone radius. Stick input below this magnitude is ignored.")]
    [SerializeField, Range(0f, 0.5f)] private float stickDeadzone = 0.15f;

    [Tooltip("Response curve exponent. 1 = linear. 2 = quadratic (more precision near centre, " +
             "faster at edges). AAA standard is 1.5–2.5.")]
    [SerializeField, Range(1f, 3f)] private float stickCurve = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Aim")]
    [Tooltip("Your normal Cinemachine Virtual Camera.")]
    [SerializeField] private CinemachineCamera normalVCam;
    [Tooltip("Your aim Cinemachine Virtual Camera. Disable it in the scene.")]
    [SerializeField] private CinemachineCamera aimVCam;
    [Tooltip("Left/right rotation speed multiplier while aiming. 1 = same as normal, 0.1 = very slow.")]
    [SerializeField, Range(0.1f, 1f)] private float aimRotationMultiplier = 0.5f;
    [Tooltip("Up/down pitch speed multiplier while aiming. 1 = same as Pitch Speed, 0.1 = very slow.")]
    [SerializeField, Range(0.1f, 1f)] private float aimPitchMultiplier = 0.5f;

    [Header("Aim Pitch (Look Up/Down)")]
    [Tooltip("Empty child GameObject at shoulder height. VCam_Aim should follow this.")]
    [SerializeField] private Transform aimPivot;
    [Tooltip("Vertical look speed in degrees per second at full stick deflection.")]
    [SerializeField] private float pitchSpeed = 120f;
    [Tooltip("How far up the player can look while aiming.")]
    [SerializeField, Range(0f, 90f)] private float pitchMax = 60f;
    [Tooltip("How far down the player can look while aiming.")]
    [SerializeField, Range(0f, 90f)] private float pitchMin = 40f;
    [Tooltip("Invert the vertical aim axis.")]
    [SerializeField] private bool invertPitch = false;

    [Header("UI")]
    [Tooltip("Assign your crosshair Panel here in the Inspector.")]
    [SerializeField] private GameObject crosshairPanel;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpRequested;
    private float currentYaw;
    private float currentPitch;
    private bool isAiming;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentYaw = transform.eulerAngles.y;

        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled  += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled  += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx =>
        {
            if (IsGrounded()) jumpRequested = true;
        };

        inputActions.Player.Aim.performed += ctx => SetAiming(true);
        inputActions.Player.Aim.canceled  += ctx => SetAiming(false);
    }

    private void OnEnable()  => inputActions.Player.Enable();
    private void OnDisable() => inputActions.Player.Disable();

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        UpdateRotationInput();
        if (isAiming) UpdatePitchInput();

        UpdateMovement();
        ApplyRotation();

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    private void SetAiming(bool aiming)
    {
        isAiming = aiming;
        aimVCam.gameObject.SetActive(aiming);

        if (crosshairPanel != null)
            crosshairPanel.SetActive(aiming);

        if (!aiming && aimPivot != null)
        {
            currentPitch = 0f;
            aimPivot.localRotation = Quaternion.identity;
        }
    }

    private void UpdateRotationInput()
    {
        float rawX = Mathf.Abs(lookInput.x);
        if (rawX < stickDeadzone) return;

        float remappedX   = Mathf.Clamp01((rawX - stickDeadzone) / (1f - stickDeadzone));
        float curvedInput = Mathf.Sign(lookInput.x) * Mathf.Pow(remappedX, stickCurve);

        float speedThisFrame = rotationSpeed * (isAiming ? aimRotationMultiplier : 1f);
        currentYaw += curvedInput * speedThisFrame * Time.fixedDeltaTime;
    }

    private void UpdatePitchInput()
    {
        if (aimPivot == null) return;

        float rawY = Mathf.Abs(lookInput.y);
        if (rawY < stickDeadzone) return;

        float remappedY   = Mathf.Clamp01((rawY - stickDeadzone) / (1f - stickDeadzone));
        float curvedInput = Mathf.Sign(lookInput.y) * Mathf.Pow(remappedY, stickCurve);
        float direction   = invertPitch ? 1f : -1f;

        currentPitch += curvedInput * pitchSpeed * aimPitchMultiplier * direction * Time.fixedDeltaTime;
        currentPitch  = Mathf.Clamp(currentPitch, -pitchMax, pitchMin);

        aimPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void ApplyRotation()
    {
        rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
    }

    private void UpdateMovement()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;

        camForward.y = 0f; camForward.Normalize();
        camRight.y   = 0f; camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    private bool IsGrounded() =>
        Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
}