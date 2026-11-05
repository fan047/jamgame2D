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
            var campaign=flow.Campaign;
            bool next=phase==SessionPhase.Won && campaign!=null && !campaign.IsFinalStage && !flow.IsPlannerTest;
            advance.gameObject.SetActive(!error && (next || phase==SessionPhase.Lost || (flow.IsPlannerTest && ended)));
            advance.interactable=!flow.IsTransitioning;restart.interactable=!flow.IsTransitioning;
            advanceLabel.text=flow.IsPlannerTest?"Retest stage":phase==SessionPhase.Lost?"Retry Stage":"Next Stage";
            var restartLabel=restart.GetComponentInChildren<TMP_Text>();if(restartLabel)restartLabel.text=flow.IsPlannerTest?"Stage selection":"Restart Run";
            title.text = error ? "Config error" : flow.IsPlannerTest ? phase==SessionPhase.Won?"Test stage clear":"Test defeat" : phase == SessionPhase.Won ? next ? "Stage Clear" : "Run Complete" : "Defeat";
            reason.fontSize = error ? 24 : 32;
            reason.rectTransform.sizeDelta = new Vector2(800,error ? 170 : 80);
            reason.rectTransform.anchoredPosition = new Vector2(0,error ? 10 : 20);
            reason.text = error ? flow.InitializationError : flow.IsPlannerTest ? $"ID: {flow.StageId} | Entry balls: {flow.EntryBalls}\nThis test does not advance the formal run." : phase == SessionPhase.Lost ? "Blocks reached the danger line" : next ? $"Bonus +{campaign.PendingBonus}\nNext stage: {campaign.NextEntryBalls} balls per round" : "All stages completed";
        }
    }
}
