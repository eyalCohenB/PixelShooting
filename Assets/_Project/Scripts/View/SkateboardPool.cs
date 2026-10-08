using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Pool of skateboards for conveyor riders. Prewarms on Awake so gameplay never instantiates;
    /// grows with a warning if it ever runs out. Also holds the board's pose under a shooter,
    /// so it can be tuned in one place (changes apply to the next board attached).
    /// </summary>
    public class SkateboardPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [Tooltip("5 riders + a few still scaling out after their lap.")]
        [SerializeField] private int prewarm = 8;

        [Header("Pose under the shooter (local to the shooter)")]
        [SerializeField] private Vector3 localPosition = Vector3.zero;
        [SerializeField] private Vector3 localEuler = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private float tweenTime = 0.15f;

        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalEuler => localEuler;
        public Vector3 LocalScale => localScale;
        public float TweenTime => tweenTime;

        private readonly Stack<SkateboardView> free = new Stack<SkateboardView>(16);
        private int created;

        private void Awake()
        {
            for (int i = 0; i < prewarm; i++)
            {
                free.Push(Create());
            }
        }

        public SkateboardView Get()
        {
            SkateboardView board;
            if (free.Count > 0)
            {
                board = free.Pop();
            }
            else
            {
                board = Create();
                Debug.LogWarning($"SkateboardPool grew to {created}; raise prewarm.", this);
            }
            board.gameObject.SetActive(true);
            return board;
        }

        public void Release(SkateboardView board)
        {
            Transform t = board.transform;
            t.DOKill();
            t.SetParent(transform, false);
            board.gameObject.SetActive(false);
            free.Push(board);
        }

        private SkateboardView Create()
        {
            GameObject go = Instantiate(prefab, transform);
            // The prefab can be the plain model; the component is added here (at prewarm time).
            if (!go.TryGetComponent(out SkateboardView board))
            {
                board = go.AddComponent<SkateboardView>();
            }
            board.Init(this);
            go.SetActive(false);
            created++;
            return board;
        }
    }
}
