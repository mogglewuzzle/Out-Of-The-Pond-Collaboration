using UnityEngine;

public class PlayParticleOnCollision : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Enemy";       // Tag that triggers the effect
    public ParticleSystem particleEffect;    // Particle system to play
    public bool moveEffectToHitPoint = true;
    public bool logParticleDebug;

    void Start()
    {
        if (particleEffect != null)
        {
            // Initialize particle system without playing
            particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.transform.position;
        TryPlayEffect(collision.gameObject, hitPoint);
    }

    void OnTriggerEnter(Collider other)
    {
        TryPlayEffect(other.gameObject, other.ClosestPoint(transform.position));
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryPlayEffect(hit.gameObject, hit.point);
    }

    private void TryPlayEffect(GameObject other, Vector3 hitPoint)
    {
        if (!HasTagInHierarchy(other.transform, targetTag))
        {
            if (logParticleDebug)
                Debug.Log($"{nameof(PlayParticleOnCollision)} ignored {other.name}: tag did not match {targetTag}.", this);

            return;
        }

        PlayEffect(hitPoint);
    }

    private void PlayEffect(Vector3 hitPoint)
    {
        if (particleEffect == null)
            return;

        if (moveEffectToHitPoint)
            particleEffect.transform.position = hitPoint;

        particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleEffect.Play(true);
    }

    private bool HasTagInHierarchy(Transform candidate, string tagToFind)
    {
        if (candidate == null || string.IsNullOrEmpty(tagToFind))
            return false;

        Transform current = candidate;
        while (current != null)
        {
            if (current.CompareTag(tagToFind))
                return true;

            current = current.parent;
        }

        return false;
    }
}
