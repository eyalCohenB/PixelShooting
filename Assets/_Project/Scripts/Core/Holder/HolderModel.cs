using PixelShooting.Core.Queue;

namespace PixelShooting.Core.Holder
{
    /// <summary>
    /// The 5 holder slots below the conveyor. A shooter that finishes its lap with bullets left
    /// comes here; the player can send it back to the conveyor from here.
    ///
    /// - Slots are fixed: shooters keep their slot, there's no sliding to fill gaps.
    /// - A returning shooter gets the leftmost free slot, assigned the moment it exits the lap
    ///   (the hop is only the animation), so two returning shooters never aim at the same slot.
    /// - A slot is freed only when its shooter actually leaves (Take, when its hop starts), so
    ///   nothing can land on a shooter that is still waiting for the conveyor gate.
    /// - Lose rule: a shooter arriving while all 5 slots are taken (TryAdd returns false).
    /// </summary>
    public sealed class HolderModel
    {
        public const int Capacity = 5;

        private readonly Shooter[] slots = new Shooter[Capacity];

        public int Count { get; private set; }
        public bool IsFull => Count == Capacity;

        /// <summary>The shooter in a slot, or null if it's empty.</summary>
        public Shooter Get(int slot) => slots[slot];

        /// <summary>
        /// Puts a returning shooter in the leftmost free slot.
        /// </summary>
        /// <returns>False if every slot is taken: the 6th arrival, the player loses.</returns>
        public bool TryAdd(Shooter shooter, out int slot)
        {
            for (slot = 0; slot < Capacity; slot++)
            {
                if (slots[slot] == null)
                {
                    slots[slot] = shooter;
                    shooter.Location = ShooterLocation.Holder;
                    Count++;
                    return true;
                }
            }
            slot = -1;
            return false;
        }

        /// <summary>Takes a shooter out of its slot, when it starts its hop to the conveyor.</summary>
        public Shooter Take(int slot)
        {
            Shooter shooter = slots[slot];
            if (shooter == null)
            {
                throw new System.InvalidOperationException($"Holder slot {slot} is empty");
            }
            slots[slot] = null;
            shooter.Location = ShooterLocation.Conveyor;
            Count--;
            return shooter;
        }

        public void Reset()
        {
            System.Array.Clear(slots, 0, slots.Length);
            Count = 0;
        }
    }
}
