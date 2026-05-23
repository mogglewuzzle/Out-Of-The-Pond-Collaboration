using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Object Dialogue Event")]
public class Object_DialogueEvent : MonoBehaviour
{
    [Header("Event")]
    [Tooltip("Unique ID used by dialogue choices to run this event. Example: start_apple_quest")]
    [SerializeField] private string eventId;

    [Header("Object State Changes")]
    [Tooltip("Objects to activate when this dialogue event runs.")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();
    [Tooltip("Objects to deactivate when this dialogue event runs.")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Unity Event")]
    [Tooltip("Optional extra actions to run after object state changes.")]
    [SerializeField] private float unityEventDelay;
    [SerializeField] private UnityEvent onEventRun;

    [Header("Scene Change")]
    [SerializeField] private bool changeScene;
    [Tooltip("Scene name to load through Systems_SceneManager. The scene must be in Build Settings.")]
    [SerializeField] private string sceneName;
    [SerializeField] private float sceneChangeDelay;

    private Coroutine delayedUnityEventRoutine;
    private Coroutine delayedSceneChangeRoutine;

    public string EventId => eventId;

    public static bool RunById(string eventId, UnityEngine.Object logContext = null)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        string normalizedEventId = eventId.Trim();
        Object_DialogueEvent[] dialogueEvents = FindObjectsByType<Object_DialogueEvent>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Object_DialogueEvent matchingEvent = null;
        int matchingCount = 0;

        for (int i = 0; i < dialogueEvents.Length; i++)
        {
            Object_DialogueEvent dialogueEvent = dialogueEvents[i];
            if (dialogueEvent == null || string.IsNullOrWhiteSpace(dialogueEvent.eventId))
                continue;

            if (!string.Equals(dialogueEvent.eventId.Trim(), normalizedEventId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (matchingEvent == null)
                matchingEvent = dialogueEvent;

            matchingCount++;
        }

        if (matchingEvent == null)
        {
            Debug.LogWarning($"No {nameof(Object_DialogueEvent)} found with event ID '{normalizedEventId}'.", logContext);
            return false;
        }

        if (matchingCount > 1)
        {
            Debug.LogWarning($"Found {matchingCount} {nameof(Object_DialogueEvent)} objects with event ID '{normalizedEventId}'. Event IDs should be unique per scene. Running the first match found.", matchingEvent);
        }

        matchingEvent.RunEvent();
        return true;
    }

    public void RunEvent()
    {
        SetObjectsActive(objectsToActivate, true);
        SetObjectsActive(objectsToDeactivate, false);

        if (delayedUnityEventRoutine != null)
            StopCoroutine(delayedUnityEventRoutine);

        if (unityEventDelay > 0f)
        {
            delayedUnityEventRoutine = StartCoroutine(RunUnityEventAfterDelay());
        }
        else
        {
            RunUnityEvent();
        }

        if (!changeScene)
            return;

        if (delayedSceneChangeRoutine != null)
            StopCoroutine(delayedSceneChangeRoutine);

        if (sceneChangeDelay > 0f)
            delayedSceneChangeRoutine = StartCoroutine(ChangeSceneAfterDelay());
        else
            ChangeScene();
    }

    private IEnumerator RunUnityEventAfterDelay()
    {
        yield return new WaitForSeconds(unityEventDelay);
        RunUnityEvent();
    }

    private void RunUnityEvent()
    {
        delayedUnityEventRoutine = null;
        onEventRun?.Invoke();
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(sceneChangeDelay);
        ChangeScene();
    }

    private void ChangeScene()
    {
        delayedSceneChangeRoutine = null;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(Object_DialogueEvent)} on {name} cannot change scene: Scene Name is empty.", this);
            return;
        }

        Systems_SceneManager sceneManager = Systems_SceneManager.Instance;
        if (sceneManager == null)
            sceneManager = FindFirstObjectByType<Systems_SceneManager>();

        if (sceneManager == null)
        {
            Debug.LogWarning($"{nameof(Object_DialogueEvent)} on {name} cannot change scene to '{sceneName}': no {nameof(Systems_SceneManager)} found.", this);
            return;
        }

        sceneManager.LoadScene(sceneName);
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

    private void OnValidate()
    {
        unityEventDelay = Mathf.Max(0f, unityEventDelay);
        sceneChangeDelay = Mathf.Max(0f, sceneChangeDelay);
    }
}
