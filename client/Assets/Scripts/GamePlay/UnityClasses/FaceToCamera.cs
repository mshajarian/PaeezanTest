using UnityEngine;

public class FaceToCamera : MonoBehaviour
{
    [Tooltip("If true, only rotates around the Y axis (good for billboards).")]
    public bool onlyRotateY = true;

    private Transform cam;

    void Start()
    {
        // Cache main camera transform for performance
        if (Camera.main != null)
            cam = Camera.main.transform;
        else
            Debug.LogWarning("No main camera found! Make sure your camera is tagged 'MainCamera'.");
    }

    void LateUpdate()
    {
        if (!cam) return;

        if (onlyRotateY)
        {
            // Lock rotation so the object faces the camera horizontally only
            Vector3 dir = cam.position - transform.position;
            dir.y = 0; // ignore vertical difference
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            // Fully face the camera (like a billboard or floating UI)
            transform.LookAt(cam);
        }
    }
}
