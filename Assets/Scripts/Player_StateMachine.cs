using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; }

    private PlayerMovementController movement;
    private PlayerInputHandler input;
    private PlayerAimController aim;

    private void Awake()
    {
        movement = GetComponent<PlayerMovementController>();
        input    = GetComponent<PlayerInputHandler>();
        aim      = GetComponent<PlayerAimController>();

        CurrentState = PlayerState.Idle;
    }

    private void Update()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        bool isGrounded = movement != null && movement.IsGroundedCached;
        bool hasMoveInput = input != null && input.MoveInput.sqrMagnitude > 0.01f;
        bool isAiming = aim != null && aim.IsAiming;

        if (!isGrounded)
        {
            // Simple split: if vertical velocity > 0 → Jump, else Fall
            float verticalVel = movement != null ? movement.GetComponent<Rigidbody>().linearVelocity.y : 0f;
            CurrentState = verticalVel > 0.1f ? PlayerState.Jump : PlayerState.Fall;
            return;
        }

        if (isAiming)
        {
            CurrentState = PlayerState.Aim;
            return;
        }

        if (hasMoveInput)
        {
            CurrentState = PlayerState.Move;
            return;
        }

        CurrentState = PlayerState.Idle;
    }
}
