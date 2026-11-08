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
        [SerializeField] VisualTheme theme;
        [SerializeField] Transform visualRoot;
        [SerializeField] SpriteRenderer roleBadge;
        Vector2 targetSize;
        MaterialPropertyBlock flash;
        static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
        public Transform ImpactRoot => visualRoot ? visualRoot : visual.transform;
        public SpriteRenderer Cracks => cracks;
        public bool OriginalColors => theme && theme.originalColors;
        public void ConfigureTheme(VisualTheme value) { theme = value; }
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
            targetSize = size;
            if(theme)
            {
                var sprite = theme.Block(block.Width, block.Height, block.IsTarget, block.RewardBalls); if(sprite) visual.sprite = sprite;
                if(theme.crackLight) lightCracks = theme.crackLight;
                if(theme.crackHeavy) heavyCracks = theme.crackHeavy;
                if(theme.flashMaterial) { visual.sharedMaterial = theme.flashMaterial; if(cracks) cracks.sharedMaterial = theme.flashMaterial; }
            }
            SpriteSizing.Fit(visual, size);
            baseVisualScale = visualRoot ? Vector3.one : visual.transform.localScale;
            ImpactRoot.localScale = baseVisualScale;
            BaseColor = OriginalColors ? Color.white : block.IsTarget ? new Color(1f, 0.67f, 0.22f) : reward>0 ? new Color(0.65f,0.42f,0.9f) : new Color(0.18f, 0.62f, 0.73f);
            visual.color = BaseColor;
            if(roleBadge)
            {
                roleBadge.sprite = theme ? block.IsTarget ? theme.targetBadge : reward > 0 ? theme.rewardBadge : null : null;
                roleBadge.enabled = roleBadge.sprite;
                roleBadge.transform.localPosition = new Vector3(-size.x * 0.32f, size.y * 0.32f, 0);
                SpriteSizing.Fit(roleBadge, Vector2.one * 0.25f);
            }
            label.rectTransform.sizeDelta = size * 0.85f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.1f;
            label.fontSizeMax = 4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            SetHealth(block.Health, block.IsTarget);
        }
        public void SetHealth(int health, bool target)
        {
            label.text = target && !(roleBadge && roleBadge.enabled) ? "* " + health : reward>0 ? health+"\n<size=65%>+"+reward+"</size>" : health.ToString();
            // Integer comparisons retain exact 1/3 and 2/3 thresholds, including Int32 health.
            DamageLevel = (long)health*3 > (long)maxHealth*2 ? 0 : (long)health*3 > maxHealth ? 1 : 2;
            if(cracks)
            {
                cracks.sprite=DamageLevel==1?lightCracks:DamageLevel==2?heavyCracks:null;
                cracks.enabled=health>0&&DamageLevel>0;
                if(visualRoot) SpriteSizing.Fit(cracks, targetSize);
            }
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
                ImpactRoot.localScale=baseVisualScale*factor;
                if(theme && theme.flashMaterial) SetFlash(elapsed<0.06f?1:0);
                else visual.color=elapsed<0.06f?Color.white:BaseColor;
                yield return null;elapsed+=Time.deltaTime;
            }
            ImpactRoot.localScale=baseVisualScale;visual.color=BaseColor;SetFlash(0);impact=null;
        }
        void SetFlash(float amount)
        {
            if(flash == null) flash = new MaterialPropertyBlock();
            void Apply(SpriteRenderer renderer)
            {
                if(!renderer) return;
                renderer.GetPropertyBlock(flash); flash.SetFloat(FlashAmount, amount); renderer.SetPropertyBlock(flash);
            }
            Apply(visual); Apply(cracks);
        }
        void StopImpact()
        {
            if(impact!=null){StopCoroutine(impact);impact=null;}
            if(visual){ImpactRoot.localScale=baseVisualScale;visual.color=BaseColor;SetFlash(0);}
        }
        void OnDisable()=>StopImpact();
        public void Remove() { shape.enabled = false; gameObject.SetActive(false); Destroy(gameObject); }
        // Used by the advance animation: the incoming row stays hidden until it reaches the bottom.
        public void SetPresentationVisible(bool visible)
        {
            if(label)label.enabled=visible;
            if(roleBadge)roleBadge.enabled=visible&&roleBadge.sprite;
        }
    }
}
