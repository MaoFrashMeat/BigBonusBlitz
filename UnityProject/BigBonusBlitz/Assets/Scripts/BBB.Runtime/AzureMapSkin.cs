using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Generated art and native controls for the illustrated chapter map.</summary>
    public static class AzureMapSkin
    {
        public const float Width=1170, Height=540;
        public static readonly Color Ink=UiSkin.Hex("#142948"), Gold=UiSkin.Hex("#e9ce86"), Navy=UiSkin.Hex("#162b4a"), Paper=UiSkin.Hex("#fcf5e6");
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static Font serif;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){sprites.Clear();serif=null;}
        public static Font Serif { get { if(serif==null)serif=Resources.Load<Font>("Fonts/NotoSerifJP-SemiBold");return serif!=null?serif:UiFactory.Font; } }

        public static Sprite Sprite(string name)
        {
            if(sprites.TryGetValue(name,out var cached)&&cached!=null)return cached;
            var t=Resources.Load<Texture2D>("Art/UI/AzureMap/"+name);if(t==null)return null;
            // These are source alpha bounds, not a destructive crop. Keep the original PNGs intact.
            Rect rect=new Rect(0,0,t.width,t.height);Vector4 border=Vector4.zero;
            if(name=="button-navy")rect=new Rect(t.width*16f/2172,t.height*(724-487f)/724,t.width*2139f/2172,t.height*284f/724);
            if(name=="button-gold")rect=new Rect(t.width*9f/2126,t.height*(740-505f)/740,t.width*2109f/2126,t.height*286f/740);
            if(name=="parchment")rect=new Rect(t.width*9f/1086,t.height*(1448-1407f)/1448,t.width*1074f/1086,t.height*1403f/1448);
            if(name.StartsWith("button-"))border=new Vector4(rect.width*.085f,rect.height*.2f,rect.width*.085f,rect.height*.2f);
            return sprites[name]=UnityEngine.Sprite.Create(t,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
        }
        public static Image Art(Transform parent,string name,float x,float y,float w,float h,string asset)
        {var im=UiSkin.Img(parent,name,new Vector2(x,y),new Vector2(w,h),Sprite(asset),Color.white);im.raycastTarget=false;return im;}
        public static void Backdrop(Transform parent)
        {
            var im=Art(parent,"AzureWorld",0,0,Width,Height,"world");
            im.gameObject.AddComponent<AzureMapBleed>().Apply();
        }
        public static void Typography(Text text,bool light=false)
        {
            text.font=Serif;text.fontStyle=FontStyle.Normal;
            if(light){var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.04f,.09f,.17f,.6f);shadow.effectDistance=new Vector2(0,-1);}
        }
        public static Button Button(Transform parent,string name,float x,float y,float width,string label,Action action,bool primary=false,float height=44,string icon=null,int fontSize=16)
        {
            var button=AtelierUi.Button(parent,name,x,y,width,label,action,Color.white,primary?Ink:Paper,height,null,fontSize);
            var image=button.GetComponent<Image>();image.sprite=Sprite(primary?"button-gold":"button-navy");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=8;
            if(width<70){image.sprite=null;image.type=Image.Type.Simple;image.color=Navy;AtelierUi.Edge(image.rectTransform,Gold);}
            var text=button.GetComponentInChildren<Text>();Typography(text,!primary);
            float inset=width<70?6:40;
            text.rectTransform.sizeDelta=new Vector2(width-inset,height-6);
            if(icon!=null)
            {
                string asset=icon=="settings"?"gear":icon=="trophy"?"star_emblem":icon=="sword"?"swords_lg":icon;
                var sprite=AtelierUi.Sprite("Art/UI/Icons/"+asset)??AtelierUi.Icon(icon);
                AtelierUi.Art(button.transform,"Icon",-width*.5f+30,0,20,20,sprite);
                text.rectTransform.anchoredPosition=new Vector2(12,0);text.rectTransform.sizeDelta=new Vector2(width-68,height-6);
            }
            return button;
        }
        public static Text Resource(Transform p,string name,float x,float width,Sprite icon,string caption)
        {
            var panel=AtelierUi.Panel(p,name,x,236,width,44,Navy);
            // A quiet opaque navy surface keeps the live balances readable over clouds.
            AtelierUi.Panel(panel,"GoldRule",0,-22,width,1,Gold);
            AtelierUi.Art(panel,"Icon",-width*.5f+17,0,27,27,icon);
            var label=AtelierUi.Text(panel,"Caption",19,14,width-46,10,caption,8,Paper);
            var value=AtelierUi.Text(panel,"Value",19,-7,width-46,28,"",17,Paper);
            Typography(value,true);return value;
        }
        public static void NodeLabel(Transform parent,string name,Vector2 position,string label,bool muted)
        {
            var im=Art(parent,name,position.x,position.y,140,22,"button-navy");im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=16;
            im.rectTransform.anchorMin=im.rectTransform.anchorMax=new Vector2(0,1);
            var text=AtelierUi.Text(im.transform,"Caption",0,0,128,20,label,11,muted?UiSkin.Hex("#bcc9d8"):Paper,false,TextAnchor.MiddleCenter);
            Typography(text,true);
        }
    }

    /// <summary>Keep world and foreground registered to the canvas while controls follow SafeStage.</summary>
    public sealed class AzureMapBleed : MonoBehaviour
    {
        readonly Vector3[] corners=new Vector3[4];
        void LateUpdate()=>Apply();
        public void Apply()
        {
            var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
            var rt=(RectTransform)transform;var parent=(RectTransform)rt.parent;
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            Vector3 lo=parent.InverseTransformPoint(corners[0]),hi=parent.InverseTransformPoint(corners[2]);
            float w=hi.x-lo.x,h=hi.y-lo.y;if(w<=0||h<=0)return;
            const float aspect=1844f/853;
            float cover=Mathf.Max(w/aspect,h);
            rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2((lo.x+hi.x)*.5f,(lo.y+hi.y)*.5f);
            rt.sizeDelta=new Vector2(cover*aspect,cover);
        }
    }
}
