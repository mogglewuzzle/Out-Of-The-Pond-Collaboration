using UnityEngine;

public class PlayerRotationController : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField, Range(0f, 0.5f)] private float stickDeadzone = 0.15f;
    [SerializeField, Range(1f, 3f)] private float stickCurve = 2f;
    [SerializeField] private float aimRotationMultiplier = 0.5f;

    private PlayerInputHandler input;
    private PlayerAimController aim;
    private PlayerFreeCameraController freeCamera; // NEW
    private Rigidbody rb;
    private float currentYaw;

    private void Awake()
    {
        input      = GetComponent<PlayerInputHandler>();
        aim        = GetComponent<PlayerAimController>();
        freeCamera = GetComponent<PlayerFreeCameraController>(); // NEW
        rb         = GetComponent<Rigidbody>();

        currentYaw = transform.eulerAngles.y;
    }

    private void FixedUpdate()
    {
        // Don't rotate the player while orbiting in free cam
        if (freeCamera != null && freeCamera.IsActive) return; // NEW

        float rawX = Mathf.Abs(input.LookInput.x);
        if (rawX < stickDeadzone) return;

        float remapped = Mathf.Clamp01((rawX - stickDeadzone) / (1f - stickDeadzone));
        float curved   = Mathf.Sign(input.LookInput.x) * Mathf.Pow(remapped, stickCurve);

        float speed = rotationSpeed * (aim.IsAiming ? aimRotationMultiplier : 1f);

        currentYaw += curved * speed * Time.fixedDeltaTime;

        rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
    }
}