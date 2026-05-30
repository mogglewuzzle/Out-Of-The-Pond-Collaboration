using UnityEngine;

public class ObjectAxisMover : MonoBehaviour
{
    private enum MoveAxis
    {
        X,
        Y,
        Z
    }

    private enum TargetMode
    {
        Distance,
        Transform
    }

    [Header("Movement")]
    [SerializeField] private MoveAxis axis = MoveAxis.Y;
    [SerializeField] private TargetMode targetMode = TargetMode.Distance;
    [SerializeField] private float distance = 1f;
    [Tooltip("When Target Mode is Transform, the target uses this Transform's position only on the selected axis.")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float speed = 1f;
    [Tooltip("When disabled, the object stops permanently after reaching its target.")]
    [SerializeField] private bool returnToStart = true;

    [Header("Timing")]
    [Tooltip("Seconds to wait before the first movement starts.")]
    [SerializeField] private float startDelay = 0f;
    [Tooltip("Seconds to wait after the object returns to its original position.")]
    [SerializeField] private float returnInterval = 0f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private bool movingToTarget = true;
    private bool movementComplete;

    private void Awake()
    {
        startPosition = transform.position;
        targetPosition = GetTargetPosition();
        waitTimer = Mathf.Max(0f, startDelay);
    }

    private void Update()
    {
        if (movementComplete)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 destination = movingToTarget ? targetPosition : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, destination) > 0.001f)
            return;

        if (movingToTarget && !returnToStart)
        {
            movementComplete = true;
            return;
        }

        movingToTarget = !movingToTarget;

        if (movingToTarget)
            waitTimer = Mathf.Max(0f, returnInterval);
    }

    private Vector3 GetAxisDirection()
    {
        switch (axis)
        {
            case MoveAxis.X:
                return Vector3.right;
            case MoveAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.up;
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (targetMode == TargetMode.Transform && targetTransform != null)
            return GetPositionOnSelectedAxis(startPosition, targetTransform.position);

        return startPosition + GetAxisDirection() * distance;
    }

    private Vector3 GetPositionOnSelectedAxis(Vector3 basePosition, Vector3 axisSourcePosition)
    {
        Vector3 position = basePosition;

        switch (axis)
        {
            case MoveAxis.X:
                position.x = axisSourcePosition.x;
                break;
            case MoveAxis.Z:
                position.z = axisSourcePosition.z;
                break;
            default:
                position.y = axisSourcePosition.y;
                break;
        }

        return position;
    }
}
