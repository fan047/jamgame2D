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
        void OnEnable() { flow.Changed += Refresh; recall.onClick.AddListener(flow.RequestRecall); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; recall.onClick.RemoveListener(flow.RequestRecall); }
        void Refresh()
        {
            var session=flow.Session;
            if(session==null) { recall.interactable=false; status.text="Preparing..."; return; }
            recall.interactable=session.Phase==SessionPhase.Volley;
            status.text=$"Round {session.Round + (session.Phase==SessionPhase.Aiming?1:0)}   |   Balls {session.ActiveCount} (+{session.Pending})\nRows {flow.Board?.IncomingCursor ?? 0}/{flow.Board?.IncomingCount ?? 0}   |   {session.Phase}";
        }
    }
}
