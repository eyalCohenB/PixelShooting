namespace PixelShooting.Core.Levels
{
    /// <summary>
    /// Static description of one shooter in a level (what the JSON says), not its runtime state.
    /// A readonly struct: plain data, no heap allocation per shooter.
    /// </summary>
    public readonly struct ShooterSpec
    {
        public readonly byte ColorIndex;
        public readonly int Bullets;

        public ShooterSpec(byte colorIndex, int bullets)
        {
            ColorIndex = colorIndex;
            Bullets = bullets;
        }
    }
}
