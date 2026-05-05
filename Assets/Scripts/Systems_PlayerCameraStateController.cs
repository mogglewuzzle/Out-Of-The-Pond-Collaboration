using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Systems_PlayerCameraStateController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera attractCamera;
    [SerializeField] private CinemachineCamera grabCamera;
    [SerializeField] private CinemachineCamera swingCamera;

    [Header("Player State References")]
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerFreeCameraController freeCameraController;
    [SerializeField] private Player_TongueAttract tongueAttract;
    [SerializeField] private Player_TongueGrab tongueGrab;
    [SerializeField] private Player_TongueSwing tongueSwing;

    [Header("Auto Reference Lookup")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private bool retryUntilFound = true;

    [Header("Default Priorities")]
    [SerializeField] private int normalDefaultPriority = 10;
    [SerializeField] private int aimDefaultPriority = 0;
    [SerializeField] private int freeLookDefaultPriority = 0;
    [SerializeField] private int attractDefaultPriority = 0;
    [SerializeField] private int grabDefaultPriority = 0;
    [SerializeField] private int swingDefaultPriority = 0;

    [Header("Active Priorities")]
    [SerializeField] private int aimActivePriority = 20;
    [SerializeField] private int freeLookActivePriority = 15;
    [SerializeField] private int attractActivePriority = 30;
    [SerializeField] private int grabActivePriority = 40;
    [SerializeField] private int swingActivePriority = 50;

    [Header("Optional Delays")]
    [SerializeField] private float attractCameraDelay = 0f;
    [SerializeField] private float grabCameraDelay = 0f;
    [SerializeField] private float swingCameraDelay = 0f;

    private GameObject currentPlayer;
    private float attractStartTime = -999f;
    private float grabStartTime = -999f;
    private float swingStartTime = -999f;

    private bool lastAttracting;
    private bool lastGrabbing;
    private bool lastSwinging;
    private bool hasLoggedMissingPlayer;
    private bool hasLoggedMissingPlayerComponents;

    private void Awake()
    {
        TryAssignPlayerReferences();
        ApplyDefaults();
    }

    private void Update()
    {
        if (retryUntilFound && MissingPlayerReferences())
            TryAssignPlayerReferences();

        bool isAttracting = tongueAttract != null && tongueAttract.IsAttracting;
        bool isGrabbing = tongueGrab != null && tongueGrab.IsGrabbing;
        bool isSwinging = tongueSwing != null && tongueSwing.IsSwinging;

        if (isAttracting && !lastAttracting)
            attractStartTime = Time.time;

        if (isGrabbing && !lastGrabbing)
            grabStartTime = Time.time;

        if (isSwinging && !lastSwinging)
            swingStartTime = Time.time;

        lastAttracting = isAttracting;
        lastGrabbing = isGrabbing;
        lastSwinging = isSwinging;

        ApplyDefaults();

        if (isSwinging && Time.time - swingStartTime >= swingCameraDelay)
        {
            SetPriority(swingCamera, swingActivePriority);
            return;
        }

        if (isGrabbing && Time.time - grabStartTime >= grabCameraDelay)
        {
            SetPriority(grabCamera, grabActivePriority);
            return;
        }

        if (isAttracting && Time.time - attractStartTime >= attractCameraDelay)
        {
            SetPriority(attractCamera, attractActivePriority);
            return;
        }

        bool isAiming = aimController != null && aimController.IsAiming;
        if (isAiming)
        {
            SetPriority(aimCamera, aimActivePriority);
            return;
        }

        bool isFreeLook = freeCameraController != null && freeCameraController.IsActive;
        if (isFreeLook)
        {
            SetPriority(freeLookCamera, freeLookActivePriority);
            return;
        }

        SetPriority(normalCamera, normalDefaultPriority);
    }

    [ContextMenu("Find And Assign Player References")]
    private void TryAssignPlayerReferences()
    {
        GameObject playerObject = FindPlayerObjectByTag();
        if (playerObject == null)
            return;

        currentPlayer = playerObject;
        aimController = currentPlayer.GetComponentInChildren<PlayerAimController>(true);
        freeCameraController = currentPlayer.GetComponentInChildren<PlayerFreeCameraController>(true);
        tongueAttract = currentPlayer.GetComponentInChildren<Player_TongueAttract>(true);
        tongueGrab = currentPlayer.GetComponentInChildren<Player_TongueGrab>(true);
        tongueSwing = currentPlayer.GetComponentInChildren<Player_TongueSwing>(true);

        if (MissingPlayerReferences())
        {
            if (!hasLoggedMissingPlayerComponents)
            {
                Debug.LogWarning($"{nameof(Systems_PlayerCameraStateController)} found player object '{currentPlayer.name}' but one or more required player camera state components are missing.", this);
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
            Debug.LogWarning($"{nameof(Systems_PlayerCameraStateController)} could not find a scene object tagged '{requestedTag}'.", this);
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

    private bool MissingPlayerReferences()
    {
        return currentPlayer == null ||
               aimController == null ||
               freeCameraController == null ||
               tongueAttract == null ||
               tongueGrab == null ||
               tongueSwing == null;
    }

    private void ApplyDefaults()
    {
        SetPriority(normalCamera, normalDefaultPriority);
        SetPriority(aimCamera, aimDefaultPriority);
        SetPriority(freeLookCamera, freeLookDefaultPriority);
        SetPriority(attractCamera, attractDefaultPriority);
        SetPriority(grabCamera, grabDefaultPriority);
        SetPriority(swingCamera, swingDefaultPriority);
    }

    private void SetPriority(CinemachineCamera cam, int priority)
    {
        if (cam != null)
            cam.Priority = priority;
    }
}
