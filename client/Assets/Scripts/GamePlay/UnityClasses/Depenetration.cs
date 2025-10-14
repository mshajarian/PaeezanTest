using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Depenetration : MonoBehaviour
{
    private CapsuleCollider capsuleCollider;
    private int layerMask;
    private Vector3 currentPosition; // smoothed position
    private const float smoothSpeed = 5; // adjust for responsiveness

    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            Debug.LogError("No CapsuleCollider found on this GameObject!");
        }

        layerMask = 1 << LayerMask.NameToLayer("Characters");
        currentPosition = transform.position;
    }

    void LateUpdate()
    {
        if (transform.parent == null) return;

        Vector3 desiredPosition = transform.parent.position;
        Quaternion desiredRotation = Quaternion.Euler(0, 90, 0);

        if (Math.Abs(transform.parent.localScale.x - (-1)) < .1)
        {
            desiredRotation = Quaternion.Euler(0, -270, 0);
        }

        // Compute depenetration
        Vector3 correctedPosition = desiredPosition;
        Vector3 offset = Vector3.zero;

        Collider[] overlaps = new Collider[10];
        int numOverlaps = Physics.OverlapCapsuleNonAlloc(
            desiredPosition + capsuleCollider.center -
            transform.up * (capsuleCollider.height / 2 - capsuleCollider.radius),
            desiredPosition + capsuleCollider.center +
            transform.up * (capsuleCollider.height / 2 - capsuleCollider.radius),
            capsuleCollider.radius,
            overlaps,
            layerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < numOverlaps; i++)
        {
            Collider hit = overlaps[i];
            if (hit == capsuleCollider) continue;

            if (Physics.ComputePenetration(
                    capsuleCollider, desiredPosition, desiredRotation,
                    hit, hit.transform.position, hit.transform.rotation,
                    out Vector3 direction, out float distance))
            {
                offset += direction * distance;
            }
        }

        correctedPosition += offset;

        // Smooth movement to reduce jitter
        currentPosition = Vector3.Lerp(currentPosition, correctedPosition, Time.deltaTime * smoothSpeed);

        transform.SetPositionAndRotation(currentPosition, transform.rotation);
    }
}