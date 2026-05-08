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
    [Tooltip("If enabled, the player says this response, dialogue ends, and this NPC can no longer start normal dialogue this play session.")]
    [SerializeField] private bool finalChoice = false;

    public string ChoiceText => choiceText;
    public string PlayerResponseText => string.IsNullOrWhiteSpace(playerResponseText) ? choiceText : playerResponseText;
    public Dialogue_Node NextNode => nextNode;
    public bool EndsConversation => nextNode == null;
    public bool Active => active;
    public bool DeactivateAfterChosen => deactivateAfterChosen;
    public bool FinalChoice => finalChoice;

    public void SetActive(bool isActive)
    {
        active = isActive;
    }

    public void MarkChosen()
    {
        if (deactivateAfterChosen)
            active = false;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue Node")]
public class Dialogue_Node : MonoBehaviour
{
    [Header("Line")]
    [Tooltip("Name shown in the dialogue box. Leave empty to use this node's parent GameObject name.")]
    [SerializeField] private string speakerName;
    [TextArea(2, 6)]
    [SerializeField] private string dialogueText;

    [Header("Choices")]
    [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>();

    public string SpeakerName => string.IsNullOrWhiteSpace(speakerName)
        ? GetDefaultSpeakerName()
        : speakerName;
    public string DialogueText => dialogueText;
    public IReadOnlyList<DialogueChoice> Choices => choices;

    private string GetDefaultSpeakerName()
    {
        return transform.parent != null ? transform.parent.name : gameObject.name;
    }
}
