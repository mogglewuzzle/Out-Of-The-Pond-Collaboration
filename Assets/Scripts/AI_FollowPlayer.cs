using UnityEngine;
using UnityEngine.AI;

public class CharacterFollower : MonoBehaviour
{
    [Header("Target Settings")]
    public string targetTag = "Player"; // Tag to follow
    [Tooltip("Runtime display only. The follow target currently found by Target Tag.")]
    [SerializeField] private Transform foundFollowTarget;

    private NavMeshAgent agent;

    [Header("Follow Settings")]
    public float followDistance = 2f;
    public float minSpeed = 2f;         // Minimum move speed
    public float maxSpeed = 5f;         // Maximum move speed
    public bool randomizeSpeedOnStart = true; // Randomize speed at start
    public bool predictTargetMovement = false;
    public float predictionTime = 0.5f;

    [Header("Rotation Settings")]
    public bool smoothRotation = true;
    public float rotationSpeed = 5f;

    private Vector3 lastTargetPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent required for follower!");
            enabled = false;
            return;
        }

        // Set initial speed
        agent.speed = randomizeSpeedOnStart ? Random.Range(minSpeed, maxSpeed) : minSpeed;
        agent.stoppingDistance = followDistance;
        agent.updateRotation = false; // We'll handle rotation manually
    }

    void Start()
    {
        lastTargetPosition = Vector3.zero;
    }

    bool TryFindTarget()
    {
        if (string.IsNullOrWhiteSpace(targetTag))
            return false;

        GameObject targetObj;
        try
        {
            targetObj = GameObject.FindGameObjectWithTag(targetTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(CharacterFollower)} on {name} cannot find follow target: tag '{targetTag}' is not defined.", this);
            return false;
        }

        if (targetObj == null)
            return false;

        foundFollowTarget = targetObj.transform;
        lastTargetPosition = foundFollowTarget.position;
        return true;
    }

    void Update()
    {
        if (foundFollowTarget == null && !TryFindTarget())
            return;

        Vector3 destination = foundFollowTarget.position;

        if (predictTargetMovement)
        {
            Vector3 targetVelocity = (foundFollowTarget.position - lastTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            destination += targetVelocity * predictionTime;
        }

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }

        lastTargetPosition = foundFollowTarget.position;

        // Smooth rotation
        if (smoothRotation && agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = agent.velocity.normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    // Optional: Call this to randomize speed at runtime
    public void RandomizeSpeed()
    {
        if (agent != null)
        {
            agent.speed = Random.Range(minSpeed, maxSpeed);
        }
    }
}
