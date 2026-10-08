using PixelShooting.Core.Holder;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Shows the holder: which ShooterView sits in which of the 5 slot transforms (Holder_0..4).
    /// The model decides the slots; this only keeps the views and answers taps (plain math, no physics).
    /// </summary>
    public class HolderView : MonoBehaviour
    {
        [Tooltip("Holder_0..4, left to right.")]
        [SerializeField] private Transform[] slots = new Transform[HolderModel.Capacity];
        [Tooltip("How close (world units) a tap must be to a slot's centre.")]
        [SerializeField] private float pickRadius = 0.8f;
        [Tooltip("Z of shooters: negative = in front of the sprites.")]
        [SerializeField] private float shooterZ = -1f;

        private readonly ShooterView[] views = new ShooterView[HolderModel.Capacity];

        /// <summary>Where a shooter stands in a slot.</summary>
        public Vector3 SlotPosition(int slot)
        {
            Vector3 p = slots[slot].position;
            p.z = shooterZ;
            return p;
        }

        /// <summary>Registers the view that is hopping into a slot (call right after HolderModel.TryAdd).</summary>
        public void Place(int slot, ShooterView view) => views[slot] = view;

        /// <summary>Takes the view out of a slot (call together with HolderModel.Take).</summary>
        public ShooterView TakeView(int slot)
        {
            ShooterView view = views[slot];
            views[slot] = null;
            return view;
        }

        /// <summary>Finds the occupied slot under a tap.</summary>
        public bool TryPickSlot(Vector3 worldPoint, out int slot)
        {
            float maxSqr = pickRadius * pickRadius;
            for (slot = 0; slot < slots.Length; slot++)
            {
                if (views[slot] == null) continue;
                Vector3 p = slots[slot].position;
                float dx = worldPoint.x - p.x;
                float dy = worldPoint.y - p.y;
                if (dx * dx + dy * dy <= maxSqr) return true;
            }
            slot = -1;
            return false;
        }

        public void Shake(int slot)
        {
            if (views[slot] != null) views[slot].Shake();
        }

        /// <summary>Returns every holder view to the pool (restart / level load).</summary>
        public void Clear()
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null) continue;
                views[i].ReleaseToPool();
                views[i] = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) Gizmos.DrawWireSphere(slots[i].position, pickRadius);
            }
        }
#endif
    }
}
