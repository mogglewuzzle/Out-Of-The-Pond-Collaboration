using UnityEngine;

public class ModeSwitcher : MonoBehaviour
{
    [Header("References")]
    public PlayerControllerWorld playerController;
    public CameraPivotController pivotController;

    public void SetTopDown()
    {
        if (playerController != null)
            playerController.mode = PlayerControllerWorld.ControlMode.TopDown;

        if (pivotController != null)
            pivotController.enabled = false;
    }

    public void SetThirdPerson()
    {
        if (playerController != null)
            playerController.mode = PlayerControllerWorld.ControlMode.ThirdPerson;

        if (pivotController != null)
            pivotController.enabled = true;
    }
}