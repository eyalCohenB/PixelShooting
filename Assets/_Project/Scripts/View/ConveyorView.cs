using System.Collections.Generic;
using PixelShooting.Core.Conveyor;
using PixelShooting.Core.Queue;
using TMPro;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Places rider views on the path each frame. The model decides; this only draws.
    /// Each rider gets a pooled skateboard while it's on the conveyor.
    /// </summary>
    public class ConveyorView : MonoBehaviour
    {
        [SerializeField] private TMP_Text capacityLabel;
        [SerializeField] private float shooterZ = -1f;
        [SerializeField] private SkateboardPool skateboards;

        private readonly Dictionary<Shooter, ShooterView> views =
            new Dictionary<Shooter, ShooterView>(ConveyorModel.Capacity);
        private readonly Dictionary<Shooter, SkateboardView> boards =
            new Dictionary<Shooter, SkateboardView>(ConveyorModel.Capacity);
        private ConveyorPath path;
        private Quaternion baseRotation;
        private bool hasBaseRotation;
        private int shownFree = -1;

        public void Init(ConveyorPath path)
        {
            this.path = path;
        }

        /// <summary>Called together with model.Enter, when the hop starts.</summary>
        public void Add(Shooter shooter, ShooterView view)
        {
            if (!hasBaseRotation)
            {
                baseRotation = view.CachedTransform.rotation;
                hasBaseRotation = true; 
            }
            views.Add(shooter, view);

            SkateboardView board = skateboards.Get();
            board.AttachTo(view.CachedTransform);
            boards.Add(shooter, board);
        }

        /// <summary>
        /// Called for each shooter that leaves the conveyor (lap done, or destroyed in stage 6).
        /// Drops its skateboard and returns its view.
        /// </summary>
        public ShooterView Remove(Shooter shooter)
        {
            if (!views.Remove(shooter, out ShooterView view))
                throw new System.InvalidOperationException("Shooter not found in views");
            if (boards.Remove(shooter, out SkateboardView board))
                board.Detach();
            view.CachedTransform.rotation = baseRotation;
            return view;
        }

        /// <summary>Call every frame, after model.Update.</summary>
        public void Sync(ConveyorModel model)
        {
            for (int i = 0; i < model.RiderCount ; i++)
            {
                ConveyorRider r = model.GetRider(i);
                if (r.Distance < 0f) continue; // still hopping on; DOTween moves it
                ShooterView v = views[r.Shooter];
                Vector2 p = path.Evaluate(r.Distance);
                v.CachedTransform.position = new Vector3(p.x, p.y, shooterZ);
            
                Vector2 t = path.Evaluate(r.Distance + 0.01f) - p; //direction of travel
                if (t.sqrMagnitude > 0)
                {
                Vector2 n = new Vector2(-t.y, t.x); //direection turned left 90 degrees
                float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 90f;
                v.CachedTransform.rotation = Quaternion.Euler(0, 0, angle) * baseRotation;
                }
            }
            if(model.Free != shownFree){
                capacityLabel.SetText("{0}/5", model.Free);
                shownFree = model.Free;
            }
        }

        public void Clear()
        {
            views.Clear();
            boards.Clear(); // callers Remove every rider first, so this is only a safety net
            shownFree = -1;
        }
    }
}
