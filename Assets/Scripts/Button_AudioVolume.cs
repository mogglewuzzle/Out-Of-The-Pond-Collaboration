using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Button/Audio Volume")]
public class Button_AudioVolume : MonoBehaviour, IMoveHandler
{
    private enum VolumeType
    {
        Music,
        Sfx
    }

    [SerializeField] private VolumeType volumeType = VolumeType.Music;
    [Tooltip("Slider controlled by this button. If left empty, the first child Slider is used.")]
    [SerializeField] private Slider volumeSlider;
    [Tooltip("Optional. If left empty, this button auto-finds Systems_AudioSettings in the scene.")]
    [SerializeField] private Systems_AudioSettings audioSettings;
    [SerializeField] private float controllerStep = 0.05f;

    private void Awake()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponentInChildren<Slider>(true);

        FindAudioSettings();
    }

    private void OnEnable()
    {
        FindAudioSettings();
        SyncSliderValue();

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }

    public void OnMove(AxisEventData eventData)
    {
        if (volumeSlider == null)
            return;

        if (eventData.moveDir == MoveDirection.Left)
        {
            AdjustVolume(-controllerStep);
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Right)
        {
            AdjustVolume(controllerStep);
            eventData.Use();
        }
    }

    private void AdjustVolume(float amount)
    {
        float newValue = Mathf.Clamp(volumeSlider.value + amount, volumeSlider.minValue, volumeSlider.maxValue);
        volumeSlider.value = newValue;
    }

    private void SetVolume(float volume)
    {
        FindAudioSettings();

        if (audioSettings == null)
        {
            Debug.LogWarning("Audio volume button could not find Systems_AudioSettings.", this);
            return;
        }

        if (volumeType == VolumeType.Music)
            audioSettings.SetMusicVolume(volume);
        else
            audioSettings.SetSfxVolume(volume);
    }

    private void SyncSliderValue()
    {
        if (volumeSlider == null || audioSettings == null)
            return;

        float volume = volumeType == VolumeType.Music
            ? audioSettings.MusicVolume
            : audioSettings.SfxVolume;

        volumeSlider.SetValueWithoutNotify(volume);
    }

    private void FindAudioSettings()
    {
        if (audioSettings != null)
            return;

        audioSettings = Systems_AudioSettings.Instance;

        if (audioSettings == null)
            audioSettings = FindFirstObjectByType<Systems_AudioSettings>();
    }
}
