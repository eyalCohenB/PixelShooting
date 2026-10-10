using System.Collections.Generic;
using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Holder;
using PixelShooting.Core.Queue;
using PixelShooting.Core.Shooting;

namespace PixelShooting.Core.Hint
{
    /// <summary>
    /// Suggests the next shooter to tap, by searching for a winning order from the live state.
    ///
    /// Model: "sequential laps". A move sends one shooter round one lap on its own: it fires at
    /// the firing points in order (same rules as ShootingSystem) until its bullets run out; if
    /// any are left it goes to the leftmost free holder slot (no free slot = a lost branch).
    /// The real game lets laps overlap, but any order that wins sequentially is a valid plan, and
    /// this keeps the search exact and cheap: a lap is just a walk over the firing-point list.
    ///
    /// Live state: riders already on the conveyor finish their lap first (from their NextPoint),
    /// then taps that are already pending, then the search starts.
    ///
    /// Search: depth-first, trying the move that hits the most cells first (so a winning line is
    /// usually found on the first dive), with a node budget. If the budget runs out, or no
    /// winning line exists, it falls back to that greedy best move.
    ///
    /// Runs only when the player asks (hint button), never per frame. Its buffers are kept and
    /// grown per level, so repeated hints allocate nothing.
    /// </summary>
    public sealed class HintSolver
    {
        private const int Slots = HolderModel.Capacity;

        /// <summary>Max search nodes per hint (each node = one simulated lap).</summary>
        public int NodeBudget { get; set; } = 50000;

        // Per search depth: a board snapshot, queue offsets (shooters taken per column),
        // and the holder (color/bullets per slot, color -1 = empty).
        private readonly List<BoardModel> boards = new List<BoardModel>();
        private readonly List<int[]> taken = new List<int[]>();
        private readonly List<int[]> holderColor = new List<int[]>();
        private readonly List<int[]> holderBullets = new List<int[]>();
        private readonly List<TapSource[]> moveBuffers = new List<TapSource[]>(); // candidate moves per depth
        private readonly List<int[]> scoreBuffers = new List<int[]>();
        private readonly BoardModel scratch = new BoardModel();

        private FiringPoints points;
        private float lapLength;
        private QueueModel queue;
        private int nodes;
        private TapSource firstMove;

        /// <summary>
        /// Finds the next tap. Returns false if there's nothing to tap or the current state is
        /// already lost. 'solved' = a full winning line was found (else 'hint' is the greedy move).
        /// </summary>
        public bool TryGetHint(BoardModel board, FiringPoints firingPoints, float lap, QueueModel queueModel,
            HolderModel holder, ConveyorModel conveyor, TapSource[] pending, int pendingCount,
            out TapSource hint, out bool solved)
        {
            points = firingPoints;
            lapLength = lap;
            queue = queueModel;
            hint = default;
            solved = false;
            Ensure(0);

            // Root = the live state.
            BoardModel root = boards[0];
            root.CopyFrom(board);
            int[] rootTaken = taken[0];
            for (int c = 0; c < rootTaken.Length; c++) rootTaken[c] = 0;
            int[] color = holderColor[0];
            int[] bullets = holderBullets[0];
            for (int s = 0; s < Slots; s++)
            {
                Shooter shooter = holder.Get(s);
                color[s] = shooter == null ? -1 : shooter.ColorIndex;
                bullets[s] = shooter == null ? 0 : shooter.BulletsLeft;
            }

            // Riders on the conveyor finish their lap, oldest first.
            for (int i = 0; i < conveyor.RiderCount; i++)
            {
                ConveyorRider rider = conveyor.GetRider(i);
                int left = RunLap(root, rider.Shooter.ColorIndex, rider.Shooter.BulletsLeft, rider.NextPoint);
                if (left > 0 && !AddToHolder(color, bullets, rider.Shooter.ColorIndex, left)) return false;
            }

            // Taps already pending go next, in order.
            for (int i = 0; i < pendingCount; i++)
            {
                if (!ApplyMove(root, rootTaken, color, bullets, pending[i])) return false;
            }

            if (root.IsCleared) return false; // already won: nothing to hint

            nodes = 0;
            if (!BestFirstMove(out TapSource greedy)) return false; // nothing left to tap
            solved = Search(0);
            hint = solved ? firstMove : greedy;
            return true;
        }

        // Depth-first: returns true if a winning line exists from depth d; records its first move.
        private bool Search(int d)
        {
            if (boards[d].IsCleared) return true;
            if (nodes >= NodeBudget) return false;
            Ensure(d + 1);

            // Try moves in order of immediate hits (most first).
            int moveCount = CollectMoves(d);
            TapSource[] moves = moveBuffers[d];
            for (int k = 0; k < moveCount; k++)
            {
                TapSource move = moves[k];
                nodes++;
                CopyState(d, d + 1);
                if (!ApplyMove(boards[d + 1], taken[d + 1], holderColor[d + 1], holderBullets[d + 1], move)) continue;
                if (Search(d + 1))
                {
                    if (d == 0) firstMove = move;
                    return true;
                }
                if (nodes >= NodeBudget) return false;
            }
            return false;
        }

