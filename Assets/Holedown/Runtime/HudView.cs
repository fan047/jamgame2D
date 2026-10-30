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
            status.text=$"Round {session.Round + (session.Phase==SessionPhase.Aiming?1:0)}   |   Balls {session.ActiveCount} (+{session.Pending})\nRows {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0}   |   {session.Phase}\n<size=65%>Config: {flow.ConfigurationLabel}</size>";
        }
    }
}
