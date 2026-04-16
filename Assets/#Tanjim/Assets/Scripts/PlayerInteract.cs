using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 2f;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            Debug.Log("Q pressed");
            TryInteract();
        }
    }

    void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Interactable"))
            {
                Debug.Log("Interacted with " + hit.gameObject.name);
                return;
            }
        }

        Debug.Log("Nothing to interact with");
    }
}