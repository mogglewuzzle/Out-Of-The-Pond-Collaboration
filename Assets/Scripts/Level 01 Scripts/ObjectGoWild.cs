using UnityEngine;

public class PingPongMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right; // direction to move
    public float moveDistance = 5f;               // distance to travel back and forth
    public float moveSpeed = 2f;                  // speed of movement

    [Header("Rotation Settings")]
    public Vector3 rotateEuler = Vector3.zero;    // rotation per second in degrees

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Move back and forth along the given direction
        Vector3 offset = moveDirection.normalized * moveDistance;
        float pingPong = Mathf.PingPong(Time.time * moveSpeed, 1f); // 0 → 1 → 0
        transform.position = startPos + offset * pingPong;

        // Rotate continuously
        transform.Rotate(rotateEuler * Time.deltaTime, Space.Self);
    }
}