using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Button/Selected Visual Feedback")]
public class Button_SelectedVisualFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Button Scale")]
    [Tooltip("Scale multiplier applied while this button is selected.")]
    [SerializeField] private float selectedScaleMultiplier = 1.1f;
    [Tooltip("If enabled, the selected button scale moves up and down over time.")]
    [SerializeField] private bool pulseSelectedScale;
    [SerializeField] private float scalePulseAmount = 0.05f;
    [SerializeField] private float scalePulseSpeed = 4f;

    [Header("Text Color")]
    [Tooltip("If enabled, child text color changes while this button is selected.")]
    [SerializeField] private bool changeTextColorOnSelected = true;
    [SerializeField] private Color selectedTextColor = Color.yellow;

    [Header("Text Size")]
    [Tooltip("If enabled, child text size increases while this button is selected.")]
    [SerializeField] private bool increaseTextSizeOnSelected;
    [SerializeField] private float selectedTextSizeIncrease = 4f;
    [Tooltip("If enabled, the selected text size moves up and down over time.")]
    [SerializeField] private bool pulseSelectedTextSize;
    [SerializeField] private float textSizePulseAmount = 2f;
    [SerializeField] private float textSizePulseSpeed = 4f;

    [Header("References")]
    [Tooltip("Optional. If empty, the first TMP_Text child is used.")]
    [SerializeField] private TMP_Text tmpText;
    [Tooltip("Optional fallback for legacy UI Text.")]
    [SerializeField] private Text legacyText;

    private Vector3 originalScale;
    private Color originalTextColor;
    private float originalTextSize;
    private bool selected;

    private void Awake()
    {
        originalScale = transform.localScale;
        FindTextReferences();
        CacheOriginalTextValues();
    }

    private void OnEnable()
    {
        if (selected)
            ApplySelectedVisuals();
    }

    private void OnDisable()
    {
        selected = false;
        RestoreOriginalVisuals();
    }

    private void Update()
    {
        if (!selected)
            return;

        ApplySelectedVisuals();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        ApplySelectedVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        RestoreOriginalVisuals();
    }

    private void ApplySelectedVisuals()
    {
        float scalePulse = pulseSelectedScale
            ? Mathf.Sin(Time.unscaledTime * scalePulseSpeed) * scalePulseAmount
            : 0f;

        float scaleMultiplier = Mathf.Max(0f, selectedScaleMultiplier + scalePulse);
        transform.localScale = originalScale * scaleMultiplier;

        if (changeTextColorOnSelected)
            SetTextColor(selectedTextColor);

        float textSizeIncrease = increaseTextSizeOnSelected ? selectedTextSizeIncrease : 0f;
        float textPulse = pulseSelectedTextSize
            ? Mathf.Sin(Time.unscaledTime * textSizePulseSpeed) * textSizePulseAmount
            : 0f;

        SetTextSize(Mathf.Max(0f, originalTextSize + textSizeIncrease + textPulse));
    }

    private void RestoreOriginalVisuals()
    {
        transform.localScale = originalScale;
        SetTextColor(originalTextColor);
        SetTextSize(originalTextSize);
    }

    private void FindTextReferences()
    {
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        if (legacyText == null)
            legacyText = GetComponentInChildren<Text>(true);
    }

    private void CacheOriginalTextValues()
    {
        if (tmpText != null)
        {
            originalTextColor = tmpText.color;
            originalTextSize = tmpText.fontSize;
            return;
        }

        if (legacyText != null)
        {
            originalTextColor = legacyText.color;
            originalTextSize = legacyText.fontSize;
            return;
        }

        originalTextColor = Color.white;
        originalTextSize = 0f;
    }

    private void SetTextColor(Color color)
    {
        if (tmpText != null)
        {
            tmpText.color = color;
            return;
        }

        if (legacyText != null)
            legacyText.color = color;
    }

    private void SetTextSize(float size)
    {
        if (tmpText != null)
        {
            tmpText.fontSize = size;
            return;
        }

        if (legacyText != null)
            legacyText.fontSize = Mathf.RoundToInt(size);
    }
}
