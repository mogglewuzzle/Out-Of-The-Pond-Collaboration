using UnityEngine;

public class PlayerTongueProjection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform tongueOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Screen Center Fire Point")]
    [Tooltip("When enabled, the tongue starts from the camera's screen-center ray and fires straight through the crosshair.")]
    [SerializeField] private bool useScreenCenterFirePoint = true;
    [Tooltip("Optional visible Transform that is moved to the screen-center fire position every frame for testing and debugging.")]
    [SerializeField] private Transform screenCenterFirePoint;
    [Tooltip("Distance in front of the camera where the screen-center fire point is placed.")]
    [SerializeField] private float screenCenterFirePointDistance = 0.5f;
    [Tooltip("Viewport offset applied to the screen-center ray while aiming or using free camera. X/Y values of 0.5 equal half the screen size.")]
    [SerializeField] private Vector2 screenCenterAimOffset = Vector2.zero;
    [Tooltip("Viewport offset applied to the screen-center ray during normal free-look movement. X/Y values of 0.5 equal half the screen size.")]
    [SerializeField] private Vector2 screenCenterFreeLookOffset = Vector2.zero;

    [Header("Tongue Visual Shape")]
    [Tooltip("When enabled, the LineRenderer uses multiple points to draw a curved tongue from the tongue origin to the tip.")]
    [SerializeField] private bool useCurvedTongueLine;
    [Tooltip("Number of points used to draw the curved tongue. Higher values make the curve smoother.")]
    [SerializeField] private int curvedTongueLinePoints = 12;
    [Tooltip("How strongly the visual tongue bends toward the captured screen-center fire path. Values above 1 can exaggerate the curve.")]
    [SerializeField] private float tongueCurveBend = 0.5f;

    [Header("Hook Timing")]
    [Tooltip("How long the tongue tip stays hooked when it reaches max distance without hitting anything.")]
    [SerializeField] private float maxDistanceHookDuration = 0.15f;
    [Tooltip("How long the tongue tip stays hooked when it hits a collider that is not an attract, grab, or swing point.")]
    [SerializeField] private float surfaceHookDuration = 0.1f;

    [Header("Settings")]
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float tipRadius = 0.1f;
    [SerializeField] private LayerMask collidableLayers = ~0;
    [SerializeField] private LayerMask tongueLayerMask;

    [Header("Tags")]
    [SerializeField] private string AttractPointTag = "AttractPoint";
    [SerializeField] private string GrabPointTag = "GrabPoint";
    [SerializeField] private string SwingPointTag = "SwingPoint";

    private Vector3 tipPosition;
    private Vector3 fireDirection;
    private Vector3 fireStartPosition;
    private float hookTimer;

    private bool wasHeld;
    private PlayerInputHandler input;
    private PlayerAimController aimController;
    private PlayerFreeCameraController freeCameraController;

    private Player_TongueAttract attractModule;
    private Player_TongueGrab grabModule;
    private Player_TongueSwing swingModule;

    private enum State { Idle, Extending, Hooked, Retracting }
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
            lineRenderer.positionCount = GetLinePositionCount();
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        UpdateScreenCenterFirePoint();

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
        /* 
        * always make tounge go towards center of screen
        * when testing, just using tongueOrigin.forward as direction makes it very hard to aim
        */

        Ray screenCenterRay = GetScreenCenterRay();
        fireDirection = screenCenterRay.direction;

        if (useScreenCenterFirePoint)
        {
            fireStartPosition = GetFireStartPosition(screenCenterRay);
        }
        else
        {
            // not useScreenCenterFirePoint prefered, as the tounge doesn't start from behind the frog and in the actual mouth
            fireStartPosition = tongueOrigin.position;
        }

        tipPosition = fireStartPosition;
        state = State.Extending;

        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    private Ray GetScreenCenterRay()
    {
        if (cam == null)
            cam = Camera.main;

        Vector2 offset = GetScreenCenterOffset();
        return cam.ViewportPointToRay(new Vector3(0.5f + offset.x, 0.5f + offset.y, 0f));
    }

    private Vector2 GetScreenCenterOffset()
    {
        bool isFreeLookActive = freeCameraController != null && freeCameraController.IsActive;
        bool isAiming = aimController != null && aimController.IsAiming;

        return (isAiming || isFreeLookActive) ? screenCenterAimOffset : screenCenterFreeLookOffset;
    }

    private Vector3 GetFireStartPosition(Ray screenCenterRay)
    {
        if (screenCenterFirePoint != null)
            return screenCenterFirePoint.position;

        return screenCenterRay.GetPoint(screenCenterFirePointDistance);
    }

    private Vector3 GetLineStartPosition()
    {
        return tongueOrigin.position;
    }

    private int GetLinePositionCount()
    {
        return useCurvedTongueLine ? Mathf.Max(3, curvedTongueLinePoints) : 2;
    }

    private void UpdateScreenCenterFirePoint()
    {
        if (!useScreenCenterFirePoint || screenCenterFirePoint == null)
            return;

        Ray screenCenterRay = GetScreenCenterRay();
        screenCenterFirePoint.SetPositionAndRotation(
            screenCenterRay.GetPoint(screenCenterFirePointDistance),
            Quaternion.LookRotation(screenCenterRay.direction, cam.transform.up));
    }

    private void TickState()
    {
        switch (state)
        {
            case State.Extending: TickExtending(); break;
            case State.Hooked: TickHooked(); break;
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
                BeginHook(surfaceHookDuration);
            }

            return;
        }

        if (Vector3.Distance(fireStartPosition, tipPosition) >= maxRange)
        {
            tipPosition = fireStartPosition + fireDirection * maxRange;
            BeginHook(maxDistanceHookDuration);
        }
    }

    private void BeginHook(float duration)
    {
        if (duration <= 0f)
        {
            BeginRetract();
            return;
        }

        hookTimer = duration;
        state = State.Hooked;
    }

    private void TickHooked()
    {
        hookTimer -= Time.deltaTime;

        if (hookTimer <= 0f)
            BeginRetract();
    }

    private void TickRetracting()
    {
        Vector3 retractTarget = tongueOrigin.position;
        tipPosition = Vector3.MoveTowards(tipPosition, retractTarget, retractSpeed * Time.deltaTime);

        if (Vector3.Distance(tipPosition, retractTarget) < 0.01f)
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

        if (useCurvedTongueLine)
            UpdateCurvedLine();
        else
            UpdateStraightLine();
    }

    private void UpdateStraightLine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, GetLineStartPosition());
        lineRenderer.SetPosition(1, tipPosition);
    }

    private void UpdateCurvedLine()
    {
        int pointCount = GetLinePositionCount();
        lineRenderer.positionCount = pointCount;

        Vector3 start = GetLineStartPosition();
        Vector3 end = tipPosition;
        Vector3 control = GetCurveControlPoint(start, end);

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            lineRenderer.SetPosition(i, GetQuadraticBezierPoint(start, control, end, t));
        }
    }

    private Vector3 GetCurveControlPoint(Vector3 start, Vector3 end)
    {
        Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);

        if (!useScreenCenterFirePoint)
            return midpoint;

        Vector3 screenCenterMidpoint = Vector3.Lerp(fireStartPosition, end, 0.5f);

        return Vector3.Lerp(midpoint, screenCenterMidpoint, tongueCurveBend);
    }

    private Vector3 GetQuadraticBezierPoint(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start
             + 2f * oneMinusT * t * control
             + t * t * end;
    }

    public void SetTipPosition(Vector3 pos)
    {
        tipPosition = pos;
    }
}
