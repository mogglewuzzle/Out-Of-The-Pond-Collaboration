using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AimHeld { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool FreeCameraHeld { get; private set; }
    public bool TongueThrowHeld { get; private set; }
    public bool PickUpPressed { get; private set; }
    public bool PickUpHeld { get; private set; }
    public bool ThrowObjectPressed { get; private set; }

    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled  += ctx => MoveInput = Vector2.zero;

        input.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled  += ctx => LookInput = Vector2.zero;

        input.Player.Jump.performed += ctx => JumpPressed = true;

        input.Player.Aim.performed += ctx => AimHeld = true;
        input.Player.Aim.canceled  += ctx => AimHeld = false;

        input.Player.Sprint.performed += ctx => SprintHeld = true;
        input.Player.Sprint.canceled  += ctx => SprintHeld = false;

        input.Player.FreeCam.performed += ctx => FreeCameraHeld = true;
        input.Player.FreeCam.canceled  += ctx => FreeCameraHeld = false;

        input.Player.TongueThrow.performed += ctx => TongueThrowHeld = true;
        input.Player.TongueThrow.canceled  += ctx => TongueThrowHeld = false;

        input.Player.PickUp.performed += ctx =>
        {
            PickUpPressed = true;
            PickUpHeld = true;
        };
        input.Player.PickUp.canceled += ctx => PickUpHeld = false;

        input.Player.ThrowObject.performed += ctx => ThrowObjectPressed = true;
    }

    private void LateUpdate()
    {
        JumpPressed = false;
        PickUpPressed = false;
        ThrowObjectPressed = false;
    }

    private void OnEnable()  => input.Player.Enable();
    private void OnDisable() => input.Player.Disable();
}
