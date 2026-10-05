using System.Collections.Generic;
using PixelShooting.Core.Board;
using PixelShooting.Core.Levels;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Temporary debug helper (Part 2, stage 3): builds a level on the BoardView.
    /// Keys: 1/2/3 = load that catalog level, Space = fire one random valid shot and pop the cell,
    /// R = restart the current level. Remove once the real game flow exists.
    /// </summary>
    public class BoardTester : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;

        private readonly BoardModel board = new BoardModel();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private LevelData level;

        private void Start() => Load(0);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Load(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Load(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Load(2);
            if (Input.GetKeyDown(KeyCode.R) && level != null)
            {
                board.Reset(level);
                boardView.Build(board, level);
            }
            if (Input.GetKeyDown(KeyCode.Space)) FireRandom();
        }

        private void Load(int index)
        {
            if (index >= catalog.Count) return;
            TextAsset file = catalog.Get(index);
            errors.Clear();
            warnings.Clear();
            if (!LevelLoader.TryLoad(file.text, file.name, out level, errors, warnings))
            {
                Debug.LogError(string.Join("\n", errors), file);
                return;
            }
            board.Reset(level);
            boardView.Build(board, level);
            Debug.Log($"{file.name}: {board.Width}x{board.Height}, {board.RemainingCells} cells, cell size {boardView.CellSize:0.00}");
        }

        // Tries random sides/lines until a shot with the exposed cell's own color succeeds.
        private void FireRandom()
        {
            if (level == null || board.IsCleared) return;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var side = (BoardSide)Random.Range(0, 4);
                int line = Random.Range(0, board.LineCount(side));
                if (!board.TryGetExposed(side, line, out int x, out int y)) continue;
                byte color = (byte)board.GetCell(x, y);
                if (board.TryFire(side, line, color, out x, out y))
                {
                    boardView.Pop(x, y);
                    if (board.IsCleared) Debug.Log("Board cleared");
                    return;
                }
            }
        }
    }
}
