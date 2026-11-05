using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Deliberately plain test controls, created only by the explicit --planner-test entry.
    // Formal UI art/navigation remains unchanged.
    public sealed class PlannerTestView : MonoBehaviour
    {
        GameFlowController flow;
        GameObject panel, overlay, manualRoot;
        TMP_FontAsset font;
        TMP_Text selection, modeLabel, estimate, error, overlayError, overlayLabel;
        InputField manual;
        string selectedId;
        PlannerBallMode mode;
        public void Configure(GameFlowController value)
        {
            flow=value;
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            font=canvas.GetComponentInChildren<TMP_Text>(true).font;
            panel=Root("PlannerSelection",canvas.transform,true);
            Label(panel.transform,"策划测试模式",400,64);
            selection=Label(panel.transform,"",290,34);
            Button(panel.transform,"上一关",-210,200,()=>Select(-1));
            Button(panel.transform,"下一关",210,200,()=>Select(1));
            modeLabel=Label(panel.transform,"",105,30);
            Button(panel.transform,"切换球数方式",0,25,()=>{mode=(PlannerBallMode)(((int)mode+1)%3);Refresh();});
            manualRoot=Element("ManualBalls",panel.transform,new Vector2(0,-65),new Vector2(350,64),typeof(Image),typeof(InputField));
            manualRoot.GetComponent<Image>().color=new Color(.9f,.94f,1);
            var textGo=Element("Text",manualRoot.transform,Vector2.zero,new Vector2(320,60),typeof(Text));
            var text=textGo.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=28;text.alignment=TextAnchor.MiddleCenter;text.color=Color.black;text.raycastTarget=false;
            manual=manualRoot.GetComponent<InputField>();manual.textComponent=text;manual.contentType=InputField.ContentType.IntegerNumber;manual.text="12";manual.onValueChanged.AddListener(_=>Refresh());
            estimate=Label(panel.transform,"",-150,28);
            Button(panel.transform,"开始测试本关",0,-235,()=>{int.TryParse(manual.text,out int balls);flow.QueuePlannerTest(selectedId,mode,balls);});
            Button(panel.transform,"重新读取配置",0,-325,()=>flow.QueueCommand(FlowCommand.Reload));
            error=Label(panel.transform,"",-455,24);error.rectTransform.sizeDelta=new Vector2(820,180);error.color=new Color(1,.7f,.5f);
            overlay=Root("PlannerTestOverlay",canvas.transform,false);
            overlayLabel=Label(overlay.transform,"",650,24);
            Button(overlay.transform,"返回选关",0,745,()=>flow.QueueCommand(FlowCommand.PlannerSelection));
            overlayError=Label(overlay.transform,"",-400,24);overlayError.rectTransform.sizeDelta=new Vector2(820,160);overlayError.color=new Color(1,.7f,.5f);
            flow.Changed+=Refresh;Refresh();
        }
        GameObject Root(string name,Transform parent,bool background)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;r.anchoredPosition=Vector2.zero;
            if(background)go.AddComponent<Image>().color=new Color(.04f,.07f,.12f,.98f);
            return go;
        }
        static GameObject Element(string name,Transform parent,Vector2 position,Vector2 size,params Type[] components)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;
            foreach(var type in components)go.AddComponent(type);return go;
        }
        TMP_Text Label(Transform parent,string text,float y,float size)
        {
            var go=Element("Label",parent,new Vector2(0,y),new Vector2(820,90),typeof(TextMeshProUGUI));
            var label=go.GetComponent<TMP_Text>();label.font=font;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=size;label.text=text;label.richText=false;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
        void Button(Transform parent,string text,float x,float y,UnityEngine.Events.UnityAction action)
        {
            var go=Element(text,parent,new Vector2(x,y),new Vector2(370,70),typeof(Image),typeof(Button));go.GetComponent<Image>().color=new Color(.1f,.4f,.5f);
            var label=Label(go.transform,text,0,28);label.rectTransform.sizeDelta=new Vector2(350,64);
            go.GetComponent<Button>().onClick.AddListener(action);
        }
        void Select(int delta)
        {
            if(flow.Stages==null || flow.Stages.Count==0)return;
            int index=0;for(int i=0;i<flow.Stages.Count;i++)if(flow.Stages[i].Id==selectedId)index=i;
            selectedId=flow.Stages[(index+delta+flow.Stages.Count)%flow.Stages.Count].Id;Refresh();
        }
        void Refresh()
        {
            if(!flow || !panel)return;
            bool selecting=flow.Phase==AppPhase.PlannerSelection;
            panel.SetActive(selecting);overlay.SetActive(!selecting && flow.Phase!=AppPhase.ConfigError && flow.Phase!=AppPhase.Loading);
            overlayLabel.text=$"策划测试 | 关卡标识：{flow.StageId} | 本次 {flow.EntryBalls} 球";
            overlayError.text=ChineseText.Error(flow.PlannerError);
            if(!selecting)return;
            int index=0;
            if(flow.Stages!=null && flow.Stages.Count>0)
            {
                for(int i=0;i<flow.Stages.Count;i++)if(flow.Stages[i].Id==selectedId)index=i;
                selectedId=flow.Stages[index].Id;selection.text=$"第 {index+1}/{flow.Stages.Count} 关 · {flow.Stages[index].Name}\n关卡标识：{selectedId}";
            }
            modeLabel.text=mode==PlannerBallMode.BaseOnly?"基础球数（不计前关奖励）":mode==PlannerBallMode.AllPreviousRewards?"拿满前关奖励":"手填入关球数（正整数）";
            manualRoot.SetActive(mode==PlannerBallMode.Manual);
            try{int.TryParse(manual.text,out int balls);estimate.text=$"本次入关球数：{flow.PreviewPlannerEntryBalls(selectedId,mode,balls)}";}
            catch(Exception e){estimate.text=ChineseText.Error(e.Message);}
            error.text=string.IsNullOrEmpty(flow.PlannerError)?"重载读取两份配置；普通启动仍走正式流程。":ChineseText.Error(flow.PlannerError);
        }
        void OnDestroy(){if(flow)flow.Changed-=Refresh;if(panel)Destroy(panel);if(overlay)Destroy(overlay);}
    }
}
