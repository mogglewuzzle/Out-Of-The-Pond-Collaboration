using UnityEngine;
using System.Collections;

public class Player_TongueAttract : MonoBehaviour
{
    [Header("Attract Settings")]
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private float releaseDistance = 1.5f;

    [Header("Camera")]
    [SerializeField] private GameObject attractCamera;
    [SerializeField] private float attractCameraDelay = 0f;

    private Rigidbody rb;
    private PlayerTongueProjection projection;

    private bool active;
    private Vector3 latchPoint;
    private Coroutine cameraRoutine;

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
        cameraRoutine = StartCoroutine(EnableCameraAfterDelay());
    }

    public void StopAttract()
    {
        active = false;
        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
            cameraRoutine = null;
        }

        SetCameraActive(false);
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

    private IEnumerator EnableCameraAfterDelay()
    {
        if (attractCameraDelay > 0f)
            yield return new WaitForSeconds(attractCameraDelay);

        if (active)
            SetCameraActive(true);

        cameraRoutine = null;
    }

    private void SetCameraActive(bool isActive)
    {
        if (attractCamera != null)
            attractCamera.SetActive(isActive);
    }

    private void OnDisable()
    {
        StopAttract();
    }
}