        // Fills moveBuffers[d] with the distinct moves from state d, sorted by immediate hits
        // (descending). Identical holder shooters (same color and bullets) are one move.
        private int CollectMoves(int d)
        {
            TapSource[] moves = moveBuffers[d];
            int[] score = scoreBuffers[d];
            int[] color = holderColor[d];
            int[] bullets = holderBullets[d];
            int count = 0;

            for (int c = 0; c < queue.ColumnCount; c++)
            {
                Shooter next = queue.GetAt(c, taken[d][c]);
                if (next == null) continue;
                moves[count] = TapSource.Queue(c);
                score[count++] = Hits(d, next.ColorIndex, next.BulletsLeft);
            }
            for (int s = 0; s < Slots; s++)
            {
                if (color[s] < 0) continue;
                bool duplicate = false;
                for (int t = 0; t < s; t++)
                {
                    if (color[t] == color[s] && bullets[t] == bullets[s]) { duplicate = true; break; }
                }
                if (duplicate) continue;
                moves[count] = TapSource.Holder(s);
                score[count++] = Hits(d, color[s], bullets[s]);
            }

            // Insertion sort, descending by score (stable: queue before holder on ties).
            for (int i = 1; i < count; i++)
            {
                TapSource mv = moves[i];
                int sc = score[i];
                int j = i - 1;
                while (j >= 0 && score[j] < sc) { moves[j + 1] = moves[j]; score[j + 1] = score[j]; j--; }
                moves[j + 1] = mv;
                score[j + 1] = sc;
            }
            return count;
        }

        // Greedy fallback: the move with the most immediate hits that doesn't lose on the spot.
        private bool BestFirstMove(out TapSource best)
        {
            int count = CollectMoves(0);
            TapSource[] moves = moveBuffers[0];
            best = moves[0];
            if (count == 0) return false;
            Ensure(1);
            for (int k = 0; k < count; k++)
            {
                CopyState(0, 1);
                if (ApplyMove(boards[1], taken[1], holderColor[1], holderBullets[1], moves[k]))
                {
                    best = moves[k];
                    return true;
                }
            }
            return true; // every move overflows the holder: still point at the best-scoring one
        }

        // Plays one move on a state. False = the shooter came back to a full holder (lost).
        private bool ApplyMove(BoardModel board, int[] takenCols, int[] color, int[] bullets, TapSource move)
        {
            int shooterColor, shooterBullets;
            if (move.FromHolder)
            {
                shooterColor = color[move.Index];
                shooterBullets = bullets[move.Index];
                color[move.Index] = -1; // the slot frees when it leaves
                bullets[move.Index] = 0;
            }
            else
            {
                Shooter next = queue.GetAt(move.Index, takenCols[move.Index]);
                if (next == null) return false;
                takenCols[move.Index]++;
                shooterColor = next.ColorIndex;
                shooterBullets = next.BulletsLeft;
            }

            int left = RunLap(board, shooterColor, shooterBullets, 0);
            return left == 0 || AddToHolder(color, bullets, shooterColor, left);
        }

        // One lap from a firing point: same rules as ShootingSystem. Returns bullets left.
        private int RunLap(BoardModel board, int color, int bullets, int fromPoint)
        {
            for (int p = fromPoint; p < points.Count && bullets > 0; p++)
            {
                FiringPoint point = points[p];
                if (point.Distance >= lapLength) break;
                if (board.TryFire(point.Side, point.Line, (byte)color, out _, out _)) bullets--;
            }
            return bullets;
        }

        // How many cells a shooter would hit in one lap from state d (played on a scratch board).
        private int Hits(int d, int color, int bullets)
        {
            scratch.CopyFrom(boards[d]);
            return bullets - RunLap(scratch, color, bullets, 0);
        }

        private static bool AddToHolder(int[] color, int[] bullets, int shooterColor, int left)
        {
            for (int s = 0; s < Slots; s++)
            {
                if (color[s] >= 0) continue;
                color[s] = shooterColor;
                bullets[s] = left;
                return true;
            }
            return false; // 6th arrival
        }

        private void CopyState(int from, int to)
        {
            boards[to].CopyFrom(boards[from]);
            System.Array.Copy(taken[from], taken[to], taken[from].Length);
            System.Array.Copy(holderColor[from], holderColor[to], Slots);
            System.Array.Copy(holderBullets[from], holderBullets[to], Slots);
        }

        // Grows the per-depth buffers (only when a deeper search than ever before is needed).
        private void Ensure(int depth)
        {
            while (boards.Count <= depth)
            {
                boards.Add(new BoardModel());
                taken.Add(new int[queue.ColumnCount]);
                holderColor.Add(new int[Slots]);
                holderBullets.Add(new int[Slots]);
                moveBuffers.Add(new TapSource[queue.ColumnCount + Slots]);
                scoreBuffers.Add(new int[queue.ColumnCount + Slots]);
            }
            for (int d = 0; d <= depth; d++)
            {
                if (taken[d].Length == queue.ColumnCount) continue; // sized for another level
                taken[d] = new int[queue.ColumnCount];
                moveBuffers[d] = new TapSource[queue.ColumnCount + Slots];
                scoreBuffers[d] = new int[queue.ColumnCount + Slots];
            }
        }
    }
}
