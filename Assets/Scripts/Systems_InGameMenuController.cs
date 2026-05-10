using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

[DisallowMultipleComponent]
[AddComponentMenu("Systems/In Game Menu Controller")]
public class Systems_InGameMenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private bool hideMenuOnStart = true;
    [SerializeField] private float fadeDuration = 0.15f;

    [Header("Input")]
    [Tooltip("Input action used to open the in-game menu during gameplay.")]
    [SerializeField] private InputActionReference pauseAction;
    [Tooltip("Optional UI actions used to close the in-game menu while the UI action map is active. If empty, Pause Action is used for both opening and closing.")]
    [SerializeField] private InputActionReference[] uiUnpauseActions;
    [SerializeField] private string gameplayActionMapName = "Player";
    [Tooltip("Name of the UI action map in the Input Actions asset. This should usually stay as UI.")]
    [SerializeField] private string uiActionMapName = "UI";

    [Header("Gameplay Control")]
    [Tooltip("Optional. If left empty, the controller auto-finds the first active PlayerInputHandler in the scene and disables it while the menu is open.")]
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [Tooltip("Extra gameplay input/control scripts to disable while the menu is open.")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisable;

    [Header("Cursor")]
    [SerializeField] private bool showCursorWhileOpen = true;
    [SerializeField] private CursorLockMode gameplayCursorLockMode = CursorLockMode.Locked;
    [SerializeField] private bool gameplayCursorVisible = false;

    public bool IsOpen { get; private set; }

    private bool[] gameplayBehaviourPreviousStates;
    private InputActionMap gameplayActionMap;
    private InputActionMap uiActionMap;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private bool playerInputHandlerPreviousState;
    private Coroutine fadeRoutine;
    private int ignoreUiPauseFrame = -1;

    private void Awake()
    {
        if (playerInputHandler == null)
            playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (menuCanvasGroup == null && menuRoot != null)
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();

        CacheActionMaps();

        if (hideMenuOnStart && menuRoot != null)
        {
            SetMenuCanvasState(0f, false);
            menuRoot.SetActive(false);
        }

    }

    private void OnEnable()
    {
        EnablePauseAction();

        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.performed += OnPausePerformed;

        RegisterUiUnpauseActions();
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.performed -= OnPausePerformed;

        UnregisterUiUnpauseActions();

        if (IsOpen)
            CloseMenuImmediate();
    }

    private void OnDestroy()
    {
        if (IsOpen)
            CloseMenuImmediate();
    }

    public void ToggleMenu()
    {
        if (IsOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        ignoreUiPauseFrame = Time.frameCount;
        previousTimeScale = Time.timeScale;
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Time.timeScale = 0f;

        if (menuRoot != null)
            menuRoot.SetActive(true);

        StartMenuFade(1f, true, false);
        DisableGameplayInput();
        EnableUiInput();
        ApplyMenuCursorState();
        SelectEventSystemFirstSelected();
    }

    public void CloseMenu()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        ClearSelectedObject();
        StartMenuFade(0f, false, true);
        DisableUiInput();
        EnableGameplayInput();
        RestoreCursorState();
        Time.timeScale = previousTimeScale;
    }

    public void ResumeGame()
    {
        CloseMenu();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (UsesGameplayPauseForUnpause())
            ToggleMenu();
        else
            OpenMenu();
    }

    private void OnUiUnpausePerformed(InputAction.CallbackContext context)
    {
        if (Time.frameCount == ignoreUiPauseFrame)
            return;

        CloseMenu();
    }

    private bool UsesGameplayPauseForUnpause()
    {
        if (uiUnpauseActions == null || uiUnpauseActions.Length == 0)
            return true;

        InputAction gameplayPause = pauseAction != null ? pauseAction.action : null;

        for (int i = 0; i < uiUnpauseActions.Length; i++)
        {
            InputAction uiUnpause = uiUnpauseActions[i] != null ? uiUnpauseActions[i].action : null;

            if (uiUnpause != null && uiUnpause != gameplayPause)
                return false;
        }

        return true;
    }

    private void CloseMenuImmediate()
    {
        IsOpen = false;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        ClearSelectedObject();
        SetMenuCanvasState(0f, false);

        if (menuRoot != null)
            menuRoot.SetActive(false);

        DisableUiInput();
        EnableGameplayInput();
        RestoreCursorState();
        Time.timeScale = previousTimeScale;
    }

    private void CacheActionMaps()
    {
        InputAction pause = pauseAction != null ? pauseAction.action : null;
        InputActionAsset actionAsset = pause != null ? pause.actionMap?.asset : null;

        if (actionAsset == null)
            return;

        gameplayActionMap = actionAsset.FindActionMap(gameplayActionMapName, false);
        uiActionMap = actionAsset.FindActionMap(uiActionMapName, false);
    }

    private void EnablePauseAction()
    {
        InputAction action = pauseAction != null ? pauseAction.action : null;

        if (action != null && !action.enabled)
            action.Enable();
    }

    private void EnableUiUnpauseActions()
    {
        if (uiUnpauseActions == null)
            return;

        for (int i = 0; i < uiUnpauseActions.Length; i++)
        {
            InputAction action = uiUnpauseActions[i] != null ? uiUnpauseActions[i].action : null;

            if (action != null && !action.enabled)
                action.Enable();
        }
    }

    private void RegisterUiUnpauseActions()
    {
        if (UsesGameplayPauseForUnpause() || uiUnpauseActions == null)
            return;

        for (int i = 0; i < uiUnpauseActions.Length; i++)
        {
            InputAction action = uiUnpauseActions[i] != null ? uiUnpauseActions[i].action : null;

            if (action != null)
                action.performed += OnUiUnpausePerformed;
        }
    }

    private void UnregisterUiUnpauseActions()
    {
        if (UsesGameplayPauseForUnpause() || uiUnpauseActions == null)
            return;

        for (int i = 0; i < uiUnpauseActions.Length; i++)
        {
            InputAction action = uiUnpauseActions[i] != null ? uiUnpauseActions[i].action : null;

            if (action != null)
                action.performed -= OnUiUnpausePerformed;
        }
    }

    private void EnableUiInput()
    {
        if (gameplayActionMap != null)
            gameplayActionMap.Disable();

        if (uiActionMap != null)
            uiActionMap.Enable();

        if (UsesGameplayPauseForUnpause())
            EnablePauseAction();
        else
            EnableUiUnpauseActions();
    }

    private void DisableUiInput()
    {
        // UI input is shared by dialogue and menus, so this controller does not disable it on close.
    }

    private void DisableGameplayInput()
    {
        if (playerInputHandler != null)
        {
            playerInputHandlerPreviousState = playerInputHandler.enabled;
            playerInputHandler.enabled = false;
        }

        if (gameplayBehavioursToDisable == null)
            return;

        gameplayBehaviourPreviousStates = new bool[gameplayBehavioursToDisable.Length];

        for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
        {
            Behaviour behaviour = gameplayBehavioursToDisable[i];
            if (behaviour == null)
                continue;

            if (behaviour == playerInputHandler)
            {
                gameplayBehaviourPreviousStates[i] = playerInputHandlerPreviousState;
                continue;
            }

            gameplayBehaviourPreviousStates[i] = behaviour.enabled;
            behaviour.enabled = false;
        }
    }

    private void EnableGameplayInput()
    {
        if (playerInputHandler != null)
            playerInputHandler.enabled = playerInputHandlerPreviousState;

        if (gameplayBehavioursToDisable != null && gameplayBehaviourPreviousStates != null)
        {
            for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
            {
                Behaviour behaviour = gameplayBehavioursToDisable[i];
                if (behaviour == null)
                    continue;

                if (behaviour == playerInputHandler)
                    continue;

                behaviour.enabled = gameplayBehaviourPreviousStates[i];
            }
        }

        gameplayBehaviourPreviousStates = null;

        if (gameplayActionMap != null)
            gameplayActionMap.Enable();

        EnablePauseAction();
    }

    private void ApplyMenuCursorState()
    {
        if (!showCursorWhileOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreCursorState()
    {
        if (showCursorWhileOpen)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
            return;
        }

        Cursor.lockState = gameplayCursorLockMode;
        Cursor.visible = gameplayCursorVisible;
    }

    private void SelectEventSystemFirstSelected()
    {
        if (EventSystem.current == null)
            return;

        GameObject firstSelected = EventSystem.current.firstSelectedGameObject;

        EventSystem.current.SetSelectedGameObject(null);

        if (firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    private void ClearSelectedObject()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void StartMenuFade(float targetAlpha, bool interactableWhenFinished, bool hideWhenFinished)
    {
        if (menuCanvasGroup == null || fadeDuration <= 0f)
        {
            SetMenuCanvasState(targetAlpha, interactableWhenFinished);

            if (hideWhenFinished && menuRoot != null)
                menuRoot.SetActive(false);

            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
        fadeRoutine = StartCoroutine(FadeMenu(targetAlpha, interactableWhenFinished, hideWhenFinished));
    }

    private IEnumerator FadeMenu(float targetAlpha, bool interactableWhenFinished, bool hideWhenFinished)
    {
        float startAlpha = menuCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        SetMenuCanvasState(targetAlpha, interactableWhenFinished);

        if (hideWhenFinished && menuRoot != null)
            menuRoot.SetActive(false);

        fadeRoutine = null;
    }

    private void SetMenuCanvasState(float alpha, bool interactable)
    {
        if (menuCanvasGroup == null)
            return;

        menuCanvasGroup.alpha = alpha;
        menuCanvasGroup.interactable = interactable;
        menuCanvasGroup.blocksRaycasts = interactable;
    }
}
