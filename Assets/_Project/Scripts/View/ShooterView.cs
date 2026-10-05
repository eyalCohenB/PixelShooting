using DG.Tweening;
using PixelShooting.Core.Queue;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Visual for one Shooter (goes on the ShooterView prefab root). The same view follows its
    /// shooter from the queue to the conveyor to the holder, and goes back to the pool when done.
    ///
    /// Color: set per instance with a MaterialPropertyBlock, so all shooters keep sharing the
    /// same materials (no material copies = no extra memory, batching kept). The outline
    /// (stroke) material gets a darker shade of the same color.
    /// </summary>
    public class ShooterView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock block;

        [Tooltip("The outline material; it gets a darker tint. Leave empty to tint everything the same.")]
        [SerializeField] private Material strokeMaterial;
        [Range(0f, 1f)]
        [SerializeField] private float strokeDarken = 0.6f;

        private Renderer[] renderers;
        private int[][] strokeSlots;   // per renderer: material indices that use the stroke material
        private int[] materialCounts;  // per renderer (cached: sharedMaterials allocates a new array on every call)
        private Transform cachedTransform;
        private ShooterViewPool pool;
        private TweenCallback releaseCallback; // cached once, so tweens can release without a new delegate

        public Shooter Shooter { get; private set; }
        public Transform CachedTransform => cachedTransform;

        /// <summary>Called once by the pool right after Instantiate.</summary>
        public void Init(ShooterViewPool owner)
        {
            pool = owner;
            cachedTransform = transform;
            releaseCallback = ReleaseToPool;
            block ??= new MaterialPropertyBlock();

            renderers = GetComponentsInChildren<Renderer>(true);
            strokeSlots = new int[renderers.Length][];
            materialCounts = new int[renderers.Length];
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] materials = renderers[r].sharedMaterials;
                materialCounts[r] = materials.Length;
                int count = 0;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == strokeMaterial) count++;
                }
                var slots = new int[count];
                count = 0;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == strokeMaterial) slots[count++] = m;
                }
                strokeSlots[r] = slots;
            }
        }

        /// <summary>Attaches this view to a shooter and tints it.</summary>
        public void Bind(Shooter shooter, Color color)
        {
            Shooter = shooter;
            Color stroke = new Color(color.r * strokeDarken, color.g * strokeDarken, color.b * strokeDarken, color.a);

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer target = renderers[r];
                for (int m = 0; m < materialCounts[r]; m++)
                {
                    block.SetColor(ColorId, IsStroke(r, m) ? stroke : color);
                    target.SetPropertyBlock(block, m);
                }
            }
        }

        /// <summary>Small "no" shake (conveyor full).</summary>
        public void Shake()
        {
            cachedTransform.DOComplete();
            cachedTransform.DOShakePosition(0.3f, new Vector3(0.15f, 0f, 0f), 20, 0f);
        }

        /// <summary>Use as a tween's OnComplete to return this view to the pool (no allocation).</summary>
        public TweenCallback ReleaseCallback => releaseCallback;

        public void ReleaseToPool() => pool.Release(this);

        internal void ClearShooter() => Shooter = null;

        private bool IsStroke(int rendererIndex, int materialIndex)
        {
            int[] slots = strokeSlots[rendererIndex];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == materialIndex) return true;
            }
            return false;
        }
    }
}
