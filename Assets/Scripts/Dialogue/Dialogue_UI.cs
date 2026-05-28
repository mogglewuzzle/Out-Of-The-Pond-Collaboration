using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Dialogue/Dialogue UI")]
public class Dialogue_UI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Panels")]
    [SerializeField] private GameObject npcDialoguePanel;
    [SerializeField] private GameObject playerDialoguePanel;

    [Header("NPC Text")]
    [SerializeField] private TMP_Text npcSpeakerNameText;
    [SerializeField] private TMP_Text npcDialogueBodyText;

    [Header("Player Text")]
    [SerializeField] private TMP_Text playerSpeakerNameText;
    [SerializeField] private TMP_Text playerDialogueBodyText;

    [Header("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;
    [Tooltip("Realtime delay after choices appear before they can be selected or clicked.")]
    [SerializeField] private float choiceInputDelay = 0.25f;

    [Header("Player Continue")]
    [Tooltip("If enabled, the player response waits for this button after typing finishes. If disabled, Interact continues after typing.")]
    [SerializeField] private bool usePlayerContinueButton;
    [Tooltip("Optional scene button used to continue after player response text. Only used when Use Player Continue Button is enabled.")]
    [SerializeField] private Button playerContinueButton;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [Tooltip("Realtime delay after the panel appears and before each dialogue line starts typing.")]
    [SerializeField] private float typeStartDelay = 0.25f;
    [Tooltip("Characters shown per second while dialogue text is typing.")]
    [SerializeField] private float charactersPerSecond = 45f;
    [Tooltip("Multiplier used when the player holds or presses Interact to speed up typing.")]
    [SerializeField] private float interactSpeedMultiplier = 4f;
    [Tooltip("When enabled, pressing Interact while text is typing makes the text type faster instead of submitting a choice.")]
    [SerializeField] private bool allowInteractToSpeedUpTyping = true;

    private Coroutine typeRoutine;
    private bool typing;
    private bool speedUpTyping;
    private Action pendingAfterTyping;
    private Action pendingPlayerLineContinue;
    private UnityAction playerContinueButtonAction;
    private Coroutine choiceInputDelayRoutine;
    private bool choiceInputBlocked;

    public bool IsTyping => typing;
    public bool AllowInteractToSpeedUpTyping => allowInteractToSpeedUpTyping;
    public bool CanContinuePlayerLineWithInteract =>
        pendingPlayerLineContinue != null &&
        (!usePlayerContinueButton || playerContinueButton == null) &&
        !typing;

    private void Awake()
    {
        Hide();
    }

    public void ShowNode(
        Dialogue_Node node,
        string speakerName,
        Action<DialogueChoice> onChoiceSelected,
        bool showEndButton,
        string endButtonText,
        Action onEndDialogue)
    {
        if (node == null)
        {
            Hide();
            return;
        }

        if (root != null)
            root.SetActive(true);

        HidePlayerContinueButton();
        SetPanelState(showNpcPanel: true, showPlayerPanel: false);

        if (npcSpeakerNameText != null)
            npcSpeakerNameText.text = speakerName;

        ClearChoices();
        ShowText(npcDialogueBodyText, node.DialogueText, () =>
            RebuildChoices(node, onChoiceSelected, showEndButton, endButtonText, onEndDialogue));
    }

    public void ShowLine(string speakerName, string dialogueText, Action afterLine)
    {
        if (root != null)
            root.SetActive(true);

        SetPanelState(showNpcPanel: false, showPlayerPanel: true);

        if (playerSpeakerNameText != null)
            playerSpeakerNameText.text = speakerName;

        ClearChoices();
        HidePlayerContinueButton();
        ShowText(playerDialogueBodyText, dialogueText, () =>
        {
            pendingPlayerLineContinue = afterLine;

            if (usePlayerContinueButton)
                ShowPlayerContinueButton();
        });
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        SetPanelState(showNpcPanel: false, showPlayerPanel: false);
        ClearChoices();
        HidePlayerContinueButton();
        StopTyping();
        StopChoiceInputDelay();
    }

    public void ContinuePlayerLineFromInteract()
    {
        if (!CanContinuePlayerLineWithInteract)
            return;

        ContinuePlayerLine();
    }

    public void SubmitSelectedButton()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        Button selectedButton = selectedObject.GetComponent<Button>();
        if (selectedButton == null || !selectedButton.IsActive() || !selectedButton.interactable)
            return;

        if (choiceInputBlocked && selectedButton.transform.parent == choiceContainer)
            return;

        selectedButton.onClick.Invoke();
    }

    public void SpeedUpTypingOnce()
    {
        if (!typing || !allowInteractToSpeedUpTyping)
            return;

        speedUpTyping = true;
    }

    private void RebuildChoices(
        Dialogue_Node node,
        Action<DialogueChoice> onChoiceSelected,
        bool showEndButton,
        string endButtonText,
        Action onEndDialogue)
    {
        ClearChoices();

        if (!HasVisibleChoices(node))
        {
            if (!showEndButton)
            {
                onEndDialogue?.Invoke();
                return;
            }

            if (choiceContainer == null || choiceButtonPrefab == null)
                return;

            Button endButton = CreateButton(endButtonText, onEndDialogue);
            BeginChoiceInputDelay(endButton);
            return;
        }

        if (choiceContainer == null || choiceButtonPrefab == null)
            return;

        Button firstButton;

        firstButton = null;
        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoice choice = node.Choices[i];
            if (choice == null || !choice.Active)
                continue;

            Button button = CreateChoiceButton(choice.ChoiceText, choice, onChoiceSelected);

            if (firstButton == null)
                firstButton = button;
        }

        BeginChoiceInputDelay(firstButton);
    }

    private bool HasVisibleChoices(Dialogue_Node node)
    {
        if (node.Choices == null || node.Choices.Count == 0)
            return false;

        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoice choice = node.Choices[i];
            if (choice != null && choice.Active)
                return true;
        }

        return false;
    }

    private Button CreateChoiceButton(string label, DialogueChoice choice, Action<DialogueChoice> onChoiceSelected)
    {
        return CreateButton(label, () => onChoiceSelected?.Invoke(choice));
    }

    private Button CreateButton(string label, Action onClick)
    {
        Button button = Instantiate(choiceButtonPrefab, choiceContainer);
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
            buttonText.text = label;

        button.onClick.AddListener(() =>
        {
            if (choiceInputBlocked && button.transform.parent == choiceContainer)
                return;

            onClick?.Invoke();
        });
        return button;
    }

    private void BeginChoiceInputDelay(Button firstButton)
    {
        StopChoiceInputDelay();

        choiceInputBlocked = true;
        SelectButton(firstButton);

        if (choiceInputDelay <= 0f)
        {
            choiceInputBlocked = false;
            return;
        }

        choiceInputDelayRoutine = StartCoroutine(EnableChoiceInputAfterDelay());
    }

    private IEnumerator EnableChoiceInputAfterDelay()
    {
        yield return new WaitForSecondsRealtime(choiceInputDelay);

        choiceInputBlocked = false;
        choiceInputDelayRoutine = null;
    }

    private void StopChoiceInputDelay()
    {
        if (choiceInputDelayRoutine == null)
            return;

        StopCoroutine(choiceInputDelayRoutine);
        choiceInputDelayRoutine = null;
        choiceInputBlocked = false;
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
        button.Select();
    }

    private void SetPanelState(bool showNpcPanel, bool showPlayerPanel)
    {
        if (npcDialoguePanel != null)
            npcDialoguePanel.SetActive(showNpcPanel);

        if (playerDialoguePanel != null)
            playerDialoguePanel.SetActive(showPlayerPanel);
    }

    private void ClearChoices()
    {
        StopChoiceInputDelay();

        if (choiceContainer == null)
            return;

        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
            Destroy(choiceContainer.GetChild(i).gameObject);
    }

    private void ShowPlayerContinueButton()
    {
        if (playerContinueButton == null)
            return;

        playerContinueButton.gameObject.SetActive(true);

        if (playerContinueButtonAction != null)
            playerContinueButton.onClick.RemoveListener(playerContinueButtonAction);

        playerContinueButtonAction = ContinuePlayerLine;
        playerContinueButton.onClick.AddListener(playerContinueButtonAction);
        SelectButton(playerContinueButton);
    }

    private void HidePlayerContinueButton()
    {
        if (playerContinueButton != null)
        {
            if (playerContinueButtonAction != null)
                playerContinueButton.onClick.RemoveListener(playerContinueButtonAction);

            playerContinueButton.gameObject.SetActive(false);
        }

        playerContinueButtonAction = null;
    }

    private void ContinuePlayerLine()
    {
        Action continueAction = pendingPlayerLineContinue;
        pendingPlayerLineContinue = null;
        HidePlayerContinueButton();
        continueAction?.Invoke();
    }

    private void ShowText(TMP_Text targetText, string text, Action afterTyping)
    {
        StopTyping();
        pendingAfterTyping = afterTyping;

        if (targetText == null)
        {
            CompleteTyping();
            return;
        }

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            targetText.text = text;
            CompleteTyping();
            return;
        }

        typeRoutine = StartCoroutine(TypeText(targetText, text));
    }

    private IEnumerator TypeText(TMP_Text targetText, string text)
    {
        typing = true;
        speedUpTyping = false;
        targetText.text = string.Empty;
        if (Audio_OtherEffects.Instance != null)
            Audio_OtherEffects.Instance.BeginDialogueTyping(speedUpTyping);

        if (typeStartDelay > 0f)
            yield return new WaitForSecondsRealtime(typeStartDelay);

        float characterDelay = 1f / charactersPerSecond;

        for (int i = 0; i < text.Length; i++)
        {
            targetText.text = text.Substring(0, i + 1);

            float multiplier = speedUpTyping ? interactSpeedMultiplier : 1f;
            if (Audio_OtherEffects.Instance != null)
                Audio_OtherEffects.Instance.SetDialogueTypingSpeedUp(speedUpTyping);

            float delay = characterDelay / Mathf.Max(1f, multiplier);
            yield return new WaitForSecondsRealtime(delay);
        }

        CompleteTyping();
    }

    private void StopTyping()
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        typing = false;
        speedUpTyping = false;
        pendingAfterTyping = null;
        pendingPlayerLineContinue = null;
        if (Audio_OtherEffects.Instance != null)
            Audio_OtherEffects.Instance.StopDialogueTyping();
    }

    private void CompleteTyping()
    {
        typing = false;
        speedUpTyping = false;
        typeRoutine = null;
        if (Audio_OtherEffects.Instance != null)
            Audio_OtherEffects.Instance.StopDialogueTyping();

        Action afterTyping = pendingAfterTyping;
        pendingAfterTyping = null;
        afterTyping?.Invoke();
    }
}
