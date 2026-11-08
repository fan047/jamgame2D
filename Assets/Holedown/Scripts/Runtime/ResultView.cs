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
        [SerializeField] TMP_Text context, rewardSummary, nextBalls, completedCount;
        [SerializeField] ScrollRect bodyScroll;
        string shownReason;
        bool wasVisible;
        Vector2 secondaryPosition;
        void Awake(){secondaryPosition=restart.GetComponent<RectTransform>().anchoredPosition;}
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
            bool primaryRestart=error || (!next && !flow.IsPlannerTest && phase!=SessionPhase.Lost);
            MineUIStyle.Button(restart,primaryRestart);MineUIStyle.Button(advance,true);
            context.text=error?"水晶矿洞":$"{(flow.IsPlannerTest?"策划测试　":"")}第 {(campaign?.StageIndex ?? 0)+1} / {campaign?.StageCount ?? 1} 关";
            rewardSummary.gameObject.SetActive(next);nextBalls.gameObject.SetActive(next);
            completedCount.gameObject.SetActive(!error && !flow.IsPlannerTest && phase==SessionPhase.Won && !next);
            rewardSummary.text=$"<size=42%>本关奖励　</size>+{campaign?.PendingBonus ?? 0}<size=42%> 球</size>";
            nextBalls.text=$"<size=54%>下一关每轮球数　</size>{campaign?.NextEntryBalls ?? flow.EntryBalls}<size=54%> 球</size>";
            completedCount.text=$"{campaign?.StageCount ?? 1} / {campaign?.StageCount ?? 1}\n<size=36%>已完成全部关卡</size>";
            reason.text = error ? ChineseText.Error(flow.InitializationError) : flow.IsPlannerTest ? $"关卡标识：{flow.StageId}\n入关 {flow.EntryBalls} 球\n本次测试不推进正式流程。" : phase == SessionPhase.Lost ? "方块到达危险线" : "";
            bodyScroll.gameObject.SetActive(!string.IsNullOrEmpty(reason.text));
            if(reason.text!=shownReason){shownReason=reason.text;Canvas.ForceUpdateCanvases();MineUIStyle.FitScrollBody(bodyScroll);reason.ForceMeshUpdate(true);bodyScroll.verticalNormalizedPosition=1;}
            // A sole primary action occupies the same fixed action slot. Layout
            // is serialized once; refresh changes content/identity, not styling.
            restart.GetComponent<RectTransform>().anchoredPosition=primaryRestart?advance.GetComponent<RectTransform>().anchoredPosition:secondaryPosition;
        }
    }
}
