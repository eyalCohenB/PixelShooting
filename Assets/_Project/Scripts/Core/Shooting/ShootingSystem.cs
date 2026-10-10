using PixelShooting.Core.Board;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Queue;

namespace PixelShooting.Core.Shooting
{
    /// <summary>One shot fired this frame, for the view to animate.</summary>
    public struct Shot
    {
        public Shooter Shooter;
        public int X, Y;              // the target cell (already removed from the board)
        public float Distance;        // where on the path it was fired (the bullet's start)
        public bool ShooterEmptied;   // this was its last bullet: it's been taken off the conveyor
    }

    /// <summary>
    /// Fires for every rider that passed a firing point since last frame.
    ///
    /// Per point: the board is asked for the exposed cell of that line; if its color matches the
    /// shooter, the cell is removed right away (the logical removal is the reservation; the view
    /// pops it when the bullet lands) and the shooter loses a bullet. One shot per line per lap.
    /// A shooter that runs out of bullets is removed from the conveyor at once, freeing its place.
    ///
    /// Frame order: conveyor.Advance → Fire → conveyor.CollectExits, so points just before the
    /// lap end still fire on the frame the rider exits.
    /// </summary>
    public sealed class ShootingSystem
    {
        private readonly ConveyorModel conveyor;
        private readonly BoardModel board;
        private readonly FiringPoints points;

        public ShootingSystem(ConveyorModel conveyor, BoardModel board, FiringPoints points)
        {
            this.conveyor = conveyor;
            this.board = board;
            this.points = points;
        }

        /// <summary>
        /// Writes this frame's shots into 'shots' (caller-owned) and returns how many.
        /// If the buffer fills up, the remaining points simply fire next frame.
        /// </summary>
        public int Fire(Shot[] shots)
        {
            int count = 0;
            float fireLimit = conveyor.FireLimit; // the lap end, or the full path in loop mode

            for (int i = 0; i < conveyor.RiderCount; i++)
            {
                ref ConveyorRider rider = ref conveyor.RiderRef(i);

                while (rider.NextPoint < points.Count)
                {
                    FiringPoint point = points[rider.NextPoint];
                    if (point.Distance > rider.Distance || point.Distance >= fireLimit)
                    {
                        break; // not there yet, or past the lap end (never reached)
                    }
                    if (count == shots.Length)
                    {
                        return count;
                    }
                    rider.NextPoint++;

                    Shooter shooter = rider.Shooter;
                    if (!board.TryFire(point.Side, point.Line, shooter.ColorIndex, out int x, out int y))
                    {
                        continue; // empty line or blocked by another color
                    }

                    shooter.BulletsLeft--;
                    bool emptied = shooter.BulletsLeft <= 0;
                    shots[count++] = new Shot
                    {
                        Shooter = shooter,
                        X = x,
                        Y = y,
                        Distance = point.Distance,
                        ShooterEmptied = emptied,
                    };

                    if (emptied)
                    {
                        shooter.Location = ShooterLocation.Destroyed;
                        conveyor.RemoveAt(i); // 'rider' is no longer valid after this
                        i--;
                        break;
                    }
                }
            }
            return count;
        }
    }
}
