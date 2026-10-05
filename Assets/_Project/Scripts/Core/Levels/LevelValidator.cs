using System.Collections.Generic;
using UnityEngine;

namespace PixelShooting.Core.Levels
{
    /// <summary>
    /// Checks a parsed level before it's built, so the rest of the game can assume valid data.
    /// Every error message should name the level and the problem, e.g.
    /// "Level02: color 0 has 10 cells but 9 bullets."
    /// </summary>
    public static class LevelValidator
    {
        // Cells are stored as sbyte, so palette indices must fit in 0..127.
        private const int MaxPaletteSize = 127;

        /// <summary>
        /// Errors (level is rejected):
        ///  1. width > 0, height > 0, cells not null and cells.Length == width * height.
        ///  2. Palette not null/empty and at most 127 colors (cells are stored as sbyte);
        ///     every hex string parses (ColorUtility.TryParseHtmlString);
        ///     every cell is -1 or a valid palette index.
        ///  3. queues not null and has at least 1 queue; every shooter has a valid palette
        ///     color and bullets > 0. (Watch for null shooters arrays.)
        ///  4. For every color: total bullets of that color == number of cells of that color.
        /// Warnings (level still loads):
        ///  5. A palette color that no cell uses; a queue with no shooters.
        /// </summary>
        /// <returns>True if there are no errors.</returns>
        public static bool Validate(LevelDto dto, string levelName,
                                    List<string> errors, List<string> warnings)
        {
            // Only errors added by THIS call decide the result (the list may already hold others).
            int errorsBefore = errors.Count;

            // --- Structural checks: if any fails, later checks would crash, so return early. ---
            if (dto == null)
            {
                errors.Add($"{levelName}: no level data.");
                return false;
            }
            if (dto.width <= 0 || dto.height <= 0)
            {
                errors.Add($"{levelName}: size must be positive (got {dto.width}x{dto.height}).");
                return false;
            }
            if (dto.cells == null || dto.cells.Length != dto.width * dto.height)
            {
                int count = dto.cells == null ? 0 : dto.cells.Length;
                errors.Add($"{levelName}: expected {dto.width * dto.height} cells ({dto.width}x{dto.height}) but got {count}.");
                return false;
            }
            if (dto.palette == null || dto.palette.Length == 0 || dto.palette.Length > MaxPaletteSize)
            {
                int count = dto.palette == null ? 0 : dto.palette.Length;
                errors.Add($"{levelName}: palette must have 1 to {MaxPaletteSize} colors (got {count}).");
                return false;
            }
            if (dto.queues == null || dto.queues.Length == 0)
            {
                errors.Add($"{levelName}: there must be at least one queue.");
                return false;
            }

            int colorCount = dto.palette.Length;

            // --- Content checks: collect every problem instead of stopping at the first. ---
            for (int i = 0; i < colorCount; i++)
            {
                if (!ColorUtility.TryParseHtmlString(dto.palette[i], out _))
                {
                    errors.Add($"{levelName}: palette color {i} (\"{dto.palette[i]}\") is not a valid hex color.");
                }
            }

            // Count the number of cells of each color (and reject unknown indices).
            var cellsPerColor = new int[colorCount];
            for (int i = 0; i < dto.cells.Length; i++)
            {
                int value = dto.cells[i];
                if (value == LevelData.EmptyCell)
                {
                    continue;
                }
                if (value < 0 || value >= colorCount)
                {
                    errors.Add($"{levelName}: cell {i} (row {i / dto.width} from the top, column {i % dto.width}) has invalid color {value}.");
                    continue;
                }
                cellsPerColor[value]++;
            }

            // Count the number of bullets of each color (and check every shooter).
            var bulletsPerColor = new int[colorCount];
            for (int q = 0; q < dto.queues.Length; q++)
            {
                QueueDto queue = dto.queues[q];
                if (queue == null || queue.shooters == null || queue.shooters.Length == 0)
                {
                    warnings.Add($"{levelName}: queue {q} has no shooters.");
                    continue;
                }
                for (int s = 0; s < queue.shooters.Length; s++)
                {
                    ShooterDto shooter = queue.shooters[s];
                    if (shooter == null || shooter.color < 0 || shooter.color >= colorCount)
                    {
                        errors.Add($"{levelName}: queue {q}, shooter {s} has an invalid color.");
                        continue;
                    }
                    if (shooter.bullets <= 0)
                    {
                        errors.Add($"{levelName}: queue {q}, shooter {s} must have at least 1 bullet (got {shooter.bullets}).");
                        continue;
                    }
                    bulletsPerColor[shooter.color] += shooter.bullets;
                }
            }

            // Compare the two counts: equal per color guarantees the level can be finished
            // (and makes the totals equal too).
            for (int c = 0; c < colorCount; c++)
            {
                if (cellsPerColor[c] != bulletsPerColor[c])
                {
                    errors.Add($"{levelName}: color {c} has {cellsPerColor[c]} cells but {bulletsPerColor[c]} bullets.");
                }
                else if (cellsPerColor[c] == 0)
                {
                    warnings.Add($"{levelName}: palette color {c} is not used.");
                }
            }

            return errors.Count == errorsBefore;
        }
    }
}
