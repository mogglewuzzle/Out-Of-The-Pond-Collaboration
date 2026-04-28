using UnityEngine;
using System.Collections;

public class Player_TongueGrab : MonoBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private float releaseDistance = 1.5f;

    private Rigidbody playerRb;
    private PlayerTongueProjection projection;

    private bool active;
    private Rigidbody grabbedBody;

    public bool IsGrabbing => active;

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        projection = GetComponent<PlayerTongueProjection>();
    }

    public void BeginGrab(Rigidbody targetBody)
    {
        if (targetBody == null)
            return;

        StopGrab();
        grabbedBody = targetBody;
        active = true;
    }

    public void StopGrab()
    {
        active = false;
        grabbedBody = null;
    }

    private void Update()
    {
        if (!active)
            return;

        if (grabbedBody == null)
        {
            projection.BeginRetract();
            return;
        }

        Vector3 grabPoint = grabbedBody.worldCenterOfMass;
        projection.SetTipPosition(grabPoint);

        Vector3 targetPos = Vector3.MoveTowards(grabbedBody.position, playerRb.position, pullSpeed * Time.deltaTime);
        grabbedBody.MovePosition(targetPos);

        float dist = Vector3.Distance(grabbedBody.position, playerRb.position);
        if (dist <= releaseDistance)
        {
            active = false;
            projection.BeginRetract();
        }
    }

    private void OnDisable()
    {
        StopGrab();
    }
}
