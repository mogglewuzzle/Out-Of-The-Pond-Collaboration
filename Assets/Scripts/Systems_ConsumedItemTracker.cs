using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Systems/Consumed Item Tracker")]
public class Systems_ConsumedItemTracker : MonoBehaviour
{
    [System.Serializable]
    private class ConsumedItemCount
    {
        [SerializeField] private string itemTag;
        [SerializeField] private int consumedCount;

        public string ItemTag => itemTag;

        public ConsumedItemCount(string itemTag, int consumedCount)
        {
            this.itemTag = itemTag;
            this.consumedCount = consumedCount;
        }

        public void SetCount(int count)
        {
            consumedCount = count;
        }
    }

    [System.Serializable]
    private class ConsumedItemThreshold
    {
        [SerializeField] private string itemTag;
        [SerializeField] private int requiredCount = 1;
        [SerializeField] private bool onlyTriggerOnce = true;
        [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();
        [SerializeField] private List<GameObject> objectsToDeactivate = new List<GameObject>();

        private bool triggered;

        public string ItemTag => itemTag;

        public void TryTrigger(string consumedTag, int consumedCount)
        {
            if (string.IsNullOrWhiteSpace(itemTag) || string.IsNullOrWhiteSpace(consumedTag))
                return;

            if (onlyTriggerOnce && triggered)
                return;

            if (itemTag.Trim() != consumedTag.Trim())
                return;

            if (consumedCount < Mathf.Max(1, requiredCount))
                return;

            SetObjectsActive(objectsToActivate, true);
            SetObjectsActive(objectsToDeactivate, false);
            triggered = true;
        }

        public void ResetTrigger()
        {
            triggered = false;
        }

        private static void SetObjectsActive(List<GameObject> objects, bool activeState)
        {
            if (objects == null)
                return;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(activeState);
            }
        }
    }

    public static Systems_ConsumedItemTracker Instance { get; private set; }

    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private List<ConsumedItemThreshold> thresholds = new List<ConsumedItemThreshold>();
    [Header("Runtime Counts")]
    [Tooltip("Runtime display of consumed counts by tag. Updated when items are consumed.")]
    [SerializeField] private List<ConsumedItemCount> consumedCounts = new List<ConsumedItemCount>();

    private readonly Dictionary<string, int> consumedCountsByTag = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecordConsumed(GameObject consumedObject)
    {
        if (consumedObject == null)
            return;

        RecordConsumedTag(consumedObject.tag);
    }

    public void RecordConsumedTag(string itemTag)
    {
        if (string.IsNullOrWhiteSpace(itemTag))
            return;

        string normalizedTag = itemTag.Trim();

        if (!consumedCountsByTag.ContainsKey(normalizedTag))
            consumedCountsByTag[normalizedTag] = 0;

        consumedCountsByTag[normalizedTag]++;
        UpdateInspectorCount(normalizedTag, consumedCountsByTag[normalizedTag]);
        EvaluateThresholds(normalizedTag);
    }

    public int GetConsumedCount(string itemTag)
    {
        if (string.IsNullOrWhiteSpace(itemTag))
            return 0;

        return consumedCountsByTag.TryGetValue(itemTag.Trim(), out int count) ? count : 0;
    }

    public void ResetConsumedCount(string itemTag)
    {
        if (string.IsNullOrWhiteSpace(itemTag))
            return;

        consumedCountsByTag.Remove(itemTag.Trim());
        RemoveInspectorCount(itemTag.Trim());
        ResetThresholdsForTag(itemTag.Trim());
    }

    public void ResetAllConsumedCounts()
    {
        consumedCountsByTag.Clear();
        consumedCounts.Clear();
        ResetAllThresholds();
    }

    private void UpdateInspectorCount(string itemTag, int consumedCount)
    {
        for (int i = 0; i < consumedCounts.Count; i++)
        {
            if (consumedCounts[i] == null || string.IsNullOrWhiteSpace(consumedCounts[i].ItemTag))
                continue;

            if (consumedCounts[i].ItemTag.Trim() != itemTag)
                continue;

            consumedCounts[i].SetCount(consumedCount);
            return;
        }

        consumedCounts.Add(new ConsumedItemCount(itemTag, consumedCount));
    }

    private void RemoveInspectorCount(string itemTag)
    {
        for (int i = consumedCounts.Count - 1; i >= 0; i--)
        {
            if (consumedCounts[i] == null || string.IsNullOrWhiteSpace(consumedCounts[i].ItemTag))
                continue;

            if (consumedCounts[i].ItemTag.Trim() == itemTag)
                consumedCounts.RemoveAt(i);
        }
    }

    private void EvaluateThresholds(string itemTag)
    {
        int consumedCount = GetConsumedCount(itemTag);

        for (int i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i] != null)
                thresholds[i].TryTrigger(itemTag, consumedCount);
        }
    }

    private void ResetThresholdsForTag(string itemTag)
    {
        for (int i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i] == null || string.IsNullOrWhiteSpace(thresholds[i].ItemTag))
                continue;

            if (thresholds[i].ItemTag.Trim() == itemTag)
                thresholds[i].ResetTrigger();
        }
    }

    private void ResetAllThresholds()
    {
        for (int i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i] != null)
                thresholds[i].ResetTrigger();
        }
    }
}
