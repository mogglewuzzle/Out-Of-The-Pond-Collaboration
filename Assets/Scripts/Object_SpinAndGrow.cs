using UnityEngine;

public class ScaleAndRotate : MonoBehaviour
{
    [Header("Scaling Settings")]
    public float scaleAmount = 0.2f;
    public float scaleSpeed = 2f;

    private Vector3 originalScale;

    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0, 90, 0);

    private Vector3 currentRotation;

    void Start()
    {
        originalScale = transform.localScale;
        currentRotation = transform.eulerAngles;
    }

    void Update()
    {
        // SCALE
        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        transform.localScale = originalScale + Vector3.one * scaleOffset;

        // ROTATE (manually control each axis)
        currentRotation += rotationSpeed * Time.deltaTime;
        transform.eulerAngles = currentRotation;
    }
}