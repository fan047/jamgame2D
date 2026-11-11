using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Holedown
{
    public sealed class AudioSettingsView : MonoBehaviour
    {
        enum Dialog { Settings, RestartStage, ChangeDifficulty, EasyRetry }

        [SerializeField] GameFlowController flow;
        [SerializeField] FeedbackPlayer feedback;
        [SerializeField] RectTransform titlePanel, pauseCard;
        [SerializeField] TMP_FontAsset font;
        RectTransform overlay, settingsCard, confirmationCard;
        Slider music;
        TMP_Text settingsTitle, confirmationTitle, confirmationBody;
        TMP_Text confirmLabel, cancelLabel;
        Button normal, easy, done, confirm, cancel, restartStage;
        Button opener;
        CanvasGroup parentMenu;
        AppPhase returnPhase;
        Dialog dialog;
        GameDifficulty selectedDifficulty;
        GameDifficulty? shownDifficulty;
        bool menuWasInteractable;

        void Start()
        {
            var top = new Vector2(0.5f, 1f);
            var titleParent = titlePanel.Find("TitleContent") as RectTransform;
            if (!titleParent) titleParent = titlePanel;
            var topRight = new Vector2(1f, 1f);
            var titleSettings = MakeButton(titleParent, "AudioSettingsButton", "设置", topRight, new Vector2(-68, -51), new Vector2(96, 56));
            var pauseSettings = MakeButton(pauseCard, "AudioSettingsButton", "设置", top, new Vector2(141, -42), new Vector2(86, 36));
            titleSettings.onClick.AddListener(() => Open(titleSettings, AppPhase.Title));
            pauseSettings.onClick.AddListener(() => Open(pauseSettings, AppPhase.Paused));
            restartStage = MakeButton(pauseCard, "RestartCurrentStage", "重新开始本关", top, new Vector2(0, -229), new Vector2(344, 52));
            restartStage.onClick.AddListener(OpenRestart);

            overlay = Rect(transform, "AudioSettingsPanel", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(overlay, Vector2.zero, Vector2.zero);
            overlay.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.05f, 0.09f, 0.85f);
            settingsCard = MakeCard("Card", new Vector2(386, 394));
            settingsTitle = Label(settingsCard, "Title", "设置", top, new Vector2(0, -45), new Vector2(310, 42), 28, TextAlignmentOptions.Center);
            music = MakeSlider(settingsCard, "MusicVolume", "背景音乐", 103, feedback.MusicVolume, feedback.SetMusicVolume);
            MakeSlider(settingsCard, "EffectsVolume", "音效音量", 181, feedback.EffectsVolume, feedback.SetEffectsVolume);
            Label(settingsCard, "DifficultyLabel", "难度", top, new Vector2(-124, -270), new Vector2(76, 34), 18, TextAlignmentOptions.Left);
            normal = MakeButton(settingsCard, "NormalDifficulty", "普通", top, new Vector2(-17, -270), new Vector2(115, 44));
            easy = MakeButton(settingsCard, "EasyDifficulty", "简单", top, new Vector2(104, -270), new Vector2(115, 44));
            normal.onClick.AddListener(() => ChooseDifficulty(GameDifficulty.Normal));
            easy.onClick.AddListener(() => ChooseDifficulty(GameDifficulty.Easy));
            done = MakeButton(settingsCard, "Close", "完成", top, new Vector2(0, -344), new Vector2(170, 44), true);
            done.onClick.AddListener(Close);

            confirmationCard = MakeCard("Confirmation", new Vector2(408, 252));
            confirmationTitle = Label(confirmationCard, "Title", "", top, new Vector2(0, -46), new Vector2(350, 42), 25, TextAlignmentOptions.Center);
            confirmationBody = Label(confirmationCard, "Body", "", top, new Vector2(0, -116), new Vector2(340, 76), 17, TextAlignmentOptions.Center);
            cancel = MakeButton(confirmationCard, "Cancel", "", top, new Vector2(-91, -203), new Vector2(166, 46));
            confirm = MakeButton(confirmationCard, "Confirm", "", top, new Vector2(91, -203), new Vector2(166, 46), true);
            cancelLabel = cancel.GetComponentInChildren<TMP_Text>();
            confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            cancel.onClick.AddListener(Cancel);
            confirm.onClick.AddListener(Confirm);
            overlay.gameObject.SetActive(false);
            Refresh();
        }

        RectTransform MakeCard(string name, Vector2 size)
        {
            var card = Rect(overlay, name, new Vector2(.5f, .5f), Vector2.zero, size);
            var background = card.gameObject.AddComponent<CutCornerImage>();
            background.Cut = MineUIStyle.PanelCut;
            background.color = MineUIStyle.Panel;
            background.raycastTarget = false;
            return card;
        }

        void OnEnable() => flow.Changed += Refresh;

        void OnDisable()
        {
            flow.Changed -= Refresh;
            Close();
        }

        void Update()
        {
            if (overlay && overlay.gameObject.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Cancel();
        }

        void Open(Button button, AppPhase phase, bool focusDifficulty = false)
        {
            if (overlay.gameObject.activeSelf || flow.Phase != phase || !flow.UserInputAllowed) return;
            bool formal = !flow.IsPlannerTest;
            settingsCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (formal ? 394 : 322) * MineUIStyle.Units);
            settingsTitle.text = formal ? "设置" : "音量设置";
            settingsCard.Find("DifficultyLabel").gameObject.SetActive(formal);
            normal.gameObject.SetActive(formal);
            easy.gameObject.SetActive(formal);
            ((RectTransform)done.transform).anchoredPosition = new Vector2(0, -(formal ? 344 : 274) * MineUIStyle.Units);
            dialog = Dialog.Settings;
            confirmationCard.gameObject.SetActive(false);
            settingsCard.gameObject.SetActive(true);
            ShowOverlay(button.GetComponentInParent<CanvasGroup>(), button, phase);
            Select(focusDifficulty ? (flow.Difficulty == GameDifficulty.Easy ? easy.gameObject : normal.gameObject) : music.gameObject);
        }

        void ShowOverlay(CanvasGroup menu, Button button, AppPhase phase)
        {
            opener = button;
            returnPhase = phase;
            parentMenu = menu;
            if (parentMenu)
            {
                menuWasInteractable = parentMenu.interactable;
                parentMenu.interactable = false;
            }
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
        }

        void Refresh()
        {
            if (!overlay) return;
            if (shownDifficulty != flow.Difficulty)
            {
                shownDifficulty = flow.Difficulty;
                MineUIStyle.Button(normal, flow.Difficulty == GameDifficulty.Normal);
                MineUIStyle.Button(easy, flow.Difficulty == GameDifficulty.Easy);
            }
            restartStage.gameObject.SetActive(!flow.IsPlannerTest);
            if (overlay.gameObject.activeSelf && (flow.Phase != returnPhase || (dialog == Dialog.EasyRetry && !flow.OffersEasyRetry)))
                Close();
            if (!overlay.gameObject.activeSelf && flow.OffersEasyRetry)
            {
                var resultPanel = transform.Find("ResultPanel");
                ShowOverlay(resultPanel.GetComponent<CanvasGroup>(), null, AppPhase.Playing);
                ShowConfirmation(Dialog.EasyRetry, "试试简单模式？", "本关已失败 3 次。\n简单模式下，方块血量降低 20%。",
                    "保持普通", "用简单模式重试");
            }
        }

        static string DifficultyLabel(GameDifficulty difficulty) => difficulty == GameDifficulty.Easy ? "简单" : "普通";

        void ChooseDifficulty(GameDifficulty difficulty)
        {
            if (!flow.UserInputAllowed || difficulty == flow.Difficulty) return;
            if (flow.Phase == AppPhase.Title) flow.ChangeDifficulty(difficulty);
            else if (flow.Phase == AppPhase.Paused)
            {
                selectedDifficulty = difficulty;
                ShowConfirmation(Dialog.ChangeDifficulty, "切换难度？",
                    $"切换到{DifficultyLabel(difficulty)}模式后，本关会从头开始。", "取消", "切换并重开");
            }
        }

        void OpenRestart()
        {
            if (flow.Phase != AppPhase.Paused || !flow.UserInputAllowed || overlay.gameObject.activeSelf) return;
            ShowOverlay(restartStage.GetComponentInParent<CanvasGroup>(), restartStage, AppPhase.Paused);
            ShowConfirmation(Dialog.RestartStage, "重新开始本关？", "本关会从头开始，清空本关暂存奖励。", "取消", "确认重开");
        }

        void ShowConfirmation(Dialog kind, string title, string body, string cancelText, string confirmText)
        {
            dialog = kind;
            confirmationTitle.text = title;
            confirmationBody.text = body;
            cancelLabel.text = cancelText;
            confirmLabel.text = confirmText;
            settingsCard.gameObject.SetActive(false);
            confirmationCard.gameObject.SetActive(true);
            Select(cancel.gameObject);
        }

        void Confirm()
        {
            bool accepted = dialog switch
            {
                Dialog.RestartStage => flow.RestartCurrentStage(),
                Dialog.ChangeDifficulty => flow.ChangeDifficulty(selectedDifficulty),
                Dialog.EasyRetry => flow.RetryOnEasy(),
                _ => false
            };
            if (accepted) Close();
        }

        void Cancel()
        {
            if (!flow.UserInputAllowed) return;
            if (dialog == Dialog.EasyRetry) flow.DismissEasyRetry();
            else Close();
        }

        static void Select(GameObject selected)
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(selected);
        }

        void Close()
        {
            if (!overlay || !overlay.gameObject.activeSelf) return;
            overlay.gameObject.SetActive(false);
            if (parentMenu) parentMenu.interactable = menuWasInteractable;
            Select(flow.Phase == returnPhase && opener && opener.isActiveAndEnabled ? opener.gameObject : null);
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
