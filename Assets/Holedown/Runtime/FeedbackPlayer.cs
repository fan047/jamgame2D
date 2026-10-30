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
        void OnEnable() { flow.BlockHit += PlayHit; }
        void OnDisable() { flow.BlockHit -= PlayHit; StopAllCoroutines(); if(audioSource)audioSource.Stop(); }
        void OnDestroy() { if(hitClip) Destroy(hitClip); }
        void PlayHit(BlockView view, HitResult hit)
        {
            if(Time.time>=nextSoundTime) { audioSource.PlayOneShot(hitClip); nextSoundTime=Time.time+0.05f; SoundsPlayed++; }
            if(!view) return;
            if(hit.Destroyed) StartCoroutine(Fragments(view.transform.position,view.BaseColor));
            else StartCoroutine(Flash(view));
        }
        IEnumerator Flash(BlockView view)
        {
            // Repeated hits extend white feedback without touching collision geometry.
            view.Visual.color=Color.white;
            yield return new WaitForSeconds(0.06f);
            if(view && view.gameObject.activeSelf) view.Visual.color=view.BaseColor;
        }
        IEnumerator Fragments(Vector3 position, Color color)
        {
            var root=new GameObject("Fragments"); root.transform.SetParent(transform); root.transform.position=position;
            var pieces=new Transform[5];var velocities=new Vector3[5];
            for(int i=0;i<pieces.Length;i++)
            {
                var go=new GameObject("Piece");go.transform.SetParent(root.transform,false);go.transform.localScale=Vector3.one*0.13f;
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=fragmentSprite;renderer.sharedMaterial=fragmentMaterial;renderer.color=color;renderer.sortingOrder=6;
                pieces[i]=go.transform;velocities[i]=Quaternion.Euler(0,0,i*72)*Vector3.up*1.8f;
            }
            float elapsed=0;
            while(elapsed<0.3f)
            { elapsed+=Time.deltaTime;for(int i=0;i<pieces.Length;i++){pieces[i].position+=velocities[i]*Time.deltaTime;pieces[i].localScale=Vector3.one*0.13f*Mathf.Max(0,1-elapsed/0.3f);}yield return null; }
            Destroy(root);
        }
    }
}
