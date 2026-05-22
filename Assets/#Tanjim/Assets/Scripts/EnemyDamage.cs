using UnityEngine;

/// <summary>
/// EnemyDamage — deals damage to the player on collision.
/// Includes a short cooldown so the enemy can't spam damage every physics frame.
/// Works alongside EnemyAI — no changes to EnemyAI needed.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage")]
    public int   damage       = 1;
    public float damageCooldown = 1f;  // seconds between hits (prevents frame-spam)

    private float lastDamageTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    // OnCollisionStay lets continuous contact also deal damage after the cooldown
    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void TryDamage(GameObject other)
    {
        if (Time.time < lastDamageTime + damageCooldown) return;

<<<<<<< Updated upstream
        PlayerHealthOld health = other.GetComponent<PlayerHealthOld>();
=======
        PlayerHealth02 health = other.GetComponent<PlayerHealth02>();
>>>>>>> Stashed changes
        if (health != null)
        {
            health.TakeDamage(damage);
            lastDamageTime = Time.time;
            Debug.Log("[EnemyDamage] Enemy damaged player");
        }
    }
}