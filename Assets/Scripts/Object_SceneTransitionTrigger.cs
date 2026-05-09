using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Object/Scene Transition Trigger")]
public class Object_SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Scene name to load. The scene must be added to Build Settings.")]
    [SerializeField] private string sceneName;
    [Tooltip("If enabled, Scene Build Index is used instead of Scene Name.")]
    [SerializeField] private bool useSceneBuildIndex;
    [SerializeField] private int sceneBuildIndex;

    [Header("Collider Filter")]
    [Tooltip("Only objects with this tag can trigger the scene switch.")]
    [SerializeField] private string requiredTag = "Player";
    [Tooltip("If enabled, this component reacts to trigger colliders.")]
    [SerializeField] private bool useTriggerEnter = true;
    [Tooltip("If enabled, this component reacts to non-trigger collisions.")]
    [SerializeField] private bool useCollisionEnter = true;

    private bool sceneSwitchRequested;

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerEnter)
            return;

        TrySwitchScene(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useCollisionEnter || collision == null)
            return;

        TrySwitchScene(collision.gameObject);
    }

    private void TrySwitchScene(GameObject otherObject)
    {
        if (sceneSwitchRequested || otherObject == null)
            return;

        if (!HasRequiredTag(otherObject))
            return;

        Systems_SceneManager sceneManager = Systems_SceneManager.Instance;
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<Systems_SceneManager>();
            if (sceneManager == null)
            {
                Debug.LogWarning($"{nameof(Object_SceneTransitionTrigger)} on {name} cannot switch scenes: no {nameof(Systems_SceneManager)} found.", this);
                return;
            }
        }

        sceneSwitchRequested = true;

        if (useSceneBuildIndex)
            sceneManager.LoadScene(sceneBuildIndex);
        else
            sceneManager.LoadScene(sceneName);
    }

    private bool HasRequiredTag(GameObject otherObject)
    {
        if (string.IsNullOrWhiteSpace(requiredTag))
            return true;

        Transform current = otherObject.transform;
        while (current != null)
        {
            if (current.CompareTag(requiredTag))
                return true;

            current = current.parent;
        }

        return false;
    }
}
