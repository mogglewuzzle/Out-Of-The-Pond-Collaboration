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

    [Header("Tags")]
    [SerializeField] private string AttractPointTag = "AttractPoint";
    [SerializeField] private string SwingPointTag = "SwingPoint";

    private Vector3 tipPosition;
    private Vector3 fireDirection;

    private bool wasHeld;
    private PlayerInputHandler input;

    private Player_TongueAttract attractModule;
    private Player_TongueSwing swingModule;

    private enum State { Idle, Extending, Retracting }
    private State state = State.Idle;

    private Camera cam;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        attractModule = GetComponent<Player_TongueAttract>();
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
        Vector3 aimTarget = cam.transform.position + cam.transform.forward * maxRange;
        fireDirection = (aimTarget - tongueOrigin.position).normalized;

        tipPosition = tongueOrigin.position;
        state = State.Extending;

        if (lineRenderer != null)
            lineRenderer.enabled = true;
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
        swingModule.StopSwing();
    }

    private void UpdateLine()
    {
        bool externalTongueActive =
            (attractModule != null && attractModule.IsAttracting) ||
            (swingModule != null && swingModule.IsSwinging);

        if (lineRenderer == null || (state == State.Idle && !externalTongueActive))
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
