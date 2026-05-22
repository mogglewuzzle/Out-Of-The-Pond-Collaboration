using UnityEngine;

public class DeathWatcher : MonoBehaviour
{
    [Header("References")]
    public GameObject target;                  // The player or object to watch
    public PlayerHealth healthScript;          // The PlayerHealth component
    public GameObject deathEffectPrefab;       // Particle effect prefab
    public GameObject objectToActivate;        // Object to activate after death delay

    [Header("Settings")]
    public float heightAboveGround = 1.5f;     // How high above the ground to spawn effect
    public float destroyDelay = 2f;            // Seconds to wait before destroying target
    public float activateDelay = 3f;           // Seconds after death to activate object

    private bool hasDied = false;

    void Update()
    {
        if (target == null || healthScript == null || hasDied)
            return;

        if (healthScript.GetHealth() <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        hasDied = true;

        // Spawn particle effect
        if (deathEffectPrefab != null)
        {
            Vector3 spawnPosition = target.transform.position;

            // Raycast to find ground below player
            RaycastHit hit;
            if (Physics.Raycast(target.transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 10f))
            {
                spawnPosition = hit.point + Vector3.up * heightAboveGround;
            }

            GameObject effect = Instantiate(deathEffectPrefab, spawnPosition, Quaternion.identity);

            ParticleSystem ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 5f);
            }
        }

        // Destroy player after delay
        Destroy(target, destroyDelay);

        // Activate object after delay
        if (objectToActivate != null)
        {
            Invoke(nameof(ActivateObject), activateDelay);
        }
    }

    void ActivateObject()
    {
        objectToActivate.SetActive(true);
    }
}