using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Object/Consumable")]
public class Object_Consumable : MonoBehaviour
{
    [Header("Consume")]
    [SerializeField] private bool deactivateOnConsume = true;
    [Tooltip("Optional. If empty, this object is deactivated when consumed.")]
    [SerializeField] private GameObject objectToDeactivate;

    [Header("Events")]
    [SerializeField] private UnityEvent onConsumed;

    public void Consume(GameObject consumer)
    {
        Consume(consumer, null);
    }

    public void Consume(GameObject consumer, GameObject defaultObjectToDeactivate)
    {
        GameObject consumedObject = defaultObjectToDeactivate != null ? defaultObjectToDeactivate : gameObject;
        if (Systems_ConsumedItemTracker.Instance != null)
            Systems_ConsumedItemTracker.Instance.RecordConsumed(consumedObject);

        onConsumed?.Invoke();

        if (!deactivateOnConsume)
            return;

        GameObject targetObject = objectToDeactivate != null
            ? objectToDeactivate
            : consumedObject;

        targetObject.SetActive(false);
    }
}
