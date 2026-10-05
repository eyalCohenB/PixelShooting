using System.Collections.Generic;
using System.Text;
using PixelShooting.Core.Levels;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Temporary debug helper (Part 2, stage 2): loads every level in the catalog on Start
    /// and logs the result, so the loader and validator can be checked in the editor.
    /// Remove once the real game flow loads levels.
    /// </summary>
    public class LevelLoadTester : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;

        private void Start()
        {
            if (catalog == null)
            {
                Debug.LogError("LevelLoadTester: no LevelCatalog assigned.", this);
                return;
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            for (int i = 0; i < catalog.Count; i++)
            {
                TextAsset file = catalog.Get(i);
                if (file == null)
                {
                    Debug.LogError($"LevelLoadTester: catalog entry {i} is empty.", catalog);
                    continue;
                }

                errors.Clear();
                warnings.Clear();
                bool ok = LevelLoader.TryLoad(file.text, file.name, out LevelData level, errors, warnings);

                for (int w = 0; w < warnings.Count; w++)
                {
                    Debug.LogWarning(warnings[w], file);
                }

                if (ok)
                {
                    Debug.Log(Describe(level), file);
                }
                else
                {
                    Debug.LogError($"{file.name}: REJECTED\n{string.Join("\n", errors)}", file);
                }
            }
        }

        // Prints size, queues and the grid as it will appear on screen (top row first),
        // which also confirms the loader's row flip.
        private static string Describe(LevelData level)
        {
            var sb = new StringBuilder();
            sb.Append(level.Name).Append(": OK  ")
              .Append(level.Width).Append('x').Append(level.Height)
              .Append(", ").Append(level.PaletteCount).Append(" colors, ")
              .Append(level.QueueCount).Append(" queues\n");

            for (int y = level.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    sbyte cell = level.GetCell(x, y);
                    sb.Append(cell == LevelData.EmptyCell ? " ." : " " + cell);
                }
                sb.Append('\n');
            }

            for (int q = 0; q < level.QueueCount; q++)
            {
                sb.Append("queue ").Append(q).Append(" (top first):");
                for (int i = 0; i < level.GetQueueLength(q); i++)
                {
                    ShooterSpec s = level.GetShooter(q, i);
                    sb.Append("  c").Append(s.ColorIndex).Append('x').Append(s.Bullets);
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
