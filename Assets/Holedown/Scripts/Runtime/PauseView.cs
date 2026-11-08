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
        [SerializeField] TMPro.TMP_Text help, context;
        [SerializeField] RectTransform card;
        [SerializeField] ScrollRect helpScroll, audioScroll;
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
            MineUIStyle.Button(resume,true);
            MineUIStyle.PauseLayout(card,(RectTransform)helpScroll.transform,flow.IsPlannerTest);
            if(reloadAudio)reloadAudio.gameObject.SetActive(flow.IsPlannerTest && flow.Phase==AppPhase.Paused);
            if(feedback)
            {
                string error=feedback.AudioError;
                if(audioStatus)audioStatus.text="音效版本："+feedback.AudioVersion+(string.IsNullOrEmpty(error)?"":"\n音效未切换："+error);
                if(audioNotice){audioNotice.text="音效配置错误（当前使用："+feedback.AudioVersion+"）\n"+error;audioNotice.gameObject.SetActive(!string.IsNullOrEmpty(error) && flow.Phase!=AppPhase.Paused && flow.Phase!=AppPhase.Loading);}
            }
            var reloadLabel=reload.GetComponentInChildren<TMPro.TMP_Text>();if(reloadLabel)reloadLabel.text=flow.IsPlannerTest?"重载当前关":"重载并重开";
            context.text=$"{(flow.IsPlannerTest?"策划测试　":"")}第 {(flow.Campaign?.StageIndex ?? 0)+1} / {flow.Campaign?.StageCount ?? 1} 关";
            help.text=flow.IsPlannerTest?"重新读取配置并重建当前关；文件有误时保留本次测试。"+(string.IsNullOrEmpty(flow.PlannerError)?"":"\n"+ChineseText.Error(flow.PlannerError)):"重载配置将从第 1 关重新开始。";
            audioScroll.gameObject.SetActive(flow.IsPlannerTest);
            panel.SetActive(flow.Phase == AppPhase.Paused);
            if(panel.activeSelf){Canvas.ForceUpdateCanvases();MineUIStyle.FitScrollBody(helpScroll);MineUIStyle.FitScrollBody(audioScroll);}
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
