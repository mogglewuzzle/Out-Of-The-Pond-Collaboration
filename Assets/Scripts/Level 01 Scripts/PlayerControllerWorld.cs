using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerControllerWorld : MonoBehaviour
{
    public enum ControlMode
    {
        TopDown,
        ThirdPerson
    }

    [Header("Mode")]
    public ControlMode mode = ControlMode.TopDown;

    private CharacterController cc;
    private Animator anim;
    private Vector2 moveInput;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSpeed = 10f;

    [Header("Camera")]
    public Transform cameraTransform;

    private bool isRunning = false;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        if (!cameraTransform && Camera.main)
            cameraTransform = Camera.main.transform;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnSprint(InputValue value)
    {
        isRunning = value.isPressed;
    }

    void OnDance(InputValue value)
    {
        if (value.isPressed)
            anim.SetTrigger("Dance");
    }

    void OnLean(InputValue value)
    {
        if (value.isPressed)
            anim.SetTrigger("Lean");
    }

    void Update()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;

        if (move.magnitude > 1f)
            move.Normalize();

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        cc.SimpleMove(move * currentSpeed);

        if (mode == ControlMode.TopDown)
        {
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
        else if (mode == ControlMode.ThirdPerson)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        float animSpeed = move.magnitude * (isRunning ? 1.5f : 1f);
        anim.SetFloat("Speed", animSpeed);
    }
}