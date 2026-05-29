using UnityEngine;
using UnityEngine.AI;

public class CharacterFollower : MonoBehaviour
{
    [Header("Target Settings")]
    public string targetTag = "Player";
    [Tooltip("Runtime display only. The follow target currently found by Target Tag.")]
    [SerializeField] private Transform foundFollowTarget;

    private NavMeshAgent agent;

    [Header("Follow Settings")]
    public float followDistance = 2f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;
    public bool randomizeSpeedOnStart = true;
    public bool predictTargetMovement = false;
    public float predictionTime = 0.5f;
    [Tooltip("Extra distance beyond Follow Distance before the enemy starts chasing again. Prevents jitter at the edge of the stopping distance.")]
    public float followResumeBuffer = 0.5f;

    [Header("Orbit")]
    [Tooltip("When enabled, the enemy circles the target instead of stopping immediately once it reaches Follow Distance.")]
    public bool allowOrbitAtFollowDistance;
    [Tooltip("Seconds to orbit after reaching Follow Distance. Set to 0 or less to orbit until disabled.")]
    public float orbitDuration = 2f;
    public float orbitSpeed = 90f;

    [Header("Rotation Settings")]
    public bool smoothRotation = true;
    public float rotationSpeed = 5f;

    // ── Dialogue pause ────────────────────────────────────────────────────────
    [Header("Dialogue")]
    [Tooltip("When enabled, the enemy stops chasing while the player is in dialogue.")]
    public bool pauseDuringDialogue = true;

    private Vector3 lastTargetPosition;
    private bool isOrbiting;
    private bool hasCompletedOrbit;
    private float orbitStartTime;
    private Vector3 orbitDirection;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent required for follower!");
            enabled = false;
            return;
        }

        agent.speed = randomizeSpeedOnStart ? Random.Range(minSpeed, maxSpeed) : minSpeed;
        agent.stoppingDistance = followDistance;
        agent.updateRotation = false;
    }

    void Start()
    {
        lastTargetPosition = Vector3.zero;
    }

    bool TryFindTarget()
    {
        if (string.IsNullOrWhiteSpace(targetTag))
            return false;

        GameObject[] targetObjects;
        try
        {
            targetObjects = GameObject.FindGameObjectsWithTag(targetTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(CharacterFollower)} on {name} cannot find follow target: tag '{targetTag}' is not defined.", this);
            return false;
        }

        if (targetObjects == null || targetObjects.Length == 0)
            return false;

        foundFollowTarget = ResolveFollowTarget(targetObjects);
        if (foundFollowTarget == null)
            return false;

        lastTargetPosition = foundFollowTarget.position;
        return true;
    }

    private Transform ResolveFollowTarget(GameObject[] targetObjects)
    {
        Transform fallbackTarget = null;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject candidate = targetObjects[i];
            if (candidate == null)
                continue;

            Player_Health playerHealth = candidate.GetComponentInParent<Player_Health>();
            if (playerHealth != null)
                return playerHealth.transform;

            if (fallbackTarget == null)
                fallbackTarget = candidate.transform;
        }

        return fallbackTarget;
    }

    void Update()
    {
        if (foundFollowTarget == null && !TryFindTarget())
            return;

        // ── Pause during dialogue ─────────────────────────────────────────────
        if (pauseDuringDialogue && IsPlayerInDialogue())
        {
            // Stop the agent in place without disabling it
            if (agent.isOnNavMesh)
                agent.SetDestination(transform.position);
            return;
        }

        agent.stoppingDistance = Mathf.Max(0f, followDistance);

        Vector3 toTarget = foundFollowTarget.position - transform.position;
        toTarget.y = 0f;
        float distanceToTarget = toTarget.magnitude;

        if (ShouldOrbit(distanceToTarget))
        {
            OrbitTarget(toTarget);
            lastTargetPosition = foundFollowTarget.position;
            return;
        }

        float effectiveFollowDistance = Mathf.Max(0f, followDistance);
        float effectiveResumeBuffer = effectiveFollowDistance > 0f ? Mathf.Max(0f, followResumeBuffer) : 0f;

        if (distanceToTarget <= effectiveFollowDistance)
        {
            StopFollowing();
            lastTargetPosition = foundFollowTarget.position;
            return;
        }

        if (distanceToTarget <= effectiveFollowDistance + effectiveResumeBuffer)
        {
            lastTargetPosition = foundFollowTarget.position;
            return;
        }

        Vector3 destination = foundFollowTarget.position;

        if (predictTargetMovement)
        {
            Vector3 targetVelocity = (foundFollowTarget.position - lastTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            destination += targetVelocity * predictionTime;
        }

        if (agent.isOnNavMesh)
            agent.SetDestination(destination);

        lastTargetPosition = foundFollowTarget.position;

        if (smoothRotation && agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = agent.velocity.normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    private bool ShouldOrbit(float distanceToTarget)
    {
        if (!allowOrbitAtFollowDistance || hasCompletedOrbit)
            return false;

        if (!isOrbiting && distanceToTarget > followDistance)
            return false;

        if (!isOrbiting)
            StartOrbit();

        if (orbitDuration > 0f && Time.time - orbitStartTime >= orbitDuration)
        {
            isOrbiting = false;
            hasCompletedOrbit = true;
            StopFollowing();
            return false;
        }

        return true;
    }

    private void StartOrbit()
    {
        isOrbiting = true;
        orbitStartTime = Time.time;
        orbitDirection = Random.value < 0.5f ? Vector3.left : Vector3.right;
    }

    private void OrbitTarget(Vector3 toTarget)
    {
        if (!agent.isOnNavMesh || foundFollowTarget == null)
            return;

        Vector3 awayFromTarget = toTarget.sqrMagnitude > 0.001f
            ? -toTarget.normalized
            : -transform.forward;

        Vector3 orbitOffset = Quaternion.AngleAxis(orbitSpeed * Time.deltaTime * orbitDirection.x, Vector3.up) * awayFromTarget * followDistance;
        agent.SetDestination(foundFollowTarget.position + orbitOffset);
    }

    private void StopFollowing()
    {
        if (!agent.isOnNavMesh)
            return;

        if (agent.hasPath)
            agent.ResetPath();

        agent.velocity = Vector3.zero;
    }

    private bool IsPlayerInDialogue()
    {
        // Uses Dialogue_Manager singleton — returns false safely if no manager exists
        return Dialogue_Manager.Instance != null && Dialogue_Manager.Instance.IsDialogueActive;
    }

    public void RandomizeSpeed()
    {
        if (agent != null)
            agent.speed = Random.Range(minSpeed, maxSpeed);
    }
}
