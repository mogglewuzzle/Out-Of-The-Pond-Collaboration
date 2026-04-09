using UnityEngine;

public class HazardReset : MonoBehaviour
{
    public Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.position = respawnPosition;

            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("Player hit hazard and was reset!");
        }
    }
}
