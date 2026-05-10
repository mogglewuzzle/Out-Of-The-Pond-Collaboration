using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Source")]
public class Dialogue_Source : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueCharacter;
    [Tooltip("Name shown in the dialogue box for this speaker. Leave empty to use the dialogue character object's name.")]
    [SerializeField] private string speakerName;
    [SerializeField] private Dialogue_Node startingNode;

    [Header("End Dialogue")]
    [Tooltip("If enabled, the final NPC line shows this button after typing finishes. If disabled, dialogue closes automatically after the final line finishes typing.")]
    [SerializeField] private bool showEndDialogueButton = true;
    [Tooltip("Button text shown on the final NPC line when Show End Dialogue Button is enabled.")]
    [SerializeField] private string endDialogueButtonText = "Goodbye";

    private bool dialogueCompleted;

    public GameObject DialogueCharacter => dialogueCharacter != null ? dialogueCharacter : gameObject;
    public string SpeakerName => string.IsNullOrWhiteSpace(speakerName) ? DialogueCharacter.name : speakerName;
    public Dialogue_Node StartingNode => startingNode;
    public bool ShowEndDialogueButton => showEndDialogueButton;
    public string EndDialogueButtonText => endDialogueButtonText;
    public bool DialogueCompleted => dialogueCompleted;

    public bool BelongsTo(GameObject target)
    {
        if (target == null)
            return false;

        GameObject owner = DialogueCharacter;
        return target == owner || target.transform.IsChildOf(owner.transform);
    }

    public void StartDialogue()
    {
        if (dialogueCompleted)
        {
            ShowCompletedAboveCharacterDialogue();
            return;
        }

        if (startingNode == null)
        {
            Debug.LogWarning($"{name} has no starting dialogue node assigned.", this);
            return;
        }

        Dialogue_Manager manager = Dialogue_Manager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("No Dialogue_Manager exists in the scene.", this);
            return;
        }

        manager.StartDialogue(this, startingNode, DialogueCharacter, showEndDialogueButton, endDialogueButtonText);
    }

    public void MarkDialogueCompleted()
    {
        dialogueCompleted = true;
    }

    private void ShowCompletedAboveCharacterDialogue()
    {
        Dialogue_AboveCharacter aboveCharacter = DialogueCharacter.GetComponentInChildren<Dialogue_AboveCharacter>(true);
        if (aboveCharacter == null)
            aboveCharacter = GetComponentInChildren<Dialogue_AboveCharacter>(true);
        if (aboveCharacter == null)
            aboveCharacter = GetComponentInParent<Dialogue_AboveCharacter>(true);

        if (aboveCharacter != null)
            aboveCharacter.ShowCompletedDialogueEntry();
    }
}
