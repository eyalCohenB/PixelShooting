using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelShooting.Core.Levels
{
    /// <summary>
    /// Turns level JSON into <see cref="LevelData"/>: parse (JsonUtility) -> validate
    /// (<see cref="LevelValidator"/>) -> build. Runs once per level load, never during gameplay,
    /// so the allocations here (DTOs, error strings) don't affect frame time.
    /// </summary>
    public static class LevelLoader
    {
        /// <param name="json">Level file contents.</param>
        /// <param name="levelName">Used in error messages (normally the TextAsset name).</param>
        /// <param name="level">The built level, or null when loading failed.</param>
        /// <param name="errors">Receives the reasons the level was rejected.</param>
        /// <param name="warnings">Receives non-fatal issues (the level still loads).</param>
        /// <returns>True if the level is valid and was built.</returns>
        public static bool TryLoad(string json, string levelName, out LevelData level,
                                   List<string> errors, List<string> warnings)
        {
            level = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add($"{levelName}: file is empty.");
                return false;
            }

            LevelDto dto;
            try
            {
                dto = JsonUtility.FromJson<LevelDto>(json);
            }
            catch (ArgumentException e)
            {
                errors.Add($"{levelName}: invalid JSON ({e.Message}).");
                return false;
            }

            if (dto == null)
            {
                errors.Add($"{levelName}: JSON produced no data.");
                return false;
            }

            if (!LevelValidator.Validate(dto, levelName, errors, warnings))
            {
                return false;
            }

            level = Build(dto, levelName);
            return true;
        }

        // Assumes the DTO passed validation, so indices and colors are known to be valid.
        private static LevelData Build(LevelDto dto, string levelName)
        {
            int width = dto.width;
            int height = dto.height;

            var palette = new Color32[dto.palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                ColorUtility.TryParseHtmlString(dto.palette[i], out Color color);
                palette[i] = color;
            }

            // Flip rows: JSON row 0 is the TOP row, model row 0 is the BOTTOM row.
            var cells = new sbyte[width * height];
            for (int fileRow = 0; fileRow < height; fileRow++)
            {
                int y = height - 1 - fileRow;
                for (int x = 0; x < width; x++)
                {
                    cells[y * width + x] = (sbyte)dto.cells[fileRow * width + x];
                }
            }

            var queues = new ShooterSpec[dto.queues.Length][];
            for (int q = 0; q < queues.Length; q++)
            {
                ShooterDto[] shooters = dto.queues[q].shooters;
                var column = new ShooterSpec[shooters.Length];
                for (int i = 0; i < column.Length; i++)
                {
                    column[i] = new ShooterSpec((byte)shooters[i].color, shooters[i].bullets);
                }
                queues[q] = column;
            }

            return new LevelData(levelName, width, height, palette, cells, queues);
        }
    }
}
