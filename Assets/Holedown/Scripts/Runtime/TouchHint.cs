using TMPro;
using UnityEngine;

namespace Holedown
{
    // Static hint line that switches to touch wording on mobile builds. Desktop keeps the
    // mouse and keyboard text exactly as before.
    public sealed class TouchHint : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] string desktop;
        [SerializeField] string touch;
        void Awake() { Apply(); }
        public void Configure(TMP_Text target, string desktopText, string touchText)
        { label = target; desktop = desktopText; touch = touchText; Apply(); }
        public void Apply()
        {
            if (!label) label = GetComponent<TMP_Text>();
            if (!label || string.IsNullOrEmpty(touch)) return;
            label.text = Application.isMobilePlatform ? touch : desktop;
        }
        public string Desktop => desktop;
        public string Touch => touch;
    }
}
