using UnityEngine;

public class DestroyOnCollision : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Player";  // Tag that triggers destruction

    // Called when using non-trigger colliders
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Destroy(gameObject);
        }
    }

    // Called when using trigger colliders
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Destroy(gameObject);
        }
    }
}