using System;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Shared Azure controls. Interactive geometry is independent of decorative art.</summary>
    public static class AzureUiControls
    {
        public static readonly Color Muted=UiSkin.Hex("#50617d"), RuleGold=UiSkin.Hex("#bba16a");
        public static Button SquareButton(Transform p,string id,float x,float y,string label,Action action)
        {
            var b=AtelierUi.Button(p,id,x,y,52,label,action,Color.white,AzureMapSkin.Paper,48,null,26);
            var im=b.GetComponent<Image>();im.sprite=UiSkin.Frame("panel_navy_sm");im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=1.5f;
            return b;
        }
        public static Text Label(Transform p,string id,float x,float y,float w,float h,string value,int size=16,bool serif=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=AtelierUi.Text(p,id,x,y,w,h,value,size,AzureMapSkin.Ink,!serif,align);
            if(serif)AzureMapSkin.Typography(t);return t;
        }
        public static void Rule(Transform p,string id,float x,float y,float w)
        {
            AtelierUi.Panel(p,id,x,y,w,1,RuleGold);
            foreach(float side in new[]{-1f,1f})
            {var d=AtelierUi.Panel(p,id+"Tip"+side,x+side*w*.5f,y,3,3,RuleGold);d.localRotation=Quaternion.Euler(0,0,45);}
        }
        public static Button Switch(Transform p,string id,float x,float y,Func<bool> get,Action<bool> set,Action changed)
        {
            var button=AtelierUi.Button(p,id,x,y,84,"",null,Color.clear,AzureMapSkin.Paper,44);
            var rim=AtelierUi.Panel(button.transform,"Rim",0,0,84,34,RuleGold).GetComponent<Image>();rim.sprite=UiSkin.Rounded(17);rim.type=Image.Type.Sliced;
            var track=AtelierUi.Panel(button.transform,"Track",0,0,81,31,Color.white).GetComponent<Image>();track.sprite=UiSkin.Rounded(16);track.type=Image.Type.Sliced;
            var label=AtelierUi.Text(button.transform,"State",0,0,38,29,"",13,AzureMapSkin.Ink,true,TextAnchor.MiddleCenter);
            var shadow=AtelierUi.Panel(button.transform,"KnobShadow",0,-1,29,29,UiSkin.Hex("#7e879b")).GetComponent<Image>();shadow.sprite=UiSkin.Circle(64);
            var knob=AtelierUi.Panel(button.transform,"Knob",0,0,26,26,Color.white).GetComponent<Image>();knob.sprite=UiSkin.Circle(64);
            void Refresh()
            {
                RefreshSwitch(button,get());
            }
            button.onClick.AddListener(()=>{set(!get());Refresh();changed?.Invoke();});Refresh();return button;
        }
        public static void RefreshSwitch(Button button,bool on)
        {
            button.transform.Find("Track").GetComponent<Image>().color=on?UiSkin.Hex("#315cba"):UiSkin.Hex("#c7cdd5");
            button.transform.Find("Knob").GetComponent<RectTransform>().anchoredPosition=new Vector2(on?24:-24,0);
            button.transform.Find("KnobShadow").GetComponent<RectTransform>().anchoredPosition=new Vector2(on?24:-24,-1);
            var label=button.transform.Find("State").GetComponent<Text>();label.rectTransform.anchoredPosition=new Vector2(on?-15:15,0);label.text=on?"ON":"OFF";label.color=on?Color.white:AzureMapSkin.Ink;
        }
        public static Slider Slider(Transform p,string id,float x,float y,float width,float value,Action<float> changed)
        {
            var slider=UiFactory.Slider(p,id,new Vector2(x,y),new Vector2(width,44),value,changed);
            var bg=slider.transform.Find("Background").GetComponent<Image>();bg.color=UiSkin.Hex("#c0c8d8");bg.sprite=UiSkin.Rounded(4);bg.type=Image.Type.Sliced;bg.rectTransform.sizeDelta=new Vector2(width,8);
            var fill=slider.fillRect.GetComponent<Image>();fill.color=UiSkin.Hex("#416cd1");fill.sprite=UiSkin.Rounded(3);fill.type=Image.Type.Sliced;
            var handle=slider.handleRect;
            // Slider.UpdateVisuals stretches the orthogonal anchors. Fix its PARENT to a fixed-height track.
            var area=(RectTransform)handle.parent;area.anchorMin=new Vector2(0,.5f);area.anchorMax=new Vector2(1,.5f);area.sizeDelta=new Vector2(-24,28);
            handle.sizeDelta=new Vector2(24,0);handle.GetComponent<Image>().color=Color.clear;
            var gem=UiSkin.Rect(handle,"Sapphire",Vector2.zero,new Vector2(24,32));gem.gameObject.AddComponent<AzureSliderGem>().raycastTarget=false;
            MotionSound.Attach(slider);slider.gameObject.AddComponent<AtelierFocus>();return slider;
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AzureSliderGem:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float x=r.width*.5f,y=r.height*.5f;
            Diamond(vh,x,y,UiSkin.Hex("#76542e"));Diamond(vh,x-1.5f,y-1.5f,UiSkin.Hex("#efd18d"));Diamond(vh,x-4,y-4,UiSkin.Hex("#d6edff"));
            Triangle(vh,new Vector2(0,y-5),new Vector2(-x+5,0),new Vector2(0,-y+5),UiSkin.Hex("#416fe4"));
            Triangle(vh,new Vector2(0,y-5),new Vector2(0,-y+5),new Vector2(x-5,0),UiSkin.Hex("#a0dfff"));
        }
        static void Diamond(VertexHelper v,float x,float y,Color c){int i=v.currentVertCount;v.AddVert(new Vector3(0,y),c,Vector2.zero);v.AddVert(new Vector3(x,0),c,Vector2.zero);v.AddVert(new Vector3(0,-y),c,Vector2.zero);v.AddVert(new Vector3(-x,0),c,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
        static void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color color){int i=v.currentVertCount;v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);}
    }
}
