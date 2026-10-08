using UnityEngine;

namespace PixelShooting.Core.Conveyor
{
    /// <summary>
    /// The conveyor track as a rectangle. A position on it is a single number:
    /// the distance travelled from the start (bottom-left corner), counter-clockwise.
    /// </summary>
    public sealed class ConveyorPath
    {
        private readonly float left, right, bottom, top;
        private readonly float width, height;

        public float Length { get; }

        public ConveyorPath(float left, float right, float bottom, float top)
        {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
            width = new Vector2(right, 0).x - new Vector2(left, 0).x;
            height = new Vector2(0, top).y - new Vector2(0, bottom).y;
            Length = 2 * (width + height);
        }

        public Vector2 Evaluate(float distance)
        {
            if (distance < 0 || distance > Length)
            {
                distance = Mathf.Clamp(distance, 0f, Length);
            }
            if (distance <= width)
            {
                return new Vector2(left + distance, bottom);
            }
            distance -= width;

            if (distance <= height)
            {
                return new Vector2(right, bottom + distance);
            }
            distance -= height;
            
            if (distance <= width)
            {
                return new Vector2(right - distance, top);
            }
            distance -= width;
            
            return new Vector2(left, top - distance);
        }
    }
}
