using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public Transform respawnPoint;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("PlayerRespawn attached to: " + gameObject.name);
    }

    public void Respawn()
    {
        Debug.Log("Respawn called");

        if (respawnPoint == null)
        {
            Debug.LogWarning("Respawn point is missing!");
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = respawnPoint.position;
            rb.rotation = respawnPoint.rotation;
        }
        else
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }

        Debug.Log("Player respawned to: " + respawnPoint.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(gameObject.name + " collided with: " + collision.gameObject.name + " tag: " + collision.gameObject.tag);

        if (collision.gameObject.CompareTag("Hazard"))
        {
            Debug.Log("Player died");
            Respawn();
        }
    }
}