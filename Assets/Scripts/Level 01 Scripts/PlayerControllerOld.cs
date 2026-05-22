using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController cc;
    private Vector2 moveInput;

    // Movement speed (units per second)
    public float speed = 5f;

    // Rotation speed (degrees per second)
    public float turnSpeed = 360f;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    // Called automatically by PlayerInput component
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        // Separate input into forward/back and left/right
        float v = moveInput.y;
        float h = moveInput.x;

        // Rotate the player around Y axis
        transform.Rotate(0, h * turnSpeed * Time.deltaTime, 0);

        // Move the player forward/back in the direction it's facing
        cc.SimpleMove(transform.forward * v * speed);
    }
}