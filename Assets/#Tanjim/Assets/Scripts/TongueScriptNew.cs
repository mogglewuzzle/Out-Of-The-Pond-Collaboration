using UnityEngine;
using UnityEngine.InputSystem;

public class TongueScript : MonoBehaviour
{
    public float maxDistance = 7f;
    public float SwingSpring = 4.5f;
    public float SwingDamper = 7f;
    public float minDistanceFactor = 0.8f;
    public float maxDistanceFactor = 0.25f;

    private SpringJoint joint;
    private LineRenderer lineRenderer;
    private Rigidbody rb;
    private Transform currentSwingPoint;

    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        // Press E → start Swing
        inputActions.Player.Attack.performed += ctx => StartSwing();

        // Release E → stop Swing
        inputActions.Player.Attack.canceled += ctx => StopSwing();
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
        if (joint != null && currentSwingPoint != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentSwingPoint.position);
        }
    }

    void StartSwing()
    {
        GameObject[] SwingPoints = GameObject.FindGameObjectsWithTag("SwingPoint");

        GameObject closestPoint = null;
        float closestDistance = maxDistance;

        foreach (GameObject point in SwingPoints)
        {
            float distance = Vector3.Distance(transform.position, point.transform.position);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

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

            joint.spring = SwingSpring;
            joint.damper = SwingDamper;
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
            Debug.Log("No Swing point nearby");
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
}