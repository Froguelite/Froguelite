using UnityEngine;

public class OffscreenArrow2D : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image arrowImage;
    [SerializeField] private Transform frog;          // assign frog boss
    [SerializeField] private Camera cam;              // usually Main Camera
    [SerializeField] private RectTransform arrow;     // UI arrow image
    [SerializeField] private float screenEdgeBuffer = 50f; // padding from edge

    void Update()
    {
        bool onScreen = IsTargetOnScreen(frog.position);

        // Always toggle visibility
        arrowImage.enabled = !onScreen;

        if (!onScreen)
        {
            // Convert frog world position to screen position
            Vector3 screenPos = cam.WorldToScreenPoint(frog.position);
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            // Direction from center to frog
            Vector3 dir = (screenPos - screenCenter).normalized;

            // Position arrow at screen edge
            Vector3 edgePos = screenCenter + dir * ((Mathf.Min(Screen.width, Screen.height) / 2f) - screenEdgeBuffer);

            // Clamp to screen bounds with buffer
            edgePos.x = Mathf.Clamp(edgePos.x, screenEdgeBuffer, Screen.width - screenEdgeBuffer);
            edgePos.y = Mathf.Clamp(edgePos.y, screenEdgeBuffer, Screen.height - screenEdgeBuffer);

            // Apply to UI arrow
            arrow.position = edgePos;

            // Rotate arrow to point toward frog
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrow.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    private bool IsTargetOnScreen(Vector3 worldPos)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        return screenPos.z > 0 &&
               screenPos.x >= 0 && screenPos.x <= Screen.width &&
               screenPos.y >= 0 && screenPos.y <= Screen.height;
    }
}
