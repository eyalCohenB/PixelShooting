using PixelShooting.Core.Levels;

namespace PixelShooting.Core.Queue
{
    /// <summary>
    /// The shooter columns below the holder. Only the top shooter of a column can be taken.
    ///
    /// Each column is a fixed array plus a "top" index: taking a shooter is just top++,
    /// so nothing is shifted or allocated. "The shooters below move up" is purely visual
    /// (the view tweens them); in the model, row 0 is simply whatever is at index top.
    /// </summary>
    public sealed class QueueModel
    {
        private Shooter[][] columns; // [column][index], index 0 = top at level start
        private int[] top;           // current top index per column

        public int ColumnCount => columns.Length;

        /// <summary>Creates the level's shooters (once per level start/restart).</summary>
        public void Reset(LevelData level)
        {
            int count = level.QueueCount;
            columns = new Shooter[count][];
            top = new int[count];

            for (int c = 0; c < count; c++)
            {
                var column = new Shooter[level.GetQueueLength(c)];
                for (int i = 0; i < column.Length; i++)
                {
                    column[i] = new Shooter(level.GetShooter(c, i));
                }
                columns[c] = column;
            }
        }

        public bool IsEmpty(int column) => top[column] >= columns[column].Length;

        /// <summary>Number of shooters still waiting in the column.</summary>
        public int Remaining(int column) => columns[column].Length - top[column];

        /// <summary>The selectable shooter of a column, or null if the column is empty.</summary>
        public Shooter PeekTop(int column) => IsEmpty(column) ? null : columns[column][top[column]];

        /// <summary>
        /// Removes and returns the top shooter (it's heading to the conveyor), or null if empty.
        /// The caller checks conveyor space before calling this.
        /// </summary>
        public Shooter TakeTop(int column)
        {
            if (IsEmpty(column))
            {
                return null;
            }
            Shooter shooter = columns[column][top[column]];
            top[column]++;
            shooter.Location = ShooterLocation.Conveyor;
            return shooter;
        }

        /// <summary>Shooter at a row counted from the current top (row 0 = top), or null past the end.</summary>
        public Shooter GetAt(int column, int row)
        {
            int index = top[column] + row;
            Shooter[] shooters = columns[column];
            return index < shooters.Length ? shooters[index] : null;
        }
    }
}
