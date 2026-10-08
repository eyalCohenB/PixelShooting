using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;

namespace PixelShooting.Core.Shooting
{
    /// <summary>One place on the conveyor where a rider lines up with a board line and fires.</summary>
    public readonly struct FiringPoint
    {
        public readonly float Distance; // along the conveyor path
        public readonly BoardSide Side; // the board side the rider is facing
        public readonly int Line;       // column (Bottom/Top) or row (Left/Right)

        public FiringPoint(float distance, BoardSide side, int line)
        {
            Distance = distance;
            Side = side;
            Line = line;
        }
    }

    /// <summary>
    /// Every firing point of the current level, sorted by path distance.
    ///
    /// A rider fires when it reaches the middle of a row or column, i.e. when its position on
    /// its side of the conveyor equals that line's cell centre:
    /// - bottom side (going right): columns, left to right, shooting up
    /// - right side (going up):     rows, bottom to top, shooting left
    /// - top side (going left):     columns, right to left, shooting down
    /// - left side (going down):    rows, top to bottom, shooting right
    /// Built in that order, the list is already sorted, so each rider only needs the index of
    /// its next point (ConveyorRider.NextPoint).
    /// </summary>
    public sealed class FiringPoints
    {
        private FiringPoint[] points = new FiringPoint[0];

        public int Count { get; private set; }
        public FiringPoint this[int index] => points[index];

        /// <param name="firstX">World x of the centre of column 0.</param>
        /// <param name="firstY">World y of the centre of row 0 (the bottom row).</param>
        /// <param name="cellSize">World distance between cell centres.</param>
        public void Build(ConveyorPath path, float firstX, float firstY, float cellSize, int width, int height)
        {
            int needed = 2 * (width + height);
            if (points.Length < needed)
            {
                points = new FiringPoint[needed]; // only when a level is bigger than any before
            }
            Count = 0;

            float pathWidth = path.Right - path.Left;
            float pathHeight = path.Top - path.Bottom;

            for (int x = 0; x < width; x++)
            {
                Add(Clamp(ColumnX(firstX, cellSize, x) - path.Left, pathWidth), BoardSide.Bottom, x);
            }
            for (int y = 0; y < height; y++)
            {
                Add(pathWidth + Clamp(RowY(firstY, cellSize, y) - path.Bottom, pathHeight), BoardSide.Right, y);
            }
            for (int x = width - 1; x >= 0; x--)
            {
                Add(pathWidth + pathHeight + Clamp(path.Right - ColumnX(firstX, cellSize, x), pathWidth), BoardSide.Top, x);
            }
            for (int y = height - 1; y >= 0; y--)
            {
                Add(2f * pathWidth + pathHeight + Clamp(path.Top - RowY(firstY, cellSize, y), pathHeight), BoardSide.Left, y);
            }
        }

        private void Add(float distance, BoardSide side, int line)
        {
            points[Count++] = new FiringPoint(distance, side, line);
        }

        private static float ColumnX(float firstX, float cellSize, int x) => firstX + x * cellSize;
        private static float RowY(float firstY, float cellSize, int y) => firstY + y * cellSize;

        // Keeps a point on its own side even if the board pokes outside the path (keeps the list sorted).
        private static float Clamp(float value, float max) => value < 0f ? 0f : (value > max ? max : value);
    }
}
