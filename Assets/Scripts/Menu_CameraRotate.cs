using UnityEngine;

public class CameraOrbitSmooth : MonoBehaviour
{
    public Transform target;
    public float orbitSpeed = 20f;
    public float distance = 5f;
    public float height = 2f;
    public float smoothTime = 0.3f;

    private float currentAngle = 0f;
    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        currentAngle += orbitSpeed * Time.deltaTime;

        float x = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * distance;
        float z = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * distance;

        Vector3 targetPos = target.position + new Vector3(x, height, z);

        // Smooth damp for cinematic movement
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        transform.LookAt(target);
    }
}