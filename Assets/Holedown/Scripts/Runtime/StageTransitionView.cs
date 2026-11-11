using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Kept on the UI rather than the flow: BeginAttempt clears the flow's coroutines.
    public sealed class StageTransitionView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] TMP_FontAsset font;
        [SerializeField, Min(0f)] float fadeOutSeconds = .12f;
        [SerializeField, Min(0f)] float holdSeconds = .06f;
        [SerializeField, Min(0f)] float fadeInSeconds = .2f;
        CanvasGroup curtain;
        TMP_Text stageLabel;
        Coroutine routine;
        bool ownsInputLock;
        public bool IsPlaying { get; private set; }
        public float Duration => fadeOutSeconds + holdSeconds + fadeInSeconds;

        void Awake()
        {
            var root = new GameObject("StageTransitionOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            // Must be a root canvas: nesting under the world-space HUD prevents
            // a screen-space overlay from sizing itself to the whole viewport.
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = .5f;
            curtain = root.GetComponent<CanvasGroup>();
            var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            shade.transform.SetParent(root.transform, false);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            shade.color = new Color(.035f, .05f, .095f, 1f);
            var labelRect = (RectTransform)new GameObject("StageLabel", typeof(RectTransform)).transform;
            labelRect.SetParent(root.transform, false);
            labelRect.sizeDelta = new Vector2(360, 80);
            stageLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            stageLabel.font = font;
            stageLabel.fontSize = 28;
            stageLabel.color = MineUIStyle.Ivory;
            stageLabel.alignment = TextAlignmentOptions.Center;
            stageLabel.raycastTarget = false;
            ResetCurtain();
        }

        public bool PlayNextStage()
        {
            if (!isActiveAndEnabled || IsPlaying || !flow.UserInputAllowed || !flow.CanSimulate
                || flow.IsPlannerTest || flow.Session?.Phase != SessionPhase.Won
                || flow.Campaign == null || flow.Campaign.IsFinalStage) return false;
            IsPlaying = true;
            stageLabel.text = $"第 {flow.Campaign.StageIndex + 2:00} 关";
            curtain.gameObject.SetActive(true);
            curtain.blocksRaycasts = true;
            ownsInputLock = true;
            flow.SetStageTransitionInputBlocked(true);
            routine = StartCoroutine(Transition());
            return true;
        }

        IEnumerator Transition()
        {
            var originalSession = flow.Session;
            try
            {
                yield return Fade(0f, 1f, fadeOutSeconds);
                // Release only around the synchronous commit. No rendered/input frame
                // can slip between unlocking, changing stages and locking again.
                flow.SetStageTransitionInputBlocked(false, rearmInput: false);
                if (flow.HasFocus && flow.Session == originalSession) flow.NextStage();
                flow.SetStageTransitionInputBlocked(true);
                float elapsed = 0f;
                while (elapsed < holdSeconds) { elapsed += Time.unscaledDeltaTime; yield return null; }
                yield return Fade(1f, 0f, fadeInSeconds);
            }
            finally { Finish(); }
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float t = Mathf.Clamp01(elapsed / seconds);
                curtain.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            curtain.alpha = to;
        }

        void Finish()
        {
            if (ownsInputLock && flow) flow.SetStageTransitionInputBlocked(false);
            ownsInputLock = false;
            IsPlaying = false;
            routine = null;
            ResetCurtain();
        }

        void ResetCurtain()
        {
            if (!curtain) return;
            curtain.alpha = 0f;
            curtain.blocksRaycasts = false;
            curtain.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (curtain) Destroy(curtain.gameObject);
        }

        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            Finish();
        }
    }
}
