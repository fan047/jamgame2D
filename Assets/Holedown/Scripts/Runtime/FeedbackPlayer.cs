using System.Collections;
using UnityEngine;

namespace Holedown
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] AudioSource audioSource;
        [SerializeField] Sprite fragmentSprite;
        [SerializeField] Material fragmentMaterial;
        [SerializeField] VisualTheme theme;
        AudioClip hitClip;
        float nextSoundTime;
        public int SoundsPlayed { get; private set; }
        void Awake()
        {
            const int rate=22050;
            var samples=new float[1323];
            for(int i=0;i<samples.Length;i++) {float t=(float)i/rate; samples[i]=Mathf.Sin(t*880*Mathf.PI*2)*0.18f*(1f-(float)i/samples.Length);}
            hitClip=AudioClip.Create("Placeholder hit",samples.Length,1,rate,false); hitClip.SetData(samples,0);
        }
        bool pausedAudio;
        void PauseAudio(bool paused)
        {
            if(!audioSource) return;
            if(paused) { pausedAudio = audioSource.isPlaying; if(pausedAudio) audioSource.Pause(); }
            else { if(pausedAudio) audioSource.UnPause(); pausedAudio = false; }
        }
        void OnEnable() { flow.BlockHit += PlayHit; flow.AttemptReset += Clear; flow.PauseChanged += PauseAudio; }
        void OnDisable() { flow.BlockHit -= PlayHit; flow.AttemptReset -= Clear; flow.PauseChanged -= PauseAudio; Clear(); }
        void Clear()
        {
            pausedAudio = false;
            StopAllCoroutines(); if(audioSource)audioSource.Stop();nextSoundTime=0;
            foreach(Transform child in transform)Destroy(child.gameObject);
        }
        void OnDestroy() { if(hitClip) Destroy(hitClip); }
        void PlayHit(BlockView view, HitResult hit)
        {
            if(Time.time>=nextSoundTime) { audioSource.PlayOneShot(hitClip); nextSoundTime=Time.time+0.05f; SoundsPlayed++; }
            if(!view) return;
            if(hit.Destroyed)
            {
                var position = view.transform.position;
                StartCoroutine(Fragments(position, view.BaseColor));
                if(hit.RewardBalls > 0 || hit.IsTarget) StartCoroutine(SpecialFeedback(position, hit.RewardBalls, hit.IsTarget));
            }
            else view.PlayImpact();
        }
        IEnumerator SpecialFeedback(Vector3 position, int reward, bool target)
        {
            var root = new GameObject(target ? "TargetFeedback" : "RewardFeedback");
            root.transform.SetParent(transform); root.transform.position = position;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = theme && theme.spark ? theme.spark : fragmentSprite;
            renderer.sharedMaterial = fragmentMaterial; renderer.sortingOrder = 6;
            Color color = theme && theme.originalColors && theme.spark ? Color.white : target ? new Color(1,0.8f,0.35f) : new Color(0.65f,1,0.7f);
            TMPro.TextMeshPro text = null;
            if(reward > 0)
            {
                var go = new GameObject("RewardValue"); go.transform.SetParent(root.transform,false);
                text = go.AddComponent<TMPro.TextMeshPro>(); text.text = $"+{reward}\n<size=65%>Next stage</size>";
                text.fontSize = 3; text.alignment = TMPro.TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(2.5f,1); text.GetComponent<MeshRenderer>().sortingOrder = 8;
            }
            float duration = reward > 0 ? 0.6f : 0.3f, elapsed = 0;
            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float lightDuration = target ? 0.3f : 0.35f;
                renderer.enabled = elapsed < lightDuration;
                renderer.color = new Color(color.r,color.g,color.b,0.55f * Mathf.Max(0,1-elapsed/lightDuration));
                SpriteSizing.Fit(renderer, Vector2.one * (0.3f + elapsed * 2));
                if(text) { text.transform.position = position + Vector3.up * elapsed; text.alpha = Mathf.Max(0,1-elapsed/duration); text.transform.localScale = new Vector3(1/root.transform.localScale.x,1/root.transform.localScale.y,1); }
                yield return null;
            }
            Destroy(root);
        }
        IEnumerator Fragments(Vector3 position, Color color)
        {
            var root=new GameObject("Fragments"); root.transform.SetParent(transform); root.transform.position=position;
            var pieces=new Transform[5];var velocities=new Vector3[5];
            for(int i=0;i<pieces.Length;i++)
            {
                var go=new GameObject("Piece");go.transform.SetParent(root.transform,false);go.transform.localScale=Vector3.one*0.13f;
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=theme && theme.fragment ? theme.fragment : fragmentSprite;renderer.sharedMaterial=fragmentMaterial;renderer.color=theme && theme.originalColors ? Color.white : color;renderer.sortingOrder=6;
                SpriteSizing.Fit(renderer, Vector2.one * 0.13f);
                pieces[i]=go.transform;velocities[i]=Quaternion.Euler(0,0,i*72)*Vector3.up*1.8f;
            }
            float elapsed=0;
            while(elapsed<0.3f)
            {elapsed+=Time.deltaTime;for(int i=0;i<pieces.Length;i++){pieces[i].position+=velocities[i]*Time.deltaTime;SpriteSizing.Fit(pieces[i].GetComponent<SpriteRenderer>(),Vector2.one*0.13f*Mathf.Max(0,1-elapsed/0.3f));}yield return null; }
            Destroy(root);
        }
    }
}
