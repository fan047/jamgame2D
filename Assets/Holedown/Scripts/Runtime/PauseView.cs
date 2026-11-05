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
            var reloadLabel=reload.GetComponentInChildren<TMPro.TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"重载当前关":"重载并重开";
            var subtitle=panel.transform.Find("Help")?.GetComponent<TMPro.TMP_Text>();
            if(subtitle)subtitle.text=flow.IsPlannerTest?"重新读取两份配置并重建当前关。\n文件有误时保留本次测试。":"继续本轮，或重新读取配置，\n从第 1 关重新开始。";
            panel.SetActive(flow.Phase == AppPhase.Paused);
            var phase = flow.Session?.Phase;
            pause.gameObject.SetActive(flow.Phase == AppPhase.Playing && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
        }
    }
}
