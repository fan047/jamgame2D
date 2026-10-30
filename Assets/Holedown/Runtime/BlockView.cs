using TMPro;
using UnityEngine;

namespace Holedown
{
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] TMP_Text label;
        [SerializeField] BoxCollider2D shape;
        public int BlockId { get; private set; }
        public Color BaseColor { get; private set; }
        public SpriteRenderer Visual => visual;
        public void Bind(BlockState block)
        {
            BlockId = block.Id;
            transform.position = new Vector3(block.Column + block.Width * 0.5f, block.Row + 0.5f, 0);
            var size = new Vector2(block.Width - 0.1f, 0.9f);
            shape.size = size;
            visual.transform.localScale = size;
            BaseColor = block.IsTarget ? new Color(1f, 0.67f, 0.22f) : new Color(0.18f, 0.62f, 0.73f);
            visual.color = BaseColor;
            SetHealth(block.Health, block.IsTarget);
        }
        public void SetHealth(int health, bool target) => label.text = target ? "* " + health : health.ToString();
        public void Remove() { shape.enabled = false; gameObject.SetActive(false); Destroy(gameObject); }
    }
}
