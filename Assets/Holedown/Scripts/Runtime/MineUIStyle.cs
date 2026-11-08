using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Design pixels are scaled to the existing 940-unit world-space stage,
    // never to Screen.width/height. BoardCamera owns the window letterboxing.
    public static class MineUIStyle
    {
        public const float Units = 940f / 540f;
        public const float ButtonCut = 6 * Units, PanelCut = 12 * Units;
        public const float BodySize = 16 * Units, ButtonSize = 18 * Units;
        public static readonly Color Ink = Hex(0x151E31), Panel = Hex(0x19263A), Secondary = Hex(0x2C3C53);
        public static readonly Color Ivory = Hex(0xF5F0D8), Muted = Hex(0xA5B0C2), Reward = Hex(0xE1A24B);
        public static readonly Color Disabled = Hex(0x263348), DisabledText = Hex(0x6E7D93);
        static Color Hex(uint rgb) => new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);
        public static void Text(TMP_Text text,float size, bool muted=false)
        {
            text.fontSize=size;text.enableAutoSizing=false;text.color=muted?Muted:Ivory;
            text.raycastTarget=false;
        }
        public static void PauseLayout(RectTransform card,RectTransform help,bool planner)
        {
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,(planner?580:380)*Units);
            help.anchoredPosition=new Vector2(0,-(planner?380:310)*Units);
        }
        public static void Availability(Button button,bool enabled)
        {
            button.interactable=enabled;
            var text=button.GetComponentInChildren<TMP_Text>(true);if(text)text.color=enabled?Ivory:DisabledText;
        }
        public static void FitScrollBody(ScrollRect scroll)
        {
            if(!scroll || !scroll.content)return;
            var text=scroll.content.GetComponent<TMP_Text>();if(!text)return;
            // Preferred geometry, not smaller fonts, accommodates arbitrarily
            // long error/version strings while actions stay outside the viewport.
            float height=text.GetPreferredValues(text.text,scroll.content.rect.width,float.PositiveInfinity).y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height+2);
        }
        public static void Button(Button button, bool primary=false)
        {
            if(!button)return;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);
            colors.pressedColor=new Color(.88f,.88f,.88f);colors.disabledColor=new Color(.55f,.60f,.68f);colors.colorMultiplier=1;colors.fadeDuration=.1f;button.colors=colors;
            if(button.targetGraphic)button.targetGraphic.color=primary?Ivory:Secondary;
            var label=button.GetComponentInChildren<TMP_Text>(true);if(label){label.color=primary?Ink:Ivory;label.raycastTarget=false;}
        }
    }
}
