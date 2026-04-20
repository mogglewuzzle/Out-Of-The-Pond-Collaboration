using UnityEngine;

public class PlayerAimController : MonoBehaviour
{
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
    }
}
