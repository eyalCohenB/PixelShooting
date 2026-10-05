using DG.Tweening;
using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Draws the queue columns under the Queue anchor: columns centered side by side,
    /// row 0 (the selectable shooter) on top, only the first visibleRows rows shown.
    ///
    /// The model just does top++ when a shooter is taken; this view makes "the shooters
    /// below move up" visible: it hands the top view to the caller, tweens the rest up one
    /// row, and brings in the next hidden shooter at the bottom.
    ///
    /// Picking uses plain math (no colliders): a tap counts for a column if it's near that
    /// column's top slot.
    /// </summary>
    public class QueueView : MonoBehaviour
    {
        [SerializeField] private ShooterViewPool pool;
        [SerializeField] private float columnSpacing = 1.8f;
        [SerializeField] private float rowSpacing = 1.4f;
        [SerializeField] private int visibleRows = 3;
        [Tooltip("Z of shooters: negative = in front of the sprites.")]
        [SerializeField] private float shooterZ = -1f;
        [SerializeField] private float moveUpDuration = 0.25f;

        private ShooterView[][] slots = new ShooterView[0][]; // [column][row]
        private LevelData level;

        /// <summary>Shows the queue's current state. Call after QueueModel.Reset.</summary>
        public void Build(QueueModel queue, LevelData levelData)
        {
            level = levelData;
            Clear();

            slots = new ShooterView[queue.ColumnCount][]; // once per level load
            for (int c = 0; c < slots.Length; c++)
            {
                slots[c] = new ShooterView[visibleRows];
                for (int row = 0; row < visibleRows; row++)
                {
                    Shooter shooter = queue.GetAt(c, row);
                    if (shooter == null) break;
                    ShooterView view = Spawn(shooter);
                    view.CachedTransform.position = SlotPosition(c, row);
                    slots[c][row] = view;
                }
            }
        }

        /// <summary>
        /// Call right AFTER QueueModel.TakeTop(column). Returns the taken shooter's view
        /// (the caller now owns it, e.g. moves it to the conveyor), moves the rest up and
        /// reveals the next hidden shooter.
        /// </summary>
        public ShooterView TakeTop(QueueModel queue, int column)
        {
            ShooterView[] col = slots[column];
            ShooterView taken = col[0];

            for (int row = 1; row < visibleRows; row++)
            {
                ShooterView view = col[row];
                col[row - 1] = view;
                if (view != null)
                {
                    view.CachedTransform.DOKill();
                    view.CachedTransform.DOMove(SlotPosition(column, row - 1), moveUpDuration).SetEase(Ease.OutQuad);
                }
            }

            // The model's top already moved, so the new last visible row is GetAt(visibleRows - 1).
            int lastRow = visibleRows - 1;
            col[lastRow] = null;
            Shooter next = queue.GetAt(column, lastRow);
            if (next != null)
            {
                ShooterView view = Spawn(next);
                Transform t = view.CachedTransform;
                Vector3 scale = t.localScale;
                t.position = SlotPosition(column, lastRow);
                t.localScale = Vector3.zero;
                t.DOScale(scale, moveUpDuration).SetEase(Ease.OutBack);
                col[lastRow] = view;
            }

            return taken;
        }

        /// <summary>Plays the "can't select" shake on a column's top shooter.</summary>
        public void Shake(int column)
        {
            ShooterView top = slots[column][0];
            if (top != null) top.Shake();
        }

        /// <summary>Which column's top shooter (if any) is at this world point.</summary>
        public bool TryPickColumn(Vector3 worldPoint, out int column)
        {
            for (int c = 0; c < slots.Length; c++)
            {
                if (slots[c][0] == null) continue;
                Vector3 top = SlotPosition(c, 0);
                if (Mathf.Abs(worldPoint.x - top.x) <= columnSpacing * 0.5f &&
                    Mathf.Abs(worldPoint.y - top.y) <= rowSpacing * 0.5f)
                {
                    column = c;
                    return true;
                }
            }
            column = -1;
            return false;
        }

        // World position of a slot: columns centered on this transform, rows going down.
        public Vector3 SlotPosition(int column, int row)
        {
            float x = (column - (slots.Length - 1) * 0.5f) * columnSpacing;
            Vector3 world = transform.TransformPoint(new Vector3(x, -row * rowSpacing, 0f));
            world.z = shooterZ;
            return world;
        }

        private ShooterView Spawn(Shooter shooter)
        {
            ShooterView view = pool.Get();
            view.Bind(shooter, level.GetColor(shooter.ColorIndex));
            return view;
        }

        private void Clear()
        {
            for (int c = 0; c < slots.Length; c++)
            {
                for (int row = 0; row < slots[c].Length; row++)
                {
                    if (slots[c][row] != null) pool.Release(slots[c][row]);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + Vector3.down * rowSpacing,
                                new Vector3(columnSpacing * 3f, rowSpacing * visibleRows, 0f));
        }
#endif
    }
}
