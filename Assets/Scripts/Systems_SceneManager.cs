using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Systems/Scene Manager")]
public class Systems_SceneManager : MonoBehaviour
{
    public static Systems_SceneManager Instance { get; private set; }

    [Header("Fade")]
    [Tooltip("Required. Assign the CanvasGroup on your fullscreen black fade panel.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool fadeInOnSceneStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    private bool loadingScene;
    private Coroutine fadeRoutine;

    public bool IsLoadingScene => loadingScene;
    public string PendingSceneName { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(Systems_SceneManager)} instances found. Destroying duplicate on {name}.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup != null)
            DontDestroyOnLoad(fadeCanvasGroup.transform.root.gameObject);
        else
            Debug.LogWarning($"{nameof(Systems_SceneManager)} needs a fade CanvasGroup assigned. Scene transitions will load without fading.", this);
    }

    private void Start()
    {
        if (fadeInOnSceneStart)
        {
            SetFadeAlpha(1f);
            FadeIn();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, fadeOutDuration);
    }

    public void LoadScene(string sceneName, float fadeOutDurationOverride)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(Systems_SceneManager)} cannot load a scene with an empty name.", this);
            return;
        }

        if (!loadingScene)
            StartCoroutine(LoadSceneRoutine(sceneName, fadeOutDurationOverride));
    }

    public void LoadScene(int sceneBuildIndex)
    {
        LoadScene(sceneBuildIndex, fadeOutDuration);
    }

    public void LoadScene(int sceneBuildIndex, float fadeOutDurationOverride)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"{nameof(Systems_SceneManager)} cannot load scene build index {sceneBuildIndex}.", this);
            return;
        }

        if (!loadingScene)
            StartCoroutine(LoadSceneRoutine(sceneBuildIndex, fadeOutDurationOverride));
    }

    public void FadeIn()
    {
        StartFade(0f, fadeInDuration);
    }

    public void FadeOut()
    {
        StartFade(1f, fadeOutDuration);
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float transitionFadeOutDuration)
    {
        loadingScene = true;
        PendingSceneName = sceneName;
        yield return FadeTo(1f, transitionFadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
            yield return null;

        yield return FadeTo(0f, fadeInDuration);
        PendingSceneName = null;
        loadingScene = false;
    }

    private IEnumerator LoadSceneRoutine(int sceneBuildIndex, float transitionFadeOutDuration)
    {
        loadingScene = true;
        PendingSceneName = GetSceneNameFromBuildIndex(sceneBuildIndex);
        yield return FadeTo(1f, transitionFadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex);
        while (loadOperation != null && !loadOperation.isDone)
            yield return null;

        yield return FadeTo(0f, fadeInDuration);
        PendingSceneName = null;
        loadingScene = false;
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(targetAlpha, duration));
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = alpha > 0f;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning($"{nameof(Systems_SceneManager)} cannot fade because no fade CanvasGroup is assigned.", this);
            fadeRoutine = null;
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        float startAlpha = fadeCanvasGroup.alpha;
        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
        fadeRoutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private string GetSceneNameFromBuildIndex(int sceneBuildIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);
        return System.IO.Path.GetFileNameWithoutExtension(scenePath);
    }

}
