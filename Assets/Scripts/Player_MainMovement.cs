using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintAcceleration = 0.3f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPoint;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Debug")]

    [Tooltip("Green Ray = Grounded\nRed Ray = Not Grounded")]
    [SerializeField] private bool debugGroundCheck = false;   // Tooltip now on checkbox

    [SerializeField] private float debugRayLength = 1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private PlayerInputHandler input;
    private PlayerSprintController sprint;
    private PlayerJumpController jump;
    private Player_TongueSwing swing;

    // for calculating acceleration/deceleration when sprinting
    private float currentSpeed;

    private Rigidbody rb;

    public float CurrentHorizontalSpeed { get; private set; }
    public bool IsGroundedCached { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        input  = GetComponent<PlayerInputHandler>();
        sprint = GetComponent<PlayerSprintController>();
        jump   = GetComponent<PlayerJumpController>();
        swing  = GetComponent<Player_TongueSwing>();

        currentSpeed = moveSpeed;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null)
            return;

        IsGroundedCached = IsGrounded();

        Move();
        HandleJump();
    }

    private void Update()
    {
        if (debugGroundCheck)
            DrawGroundDebug();
    }

    private void Move()
    {
        if (swing != null && swing.IsSwinging)
            return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;

        camForward.y = 0f; camForward.Normalize();
        camRight.y   = 0f; camRight.Normalize();

        Vector3 moveInputWorld =
            camForward * input.MoveInput.y +
            camRight   * input.MoveInput.x;

        moveInputWorld = Vector3.ClampMagnitude(moveInputWorld, 1f);

        float targetSpeed = Accelerate();

        Vector3 move = moveInputWorld * targetSpeed;

        rb.linearVelocity = new Vector3(
            move.x,
            rb.linearVelocity.y,
            move.z
        );

        CurrentHorizontalSpeed = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        ).magnitude;
    }

    private void HandleJump()
    {
        if (swing != null && swing.IsSwinging)
            return;

        if (jump == null)
            return;

        if (!jump.ShouldJump)
            return;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(Vector3.up * jump.JumpForce, ForceMode.Impulse);

        jump.ConsumeJump();
    }

    private bool IsGrounded()
    {
        Vector3 origin = feetPoint != null ? feetPoint.position : transform.position;

        return Physics.Raycast(
            origin,
            Vector3.down,
            groundCheckDistance,
            groundMask
        );
    }

    private void DrawGroundDebug()
    {
        Vector3 origin = feetPoint != null ? feetPoint.position : transform.position;

        bool grounded = IsGroundedCached;

        Debug.DrawRay(
            origin,
            Vector3.down * debugRayLength,
            grounded ? Color.green : Color.red
        );

        Debug.Log("Grounded: " + grounded);
    }

    private float Accelerate()
    {
        // gradually speeds/slows down player when holding/letting go of sprint button,
        // smother transition in speed and animations
        if (sprint.IsSprinting)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, sprint.SprintSpeed, sprintAcceleration);    
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, sprintAcceleration);
        }

        return currentSpeed;
    }
}
