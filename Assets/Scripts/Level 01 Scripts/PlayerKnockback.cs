using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerKnockbackWithDisable : MonoBehaviour
{
    [Header("Knockback Settings")]
    public string damagingTag = "Enemy";
    public float horizontalForce = 5f;
    public float verticalForce = 3f;
    public float cooldown = 1f;

    [Header("Components to disable during knockback")]
    public List<Behaviour> componentsToDisable = new List<Behaviour>();

    private CharacterController controller;
    private Rigidbody rb;
    private bool canBeKnockedBack = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleKnockback(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleKnockback(other.gameObject);
    }

    void HandleKnockback(GameObject other)
    {
        if (!canBeKnockedBack) return;
        if (!other.CompareTag(damagingTag)) return;

        StartCoroutine(KnockbackRoutine(other.transform.position));
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        canBeKnockedBack = false;

        // Disable CharacterController
        controller.enabled = false;

        // Disable listed components
        foreach (var comp in componentsToDisable)
        {
            if (comp != null)
                comp.enabled = false;
        }

        // Calculate direction and force
        Vector3 direction = (transform.position - sourcePosition).normalized;
        direction.y = 0f;
        Vector3 force = direction * horizontalForce + Vector3.up * verticalForce;

        // Reset velocity and apply force
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        // Wait for cooldown
        yield return new WaitForSeconds(cooldown);

        // Re-enable CharacterController
        controller.enabled = true;

        // Re-enable components
        foreach (var comp in componentsToDisable)
        {
            if (comp != null)
                comp.enabled = true;
        }

        canBeKnockedBack = true;
    }
}