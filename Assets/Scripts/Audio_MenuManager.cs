using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Menu Manager")]
public class Audio_MenuManager : MonoBehaviour
{
    public static Audio_MenuManager Instance { get; private set; }

    [Header("Menu Scenes")]
    [Tooltip("Scene names where this menu audio manager should continue playing. Leave empty to allow every scene.")]
    [SerializeField] private List<string> menuSceneNames = new List<string>();
    [SerializeField] private bool persistAcrossMenuScenes = true;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;

    [Header("Button Sounds")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonHoverClip;

    [Header("Music")]
    [SerializeField] private AudioClip musicTrack;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = true;

    [Header("Button Auto Hook")]
    [Tooltip("When enabled, all active buttons in menu scenes get click, hover, and gamepad selection sounds.")]
    [SerializeField] private bool automaticallyHookSceneButtons = true;
    [Tooltip("Prevents hover/select sounds caused by Unity automatically selecting a button as a menu scene loads.")]
    [SerializeField] private float hoverSoundDelayAfterSceneLoad = 0.25f;
    [Tooltip("When enabled, the first UI selection after a menu scene loads will not play a hover sound.")]
    [SerializeField] private bool ignoreFirstSelectionAfterSceneLoad = true;

    private float hoverSoundsAllowedTime;
    private bool shouldIgnoreNextSelectionSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureSources();

        if (persistAcrossMenuScenes)
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
            PlayMusic();

        ResetHoverSoundDelay();
        HookSceneButtons();
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

    public void PlayButtonClick()
    {
        PlayEffect(buttonClickClip);
    }

    public void PlayButtonHover()
    {
        if (!CanPlayHoverSound())
            return;

        PlayEffect(buttonHoverClip);
    }

    public void PlayButtonSelectionHover()
    {
        if (shouldIgnoreNextSelectionSound && ignoreFirstSelectionAfterSceneLoad)
        {
            shouldIgnoreNextSelectionSound = false;
            return;
        }

        shouldIgnoreNextSelectionSound = false;
        PlayButtonHover();
    }

    public void PlayMusic()
    {
        if (musicSource == null || musicTrack == null)
            return;

        if (musicSource.clip == musicTrack && musicSource.isPlaying)
            return;

        musicSource.clip = musicTrack;
        musicSource.loop = loopMusic;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    private void PlayEffect(AudioClip clip)
    {
        if (effectsSource == null || clip == null)
            return;

        effectsSource.PlayOneShot(clip);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSceneAllowed(scene.name))
        {
            StopMusic();
            Destroy(gameObject);
            return;
        }

        if (playMusicOnStart)
            PlayMusic();

        ResetHoverSoundDelay();
        HookSceneButtons();
    }

    private bool CanPlayHoverSound()
    {
        return Time.unscaledTime >= hoverSoundsAllowedTime;
    }

    private void ResetHoverSoundDelay()
    {
        hoverSoundsAllowedTime = Time.unscaledTime + Mathf.Max(0f, hoverSoundDelayAfterSceneLoad);
        shouldIgnoreNextSelectionSound = ignoreFirstSelectionAfterSceneLoad;
    }

    private void HookSceneButtons()
    {
        if (!automaticallyHookSceneButtons)
            return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Audio_MenuButtonSounds relay = buttons[i].GetComponent<Audio_MenuButtonSounds>();
            if (relay == null)
                relay = buttons[i].gameObject.AddComponent<Audio_MenuButtonSounds>();

            relay.SetManager(this);
            buttons[i].onClick.RemoveListener(PlayButtonClick);
            buttons[i].onClick.AddListener(PlayButtonClick);
        }
    }

    private bool IsCurrentSceneAllowed()
    {
        return IsSceneAllowed(SceneManager.GetActiveScene().name);
    }

    private bool IsSceneAllowed(string sceneName)
    {
        if (menuSceneNames == null || menuSceneNames.Count == 0)
            return true;

        for (int i = 0; i < menuSceneNames.Count; i++)
        {
            if (menuSceneNames[i] == sceneName)
                return true;
        }

        return false;
    }

    private void EnsureSources()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (effectsSource == null)
            effectsSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        effectsSource.playOnAwake = false;
    }
}

public class Audio_MenuButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Audio_MenuManager manager;

    public void SetManager(Audio_MenuManager newManager)
    {
        manager = newManager;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayPointerHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlaySelectionHover();
    }

    private void PlayPointerHover()
    {
        if (manager == null)
            manager = Audio_MenuManager.Instance;

        if (manager != null)
            manager.PlayButtonHover();
    }

    private void PlaySelectionHover()
    {
        if (manager == null)
            manager = Audio_MenuManager.Instance;

        if (manager != null)
            manager.PlayButtonSelectionHover();
    }
}
