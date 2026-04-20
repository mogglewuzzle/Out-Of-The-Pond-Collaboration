using UnityEngine;

public class Player_ThrowObjectControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private PlayerInputHandler input;
    private Player_PickupControl pickupControl;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        pickupControl = GetComponent<Player_PickupControl>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (input == null || pickupControl == null || !input.ThrowObjectPressed)
            return;

        if (pickupControl.HeldObject == null)
            return;

        Object_Pickupable heldObject = pickupControl.HeldObject;
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 throwDirection = (forward + Vector3.up * heldObject.UpwardThrowBoost).normalized;
        Vector3 throwVelocity = throwDirection * heldObject.ThrowForce;

        pickupControl.TryThrowHeldObject(throwVelocity);
    }
}
