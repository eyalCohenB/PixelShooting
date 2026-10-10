using System;
using PixelShooting.Core.Levels;

namespace PixelShooting.Core.Board
{
    /// <summary>The board side a shooter is on. Order = lap order (counter-clockwise from bottom-left).</summary>
    public enum BoardSide : byte { Bottom, Right, Top, Left }

    /// <summary>
    /// Live board state for one play-through of a level. Pure C#, no Unity dependency.
    /// Coordinates match LevelData: x right, y up, (0,0) = bottom-left.
    ///
    /// Firing removes a cell from the logic immediately (it becomes EmptyCell); the view
    /// removes the sprite later when the bullet lands. That immediate removal is what stops
    /// two shooters from targeting the same cell.
    ///
    /// Exposed cells: for every side and line we keep a "depth" = how many steps in from that
    /// side the search starts. Rule: every cell before the depth is empty. Cells are only ever
    /// removed, so the rule can't break; the depth can only fall behind, and TryGetExposed
    /// catches it up by skipping empty cells (holes, cells shot from other sides).
    /// Each depth only moves forward, so total work per level is at most 4*W*H steps:
    /// O(1) amortized per query, no allocations.
    /// </summary>
    public sealed class BoardModel
    {
        private const int SideCount = 4;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RemainingCells { get; private set; }
        public bool IsCleared => RemainingCells == 0;

        private sbyte[] cells;    // live copy of the level's cells, row-major
        private int[][] depths;   // [side][line]

        /// <summary>
        /// Starts a fresh play-through. Reuses the arrays when the size matches,
        /// so restarting a level allocates nothing.
        /// </summary>
        public void Reset(LevelData level)
        {
            Width = level.Width;
            Height = level.Height;

            if (cells == null || cells.Length != Width * Height)
            {
                cells = new sbyte[Width * Height];
            }
            level.CopyCellsTo(cells);

            // Holes (EmptyCell) in the level don't count.
            int remaining = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != LevelData.EmptyCell)
                {
                    remaining++;
                }
            }
            RemainingCells = remaining;

            if (depths == null)
            {
                depths = new int[SideCount][];
            }
            for (int side = 0; side < SideCount; side++)
            {
                int lineCount = LineCount((BoardSide)side);
                if (depths[side] == null || depths[side].Length != lineCount)
                {
                    depths[side] = new int[lineCount];
                }
                else
                {
                    Array.Clear(depths[side], 0, lineCount);
                }
            }
        }

        /// <summary>
        /// Makes this board an exact copy of another (cells, depths, count), e.g. a snapshot for
        /// the hint solver to play on. Reuses the arrays when the size matches.
        /// </summary>
        public void CopyFrom(BoardModel source)
        {
            Width = source.Width;
            Height = source.Height;
            RemainingCells = source.RemainingCells;

            if (cells == null || cells.Length != source.cells.Length)
            {
                cells = new sbyte[source.cells.Length];
            }
            Array.Copy(source.cells, cells, cells.Length);

            if (depths == null)
            {
                depths = new int[SideCount][];
            }
            for (int side = 0; side < SideCount; side++)
            {
                int[] from = source.depths[side];
                if (depths[side] == null || depths[side].Length != from.Length)
                {
                    depths[side] = new int[from.Length];
                }
                Array.Copy(from, depths[side], from.Length);
            }
        }

        public sbyte GetCell(int x, int y) => cells[y * Width + x];

        /// <summary>
        /// Finds the first non-empty cell in a line, looking in from the given side.
        /// Only looks; doesn't change the board (apart from caching the depth).
        /// </summary>
        /// <returns>False if the line has no cells left from that side.</returns>
        public bool TryGetExposed(BoardSide side, int line, out int x, out int y)
        {
            int[] sideDepths = depths[(int)side];
            int depth = sideDepths[line];
            int length = LineLength(side);

            // Skip cells that are empty: holes, or cells already shot from any side.
            while (depth < length)
            {
                ToCell(side, line, depth, out x, out y);
                if (cells[y * Width + x] != LevelData.EmptyCell)
                {
                    sideDepths[line] = depth;
                    return true;
                }
                depth++;
            }

            sideDepths[line] = depth; // == length: line is empty from this side
            x = y = -1;
            return false;
        }

        /// <summary>
        /// Shoots into a line: if the exposed cell matches the color, removes it from the
        /// logic and returns its position (the bullet's target).
        /// </summary>
        /// <returns>False if the line is empty or blocked by a different color.</returns>
        public bool TryFire(BoardSide side, int line, byte color, out int x, out int y)
        {
            if (!TryGetExposed(side, line, out x, out y))
            {
                return false;
            }

            int index = y * Width + x;
            if (cells[index] != color)
            {
                return false; // blocked: the exposed cell is another color
            }

            cells[index] = LevelData.EmptyCell;
            RemainingCells--;
            return true;
        }

        /// <summary>Number of lines a side has: columns for Bottom/Top, rows for Left/Right.</summary>
        public int LineCount(BoardSide side) =>
            side == BoardSide.Bottom || side == BoardSide.Top ? Width : Height;

        // How deep a line goes from that side: column height for Bottom/Top, row width for Left/Right.
        private int LineLength(BoardSide side) =>
            side == BoardSide.Bottom || side == BoardSide.Top ? Height : Width;

        // Converts (side, line, depth) to a cell position.
        private void ToCell(BoardSide side, int line, int depth, out int x, out int y)
        {
            switch (side)
            {
                case BoardSide.Bottom: x = line;              y = depth;              break;
                case BoardSide.Top:    x = line;              y = Height - 1 - depth; break;
                case BoardSide.Left:   x = depth;             y = line;               break;
                default:               x = Width - 1 - depth; y = line;               break; // Right
            }
        }
    }
}
