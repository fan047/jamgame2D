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
        void OnEnable() { flow.Changed += Refresh; recall.onClick.AddListener(flow.RequestRecall); reload.onClick.AddListener(flow.Restart); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; recall.onClick.RemoveListener(flow.RequestRecall); reload.onClick.RemoveListener(flow.Restart); }
        void Refresh()
        {
            var session=flow.Session;
            if(session==null) { recall.interactable=false; status.text="Preparing..."; return; }
            recall.interactable=session.Phase==SessionPhase.Volley;
            var campaign=flow.Campaign;
            status.text=$"Stage {(campaign?.StageIndex ?? 0)+1}/{campaign?.StageCount ?? 1} | Per Round {campaign?.EntryBalls ?? flow.Settings.ballsPerRound} | Round {session.Round + (session.Phase==SessionPhase.Aiming?1:0)}\nBalls {session.ActiveCount} (+{session.Pending}) | Rows {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0} | {session.Phase}\n<size=65%>Bonus +{campaign?.PendingBonus ?? 0} (next stage) | Config: {flow.ConfigurationLabel}</size>";
        }
    }
}
