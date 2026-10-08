using System.Collections.Generic;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Pooled bullets, moved in one plain update loop (not DOTween: many short-lived flights,
    /// no tween objects or callbacks needed). When a bullet arrives, its target cell pops.
    ///
    /// The cell was already removed from BoardModel when the shot was fired, so the logic never
    /// waits for bullets; this is only the picture catching up.
    /// </summary>
    public class BulletSystem : MonoBehaviour
    {
        private struct Flight
        {
            public SpriteRenderer Renderer;
            public Transform Transform;
            public Vector3 From, To;
            public float Elapsed, Duration;
            public int X, Y;
        }

        [SerializeField] private BoardView boardView;
        [SerializeField] private Sprite sprite;
        [SerializeField] private float size = 0.25f;
        [Tooltip("World units per second.")]
        [SerializeField] private float speed = 25f;
        [SerializeField] private string sortingLayer = "FX";
        [SerializeField] private int prewarm = 32;

        private readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>(64);
        private Flight[] flights = new Flight[32];
        private int flightCount;
        private int sortingLayerId;
        private Vector3 scale;

        /// <summary>Bullets still in the air (the win flow must wait for 0).</summary>
        public int InFlight => flightCount;

        private void Awake()
        {
            sortingLayerId = SortingLayer.NameToID(sortingLayer);
            float spriteSize = sprite.bounds.size.x;
            scale = new Vector3(size / spriteSize, size / spriteSize, 1f);
            for (int i = 0; i < prewarm; i++)
            {
                free.Push(Create());
            }
        }

        public void Fire(Vector3 from, Vector3 to, Color color, int x, int y)
        {
            SpriteRenderer renderer = free.Count > 0 ? free.Pop() : CreateWithWarning();
            renderer.color = color;
            Transform t = renderer.transform;
            t.position = from;
            renderer.gameObject.SetActive(true);

            if (flightCount == flights.Length)
            {
                System.Array.Resize(ref flights, flights.Length * 2); // rare: only if more than ever before
            }
            flights[flightCount++] = new Flight
            {
                Renderer = renderer,
                Transform = t,
                From = from,
                To = to,
                Elapsed = 0f,
                Duration = Mathf.Max(0.01f, Vector3.Distance(from, to) / speed),
                X = x,
                Y = y,
            };
        }

        /// <summary>Removes every bullet without popping cells (restart / level load).</summary>
        public void Clear()
        {
            for (int i = 0; i < flightCount; i++)
            {
                Release(flights[i].Renderer);
                flights[i] = default;
            }
            flightCount = 0;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // Backwards, so a finished flight can be swapped with the last one and removed.
            for (int i = flightCount - 1; i >= 0; i--)
            {
                ref Flight f = ref flights[i];
                f.Elapsed += dt;
                float t = f.Elapsed / f.Duration;
                if (t < 1f)
                {
                    f.Transform.position = Vector3.LerpUnclamped(f.From, f.To, t);
                    continue;
                }

                boardView.Pop(f.X, f.Y);
                Release(f.Renderer);
                flightCount--;
                flights[i] = flights[flightCount];
                flights[flightCount] = default;
            }
        }

        private void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            free.Push(renderer);
        }

        private SpriteRenderer CreateWithWarning()
        {
            Debug.LogWarning("BulletSystem pool grew; raise prewarm.", this);
            return Create();
        }

        private SpriteRenderer Create()
        {
            var go = new GameObject("Bullet");
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = sortingLayerId;
            go.SetActive(false);
            return renderer;
        }
    }
}
