using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[AddComponentMenu("Button/Screen Transition")]
public class Button_ScreenTransition : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Scene name to load. The scene must be added to Build Settings.")]
    [SerializeField] private string sceneName;
    [Tooltip("If enabled, Scene Build Index is used instead of Scene Name.")]
    [SerializeField] private bool useSceneBuildIndex;
    [SerializeField] private int sceneBuildIndex;

    private Button button;
    private bool sceneSwitchRequested;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(RequestSceneTransition);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(RequestSceneTransition);
    }

    public void RequestSceneTransition()
    {
        if (sceneSwitchRequested)
            return;

        Systems_SceneManager sceneManager = Systems_SceneManager.Instance;
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<Systems_SceneManager>();
            if (sceneManager == null)
            {
                Debug.LogWarning($"{nameof(Button_ScreenTransition)} on {name} cannot switch scenes: no {nameof(Systems_SceneManager)} found.", this);
                return;
            }
        }

        sceneSwitchRequested = true;

        if (useSceneBuildIndex)
            sceneManager.LoadScene(sceneBuildIndex);
        else
            sceneManager.LoadScene(sceneName);
    }
}
