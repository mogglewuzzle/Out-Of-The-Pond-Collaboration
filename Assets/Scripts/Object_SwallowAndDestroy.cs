using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Object/Swallow And Destroy")]
public class Object_SwallowAndDestroy : MonoBehaviour
{
    [Header("Consume")]
    [Tooltip("Objects with one of these tags are recorded as consumed and destroyed when they collide with this object.")]
    [SerializeField] private string[] consumedTags;
    [SerializeField] private bool destroyRootObject;

    [Header("Events")]
    [SerializeField] private UnityEvent onConsumedObjectDestroyed;

    private readonly HashSet<GameObject> consumedObjects = new HashSet<GameObject>();

    private void OnCollisionEnter(Collision collision)
    {
        TryConsumeAndDestroy(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryConsumeAndDestroy(other.gameObject);
    }

    private void TryConsumeAndDestroy(GameObject candidate)
    {
        if (candidate == null || !HasConsumedTag(candidate))
            return;

        GameObject objectToDestroy = GetObjectToDestroy(candidate);
        if (objectToDestroy == null || consumedObjects.Contains(objectToDestroy))
            return;

        consumedObjects.Add(objectToDestroy);

        if (Systems_ConsumedItemTracker.Instance != null)
            Systems_ConsumedItemTracker.Instance.RecordConsumed(candidate);

        onConsumedObjectDestroyed?.Invoke();
        Destroy(objectToDestroy);
    }

    private GameObject GetObjectToDestroy(GameObject candidate)
    {
        if (!destroyRootObject)
            return candidate;

        Transform root = candidate.transform.root;
        return root != null ? root.gameObject : candidate;
    }

    private bool HasConsumedTag(GameObject candidate)
    {
        if (consumedTags == null)
            return false;

        for (int i = 0; i < consumedTags.Length; i++)
        {
            string consumedTag = consumedTags[i];
            if (string.IsNullOrWhiteSpace(consumedTag))
                continue;

            if (HasTag(candidate, consumedTag.Trim()))
                return true;
        }

        return false;
    }

    private bool HasTag(GameObject candidate, string consumedTag)
    {
        try
        {
            return candidate.CompareTag(consumedTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(Object_SwallowAndDestroy)} on {name} cannot check undefined tag '{consumedTag}'.", this);
            return false;
        }
    }
}
