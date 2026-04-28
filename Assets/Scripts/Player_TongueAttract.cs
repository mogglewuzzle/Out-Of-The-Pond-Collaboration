using UnityEngine;
using System.Collections;

public class Player_TongueAttract : MonoBehaviour
{
    [Header("Attract Settings")]
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private float releaseDistance = 1.5f;

    private Rigidbody rb;
    private PlayerTongueProjection projection;

    private bool active;
    private Vector3 latchPoint;

    public bool IsAttracting => active;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projection = GetComponent<PlayerTongueProjection>();
    }

    public void BeginAttract(Vector3 point)
    {
        StopAttract();
        latchPoint = point;
        active = true;
    }

    public void StopAttract()
    {
        active = false;
    }

    private void Update()
    {
        if (!active)
            return;

        projection.SetTipPosition(latchPoint);

        Vector3 newPos = Vector3.MoveTowards(rb.position, latchPoint, pullSpeed * Time.deltaTime);
        rb.MovePosition(newPos);

        float dist = Vector3.Distance(rb.position, latchPoint);
        if (dist <= releaseDistance)
        {
            active = false;
            projection.BeginRetract();
        }
    }

    private void OnDisable()
    {
        StopAttract();
    }
}
