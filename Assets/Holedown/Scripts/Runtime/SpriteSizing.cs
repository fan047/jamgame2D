using UnityEngine;

namespace Holedown
{
    public static class SpriteSizing
    {
        public static Vector3 Scale(Sprite sprite, Vector2 size)
        {
            if(!sprite || sprite.bounds.size.x <= 0 || sprite.bounds.size.y <= 0) return Vector3.one;
            return new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        }
        public static void Fit(SpriteRenderer renderer, Vector2 size)
        {
            if(renderer && renderer.sprite) renderer.transform.localScale = Scale(renderer.sprite, size);
        }
        public static void Cover(SpriteRenderer renderer, Vector2 size)
        {
            if(!renderer || !renderer.sprite) return;
            var scale = Scale(renderer.sprite, size);
            renderer.transform.localScale = Vector3.one * Mathf.Max(scale.x, scale.y);
        }
    }
}
