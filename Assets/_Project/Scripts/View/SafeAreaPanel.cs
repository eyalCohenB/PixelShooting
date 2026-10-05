using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Resizes this RectTransform to match Screen.safeArea, so HUD elements placed under it
    /// never end up behind a notch, rounded corner or home indicator.
    /// Must sit directly under a full-screen Canvas (Screen Space - Overlay or Camera).
    /// Works with anchors only (normalized 0..1), so it's independent of the Canvas Scaler.
    /// Recalculates only when the screen size or safe area changes; no per-frame allocations.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform rectTransform;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        private void LateUpdate()
        {
            if (Screen.width != lastScreenWidth ||
                Screen.height != lastScreenHeight ||
                Screen.safeArea != lastSafeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;

            if (lastScreenWidth <= 0 || lastScreenHeight <= 0)
            {
                return;
            }

            // Convert the safe area from pixels to normalized (0..1) anchors of the full-screen parent.
            Vector2 anchorMin = lastSafeArea.position;
            Vector2 anchorMax = lastSafeArea.position + lastSafeArea.size;
            anchorMin.x /= lastScreenWidth;
            anchorMin.y /= lastScreenHeight;
            anchorMax.x /= lastScreenWidth;
            anchorMax.y /= lastScreenHeight;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            // Zero offsets so the rect is exactly the anchor area.
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
