using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BBB.Runtime
{
    /// <summary>Approved Atelier studies, expressed in the game's 960 x 540 safe stage.</summary>
    public static class AtelierUi
    {
        public static readonly Color Ink = UiSkin.Hex("#192c3d"), Paper = UiSkin.Hex("#efebe2"), Muted = UiSkin.Hex("#596663"), Gold = UiSkin.Hex("#dfcf99"), Night = UiSkin.Hex("#111729"), Light = UiSkin.Hex("#f1eee6"), Sub = UiSkin.Hex("#b6c4ce"), Mint = UiSkin.Hex("#a8cec7");
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Sprites.Clear();
        public static Sprite Sprite(string path)
        {
            if (Sprites.TryGetValue(path, out var s) && s != null) return s;
            var t = Resources.Load<Texture2D>(path);
            if (t == null) return null;
            return Sprites[path] = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
        public static RectTransform Panel(Transform p, string name, float x, float y, float w, float h, Color c)
        {
            var r = UiFactory.Panel(p, name, new Vector2(x,y), new Vector2(w,h), c);
            r.GetComponent<Image>().raycastTarget = false; return r;
        }
        public static Text Text(Transform p, string name, float x, float y, float w, float h, string text, int size = 16, Color? color = null, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = UiFactory.Label(p, name, new Vector2(x,y), new Vector2(w,h), text, size, align, color ?? Ink);
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
        /// <summary>板の縁。1px の線 4 本（フラットでも板と背景を分けるため。game-design §8.3）。</summary>
        public static void Edge(RectTransform r, Color c, float t = 1f)
        {
            var w = r.sizeDelta.x; var h = r.sizeDelta.y;
            foreach (var (n, x, y, ew, eh) in new[] { ("EdgeT", 0f, h * .5f - t * .5f, w, t), ("EdgeB", 0f, -h * .5f + t * .5f, w, t), ("EdgeL", -w * .5f + t * .5f, 0f, t, h), ("EdgeR", w * .5f - t * .5f, 0f, t, h) })
                Panel(r, n, x, y, ew, eh, c);
        }
        /// <summary>数値の札。アイコン + 小見出し + 太い値（game-design §8.2）。戻り値は値の Text。</summary>
        public static Text Tile(Transform p, string name, float x, float y, float w, Sprite icon, string caption, Color? valueColor = null, float h = 44)
        {
            var r = UiSkin.Rect(p, name, new Vector2(x, y), new Vector2(w, h));
            float left = -w * .5f;
            if (icon != null) Art(r, "Icon", left + 15, 0, 26, 26, icon);
            float textLeft = left + (icon != null ? 36 : 2), textW = w - (icon != null ? 38 : 4);
            Text(r, "Caption", textLeft + textW * .5f, 10, textW, 14, caption, 10, Sub);
            return Text(r, "Value", textLeft + textW * .5f, -9, textW, 22, "—", 17, valueColor ?? Light, true);
        }
        public static Button Button(Transform p, string name, float x, float y, float w, string text, Action action, Color? bg = null, Color? fg = null, float h = 44, Color? edge = null, int fontSize = 14)
        {
            var r = Panel(p, name,x,y,w,h,bg ?? Ink); var im = r.GetComponent<Image>(); im.raycastTarget = true;
            if (edge != null) Edge(r, edge.Value);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = im;
            var c = b.colors; c.highlightedColor = new Color(1.15f,1.15f,1.15f); c.selectedColor = new Color(1.2f,1.2f,1.2f); c.pressedColor = new Color(.7f,.8f,.85f); c.disabledColor = new Color(.55f,.55f,.55f); b.colors = c;
            Text(r,"Label",0,0,w-(w<60?6:20),h-4,text,fontSize,fg ?? Light,true,TextAnchor.MiddleCenter);   // 小さな四角のボタンは文字の幅をぎりぎりまで取る
            MotionSound.Attach(b);
            b.onClick.AddListener(() => MotionSound.Invoke(name, action));
            r.gameObject.AddComponent<AtelierFocus>(); return b;
        }
        public static void SetText(Button b,string text) { if(b != null) b.GetComponentInChildren<Text>().text=text; }
        public static Image Art(Transform p,string name,float x,float y,float w,float h,Sprite sprite)
        {
            var im=UiSkin.Img(p,name,new Vector2(x,y),new Vector2(w,h),sprite,Color.white); im.preserveAspect=true; im.raycastTarget=false; im.enabled=sprite!=null; return im;
        }
        public static Sprite Icon(string name)
        {
            switch(name) { case "soul":return MapUiV2.Sprite("crystal"); case "sword": return Sprite("Art/UI/Icons/swords_lg"); case "book":return Sprite("Art/UI/Icons/tome"); case "potion":return MapUiV2.Sprite("heart"); case "ember":return MapUiV2.Sprite("ember"); }
            return UiSkin.Icon(name,128);
        }
        public static RectTransform Screen(Transform stage,string name,Color color,Action close,out GameObject overlay)
        {
            var dim=Panel(stage,name+"Overlay",0,0,4000,4000,new Color(0,0,0,.88f));
            dim.GetComponent<Image>().raycastTarget=true;dim.gameObject.AddComponent<AtelierModalInput>();
            var body=Panel(dim,name,0,0,960,540,color); body.GetComponent<Image>().raycastTarget=true;
            if(close!=null) Button(body,"Close",436,232,44,"×",close,Ink,Light);
            overlay=dim.gameObject; return body;
        }
        public static void Header(Transform p,string eyebrow,string title,Color? color=null)
        {
            Text(p,"Eyebrow",-254,235,400,18,eyebrow,10,color??Ink);
            Text(p,"Heading",-254,203,400,38,title,28,color??Ink,true);
        }
        public static void Clear(Transform p)
        {
            p.GetComponentInParent<AtelierModalInput>()?.RememberSelection(p);
            for(int i=p.childCount-1;i>=0;i--){var g=p.GetChild(i).gameObject;g.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(g);else UnityEngine.Object.DestroyImmediate(g);}
        }
        public static void Confirm(Transform parent,string message,Action accept,Action cancel=null)
        {
            var old=EventSystem.current?.currentSelectedGameObject;
            var panel=Screen(parent,"Confirmation",Night,null,out var root);
            panel.sizeDelta=new Vector2(520,240);
            Text(panel,"Title",0,76,456,30,"操作の確認",24,Light,true);
            Text(panel,"Message",0,18,456,74,message,17,Light);
            void Close(){root.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);if(old!=null&&old.activeInHierarchy)EventSystem.current?.SetSelectedGameObject(old);}
            var back=Button(panel,"Cancel",-116,-76,216,"キャンセル",()=>{Close();cancel?.Invoke();},UiSkin.Hex("#303c50"));
            Button(panel,"Confirm",116,-76,216,"確定する",()=>{Close();accept?.Invoke();},Gold,Ink);
            EventSystem.current?.SetSelectedGameObject(back.gameObject);
        }
    }
    public sealed class AtelierFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        Outline outline;
        public void OnSelect(BaseEventData e){if(outline==null){outline=gameObject.AddComponent<Outline>();outline.effectColor=AtelierUi.Gold;outline.effectDistance=new Vector2(2,2);}outline.enabled=true;}
        public void OnDeselect(BaseEventData e){if(outline!=null)outline.enabled=false;}
    }
}
