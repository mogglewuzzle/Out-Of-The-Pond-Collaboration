using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Object_Pickupable : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private int pickupPriority = 0;
    [SerializeField] private bool disableGravityWhileHeld = true;

    [Header("Layer Settings")]
    [SerializeField] private string defaultLayerName = "Pickupable";
    [SerializeField] private string heldLayerName = "BeingHeld";
    [SerializeField] private float layerResetDelay = 0.2f;

    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float upwardThrowBoost = 1.5f;
    [SerializeField] private float thrownGravityMultiplier = 1f;

    private Rigidbody cachedRb;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool applyThrownGravity;
    private Coroutine layerResetRoutine;

    public Rigidbody Rigidbody
    {
        get
        {
            if (cachedRb == null)
                cachedRb = GetComponent<Rigidbody>();

            return cachedRb;
        }
    }

    public int PickupPriority => pickupPriority;
    public float ThrowForce => throwForce;
    public float UpwardThrowBoost => upwardThrowBoost;
    public float ThrownGravityMultiplier => thrownGravityMultiplier;

    public void OnPickedUp()
    {
        originalUseGravity = Rigidbody.useGravity;
        originalIsKinematic = Rigidbody.isKinematic;
        applyThrownGravity = false;

        CancelLayerReset();
        SetLayerRecursively(gameObject, GetLayerIndexOrCurrent(heldLayerName));

        if (disableGravityWhileHeld)
            Rigidbody.useGravity = false;

        Rigidbody.linearVelocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
        Rigidbody.isKinematic = true;
    }

    public void OnDropped()
    {
        applyThrownGravity = false;
        CancelLayerReset();
        Rigidbody.useGravity = originalUseGravity;
        Rigidbody.isKinematic = originalIsKinematic;
        layerResetRoutine = StartCoroutine(ResetLayerAfterDelay());
    }

    public void OnThrown(Vector3 throwVelocity)
    {
        applyThrownGravity = false;
        CancelLayerReset();
        Rigidbody.useGravity = originalUseGravity;
        Rigidbody.isKinematic = originalIsKinematic;
        Rigidbody.linearVelocity = throwVelocity;
        applyThrownGravity = thrownGravityMultiplier != 1f;
        layerResetRoutine = StartCoroutine(ResetLayerAfterDelay());
    }

    private void FixedUpdate()
    {
        if (!applyThrownGravity || Rigidbody.isKinematic || !Rigidbody.useGravity)
            return;

        Rigidbody.AddForce(Physics.gravity * (thrownGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private IEnumerator ResetLayerAfterDelay()
    {
        if (layerResetDelay > 0f)
            yield return new WaitForSeconds(layerResetDelay);

        SetLayerRecursively(gameObject, GetLayerIndexOrCurrent(defaultLayerName));
        layerResetRoutine = null;
    }

    private void CancelLayerReset()
    {
        if (layerResetRoutine == null)
            return;

        StopCoroutine(layerResetRoutine);
        layerResetRoutine = null;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private int GetLayerIndexOrCurrent(string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        return layerIndex >= 0 ? layerIndex : gameObject.layer;
    }

    private void OnDisable()
    {
        CancelLayerReset();
    }
}
