using UnityEngine;
using Unity.Cinemachine;

public class Player_CameraStateController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera attractCamera;
    [SerializeField] private CinemachineCamera grabCamera;
    [SerializeField] private CinemachineCamera swingCamera;

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

    private PlayerAimController aimController;
    private PlayerFreeCameraController freeCameraController;
    private Player_TongueAttract tongueAttract;
    private Player_TongueGrab tongueGrab;
    private Player_TongueSwing tongueSwing;

    private float attractStartTime = -999f;
    private float grabStartTime = -999f;
    private float swingStartTime = -999f;

    private bool lastAttracting;
    private bool lastGrabbing;
    private bool lastSwinging;

    private void Awake()
    {
        aimController = GetComponent<PlayerAimController>();
        freeCameraController = GetComponent<PlayerFreeCameraController>();
        tongueAttract = GetComponent<Player_TongueAttract>();
        tongueGrab = GetComponent<Player_TongueGrab>();
        tongueSwing = GetComponent<Player_TongueSwing>();

        ApplyDefaults();
    }

    private void Update()
    {
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
