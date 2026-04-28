using UnityEngine;

public class PlayerFreeCameraController : MonoBehaviour
{
    private PlayerInputHandler _input;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (_input == null) return;

        if (_input.FreeCameraHeld && !IsActive)
            Activate();
        else if (!_input.FreeCameraHeld && IsActive)
            Deactivate();
    }

    private void Activate()
    {
        IsActive = true;
    }

    private void Deactivate()
    {
        IsActive = false;
    }
}
