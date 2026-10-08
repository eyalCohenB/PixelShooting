using System.Collections.Generic;
using DG.Tweening;
using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Holder;
using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue;
using PixelShooting.Core.Shooting;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Temporary debug helper (replaces BoardTester) until the real game flow exists.
    /// Keys: 1/2/3 load a level, R restart, S shake column 0.
    /// Click/tap a column's top shooter or a holder shooter: it reserves a conveyor place (or shakes
    /// if full), waits in place until the conveyor gate opens, then hops on and rides one lap. The
    /// lap ends on the left side, just above the entrance; from there it hops to the leftmost free
    /// holder slot. A 6th arrival at a full holder loses: logged, the game freezes until R/1/2/3
    /// (stage 8 adds the real popup).
    /// While riding, it fires at every row/column middle it passes (ShootingSystem); a shooter
    /// that uses its last bullet is taken off the conveyor and shrinks away.
    /// </summary>
    public class GameplayTester : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private QueueView queueView;
        [SerializeField] private ConveyorView conveyorView;
        [SerializeField] private BulletSystem bullets;
        [SerializeField] private HolderView holderView;

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
        [SerializeField] private float deathTime = 0.25f;

        private readonly BoardModel board = new BoardModel();
        private readonly QueueModel queue = new QueueModel();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private LevelData level;
        private Camera cam;

        private ConveyorPath path;
        private ConveyorModel conveyor;
        private readonly Shooter[] exited = new Shooter[ConveyorModel.Capacity];

        private readonly FiringPoints firingPoints = new FiringPoints();
        private ShootingSystem shooting;
        private readonly Shot[] shots = new Shot[32];

        private readonly HolderModel holder = new HolderModel();
        private bool lost;
        private ShooterView lostView; // the 6th shooter, shaking where it got off

        // A tap that reserved a conveyor place; its shooter still waits in place
        // (on top of a queue column, or in a holder slot) until the conveyor gate opens.
        private struct PendingTap
        {
            public bool FromHolder;
            public int Index; // queue column or holder slot
        }

        private readonly PendingTap[] pending = new PendingTap[ConveyorModel.Capacity]; // FIFO
        private int pendingCount;

        private void Start()
        {
            cam = Camera.main;
            path = new ConveyorPath(pathLeft, pathRight, pathBottom, pathTop);
            conveyor = new ConveyorModel(path, path.Length - lapEndGap, speed, minSpacing, hopTime);
            conveyorView.Init(path);
            shooting = new ShootingSystem(conveyor, board, firingPoints);
            Load(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Load(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Load(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Load(2);
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.S)) queueView.Shake(0);
            if (Input.GetMouseButtonDown(0)) OnTap(Input.mousePosition);

            if (level == null || lost) return;
            StartPendingHops();

            // Order matters: move, then fire at the points passed, then take off finished laps.
            conveyor.Advance(Time.deltaTime);
            int shotCount = shooting.Fire(shots);
            for (int i = 0; i < shotCount; i++)
            {
                ShowShot(in shots[i]);
                shots[i] = default;
            }
            int exitCount = conveyor.CollectExits(exited);
            conveyorView.Sync(conveyor);
            for (int i = 0; i < exitCount; i++)
            {
                ReturnToHolder(exited[i], conveyorView.Remove(exited[i]));
                exited[i] = null;
            }
        }

        private void OnTap(Vector3 screenPosition)
        {
            if (lost) return;
            Vector3 world = cam.ScreenToWorldPoint(screenPosition);

            if (holderView.TryPickSlot(world, out int slot))
            {
                if (IsPending(true, slot)) return; // already waiting for the gate
                if (!conveyor.TryReserve())
                {
                    holderView.Shake(slot); // conveyor full
                    return;
                }
                pending[pendingCount++] = new PendingTap { FromHolder = true, Index = slot };
                return;
            }

            if (!queueView.TryPickColumn(world, out int column)) return;

            // Every pending tap on this column will take one more shooter from it.
            if (CountPending(column) >= queue.Remaining(column)) return;

            if (!conveyor.TryReserve())
            {
                queueView.Shake(column); // conveyor full
                return;
            }
            pending[pendingCount++] = new PendingTap { FromHolder = false, Index = column };
        }

        /// <summary>Lets waiting shooters hop on, oldest tap first, while the conveyor gate is open.</summary>
        private void StartPendingHops()
        {
            while (pendingCount > 0 && conveyor.CanEnter)
            {
                PendingTap tap = pending[0];
                for (int i = 1; i < pendingCount; i++) pending[i - 1] = pending[i];
                pendingCount--;

                Shooter shooter;
                ShooterView view;
                if (tap.FromHolder)
                {
                    shooter = holder.Take(tap.Index); // the slot is freed only now, as it leaves
                    view = holderView.TakeView(tap.Index);
                }
                else
                {
                    shooter = queue.TakeTop(tap.Index);
                    view = queueView.TakeTop(queue, tap.Index);
                }
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

        /// <summary>Sends the bullet; if it was the shooter's last one, the shooter shrinks away.</summary>
        private void ShowShot(in Shot shot)
        {
            Vector2 from = path.Evaluate(shot.Distance);
            Vector3 to = boardView.GetCellWorldPosition(shot.X, shot.Y);
            bullets.Fire(new Vector3(from.x, from.y, to.z), to, level.GetColor(shot.Shooter.ColorIndex), shot.X, shot.Y);

            if (shot.ShooterEmptied)
            {
                ShooterView view = conveyorView.Remove(shot.Shooter); // also drops its skateboard
                view.CachedTransform.DOKill();
                view.CachedTransform.DOScale(0f, deathTime).SetEase(Ease.InBack).OnComplete(view.ReleaseCallback);
            }
        }

        /// <summary>
        /// A rider finished its lap with bullets left: it gets the leftmost free holder slot right
        /// now (the hop is only the animation). No free slot = the 6th arrival = lose.
        /// </summary>
        private void ReturnToHolder(Shooter shooter, ShooterView view)
        {
            Transform t = view.CachedTransform;
            t.DOKill();

            if (holder.TryAdd(shooter, out int slot))
            {
                holderView.Place(slot, view);
                t.DOJump(holderView.SlotPosition(slot), hopHeight, 1, hopTime);
                return;
            }

            if (lostView != null)
            {
                view.ReleaseToPool(); // already lost this frame; one shaking shooter is enough
                return;
            }
            lost = true;
            lostView = view;
            view.Shake();
            Debug.Log("LOST: a 6th shooter arrived at the full holder. Press R to restart.");
        }

        private bool IsPending(bool fromHolder, int index)
        {
            for (int i = 0; i < pendingCount; i++)
            {
                if (pending[i].FromHolder == fromHolder && pending[i].Index == index) return true;
            }
            return false;
        }

        private int CountPending(int column)
        {
            int count = 0;
            for (int i = 0; i < pendingCount; i++)
            {
                if (!pending[i].FromHolder && pending[i].Index == column) count++;
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
            holderView.Clear();
            holder.Reset();
            if (lostView != null)
            {
                lostView.ReleaseToPool();
                lostView = null;
            }
            lost = false;
            bullets.Clear();
            board.Reset(level);
            boardView.Build(board, level);
            BuildFiringPoints();
            queue.Reset(level);
            queueView.Build(queue, level);
        }

        // Firing points depend on where the board's cells are, so rebuild them for every level.
        private void BuildFiringPoints()
        {
            Vector3 firstCell = boardView.GetCellWorldPosition(0, 0);
            float cellSize = boardView.CellSize * boardView.transform.lossyScale.x;
            firingPoints.Build(path, firstCell.x, firstCell.y, cellSize, board.Width, board.Height);
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
    }
}
