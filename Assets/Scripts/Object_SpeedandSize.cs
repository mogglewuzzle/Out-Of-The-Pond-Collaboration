using UnityEngine;

public class MoveAndGrow : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveAxis = Vector3.forward;  // Axis of movement
    public float baseSpeed = 5f;                // Starting speed
    public float speedIncrease = 1f;            // Linear acceleration per second
    public AnimationCurve speedCurve;           // Optional non-linear speed modifier
    public bool useLocalSpace = true;           // Move relative to local or world space

    [Header("Scaling Settings")]
    public Vector3 initialScale = Vector3.one;  // Starting scale
    public Vector3 sizeIncreasePerSecond = new Vector3(0.1f, 0.1f, 0.1f); // Growth rates

    private float currentSpeed;
    private Vector3 currentScale;
    private float elapsedTime = 0f;

    void Start()
    {
        currentSpeed = baseSpeed;
        currentScale = initialScale;
        transform.localScale = currentScale;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // --- Move ---
        currentSpeed += speedIncrease * Time.deltaTime;

        float curveMultiplier = (speedCurve != null && speedCurve.keys.Length > 0)
            ? speedCurve.Evaluate(elapsedTime)
            : 1f;

        float speedThisFrame = currentSpeed * curveMultiplier;

        Vector3 movement = moveAxis.normalized * speedThisFrame * Time.deltaTime;

        if (useLocalSpace)
            transform.Translate(movement, Space.Self);
        else
            transform.Translate(movement, Space.World);

        // --- Grow ---
        currentScale += sizeIncreasePerSecond * Time.deltaTime;
        transform.localScale = currentScale;
    }
}