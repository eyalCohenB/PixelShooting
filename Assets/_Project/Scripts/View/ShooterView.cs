using DG.Tweening;
using PixelShooting.Core.Queue;
using TMPro;
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
    ///
    /// Bullet bubble: a small badge with the bullets left, built in code in Init (no prefab work).
    /// It's a child of the shooter, so it follows hops/shakes/scaling for free. On the conveyor the
    /// shooter turns, so ConveyorView calls UpdateBubble each frame to keep it upright and at the
    /// same world offset. The number uses TMP's allocation-free SetText.
    /// </summary>
    public class ShooterView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock block;

        [Tooltip("The outline material; it gets a darker tint. Leave empty to tint everything the same.")]
        [SerializeField] private Material strokeMaterial;
        [Range(0f, 1f)]
        [SerializeField] private float strokeDarken = 0.6f;

        [Header("Bullet bubble (world units)")]
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0.45f, 0.5f, -0.6f);
        [SerializeField] private float bubbleSize = 0.5f;
        [SerializeField] private Color bubbleColor = Color.white;
        [SerializeField] private Color bubbleBorderColor = new Color(0.2f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color bubbleTextColor = new Color(0.2f, 0.15f, 0.1f, 1f);
        [SerializeField] private string bubbleSortingLayer = "FX";

        private Renderer[] renderers;
        private int[][] strokeSlots;   // per renderer: material indices that use the stroke material
        private int[] materialCounts;  // per renderer (cached: sharedMaterials allocates a new array on every call)
        private Transform cachedTransform;
        private ShooterViewPool pool;
        private TweenCallback releaseCallback; // cached once, so tweens can release without a new delegate
        private Transform bubble;
        private TextMeshPro bubbleText;

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

            BuildBubble();
        }

        // Badge = dark border square + white square (rounded sprite) + number, on the FX layer.
        private void BuildBubble()
        {
            int layer = SortingLayer.NameToID(bubbleSortingLayer);
            Sprite rounded = RoundedSprite.Get();
            float spriteSize = rounded.bounds.size.x;

            bubble = new GameObject("BulletBubble").transform;
            bubble.SetParent(cachedTransform, false);
            // Same world size whatever the shooter prefab's own scale.
            Vector3 parentScale = cachedTransform.lossyScale;
            bubble.localScale = new Vector3(bubbleSize / parentScale.x, bubbleSize / parentScale.y, bubbleSize / parentScale.z);

            AddBubbleSprite("Border", rounded, bubbleBorderColor, 1.18f / spriteSize, 0f, layer, 0);
            AddBubbleSprite("Fill", rounded, bubbleColor, 1f / spriteSize, -0.01f, layer, 1);

            var textGo = new GameObject("Count");
            textGo.transform.SetParent(bubble, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            bubbleText = textGo.AddComponent<TextMeshPro>();
            bubbleText.rectTransform.sizeDelta = new Vector2(0.9f, 0.8f);
            bubbleText.alignment = TextAlignmentOptions.Center;
            bubbleText.fontStyle = FontStyles.Bold;
            bubbleText.color = bubbleTextColor;
            bubbleText.enableAutoSizing = true;
            bubbleText.fontSizeMin = 1f;
            bubbleText.fontSizeMax = 7f;
            bubbleText.enableWordWrapping = false;
            bubbleText.sortingLayerID = layer;
            bubbleText.sortingOrder = 2;

            UpdateBubble();
        }

        private void AddBubbleSprite(string name, Sprite sprite, Color color, float scale, float z, int layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bubble, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }

        /// <summary>Shows how many bullets this shooter has left.</summary>
        public void SetBullets(int bullets) => bubbleText.SetText("{0}", bullets);

        /// <summary>Keeps the bubble upright and at the same world offset (call after rotating the shooter).</summary>
        public void UpdateBubble()
        {
            bubble.SetPositionAndRotation(cachedTransform.position + bubbleOffset, Quaternion.identity);
        }

        /// <summary>Attaches this view to a shooter and tints it.</summary>
        public void Bind(Shooter shooter, Color color)
        {
            Shooter = shooter;
            SetBullets(shooter.BulletsLeft);
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
