using UnityEngine;

public class PlayerTongueProjection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform tongueOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Settings")]
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float tipRadius = 0.1f;
    [SerializeField] private LayerMask collidableLayers = ~0;
    [SerializeField] private LayerMask tongueLayerMask;

    [Header("Aim Offsets")]
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private Vector3 freeLookOffset = Vector3.zero;

    [Header("Tags")]
    [SerializeField] private string AttractPointTag = "AttractPoint";
    [SerializeField] private string GrabPointTag = "GrabPoint";
    [SerializeField] private string SwingPointTag = "SwingPoint";

    private Vector3 tipPosition;
    private Vector3 fireDirection;

    private bool wasHeld;
    private PlayerInputHandler input;
    private PlayerAimController aimController;
    private PlayerFreeCameraController freeCameraController;

    private Player_TongueAttract attractModule;
    private Player_TongueGrab grabModule;
    private Player_TongueSwing swingModule;

    private enum State { Idle, Extending, Retracting }
    private State state = State.Idle;

    public bool IsTongueActive =>
        state != State.Idle ||
        (attractModule != null && attractModule.IsAttracting) ||
        (grabModule != null && grabModule.IsGrabbing) ||
        (swingModule != null && swingModule.IsSwinging);

    private Camera cam;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        aimController = GetComponent<PlayerAimController>();
        freeCameraController = GetComponent<PlayerFreeCameraController>();
        attractModule = GetComponent<Player_TongueAttract>();
        grabModule = GetComponent<Player_TongueGrab>();
        swingModule = GetComponent<Player_TongueSwing>();

        cam = Camera.main;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        bool held = input != null && input.TongueThrowHeld;

        if (held && !wasHeld && state == State.Idle)
            FireTongue();

        if (!held && wasHeld)
            BeginRetract();

        wasHeld = held;

        TickState();
        UpdateLine();
    }

    private void FireTongue()
    {
        Vector3 aimTarget = ComputeAimTarget();
        fireDirection = (aimTarget - tongueOrigin.position).normalized;

        tipPosition = tongueOrigin.position;
        state = State.Extending;

        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    private Vector3 ComputeAimTarget()
    {
        if (cam == null)
            cam = Camera.main;

        Vector3 baseTarget = cam.transform.position + cam.transform.forward * maxRange;

        bool isFreeLookActive = freeCameraController != null && freeCameraController.IsActive;
        bool isAiming = aimController != null && aimController.IsAiming;

        Vector3 offset = (isAiming || isFreeLookActive) ? aimOffset : freeLookOffset;

        return baseTarget
             + cam.transform.right * offset.x
             + cam.transform.up * offset.y
             + cam.transform.forward * offset.z;
    }

    private void TickState()
    {
        switch (state)
        {
            case State.Extending: TickExtending(); break;
            case State.Retracting: TickRetracting(); break;
        }
    }

    private void TickExtending()
    {
        Vector3 prev = tipPosition;
        tipPosition += fireDirection * extendSpeed * Time.deltaTime;

        Vector3 step = tipPosition - prev;
        float stepDist = step.magnitude;

        if (Physics.SphereCast(prev, tipRadius, step.normalized, out RaycastHit hit, stepDist, collidableLayers & ~tongueLayerMask))
        {
            tipPosition = hit.point;

            if (hit.collider.CompareTag(AttractPointTag))
            {
                attractModule.BeginAttract(hit.point);
                state = State.Idle;
            }
            else if (hit.collider.CompareTag(GrabPointTag))
            {
                Rigidbody targetRb = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;

                if (targetRb != null && grabModule != null)
                {
                    grabModule.BeginGrab(targetRb);
                    state = State.Idle;
                }
                else
                {
                    BeginRetract();
                }
            }
            else if (hit.collider.CompareTag(SwingPointTag))
            {
                swingModule.BeginSwing(hit.point);
                state = State.Idle;
            }
            else
            {
                BeginRetract();
            }

            return;
        }

        if (Vector3.Distance(tongueOrigin.position, tipPosition) >= maxRange)
            BeginRetract();
    }

    private void TickRetracting()
    {
        tipPosition = Vector3.MoveTowards(tipPosition, tongueOrigin.position, retractSpeed * Time.deltaTime);

        if (Vector3.Distance(tipPosition, tongueOrigin.position) < 0.01f)
        {
            state = State.Idle;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }
    }

    public void BeginRetract()
    {
        state = State.Retracting;
        attractModule.StopAttract();
        if (grabModule != null)
            grabModule.StopGrab();
        swingModule.StopSwing();
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || !IsTongueActive)
            return;

        if (!lineRenderer.enabled)
            lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, tongueOrigin.position);
        lineRenderer.SetPosition(1, tipPosition);
    }

    public void SetTipPosition(Vector3 pos)
    {
        tipPosition = pos;
    }
}
