using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Button/Resume Game")]
public class Button_ResumeGame : MonoBehaviour
{
    [Tooltip("Optional. If left empty, this button auto-finds the first active Systems_InGameMenuController in the scene.")]
    [SerializeField] private Systems_InGameMenuController menuController;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        FindMenuController();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(ResumeGame);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(ResumeGame);
    }

    public void ResumeGame()
    {
        FindMenuController();

        if (menuController == null)
        {
            Debug.LogWarning("Resume button could not find a Systems_InGameMenuController.", this);
            return;
        }

        menuController.ResumeGame();
    }

    private void FindMenuController()
    {
        if (menuController != null)
            return;

        menuController = FindFirstObjectByType<Systems_InGameMenuController>();
    }
}
