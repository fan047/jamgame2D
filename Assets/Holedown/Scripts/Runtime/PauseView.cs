using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class PauseView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] GameObject panel;
        [SerializeField] Button pause;
        [SerializeField] Button resume;
        [SerializeField] Button reload;
        void OnEnable()
        {
            flow.Changed += Refresh; pause.onClick.AddListener(Pause);
            resume.onClick.AddListener(Continue); reload.onClick.AddListener(Reload); Refresh();
        }
        void OnDisable()
        {
            flow.Changed -= Refresh; pause.onClick.RemoveListener(Pause);
            resume.onClick.RemoveListener(Continue); reload.onClick.RemoveListener(Reload);
        }
        void Pause() { if(flow.UserInputAllowed) flow.RequestPause(); }
        void Continue() => flow.QueueCommand(FlowCommand.Continue);
        void Reload() => flow.QueueCommand(FlowCommand.Reload);
        void Refresh()
        {
            panel.SetActive(flow.Phase == AppPhase.Paused);
            var phase = flow.Session?.Phase;
            pause.gameObject.SetActive(flow.Phase == AppPhase.Playing && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
        }
    }
}
