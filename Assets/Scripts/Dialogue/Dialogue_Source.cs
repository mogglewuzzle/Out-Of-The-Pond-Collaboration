using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Source")]
public class Dialogue_Source : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueCharacter;
    [SerializeField] private Dialogue_Node startingNode;

    [Header("End Dialogue")]
    [Tooltip("If enabled, the final NPC line shows this button after typing finishes. If disabled, dialogue closes automatically after the final line finishes typing.")]
    [SerializeField] private bool showEndDialogueButton = true;
    [Tooltip("Button text shown on the final NPC line when Show End Dialogue Button is enabled.")]
    [SerializeField] private string endDialogueButtonText = "Goodbye";

    public GameObject DialogueCharacter => dialogueCharacter != null ? dialogueCharacter : gameObject;
    public Dialogue_Node StartingNode => startingNode;
    public bool ShowEndDialogueButton => showEndDialogueButton;
    public string EndDialogueButtonText => endDialogueButtonText;

    public bool BelongsTo(GameObject target)
    {
        if (target == null)
            return false;

        GameObject owner = DialogueCharacter;
        return target == owner || target.transform.IsChildOf(owner.transform);
    }

    public void StartDialogue()
    {
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

        manager.StartDialogue(startingNode, DialogueCharacter, showEndDialogueButton, endDialogueButtonText);
    }
}
