namespace PixelShooting.Core.Hint
{
    /// <summary>Where a tapped shooter comes from: the top of a queue column, or a holder slot.</summary>
    public struct TapSource
    {
        public bool FromHolder;
        public int Index; // queue column, or holder slot

        public static TapSource Queue(int column) => new TapSource { FromHolder = false, Index = column };
        public static TapSource Holder(int slot) => new TapSource { FromHolder = true, Index = slot };
    }
}
