using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovementController movement;
    private PlayerInputHandler input;
    private PlayerAimController aim;
    private PlayerSprintController sprint;

    private static readonly int HashMoveSpeed   = Animator.StringToHash("MoveSpeed");
    private static readonly int HashIsGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int HashIsJumping   = Animator.StringToHash("IsJumping");
    private static readonly int HashIsAiming    = Animator.StringToHash("IsAiming");
    private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");

    private bool wasGroundedLastFrame;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        movement = GetComponent<PlayerMovementController>();
        input    = GetComponent<PlayerInputHandler>();
        aim      = GetComponent<PlayerAimController>();
        sprint   = GetComponent<PlayerSprintController>();
    }

    private void Update()
    {
        UpdateLocomotion();
        UpdateJump();
        UpdateAim();
        UpdateSprint();
    }

    private void UpdateLocomotion()
    {
        float normalizedSpeed = 0f;

        if (movement != null && sprint != null)
        {
            float current = movement.CurrentHorizontalSpeed;
            float max = sprint.SprintSpeed;

            normalizedSpeed = Mathf.Clamp01(current / max);
        }

        animator.SetFloat(HashMoveSpeed, normalizedSpeed);
        animator.SetBool(HashIsGrounded, movement.IsGroundedCached);
    }

    private void UpdateJump()
    {
        bool grounded = movement.IsGroundedCached;

        if (wasGroundedLastFrame && !grounded)
            animator.SetBool(HashIsJumping, true);

        if (!wasGroundedLastFrame && grounded)
            animator.SetBool(HashIsJumping, false);

        wasGroundedLastFrame = grounded;
    }

    private void UpdateAim()
    {
        animator.SetBool(HashIsAiming, aim != null && aim.IsAiming);
    }

    private void UpdateSprint()
    {
        animator.SetBool(HashIsSprinting, sprint != null && sprint.IsSprinting);
    }
}
