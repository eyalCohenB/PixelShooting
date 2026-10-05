using UnityEngine;

namespace PixelShooting.Core
{
    /// <summary>
    /// Pure math for fitting an orthographic camera so that a world-space content
    /// rectangle is fully visible inside the screen's safe area ("contain" fit).
    /// Has no dependency on Screen/Camera so it can be unit-tested.
    /// </summary>
    public static class CameraFitMath
    {
        /// <param name="content">World-space rectangle that must always be visible.</param>
        /// <param name="safeArea">Safe area in screen pixels (origin bottom-left, like Screen.safeArea).</param>
        /// <param name="screenSize">Full screen size in pixels.</param>
        /// <param name="orthoSize">Resulting Camera.orthographicSize (half of the full screen height, in world units).</param>
        /// <param name="cameraPosition">Resulting camera XY position in world space.</param>
        public static void Fit(Rect content, Rect safeArea, Vector2 screenSize,
                               out float orthoSize, out Vector2 cameraPosition)
        {
            // Some devices report an empty safe area for a frame during startup/rotation.
            // Fall back to the full screen instead of dividing by zero.
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                safeArea = new Rect(0f, 0f, screenSize.x, screenSize.y);
            }

            // How many world units one pixel must cover so the content fits each axis.
            // The larger value is the tighter axis; using the smaller one would crop the other.
            float unitsPerPixel = Mathf.Max(content.width / safeArea.width,
                                            content.height / safeArea.height);

            // orthographicSize is half the height of the WHOLE screen, not of the safe area.
            orthoSize = unitsPerPixel * screenSize.y * 0.5f;

            // Centering the camera on the content would center it on the screen.
            // Shift by the gap between the safe-area center and the screen center
            // (pixels -> world units) so the content is centered on the safe area instead.
            // E.g. a notch pushes the safe center down -> offset.y < 0 -> camera moves up -> content appears lower.
            Vector2 offsetPx = safeArea.center - screenSize * 0.5f;
            cameraPosition = content.center - offsetPx * unitsPerPixel;
        }
    }
}
