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
            var reloadLabel=reload.GetComponentInChildren<TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"Reload current":"Reload + Restart";
            bool playing = flow.Phase == AppPhase.Playing;
            recall.gameObject.SetActive(playing); reload.gameObject.SetActive(playing);
            status.gameObject.SetActive(playing);
            if(!playing) return;
            var session=flow.Session;
            if(session==null) { recall.interactable=false; status.text="Preparing..."; return; }
            recall.interactable=session.Phase==SessionPhase.Volley;
            var campaign=flow.Campaign;
            status.text=$"Stage {(campaign?.StageIndex ?? 0)+1}/{campaign?.StageCount ?? 1} | Per Round {flow.EntryBalls} | Round {session.Round + (session.Phase==SessionPhase.Aiming?1:0)}\nBalls {session.ActiveCount} (+{session.Pending}) | Rows {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0} | {session.Phase}\n<size=65%>Bonus +{campaign?.PendingBonus ?? 0} (next stage) | Config: {flow.ConfigurationLabel}</size>";
        }
    }
}
