using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Shared OP/adventure dialogue view. Call Tick from its owner so modals can pause it.</summary>
    public sealed class DialoguePresenter : MonoBehaviour
    {
        public RectTransform Root { get; private set; }
        public Text Body { get; private set; }
        public Text Name { get; private set; }
        public Image NamePlate { get; private set; }
        public Image Portrait { get; private set; }
        public bool Revealed => visible >= ends.Length;
        public bool LastPage => page >= pages.Count - 1;
        public int PageCount => pages.Count;
        public int VisibleElements => visible;
        public string CurrentPage => pages.Count == 0 ? "" : pages[page];
        public float ReadSeconds => Mathf.Max(AdventurePresentation.Config.minimumReadSeconds,
            ends.Length * AdventurePresentation.Config.readSecondsPerCharacter);
        public event Action Tapped;
        readonly List<string> pages = new List<string>();
        int page, visible;
        int[] ends = Array.Empty<int>();
        float clock;
        float width, bottom;
        bool large, narration;
        string speaker;
        RectTransform portraitFrame;
        Image background;
        Text hint;

        public static DialoguePresenter Create(Transform parent, string name, float width, float bottom, bool large = false)
        {
            var rt = UiSkin.Rect(parent,name,Vector2.zero,new Vector2(width,110));
            var view = rt.gameObject.AddComponent<DialoguePresenter>();
            view.Root=rt; view.width=width;view.bottom=bottom;view.large=large;
            view.Build();return view;
        }
        void Build()
        {
            background = UiSkin.Img(Root,"Background",Vector2.zero,Root.sizeDelta,AzureMapSkin.Sprite("button-navy"),Color.white,true);
            background.type=Image.Type.Sliced;background.pixelsPerUnitMultiplier=8;
            UiSkin.Stretch(background.rectTransform);
            var tap = background.gameObject.AddComponent<Button>();tap.transition=Selectable.Transition.None;tap.onClick.AddListener(()=>Tapped?.Invoke());
            portraitFrame=UiSkin.Rect(Root,"PortraitFrame",Vector2.zero,new Vector2(80,80));
            var rim=UiSkin.Img(portraitFrame,"GoldRim",Vector2.zero,new Vector2(80,80),UiSkin.Rounded(40),AzureMapSkin.Gold);
            var mask=UiSkin.Img(rim.transform,"PortraitMask",Vector2.zero,new Vector2(74,74),UiSkin.Rounded(37),AzureMapSkin.Navy);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            Portrait=UiSkin.Img(mask.transform,"Portrait",Vector2.zero,new Vector2(74,74),null,Color.white);
            Portrait.preserveAspect=true;
            NamePlate=UiSkin.Img(Root,"NamePlate",Vector2.zero,new Vector2(180,25),null,new Color(0,0,0,0));
            Name=UiFactory.Label(NamePlate.transform,"Name",Vector2.zero,new Vector2(180,25),"",16,TextAnchor.MiddleLeft,AzureMapSkin.Gold);
            AzureMapSkin.Typography(Name);
            Body=UiFactory.Label(Root,"Text",Vector2.zero,new Vector2(width-150,60),"",17,TextAnchor.UpperLeft,AzureMapSkin.Paper);
            Body.font=AzureMapSkin.Serif;
            Body.horizontalOverflow=HorizontalWrapMode.Wrap;Body.verticalOverflow=VerticalWrapMode.Truncate;Body.supportRichText=true;
            hint=UiFactory.Label(Root,"Hint",Vector2.zero,new Vector2(250,18),"",10,TextAnchor.MiddleRight,UiSkin.TextSub);
            hint.font=AzureMapSkin.Serif;
        }
        public void Begin(string who, string text, string heroName="サリア", string expression=null)
        {
            gameObject.SetActive(true);speaker=who ?? "";narration=speaker=="*";
            Name.text=narration?"":string.IsNullOrEmpty(speaker)||speaker=="主人公"?heroName:speaker;
            portraitFrame.gameObject.SetActive(!narration);
            Portrait.sprite=AdventurePresentation.Portrait(speaker,heroName,expression);
            Portrait.color=Color.white;
            Configure();
            pages.Clear();page=0;
            string plain=Plain(text);
            // Pagination is measured with the same font, scale, width and line spacing as the live body.
            int start=0;
            int[] indices=StringInfo.ParseCombiningCharacters(plain);
            for(int i=0;i<indices.Length;i++)
            {
                int end=i+1<indices.Length?indices[i+1]:plain.Length;
                Body.text=plain.Substring(start,end-start);
                if(Body.preferredHeight>Body.rectTransform.rect.height+.1f && indices[i]>start)
                {pages.Add(plain.Substring(start,indices[i]-start));start=indices[i];}
            }
            pages.Add(plain.Substring(start));LoadPage();
        }
        void Configure()
        {
            int font=Mathf.RoundToInt((large?19:16)*AtelierPreferences.Scale/100f);
            float h=large?156:112;
            if(AtelierPreferences.Scale>100)h+=large?38:28;
            float sc=Mathf.Clamp(AdventurePresentation.Config?.dialogueScale ?? 1f,.3f,1.5f);
            Root.sizeDelta=new Vector2(width,h);Root.localScale=new Vector3(sc,sc,1f);Root.anchoredPosition=new Vector2(0,bottom+h*sc/2);   // 下端を揃えたまま縮める
            Body.fontSize=font;
            float left=narration?30:116;
            Body.rectTransform.sizeDelta=new Vector2(width-left-30,h-60);
            Body.rectTransform.anchoredPosition=new Vector2((left-30)/2,-5);
            NamePlate.rectTransform.anchoredPosition=new Vector2(-width/2+left+90,h/2-24);
            portraitFrame.anchoredPosition=new Vector2(-width/2+62,0);
            hint.rectTransform.anchoredPosition=new Vector2(width/2-155,-h/2+16);
            background.sprite=AtelierPreferences.Contrast?UiSkin.Rounded(8):AzureMapSkin.Sprite("button-navy");
            background.color=AtelierPreferences.Contrast?Color.black:Color.white;
            Body.color=Color.white;
        }
        public static string Plain(string text)
        {
            text=Regex.Replace(text??"",@"</?(?:b|i|color|size|material)(?:=[^>]*)?>","",RegexOptions.IgnoreCase);
            return text.Replace("{soul}","ソウル ").Replace("{ember}","エンバー ").Replace("{book}","EXP ")
                .Replace("{games}","G ").Replace("<","〈").Replace(">","〉");
        }
        void LoadPage()
        {
            int[] starts=StringInfo.ParseCombiningCharacters(CurrentPage);
            ends=new int[starts.Length];for(int i=0;i<starts.Length;i++)ends[i]=i+1<starts.Length?starts[i+1]:CurrentPage.Length;
            visible=0;clock=0;Paint();
        }
        void Paint()
        {
            int cut=visible==0?0:ends[Mathf.Min(visible,ends.Length)-1];
            // Transparent suffix retains the complete line layout, including spaces and Japanese punctuation.
            Body.text=Revealed?CurrentPage:CurrentPage.Substring(0,cut)+"<color=#FFFFFF00>"+CurrentPage.Substring(cut)+"</color>";
            hint.text=(pages.Count>1?$"{page+1} / {pages.Count}    ":"")+(Revealed?"タップで続ける  ▽":"タップで全文表示");
        }
        public void Reveal() {visible=ends.Length;Paint();}
        public bool Advance()
        {
            if(!Revealed){Reveal();return false;}
            if(!LastPage){page++;LoadPage();return false;}
            return true;
        }
        public void Tick(float delta, bool sound=true)
        {
            if(Revealed||!gameObject.activeInHierarchy)return;
            var cfg=AdventurePresentation.Config;clock+=Mathf.Max(0,delta);
            bool speak=false;
            while(!Revealed && clock>=1f/Mathf.Max(1,cfg.charactersPerSecond))
            {
                clock-=1f/Mathf.Max(1,cfg.charactersPerSecond);
                int from=visible==0?0:ends[visible-1];string elem=CurrentPage.Substring(from,ends[visible]-from);visible++;
                bool punctuation="。、！？!?…\n\r".Contains(elem);
                if(punctuation)clock-=Mathf.Max(0,cfg.punctuationPause);
                else if(!string.IsNullOrWhiteSpace(elem))speak=true;
            }
            Paint();
            // At most one decorative sound per update; frame catch-up never creates a burst of audio.
            if(sound&&speak&&!narration)AudioManager.Create().DialogueTick(speaker);
        }
    }
}
