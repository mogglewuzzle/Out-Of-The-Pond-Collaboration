using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_CrosshairStateController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject aimCrosshairPanel;
    [SerializeField] private GameObject freeLookCrosshairPanel;

    [Header("Auto Find Panels")]
    [SerializeField] private bool autoFindPanels = true;
    [Tooltip("Searches this object's children for this GameObject name.")]
    [SerializeField] private string aimCrosshairPanelName = "Panel Crosshair Aim";
    [Tooltip("Searches this object's children for this GameObject name.")]
    [SerializeField] private string freeLookCrosshairPanelName = "Panel Crosshair Main";

    [Header("References")]
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerFreeCameraController freeCameraController;
    [SerializeField] private PlayerTongueProjection tongueProjection;

    [Header("Auto Find References")]
    [SerializeField] private bool autoFindPlayerReferences = true;
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private bool retryUntilFound = true;
    [Tooltip("Tag used to find the current player object.")]
    [SerializeField] private string playerTag = "Player";

    private bool hasLoggedMissingPlayer;
    private bool hasLoggedMissingPlayerComponents;

    private void Awake()
    {
        AutoAssignMissingReferences();
    }

    private void Update()
    {
        if (retryUntilFound && autoFindPlayerReferences && HasMissingPlayerReference())
            AutoAssignMissingReferences();

        bool freeCamActive = freeCameraController != null && freeCameraController.IsActive;
        bool tongueActive = tongueProjection != null && tongueProjection.IsTongueActive;
        bool isAiming = aimController != null && aimController.IsAiming;

        if (freeCamActive)
        {
            SetPanels(false, false);
            return;
        }

        if (tongueActive)
        {
            SetPanels(false, false);
            return;
        }

        if (isAiming)
        {
            SetPanels(true, false);
            return;
        }

        SetPanels(false, true);
    }

    private void SetPanels(bool aimActive, bool freeLookActive)
    {
        if (aimCrosshairPanel != null && aimCrosshairPanel.activeSelf != aimActive)
            aimCrosshairPanel.SetActive(aimActive);

        if (freeLookCrosshairPanel != null && freeLookCrosshairPanel.activeSelf != freeLookActive)
            freeLookCrosshairPanel.SetActive(freeLookActive);
    }

    [ContextMenu("Auto Assign Missing References")]
    private void AutoAssignMissingReferences()
    {
        if (autoFindPanels)
        {
            if (aimCrosshairPanel == null)
                aimCrosshairPanel = FindChildGameObjectByName(aimCrosshairPanelName);

            if (freeLookCrosshairPanel == null)
                freeLookCrosshairPanel = FindChildGameObjectByName(freeLookCrosshairPanelName);
        }

        if (autoFindPlayerReferences)
        {
            if (HasMissingPlayerReference())
                AutoAssignPlayerReferences();
        }
    }

    private GameObject FindChildGameObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        string requestedName = objectName.Trim();
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != transform && child.name == requestedName)
                return child.gameObject;
        }

        return null;
    }

    private void AutoAssignPlayerReferences()
    {
        GameObject playerObject = FindPlayerObjectByTag();
        if (playerObject == null)
            return;

        if (aimController == null)
            aimController = playerObject.GetComponentInChildren<PlayerAimController>(true);

        if (freeCameraController == null)
            freeCameraController = playerObject.GetComponentInChildren<PlayerFreeCameraController>(true);

        if (tongueProjection == null)
            tongueProjection = playerObject.GetComponentInChildren<PlayerTongueProjection>(true);

        if (HasMissingPlayerReference())
        {
            if (!hasLoggedMissingPlayerComponents)
            {
                Debug.LogWarning($"{nameof(UI_CrosshairStateController)} found player object '{playerObject.name}' but one or more required player components are missing.", this);
                hasLoggedMissingPlayerComponents = true;
            }

            return;
        }

        hasLoggedMissingPlayerComponents = false;
    }

    private GameObject FindPlayerObjectByTag()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            return null;

        string requestedTag = playerTag.Trim();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                GameObject match = FindGameObjectByTag(rootObject.transform, requestedTag);
                if (match != null)
                {
                    hasLoggedMissingPlayer = false;
                    return match;
                }
            }
        }

        if (!hasLoggedMissingPlayer)
        {
            Debug.LogWarning($"{nameof(UI_CrosshairStateController)} could not find a scene object tagged '{requestedTag}'.", this);
            hasLoggedMissingPlayer = true;
        }

        return null;
    }

    private GameObject FindGameObjectByTag(Transform parent, string targetTag)
    {
        if ((includeInactiveObjects || parent.gameObject.activeInHierarchy) && parent.gameObject.tag == targetTag)
            return parent.gameObject;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (!includeInactiveObjects && !child.gameObject.activeInHierarchy)
                continue;

            GameObject match = FindGameObjectByTag(child, targetTag);
            if (match != null)
                return match;
        }

        return null;
    }

    private bool HasMissingPlayerReference()
    {
        return aimController == null || freeCameraController == null || tongueProjection == null;
    }
}
