using UnityEngine;

public class HazardDamage : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered hazard: " + other.gameObject.name);

        PlayerHealth health = other.GetComponent<PlayerHealth>();

        if (health != null)
        {
            Debug.Log("Hazard hit player");
            health.TakeDamage(damage);
        }
    }
}