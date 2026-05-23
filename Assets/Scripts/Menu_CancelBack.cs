using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Menu/Cancel Back")]
public class Menu_CancelBack : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Usually InputSystem_Actions/UI/Cancel.")]
    [SerializeField] private InputActionReference cancelAction;

    [Header("Back Target")]
    [Tooltip("Optional. If assigned, Cancel invokes this button's On Click, so it uses the same transition as your Back button.")]
    [SerializeField] private Button backButton;

    [Tooltip("Used only if no Back Button is assigned.")]
    [SerializeField] private string fallbackSceneName;

    private bool backRequested;
    private bool enabledCancelAction;

    private void OnEnable()
    {
        InputAction action = cancelAction != null ? cancelAction.action : null;
        if (action == null)
            return;

        if (!action.enabled)
        {
            action.Enable();
            enabledCancelAction = true;
        }

        action.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        InputAction action = cancelAction != null ? cancelAction.action : null;
        if (action != null)
            action.performed -= OnCancelPerformed;

        if (enabledCancelAction && action != null)
            action.Disable();

        enabledCancelAction = false;
        backRequested = false;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        RequestBack();
    }

    public void RequestBack()
    {
        if (backRequested)
            return;

        if (backButton != null && backButton.gameObject.activeInHierarchy && backButton.interactable)
        {
            backRequested = true;
            backButton.onClick.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(fallbackSceneName))
            return;

        Systems_SceneManager sceneManager = Systems_SceneManager.Instance;
        if (sceneManager == null)
            sceneManager = FindFirstObjectByType<Systems_SceneManager>();

        if (sceneManager == null)
        {
            Debug.LogWarning($"{nameof(Menu_CancelBack)} on {name} cannot go back: no {nameof(Systems_SceneManager)} found.", this);
            return;
        }

        backRequested = true;
        sceneManager.LoadScene(fallbackSceneName);
    }
}
