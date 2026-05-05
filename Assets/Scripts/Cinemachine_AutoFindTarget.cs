using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CinemachineCamera))]
public class Cinemachine_AutoFindTarget : MonoBehaviour
{
    [Header("Target Search")]
    [SerializeField] private string targetName;
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private bool retryUntilFound = true;

    [Header("Cinemachine")]
    [SerializeField] private bool alsoSetLookAtTarget;

    private CinemachineCamera cinemachineCamera;
    private Transform currentTarget;
    private bool hasLoggedMissingTarget;

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        TryFindAndAssignTarget();
    }

    private void Update()
    {
        if (retryUntilFound && currentTarget == null)
            TryFindAndAssignTarget();
    }

    [ContextMenu("Find And Assign Target")]
    public void TryFindAndAssignTarget()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();

        if (string.IsNullOrWhiteSpace(targetName))
        {
            Debug.LogWarning($"{nameof(Cinemachine_AutoFindTarget)} on {name} has no target name set.", this);
            return;
        }

        Transform foundTarget = FindSceneObjectByName(targetName);
        if (foundTarget == null)
        {
            if (!hasLoggedMissingTarget)
            {
                Debug.LogWarning($"{nameof(Cinemachine_AutoFindTarget)} could not find a scene object named '{targetName}'.", this);
                hasLoggedMissingTarget = true;
            }

            return;
        }

        currentTarget = foundTarget;
        hasLoggedMissingTarget = false;
        cinemachineCamera.Follow = currentTarget;

        if (alsoSetLookAtTarget)
            cinemachineCamera.LookAt = currentTarget;
    }

    private Transform FindSceneObjectByName(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                Transform match = FindInChildren(rootObject.transform, objectName);
                if (match != null)
                    return match;
            }
        }

        return null;
    }

    private Transform FindInChildren(Transform parent, string objectName)
    {
        if ((includeInactiveObjects || parent.gameObject.activeInHierarchy) && parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (!includeInactiveObjects && !child.gameObject.activeInHierarchy)
                continue;

            Transform match = FindInChildren(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }
}
