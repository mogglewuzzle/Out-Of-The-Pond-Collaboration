using UnityEngine;
using System.Collections.Generic;

public class Player_PickupControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pickupOrigin;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Camera playerCamera;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 3f;
    [SerializeField] private bool holdOnlyWhileButtonHeld = false;
    [SerializeField] private bool useConeAngle = true;
    [SerializeField] private float maxConeAngle = 15f;
    [SerializeField] private string pickupTag = "GrabPoint";
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    private PlayerInputHandler input;
    private Object_Pickupable heldObject;

    public Object_Pickupable HeldObject => heldObject;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (holdOnlyWhileButtonHeld && heldObject != null && input != null && !input.PickUpHeld)
        {
            DropHeldObject();
            return;
        }

        if (input == null || !input.PickUpPressed)
            return;

        if (heldObject != null)
        {
            if (holdOnlyWhileButtonHeld)
                return;

            DropHeldObject();
            return;
        }

        Object_Pickupable candidate = FindBestPickupCandidate();
        if (candidate != null)
            PickUpObject(candidate);
    }

    private void FixedUpdate()
    {
        if (heldObject == null || holdPoint == null)
            return;

        heldObject.Rigidbody.MovePosition(holdPoint.position);
        heldObject.Rigidbody.MoveRotation(holdPoint.rotation);
    }

    private Object_Pickupable FindBestPickupCandidate()
    {
        if (pickupOrigin == null || playerCamera == null)
            return null;

        Collider[] hits = Physics.OverlapSphere(pickupOrigin.position, pickupRadius);
        if (hits.Length == 0)
            return null;

        float minCenterDot = Mathf.Cos(maxConeAngle * Mathf.Deg2Rad);
        HashSet<Object_Pickupable> uniqueCandidates = new HashSet<Object_Pickupable>();
        Object_Pickupable bestCandidate = null;
        int bestPriority = int.MinValue;
        float bestDot = -1f;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Object_Pickupable pickupable = hits[i].GetComponentInParent<Object_Pickupable>();
            if (pickupable == null || !uniqueCandidates.Add(pickupable))
                continue;

            if (!pickupable.CompareTag(pickupTag))
                continue;

            Vector3 candidatePos = pickupable.Rigidbody.worldCenterOfMass;
            float distance = Vector3.Distance(pickupOrigin.position, candidatePos);
            if (distance > pickupRadius)
                continue;

            float centerDot = 1f;
            if (useConeAngle)
            {
                Vector3 dirToCandidate = (candidatePos - playerCamera.transform.position).normalized;
                centerDot = Vector3.Dot(playerCamera.transform.forward, dirToCandidate);
                if (centerDot < minCenterDot)
                    continue;
            }

            if (requireLineOfSight)
            {
                Vector3 camPos = playerCamera.transform.position;
                Vector3 toCandidate = candidatePos - camPos;
                float rayDistance = toCandidate.magnitude;

                if (Physics.Raycast(camPos, toCandidate.normalized, out RaycastHit hit, rayDistance, lineOfSightMask))
                {
                    Object_Pickupable hitPickupable = hit.collider.GetComponentInParent<Object_Pickupable>();
                    if (hitPickupable != pickupable)
                        continue;
                }
            }

            int priority = pickupable.PickupPriority;
            bool isBetter =
                priority > bestPriority ||
                (priority == bestPriority && centerDot > bestDot) ||
                (priority == bestPriority && Mathf.Approximately(centerDot, bestDot) && distance < bestDistance);

            if (!isBetter)
                continue;

            bestCandidate = pickupable;
            bestPriority = priority;
            bestDot = centerDot;
            bestDistance = distance;
        }

        return bestCandidate;
    }

    private void PickUpObject(Object_Pickupable pickupable)
    {
        heldObject = pickupable;
        heldObject.OnPickedUp();

        if (holdPoint != null)
        {
            heldObject.Rigidbody.position = holdPoint.position;
            heldObject.Rigidbody.rotation = holdPoint.rotation;
        }
    }

    private void DropHeldObject()
    {
        if (heldObject == null)
            return;

        heldObject.OnDropped();
        heldObject = null;
    }

    public bool TryThrowHeldObject(Vector3 throwVelocity)
    {
        if (heldObject == null)
            return false;

        Object_Pickupable objectToThrow = heldObject;
        heldObject = null;
        objectToThrow.OnThrown(throwVelocity);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || pickupOrigin == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pickupOrigin.position, pickupRadius);
    }
}
