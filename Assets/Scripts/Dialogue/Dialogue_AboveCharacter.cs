using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Above Character")]
public class Dialogue_AboveCharacter : MonoBehaviour
{
    [Serializable]
    private class TaggedDialogueEntry
    {
        [SerializeField] private string triggerTag = "Player";
        [Tooltip("Collider used by this entry. It can be a trigger collider or a normal collider.")]
        [SerializeField] private Collider triggerCollider;
        [SerializeField] private string firstText;
        [SerializeField] private string secondText;
        [SerializeField] private string thirdText;

        private int nextTextIndex;

        public string TriggerTag => triggerTag;
        public Collider TriggerCollider => triggerCollider;

        public string GetNextText()
        {
            string selectedText;
            switch (nextTextIndex)
            {
                case 0:
                    selectedText = firstText;
                    break;
                case 1:
                    selectedText = secondText;
                    break;
                default:
                    selectedText = thirdText;
                    break;
            }

            nextTextIndex = (nextTextIndex + 1) % 3;
            return selectedText;
        }
    }

    [Header("Text")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private bool hideOnExit = false;

    [Header("Facing")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool keepUpright = true;
    [SerializeField] private Camera targetCamera;

    [Header("Triggers")]
    [SerializeField] private bool checkParentTags = true;
    [SerializeField] private List<TaggedDialogueEntry> taggedDialogueEntries = new List<TaggedDialogueEntry>();

    private readonly HashSet<string> activeTriggerContacts = new HashSet<string>();
    private Coroutine hideRoutine;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        RegisterTriggerRelays();
        HideDialogue();
    }

    private void LateUpdate()
    {
        UpdateFacing();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTriggerEnter(null, other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleTriggerExit(null, other);
    }

    public void HandleTriggerEnter(Collider entryTriggerCollider, Collider other)
    {
        GameObject triggeringObject = GetTriggeringObject(other);
        if (triggeringObject == null)
            return;

        string contactKey = GetContactKey(entryTriggerCollider, triggeringObject);
        if (!activeTriggerContacts.Add(contactKey))
            return;

        TaggedDialogueEntry entry = GetEntryForObject(entryTriggerCollider, other);
        if (entry == null)
            return;

        ShowDialogue(entry.GetNextText());
    }

    public void HandleTriggerExit(Collider entryTriggerCollider, Collider other)
    {
        GameObject triggeringObject = GetTriggeringObject(other);
        if (triggeringObject != null)
            activeTriggerContacts.Remove(GetContactKey(entryTriggerCollider, triggeringObject));

        if (hideOnExit)
            HideDialogue();
    }

    private TaggedDialogueEntry GetEntryForObject(Collider entryTriggerCollider, Collider other)
    {
        for (int i = 0; i < taggedDialogueEntries.Count; i++)
        {
            TaggedDialogueEntry entry = taggedDialogueEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.TriggerTag))
                continue;

            if (entry.TriggerCollider != null && entryTriggerCollider != entry.TriggerCollider)
                continue;

            if (ObjectHasTag(other, entry.TriggerTag.Trim()))
                return entry;
        }

        return null;
    }

    private bool ObjectHasTag(Collider other, string targetTag)
    {
        if (other.gameObject.tag == targetTag)
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject.tag == targetTag)
            return true;

        if (!checkParentTags)
            return false;

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.gameObject.tag == targetTag)
                return true;

            current = current.parent;
        }

        return false;
    }

    private GameObject GetTriggeringObject(Collider other)
    {
        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.gameObject;

        return other.gameObject;
    }

    private string GetContactKey(Collider entryTriggerCollider, GameObject triggeringObject)
    {
        int triggerId = entryTriggerCollider != null ? entryTriggerCollider.GetInstanceID() : 0;
        return triggerId + ":" + triggeringObject.GetInstanceID();
    }

    private void RegisterTriggerRelays()
    {
        for (int i = 0; i < taggedDialogueEntries.Count; i++)
        {
            TaggedDialogueEntry entry = taggedDialogueEntries[i];
            if (entry == null || entry.TriggerCollider == null)
                continue;

            Dialogue_AboveCharacterTrigger relay = entry.TriggerCollider.GetComponent<Dialogue_AboveCharacterTrigger>();
            if (relay == null)
                relay = entry.TriggerCollider.gameObject.AddComponent<Dialogue_AboveCharacterTrigger>();

            relay.Assign(this, entry.TriggerCollider);
        }
    }

    private void ShowDialogue(string text)
    {
        StopHideRoutine();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = text;

        if (displayDuration > 0f)
            hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void UpdateFacing()
    {
        if (!faceCamera || dialogueRoot == null || !dialogueRoot.activeInHierarchy)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Transform rootTransform = dialogueRoot.transform;
        Vector3 toCamera = targetCamera.transform.position - rootTransform.position;

        if (keepUpright)
            toCamera.y = 0f;

        if (toCamera.sqrMagnitude <= 0.0001f)
            return;

        rootTransform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        hideRoutine = null;
        HideDialogue();
    }

    private void HideDialogue()
    {
        StopHideRoutine();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = string.Empty;
    }

    private void StopHideRoutine()
    {
        if (hideRoutine == null)
            return;

        StopCoroutine(hideRoutine);
        hideRoutine = null;
    }

    private void OnValidate()
    {
        displayDuration = Mathf.Max(0f, displayDuration);
    }

    private void OnDisable()
    {
        HideDialogue();
        activeTriggerContacts.Clear();
    }

    private void Reset()
    {
        dialogueText = GetComponentInChildren<TMP_Text>(true);

        if (dialogueText != null)
            dialogueRoot = dialogueText.gameObject;
    }
}
