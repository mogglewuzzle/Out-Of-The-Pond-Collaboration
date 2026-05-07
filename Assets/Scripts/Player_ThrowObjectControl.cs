using System.Collections.Generic;
using UnityEngine;

public class Player_ThrowObjectControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LineRenderer arcLineRenderer;
    [SerializeField] private Canvas canvasToDisableWhileHolding;

    [Header("Throw Aim")]
    [SerializeField] private Vector2 throwDirectionOffset = Vector2.zero;

    [Header("Throw Arc Preview")]
    [SerializeField] private bool showThrowArc = true;
    [SerializeField] private int arcMaxPointCount = 120;
    [SerializeField] private float arcMaxSimulationTime = 8f;
    [SerializeField] private float arcTimeStep = 0.08f;
    [SerializeField] private float arcCollisionRadius = 0.15f;
    [SerializeField] private LayerMask arcCollisionMask = ~0;
    [SerializeField] private float arcLineWidth = 0.04f;
    [SerializeField] private Color arcLineColor = Color.white;

    [Header("Charged Throw")]
    [SerializeField] private bool useChargedThrow = true;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float minThrowMultiplier = 1f;
    [SerializeField] private float maxThrowMultiplier = 2f;
    [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private PlayerInputHandler input;
    private Player_PickupControl pickupControl;
    private Collider[] heldObjectColliders = new Collider[0];
    private readonly List<Vector3> arcPoints = new List<Vector3>();
    private bool isChargingThrow;
    private bool canvasDefaultEnabled;
    private float throwChargeTimer;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        pickupControl = GetComponent<Player_PickupControl>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        EnsureArcLineRenderer();
        HideArc();

        if (canvasToDisableWhileHolding != null)
            canvasDefaultEnabled = canvasToDisableWhileHolding.enabled;
    }

    private void Update()
    {
        UpdateHeldObjectCanvas();
        UpdateThrowCharge();
        UpdateThrowArc();
    }

    private void UpdateHeldObjectCanvas()
    {
        if (canvasToDisableWhileHolding == null || pickupControl == null)
            return;

        canvasToDisableWhileHolding.enabled = pickupControl.HeldObject == null && canvasDefaultEnabled;
    }

    private void UpdateThrowCharge()
    {
        if (input == null || pickupControl == null)
        {
            ResetThrowCharge();
            return;
        }

        if (pickupControl.HeldObject == null)
        {
            ResetThrowCharge();
            return;
        }

        if (!useChargedThrow)
        {
            if (input.ThrowObjectPressed)
                ThrowHeldObject();

            return;
        }

        if (input.ThrowObjectPressed && !isChargingThrow)
        {
            isChargingThrow = true;
            throwChargeTimer = 0f;
        }

        if (isChargingThrow && input.ThrowObjectHeld)
            throwChargeTimer = Mathf.Min(throwChargeTimer + Time.deltaTime, maxChargeTime);

        if (isChargingThrow && input.ThrowObjectReleased)
            ThrowHeldObject();
    }

    private void ThrowHeldObject()
    {
        Object_Pickupable heldObject = pickupControl.HeldObject;
        if (heldObject == null)
            return;

        Vector3 throwVelocity = GetThrowVelocity(heldObject, GetCurrentThrowMultiplier());
        pickupControl.TryThrowHeldObject(throwVelocity);
        ResetThrowCharge();
    }

    private void UpdateThrowArc()
    {
        if (!showThrowArc ||
            arcLineRenderer == null ||
            pickupControl == null ||
            pickupControl.HeldObject == null ||
            !ShouldShowThrowArc())
        {
            HideArc();
            return;
        }

        Object_Pickupable heldObject = pickupControl.HeldObject;
        Rigidbody heldRigidbody = heldObject.Rigidbody;
        Vector3 position = heldRigidbody.worldCenterOfMass;
        Vector3 velocity = GetThrowVelocity(heldObject, GetCurrentThrowMultiplier());
        Vector3 gravity = heldObject.UsesGravityWhenThrown ? Physics.gravity * heldObject.ThrownGravityMultiplier : Vector3.zero;
        float linearDamping = heldRigidbody.linearDamping;

        heldObjectColliders = heldObject.GetComponentsInChildren<Collider>();

        arcPoints.Clear();
        arcPoints.Add(position);

        float safeTimeStep = Mathf.Max(0.01f, arcTimeStep);
        int maxStepsByTime = Mathf.CeilToInt(arcMaxSimulationTime / safeTimeStep);
        int maxSteps = Mathf.Min(arcMaxPointCount - 1, maxStepsByTime);
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 nextVelocity = velocity + gravity * safeTimeStep;
            nextVelocity *= GetDampingMultiplier(linearDamping, safeTimeStep);

            Vector3 nextPosition = position + ((velocity + nextVelocity) * 0.5f * safeTimeStep);

            if (TryGetTrajectoryHit(position, nextPosition, out RaycastHit hit))
            {
                arcPoints.Add(hit.point);
                break;
            }

            arcPoints.Add(nextPosition);

            velocity = nextVelocity;
            position = nextPosition;
        }

        arcLineRenderer.enabled = true;
        arcLineRenderer.positionCount = arcPoints.Count;
        for (int i = 0; i < arcPoints.Count; i++)
            arcLineRenderer.SetPosition(i, arcPoints[i]);
    }

    private Vector3 GetThrowVelocity(Object_Pickupable heldObject, float throwMultiplier)
    {
        Transform aimTransform = playerCamera != null ? playerCamera.transform : transform;
        Vector3 offsetDirection = aimTransform.forward +
                                  aimTransform.right * throwDirectionOffset.x +
                                  aimTransform.up * throwDirectionOffset.y;
        Vector3 throwDirection = (offsetDirection + Vector3.up * heldObject.UpwardThrowBoost).normalized;
        return throwDirection * heldObject.ThrowForce * throwMultiplier;
    }

    private float GetCurrentThrowMultiplier()
    {
        if (!useChargedThrow)
            return 1f;

        float chargePercent = maxChargeTime > 0f ? Mathf.Clamp01(throwChargeTimer / maxChargeTime) : 1f;
        float curveValue = chargeCurve != null ? chargeCurve.Evaluate(chargePercent) : chargePercent;
        return Mathf.Lerp(minThrowMultiplier, maxThrowMultiplier, Mathf.Clamp01(curveValue));
    }

    private void ResetThrowCharge()
    {
        isChargingThrow = false;
        throwChargeTimer = 0f;
    }

    private bool ShouldShowThrowArc()
    {
        return useChargedThrow ? isChargingThrow && input != null && input.ThrowObjectHeld : input != null && input.ThrowObjectPressed;
    }

    private bool TryGetTrajectoryHit(Vector3 start, Vector3 end, out RaycastHit closestHit)
    {
        Vector3 segment = end - start;
        float distance = segment.magnitude;
        closestHit = default;

        if (distance <= Mathf.Epsilon)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(start, arcCollisionRadius, segment / distance, distance, arcCollisionMask, QueryTriggerInteraction.Ignore);
        bool foundHit = false;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsHeldObjectCollider(hits[i].collider) || hits[i].distance >= closestDistance)
                continue;

            closestHit = hits[i];
            closestDistance = hits[i].distance;
            foundHit = true;
        }

        return foundHit;
    }

    private float GetDampingMultiplier(float damping, float timeStep)
    {
        if (damping <= 0f)
            return 1f;

        return 1f / (1f + damping * timeStep);
    }

    private bool IsHeldObjectCollider(Collider candidate)
    {
        for (int i = 0; i < heldObjectColliders.Length; i++)
        {
            if (heldObjectColliders[i] == candidate)
                return true;
        }

        return false;
    }

    private void EnsureArcLineRenderer()
    {
        if (arcLineRenderer == null)
        {
            GameObject arcObject = new GameObject("Throw Arc Preview");
            arcObject.transform.SetParent(transform, false);
            arcLineRenderer = arcObject.AddComponent<LineRenderer>();
        }

        arcLineRenderer.useWorldSpace = true;
        arcLineRenderer.widthMultiplier = arcLineWidth;
        arcLineRenderer.startColor = arcLineColor;
        arcLineRenderer.endColor = arcLineColor;

        if (arcLineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                arcLineRenderer.sharedMaterial = new Material(shader);
        }
    }

    private void OnValidate()
    {
        arcMaxPointCount = Mathf.Max(2, arcMaxPointCount);
        arcTimeStep = Mathf.Max(0.01f, arcTimeStep);
        arcMaxSimulationTime = Mathf.Max(arcTimeStep, arcMaxSimulationTime);
        arcCollisionRadius = Mathf.Max(0f, arcCollisionRadius);
        arcLineWidth = Mathf.Max(0.001f, arcLineWidth);
        maxChargeTime = Mathf.Max(0f, maxChargeTime);
        minThrowMultiplier = Mathf.Max(0f, minThrowMultiplier);
        maxThrowMultiplier = Mathf.Max(minThrowMultiplier, maxThrowMultiplier);

        if (arcLineRenderer == null)
            return;

        arcLineRenderer.widthMultiplier = arcLineWidth;
        arcLineRenderer.startColor = arcLineColor;
        arcLineRenderer.endColor = arcLineColor;
    }

    private void OnDisable()
    {
        HideArc();
        ResetThrowCharge();

        if (canvasToDisableWhileHolding != null)
            canvasToDisableWhileHolding.enabled = canvasDefaultEnabled;
    }

    private void HideArc()
    {
        if (arcLineRenderer == null)
            return;

        arcLineRenderer.enabled = false;
        arcLineRenderer.positionCount = 0;
    }
}
