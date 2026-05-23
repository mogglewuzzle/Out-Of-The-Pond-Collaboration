using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    [System.Serializable]
    private class DamageSource
    {
        public string damageTag = "Hazard";
        public int healthDamageAmount = 99;
        public float cooldown = 0.5f;
    }

    [Header("Health")]
    [SerializeField] private int startingHealth = 10;
    [Tooltip("The player's current health at runtime. When this reaches 0, the player loses one life.")]
    [SerializeField] private int currentHealth;
    [Tooltip("When enabled, health cannot go below 0 after taking damage.")]
    [SerializeField] private bool clampToZero = true;

    [Header("Collision Damage")]
    [SerializeField] private List<DamageSource> damageSources = new List<DamageSource>();
    [SerializeField] private bool logDamageDebug;

    [Header("Health Recovery")]
    [SerializeField] private bool recoverHealthOverTime = true;
    [Tooltip("Seconds the player must avoid damage before health starts recovering.")]
    [SerializeField] private float recoveryDelayAfterHit = 3f;
    [Tooltip("How much health is restored each recovery tick.")]
    [SerializeField] private int recoveryAmount = 1;
    [Tooltip("Seconds between each recovery tick after recovery starts.")]
    [SerializeField] private float recoveryTickInterval = 1f;

    [Header("Low Health Flash")]
    [SerializeField] private bool flashAtLowHealth = true;
    [Tooltip("Health value that starts the warning flash.")]
    [SerializeField] private int lowHealthThreshold = 1;
    [Tooltip("Child object containing the renderer to flash. Defaults to child named 'Cube'.")]
    [SerializeField] private Renderer flashingRenderer;
    [SerializeField] private string flashingChildName = "Cube";
    [Tooltip("Runtime display only. Renderer found on the child named above.")]
    [SerializeField] private Renderer foundFlashingRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("How quickly the material blends to and from the flash colour.")]
    [SerializeField] private float flashSpeed = 8f;
    [Tooltip("Seconds to wait between flashes.")]
    [SerializeField] private float timeBetweenFlashes = 0.25f;

    [Header("Second Low Health Flash")]
    [SerializeField] private bool useSecondLowHealthFlash = true;
    [Tooltip("Lower health value that switches to Second Flash Color.")]
    [SerializeField] private int secondLowHealthThreshold = 1;
    [SerializeField] private Color secondFlashColor = new Color(0.5f, 0f, 1f, 1f);
    [Tooltip("How quickly the material blends to and from the second flash colour.")]
    [SerializeField] private float secondFlashSpeed = 8f;
    [Tooltip("Seconds to wait between second-threshold flashes.")]
    [SerializeField] private float secondTimeBetweenFlashes = 0.25f;

    private Coroutine recoveryCoroutine;
    private Coroutine flashCoroutine;
    private Material flashingMaterial;
    private MaterialPropertyBlock flashingPropertyBlock;
    private Color originalFlashColor;
    private Color activeLowHealthFlashColor;
    private float activeLowHealthFlashSpeed;
    private float activeLowHealthTimeBetweenFlashes;
    private bool hasActiveLowHealthFlashColor;
    [Tooltip("Runtime display only. Material instance being flashed.")]
    [SerializeField] private Material foundFlashingMaterial;
    private int baseColorPropertyId;
    private int colorPropertyId;
    private bool hasInitialized;
    private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();

    public int CurrentHealth => currentHealth;
    public int StartingHealth => startingHealth;
    public event Action<int, int> HealthChanged;
    public event Action<int, int> HealthDropped;

    private void Awake()
    {
        InitializeHealth();
        baseColorPropertyId = Shader.PropertyToID("_BaseColor");
        colorPropertyId = Shader.PropertyToID("_Color");
        EnsureDefaultDamageSources();
        ResolveFlashingMaterial();
        UpdateLowHealthFlash();
    }

    private void OnEnable()
    {
        if (!hasInitialized)
            InitializeHealth();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            currentHealth = startingHealth;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTakeCollisionDamage(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTakeCollisionDamage(other.gameObject);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryTakeCollisionDamage(hit.gameObject);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        int previousHealth = currentHealth;
        currentHealth -= amount;

        if (clampToZero && currentHealth < 0)
            currentHealth = 0;

        NotifyHealthChanged(previousHealth, currentHealth);
        RestartRecoveryTimer();
        UpdateLowHealthFlash();

    }

    public void RestoreHealth()
    {
        int previousHealth = currentHealth;
        currentHealth = startingHealth;
        NotifyHealthChanged(previousHealth, currentHealth);
        StopRecoveryTimer();
        UpdateLowHealthFlash();
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    [ContextMenu("Test Low Health Flash")]
    private void TestLowHealthFlash()
    {
        currentHealth = Mathf.Max(1, lowHealthThreshold);
        UpdateLowHealthFlash();
    }

    private void InitializeHealth()
    {
        currentHealth = startingHealth;
        hasInitialized = true;
    }

    private void NotifyHealthChanged(int previousHealth, int newHealth)
    {
        if (newHealth == previousHealth)
            return;

        HealthChanged?.Invoke(previousHealth, newHealth);

        if (newHealth < previousHealth)
            HealthDropped?.Invoke(previousHealth, newHealth);
    }

    private void TryTakeCollisionDamage(GameObject other)
    {
        if (other == null || currentHealth <= 0)
            return;

        if (!TryGetDamageSource(other.transform, out DamageSource damageSource))
        {
            if (logDamageDebug)
                Debug.Log($"{nameof(Player_Health)} on {name} ignored collision with {other.name}: no matching damage tag.", this);

            return;
        }

        if (damageSource.healthDamageAmount <= 0 || IsOnCooldown(damageSource))
            return;

        if (logDamageDebug)
            Debug.Log($"{nameof(Player_Health)} on {name} taking {damageSource.healthDamageAmount} damage from {other.name}.", this);

        TakeDamage(damageSource.healthDamageAmount);
        StartCooldown(damageSource);
    }

    private bool TryGetDamageSource(Transform hitTransform, out DamageSource damageSource)
    {
        damageSource = null;

        if (damageSources == null)
            return false;

        for (int i = 0; i < damageSources.Count; i++)
        {
            DamageSource candidate = damageSources[i];
            if (candidate == null || string.IsNullOrEmpty(candidate.damageTag))
                continue;

            if (HasTagInHierarchy(hitTransform, candidate.damageTag))
            {
                damageSource = candidate;
                return true;
            }
        }

        return false;
    }

    private bool HasTagInHierarchy(Transform hitTransform, string tagToFind)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            if (current.tag == tagToFind)
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool IsOnCooldown(DamageSource damageSource)
    {
        if (damageSource.cooldown <= 0f)
            return false;

        return cooldownEndTimes.TryGetValue(damageSource.damageTag, out float cooldownEndTime) && Time.time < cooldownEndTime;
    }

    private void StartCooldown(DamageSource damageSource)
    {
        if (damageSource.cooldown <= 0f)
            return;

        cooldownEndTimes[damageSource.damageTag] = Time.time + damageSource.cooldown;
    }

    private void EnsureDefaultDamageSources()
    {
        if (damageSources == null)
            damageSources = new List<DamageSource>();

        if (damageSources.Count > 0)
            return;

        damageSources.Add(new DamageSource { damageTag = "Hazard", healthDamageAmount = 99, cooldown = 0.5f });
        damageSources.Add(new DamageSource { damageTag = "Enemy", healthDamageAmount = 1, cooldown = 1.5f });
    }

    private void RestartRecoveryTimer()
    {
        if (!recoverHealthOverTime || currentHealth <= 0 || currentHealth >= startingHealth)
            return;

        StopRecoveryTimer();
        recoveryCoroutine = StartCoroutine(RecoverHealthAfterDelay());
    }

    private void StopRecoveryTimer()
    {
        if (recoveryCoroutine == null)
            return;

        StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = null;
    }

    private IEnumerator RecoverHealthAfterDelay()
    {
        if (recoveryDelayAfterHit > 0f)
            yield return new WaitForSeconds(recoveryDelayAfterHit);

        while (currentHealth > 0 && currentHealth < startingHealth)
        {
            currentHealth += Mathf.Max(1, recoveryAmount);
            if (currentHealth > startingHealth)
                currentHealth = startingHealth;

            UpdateLowHealthFlash();

            if (currentHealth >= startingHealth)
                break;

            if (recoveryTickInterval > 0f)
                yield return new WaitForSeconds(recoveryTickInterval);
            else
                yield return null;
        }

        recoveryCoroutine = null;
    }

    private void ResolveFlashingMaterial()
    {
        if (flashingRenderer == null && !string.IsNullOrWhiteSpace(flashingChildName))
        {
            Transform child = FindChildByExactName(transform, flashingChildName);
            if (child != null)
                flashingRenderer = child.GetComponent<Renderer>();
        }

        if (flashingRenderer == null)
            return;

        flashingMaterial = GetFirstMaterialWithBaseColor(flashingRenderer);
        foundFlashingRenderer = flashingRenderer;
        foundFlashingMaterial = flashingMaterial;
        flashingPropertyBlock = new MaterialPropertyBlock();
        originalFlashColor = GetMaterialColor();
    }

    private Material GetFirstMaterialWithBaseColor(Renderer rendererToSearch)
    {
        Material[] materials = rendererToSearch.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].HasProperty(baseColorPropertyId))
                return materials[i];
        }

        return rendererToSearch.material;
    }

    private Transform FindChildByExactName(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nestedChild = FindChildByExactName(child, childName);
            if (nestedChild != null)
                return nestedChild;
        }

        return null;
    }

    private void UpdateLowHealthFlash()
    {
        if (!flashAtLowHealth || !ShouldRunLowHealthFlash())
        {
            StopLowHealthFlash();
            return;
        }

        if (flashingMaterial == null)
            ResolveFlashingMaterial();

        if (flashingMaterial == null)
            return;

        LowHealthFlashSettings desiredFlashSettings = GetActiveLowHealthFlashSettings();
        if (flashCoroutine != null && hasActiveLowHealthFlashColor && !ActiveFlashSettingsMatch(desiredFlashSettings))
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            SetMaterialColor(originalFlashColor);
        }

        if (flashCoroutine == null)
        {
            activeLowHealthFlashColor = desiredFlashSettings.color;
            activeLowHealthFlashSpeed = desiredFlashSettings.speed;
            activeLowHealthTimeBetweenFlashes = desiredFlashSettings.timeBetweenFlashes;
            hasActiveLowHealthFlashColor = true;
            flashCoroutine = StartCoroutine(FlashLowHealthMaterial());
        }
    }

    private IEnumerator FlashLowHealthMaterial()
    {
        while (ShouldRunLowHealthFlash())
        {
            yield return LerpFlashColor(originalFlashColor, activeLowHealthFlashColor);
            yield return LerpFlashColor(activeLowHealthFlashColor, originalFlashColor);

            if (activeLowHealthTimeBetweenFlashes > 0f)
                yield return new WaitForSeconds(activeLowHealthTimeBetweenFlashes);
        }

        SetMaterialColor(originalFlashColor);
        flashCoroutine = null;
        hasActiveLowHealthFlashColor = false;
    }

    private IEnumerator LerpFlashColor(Color from, Color to)
    {
        float elapsed = 0f;
        float duration = activeLowHealthFlashSpeed > 0f ? 1f / activeLowHealthFlashSpeed : 0f;

        if (duration <= 0f)
        {
            SetMaterialColor(to);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetMaterialColor(Color.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetMaterialColor(to);
    }

    private bool ShouldRunLowHealthFlash()
    {
        if (currentHealth > lowHealthThreshold)
            return false;

        if (currentHealth > 0)
            return true;

        return lowHealthThreshold <= 0 || (useSecondLowHealthFlash && secondLowHealthThreshold <= 0);
    }

    private LowHealthFlashSettings GetActiveLowHealthFlashSettings()
    {
        if (useSecondLowHealthFlash && currentHealth <= secondLowHealthThreshold)
            return new LowHealthFlashSettings(secondFlashColor, secondFlashSpeed, secondTimeBetweenFlashes);

        return new LowHealthFlashSettings(flashColor, flashSpeed, timeBetweenFlashes);
    }

    private bool ActiveFlashSettingsMatch(LowHealthFlashSettings settings)
    {
        return activeLowHealthFlashColor == settings.color &&
            Mathf.Approximately(activeLowHealthFlashSpeed, settings.speed) &&
            Mathf.Approximately(activeLowHealthTimeBetweenFlashes, settings.timeBetweenFlashes);
    }

    private struct LowHealthFlashSettings
    {
        public readonly Color color;
        public readonly float speed;
        public readonly float timeBetweenFlashes;

        public LowHealthFlashSettings(Color color, float speed, float timeBetweenFlashes)
        {
            this.color = color;
            this.speed = speed;
            this.timeBetweenFlashes = timeBetweenFlashes;
        }
    }

    private void StopLowHealthFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (flashingMaterial != null)
            SetMaterialColor(originalFlashColor);

        hasActiveLowHealthFlashColor = false;
    }

    private Color GetMaterialColor()
    {
        if (flashingMaterial.HasProperty(baseColorPropertyId))
            return flashingMaterial.GetColor(baseColorPropertyId);

        if (flashingMaterial.HasProperty(colorPropertyId))
            return flashingMaterial.GetColor(colorPropertyId);

        return flashingMaterial.color;
    }

    private void SetMaterialColor(Color color)
    {
        if (flashingMaterial == null)
            return;

        if (flashingPropertyBlock == null)
            flashingPropertyBlock = new MaterialPropertyBlock();

        flashingRenderer.GetPropertyBlock(flashingPropertyBlock);
        flashingPropertyBlock.SetColor(baseColorPropertyId, color);
        flashingPropertyBlock.SetColor(colorPropertyId, color);
        flashingRenderer.SetPropertyBlock(flashingPropertyBlock);
    }

}
