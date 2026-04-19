using UnityEngine;
using Unity.Cinemachine;

public class PlayerAimController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera normalVCam;
    [SerializeField] private CinemachineCamera aimVCam;
    [SerializeField] private GameObject crosshairPanel;

    private PlayerInputHandler input;
    public bool IsAiming { get; private set; }

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (IsAiming != input.AimHeld)
            SetAiming(input.AimHeld);
    }

    private void SetAiming(bool aiming)
    {
        IsAiming = aiming;

        aimVCam.gameObject.SetActive(aiming);
        normalVCam.gameObject.SetActive(!aiming);

        if (crosshairPanel != null)
            crosshairPanel.SetActive(aiming);
    }
}
