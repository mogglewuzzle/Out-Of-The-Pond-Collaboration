using UnityEngine;

public class Player_EmitParticles : MonoBehaviour
{
    [Header("Health Source")]
    [Tooltip("Optional. If empty, this script searches this object, parents, then children for Player_Health.")]
    [SerializeField] private Player_Health playerHealth;
    [Tooltip("Runtime display only. The Player_Health component currently being watched.")]
    [SerializeField] private Player_Health foundPlayerHealth;

    [Header("Particle Effects")]
    [Tooltip("Played whenever player health drops.")]
    [SerializeField] private ParticleSystem healthDropEffect;
    [Tooltip("Played in addition to Health Drop Effect when health reaches 0.")]
    [SerializeField] private ParticleSystem zeroHealthEffect;
    [Tooltip("Optional. If assigned, particle effects are moved here before playing.")]
    [SerializeField] private Transform emitPoint;

    [Header("Playback")]
    [SerializeField] private bool moveEffectsToEmitPoint = true;
    [SerializeField] private bool restartEffectsBeforePlaying = true;
    [SerializeField] private bool logParticleDebug;

    private int previousHealth;
    private bool hasHealthSnapshot;
    private Player_Health subscribedPlayerHealth;

    [Header("Runtime Display")]
    [Tooltip("Runtime display only. The scene particle system used for health drops.")]
    [SerializeField] private ParticleSystem healthDropEffectInstance;
    [Tooltip("Runtime display only. The scene particle system used for zero health.")]
    [SerializeField] private ParticleSystem zeroHealthEffectInstance;

    private void Awake()
    {
        ResolvePlayerHealth();
        healthDropEffectInstance = GetPlayableEffect(healthDropEffect);
        zeroHealthEffectInstance = GetPlayableEffect(zeroHealthEffect);
        StopEffect(healthDropEffectInstance);
        StopEffect(zeroHealthEffectInstance);
    }

    private void OnEnable()
    {
        ResolvePlayerHealth();
        SubscribeToPlayerHealth();
        SnapshotHealth();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerHealth();
    }

    private void Update()
    {
        if (foundPlayerHealth == null)
        {
            ResolvePlayerHealth();
            SubscribeToPlayerHealth();
            SnapshotHealth();
            return;
        }

        int currentHealth = foundPlayerHealth.CurrentHealth;

        if (!hasHealthSnapshot)
        {
            SnapshotHealth();
            return;
        }

        if (currentHealth < previousHealth)
            PlayDamageEffects(currentHealth);

        previousHealth = currentHealth;
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth != null)
        {
            foundPlayerHealth = playerHealth;
            return;
        }

        foundPlayerHealth = GetComponent<Player_Health>();

        if (foundPlayerHealth == null)
            foundPlayerHealth = GetComponentInParent<Player_Health>();

        if (foundPlayerHealth == null)
            foundPlayerHealth = GetComponentInChildren<Player_Health>();

        if (foundPlayerHealth == null && logParticleDebug)
            Debug.LogWarning($"{nameof(Player_EmitParticles)} on {name} could not find a {nameof(Player_Health)} component.", this);
    }

    private void SubscribeToPlayerHealth()
    {
        if (subscribedPlayerHealth == foundPlayerHealth)
            return;

        UnsubscribeFromPlayerHealth();

        if (foundPlayerHealth == null)
            return;

        subscribedPlayerHealth = foundPlayerHealth;
        subscribedPlayerHealth.HealthDropped += HandleHealthDropped;
    }

    private void UnsubscribeFromPlayerHealth()
    {
        if (subscribedPlayerHealth == null)
            return;

        subscribedPlayerHealth.HealthDropped -= HandleHealthDropped;
        subscribedPlayerHealth = null;
    }

    private void HandleHealthDropped(int previousValue, int currentValue)
    {
        previousHealth = previousValue;
        hasHealthSnapshot = true;
        PlayDamageEffects(currentValue);
        previousHealth = currentValue;
    }

    private void SnapshotHealth()
    {
        if (foundPlayerHealth == null)
        {
            hasHealthSnapshot = false;
            return;
        }

        previousHealth = foundPlayerHealth.CurrentHealth;
        hasHealthSnapshot = true;
    }

    private void PlayDamageEffects(int currentHealth)
    {
        if (logParticleDebug)
            Debug.Log($"{nameof(Player_EmitParticles)} on {name} detected health drop from {previousHealth} to {currentHealth}.", this);

        PlayEffect(healthDropEffect, ref healthDropEffectInstance);

        if (currentHealth <= 0)
            PlayEffect(zeroHealthEffect, ref zeroHealthEffectInstance);
    }

    [ContextMenu("Test Health Drop Effect")]
    private void TestHealthDropEffect()
    {
        PlayEffect(healthDropEffect, ref healthDropEffectInstance);
    }

    [ContextMenu("Test Zero Health Effect")]
    private void TestZeroHealthEffect()
    {
        PlayEffect(zeroHealthEffect, ref zeroHealthEffectInstance);
    }

    private void PlayEffect(ParticleSystem effectTemplate, ref ParticleSystem effectInstance)
    {
        ParticleSystem effect = GetPlayableEffect(effectTemplate, ref effectInstance);
        if (effect == null)
            return;

        if (moveEffectsToEmitPoint)
        {
            Transform targetPoint = emitPoint != null ? emitPoint : foundPlayerHealth != null ? foundPlayerHealth.transform : null;
            if (targetPoint != null)
                effect.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        }

        if (restartEffectsBeforePlaying)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        effect.Play(true);
    }

    private ParticleSystem GetPlayableEffect(ParticleSystem effectTemplate)
    {
        ParticleSystem effectInstance = null;
        return GetPlayableEffect(effectTemplate, ref effectInstance);
    }

    private ParticleSystem GetPlayableEffect(ParticleSystem effectTemplate, ref ParticleSystem effectInstance)
    {
        if (effectTemplate == null)
            return null;

        if (effectInstance != null)
            return effectInstance;

        if (effectTemplate.gameObject.scene.IsValid())
        {
            effectInstance = effectTemplate;
            return effectInstance;
        }

        Transform targetPoint = GetEffectTargetPoint();
        Vector3 spawnPosition = targetPoint != null ? targetPoint.position : transform.position;
        Quaternion spawnRotation = targetPoint != null ? targetPoint.rotation : transform.rotation;

        effectInstance = Instantiate(effectTemplate, spawnPosition, spawnRotation, targetPoint);
        effectInstance.name = $"{effectTemplate.name} Runtime";
        return effectInstance;
    }

    private Transform GetEffectTargetPoint()
    {
        if (emitPoint != null)
            return emitPoint;

        if (foundPlayerHealth != null)
            return foundPlayerHealth.transform;

        return transform;
    }

    private void StopEffect(ParticleSystem effect)
    {
        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
