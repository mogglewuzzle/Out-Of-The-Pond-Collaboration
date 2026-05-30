using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintAcceleration = 0.3f;
    [Tooltip("Horizontal movement multiplier while the player is not grounded.")]
    [SerializeField, Range(0f, 5f)] private float airborneMovementMultiplier = 0.6f;

    [Header("Gravity")]
    [Tooltip("Extra gravity applied any time the player is not grounded. 0 means no extra airborne gravity.")]
    [SerializeField] private float airborneGravityMultiplier = 0.5f;
    [Tooltip("Extra gravity applied while the player is falling. 1 uses normal gravity; higher values fall faster.")]
    [SerializeField] private float fallGravityMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPoint;
    [SerializeField] private float groundCheckDistance = 0.55f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckOriginOffset = 0.35f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Wall Contact")]
    [Tooltip("Surfaces with an upward-facing normal below this value are treated as walls.")]
    [SerializeField, Range(0f, 1f)] private float wallNormalMaxUpDot = 0.5f;

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
    private Vector3 externalVelocity;
    private Vector3 launchVelocity;
    private float launchTimer;
    private float launchDuration;

    private Rigidbody rb;
    private Collider[] playerColliders;
    private readonly List<Vector3> wallContactNormals = new List<Vector3>();

    public float CurrentHorizontalSpeed { get; private set; }
    public bool IsGroundedCached { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        playerColliders = GetComponentsInChildren<Collider>();

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
        wallContactNormals.Clear();
        HandleJump();
        ApplyAirborneGravity();
        ApplyFallGravity();
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
        if (!IsGroundedCached)
            targetSpeed *= airborneMovementMultiplier;

        Vector3 move = moveInputWorld * targetSpeed;
        RemoveMovementIntoWalls(ref move);
        Vector3 finalVelocity = move + GetLaunchVelocity() + externalVelocity;

        rb.linearVelocity = new Vector3(
            finalVelocity.x,
            rb.linearVelocity.y + finalVelocity.y,
            finalVelocity.z
        );

        externalVelocity = Vector3.zero;
        TickLaunch();

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

    private void ApplyFallGravity()
    {
        if (swing != null && swing.IsSwinging)
            return;

        if (IsGroundedCached || rb.linearVelocity.y >= 0f)
            return;

        rb.AddForce(Physics.gravity * fallGravityMultiplier, ForceMode.Acceleration);
    }

    private void ApplyAirborneGravity()
    {
        if (swing != null && swing.IsSwinging)
            return;

        if (IsGroundedCached || airborneGravityMultiplier <= 0f)
            return;

        rb.AddForce(Physics.gravity * airborneGravityMultiplier, ForceMode.Acceleration);
    }

    private bool IsGrounded()
    {
        Vector3 origin = GetGroundCheckOrigin();
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            groundCheckRadius,
            Vector3.down,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (
                hitCollider != null &&
                !IsPlayerCollider(hitCollider) &&
                hits[i].normal.y >= 0.5f
            )
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        CacheWallContactNormals(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CacheWallContactNormals(collision);
    }

    private void CacheWallContactNormals(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            if (Mathf.Abs(normal.y) <= wallNormalMaxUpDot)
                wallContactNormals.Add(normal);
        }
    }

    private void RemoveMovementIntoWalls(ref Vector3 move)
    {
        for (int i = 0; i < wallContactNormals.Count; i++)
        {
            Vector3 wallNormal = wallContactNormals[i];

            if (Vector3.Dot(move, wallNormal) < 0f)
                move = Vector3.ProjectOnPlane(move, wallNormal);
        }
    }

    private void DrawGroundDebug()
    {
        Vector3 origin = GetGroundCheckOrigin();

        bool grounded = IsGroundedCached;

        Debug.DrawRay(
            origin,
            Vector3.down * debugRayLength,
            grounded ? Color.green : Color.red
        );

        Debug.Log("Grounded: " + grounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugGroundCheck)
            return;

        Vector3 origin = GetGroundCheckOrigin();
        Vector3 end = origin + Vector3.down * groundCheckDistance;
        Color color = IsGroundedCached ? Color.green : Color.red;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
        Gizmos.DrawWireSphere(end, groundCheckRadius);

        Vector3 right = Vector3.right * groundCheckRadius;
        Vector3 forward = Vector3.forward * groundCheckRadius;

        Gizmos.DrawLine(origin + right, end + right);
        Gizmos.DrawLine(origin - right, end - right);
        Gizmos.DrawLine(origin + forward, end + forward);
        Gizmos.DrawLine(origin - forward, end - forward);
    }

    private Vector3 GetGroundCheckOrigin()
    {
        Vector3 origin = feetPoint != null ? feetPoint.position : transform.position;
        return origin + Vector3.up * groundCheckOriginOffset;
    }

    private bool IsPlayerCollider(Collider hitCollider)
    {
        if (playerColliders == null)
            return false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (hitCollider == playerColliders[i])
                return true;
        }

        return false;
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

    public void AddExternalVelocity(Vector3 velocity)
    {
        externalVelocity += velocity;
    }

    public void BeginExternalLaunch(Vector3 velocity, float duration)
    {
        launchVelocity = velocity;
        launchDuration = Mathf.Max(0f, duration);
        launchTimer = launchDuration;
    }

    private Vector3 GetLaunchVelocity()
    {
        if (launchTimer <= 0f || launchDuration <= 0f)
            return Vector3.zero;

        float t = launchTimer / launchDuration;
        return launchVelocity * t;
    }

    private void TickLaunch()
    {
        if (launchTimer <= 0f)
            return;

        launchTimer -= Time.fixedDeltaTime;
        if (launchTimer <= 0f)
        {
            launchTimer = 0f;
            launchVelocity = Vector3.zero;
        }
    }
}
