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

    [Header("Scene Load")]
    [SerializeField] private bool loadSceneOnConsume;
    [Tooltip("Scene name to load when consumed. The scene must be added to Build Settings.")]
    [SerializeField] private string sceneName;
    [Tooltip("If enabled, Scene Build Index is used instead of Scene Name.")]
    [SerializeField] private bool useSceneBuildIndex;
    [SerializeField] private int sceneBuildIndex;

    [Header("Events")]
    [SerializeField] private UnityEvent onConsumed;

    private bool sceneLoadRequested;

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
        TryLoadSceneOnConsume();

        if (!deactivateOnConsume)
            return;

        GameObject targetObject = objectToDeactivate != null
            ? objectToDeactivate
            : consumedObject;

        targetObject.SetActive(false);
    }

    private void TryLoadSceneOnConsume()
    {
        if (!loadSceneOnConsume || sceneLoadRequested)
            return;

        Systems_SceneManager sceneManager = Systems_SceneManager.Instance;
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<Systems_SceneManager>();
            if (sceneManager == null)
            {
                Debug.LogWarning($"{nameof(Object_Consumable)} on {name} cannot load scene after consume: no {nameof(Systems_SceneManager)} found.", this);
                return;
            }
        }

        sceneLoadRequested = true;

        if (useSceneBuildIndex)
            sceneManager.LoadScene(sceneBuildIndex);
        else
            sceneManager.LoadScene(sceneName);
    }
}
