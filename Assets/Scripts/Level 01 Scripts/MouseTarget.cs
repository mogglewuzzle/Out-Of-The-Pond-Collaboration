using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MouseTarget : MonoBehaviour
{
    NavMeshAgent agent;

    void Start()
    {
         agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var ray = Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit info))
            {
                Debug.Log("Hit at world position " + info.point +
                    " on object " + info.collider.gameObject.name);

                agent.SetDestination(info.point);
            }
        }
    }
}