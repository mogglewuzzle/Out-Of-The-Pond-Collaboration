using UnityEngine;

public class Player_BoneRotationMover : MonoBehaviour
{
    private enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Bone")]
    [SerializeField] private Transform bone;
    [SerializeField] private RotationAxis yawAxis = RotationAxis.Y;
    [SerializeField] private RotationAxis pitchAxis = RotationAxis.X;
    [SerializeField] private bool invertYaw;
    [SerializeField] private bool invertPitch;

    [Header("Yaw Rotation")]
    [SerializeField, Range(0f, 90f)] private float maxYawRotation = 25f;

    [Header("Pitch Rotation")]
    [SerializeField, Range(0f, 90f)] private float maxPitchRotation = 20f;

    [Header("Smoothing")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float returnSpeed = 120f;

    [Header("Input")]
    [SerializeField, Range(0f, 0.5f)] private float stickDeadzone = 0.15f;
    [SerializeField, Range(1f, 3f)] private float stickCurve = 2f;

    private PlayerInputHandler input;
    private Quaternion originalLocalRotation;
    private float currentYawRotation;
    private float currentPitchRotation;
    private bool hasOriginalRotation;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        CacheOriginalRotation();
    }

    private void OnEnable()
    {
        CacheOriginalRotation();
    }

    private void LateUpdate()
    {
        if (bone == null)
            return;

        if (!hasOriginalRotation)
            CacheOriginalRotation();

        float targetYawRotation = GetTargetYawRotation();
        float targetPitchRotation = GetTargetPitchRotation();

        currentYawRotation = MoveRotation(currentYawRotation, targetYawRotation);
        currentPitchRotation = MoveRotation(currentPitchRotation, targetPitchRotation);

        bone.localRotation =
            originalLocalRotation *
            Quaternion.AngleAxis(currentYawRotation, GetRotationAxis(yawAxis)) *
            Quaternion.AngleAxis(currentPitchRotation, GetRotationAxis(pitchAxis));
    }

    private void CacheOriginalRotation()
    {
        if (bone == null)
            return;

        originalLocalRotation = bone.localRotation;
        hasOriginalRotation = true;
    }

    private float MoveRotation(float currentRotation, float targetRotation)
    {
        float speed = Mathf.Abs(targetRotation) > 0f ? rotationSpeed : returnSpeed;
        return Mathf.MoveTowards(currentRotation, targetRotation, speed * Time.deltaTime);
    }

    private float GetTargetYawRotation()
    {
        if (input == null)
            return 0f;

        float rawX = Mathf.Abs(input.LookInput.x);
        if (rawX < stickDeadzone)
            return 0f;

        float remapped = Mathf.Clamp01((rawX - stickDeadzone) / (1f - stickDeadzone));
        float curved = Mathf.Sign(input.LookInput.x) * Mathf.Pow(remapped, stickCurve);
        float direction = invertYaw ? -1f : 1f;

        return Mathf.Clamp(curved * maxYawRotation * direction, -maxYawRotation, maxYawRotation);
    }

    private float GetTargetPitchRotation()
    {
        if (input == null)
            return 0f;

        float rawY = Mathf.Abs(input.LookInput.y);
        if (rawY < stickDeadzone)
            return 0f;

        float remapped = Mathf.Clamp01((rawY - stickDeadzone) / (1f - stickDeadzone));
        float curved = Mathf.Sign(input.LookInput.y) * Mathf.Pow(remapped, stickCurve);
        float direction = invertPitch ? -1f : 1f;

        return Mathf.Clamp(curved * maxPitchRotation * direction, -maxPitchRotation, maxPitchRotation);
    }

    private Vector3 GetRotationAxis(RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.up;
        }
    }
}
