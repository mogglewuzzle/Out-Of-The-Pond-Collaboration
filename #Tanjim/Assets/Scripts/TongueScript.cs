using UnityEngine;

public class TongueScript : MonoBehaviour
{
    public float maxDistance = 7f;
    public float swingSpring = 4.5f;
    public float swingDamper = 7f;
    public float minDistanceFactor = 0.8f;
    public float maxDistanceFactor = 0.25f;

    private SpringJoint joint;
    private LineRenderer lineRenderer;
    private Rigidbody rb;
    private Transform currentSwingPoint;

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
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartSwing();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            StopSwing();
        }

        if (joint != null && currentSwingPoint != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentSwingPoint.position);
        }
    }

    void StartSwing()
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
}