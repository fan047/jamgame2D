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
        [SerializeField] Renderer launcher, dangerLine;
        [SerializeField] PanelTransition panelTransition;
        bool wasVisible;
        public Image Logo => logo;
        void OnEnable() { flow.Changed += Refresh; start.onClick.AddListener(StartGame); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; start.onClick.RemoveListener(StartGame); }
        void StartGame() => flow.QueueCommand(FlowCommand.Start);
        void Refresh()
        {
            bool visible=flow.Phase==AppPhase.Title;
            panel.SetActive(visible);start.interactable=!flow.IsTransitioning;
            bool boardVisible=flow.Phase==AppPhase.Playing || flow.Phase==AppPhase.Paused;
            if(launcher)launcher.enabled=boardVisible;if(dangerLine)dangerLine.enabled=boardVisible;
            if(panelTransition && visible!=wasVisible){panelTransition.Play(visible);wasVisible=visible;}
        }
    }
}
