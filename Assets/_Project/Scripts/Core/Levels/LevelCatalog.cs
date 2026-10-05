using UnityEngine;

namespace PixelShooting.Core.Levels
{
    /// <summary>
    /// Ordered list of level files. The list order is the level order.
    /// Levels are referenced directly as TextAssets (no Resources folder, no string paths),
    /// so a missing or renamed file shows up in the Inspector instead of failing at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "PixelShooting/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private TextAsset[] levels = new TextAsset[0];

        public int Count => levels.Length;

        public TextAsset Get(int index) => levels[index];
    }
}
