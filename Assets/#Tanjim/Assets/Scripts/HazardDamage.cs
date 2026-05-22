using UnityEngine;

public class HazardDamage : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered hazard: " + other.gameObject.name);

<<<<<<< Updated upstream
        PlayerHealthOld health = other.GetComponent<PlayerHealthOld>();
=======
        PlayerHealth02 health = other.GetComponent<PlayerHealth02>();
>>>>>>> Stashed changes

        if (health != null)
        {
            Debug.Log("Hazard hit player");
            health.TakeDamage(damage);
        }
    }
}