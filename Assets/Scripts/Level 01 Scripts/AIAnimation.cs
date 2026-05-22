using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FollowerAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;           // Assign your Animator
    public float animationSmooth = 10f; // Smoothing for animation transitions

    private NavMeshAgent agent;
    private float smoothSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            Debug.LogWarning("Animator not assigned!");
    }

    void Update()
    {
        if (animator == null || agent == null) return;

        // Smoothly update speed for animation
        float targetSpeed = agent.velocity.magnitude;
        smoothSpeed = Mathf.Lerp(smoothSpeed, targetSpeed, Time.deltaTime * animationSmooth);

        // Stop animation when almost idle
        if (smoothSpeed < 0.05f) smoothSpeed = 0f;

        animator.SetFloat("Speed", smoothSpeed);
    }
}