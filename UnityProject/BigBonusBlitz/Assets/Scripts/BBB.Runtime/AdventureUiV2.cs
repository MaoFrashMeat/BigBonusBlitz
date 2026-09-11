using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Construction-time presentation only; gameplay references and callbacks remain intact.</summary>
    public static class AdventureUiV2
    {
        private static readonly Color Ink = UiSkin.Hex("#101f32"), Muted = UiSkin.Hex("#b9cde2"), Gold = UiSkin.Hex("#f4cd7c");
        public static void Apply(RectTransform stage)
        {
            var basePlate = UiSkin.Img(stage, "ConsoleSurface", new Vector2(0,-134), new Vector2(960,272), null, UiSkin.Hex("#0a1423"));
            basePlate.transform.SetAsFirstSibling();
            var outerDim = stage.GetComponentInParent<Canvas>().transform.Find("BgOuterDim")?.GetComponent<Image>();
            if (outerDim != null) outerDim.color = UiSkin.Hex("#0a1423");
            Panel(stage.Find("StageCard")); Panel(stage.Find("Display")); Panel(stage.Find("Side")); Panel(stage.Find("ReelCabinet"));
            foreach (string name in new[] { "CreditInset", "PayoutInset" }) Panel(stage.Find("Display/"+name), UiSkin.Hex("#0b1727"));
            for(int i=0;i<3;i++) Panel(stage.Find("ReelCabinet/Window"+i), UiSkin.Hex("#08111c"));

            var display = stage.Find("Display");
            Label(display,"CreditLabel","所持エンバー",14,new Vector2(8,76),new Vector2(180,20));
            Label(display,"PayoutLabel","今回の獲得",14,new Vector2(8,-5),new Vector2(180,20));
            Move(display,"EmberIcon",new Vector2(-99,76),new Vector2(20,20));
            Move(display,"PayoutIcon",new Vector2(-99,-5),new Vector2(16,16));
            Move(display,"PayoutIconIn",new Vector2(-99,-5),new Vector2(8,8));
            Move(display,"CreditInset",new Vector2(0,39),new Vector2(210,44));
            Move(display,"PayoutInset",new Vector2(0,-37),new Vector2(210,36));
            Label(display,"CreditInset/CreditNum",null,34,new Vector2(-5,0),new Vector2(188,44));
            Label(display,"PayoutInset/PayoutNum",null,28,new Vector2(-5,0),new Vector2(188,36));
            Label(display,"Mode",null,12,null,new Vector2(105,32)); Label(display,"Soul",null,12,null,new Vector2(90,32));
            V2Icon(display,"EmberIcon","ember"); V2Icon(display,"SoulIcon","crystal");

            var side = stage.Find("Side");
            Label(side,"PlayerLabel","プレイヤー",13); Label(side,"BonusHead","ボーナス",13);
            Label(side,"Lv",null,20);
            Label(side,"BonusLabel",null,13,new Vector2(0,0),new Vector2(218,32));
            Move(side,"BonusGauge",new Vector2(0,-24),new Vector2(218,8));
            Label(side,"Status",null,12,new Vector2(0,-51),new Vector2(218,32));
            Gauge(side,"Exp"); Gauge(side,"BonusGauge");
            V2Icon(side,"PlayerIcon","swords"); V2Icon(side,"BonusIcon","crystal");
            var settings = side.Find("BtnSettings"); var graph = side.Find("BtnGraph");
            Tool(settings,stage,-78,"設定・音量"); Tool(graph,stage,78,"収支グラフ");
            var debug = side.Find("BtnDebug"); if(debug!=null)debug.gameObject.SetActive(false); // D shortcut remains for development.
            MainButton(stage.Find("BtnBet"),"btn_pink"); MainButton(stage.Find("BtnAuto"),"btn_blue");
            var hint=UiFactory.Label(stage,"ReelHint",new Vector2(0,-207),new Vector2(400,14),"リールをタップして停止  ·  Z / X / C",11,TextAnchor.MiddleCenter,Muted);

            var scene=stage.Find("StageCard");
            Flat(scene,"StageTag/Bg"); Flat(scene,"TorchTag/Bg");
            Gauge(scene,"TorchTag/Gauge");
            var hold=scene.Find("StageTag/Hold");
            if(hold!=null)
            {
                foreach(Transform child in hold) if(child.name.StartsWith("Link")||child.name=="ChainShade")child.gameObject.SetActive(false);
                Label(hold,"Text","進行停止",14,new Vector2(-10,0),new Vector2(180,30));
                Flat(hold,"Dim");
            }
            foreach(var text in scene.GetComponentsInChildren<Text>(true))
                if(text.name=="Title" && text.transform.parent.name=="RouteBox") { text.fontSize=13;text.color=Muted; }
            var frame=stage.Find("AtFrame");
            if(frame!=null) foreach(Transform child in frame)
            {
                if(child.name=="Glow")child.gameObject.SetActive(false);
                else if(child.name=="Core") {var rt=(RectTransform)child; var s=rt.sizeDelta;rt.sizeDelta=s.x<s.y?new Vector2(3,s.y):new Vector2(s.x,3);}
            }
        }
        private static void Panel(Transform root, Color? color=null)
        {
            if(root==null)return;
            foreach(Transform child in root)
                if(child.name=="Shadow"||child.name=="Glow"||child.name=="Sheen"||child.name.StartsWith("Rivet")||child.name=="GoldSheen")child.gameObject.SetActive(false);
            Flat(root,"Body",color??Ink); Flat(root,"Edge",UiSkin.Hex("#334d69"));
        }
        private static void Flat(Transform parent,string path,Color? color=null)
        {
            if(parent==null)return;var im=parent.Find(path)?.GetComponent<Image>(); if(im==null)return;
            im.sprite=UiSkin.Rounded(6);im.type=Image.Type.Sliced;im.color=color??Ink;
        }
        private static void Move(Transform root,string path,Vector2 pos,Vector2 size)
        { var rt=root?.Find(path) as RectTransform;if(rt==null)return;rt.anchoredPosition=pos;rt.sizeDelta=size; }
        private static void Label(Transform root,string path,string value,int font,Vector2? pos=null,Vector2? size=null)
        {
            var t=root?.Find(path)?.GetComponent<Text>();if(t==null)return;
            if(value!=null)t.text=value;t.fontSize=font;t.color=path.Contains("Num")?Gold:Muted;
            if(pos.HasValue)t.rectTransform.anchoredPosition=pos.Value;if(size.HasValue)t.rectTransform.sizeDelta=size.Value;
        }
        private static void V2Icon(Transform root,string path,string asset)
        { var im=root?.Find(path)?.GetComponent<Image>();if(im==null)return;im.sprite=MapUiV2.Sprite(asset);im.color=Color.white;im.preserveAspect=true; }
        private static void Gauge(Transform parent,string name)
        {
            var track=parent.Find(name);if(track==null)return;Flat(track,"Track",UiSkin.Hex("#263b51"));
            var fill=track.Find("Fill")?.GetComponent<Image>();if(fill!=null){fill.sprite=UiSkin.Rounded(3);fill.type=Image.Type.Sliced;}
        }
        private static void MainButton(Transform root,string art)
        {
            if(root==null)return;
            var im=root.Find("Body")?.GetComponent<Image>();if(im!=null){im.sprite=MapUiV2.Sprite(art);im.type=Image.Type.Sliced;im.color=Color.white;im.pixelsPerUnitMultiplier=im.sprite.texture.width/(art=="btn_blue"?1672f:2172f)*9;}
            foreach(string n in new[]{"Sub","Shadow","Lamp","Sheen","Bottom"}){var c=root.Find(n);if(c!=null)c.gameObject.SetActive(false);}
            Label(root,"Label",null,20,Vector2.zero);var label=root.Find("Label")?.GetComponent<Text>();if(label!=null)label.color=Color.white;
        }
        private static void Tool(Transform root,Transform stage,float x,string title)
        {
            if(root==null)return;root.SetParent(stage,false);var rt=(RectTransform)root;rt.anchoredPosition=new Vector2(x,-242);rt.sizeDelta=new Vector2(144,44);
            foreach(Transform c in root)if(c.GetComponent<Image>()!=null)((RectTransform)c).sizeDelta=rt.sizeDelta;
            Panel(root);Label(root,"Label",title,14,Vector2.zero,new Vector2(136,40));
            Flat(root,"Body",UiSkin.Hex("#21354b"));
            var button=root.GetComponent<Button>();var colors=button.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);colors.pressedColor=new Color(.7f,.8f,.9f);button.colors=colors;
        }
    }
}
