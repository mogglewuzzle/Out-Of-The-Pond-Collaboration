using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Game Music")]
public class Audio_GameMusic : MonoBehaviour
{
    public static Audio_GameMusic Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Music")]
    [SerializeField] private AudioClip musicTrack;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = true;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Header("Ambient")]
    [SerializeField] private List<AudioClip> ambientTracks = new List<AudioClip>();
    [SerializeField] private bool playAmbientOnStart = true;
    [SerializeField] private bool loopAmbient = true;
    [SerializeField] private bool randomizeAmbientTrack;
    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 1f;

    [Header("Scene Behaviour")]
    [SerializeField] private bool persistAcrossGameScenes = true;
    [Tooltip("If assigned, music only keeps playing in these scene names. Leave empty to allow every scene where this manager exists.")]
    [SerializeField] private List<string> gameSceneNames = new List<string>();

    private Coroutine musicFadeRoutine;
    private bool wasLoadingScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSources();

        if (persistAcrossGameScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (!IsCurrentSceneAllowed())
        {
            Destroy(gameObject);
            return;
        }

        if (playMusicOnStart)
            PlayMusicWithFadeIn();

        if (playAmbientOnStart)
            PlayAmbient();
    }

    private void Update()
    {
        bool loadingScene = Systems_SceneManager.Instance != null && Systems_SceneManager.Instance.IsLoadingScene;
        if (loadingScene && !wasLoadingScene)
            FadeOutMusic();

        wasLoadingScene = loadingScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayMusicWithFadeIn()
    {
        if (musicSource == null || musicTrack == null)
            return;

        musicSource.clip = musicTrack;
        musicSource.loop = loopMusic;

        if (!musicSource.isPlaying)
            musicSource.Play();

        FadeMusicTo(musicVolume, fadeInDuration);
    }

    public void FadeOutMusic()
    {
        FadeMusicTo(0f, fadeOutDuration);
    }

    public void PlayAmbient()
    {
        if (ambientSource == null || ambientTracks == null || ambientTracks.Count == 0)
            return;

        AudioClip clip = GetAmbientClip();
        if (clip == null)
            return;

        ambientSource.clip = clip;
        ambientSource.loop = loopAmbient;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSceneAllowed(scene.name))
        {
            Destroy(gameObject);
            return;
        }

        if (playMusicOnStart)
            PlayMusicWithFadeIn();

        if (playAmbientOnStart && (ambientSource == null || !ambientSource.isPlaying))
            PlayAmbient();
    }

    private AudioClip GetAmbientClip()
    {
        if (ambientTracks.Count == 1 || !randomizeAmbientTrack)
            return ambientTracks[0];

        return ambientTracks[Random.Range(0, ambientTracks.Count)];
    }

    private void FadeMusicTo(float targetVolume, float duration)
    {
        if (musicSource == null)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicRoutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;

        if (duration <= 0f)
        {
            musicSource.volume = targetVolume;
            musicFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeRoutine = null;
    }

    private bool IsCurrentSceneAllowed()
    {
        return IsSceneAllowed(SceneManager.GetActiveScene().name);
    }

    private bool IsSceneAllowed(string sceneName)
    {
        if (gameSceneNames == null || gameSceneNames.Count == 0)
            return true;

        for (int i = 0; i < gameSceneNames.Count; i++)
        {
            if (gameSceneNames[i] == sceneName)
                return true;
        }

        return false;
    }

    private void EnsureSources()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.volume = 0f;
        ambientSource.playOnAwake = false;
    }
}
