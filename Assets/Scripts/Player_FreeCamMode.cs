using UnityEngine;
using Unity.Cinemachine;

public class PlayerFreeCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [Tooltip("Your normal third-person follow camera.")]
    [SerializeField] private CinemachineCamera followCam;

    [Tooltip("A CinemachineCamera with Orbital Follow + Rotation Composer. " +
             "Set its Follow and Look At to the player. Leave it disabled in the scene.")]
    [SerializeField] private CinemachineCamera freeLookCam;

    private PlayerInputHandler _input;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();

        // Make sure it starts disabled
        if (freeLookCam != null)
            freeLookCam.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_input == null || freeLookCam == null) return;

        if (_input.FreeCameraHeld && !IsActive)
            Activate();
        else if (!_input.FreeCameraHeld && IsActive)
            Deactivate();
    }

    private void Activate()
    {
        IsActive = true;
        freeLookCam.gameObject.SetActive(true);
    }

    private void Deactivate()
    {
        IsActive = false;
        freeLookCam.gameObject.SetActive(false);
    }
}