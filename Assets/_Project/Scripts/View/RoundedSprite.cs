using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Makes a white rounded-rectangle sprite at runtime, 9-sliced so it stretches to any size
    /// with crisp corners (the package has no UI panel sprite). Built once and cached.
    /// Tint it with Image.color; Image.pixelsPerUnitMultiplier scales the corner radius.
    /// </summary>
    public static class RoundedSprite
    {
        private const int Size = 64;
        private const int Radius = 28;
        private static Sprite cached;

        public static Sprite Get()
        {
            if (cached != null) return cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "RoundedRect",
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Distance outside the inner (radius-inset) rectangle = distance from the corner arc.
                    float dx = Mathf.Max(Radius - (x + 0.5f), (x + 0.5f) - (Size - Radius), 0f);
                    float dy = Mathf.Max(Radius - (y + 0.5f), (y + 0.5f) - (Size - Radius), 0f);
                    float alpha = Mathf.Clamp01(Radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f); // 1px anti-alias
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(Radius, Radius, Radius, Radius));
            cached.name = "RoundedRect";
            return cached;
        }
    }
}
