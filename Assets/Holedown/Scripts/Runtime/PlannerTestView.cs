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
            Label(panel.transform,"Planner test mode",400,64);
            selection=Label(panel.transform,"",290,34);
            Button(panel.transform,"Previous stage",-210,200,()=>Select(-1));
            Button(panel.transform,"Next stage",210,200,()=>Select(1));
            modeLabel=Label(panel.transform,"",105,30);
            Button(panel.transform,"Change ball mode",0,25,()=>{mode=(PlannerBallMode)(((int)mode+1)%3);Refresh();});
            manualRoot=Element("ManualBalls",panel.transform,new Vector2(0,-65),new Vector2(350,64),typeof(Image),typeof(InputField));
            manualRoot.GetComponent<Image>().color=new Color(.9f,.94f,1);
            var textGo=Element("Text",manualRoot.transform,Vector2.zero,new Vector2(320,60),typeof(Text));
            var text=textGo.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=28;text.alignment=TextAnchor.MiddleCenter;text.color=Color.black;text.raycastTarget=false;
            manual=manualRoot.GetComponent<InputField>();manual.textComponent=text;manual.contentType=InputField.ContentType.IntegerNumber;manual.text="12";manual.onValueChanged.AddListener(_=>Refresh());
            estimate=Label(panel.transform,"",-150,28);
            Button(panel.transform,"Start selected stage",0,-235,()=>{int.TryParse(manual.text,out int balls);flow.QueuePlannerTest(selectedId,mode,balls);});
            Button(panel.transform,"Reload configuration",0,-325,()=>flow.QueueCommand(FlowCommand.Reload));
            error=Label(panel.transform,"",-455,24);error.rectTransform.sizeDelta=new Vector2(820,180);error.color=new Color(1,.7f,.5f);
            overlay=Root("PlannerTestOverlay",canvas.transform,false);
            overlayLabel=Label(overlay.transform,"",650,24);
            Button(overlay.transform,"Stage selection",0,745,()=>flow.QueueCommand(FlowCommand.PlannerSelection));
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
            var label=go.GetComponent<TMP_Text>();label.font=font;label.fontSize=size;label.text=text;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
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
            overlayLabel.text=$"PLANNER TEST | {flow.StageId} | Effective balls {flow.EntryBalls}";
            overlayError.text=flow.PlannerError??"";
            if(!selecting)return;
            int index=0;
            if(flow.Stages!=null && flow.Stages.Count>0)
            {
                for(int i=0;i<flow.Stages.Count;i++)if(flow.Stages[i].Id==selectedId)index=i;
                selectedId=flow.Stages[index].Id;selection.text=$"Stage {index+1}/{flow.Stages.Count} | ID: {selectedId}";
            }
            modeLabel.text=mode==PlannerBallMode.BaseOnly?"Base balls only (no prior rewards)":mode==PlannerBallMode.AllPreviousRewards?"All previous rewards":"Manual entry balls";
            manualRoot.SetActive(mode==PlannerBallMode.Manual);
            try{int.TryParse(manual.text,out int balls);estimate.text=$"Effective entry balls: {flow.PreviewPlannerEntryBalls(selectedId,mode,balls)}";}
            catch(Exception e){estimate.text=e.Message;}
            error.text=flow.PlannerError??"Reload reads both files. Normal launch is unaffected.";
        }
        void OnDestroy(){if(flow)flow.Changed-=Refresh;if(panel)Destroy(panel);if(overlay)Destroy(overlay);}
    }
}
