using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] TMP_Text status;
        [SerializeField] TMP_Text stageIndex, ballCount, round, rewardValue;
        [SerializeField] GameObject hudRoot, rewardStrip, recallHint;
        [SerializeField] SoftScrim recallScrim;
        [SerializeField] Button recall;
        [SerializeField] Button reload;
        [SerializeField] float pendingBounceSeconds = 0.18f;
        [SerializeField, Range(1f, 1.3f)] float pendingBounceScale = 1.08f;
        int shownPending = -1;
        Coroutine bounce;
        void Recall() => flow.QueueCommand(FlowCommand.Recall);
        void Reload() => flow.QueueCommand(FlowCommand.Reload);
        void OnEnable() { flow.Changed += Refresh; recall.onClick.AddListener(Recall); reload.onClick.AddListener(Reload); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; recall.onClick.RemoveListener(Recall); reload.onClick.RemoveListener(Reload); if(bounce!=null)StopCoroutine(bounce);bounce=null;if(rewardValue)rewardValue.rectTransform.localScale=Vector3.one; }
        void BouncePending()
        {
            if (pendingBounceSeconds <= 0f || !isActiveAndEnabled) return;
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(PendingPulse());
        }
        System.Collections.IEnumerator PendingPulse()
        {
            var rect = rewardValue.rectTransform; var rest = Vector3.one; float elapsed = 0;
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
            reload.gameObject.SetActive(false);
            bool playing = flow.Phase == AppPhase.Playing && flow.Session?.Phase != SessionPhase.Won && flow.Session?.Phase != SessionPhase.Lost;
            hudRoot.SetActive(playing);recall.gameObject.SetActive(playing);recallHint.SetActive(playing);
            status.gameObject.SetActive(playing && flow.IsPlannerTest);
            if(!playing) return;
            var session=flow.Session;
            MineUIStyle.Availability(recall,session?.Phase==SessionPhase.Volley);
            // The borderless backing dims with availability so a disabled recall is still
            // visible without drawing a box over the cave floor.
            if(recallScrim)recallScrim.color=new Color(recallScrim.color.r,recallScrim.color.g,recallScrim.color.b,recall.interactable?1f:.72f);
            if(session==null)return;
            var campaign=flow.Campaign;
            int pending=campaign?.PendingBonus ?? 0;
            stageIndex.text=$"{(campaign?.StageIndex ?? 0)+1:00} / {campaign?.StageCount ?? 1:00}";
            ballCount.text=$"{flow.EntryBalls}<size=34%> 球</size>";
            round.text=$"第 {session.Round + (session.Phase==SessionPhase.Aiming?1:0)} 轮";
            rewardStrip.SetActive(pending>0);rewardValue.text=$"+{pending}";
            if(pending!=shownPending){shownPending=pending;if(pending>0)BouncePending();else{if(bounce!=null)StopCoroutine(bounce);bounce=null;rewardValue.rectTransform.localScale=Vector3.one;}}
            status.text=$"关卡：{flow.StageId} | 在场 {session.ActiveCount} 球（待发 {session.Pending}） | 补行 {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0} | {(flow.IsAdvancing ? "推进中" : ChineseText.Session(session.Phase))}\n配置：{ChineseText.Configuration(flow.ConfigurationLabel)}";
        }
    }
}
