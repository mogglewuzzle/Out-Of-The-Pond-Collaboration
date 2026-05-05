using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OscillateCollider : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveAxis = Vector3.up;  // Axis to move along (X, Y, or Z)
    public float amplitude = 1f;           // How far it moves from start
    public float speed = 1f;               // How fast it oscillates

    private Vector3 startPosition;

    void Start()
    {
        // Remember the starting position
        startPosition = transform.position;
    }

    void Update()
    {
        // Compute offset along chosen axis
        Vector3 offset = moveAxis.normalized * Mathf.Sin(Time.time * speed) * amplitude;

        // Apply offset to starting position
        transform.position = startPosition + offset;
    }
}