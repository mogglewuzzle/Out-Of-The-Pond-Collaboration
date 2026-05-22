using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPivotController : MonoBehaviour
{
    [Header("Input")]
    public PlayerInput playerInput;
    public string lookActionName = "Look";

    [Header("Rotation")]
    public float sensitivity = 120f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    private float yaw;
    private float pitch;
    private InputAction lookAction;

    void Awake()
    {
        lookAction = playerInput.actions[lookActionName];

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void OnEnable() => lookAction.Enable();
    void OnDisable() => lookAction.Disable();

    void LateUpdate()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        float dt = Time.deltaTime;

        yaw   += look.x * sensitivity * dt;
        pitch -= look.y * sensitivity * dt;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}