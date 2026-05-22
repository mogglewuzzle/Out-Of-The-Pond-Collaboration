using UnityEngine;

public class PlayParticleOnCollision : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Enemy";       // Tag that triggers the effect
    public ParticleSystem particleEffect;    // Particle system to play

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
        if (collision.gameObject.CompareTag(targetTag))
            PlayEffect();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
            PlayEffect();
    }

    private void PlayEffect()
    {
        if (particleEffect != null)
            particleEffect.Play();
    }
}