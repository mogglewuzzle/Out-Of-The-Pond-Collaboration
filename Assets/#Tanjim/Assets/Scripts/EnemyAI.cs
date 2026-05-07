using UnityEngine;

/// <summary>
/// EnemyAI — patrol + chase state machine.
///
/// Recommended Rigidbody settings:
///   Is Kinematic        : true
///   Interpolate         : Interpolate
///   Freeze Rotation XYZ : checked
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    // ── Patrol ────────────────────────────────────────────────────────────────
    [Header("Patrol")]
    public float moveDistance = 3f;
    public float moveSpeed    = 2f;

    // ── Detection / Chase ─────────────────────────────────────────────────────
    [Header("Detection")]
    public float detectionRange = 25f;
    public float chaseSpeed     = 5f;
    public float loseRange      = 35f;

    // ── Ground snapping ───────────────────────────────────────────────────────
    [Header("Ground Snapping")]
    [Tooltip("Enable to keep the enemy on the ground surface while moving.")]
    public bool  snapToGround  = true;
    [Tooltip("How high above the enemy to start the downward raycast.")]
    public float rayStartAbove = 2f;
    [Tooltip("Maximum distance to look downward for the ground.")]
    public float rayDistance   = 6f;
    [Tooltip("How far above the ground hit point the enemy rests.")]
    public float groundOffset  = 0.5f;

    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    public Transform player;

    // ── Internal ──────────────────────────────────────────────────────────────
    private enum State { Patrol, Chase }
    private State currentState = State.Patrol;

    private Vector3   startPos;
    private bool      movingRight = true;
    private Rigidbody rb;
    private Collider  myCollider;   // used to exclude self from raycast

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        startPos   = transform.position;
        rb         = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                Debug.LogWarning("[EnemyAI] No GameObject tagged 'Player' found.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrol:
                if (dist <= detectionRange)
                {
                    currentState = State.Chase;
                    Debug.Log("[EnemyAI] → CHASE");
                }
                break;

            case State.Chase:
                if (dist > loseRange)
                {
                    currentState = State.Patrol;
                    Debug.Log("[EnemyAI] → PATROL");
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        switch (currentState)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.Chase:  HandleChase();  break;
        }
    }

    // ── Patrol ────────────────────────────────────────────────────────────────
    private void HandlePatrol()
    {
        float   dir  = movingRight ? 1f : -1f;
        Vector3 next = rb.position + Vector3.right * dir * moveSpeed * Time.fixedDeltaTime;
        next         = ApplyGroundSnap(next);
        rb.MovePosition(next);

        if ( movingRight && rb.position.x >= startPos.x + moveDistance) movingRight = false;
        if (!movingRight && rb.position.x <= startPos.x - moveDistance) movingRight = true;
    }

    // ── Chase ─────────────────────────────────────────────────────────────────
    private void HandleChase()
    {
        // Chase on X/Z only — ground snap handles Y so enemy stays on terrain
        Vector3 flat      = new Vector3(player.position.x, rb.position.y, player.position.z);
        Vector3 direction = flat - rb.position;

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 next = rb.position + direction.normalized * chaseSpeed * Time.fixedDeltaTime;
        next         = ApplyGroundSnap(next);
        rb.MovePosition(next);
    }

    // ── Ground snap ───────────────────────────────────────────────────────────
    // Casts downward from above the candidate position.
    // Excludes the enemy's own collider so it can't snap to itself.
    // Only applies the snap if the hit is within a reasonable vertical range.
    private Vector3 ApplyGroundSnap(Vector3 candidatePos)
    {
        if (!snapToGround) return candidatePos;

        Vector3 origin = candidatePos + Vector3.up * rayStartAbove;

        // QueryTriggerInteraction.Ignore prevents snapping to trigger volumes
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            rayStartAbove + rayDistance,
                            Physics.AllLayers,
                            QueryTriggerInteraction.Ignore))
        {
            // Make sure we didn't hit ourselves
            if (hit.collider == myCollider) return candidatePos;

            // Sanity-check: only snap if the ground is within rayDistance below
            // the candidate position — prevents flying off to distant geometry
            float distBelow = candidatePos.y - hit.point.y;
            if (distBelow > -1f && distBelow < rayDistance)
            {
                candidatePos.y = hit.point.y + groundOffset;
            }
        }

        return candidatePos;
    }

    // ── Scene gizmos ──────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}