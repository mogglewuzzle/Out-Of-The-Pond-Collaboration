using UnityEngine;

public class UI_CrosshairStateController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject aimCrosshairPanel;
    [SerializeField] private GameObject freeLookCrosshairPanel;

    [Header("References")]
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerFreeCameraController freeCameraController;
    [SerializeField] private PlayerTongueProjection tongueProjection;

    private void Awake()
    {
        if (aimController == null)
            aimController = FindFirstObjectByType<PlayerAimController>();

        if (freeCameraController == null)
            freeCameraController = FindFirstObjectByType<PlayerFreeCameraController>();

        if (tongueProjection == null)
            tongueProjection = FindFirstObjectByType<PlayerTongueProjection>();
    }

    private void Update()
    {
        bool freeCamActive = freeCameraController != null && freeCameraController.IsActive;
        bool tongueActive = tongueProjection != null && tongueProjection.IsTongueActive;
        bool isAiming = aimController != null && aimController.IsAiming;

        // 🔥 Highest priority: FreeCam disables everything
        if (freeCamActive)
        {
            SetPanels(false, false);
            return;
        }

        // 🕸 Tongue active disables everything
        if (tongueActive)
        {
            SetPanels(false, false);
            return;
        }

        // 🎯 Aiming
        if (isAiming)
        {
            SetPanels(true, false);
            return;
        }

        // 👁 Free look (default)
        SetPanels(false, true);
    }

    private void SetPanels(bool aimActive, bool freeLookActive)
    {
        if (aimCrosshairPanel != null && aimCrosshairPanel.activeSelf != aimActive)
            aimCrosshairPanel.SetActive(aimActive);

        if (freeLookCrosshairPanel != null && freeLookCrosshairPanel.activeSelf != freeLookActive)
            freeLookCrosshairPanel.SetActive(freeLookActive);
    }
}
