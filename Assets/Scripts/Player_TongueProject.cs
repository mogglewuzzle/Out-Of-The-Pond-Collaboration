using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTongueProject : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Tag")]
    [Tooltip("Objects with this tag can be hooked in both modes.")]
    [SerializeField] private string swingTag = "SwingPoint";

    [Header("Free-Swing Spring (not aiming)")]
    [Tooltip("Maximum world-space distance a SwingPoint can be to count as a candidate.")]
    [SerializeField] private float maxDistance = 7f;
    [SerializeField] private float freeSpring = 4.5f;
    [SerializeField] private float freeDamper = 7f;
    [Tooltip("How far in (0–1 × hookDist) the joint lets you travel before pulling back.")]
    [SerializeField, Range(0f, 1f)] private float freeMinDistFactor = 0f;
    [Tooltip("Rope length as a fraction of the distance at the moment you hooked.")]
    [SerializeField, Range(0f, 1f)] private float freeMaxDistFactor = 0.8f;

    [Header("Aimed Grapple Spring")]
    [Tooltip("How far the aimed raycast reaches.")]
    [SerializeField] private float aimRaycastDistance = 50f;
    [SerializeField] private float aimSpring = 10f;
    [SerializeField] private float aimDamper = 5f;
    [SerializeField, Range(0f, 1f)] private float aimMinDistFactor = 0f;
    [Tooltip("Near zero = pulls you almost all the way to the hook point.")]
    [SerializeField, Range(0f, 1f)] private float aimMaxDistFactor = 0.1f;
    [SerializeField] private float aimMassScale = 4.5f;

    [Header("Target Glow")]
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float glowIntensity = 2f;

    [Header("UI")]
    [Tooltip("A small dot/reticle shown in free-look mode so the player can see what they're aiming at. " +
             "Should be centred on your Canvas.")]
    [SerializeField] private GameObject freeLookCrosshair;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    // ─────────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────────

    private Rigidbody         rb;
    private LineRenderer      lineRenderer;
    private Camera            mainCam;
    private PlayerThirdPersonController playerCtrl;

    private SpringJoint       joint;
    private Vector3           hookedPoint;

    private List<GameObject>  swingPoints     = new();
    private GameObject        highlightedTarget;

    private InputSystem_Actions inputActions;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Interact.performed += _ => TryStartSwing();
        inputActions.Player.Interact.canceled  += _ => StopSwing();
    }

    void OnEnable()  => inputActions.Player.Enable();
    void OnDisable() => inputActions.Player.Disable();

    void Start()
    {
        rb          = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();
        mainCam     = Camera.main;
        playerCtrl  = GetComponent<PlayerThirdPersonController>();

        if (lineRenderer != null)
        {
            lineRenderer.enabled       = false;
            lineRenderer.positionCount = 2;
        }

        RefreshSwingPoints();
    }

    void Update()
    {
        UpdateLineRenderer();

        bool aiming    = IsAiming();
        bool hooked    = joint != null;

        // Free-look crosshair: visible only when not aiming and not currently hooked
        if (freeLookCrosshair != null)
            freeLookCrosshair.SetActive(!aiming && !hooked);

        // Highlight the best candidate while idle and not aiming
        if (!aiming && !hooked)
            UpdateHighlight();
        else
            ClearHighlight();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this whenever SwingPoint objects are spawned or destroyed so the
    /// cached list stays accurate.
    /// </summary>
    public void RefreshSwingPoints()
    {
        swingPoints.Clear();
        swingPoints.AddRange(GameObject.FindGameObjectsWithTag(swingTag));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core swing logic
    // ─────────────────────────────────────────────────────────────────────────

    void TryStartSwing()
    {
        // The player must leave the ground first (jump) before hooking
        if (IsGrounded())
        {
            Debug.Log("Tongue: must jump first!");
            return;
        }

        if (IsAiming())
            StartAimedGrapple();
        else
            StartFreeSwing();
    }

    /// <summary>
    /// Aimed mode: fire a raycast from the camera centre; hook at the exact
    /// surface point where it hits a tagged object.
    /// </summary>
    void StartAimedGrapple()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, aimRaycastDistance))
        {
            Debug.Log("Grapple: nothing in range.");
            return;
        }

        if (!hit.collider.CompareTag(swingTag))
        {
            Debug.Log($"Grapple: hit '{hit.collider.name}' but it isn't tagged '{swingTag}'.");
            return;
        }

        // hook.point is the exact surface contact — not the object's pivot
        AttachJoint(hit.point, aimSpring, aimDamper, aimMinDistFactor, aimMaxDistFactor, aimMassScale);
        Debug.Log($"Grapple: hooked onto {hit.collider.name} at {hit.point}");
    }

    /// <summary>
    /// Free mode: hook onto whichever tagged object is currently highlighted
    /// (closest to the screen-centre reticle within maxDistance).
    /// </summary>
    void StartFreeSwing()
    {
        if (highlightedTarget == null)
        {
            Debug.Log("Tongue: no target in range.");
            return;
        }

        AttachJoint(
            highlightedTarget.transform.position,
            freeSpring, freeDamper,
            freeMinDistFactor, freeMaxDistFactor,
            massScale: 4.5f);

        Debug.Log($"Tongue: swinging to {highlightedTarget.name}");
    }

    void AttachJoint(
        Vector3 worldPoint,
        float   spring,
        float   damper,
        float   minFactor,
        float   maxFactor,
        float   massScale)
    {
        hookedPoint = worldPoint;

        if (joint == null)
            joint = gameObject.AddComponent<SpringJoint>();

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = worldPoint;

        float dist           = Vector3.Distance(transform.position, worldPoint);
        joint.minDistance    = dist * minFactor;
        joint.maxDistance    = dist * maxFactor;
        joint.spring         = spring;
        joint.damper         = damper;
        joint.massScale      = massScale;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, worldPoint);
        }
    }

    void StopSwing()
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        Debug.Log("Tongue: released.");
    }

    void UpdateLineRenderer()
    {
        if (joint == null || lineRenderer == null) return;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, hookedPoint);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Target highlighting (free mode only)
    // ─────────────────────────────────────────────────────────────────────────

    void UpdateHighlight()
    {
        Vector2    screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        GameObject best         = null;
        float      bestDist     = float.MaxValue;

        foreach (GameObject point in swingPoints)
        {
            if (point == null) continue;

            // Cull by world-space distance first (cheap)
            float worldDist = Vector3.Distance(transform.position, point.transform.position);
            if (worldDist > maxDistance) continue;

            // Then rank by screen-space proximity to the reticle
            Vector3 screenPos = mainCam.WorldToScreenPoint(point.transform.position);
            if (screenPos.z < 0f) continue; // behind camera

            float screenDist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
            if (screenDist < bestDist)
            {
                bestDist = screenDist;
                best     = point;
            }
        }

        if (best == highlightedTarget) return; // no change

        ClearHighlight();
        highlightedTarget = best;
        if (highlightedTarget != null)
            SetGlow(highlightedTarget, true);
    }

    void ClearHighlight()
    {
        if (highlightedTarget == null) return;
        SetGlow(highlightedTarget, false);
        highlightedTarget = null;
    }

    /// <summary>
    /// Uses a MaterialPropertyBlock so we never modify the shared material.
    /// Requires "Emission" to be enabled on the object's material.
    /// Works with URP (Lit), HDRP (Lit), and Built-in (Standard).
    /// </summary>
    void SetGlow(GameObject obj, bool on)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", on ? glowColor * glowIntensity : Color.black);
        r.SetPropertyBlock(mpb);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    bool IsAiming()   => playerCtrl != null && playerCtrl.IsAiming;
    bool IsGrounded() => Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
}