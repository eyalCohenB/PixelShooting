using System.Collections.Generic;
using DG.Tweening;
using PixelShooting.Core.Board;
using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Temporary debug helper (replaces BoardTester) until the real game flow exists.
    /// Keys: 1/2/3 load a level, R restart, Space fire a random valid shot, S shake column 0.
    /// Click/tap a column's top shooter: it's taken from the queue and flies to the conveyor
    /// start (then returns to the pool, since the conveyor doesn't exist yet).
    /// </summary>
    public class GameplayTester : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private QueueView queueView;
        [SerializeField] private Transform conveyorStart;

        private readonly BoardModel board = new BoardModel();
        private readonly QueueModel queue = new QueueModel();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private LevelData level;
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            Load(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Load(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Load(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Load(2);
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Space)) FireRandom();
            if (Input.GetKeyDown(KeyCode.S)) queueView.Shake(0);
            if (Input.GetMouseButtonDown(0)) OnTap(Input.mousePosition);
        }

        private void OnTap(Vector3 screenPosition)
        {
            Vector3 world = cam.ScreenToWorldPoint(screenPosition);
            if (!queueView.TryPickColumn(world, out int column)) return;

            queue.TakeTop(column);
            ShooterView view = queueView.TakeTop(queue, column);

            Vector3 target = conveyorStart.position;
            target.z = view.CachedTransform.position.z;
            view.CachedTransform.DOKill();
            view.CachedTransform.DOJump(target, 1f, 1, 0.4f).OnComplete(view.ReleaseCallback);
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
            Restart();
        }

        private void Restart()
        {
            if (level == null) return;
            board.Reset(level);
            boardView.Build(board, level);
            queue.Reset(level);
            queueView.Build(queue, level);
        }

        private void FireRandom()
        {
            if (level == null || board.IsCleared) return;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var side = (BoardSide)Random.Range(0, 4);
                int line = Random.Range(0, board.LineCount(side));
                if (!board.TryGetExposed(side, line, out int x, out int y)) continue;
                if (board.TryFire(side, line, (byte)board.GetCell(x, y), out x, out y))
                {
                    boardView.Pop(x, y);
                    return;
                }
            }
        }
    }
}
