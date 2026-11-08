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
        [SerializeField] FeedbackPlayer feedback;
        [SerializeField] Button reloadAudio;
        [SerializeField] TMPro.TMP_Text audioStatus;
        [SerializeField] TMPro.TMP_Text audioNotice;
        [SerializeField] PanelTransition panelTransition;
        bool wasVisible;
        void OnEnable()
        {
            flow.Changed += Refresh; pause.onClick.AddListener(Pause);
            resume.onClick.AddListener(Continue); reload.onClick.AddListener(Reload);
            if(reloadAudio)reloadAudio.onClick.AddListener(ReloadAudio);
            if(feedback)feedback.AudioChanged+=Refresh;
            Refresh();
        }
        void OnDisable()
        {
            flow.Changed -= Refresh; pause.onClick.RemoveListener(Pause);
            resume.onClick.RemoveListener(Continue); reload.onClick.RemoveListener(Reload);
            if(reloadAudio)reloadAudio.onClick.RemoveListener(ReloadAudio);
            if(feedback)feedback.AudioChanged-=Refresh;
        }
        void Pause() { if(flow.UserInputAllowed) flow.RequestPause(); }
        void Continue() => flow.QueueCommand(FlowCommand.Continue);
        void Reload() => flow.QueueCommand(FlowCommand.Reload);
        void ReloadAudio() { if(feedback)feedback.ReloadAudio(); }
        void Refresh()
        {
            if(reloadAudio)reloadAudio.gameObject.SetActive(flow.IsPlannerTest && flow.Phase==AppPhase.Paused);
            if(feedback)
            {
                string error=feedback.AudioError;
                if(audioStatus)audioStatus.text="音效版本："+feedback.AudioVersion+(string.IsNullOrEmpty(error)?"":"\n音效未切换："+error);
                if(audioNotice){audioNotice.text="音效配置错误（当前使用："+feedback.AudioVersion+"）\n"+error;audioNotice.gameObject.SetActive(!string.IsNullOrEmpty(error) && flow.Phase!=AppPhase.Paused && flow.Phase!=AppPhase.Loading);}
            }
            var reloadLabel=reload.GetComponentInChildren<TMPro.TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"重载当前关":"重载并重开";
            var subtitle=panel.transform.Find("Help")?.GetComponent<TMPro.TMP_Text>();
            if(subtitle)subtitle.text=flow.IsPlannerTest?"重新读取两份配置并重建当前关。\n文件有误时保留本次测试。":"继续本轮，或重新读取配置，\n从第 1 关重新开始。";
            panel.SetActive(flow.Phase == AppPhase.Paused);
            if (panelTransition)
            {
                var visible = panel.activeSelf;
                if (visible != wasVisible) { panelTransition.Play(visible); wasVisible = visible; }
            }
            var phase = flow.Session?.Phase;
            pause.gameObject.SetActive(flow.Phase == AppPhase.Playing && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
        }
    }
}
