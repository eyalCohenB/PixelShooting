using System;

namespace PixelShooting.Core.Levels
{
    // Data Transfer Objects: an exact mirror of the level JSON, read with JsonUtility.
    // Field names must match the JSON keys. JsonUtility can't read nested arrays,
    // which is why cells are flat and each queue is wrapped in an object.
    // Only LevelLoader/LevelValidator see these; the game uses LevelData.

    [Serializable]
    public class LevelDto
    {
        public int width;
        public int height;
        public string[] palette;   // hex colors, e.g. "#E53935"
        public int[] cells;        // width*height palette indices, -1 = empty, TOP row first
        public QueueDto[] queues;  // left-to-right column order
    }

    [Serializable]
    public class QueueDto
    {
        public ShooterDto[] shooters; // index 0 = top of the column (the selectable one)
    }

    [Serializable]
    public class ShooterDto
    {
        public int color;   // palette index
        public int bullets;
    }
}
