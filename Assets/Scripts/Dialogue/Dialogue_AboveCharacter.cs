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
        [SerializeField] private bool randomise;
        [SerializeField] private string firstText;
        [SerializeField] private string secondText;
        [SerializeField] private string thirdText;

        private int nextTextIndex;

        public string TriggerTag => triggerTag;

        public string GetNextText()
        {
            if (randomise)
                return GetRandomText();

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

        private string GetRandomText()
        {
            int randomIndex = UnityEngine.Random.Range(0, 3);

            switch (randomIndex)
            {
                case 0:
                    return firstText;
                case 1:
                    return secondText;
                default:
                    return thirdText;
            }
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
    [Tooltip("Shared collider used for every dialogue entry. If empty, the first parent collider is used.")]
    [SerializeField] private Collider triggerCollider;

    [Header("Triggers")]
    [SerializeField] private bool checkParentTags = true;
    [SerializeField] private List<TaggedDialogueEntry> taggedDialogueEntries = new List<TaggedDialogueEntry>();

    [Header("Audio")]
    [SerializeField] private bool playTongueHitSound = true;
    [SerializeField] private string tongueTriggerTag = "Tongue";
    [SerializeField] private bool playObjectHitSound = true;
    [SerializeField] private string objectHitTriggerTag = "GrabPoint";

    [Header("Completed Dialogue")]
    [SerializeField] private bool randomiseCompletedDialogue;
    [SerializeField] private string completedFirstText;
    [SerializeField] private string completedSecondText;
    [SerializeField] private string completedThirdText;

    private readonly HashSet<string> activeTriggerContacts = new HashSet<string>();
    private Coroutine hideRoutine;
    private int nextCompletedTextIndex;

    private void Awake()
    {
        AssignMissingReferences();

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

        Collider contactTriggerCollider = GetContactTriggerCollider(entryTriggerCollider);
        string contactKey = GetContactKey(contactTriggerCollider, triggeringObject);
        if (!activeTriggerContacts.Add(contactKey))
            return;

        TaggedDialogueEntry entry = GetEntryForObject(other);
        if (entry == null)
            return;

        PlayHitSoundIfNeeded(other);
        ShowDialogue(entry.GetNextText());
    }

    public void HandleTriggerExit(Collider entryTriggerCollider, Collider other)
    {
        GameObject triggeringObject = GetTriggeringObject(other);
        if (triggeringObject != null)
            activeTriggerContacts.Remove(GetContactKey(GetContactTriggerCollider(entryTriggerCollider), triggeringObject));

        if (hideOnExit)
            HideDialogue();
    }

    public void ShowCompletedDialogueEntry()
    {
        ShowDialogue(GetNextCompletedText());
    }

    private TaggedDialogueEntry GetEntryForObject(Collider other)
    {
        for (int i = 0; i < taggedDialogueEntries.Count; i++)
        {
            TaggedDialogueEntry entry = taggedDialogueEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.TriggerTag))
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

    private void PlayHitSoundIfNeeded(Collider other)
    {
        if (Audio_OtherEffects.Instance == null)
            return;

        if (playTongueHitSound && ObjectHasAudioTag(other, tongueTriggerTag))
            Audio_OtherEffects.Instance.PlayTongueHitCharacter();

        if (playObjectHitSound && ObjectHasAudioTag(other, objectHitTriggerTag))
            Audio_OtherEffects.Instance.PlayObjectHitCharacter();
    }

    private bool ObjectHasAudioTag(Collider other, string targetTag)
    {
        if (string.IsNullOrWhiteSpace(targetTag))
            return false;

        return ObjectHasTag(other, targetTag.Trim());
    }

    private GameObject GetTriggeringObject(Collider other)
    {
        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.gameObject;

        return other.gameObject;
    }

    private Collider GetContactTriggerCollider(Collider entryTriggerCollider)
    {
        if (entryTriggerCollider != null)
            return entryTriggerCollider;

        return triggerCollider;
    }

    private string GetContactKey(Collider entryTriggerCollider, GameObject triggeringObject)
    {
        int triggerId = entryTriggerCollider != null ? entryTriggerCollider.GetInstanceID() : 0;
        return triggerId + ":" + triggeringObject.GetInstanceID();
    }

    private void RegisterTriggerRelays()
    {
        if (triggerCollider == null)
            return;

        Dialogue_AboveCharacterTrigger relay = triggerCollider.GetComponent<Dialogue_AboveCharacterTrigger>();
        if (relay == null)
            relay = triggerCollider.gameObject.AddComponent<Dialogue_AboveCharacterTrigger>();

        relay.Assign(this, triggerCollider);
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

    private string GetNextCompletedText()
    {
        if (randomiseCompletedDialogue)
            return GetRandomCompletedText();

        string selectedText;
        switch (nextCompletedTextIndex)
        {
            case 0:
                selectedText = completedFirstText;
                break;
            case 1:
                selectedText = completedSecondText;
                break;
            default:
                selectedText = completedThirdText;
                break;
        }

        nextCompletedTextIndex = (nextCompletedTextIndex + 1) % 3;
        return selectedText;
    }

    private string GetRandomCompletedText()
    {
        int randomIndex = UnityEngine.Random.Range(0, 3);

        switch (randomIndex)
        {
            case 0:
                return completedFirstText;
            case 1:
                return completedSecondText;
            default:
                return completedThirdText;
        }
    }

    private void UpdateFacing()
    {
        if (!faceCamera || dialogueRoot == null || !dialogueRoot.activeInHierarchy)
            return;

        if (targetCamera == null)
            targetCamera = FindMainCamera();

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
        AssignMissingReferences();
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

        AssignMissingReferences();
    }

    private void AssignMissingReferences()
    {
        if (targetCamera == null)
            targetCamera = FindMainCamera();

        if (triggerCollider == null)
            triggerCollider = FindFirstParentCollider();
    }

    private Camera FindMainCamera()
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
    }

    private Collider FindFirstParentCollider()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Collider parentCollider = current.GetComponent<Collider>();
            if (parentCollider != null)
                return parentCollider;

            current = current.parent;
        }

        return null;
    }
}
