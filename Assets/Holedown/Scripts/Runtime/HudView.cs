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
        void Recall() => flow.QueueCommand(FlowCommand.Recall);
        void Reload() => flow.QueueCommand(FlowCommand.Reload);
        void OnEnable() { flow.Changed += Refresh; recall.onClick.AddListener(Recall); reload.onClick.AddListener(Reload); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; recall.onClick.RemoveListener(Recall); reload.onClick.RemoveListener(Reload); }
        void Refresh()
        {
            var reloadLabel=reload.GetComponentInChildren<TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"重载当前关":"重载并重开";
            bool playing = flow.Phase == AppPhase.Playing;
            recall.gameObject.SetActive(playing); reload.gameObject.SetActive(playing);
            status.gameObject.SetActive(playing);
            if(!playing) return;
            var session=flow.Session;
            if(session==null) { recall.interactable=false; status.text="正在准备…"; return; }
            recall.interactable=session.Phase==SessionPhase.Volley;
            var campaign=flow.Campaign;
            status.text=$"第 {(campaign?.StageIndex ?? 0)+1}/{campaign?.StageCount ?? 1} 关 | 每轮 {flow.EntryBalls} 球 | 第 {session.Round + (session.Phase==SessionPhase.Aiming?1:0)} 轮\n在场 {session.ActiveCount} 球（待发 {session.Pending}）| 补行 {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0} | {ChineseText.Session(session.Phase)}\n<size=65%>待确认奖励 +{campaign?.PendingBonus ?? 0}（下关生效）| 配置：{ChineseText.Configuration(flow.ConfigurationLabel)}</size>";
        }
    }
}
