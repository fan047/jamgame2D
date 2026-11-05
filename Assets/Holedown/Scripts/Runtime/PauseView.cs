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
            var reloadLabel=reload.GetComponentInChildren<TMPro.TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"Reload current stage":"Reload + Restart";
            var subtitle=panel.transform.Find("Help")?.GetComponent<TMPro.TMP_Text>();
            if(subtitle)subtitle.text=flow.IsPlannerTest?"Reload both files and rebuild this stage.\nInvalid files preserve the current test.":"Continue this round, or reload files\nand restart from Stage 1.";
            panel.SetActive(flow.Phase == AppPhase.Paused);
            var phase = flow.Session?.Phase;
            pause.gameObject.SetActive(flow.Phase == AppPhase.Playing && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
        }
    }
}
