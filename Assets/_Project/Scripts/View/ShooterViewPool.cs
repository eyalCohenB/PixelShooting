using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Pool of ShooterView instances shared by the queue, conveyor and holder views.
    /// Prewarms on Awake so gameplay normally never instantiates; if it ever runs out it
    /// grows (and logs a warning so the prewarm size can be raised).
    /// </summary>
    public class ShooterViewPool : MonoBehaviour
    {
        [SerializeField] private ShooterView prefab;
        [Tooltip("Enough for queue (columns x visible rows) + conveyor (5) + holder (5).")]
        [SerializeField] private int prewarm = 24;

        private readonly Stack<ShooterView> free = new Stack<ShooterView>(32);
        private int created;

        private void Awake()
        {
            for (int i = 0; i < prewarm; i++)
            {
                free.Push(Create());
            }
        }

        public ShooterView Get()
        {
            ShooterView view;
            if (free.Count > 0)
            {
                view = free.Pop();
            }
            else
            {
                view = Create();
                Debug.LogWarning($"ShooterViewPool grew to {created}; raise prewarm.", this);
            }
            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(ShooterView view)
        {
            view.CachedTransform.DOKill();
            view.CachedTransform.localScale = prefab.transform.localScale;
            view.ClearShooter();
            view.gameObject.SetActive(false);
            free.Push(view);
        }

        private ShooterView Create()
        {
            ShooterView view = Instantiate(prefab, transform);
            view.Init(this);
            view.gameObject.SetActive(false);
            created++;
            return view;
        }
    }
}
