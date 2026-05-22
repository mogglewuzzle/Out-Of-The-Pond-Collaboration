using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MouseTargetWithAnimation : MonoBehaviour
{
    public Animator animator; // assign in inspector

    private NavMeshAgent agent;
    private Camera cam;
    private float smoothSpeed; // smoothed speed

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        cam = Camera.main;

        if (animator == null)
            Debug.LogWarning("Animator not assigned!");
        if (cam == null)
            Debug.LogError("No MainCamera found! Tag your camera as MainCamera.");
    }

    void Update()
    {
        HandleMouseClick();
        UpdateAnimation();
    }

    void HandleMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hit.point, out navHit, 1f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                    agent.isStopped = false;
                }
            }
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        // Smooth speed to avoid animation flicker
        float targetSpeed = agent.velocity.magnitude;
        smoothSpeed = Mathf.Lerp(smoothSpeed, targetSpeed, Time.deltaTime * 10f);

        // Apply a small threshold to stop animation when almost idle
        if (smoothSpeed < 0.05f)
            smoothSpeed = 0f;

        animator.SetFloat("Speed", smoothSpeed);

        // Stop agent when reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            agent.isStopped = true;
    }
}