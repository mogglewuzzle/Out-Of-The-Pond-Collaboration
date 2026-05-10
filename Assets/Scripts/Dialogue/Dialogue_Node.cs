using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    [Tooltip("Short text shown on the choice button.")]
    [SerializeField] private string choiceText = "Continue";
    [Tooltip("Full sentence the player says after choosing this option.")]
    [TextArea(2, 4)]
    [SerializeField] private string playerResponseText;
    [Tooltip("The node that appears after the player's sentence.")]
    [SerializeField] private Dialogue_Node nextNode;
    [Tooltip("If disabled, this choice is hidden from the dialogue list.")]
    [SerializeField] private bool active = true;
    [Tooltip("If enabled, this choice becomes inactive after the player selects it.")]
    [SerializeField] private bool deactivateAfterChosen = false;
    [Tooltip("If enabled, the player says this response and then dialogue closes instead of moving to another node.")]
    [SerializeField] private bool goodbyeChoice = false;
    [Tooltip("If enabled, the player says this response, dialogue ends, and this NPC can no longer start normal dialogue this play session.")]
    [SerializeField] private bool finalChoice = false;

    [Tooltip("If enabled, this choice runs the Object_DialogueEvent with the matching Dialogue Event ID when selected.")]
    [SerializeField] private bool runDialogueEvent = false;
    [Tooltip("ID of the Object_DialogueEvent to run. Example: start_apple_quest")]
    [SerializeField] private string dialogueEventId;

    public string ChoiceText => choiceText;
    public string PlayerResponseText => string.IsNullOrWhiteSpace(playerResponseText) ? choiceText : playerResponseText;
    public Dialogue_Node NextNode => nextNode;
    public bool EndsConversation => goodbyeChoice || nextNode == null;
    public bool Active => active;
    public bool DeactivateAfterChosen => deactivateAfterChosen;
    public bool FinalChoice => finalChoice;
    public bool RunDialogueEvent => runDialogueEvent;
    public string DialogueEventId => dialogueEventId;

    public void SetActive(bool isActive)
    {
        active = isActive;
    }

    public void MarkChosen()
    {
        if (deactivateAfterChosen)
            active = false;
    }

    public void RunLinkedDialogueEvent(UnityEngine.Object logContext = null)
    {
        if (!runDialogueEvent)
            return;

        Object_DialogueEvent.RunById(dialogueEventId, logContext);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Node")]
public class Dialogue_Node : MonoBehaviour
{
    [Header("Line")]
    [TextArea(2, 6)]
    [SerializeField] private string dialogueText;

    [Header("Choices")]
    [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>();

    public string DialogueText => dialogueText;
    public IReadOnlyList<DialogueChoice> Choices => choices;
}
