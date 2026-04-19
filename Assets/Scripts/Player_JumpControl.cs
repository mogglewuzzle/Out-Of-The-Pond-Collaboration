using UnityEngine;

public class PlayerJumpController : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    private PlayerInputHandler input;
    private PlayerMovementController movement;

    private float lastGroundedTime;
    private float lastJumpPressedTime;

    public bool ShouldJump { get; private set; }
    public float JumpForce => jumpForce;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovementController>();
    }

    private void Update()
    {
        // Input should always be read in Update
        if (input.JumpPressed)
            lastJumpPressedTime = Time.time;
    }

    private void FixedUpdate()
    {
        // Grounded state should always be read in FixedUpdate
        if (movement.IsGroundedCached)
            lastGroundedTime = Time.time;

        // Evaluate jump conditions in FixedUpdate (physics loop)
        ShouldJump =
            (Time.time - lastJumpPressedTime <= jumpBufferTime) &&
            (Time.time - lastGroundedTime <= coyoteTime);
    }

    public void ConsumeJump()
    {
        ShouldJump = false;
        lastJumpPressedTime = -999f;
    }
}
