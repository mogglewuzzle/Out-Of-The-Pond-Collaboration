using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class MouseClickSpawner : MonoBehaviour
{
    [Header("References")]
    public Camera cam;           // Assign in inspector (Main Camera)
    public GameObject prefabToSpawn; // Assign prefab to spawn
    public bool snapToNavMesh = true; // Optional: snap spawn to NavMesh
    public float navMeshSampleDistance = 1f; // Max distance to snap

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            Debug.LogError("No camera assigned or tagged MainCamera!");
        if (prefabToSpawn == null)
            Debug.LogError("No prefab assigned to spawn!");
    }

    void Update()
    {
        HandleMouseClick();
    }

    void HandleMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 spawnPosition = hit.point;

                // Optionally snap to NavMesh
                if (snapToNavMesh)
                {
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(hit.point, out navHit, navMeshSampleDistance, NavMesh.AllAreas))
                    {
                        spawnPosition = navHit.position;
                    }
                }

                // Instantiate the prefab
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            }
        }
    }
}