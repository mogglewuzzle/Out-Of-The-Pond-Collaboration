using UnityEngine;
using UnityEngine.InputSystem;

public class TongueScript : MonoBehaviour
{
    public float maxDistance = 7f;
    public float swingSpring = 4.5f;
    public float swingDamper = 7f;
    public float minDistanceFactor = 0.8f;
    public float maxDistanceFactor = 0.25f;
    public float pullForce = 45f;

    private SpringJoint joint;
    private LineRenderer lineRenderer;
    private Rigidbody rb;
    private Transform currentSwingPoint;

    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        // Press/release interact for swinging
        inputActions.Player.Interact.performed += ctx => StartSwing();
        inputActions.Player.Interact.canceled += ctx => StopSwing();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
        }
    }

    void Update()
    {
        // Pull using F key
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            PullToPoint();
        }

        // Keep line connected while swinging
        if (joint != null && currentSwingPoint != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentSwingPoint.position);
        }
    }

    void StartSwing()
    {
        GameObject closestPoint = FindClosestSwingPoint();

        if (closestPoint != null)
        {
            currentSwingPoint = closestPoint.transform;

            if (joint == null)
            {
                joint = gameObject.AddComponent<SpringJoint>();
            }

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = currentSwingPoint.position;

            float distanceFromPoint = Vector3.Distance(transform.position, currentSwingPoint.position);

            joint.maxDistance = distanceFromPoint * maxDistanceFactor;
            joint.minDistance = distanceFromPoint * minDistanceFactor;

            joint.spring = swingSpring;
            joint.damper = swingDamper;
            joint.massScale = 4.5f;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, currentSwingPoint.position);
            }

            Debug.Log("Swing attached!");
        }
        else
        {
            Debug.Log("No swing point nearby");
        }
    }

    void StopSwing()
    {
        currentSwingPoint = null;

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        Debug.Log("Swing released!");
    }

    void PullToPoint()
    {
        GameObject closestPoint = FindClosestSwingPoint();

        if (closestPoint != null)
        {
            Vector3 direction = (closestPoint.transform.position - transform.position).normalized;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(direction * pullForce, ForceMode.VelocityChange);

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, closestPoint.transform.position);
                Invoke(nameof(HideTongueLine), 0.15f);
            }

            Debug.Log("Pull activated!");
        }
        else
        {
            Debug.Log("No swing point nearby for pull");
        }
    }

    GameObject FindClosestSwingPoint()
    {
        GameObject[] swingPoints = GameObject.FindGameObjectsWithTag("SwingPoint");

        GameObject closestPoint = null;
        float closestDistance = maxDistance;

        foreach (GameObject point in swingPoints)
        {
            float distance = Vector3.Distance(transform.position, point.transform.position);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    void HideTongueLine()
    {
        if (lineRenderer != null && currentSwingPoint == null)
        {
            lineRenderer.enabled = false;
        }
    }
}