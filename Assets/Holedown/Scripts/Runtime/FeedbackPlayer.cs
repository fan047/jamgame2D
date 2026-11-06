using System.Collections;
using UnityEngine;

namespace Holedown
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip launchSound, hitSound, breakSound, rewardSound;
        [SerializeField] AudioClip winSound, lossSound, recallSound, confirmSound;
        [SerializeField] Sprite fragmentSprite;
        [SerializeField] Material fragmentMaterial;
        [SerializeField] VisualTheme theme;
        AudioSource accentSource, uiSource;
        AudioClip fallbackHit;
        float nextHitTime, nextBreakTime;
        public int SoundsPlayed { get; private set; }
        AudioSnapshot audioConfiguration=new AudioSnapshot();
        public string AudioVersion => audioConfiguration.Version;
        public string AudioError { get; private set; }
        public event System.Action AudioChanged;
        public AudioCue? LastCue { get; private set; }
        public float LastCueGain { get; private set; }
        public AudioClip ConfiguredClip(AudioCue cue) => audioConfiguration.Cues[(int)cue].Replacement ? audioConfiguration.Cues[(int)cue].Replacement : DefaultClip(cue);
        AudioClip DefaultClip(AudioCue cue) => cue switch
        {
            AudioCue.VolleyLaunch=>launchSound,AudioCue.BlockHit=>hitSound?hitSound:fallbackHit,AudioCue.BlockBreak=>breakSound,AudioCue.RewardBreak=>rewardSound,
            AudioCue.StageClear=>winSound,AudioCue.StageFail=>lossSound,AudioCue.ManualRecall=>recallSound,AudioCue.UiConfirm=>confirmSound,_=>null
        };
        IEnumerator Start()
        {
            while(flow && flow.ConfigurationDirectory==null)yield return null;
            if(flow)LoadAudio();
        }
        bool LoadAudio()
        {
            try
            {
                var candidate=AudioConfiguration.Read(flow.ConfigurationDirectory);
                StopSounds();var old=audioConfiguration;audioConfiguration=candidate;AudioError=null;old.Dispose();
                AudioChanged?.Invoke();return true;
            }
            catch(System.Exception error){AudioError=error.Message;AudioChanged?.Invoke();return false;}
        }
        public bool ReloadAudio()
        {
            if(!flow.IsPlannerTest || flow.Phase!=AppPhase.Paused || !flow.UserInputAllowed)return false;
            bool success=LoadAudio();if(success)PlayConfirm();return success;
        }
        void StopSounds(){if(audioSource)audioSource.Stop();if(accentSource)accentSource.Stop();if(uiSource)uiSource.Stop();}

        void Awake()
        {
            if (!audioSource) return;
            accentSource = CreateSource(64);
            uiSource = CreateSource(128);
            if (hitSound) return;
            const int rate=22050;
            var samples=new float[1323];
            for(int i=0;i<samples.Length;i++) {float t=(float)i/rate; samples[i]=Mathf.Sin(t*880*Mathf.PI*2)*0.18f*(1f-(float)i/samples.Length);}
            fallbackHit=AudioClip.Create("Placeholder hit",samples.Length,1,rate,false);
            fallbackHit.SetData(samples,0);
        }

        AudioSource CreateSource(int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            source.volume = audioSource.volume;
            source.priority = priority;
            return source;
        }

        void PauseAudio(bool paused)
        {
            if(!audioSource) return;
            if (paused) { audioSource.Pause(); accentSource.Pause(); }
            else { audioSource.UnPause(); accentSource.UnPause(); }
        }

        void OnEnable()
        {
            flow.BlockHit += PlayHit;
            flow.VolleyLaunched += PlayLaunch;
            flow.ManualRecallAccepted += PlayRecall;
            flow.RoundSettled += PlayResult;
            flow.CommandConfirmed += PlayConfirm;
            flow.AttemptReset += Clear;
            flow.PauseChanged += PauseAudio;
        }

        void OnDisable()
        {
            flow.BlockHit -= PlayHit;
            flow.VolleyLaunched -= PlayLaunch;
            flow.ManualRecallAccepted -= PlayRecall;
            flow.RoundSettled -= PlayResult;
            flow.CommandConfirmed -= PlayConfirm;
            flow.AttemptReset -= Clear;
            flow.PauseChanged -= PauseAudio;
            Clear();
        }

        void PlayLaunch() => PlaySound(audioSource, AudioCue.VolleyLaunch, 0.65f);
        void PlayRecall() => PlaySound(accentSource, AudioCue.ManualRecall, 0.7f);
        void PlayConfirm() => PlaySound(uiSource, AudioCue.UiConfirm, 0.35f);

        void PlayResult(SessionPhase phase)
        {
            if (phase == SessionPhase.Won) PlaySound(accentSource, AudioCue.StageClear, 0.9f);
            if (phase == SessionPhase.Lost) PlaySound(accentSource, AudioCue.StageFail, 0.8f);
        }

        bool PlaySound(AudioSource source, AudioCue slot, float gain)
        {
            var cue=audioConfiguration.Cues[(int)slot];var clip=ConfiguredClip(slot);
            if (!source || !clip || cue.Muted || cue.VolumePercent==0) return false;
            gain*=cue.VolumePercent/100f;source.PlayOneShot(clip, gain);
            LastCue=slot;LastCueGain=gain;
            return true;
        }

        void Clear()
        {
            StopAllCoroutines();
            StopSounds();
            nextHitTime = nextBreakTime = 0;
            foreach(Transform child in transform)Destroy(child.gameObject);
        }
        void OnDestroy() { audioConfiguration.Dispose();if(fallbackHit) Destroy(fallbackHit); }

        void PlayBlockSound(HitResult hit)
        {
            if (!hit.Destroyed)
            {
                if (Time.time < nextHitTime || !PlaySound(audioSource, AudioCue.BlockHit, 0.3f)) return;
                nextHitTime = Time.time + 0.05f;
            }
            else if (hit.RewardBalls > 0)
            {
                if (!PlaySound(accentSource, AudioCue.RewardBreak, 0.95f)) return;
            }
            else if (hit.IsTarget)
            {
                if (!PlaySound(accentSource, AudioCue.BlockBreak, 0.72f)) return;
            }
            else
            {
                if (Time.time < nextBreakTime || !PlaySound(audioSource, AudioCue.BlockBreak, 0.42f)) return;
                nextBreakTime = Time.time + 0.08f;
            }
            SoundsPlayed++;
        }

        void PlayHit(BlockView view, HitResult hit)
        {
            PlayBlockSound(hit);
            if(!view) return;
            if(hit.Destroyed)
            {
                var position = view.transform.position;
                StartCoroutine(Fragments(position, view.BaseColor));
                if(hit.RewardBalls > 0 || hit.IsTarget) StartCoroutine(SpecialFeedback(position, hit.RewardBalls, hit.IsTarget, view.GetComponentInChildren<TMPro.TMP_Text>(true).font));
            }
            else view.PlayImpact();
        }
        IEnumerator SpecialFeedback(Vector3 position, int reward, bool target, TMPro.TMP_FontAsset font)
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
                text = go.AddComponent<TMPro.TextMeshPro>(); text.font=font; text.text = $"+{reward}\n<size=65%>下关生效</size>";
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
