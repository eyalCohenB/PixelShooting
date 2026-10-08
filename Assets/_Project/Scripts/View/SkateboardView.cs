using DG.Tweening;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// One pooled skateboard. While a shooter rides the conveyor, the board is a child of the
    /// shooter, so it follows its position and rotation with no per-frame code.
    /// </summary>
    public class SkateboardView : MonoBehaviour
    {
        private SkateboardPool pool;
        private Transform cachedTransform;
        private TweenCallback releaseCallback; // cached: no closure allocation per tween

        public void Init(SkateboardPool owner)
        {
            pool = owner;
            cachedTransform = transform;
            releaseCallback = ReleaseToPool;
        }

        /// <summary>Puts the board under the shooter's feet and scales it in.</summary>
        public void AttachTo(Transform shooter)
        {
            cachedTransform.DOKill();
            cachedTransform.SetParent(shooter, false);
            cachedTransform.localPosition = pool.LocalPosition;
            cachedTransform.localRotation = Quaternion.Euler(pool.LocalEuler);
            cachedTransform.localScale = Vector3.zero;
            cachedTransform.DOScale(pool.LocalScale, pool.TweenTime);
        }

        /// <summary>
        /// Leaves the board where it is (the shooter hops away without it), scales it out,
        /// then returns it to the pool.
        /// </summary>
        public void Detach()
        {
            cachedTransform.DOKill();
            cachedTransform.SetParent(pool.transform, true); // keep the world pose
            cachedTransform.DOScale(0f, pool.TweenTime).OnComplete(releaseCallback);
        }

        private void ReleaseToPool() => pool.Release(this);
    }
}
