using System.Collections;
using TMPro;
using UnityEngine;

namespace Holedown
{
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] TMP_Text label;
        [SerializeField] BoxCollider2D shape;
        [SerializeField] SpriteRenderer cracks;
        [SerializeField] Sprite lightCracks;
        [SerializeField] Sprite heavyCracks;
        int reward;
        int maxHealth;
        Vector3 baseVisualScale;
        Coroutine impact;
        public int DamageLevel { get; private set; }
        public int BlockId { get; private set; }
        public Color BaseColor { get; private set; }
        public SpriteRenderer Visual => visual;
        public void Bind(BlockState block)
        {
            StopImpact();
            BlockId = block.Id;reward=block.RewardBalls;maxHealth=block.MaxHealth;
            transform.position = new Vector3(block.Column + block.Width * 0.5f, block.Row + block.Height * 0.5f, 0);
            var size = new Vector2(block.Width - 0.1f, block.Height - 0.1f);
            shape.size = size;
            baseVisualScale = new Vector3(size.x,size.y,1);
            visual.transform.localScale = baseVisualScale;
            BaseColor = block.IsTarget ? new Color(1f, 0.67f, 0.22f) : reward>0 ? new Color(0.65f,0.42f,0.9f) : new Color(0.18f, 0.62f, 0.73f);
            visual.color = BaseColor;
            SetHealth(block.Health, block.IsTarget);
        }
        public void SetHealth(int health, bool target)
        {
            label.text = target ? "* " + health : reward>0 ? health+"\n<size=65%>+"+reward+"</size>" : health.ToString();
            // Integer comparisons retain exact 1/3 and 2/3 thresholds, including Int32 health.
            DamageLevel = (long)health*3 > (long)maxHealth*2 ? 0 : (long)health*3 > maxHealth ? 1 : 2;
            if(cracks) { cracks.sprite=DamageLevel==1?lightCracks:DamageLevel==2?heavyCracks:null;cracks.enabled=health>0&&DamageLevel>0; }
        }
        public void PlayImpact()
        {
            StopImpact();
            if(isActiveAndEnabled)impact=StartCoroutine(Impact());
        }
        IEnumerator Impact()
        {
            float elapsed=0;
            while(elapsed<0.12f) {
                // Restart from the baseline on every hit: never multiply the current shrink.
                float factor=elapsed<0.04f?Mathf.Lerp(1f,0.94f,elapsed/0.04f):Mathf.Lerp(0.94f,1f,(elapsed-0.04f)/0.08f);
                visual.transform.localScale=baseVisualScale*factor;
                visual.color=elapsed<0.06f?Color.white:BaseColor;
                yield return null;elapsed+=Time.deltaTime;
            }
            visual.transform.localScale=baseVisualScale;visual.color=BaseColor;impact=null;
        }
        void StopImpact()
        {
            if(impact!=null){StopCoroutine(impact);impact=null;}
            if(visual){visual.transform.localScale=baseVisualScale;visual.color=BaseColor;}
        }
        void OnDisable()=>StopImpact();
        public void Remove() { shape.enabled = false; gameObject.SetActive(false); Destroy(gameObject); }
    }
}
