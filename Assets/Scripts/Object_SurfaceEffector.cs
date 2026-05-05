using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Object_SurfaceEffector : MonoBehaviour
{
    private enum PushMode
    {
        Force,
        SetHorizontalVelocity
    }

    [Header("Push")]
    [Tooltip("World-space direction to push affected rigidbodies. Use X, Y, or Z values to choose direction.")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [Tooltip("Push strength. In Force mode this is acceleration. In Set Horizontal Velocity mode this is target speed.")]
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private PushMode pushMode = PushMode.SetHorizontalVelocity;

    [Header("Affected Objects")]
    [SerializeField] private string affectedTag = "Player";

    [Header("Collider")]
    [Tooltip("Collider used as the trigger area. If empty, the collider on this GameObject is used.")]
    [SerializeField] private Collider triggerCollider;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogWarning($"{nameof(Object_SurfaceEffector)} on {name} needs a trigger collider assigned.", this);
            return;
        }

        if (!triggerCollider.isTrigger)
            Debug.LogWarning($"{nameof(Object_SurfaceEffector)} on {name} needs its collider set to Is Trigger.", this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsAffectedObject(other))
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null)
            return;

        Vector3 direction = pushDirection.normalized;
        if (direction == Vector3.zero)
            return;

        PlayerMovementController movement = rb.GetComponent<PlayerMovementController>();
        if (movement != null)
        {
            Vector3 playerPush = direction * pushForce;

            if (pushMode == PushMode.Force)
                playerPush *= Time.fixedDeltaTime;

            movement.AddExternalVelocity(playerPush);
            return;
        }

        if (pushMode == PushMode.Force)
        {
            rb.AddForce(direction * pushForce, ForceMode.Acceleration);
            return;
        }

        Vector3 pushVelocity = direction * pushForce;
        rb.linearVelocity = new Vector3(pushVelocity.x, rb.linearVelocity.y + pushVelocity.y, pushVelocity.z);
    }

    private bool IsAffectedObject(Collider other)
    {
        if (string.IsNullOrWhiteSpace(affectedTag))
            return true;

        Rigidbody rb = other.attachedRigidbody;
        GameObject candidate = rb != null ? rb.gameObject : other.gameObject;
        return candidate.tag == affectedTag;
    }
}
