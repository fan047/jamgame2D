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
        void RestartRun() { if(!string.IsNullOrEmpty(flow.InitializationError))flow.ReloadAndRestart();else flow.RestartRun(); }
        void Advance() { if(flow.Session?.Phase==SessionPhase.Lost)flow.RetryStage();else flow.NextStage(); }
        void Refresh()
        {
            bool error = !string.IsNullOrEmpty(flow.InitializationError);
            var phase = flow.Session?.Phase ?? SessionPhase.Initializing;
            bool ended = phase == SessionPhase.Won || phase == SessionPhase.Lost;
            panel.SetActive(ended || error);
            var campaign=flow.Campaign;
            bool next=phase==SessionPhase.Won && campaign!=null && !campaign.IsFinalStage;
            advance.gameObject.SetActive(!error && (next || phase==SessionPhase.Lost));
            advance.interactable=!flow.IsTransitioning;restart.interactable=!flow.IsTransitioning;
            advanceLabel.text=phase==SessionPhase.Lost?"Retry Stage":"Next Stage";
            title.text = error ? "Config error" : phase == SessionPhase.Won ? next ? "Stage Clear" : "Run Complete" : "Defeat";
            reason.fontSize = error ? 24 : 32;
            reason.rectTransform.sizeDelta = new Vector2(800,error ? 170 : 80);
            reason.rectTransform.anchoredPosition = new Vector2(0,error ? 10 : 20);
            reason.text = error ? flow.InitializationError : phase == SessionPhase.Lost ? "Blocks reached the danger line" : next ? $"Bonus +{campaign.PendingBonus}\nNext stage: {campaign.NextEntryBalls} balls per round" : "All stages completed";
        }
    }
}
