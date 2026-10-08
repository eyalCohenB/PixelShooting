using System.Collections.Generic;
using DG.Tweening;
using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Temporary debug helper (replaces BoardTester) until the real game flow exists.
    /// Keys: 1/2/3 load a level, R restart, Space fire a random valid shot, S shake column 0.
    /// Click/tap a column's top shooter: it reserves a conveyor place (or shakes if full), waits
    /// in place until the conveyor gate opens, then hops on and rides one lap. The lap ends on the
    /// left side, just above the entrance; from there it hops onto the conveyor start box and
    /// returns to the pool (placeholder: stage 7 hops it to the holder instead).
    /// </summary>
    public class GameplayTester : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private QueueView queueView;
        [SerializeField] private ConveyorView conveyorView;
        [Tooltip("Placeholder exit-hop target until the holder exists (stage 7).")]
        [SerializeField] private Transform conveyorStart;

        [Header("Conveyor (world units, track centre line)")]
        [SerializeField] private float pathLeft = -4.75f;
        [SerializeField] private float pathRight = 4.75f;
        [SerializeField] private float pathBottom = -1.77f;
        [SerializeField] private float pathTop = 8.26f;
        [Tooltip("The lap ends this far before the end of the path (up the left side from the bottom-left corner).")]
        [SerializeField] private float lapEndGap = 1.6f;
        [SerializeField] private float speed = 5f;
        [Tooltip("Minimum distance between riders along the path; at least a shooter's width.")]
        [SerializeField] private float minSpacing = 1.2f;
        [SerializeField] private float hopTime = 0.4f;
        [SerializeField] private float hopHeight = 1f;

        private readonly BoardModel board = new BoardModel();
        private readonly QueueModel queue = new QueueModel();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private LevelData level;
        private Camera cam;

        private ConveyorPath path;
        private ConveyorModel conveyor;
        private readonly Shooter[] exited = new Shooter[ConveyorModel.Capacity];

        // Taps that reserved a place but whose shooter still waits in place (FIFO of queue columns).
        private readonly int[] pendingColumns = new int[ConveyorModel.Capacity];
        private int pendingCount;

        private void Start()
        {
            cam = Camera.main;
            path = new ConveyorPath(pathLeft, pathRight, pathBottom, pathTop);
            conveyor = new ConveyorModel(path, path.Length - lapEndGap, speed, minSpacing, hopTime);
            conveyorView.Init(path);
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

            if (level == null) return;
            StartPendingHops();
            int exitCount = conveyor.Update(Time.deltaTime, exited);
            conveyorView.Sync(conveyor);
            for (int i = 0; i < exitCount; i++)
            {
                HopOff(conveyorView.Remove(exited[i]));
                exited[i] = null;
            }
        }

        private void OnTap(Vector3 screenPosition)
        {
            Vector3 world = cam.ScreenToWorldPoint(screenPosition);
            if (!queueView.TryPickColumn(world, out int column)) return;

            // Every pending tap on this column will take one more shooter from it.
            if (CountPending(column) >= queue.Remaining(column)) return;

            if (!conveyor.TryReserve())
            {
                queueView.Shake(column); // conveyor full
                return;
            }
            pendingColumns[pendingCount++] = column;
        }

        /// <summary>Lets waiting shooters hop on, oldest tap first, while the conveyor gate is open.</summary>
        private void StartPendingHops()
        {
            while (pendingCount > 0 && conveyor.CanEnter)
            {
                int column = pendingColumns[0];
                for (int i = 1; i < pendingCount; i++) pendingColumns[i - 1] = pendingColumns[i];
                pendingCount--;

                Shooter shooter = queue.TakeTop(column);
                ShooterView view = queueView.TakeTop(queue, column);
                conveyor.Enter(shooter);
                conveyorView.Add(shooter, view);

                // The hop takes exactly HopDuration, so it lands at the start when the model's
                // distance reaches 0 and ConveyorView.Sync takes over.
                Vector2 start = path.Evaluate(0f);
                Vector3 target = new Vector3(start.x, start.y, view.CachedTransform.position.z);
                view.CachedTransform.DOKill();
                view.CachedTransform.DOJump(target, hopHeight, 1, conveyor.HopDuration).SetEase(Ease.Linear);
            }
        }

        /// <summary>Placeholder until stage 7: hop onto the conveyor start box, then back to the pool.</summary>
        private void HopOff(ShooterView view)
        {
            Vector3 target = conveyorStart.position;
            target.z = view.CachedTransform.position.z;
            view.CachedTransform.DOKill();
            view.CachedTransform.DOJump(target, hopHeight, 1, hopTime).OnComplete(view.ReleaseCallback);
        }

        private int CountPending(int column)
        {
            int count = 0;
            for (int i = 0; i < pendingCount; i++)
            {
                if (pendingColumns[i] == column) count++;
            }
            return count;
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
            ClearConveyor();
            board.Reset(level);
            boardView.Build(board, level);
            queue.Reset(level);
            queueView.Build(queue, level);
        }

        private void ClearConveyor()
        {
            for (int i = 0; i < conveyor.RiderCount; i++)
            {
                conveyorView.Remove(conveyor.GetRider(i).Shooter).ReleaseToPool();
            }
            conveyor.Reset();
            conveyorView.Clear();
            pendingCount = 0;
        }

        // Draws the conveyor path (yellow) and where the lap ends (red), for tuning in the Scene view.
        private void OnDrawGizmosSelected()
        {
            var p = new ConveyorPath(pathLeft, pathRight, pathBottom, pathTop);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(pathLeft, pathBottom), new Vector3(pathRight, pathBottom));
            Gizmos.DrawLine(new Vector3(pathRight, pathBottom), new Vector3(pathRight, pathTop));
            Gizmos.DrawLine(new Vector3(pathRight, pathTop), new Vector3(pathLeft, pathTop));
            Gizmos.DrawLine(new Vector3(pathLeft, pathTop), new Vector3(pathLeft, pathBottom));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(p.Evaluate(p.Length - lapEndGap), 0.3f);
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
