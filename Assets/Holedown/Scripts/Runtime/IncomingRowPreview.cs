using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    // Display-only preview of the row that waits below the floor line. It owns no rule
    // data, no collider, no rigidbody and no text: only the column footprint of the next
    // incoming row, drawn as a low-saturation silhouette that softens towards the bottom.
    // The rule board stays the single owner of health, reward and target identity.
    public sealed class IncomingRowPreview : MonoBehaviour
    {
        [SerializeField] VisualTheme theme;
        [SerializeField, Tooltip("World Y of the preview row centre; the row sits below the floor line.")]
        float centreY = -0.55f;
        [SerializeField, Tooltip("Low-saturation silhouette tint; role colours never appear here.")]
        Color tint = new Color(0.74f, 0.82f, 0.93f, 1f);
        [SerializeField, Tooltip("Short fade-in when a preview appears; 0 shows it at once.")]
        [Min(0f)] float fadeInSeconds = 0.25f;
        [SerializeField, Tooltip("Behind the rock foreground (10) so real decorations cover it.")]
        int sortingOrder = 2;
        readonly List<SpriteRenderer> cells = new List<SpriteRenderer>();
        Coroutine fade;
        public float CentreY => centreY;
        public Color Tint => tint;
        public float FadeInSeconds => fadeInSeconds;
        public int CellCount { get { int count = 0; foreach (var cell in cells) if (cell && cell.gameObject.activeSelf) count++; return count; } }
        public IReadOnlyList<SpriteRenderer> Cells => cells;
        public VisualTheme Theme => theme;

        void OnDisable() { StopFade(); SetVisible(false); }

        public void Configure(VisualTheme value) { theme = value; }

        // Shows the row the next advance would add; the returned snapshot of BoardState is
        // read-only, so presenting never consumes the cursor or creates rule blocks.
        public void Present(BoardState board, bool animate)
        {
            StopFade();
            IReadOnlyList<IncomingBlockPreview> row = null;
            bool available = board != null && board.TryPeekIncomingRow(out row) && row.Count > 0;
            if (!available) { SetVisible(false); return; }
            for (int i = 0; i < row.Count; i++)
            {
                var cell = Cell(i);
                // The shared grey-blue silhouette uses the first cave's ordinary sprite: the
                // preview material multiplies the base colour, so a group sprite would tint it.
                var sprite = theme ? theme.Block(row[i].Width, 1) : null;
                cell.sprite = sprite;
                cell.sortingOrder = sortingOrder;
                cell.sharedMaterial = theme ? theme.previewMaterial : null;
                cell.transform.localPosition = new Vector3(row[i].Column + row[i].Width * 0.5f, centreY, 0);
                SpriteSizing.Fit(cell, new Vector2(row[i].Width - 0.1f, 0.9f));
                cell.color = new Color(tint.r, tint.g, tint.b, animate && fadeInSeconds > 0f ? 0f : 1f);
            }
            for (int i = row.Count; i < cells.Count; i++) if (cells[i]) cells[i].gameObject.SetActive(false);
            if (animate && fadeInSeconds > 0f && isActiveAndEnabled) fade = StartCoroutine(FadeIn());
        }

        public void SetVisible(bool visible)
        {
            StopFade();
            foreach (var cell in cells) if (cell) cell.gameObject.SetActive(visible);
            if (visible) foreach (var cell in cells) if (cell) cell.color = new Color(tint.r, tint.g, tint.b, 1f);
        }

        SpriteRenderer Cell(int index)
        {
            while (cells.Count <= index)
            {
                var created = new GameObject("Preview " + cells.Count);
                created.transform.SetParent(transform, false);
                var renderer = created.AddComponent<SpriteRenderer>();
                cells.Add(renderer);
            }
            var cell = cells[index];
            cell.gameObject.SetActive(true);
            return cell;
        }

        IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInSeconds);
                foreach (var cell in cells) if (cell && cell.gameObject.activeSelf) cell.color = new Color(tint.r, tint.g, tint.b, alpha);
                yield return null;
            }
            foreach (var cell in cells) if (cell) cell.color = new Color(tint.r, tint.g, tint.b, 1f);
            fade = null;
        }

        void StopFade() { if (fade != null) { StopCoroutine(fade); fade = null; } }
    }
}
