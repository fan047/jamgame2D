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
        void OnEnable() { flow.Changed += Refresh; restart.onClick.AddListener(flow.Restart); Refresh(); }
        void OnDisable() { flow.Changed -= Refresh; restart.onClick.RemoveListener(flow.Restart); }
        void Refresh()
        {
            bool error = !string.IsNullOrEmpty(flow.InitializationError);
            var phase = flow.Session?.Phase ?? SessionPhase.Initializing;
            bool ended = phase == SessionPhase.Won || phase == SessionPhase.Lost;
            panel.SetActive(ended || error);
            title.text = error ? "Config error" : phase == SessionPhase.Won ? "Victory" : "Defeat";
            reason.fontSize = error ? 24 : 32;
            reason.rectTransform.sizeDelta = new Vector2(800,error ? 170 : 80);
            reason.rectTransform.anchoredPosition = new Vector2(0,error ? 10 : 20);
            reason.text = error ? flow.InitializationError : phase == SessionPhase.Lost ? "Blocks reached the danger line" : "Target destroyed";
        }
    }
}
