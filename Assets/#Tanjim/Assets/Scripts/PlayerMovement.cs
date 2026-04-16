using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool isSprinting;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        // Movement input
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Jump input
        inputActions.Player.Jump.performed += ctx => Jump();
    }

    void Update()
    {
        isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        rb.linearVelocity = new Vector3(
            moveInput.x * currentSpeed,
            currentVelocity.y,
            moveInput.y * currentSpeed
        );
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}