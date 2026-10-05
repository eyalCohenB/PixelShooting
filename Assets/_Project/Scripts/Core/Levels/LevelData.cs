using UnityEngine;

namespace PixelShooting.Core.Levels
{
    /// <summary>
    /// Immutable runtime definition of a level, built once by <see cref="LevelLoader"/>.
    /// Mutable game state (destroyed cells, taken shooters) lives in the models built from it,
    /// so restarting a level just rebuilds those models: no re-parsing, no allocations from JSON.
    ///
    /// Grid convention: row-major, y = 0 is the BOTTOM row (Unity's y-up). The JSON is written
    /// top row first for readability; the loader flips it, and nothing else knows about the flip.
    ///
    /// Data is exposed through small accessor methods rather than arrays or IReadOnlyList:
    /// arrays would let callers mutate the level, and foreach over an interface boxes its enumerator.
    /// </summary>
    public sealed class LevelData
    {
        public const sbyte EmptyCell = -1;

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public int PaletteCount => palette.Length;
        public int QueueCount => queues.Length;

        private readonly Color32[] palette;
        private readonly sbyte[] cells;           // palette index per cell, EmptyCell = no cell
        private readonly ShooterSpec[][] queues;  // [column][0 = top of the column]

        // Only the loader creates LevelData; it hands over arrays it no longer touches.
        internal LevelData(string name, int width, int height,
                           Color32[] palette, sbyte[] cells, ShooterSpec[][] queues)
        {
            Name = name;
            Width = width;
            Height = height;
            this.palette = palette;
            this.cells = cells;
            this.queues = queues;
        }

        public sbyte GetCell(int x, int y) => cells[y * Width + x];

        public Color32 GetColor(int colorIndex) => palette[colorIndex];

        public int GetQueueLength(int column) => queues[column].Length;

        public ShooterSpec GetShooter(int column, int indexFromTop) => queues[column][indexFromTop];
    }
}
