using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[AddComponentMenu("Systems/Audio Settings")]
public class Systems_AudioSettings : MonoBehaviour
{
    public static Systems_AudioSettings Instance { get; private set; }

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";

    [Header("Mixer")]
    [Tooltip("Optional. Assign an AudioMixer if you want volume changes applied through exposed mixer parameters.")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("Name of the exposed AudioMixer parameter used for music volume.")]
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [Tooltip("Name of the exposed AudioMixer parameter used for SFX/audio volume.")]
    [SerializeField] private string sfxVolumeParameter = "SfxVolume";

    [Header("Defaults")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float defaultSfxVolume = 1f;
    [SerializeField] private bool persistAcrossScenes = true;

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    public void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
    }

    public void ApplyAllVolumes()
    {
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void ApplyMusicVolume()
    {
        SetMixerVolume(musicVolumeParameter, MusicVolume);
    }

    private void ApplySfxVolume()
    {
        SetMixerVolume(sfxVolumeParameter, SfxVolume);
    }

    private void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        audioMixer.SetFloat(parameterName, NormalizedVolumeToDecibels(normalizedVolume));
    }

    private float NormalizedVolumeToDecibels(float normalizedVolume)
    {
        return normalizedVolume <= 0.0001f ? -80f : Mathf.Log10(normalizedVolume) * 20f;
    }
}
