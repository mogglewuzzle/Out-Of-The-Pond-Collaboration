using UnityEngine;

[DisallowMultipleComponent]
public class Dialogue_AboveCharacterTrigger : MonoBehaviour
{
    private Dialogue_AboveCharacter owner;
    private Collider triggerCollider;

    public void Assign(Dialogue_AboveCharacter newOwner, Collider newTriggerCollider)
    {
        owner = newOwner;
        triggerCollider = newTriggerCollider;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleTriggerEnter(triggerCollider, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner != null)
            owner.HandleTriggerExit(triggerCollider, other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (owner == null || collision == null || collision.collider == null)
            return;

        owner.HandleTriggerEnter(triggerCollider, collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (owner == null || collision == null || collision.collider == null)
            return;

        owner.HandleTriggerExit(triggerCollider, collision.collider);
    }
}
