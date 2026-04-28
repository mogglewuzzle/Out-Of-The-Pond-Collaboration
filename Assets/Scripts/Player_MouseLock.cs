using UnityEngine;

public class Player_MouseLock : MonoBehaviour
{
    // Keeps mouse on screen when using Mause and keiboard
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
