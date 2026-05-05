using UnityEngine;
using System.Collections;

public class Player_TongueSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    [SerializeField] private float swingGravityMultiplier = 2f;
    [SerializeField] private float swingHorizontalAcceleration = 20f;
    [SerializeField] private float swingGroundDelay = 0.15f;
    [SerializeField] private float swingStartupPullForce = 12f;
    [SerializeField] private float swingMinGroundDistance = 1.5f;
    [SerializeField] private float maxSwingRopeLength = 25f;

    [Header("Release Boost")]
    [SerializeField] private float releaseForwardSpeed = 12f;
    [SerializeField] private float releaseUpSpeed = 3f;
    [SerializeField] private float releaseLaunchDuration = 0.35f;
    [SerializeField] private float minSwingTimeForReleaseBoost = 0.25f;

    [Header("References")]
    [SerializeField] private Transform feetPoint;
    [SerializeField] private LayerMask groundMask = ~0;

    private Rigidbody rb;
    private PlayerMovementController movement;
    private PlayerInputHandler input;
    private PlayerTongueProjection projection;
    private Camera cam;

    private bool active;
    private bool startingSwing;
    private Vector3 latchPoint;
    private float ropeLength;
    private float activeSwingStartTime = -999f;
    private Coroutine swingStartupRoutine;

    public bool IsSwinging => active || startingSwing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovementController>();
        input = GetComponent<PlayerInputHandler>();
        projection = GetComponent<PlayerTongueProjection>();
        cam = Camera.main;
    }

    public void BeginSwing(Vector3 point)
    {
        StopSwing();
        latchPoint = point;
        ropeLength = Vector3.Distance(rb.position, latchPoint);
        ropeLength = Mathf.Min(ropeLength, maxSwingRopeLength);

        active = false;
        startingSwing = true;
        swingStartupRoutine = StartCoroutine(SwingStartup());
    }

    public void StopSwing()
    {
        StopSwing(false);
    }

    public void ReleaseSwing()
    {
        StopSwing(true);
    }

    private void StopSwing(bool applyReleaseBoost)
    {
        bool wasActive = active;

        if (swingStartupRoutine != null)
        {
            StopCoroutine(swingStartupRoutine);
            swingStartupRoutine = null;
        }

        active = false;
        startingSwing = false;

        if (applyReleaseBoost && wasActive)
            ApplyReleaseBoost();
    }

    private IEnumerator SwingStartup()
    {
        if (movement != null && movement.IsGroundedCached)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            rb.AddForce(Vector3.up * swingStartupPullForce, ForceMode.Impulse);

            if (swingGroundDelay > 0f)
                yield return new WaitForSeconds(swingGroundDelay);
        }

        startingSwing = false;
        active = true;
        activeSwingStartTime = Time.time;
        swingStartupRoutine = null;
    }

    private void ApplyReleaseBoost()
    {
        if (Time.time - activeSwingStartTime < minSwingTimeForReleaseBoost)
            return;

        Vector3 releaseDirection = GetReleaseForwardDirection();
        Vector3 launchVelocity = Vector3.zero;

        if (releaseDirection != Vector3.zero && releaseForwardSpeed > 0f)
            launchVelocity += releaseDirection * releaseForwardSpeed;

        if (releaseUpSpeed > 0f)
            launchVelocity += Vector3.up * releaseUpSpeed;

        if (launchVelocity == Vector3.zero)
            return;

        if (movement != null)
            movement.BeginExternalLaunch(launchVelocity, releaseLaunchDuration);
        else
            rb.linearVelocity += launchVelocity;
    }

    private Vector3 GetReleaseForwardDirection()
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        if (horizontalVelocity.sqrMagnitude > 0.01f)
            return horizontalVelocity.normalized;

        Vector2 move = input != null ? input.MoveInput : Vector2.zero;
        if (move.sqrMagnitude > 0.01f && cam != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            Vector3 inputDirection = camForward * move.y + camRight * move.x;

            if (inputDirection.sqrMagnitude > 0.01f)
                return inputDirection.normalized;
        }

        Vector3 playerForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (playerForward.sqrMagnitude > 0.01f)
            return playerForward.normalized;

        if (cam != null)
        {
            Vector3 cameraForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude > 0.01f)
                return cameraForward.normalized;
        }

        return Vector3.zero;
    }

    private void Update()
    {
        if (!IsSwinging)
            return;

        projection.SetTipPosition(latchPoint);
    }

    private void FixedUpdate()
    {
        if (!active)
            return;

        rb.AddForce(Physics.gravity * swingGravityMultiplier, ForceMode.Acceleration);

        Vector2 move = input != null ? input.MoveInput : Vector2.zero;

        if (move.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = cam.transform.right;

            Vector3 inputDir = (camForward * move.y + camRight * move.x).normalized;
            rb.AddForce(inputDir * swingHorizontalAcceleration, ForceMode.Acceleration);
        }

        if (feetPoint != null)
        {
            if (Physics.Raycast(feetPoint.position, Vector3.down, out RaycastHit hit, 5f, groundMask))
            {
                float dist = hit.distance;
                if (dist < swingMinGroundDistance)
                {
                    float shorten = swingMinGroundDistance - dist;
                    ropeLength -= shorten;
                    ropeLength = Mathf.Max(0.5f, ropeLength);
                }
            }
        }

        Vector3 toPlayer = rb.position - latchPoint;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return;

        Vector3 dir = toPlayer.normalized;

        Vector3 constrainedPos = latchPoint + dir * ropeLength;

        Vector3 vel = rb.linearVelocity;
        Vector3 radial = Vector3.Project(vel, dir);
        Vector3 tangential = vel - radial;

        rb.position = constrainedPos;
        rb.linearVelocity = tangential;

        float tongueLength = Vector3.Distance(rb.position, latchPoint);
        if (tongueLength <= 1f)
        {
            active = false;
            projection.BeginRetract();
        }
    }

    private void OnDisable()
    {
        StopSwing();
    }
}
