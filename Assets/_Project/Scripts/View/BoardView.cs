using DG.Tweening;
using PixelShooting.Core.Board;
using PixelShooting.Core.Levels;
using UnityEngine;

namespace PixelShooting.View
{
    /// <summary>
    /// Draws the board: one pooled SpriteRenderer per cell, tinted with the palette color.
    /// Sits on the BoardArea object; the grid is centered on it and scaled to fit areaSize,
    /// so smaller grids get bigger cells.
    ///
    /// Pooling: renderers are created only when a level needs more than exist, and are reused
    /// on every Build (restart / next level). Gameplay itself creates nothing.
    /// Pop: a DOTween scale-to-zero with no OnComplete callback (a callback that knows which
    /// cell to hide would need a capturing lambda = allocation). A zero-scale sprite is
    /// invisible; the next Build resets or hides it.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private Sprite cellSprite;
        [Tooltip("World-space size the grid must fit in (the sand area).")]
        [SerializeField] private Vector2 areaSize = new Vector2(8.4f, 8.4f);
        [Tooltip("Fraction of the cell slot the sprite fills; the rest is the gap between cells.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float cellFill = 0.9f;
        [SerializeField] private string sortingLayer = "Board";
        [SerializeField] private float popDuration = 0.2f;

        private SpriteRenderer[] pool = new SpriteRenderer[0]; // every renderer ever created
        private int poolCount;
        private SpriteRenderer[] cellRenderers = new SpriteRenderer[0]; // [y * width + x], null = empty
        private int width;
        private int height;
        private float cellSize;
        private Vector3 cellScale;
        private Vector2 origin; // local position of cell (0,0)'s center
        private int sortingLayerId;

        public float CellSize => cellSize;

        private void Awake()
        {
            sortingLayerId = SortingLayer.NameToID(sortingLayer);
        }

        /// <summary>
        /// Shows the board's current cells. Call after BoardModel.Reset (level start or restart).
        /// Allocates only when this level has more cells than the pool, or a different size.
        /// </summary>
        public void Build(BoardModel board, LevelData level)
        {
            width = board.Width;
            height = board.Height;

            // Fit the grid in the area: the tighter axis decides the cell size.
            cellSize = Mathf.Min(areaSize.x / width, areaSize.y / height);
            // Scale the sprite so it covers cellFill of a slot, whatever the sprite's own size.
            float spriteSize = cellSprite.bounds.size.x;
            float scale = cellSize * cellFill / spriteSize;
            cellScale = new Vector3(scale, scale, 1f);
            // Center the grid on this transform: cell (0,0) is bottom-left.
            origin = new Vector2(-(width - 1) * cellSize * 0.5f, -(height - 1) * cellSize * 0.5f);

            if (cellRenderers.Length != width * height)
            {
                cellRenderers = new SpriteRenderer[width * height];
            }

            // Return every renderer to the pool (and stop any running pop).
            for (int i = 0; i < poolCount; i++)
            {
                pool[i].transform.DOKill();
                pool[i].gameObject.SetActive(false);
            }

            int used = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    sbyte color = board.GetCell(x, y);
                    if (color == LevelData.EmptyCell)
                    {
                        cellRenderers[index] = null;
                        continue;
                    }

                    SpriteRenderer cell = GetPooled(used++);
                    cell.color = level.GetColor(color);
                    Transform t = cell.transform;
                    t.localPosition = LocalCellPosition(x, y);
                    t.localScale = cellScale;
                    cell.gameObject.SetActive(true);
                    cellRenderers[index] = cell;
                }
            }
        }

        /// <summary>Plays the destroy animation for a cell (call when its bullet lands).</summary>
        public void Pop(int x, int y)
        {
            int index = y * width + x;
            SpriteRenderer cell = cellRenderers[index];
            if (cell == null)
            {
                return;
            }
            cellRenderers[index] = null;
            cell.transform.DOScale(0f, popDuration).SetEase(Ease.InBack);
        }

        /// <summary>World position of a cell's center (bullet target, firing points).</summary>
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return transform.TransformPoint(LocalCellPosition(x, y));
        }

        private Vector3 LocalCellPosition(int x, int y)
        {
            return new Vector3(origin.x + x * cellSize, origin.y + y * cellSize, 0f);
        }

        // Returns pool[i], creating renderers only if the pool is too small.
        private SpriteRenderer GetPooled(int i)
        {
            if (i < poolCount)
            {
                return pool[i];
            }

            if (poolCount == pool.Length)
            {
                System.Array.Resize(ref pool, Mathf.Max(64, pool.Length * 2));
            }

            var go = new GameObject("Cell");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = cellSprite;
            renderer.sortingLayerID = sortingLayerId;
            pool[poolCount++] = renderer;
            return renderer;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
        }
#endif
    }
}
