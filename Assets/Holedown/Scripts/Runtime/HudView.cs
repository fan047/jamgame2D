using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] TMP_Text status;
        [SerializeField] Button recall;
        [SerializeField] Button reload;
        [SerializeField] float pendingBounceSeconds = 0.18f;
        [SerializeField, Range(1f, 1.3f)] float pendingBounceScale = 1.08f;
        int shownPending = -1;
        Coroutine bounce;
        void Recall() => flow.QueueCommand(FlowCommand.Recall);
        void Reload() => flow.QueueCommand(FlowCommand.Reload);
        void OnEnable() { flow.Changed += Refresh; recall.onClick.AddListener(Recall); reload.onClick.AddListener(Reload); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; recall.onClick.RemoveListener(Recall); reload.onClick.RemoveListener(Reload); }
        void BouncePending()
        {
            if (pendingBounceSeconds <= 0f || !isActiveAndEnabled) return;
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(PendingPulse());
        }
        System.Collections.IEnumerator PendingPulse()
        {
            var rect = status.rectTransform; var rest = Vector3.one; float elapsed = 0;
            while (elapsed < pendingBounceSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pendingBounceSeconds);
                float factor = t < 0.5f ? Mathf.Lerp(1f, pendingBounceScale, t * 2f) : Mathf.Lerp(pendingBounceScale, 1f, (t - 0.5f) * 2f);
                rect.localScale = rest * factor;
                yield return null;
            }
            rect.localScale = rest; bounce = null;
        }
        void Refresh()
        {
            // Configuration reload belongs to the pause menu, never the live board.
            reload.gameObject.SetActive(false);
            var instructions=transform.Find("Instructions");if(instructions)instructions.gameObject.SetActive(false);
            bool playing = flow.Phase == AppPhase.Playing && flow.Session?.Phase != SessionPhase.Won && flow.Session?.Phase != SessionPhase.Lost;
            recall.gameObject.SetActive(playing);
            status.gameObject.SetActive(playing);
            if(!playing) return;
            var session=flow.Session;
            if(session==null) { recall.interactable=false; status.text="正在准备…"; return; }
            recall.interactable=session.Phase==SessionPhase.Volley;
            var campaign=flow.Campaign;
            // Consecutive rewards merge into one value; the number bounces once per change.
            int pending=campaign?.PendingBonus ?? 0;
            if (pending != shownPending) { shownPending = pending; if (playing) BouncePending(); }
            string summary=$"第 {(campaign?.StageIndex ?? 0)+1}/{campaign?.StageCount ?? 1} 关   每轮 {flow.EntryBalls} 球   第 {session.Round + (session.Phase==SessionPhase.Aiming?1:0)} 轮\n待确认奖励 +{pending}（下关生效）";
            status.text=flow.IsPlannerTest ? summary+$"\n<size=65%>在场 {session.ActiveCount} 球（待发 {session.Pending}） | 补行 {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0} | {(flow.IsAdvancing ? "推进中" : ChineseText.Session(session.Phase))}\n配置：{ChineseText.Configuration(flow.ConfigurationLabel)}</size>" : summary;
        }
    }
}
