using PixelShooting.Core.Queue;

namespace PixelShooting.Core.Conveyor
{
    /// <summary>
    /// A shooter on the conveyor and how far along the path it is.
    /// A negative distance means it's still hopping onto the start (see ConveyorModel).
    /// Named ConveyorRider, not Rider: Unity defines a global "Rider" namespace that hides a type with that name.
    /// </summary>
    public struct ConveyorRider
    {
        public Shooter Shooter;
        public float Distance;
        /// <summary>Index of the next firing point this rider will reach (see Shooting.FiringPoints).</summary>
        public int NextPoint;
    }

    /// <summary>
    /// Capacity, riders and the entry gate. No overlap by construction:
    /// - All riders move at the same speed, so the gaps between them never change.
    /// - A tap only reserves a place. The shooter waits in place (queue top / holder slot)
    ///   until CanEnter, so nothing ever waits on the track.
    /// - The hop counts as part of the ride: a rider enters at Distance = -approach
    ///   (approach = speed * hopTime) and reaches 0 exactly when the hop lands.
    /// - The gate checks the gap around the loop to every rider, ahead of the entry point and
    ///   behind it (a rider coming round the end of the lap).
    /// - The lap ends at LapLength, which can be a bit before the end of the path (the riders
    ///   get off on the left side, just above the entrance, and hop away).
    /// - Loop mode (Part 3 bonus): riders never get off; at the end of the path they wrap to 0
    ///   and start a new lap (firing points start over), at a higher speed. Wrapping keeps the
    ///   gaps around the loop unchanged, so it can't cause an overlap either.
    /// </summary>
    public sealed class ConveyorModel
    {
        public const int Capacity = 5;

        private readonly ConveyorPath path;
        private readonly float baseSpeed;
        private float speed;
        private readonly float minSpacing;
        private readonly float approach;
        private readonly float lapLength;

        // Entry order: [0] = oldest. Before loop mode that's also the furthest along.
        private readonly ConveyorRider[] riders = new ConveyorRider[Capacity];
        private int riderCount;
        private int reserved; // riders + reservations still waiting in place

        public int Free => Capacity - reserved;
        public int RiderCount => riderCount;
        /// <summary>Reservations whose shooter hasn't entered yet (still waiting in place).</summary>
        public int Pending => reserved - riderCount;
        /// <summary>How long the hop onto the start must take, so it lands exactly at distance 0.</summary>
        public float HopDuration => approach / speed;
        /// <summary>Distance along the path where a rider's lap ends and it gets off.</summary>
        public float LapLength => lapLength;
        /// <summary>Loop mode: riders keep lapping until their bullets run out (Part 3 bonus).</summary>
        public bool Looping { get; private set; }
        /// <summary>Firing points at or past this distance are never fired (riders don't get there).</summary>
        public float FireLimit => Looping ? path.Length : lapLength;
        public ConveyorRider GetRider(int i) => riders[i];
        /// <summary>Direct access to a rider, so the shooting system can advance NextPoint in place.</summary>
        public ref ConveyorRider RiderRef(int i) => ref riders[i];

        /// <param name="lapLength">Where the lap ends; clamped to (0, path.Length].</param>
        public ConveyorModel(ConveyorPath path, float lapLength, float speed, float minSpacing, float hopTime)
        {
            this.path = path;
            this.lapLength = lapLength > 0f && lapLength < path.Length ? lapLength : path.Length;
            baseSpeed = speed;
            this.speed = speed;
            this.minSpacing = minSpacing;
            approach = speed * hopTime;
        }

        /// <summary>Called on tap. False = full (the caller shakes the shooter).</summary>
        public bool TryReserve()
        {
            if (Free <= 0) return false;
            reserved++;
            return true;
        }

        /// <summary>True when a reserved shooter may start its hop now without ever overlapping.</summary>
        public bool CanEnter
        {
            get
            {
                if (Pending <= 0) return false;

                // The new rider starts at -approach. Every rider must be at least minSpacing from
                // it around the loop, both ahead (forward gap) and behind (coming round the end).
                // Gaps never change afterwards (same speed; wrapping keeps loop distances), so
                // checking once, now, is enough. Measured on the full path, not LapLength, so it
                // is safe whether riders get off early or loop.
                float length = path.Length;
                for (int i = 0; i < riderCount; i++)
                {
                    float ahead = (riders[i].Distance + approach) % length;
                    if (ahead < 0f) ahead += length;
                    if (ahead < minSpacing || length - ahead < minSpacing) return false;
                }
                return true;
            }
        }

        /// <summary>Starts a reserved shooter's ride. Call only when CanEnter; then start a hop of HopDuration.</summary>
        public void Enter(Shooter shooter)
        {
            if (!CanEnter) throw new System.InvalidOperationException("Enter called while the gate is closed");
            riders[riderCount++] = new ConveyorRider { Shooter = shooter, Distance = -approach };
        }

        /// <summary>
        /// Moves riders forward, then writes the shooters that finished their lap into 'exited'.
        /// Same as Advance + CollectExits; use those two when shooting must happen in between.
        /// </summary>
        public int Update(float dt, Shooter[] exited)
        {
            Advance(dt);
            return CollectExits(exited);
        }

        /// <summary>Moves every rider forward by speed * dt.</summary>
        public void Advance(float dt)
        {
            float step = speed * dt;
            for (int i = 0; i < riderCount; i++)
            {
                riders[i].Distance += step;
            }
        }

        /// <summary>
        /// Switches to loop mode at a new speed (Part 3). The approach distance stays the same,
        /// so HopDuration gets shorter and hops still land exactly at distance 0.
        /// </summary>
        public void StartLooping(float newSpeed)
        {
            Looping = true;
            speed = newSpeed;
        }

        /// <summary>
        /// Removes riders that reached LapLength and writes their shooters into 'exited'
        /// (size >= Capacity, owned by the caller). Returns how many exited.
        /// In loop mode nobody exits: riders at the end of the path wrap to a new lap instead.
        /// </summary>
        public int CollectExits(Shooter[] exited)
        {
            if (Looping)
            {
                float length = path.Length;
                for (int i = 0; i < riderCount; i++)
                {
                    if (riders[i].Distance < length) continue;
                    riders[i].Distance -= length;
                    riders[i].NextPoint = 0;
                }
                return 0;
            }

            int exitCount = 0;
            // The oldest is always the furthest along, so exits only ever happen at index 0.
            while (riderCount > 0 && riders[0].Distance >= lapLength)
            {
                exited[exitCount++] = riders[0].Shooter;
                RemoveAt(0);
            }
            return exitCount;
        }

        /// <summary>
        /// Takes a rider off the conveyor (lap done, or out of bullets) and frees its place.
        /// Removing a rider only makes a gap bigger, so it can never cause an overlap.
        /// </summary>
        public void RemoveAt(int index)
        {
            for (int j = index + 1; j < riderCount; j++)
            {
                riders[j - 1] = riders[j];
            }
            riderCount--;
            riders[riderCount] = default; // don't keep a reference to the shooter
            reserved--;
        }

        public void Reset()
        {
            System.Array.Clear(riders, 0, riders.Length);
            riderCount = 0;
            reserved = 0;
            Looping = false;
            speed = baseSpeed;
        }
    }
}
