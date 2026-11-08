using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text reason;
        [SerializeField] Button restart;
        [SerializeField] Button advance;
        [SerializeField] TMP_Text advanceLabel;
        [SerializeField] PanelTransition panelTransition;
        bool wasVisible;
        void OnEnable() { flow.Changed += Refresh; restart.onClick.AddListener(RestartRun); advance.onClick.AddListener(Advance); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; restart.onClick.RemoveListener(RestartRun); advance.onClick.RemoveListener(Advance); }
        void RestartRun() => flow.QueueCommand(!string.IsNullOrEmpty(flow.InitializationError) ? FlowCommand.Reload : FlowCommand.RestartRun);
        void Advance() => flow.QueueCommand(flow.IsPlannerTest || flow.Session?.Phase == SessionPhase.Lost ? FlowCommand.RetryStage : FlowCommand.NextStage);
        void Refresh()
        {
            bool error = !string.IsNullOrEmpty(flow.InitializationError);
            var phase = flow.Session?.Phase ?? SessionPhase.Initializing;
            bool ended = flow.Phase == AppPhase.Playing && (phase == SessionPhase.Won || phase == SessionPhase.Lost);
            panel.SetActive(ended || error);
            if (panelTransition)
            {
                var visible = panel.activeSelf;
                if (visible != wasVisible) { panelTransition.Play(visible); wasVisible = visible; }
            }
            var campaign=flow.Campaign;
            bool next=phase==SessionPhase.Won && campaign!=null && !campaign.IsFinalStage && !flow.IsPlannerTest;
            advance.gameObject.SetActive(!error && (next || phase==SessionPhase.Lost || (flow.IsPlannerTest && ended)));
            advance.interactable=!flow.IsTransitioning;restart.interactable=!flow.IsTransitioning;
            advanceLabel.text=flow.IsPlannerTest?"重测本关":phase==SessionPhase.Lost?"重试本关":"进入下一关";
            var restartLabel=restart.GetComponentInChildren<TMP_Text>();if(restartLabel)restartLabel.text=error?"重新读取配置":flow.IsPlannerTest?"返回选关":"重新开始全程";
            title.text = error ? "配置错误" : flow.IsPlannerTest ? phase==SessionPhase.Won?"测试通关":"测试失败" : phase == SessionPhase.Won ? next ? "本关通关" : "全程通关" : "挑战失败";
            reason.enableAutoSizing = true; reason.fontSizeMin = 18; reason.fontSizeMax = error ? 24 : 32;
            reason.fontSize = error ? 24 : 32;
            reason.rectTransform.sizeDelta = new Vector2(670,190);
            reason.rectTransform.anchoredPosition = new Vector2(0,50);
            var primary=new Color(0.88f,0.66f,0.27f);var secondary=new Color(0.20f,0.30f,0.40f);
            restart.GetComponent<Image>().color=error || (!next && !flow.IsPlannerTest && phase!=SessionPhase.Lost)?primary:secondary;
            advance.GetComponent<Image>().color=primary;
            var restartText=restart.GetComponentInChildren<TMP_Text>();if(restartText)restartText.color=restart.GetComponent<Image>().color==primary?new Color(0.10f,0.16f,0.23f):Color.white;
            advanceLabel.color=new Color(0.10f,0.16f,0.23f);
            reason.text = error ? ChineseText.Error(flow.InitializationError) : flow.IsPlannerTest ? $"关卡标识：{flow.StageId} | 入关 {flow.EntryBalls} 球\n本次测试不推进正式流程。" : phase == SessionPhase.Lost ? "方块到达危险线" : next ? $"本关奖励 +{campaign.PendingBonus}\n下一关每轮 {campaign.NextEntryBalls} 球" : "已完成全部关卡";
        }
    }
}
