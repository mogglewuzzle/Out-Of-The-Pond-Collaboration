using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Player_Knockback : MonoBehaviour
{
    [System.Serializable]
    private class KnockbackRule
    {
        public string triggeringTag = "Enemy";
        public float horizontalForce = 8f;
        public float verticalForce = 5f;
        public float duration = 0.25f;
    }

    [Header("Test Knockback")]
    [SerializeField] private float horizontalForce = 8f;
    [SerializeField] private float verticalForce = 5f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("Collision Knockbacks")]
    [SerializeField] private List<KnockbackRule> knockbacks = new List<KnockbackRule>();

    [Header("Direction")]
    [Tooltip("When enabled, test knockback pushes opposite the player's forward direction.")]
    [SerializeField] private bool usePlayerBackward = true;
    [Tooltip("Used when Use Player Backward is disabled.")]
    [SerializeField] private Vector3 customHorizontalDirection = Vector3.back;
    [Tooltip("Optional. Disable normal movement while the test knockback is active so horizontal force is not overwritten.")]
    [SerializeField] private bool disableMovementDuringKnockback = true;

    [Header("Runtime Display")]
    [Tooltip("Runtime display only. Rigidbody found on this player.")]
    [SerializeField] private Rigidbody foundRigidbody;
    [Tooltip("Runtime display only. Movement controller used for external launch, when available.")]
    [SerializeField] private PlayerMovementController foundMovementController;

    private Coroutine knockbackRoutine;
    private bool movementDisabledByKnockback;

    private void Awake()
    {
        foundRigidbody = GetComponent<Rigidbody>();
        foundMovementController = GetComponent<PlayerMovementController>();
        EnsureDefaultKnockbacks();
    }

    [ContextMenu("Test Knockback")]
    public void TestKnockback()
    {
        ApplyKnockback(GetTestDirection());
    }

    public void ApplyKnockback(Vector3 horizontalDirection)
    {
        ApplyKnockback(horizontalDirection, horizontalForce, verticalForce, knockbackDuration);
    }

    private void OnDisable()
    {
        RestoreMovementController();
        knockbackRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 sourcePosition = collision.contactCount > 0 ? collision.GetContact(0).point : collision.transform.position;
        TryApplyCollisionKnockback(collision.gameObject, sourcePosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyCollisionKnockback(other.gameObject, other.transform.position);
    }

    private void TryApplyCollisionKnockback(GameObject other, Vector3 sourcePosition)
    {
        if (other == null || !TryGetKnockbackRule(other.transform, out KnockbackRule rule))
            return;

        Vector3 directionAwayFromSource = transform.position - sourcePosition;
        ApplyKnockback(directionAwayFromSource, rule.horizontalForce, rule.verticalForce, rule.duration);
    }

    private void ApplyKnockback(Vector3 horizontalDirection, float selectedHorizontalForce, float selectedVerticalForce, float selectedDuration)
    {
        if (foundRigidbody == null)
            foundRigidbody = GetComponent<Rigidbody>();

        if (foundMovementController == null)
            foundMovementController = GetComponent<PlayerMovementController>();

        Vector3 flatDirection = horizontalDirection;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.0001f)
            flatDirection = -transform.forward;

        flatDirection.Normalize();

        Vector3 knockbackVelocity = flatDirection * selectedHorizontalForce + Vector3.up * selectedVerticalForce;

        if (foundRigidbody == null && foundMovementController == null)
            return;

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            RestoreMovementController();
        }

        knockbackRoutine = StartCoroutine(KnockbackRoutine(knockbackVelocity, selectedDuration));
    }

    private Vector3 GetTestDirection()
    {
        if (usePlayerBackward)
            return -transform.forward;

        return customHorizontalDirection;
    }

    private IEnumerator KnockbackRoutine(Vector3 knockbackVelocity, float duration)
    {
        if (disableMovementDuringKnockback && foundMovementController != null && foundMovementController.enabled)
        {
            foundMovementController.enabled = false;
            movementDisabledByKnockback = true;
        }

        if (foundRigidbody != null)
            foundRigidbody.linearVelocity = knockbackVelocity;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        RestoreMovementController();

        knockbackRoutine = null;
    }

    private void RestoreMovementController()
    {
        if (!movementDisabledByKnockback)
            return;

        if (foundMovementController != null)
            foundMovementController.enabled = true;

        movementDisabledByKnockback = false;
    }

    private bool TryGetKnockbackRule(Transform hitTransform, out KnockbackRule rule)
    {
        rule = null;

        if (hitTransform == null || knockbacks == null)
            return false;

        for (int i = 0; i < knockbacks.Count; i++)
        {
            KnockbackRule candidate = knockbacks[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.triggeringTag))
                continue;

            if (HasTagInHierarchy(hitTransform, candidate.triggeringTag))
            {
                rule = candidate;
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
            if (current.CompareTag(tagToFind))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void EnsureDefaultKnockbacks()
    {
        if (knockbacks == null)
            knockbacks = new List<KnockbackRule>();

        if (knockbacks.Count > 0)
            return;

        knockbacks.Add(new KnockbackRule());
    }
}
