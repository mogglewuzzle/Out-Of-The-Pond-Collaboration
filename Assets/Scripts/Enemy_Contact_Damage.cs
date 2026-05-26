using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public class Enemy_Contact_Damage : MonoBehaviour
{
    [Header("Player Attack Settings")]
    public float minImpactSpeed = 3f;
    public int damageToEnemy = 1;
    public float damageCooldown = 0.5f;

    private Enemy_Health enemyHealth;
    private float lastHitTime = -999f;
    private CharacterController playerController;

    void Awake()
    {
        enemyHealth = GetComponent<Enemy_Health>();
    }

    void Update()
    {
        // Find player controller once
        if (playerController == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerController = p.GetComponent<CharacterController>();
        }

        if (playerController == null) return;
        if (Time.time < lastHitTime + damageCooldown) return;

        // Check if player is close enough and moving fast enough
        float dist = Vector3.Distance(transform.position, playerController.transform.position);
        float playerSpeed = playerController.velocity.magnitude;

        if (dist <= 1.5f && playerSpeed >= minImpactSpeed)
        {
            lastHitTime = Time.time;
            enemyHealth.TakeDamage(damageToEnemy);
            Debug.Log($"[Enemy_Contact_Damage] Hit! Speed: {playerSpeed:F1}, Health left: {enemyHealth.CurrentHealth}");
        }
    }
}