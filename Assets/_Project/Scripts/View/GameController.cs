using System.Collections.Generic;
using DG.Tweening;
using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Holder;
using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue;
using PixelShooting.Core.Shooting;
using TMPro;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Runs the game: level loading, input, the per-frame gameplay loop and win/lose.
    /// (Grew out of the stage 4-7 GameplayTester; decision 1B kept it as one MonoBehaviour.)
    ///
    /// Tap a column's top shooter or a holder shooter: it reserves a conveyor place (or shakes
    /// if full), waits in place until the conveyor gate opens, then hops on and rides one lap,
    /// firing at every row/column middle it passes. A shooter that uses its last bullet shrinks
    /// away; one with bullets left hops to the leftmost free holder slot after its lap.
    ///
    /// Win: the board is cleared (bullets == cells, so every shooter is used up at that moment
    /// too); the popup waits until no bullet is in flight and the last pops have played.
    /// Lose: a 6th shooter arrives at the full holder; the popup follows its shake.
    ///
    /// Loop finale (Part 3 bonus): when the shooters still alive drop to loopShooterCount (5),
    /// input locks, every holder/queue shooter boards one by one through the normal gate, and the
    /// conveyor loops at speed x loopSpeedMultiplier until the board is clear. It always wins:
    /// bullets == cells per color, so every exposed cell has a matching shooter on the loop.
    /// Levels that start with 5 or fewer shooters never trigger it (only a drop does).
    /// Editor-only keys: 1/2/3 load a level, R restart.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private QueueView queueView;
        [SerializeField] private ConveyorView conveyorView;
        [SerializeField] private BulletSystem bullets;
        [SerializeField] private HolderView holderView;

        [Header("HUD")]
        [SerializeField] private GamePopup popup;
        [SerializeField] private TMP_Text levelLabel;
        [Tooltip("After the last bullet lands: time for the last pop / shrink to play.")]
        [SerializeField] private float winPopupDelay = 0.5f;
        [Tooltip("After the 6th shooter arrives: time for its shake to play.")]
        [SerializeField] private float losePopupDelay = 0.5f;

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

        [Header("Loop finale (Part 3)")]
        [Tooltip("When this many shooters (or fewer) are left, the finale starts.")]
        [SerializeField] private int loopShooterCount = 5;
        [Tooltip("Conveyor speed multiplier during the finale.")]
        [SerializeField] private int loopSpeedMultiplier = 3;

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
        private ShooterView lostView; // the 6th shooter, shaking where it got off

        private enum GameState : byte { Playing, Won, Lost }
        private GameState state;
        private float popupTimer;
        private int levelIndex;

        private int aliveShooters;   // not destroyed yet (queue + pending + conveyor + holder)
        private bool loopArmed;      // the level started with more than loopShooterCount
        private bool looping;

        // A tap that reserved a conveyor place; its shooter still waits in place
        // (on top of a queue column, or in a holder slot) until the conveyor gate opens.
        private struct PendingTap
        {
            public bool FromHolder;
            public int Index; // queue column or holder slot
        }

        private readonly PendingTap[] pending = new PendingTap[ConveyorModel.Capacity]; // FIFO
        private int pendingCount;

        private void Awake()
        {
            // Preallocate DOTween's tween pools once (recycling + safe mode), so gameplay never grows them.
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(200, 50);
            popup.Init();
            popup.Button.onClick.AddListener(OnPopupButton); // once: no per-show allocation
        }

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
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Alpha1)) Load(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Load(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Load(2);
            if (Input.GetKeyDown(KeyCode.R)) Restart();
#endif
            if (level == null) return;
            if (state != GameState.Playing)
            {
                UpdateEnd();
                return;
            }

            if (Input.GetMouseButtonDown(0)) OnTap(Input.mousePosition);
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

            if (loopArmed && !looping && aliveShooters <= loopShooterCount)
            {
                StartLoopFinale();
            }

            if (state == GameState.Playing && board.IsCleared)
            {
                EndLevel(GameState.Won);
            }
        }

        /// <summary>
        /// Part 3: lock input, speed the conveyor up, switch it to looping, and queue every
        /// remaining holder/queue shooter to board one by one through the normal gate.
        /// At most loopShooterCount (5 = conveyor capacity) are alive, so every reservation fits.
        /// </summary>
        private void StartLoopFinale()
        {
            looping = true;
            conveyor.StartLooping(speed * loopSpeedMultiplier);

            for (int slot = 0; slot < HolderModel.Capacity; slot++)
            {
                if (holder.Get(slot) == null || IsPending(true, slot)) continue;
                QueueBoarding(true, slot);
            }
            for (int column = 0; column < queue.ColumnCount; column++)
            {
                int waiting = queue.Remaining(column) - CountPending(column);
                for (int k = 0; k < waiting; k++)
                {
                    QueueBoarding(false, column);
                }
            }
        }

        private void QueueBoarding(bool fromHolder, int index)
        {
            if (!conveyor.TryReserve())
            {
                Debug.LogError("Loop finale: no conveyor place left (should be impossible).", this);
                return;
            }
            pending[pendingCount++] = new PendingTap { FromHolder = fromHolder, Index = index };
        }

        private void EndLevel(GameState result)
        {
            state = result;
            popupTimer = 0f;
        }

        // After a win or loss: wait for the last animations, then show the popup once.
        private void UpdateEnd()
        {
            if (popup.IsShown) return;
            if (state == GameState.Won && bullets.InFlight > 0) return; // cells still popping
            popupTimer += Time.deltaTime;
            if (popupTimer >= (state == GameState.Won ? winPopupDelay : losePopupDelay))
            {
                popup.Show(state == GameState.Won);
            }
        }

        private void OnPopupButton()
        {
            if (state == GameState.Won) Load(levelIndex + 1); // after the last level, wraps to the first
            else Restart();
        }

        private void OnTap(Vector3 screenPosition)
        {
            if (looping) return; // the finale plays itself
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
            conveyorView.GetView(shot.Shooter).SetBullets(shot.Shooter.BulletsLeft);

            Vector2 from = path.Evaluate(shot.Distance);
            Vector3 to = boardView.GetCellWorldPosition(shot.X, shot.Y);
            bullets.Fire(new Vector3(from.x, from.y, to.z), to, level.GetColor(shot.Shooter.ColorIndex), shot.X, shot.Y);

            if (shot.ShooterEmptied)
            {
                aliveShooters--;
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
            lostView = view;
            view.Shake();
            EndLevel(GameState.Lost);
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
            // Wraps past the last level, and skips any level the validator rejects (logged),
            // so a bad entry in the catalog can never leave the game without a level.
            for (int attempt = 0; attempt < catalog.Count; attempt++)
            {
                int i = (index + attempt) % catalog.Count;
                TextAsset file = catalog.Get(i);
                errors.Clear();
                warnings.Clear();
                // Load into a local: a failed TryLoad must not wipe the current level.
                if (!LevelLoader.TryLoad(file.text, file.name, out LevelData loaded, errors, warnings))
                {
                    Debug.LogError(string.Join("\n", errors), file);
                    continue;
                }
                level = loaded;
                levelIndex = i;
                levelLabel.SetText("Level {0}", i + 1);
                Restart();
                return;
            }
            Debug.LogError("No valid level in the catalog.", catalog);
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
            state = GameState.Playing;
            popup.HideNow();
            bullets.Clear();
            board.Reset(level);
            boardView.Build(board, level);
            BuildFiringPoints();
            queue.Reset(level);
            queueView.Build(queue, level);

            aliveShooters = 0;
            for (int column = 0; column < queue.ColumnCount; column++)
            {
                aliveShooters += queue.Remaining(column);
            }
            loopArmed = aliveShooters > loopShooterCount; // only a drop to the threshold triggers it
            looping = false;
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
