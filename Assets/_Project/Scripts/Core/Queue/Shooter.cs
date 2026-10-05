using PixelShooting.Core.Levels;

namespace PixelShooting.Core.Queue
{
    /// <summary>Where a shooter currently is.</summary>
    public enum ShooterLocation : byte { Queue, Conveyor, Holder, Destroyed }

    /// <summary>
    /// A shooter during play. A class (not a struct) so the same object moves between the
    /// queue, the conveyor and the holder without being copied. Created once per level.
    /// </summary>
    public sealed class Shooter
    {
        public byte ColorIndex { get; }
        public int BulletsLeft { get; set; }
        public ShooterLocation Location { get; set; }

        public Shooter(ShooterSpec spec)
        {
            ColorIndex = spec.ColorIndex;
            BulletsLeft = spec.Bullets;
            Location = ShooterLocation.Queue;
        }
    }
}
