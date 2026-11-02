using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class StartScreenView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] GameObject panel;
        [SerializeField] Button start;
        [SerializeField] Image logo;
        public Image Logo => logo;
        void OnEnable() { flow.Changed += Refresh; start.onClick.AddListener(StartGame); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; start.onClick.RemoveListener(StartGame); }
        void StartGame() => flow.QueueCommand(FlowCommand.Start);
        void Refresh() { panel.SetActive(flow.Phase == AppPhase.Title); start.interactable = !flow.IsTransitioning; }
    }
}
