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

    [Header("Rotation Settings")]
    public bool smoothRotation = true;
    public float rotationSpeed = 5f;

    // ── Dialogue pause ────────────────────────────────────────────────────────
    [Header("Dialogue")]
    [Tooltip("When enabled, the enemy stops chasing while the player is in dialogue.")]
    public bool pauseDuringDialogue = true;

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

        // ── Pause during dialogue ─────────────────────────────────────────────
        if (pauseDuringDialogue && IsPlayerInDialogue())
        {
            // Stop the agent in place without disabling it
            if (agent.isOnNavMesh)
                agent.SetDestination(transform.position);
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