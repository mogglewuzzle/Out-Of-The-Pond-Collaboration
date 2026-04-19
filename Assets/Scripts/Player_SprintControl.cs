using UnityEngine;

public class PlayerSprintController : MonoBehaviour
{
    [Header("Sprint Settings")]
    [SerializeField] private bool holdToSprint = true;
    [SerializeField] private float minMoveThreshold = 0.1f;
    [SerializeField] private float sprintSpeed = 8f;   // MOVED HERE

    private PlayerInputHandler input;
    private PlayerMovementController movement;

    public bool IsSprinting { get; private set; }
    public float SprintSpeed => sprintSpeed;           // expose to movement

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovementController>();
    }

    private void Update()
    {
        bool hasMoveInput = input.MoveInput.sqrMagnitude > minMoveThreshold;

        if (holdToSprint)
        {
            IsSprinting = input.SprintHeld && hasMoveInput;
        }
        else
        {
            if (input.SprintHeld && hasMoveInput)
                IsSprinting = !IsSprinting;
        }

        if (!hasMoveInput)
            IsSprinting = false;
    }
}
