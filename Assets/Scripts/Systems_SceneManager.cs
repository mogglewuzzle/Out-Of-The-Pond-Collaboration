using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Systems/Scene Manager")]
public class Systems_SceneManager : MonoBehaviour
{
    public static Systems_SceneManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool fadeInOnSceneStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    private bool loadingScene;
    private Coroutine fadeRoutine;

    public bool IsLoadingScene => loadingScene;

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

        if (fadeCanvasGroup == null)
            fadeCanvasGroup = CreateFadeCanvas();
        else
            DontDestroyOnLoad(fadeCanvasGroup.transform.root.gameObject);
    }

    private void Start()
    {
        if (fadeInOnSceneStart)
            FadeIn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(Systems_SceneManager)} cannot load a scene with an empty name.", this);
            return;
        }

        if (!loadingScene)
            StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void LoadScene(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"{nameof(Systems_SceneManager)} cannot load scene build index {sceneBuildIndex}.", this);
            return;
        }

        if (!loadingScene)
            StartCoroutine(LoadSceneRoutine(sceneBuildIndex));
    }

    public void FadeIn()
    {
        StartFade(0f, fadeInDuration);
    }

    public void FadeOut()
    {
        StartFade(1f, fadeOutDuration);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        loadingScene = true;
        yield return FadeTo(1f, fadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
            yield return null;

        yield return FadeTo(0f, fadeInDuration);
        loadingScene = false;
    }

    private IEnumerator LoadSceneRoutine(int sceneBuildIndex)
    {
        loadingScene = true;
        yield return FadeTo(1f, fadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex);
        while (loadOperation != null && !loadOperation.isDone)
            yield return null;

        yield return FadeTo(0f, fadeInDuration);
        loadingScene = false;
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(targetAlpha, duration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = CreateFadeCanvas();

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

    private CanvasGroup CreateFadeCanvas()
    {
        GameObject canvasObject = new GameObject("Scene Fade Canvas");
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Fade Image");
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = fadeColor;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        CanvasGroup canvasGroup = imageObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = fadeInOnSceneStart ? 1f : 0f;
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0f;
        return canvasGroup;
    }
}
