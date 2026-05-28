using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Other Effects")]
public class Audio_OtherEffects : MonoBehaviour
{
    public static Audio_OtherEffects Instance { get; private set; }

    [Header("Source")]
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource dialogueTypingSource;

    [Header("Dialogue")]
    [SerializeField] private AudioClip dialogueStartClip;
    [SerializeField] private AudioClip dialogueEndClip;
    [SerializeField] private AudioClip dialogueTypingClip;
    [SerializeField] private bool loopDialogueTyping = true;
    [SerializeField] private float dialogueTypingStartDelay = 0f;
    [SerializeField] private float normalTypingPitch = 1f;
    [SerializeField] private float spedUpTypingPitch = 1.5f;

    [Header("Hit Sounds")]
    [SerializeField] private AudioClip tongueHitCharacterClip;
    [SerializeField] private AudioClip objectHitCharacterClip;

    [Header("Lifetime")]
    [SerializeField] private bool persistAcrossScenes = true;

    private Coroutine dialogueTypingDelayRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSources();

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayDialogueStart()
    {
        PlayOneShot(dialogueStartClip);
    }

    public void PlayDialogueEnd()
    {
        PlayOneShot(dialogueEndClip);
        StopDialogueTyping();
    }

    public void BeginDialogueTyping(bool spedUp)
    {
        if (dialogueTypingSource == null || dialogueTypingClip == null)
            return;

        StopDialogueTypingDelay();
        dialogueTypingSource.clip = dialogueTypingClip;
        dialogueTypingSource.loop = loopDialogueTyping;
        SetDialogueTypingSpeedUp(spedUp);

        if (dialogueTypingStartDelay > 0f)
        {
            dialogueTypingDelayRoutine = StartCoroutine(BeginDialogueTypingAfterDelay());
            return;
        }

        if (!dialogueTypingSource.isPlaying)
            dialogueTypingSource.Play();
    }

    public void SetDialogueTypingSpeedUp(bool spedUp)
    {
        if (dialogueTypingSource == null)
            return;

        dialogueTypingSource.pitch = spedUp ? spedUpTypingPitch : normalTypingPitch;
    }

    public void StopDialogueTyping()
    {
        StopDialogueTypingDelay();

        if (dialogueTypingSource != null)
            dialogueTypingSource.Stop();
    }

    public void PlayTongueHitCharacter()
    {
        PlayOneShot(tongueHitCharacterClip);
    }

    public void PlayObjectHitCharacter()
    {
        PlayOneShot(objectHitCharacterClip);
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (effectsSource == null || clip == null)
            return;

        effectsSource.PlayOneShot(clip);
    }

    private void EnsureSources()
    {
        if (effectsSource == null)
            effectsSource = gameObject.AddComponent<AudioSource>();

        if (dialogueTypingSource == null)
            dialogueTypingSource = gameObject.AddComponent<AudioSource>();

        effectsSource.playOnAwake = false;
        dialogueTypingSource.playOnAwake = false;
    }

    private IEnumerator BeginDialogueTypingAfterDelay()
    {
        yield return new WaitForSecondsRealtime(dialogueTypingStartDelay);

        if (dialogueTypingSource != null && dialogueTypingSource.clip != null && !dialogueTypingSource.isPlaying)
            dialogueTypingSource.Play();

        dialogueTypingDelayRoutine = null;
    }

    private void StopDialogueTypingDelay()
    {
        if (dialogueTypingDelayRoutine == null)
            return;

        StopCoroutine(dialogueTypingDelayRoutine);
        dialogueTypingDelayRoutine = null;
    }
}
