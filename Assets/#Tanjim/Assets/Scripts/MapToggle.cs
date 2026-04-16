using UnityEngine;
using UnityEngine.InputSystem;

public class MapToggle : MonoBehaviour
{
    private bool mapOpen = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            mapOpen = !mapOpen;

            Debug.Log(mapOpen ? "Map Opened" : "Map Closed");
        }
    }
}