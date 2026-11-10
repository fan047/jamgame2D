using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class AudioSettingsView : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] FeedbackPlayer feedback;
        [SerializeField] RectTransform titlePanel, pauseCard;
        [SerializeField] TMP_FontAsset font;
        RectTransform overlay;
        Slider music;
        Button opener;
        CanvasGroup parentMenu;
        AppPhase returnPhase;
        bool menuWasInteractable;

        void Start()
        {
            var top = new Vector2(0.5f, 1f);
            var titleSettings = MakeButton(titlePanel, "AudioSettingsButton", "设置", top, new Vector2(194, -51), new Vector2(96, 42));
            var pauseSettings = MakeButton(pauseCard, "AudioSettingsButton", "设置", top, new Vector2(141, -42), new Vector2(86, 36));
            titleSettings.onClick.AddListener(() => Open(titleSettings, AppPhase.Title));
            pauseSettings.onClick.AddListener(() => Open(pauseSettings, AppPhase.Paused));

            overlay = Rect(transform, "AudioSettingsPanel", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(overlay, Vector2.zero, Vector2.zero);
            overlay.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.05f, 0.09f, 0.85f);
            var card = Rect(overlay, "Card", new Vector2(.5f, .5f), Vector2.zero, new Vector2(386, 322));
            var background = card.gameObject.AddComponent<CutCornerImage>();
            background.Cut = MineUIStyle.PanelCut;
            background.color = MineUIStyle.Panel;
            background.raycastTarget = false;
            Label(card, "Title", "音量设置", top, new Vector2(0, -45), new Vector2(310, 42), 28, TextAlignmentOptions.Center);
            music = MakeSlider(card, "MusicVolume", "背景音乐", 103, feedback.MusicVolume, feedback.SetMusicVolume);
            MakeSlider(card, "EffectsVolume", "音效音量", 181, feedback.EffectsVolume, feedback.SetEffectsVolume);
            var done = MakeButton(card, "Close", "完成", top, new Vector2(0, -274), new Vector2(170, 44), true);
            done.onClick.AddListener(Close);
            overlay.gameObject.SetActive(false);
        }

        void OnEnable() => flow.Changed += CloseIfPhaseChanged;

        void OnDisable()
        {
            flow.Changed -= CloseIfPhaseChanged;
            Close();
        }

        void Update()
        {
            if (overlay.gameObject.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true && flow.UserInputAllowed)
                Close();
        }

        void Open(Button button, AppPhase phase)
        {
            if (overlay.gameObject.activeSelf || flow.Phase != phase || !flow.UserInputAllowed) return;
            opener = button;
            returnPhase = phase;
            parentMenu = button.GetComponentInParent<CanvasGroup>();
            if (parentMenu)
            {
                menuWasInteractable = parentMenu.interactable;
                parentMenu.interactable = false;
            }
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(music.gameObject);
        }

        void CloseIfPhaseChanged()
        {
            if (flow.Phase != returnPhase) Close();
        }

        void Close()
        {
            if (!overlay || !overlay.gameObject.activeSelf) return;
            overlay.gameObject.SetActive(false);
            if (parentMenu) parentMenu.interactable = menuWasInteractable;
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(flow.Phase == returnPhase && opener && opener.isActiveAndEnabled ? opener.gameObject : null);
            PlayerPrefs.Save();
        }

        Slider MakeSlider(RectTransform parent, string name, string title, float y, int initial, System.Action<int> changed)
        {
            var top = new Vector2(.5f, 1);
            Label(parent, name + "Label", title, top, new Vector2(-79, -y), new Vector2(164, 30), 18, TextAlignmentOptions.Left);
            var value = Label(parent, name + "Value", initial + "%", top, new Vector2(121, -y), new Vector2(80, 30), 16, TextAlignmentOptions.Right);
            var rect = Rect(parent, name, top, new Vector2(0, -y - 30), new Vector2(320, 32));
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
            var track = Rect(rect, "Track", new Vector2(.5f, .5f), Vector2.zero, new Vector2(306, 8));
            var trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            trackImage.color = MineUIStyle.Secondary;
            trackImage.raycastTarget = false;
            var fill = Rect(track, "Fill", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(fill, Vector2.zero, Vector2.zero);
            var fillImage = fill.gameObject.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = MineUIStyle.Reward;
            fillImage.raycastTarget = false;

            var handleArea = Rect(rect, "HandleArea", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            handleArea.anchorMin = new Vector2(0, .5f);
            handleArea.anchorMax = new Vector2(1, .5f);
            handleArea.offsetMin = new Vector2(7 * MineUIStyle.Units, 0);
            handleArea.offsetMax = new Vector2(-7 * MineUIStyle.Units, 0);
            var handle = Rect(handleArea, "Handle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(14, 24));
            var handleImage = handle.gameObject.AddComponent<CutCornerImage>();
            handleImage.Cut = 3 * MineUIStyle.Units;
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(initial);
            slider.onValueChanged.AddListener(volume =>
            {
                int percent = Mathf.RoundToInt(volume);
                value.text = percent + "%";
                changed(percent);
            });
            return slider;
        }

        Button MakeButton(Transform parent, string name, string title, Vector2 anchor, Vector2 position, Vector2 size, bool primary = false)
        {
            var rect = Rect(parent, name, anchor, position, size);
            var surface = rect.gameObject.AddComponent<CutCornerImage>();
            surface.Cut = MineUIStyle.ButtonCut;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = surface;
            rect.gameObject.AddComponent<ButtonPressMotion>();
            var label = Label(rect, "Label", title, new Vector2(.5f, .5f), Vector2.zero, size, 16, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
            MineUIStyle.Button(button, primary);
            return button;
        }

        TMP_Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            var rect = Rect(parent, name, anchor, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.alignment = align;
            MineUIStyle.Text(label, fontSize * MineUIStyle.Units);
            return label;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position * MineUIStyle.Units;
            rect.sizeDelta = size * MineUIStyle.Units;
            return rect;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min * MineUIStyle.Units;
            rect.offsetMax = max * MineUIStyle.Units;
        }
    }
}
