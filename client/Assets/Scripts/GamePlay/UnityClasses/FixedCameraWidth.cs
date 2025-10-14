using UnityEngine;

[ExecuteAlways] // Makes it run in both Play Mode and Edit Mode
[RequireComponent(typeof(Camera))]
public class FixedCameraWidth : MonoBehaviour
{
    [Header("Fixed Horizontal Size (World Units)")]
    [Tooltip("The total width in world units that should be visible horizontally.")]
    public float fixedWidth = 27f; // The desired width across your game world

    private Camera cam;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void Update()
    {
        // Continuously update to handle resolution/aspect changes
        UpdateCameraSize();
    }

    private void UpdateCameraSize()
    {
        if (cam == null || !cam.orthographic)
            return;

        // Calculate the orthographic size based on fixed width and current aspect ratio
        float aspect = cam.aspect; // width / height
        cam.orthographicSize = fixedWidth / (2f * aspect);
    }

#if UNITY_EDITOR
    // Update when something changes in editor (scene view, resolution, etc.)
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            cam = GetComponent<Camera>();
            UpdateCameraSize();
        }
    }
#endif
}
