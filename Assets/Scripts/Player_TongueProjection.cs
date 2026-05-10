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

    [Header("Tongue Bullet Trigger")]
    [Tooltip("Optional invisible trigger prefab that moves with the tongue tip. Use this for trigger-based reactions such as above-character dialogue.")]
    [SerializeField] private GameObject tongueBulletPrefab;
    [Tooltip("If enabled, the spawned tongue bullet is destroyed when the tongue starts retracting. If disabled, it remains until the tongue finishes retracting.")]
    [SerializeField] private bool destroyTongueBulletOnRetract = true;

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
    [SerializeField] private string[] GrabPointTags = { "GrabPoint" };
    [SerializeField] private string SwingPointTag = "SwingPoint";

    [Header("Auto Aim")]
    [SerializeField] private bool useAutoAim = true;
    [Tooltip("World-space thickness of the search tube around the screen-center ray. Larger values catch targets farther beside the ray, but the same value appears smaller on screen at long distances.")]
    [SerializeField] private float autoAimRadius = 0.75f;
    [Tooltip("Cone limit from the crosshair direction. This keeps the world-space radius from grabbing targets that feel too far from the crosshair on screen.")]
    [SerializeField] private float autoAimMaxAngle = 7f;
    [Tooltip("Only these tags can receive auto aim. Tags listed earlier win first when multiple valid targets are close together, then the closest-to-crosshair target wins.")]
    [SerializeField] private string[] autoAimTagPriority = { "AttractPoint", "GrabPoint", "SwingPoint" };
    [Tooltip("When enabled, look speed is reduced while a valid auto aim target is near the crosshair.")]
    [SerializeField] private bool useAimSlowdown = true;
    [Tooltip("Look speed multiplier while aim slowdown is active. Lower values feel stickier near tongue targets.")]
    [SerializeField, Range(0.1f, 1f)] private float aimSlowdownMultiplier = 0.5f;

    private Vector3 tipPosition;
    private Vector3 fireDirection;
    private Vector3 fireStartPosition;
    private float hookTimer;
    private float currentAimSlowdownMultiplier = 1f;
    private GameObject activeTongueBullet;

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

    public float AimSlowdownMultiplier => currentAimSlowdownMultiplier;

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

    private void OnDisable()
    {
        DestroyTongueBullet();
    }

    private void Update()
    {
        UpdateScreenCenterFirePoint();
        UpdateAimSlowdown();

        bool held = input != null && input.TongueThrowHeld;

        if (held && !wasHeld && state == State.Idle)
            FireTongue();

        if (!held && wasHeld)
            BeginRetract();

        wasHeld = held;

        TickState();
        UpdateTongueBulletPosition();
        UpdateLine();
    }

    private void FireTongue()
    {
        /* 
        * always make tounge go towards center of screen
        * when testing, just using tongueOrigin.forward as direction makes it very hard to aim
        */

        Ray screenCenterRay = GetScreenCenterRay();

        if (useScreenCenterFirePoint)
        {
            fireStartPosition = GetFireStartPosition(screenCenterRay);
        }
        else
        {
            // not useScreenCenterFirePoint prefered, as the tounge doesn't start from behind the frog and in the actual mouth
            fireStartPosition = tongueOrigin.position;
        }

        fireDirection = GetFireDirection(screenCenterRay);

        tipPosition = fireStartPosition;
        state = State.Extending;
        SpawnTongueBullet();

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

    private Vector3 GetFireDirection(Ray screenCenterRay)
    {
        if (useAutoAim && TryGetAutoAimPoint(screenCenterRay, out Vector3 autoAimPoint))
        {
            Vector3 assistedDirection = (autoAimPoint - fireStartPosition).normalized;
            if (assistedDirection != Vector3.zero)
                return assistedDirection;
        }

        return screenCenterRay.direction;
    }

    private bool TryGetAutoAimPoint(Ray screenCenterRay, out Vector3 autoAimPoint)
    {
        autoAimPoint = Vector3.zero;

        if (autoAimRadius <= 0f || autoAimMaxAngle <= 0f)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            screenCenterRay,
            autoAimRadius,
            maxRange,
            collidableLayers & ~tongueLayerMask);

        int bestPriority = int.MaxValue;
        float bestAngle = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;
        bool foundTarget = false;

        foreach (RaycastHit hit in hits)
        {
            int priority = GetAutoAimTagPriority(hit.collider);
            if (priority < 0)
                continue;

            Vector3 toHit = hit.point - screenCenterRay.origin;
            if (toHit == Vector3.zero)
                continue;

            float angle = Vector3.Angle(screenCenterRay.direction, toHit);
            if (angle > autoAimMaxAngle)
                continue;

            bool isBetter =
                priority < bestPriority ||
                (priority == bestPriority && angle < bestAngle) ||
                (priority == bestPriority && Mathf.Approximately(angle, bestAngle) && hit.distance < bestDistance);

            if (!isBetter)
                continue;

            autoAimPoint = hit.point;
            bestPriority = priority;
            bestAngle = angle;
            bestDistance = hit.distance;
            foundTarget = true;
        }

        return foundTarget;
    }

    private void UpdateAimSlowdown()
    {
        currentAimSlowdownMultiplier = 1f;

        if (!useAimSlowdown)
            return;

        Ray screenCenterRay = GetScreenCenterRay();
        Vector3 autoAimPoint;
        if (TryGetAutoAimPoint(screenCenterRay, out autoAimPoint))
            currentAimSlowdownMultiplier = aimSlowdownMultiplier;
    }

    private int GetAutoAimTagPriority(Collider hitCollider)
    {
        if (hitCollider == null)
            return -1;

        if (autoAimTagPriority != null)
        {
            for (int i = 0; i < autoAimTagPriority.Length; i++)
            {
                if (!string.IsNullOrEmpty(autoAimTagPriority[i]) && hitCollider.tag == autoAimTagPriority[i])
                    return i;
            }
        }

        return -1;
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
            else if (HasAnyGrabPointTag(hit.collider))
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
            DestroyTongueBullet();
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }
    }

    public void BeginRetract()
    {
        state = State.Retracting;
        if (destroyTongueBulletOnRetract)
            DestroyTongueBullet();

        attractModule.StopAttract();
        if (grabModule != null)
            grabModule.StopGrab();

        if (swingModule != null)
            swingModule.ReleaseSwing();
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

    private bool HasAnyGrabPointTag(Collider hitCollider)
    {
        if (hitCollider == null || GrabPointTags == null || GrabPointTags.Length == 0)
            return false;

        for (int i = 0; i < GrabPointTags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(GrabPointTags[i]) && hitCollider.CompareTag(GrabPointTags[i].Trim()))
                return true;
        }

        return false;
    }

    public void SetTipPosition(Vector3 pos)
    {
        tipPosition = pos;
        UpdateTongueBulletPosition();
    }

    private void SpawnTongueBullet()
    {
        DestroyTongueBullet();

        if (tongueBulletPrefab == null)
            return;

        Quaternion rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : Quaternion.identity;

        activeTongueBullet = Instantiate(tongueBulletPrefab, tipPosition, rotation);
        EnsureTongueBulletPhysics(activeTongueBullet);
    }

    private void UpdateTongueBulletPosition()
    {
        if (activeTongueBullet == null)
            return;

        activeTongueBullet.transform.position = tipPosition;

        if (fireDirection.sqrMagnitude > 0.0001f)
            activeTongueBullet.transform.rotation = Quaternion.LookRotation(fireDirection.normalized, Vector3.up);
    }

    private void DestroyTongueBullet()
    {
        if (activeTongueBullet == null)
            return;

        Destroy(activeTongueBullet);
        activeTongueBullet = null;
    }

    private void EnsureTongueBulletPhysics(GameObject bulletObject)
    {
        Collider[] bulletColliders = bulletObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < bulletColliders.Length; i++)
            bulletColliders[i].isTrigger = true;

        Rigidbody bulletRigidbody = bulletObject.GetComponent<Rigidbody>();
        if (bulletRigidbody == null)
            bulletRigidbody = bulletObject.AddComponent<Rigidbody>();

        bulletRigidbody.isKinematic = true;
        bulletRigidbody.useGravity = false;
    }
}
