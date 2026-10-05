using PixelShooting.Core;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Keeps the gameplay content rectangle fully visible inside Screen.safeArea on any
    /// device by adjusting the orthographic size and XY position of this camera.
    /// Recalculates only when the screen size or safe area changes (e.g. device switch in
    /// the Device Simulator, foldables), so the per-frame cost is a few comparisons and no allocations.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [Tooltip("World-space area that must always be visible inside the safe area. " +
                 "X/Y is the bottom-left corner. Assumes the BG sits at the world origin.")]
        [SerializeField] private Rect contentArea = new Rect(-5.6f, -10f, 11.2f, 19.4f);

        private Camera cam;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
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

            CameraFitMath.Fit(contentArea, lastSafeArea,
                              new Vector2(lastScreenWidth, lastScreenHeight),
                              out float orthoSize, out Vector2 position);

            cam.orthographicSize = orthoSize;
            // Keep Z: the camera must stay in front of the sprites (z = 0).
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

#if UNITY_EDITOR
        // Re-fit immediately when the rect is edited in the Inspector during Play mode.
        private void OnValidate()
        {
            if (cam != null && Application.isPlaying)
            {
                Apply();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(contentArea.center, contentArea.size);
        }
#endif
    }
}
