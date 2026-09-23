using System;
using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    public static class AtelierPreferences
    {
        static bool loaded,motion,contrast,subtitles;static int scale;
        static void Load(){if(loaded)return;loaded=true;motion=PlayerPrefs.GetInt("bbb_ui_title_motion",0)==1;contrast=PlayerPrefs.GetInt("bbb_ui_subtitle_contrast",0)==1;subtitles=PlayerPrefs.GetInt("bbb_ui_subtitles",1)==1;scale=Mathf.Clamp(PlayerPrefs.GetInt("bbb_ui_subtitle_scale",100),100,150);}
        public static bool Motion{get{Load();return motion;}set{Load();motion=value;Save();}}
        public static bool Contrast{get{Load();return contrast;}set{Load();contrast=value;Save();}}
        public static bool Subtitles{get{Load();return subtitles;}set{Load();subtitles=value;Save();}}
        public static int Scale{get{Load();return scale;}set{Load();scale=Mathf.Clamp(value,100,150);Save();}}
        static void Save(){PlayerPrefs.SetInt("bbb_ui_title_motion",motion?1:0);PlayerPrefs.SetInt("bbb_ui_subtitle_contrast",contrast?1:0);PlayerPrefs.SetInt("bbb_ui_subtitles",subtitles?1:0);PlayerPrefs.SetInt("bbb_ui_subtitle_scale",scale);PlayerPrefs.Save();}
        public static void Defaults(){loaded=true;motion=false;contrast=false;subtitles=true;scale=100;Save();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetCache()=>loaded=false;
    }
    public static class AtelierSettings
    {
        public static GameObject Build(Transform stage,AudioManager audio,Action close,Action<RectTransform> gameControls=null,string page4Name="ゲーム設定")
        {
            var body=AtelierUi.Screen(stage,"Settings",Color.clear,null,out var overlay);body.sizeDelta=new Vector2(1170,540);body.gameObject.AddComponent<AzureScreenFit>().Apply();
            var bg=UiSkin.Img(body,"Terrace",Vector2.zero,body.sizeDelta,AzureShopSkin.Sprite("terrace"),Color.white);bg.raycastTarget=false;bg.gameObject.AddComponent<AzureMapBleed>().Apply();
            AzureShopSkin.Surface(body,"SettingsPaper",98,-57,908,410,true);
            AzureShopSkin.Surface(body,"HeadingPaper",-34,209,600,118,false);
            AzureMapSkin.Button(body,"Back",-442,233,180,"←  戻る",close,false,48,fontSize:20);
            AzureUiControls.SquareButton(body,"Close",540,234,"×",close);
            AzureUiControls.Label(body,"Eyebrow",-19,247,526,19,"SETTINGS",11,true);
            AzureUiControls.Label(body,"Heading",-19,212,526,50,"あなたのための冒険",30,true);
            AzureUiControls.Label(body,"Subheading",-19,173,526,26,"より快適に、あなただけの冒険を。",15,true);
            var compass=AtelierUi.Art(body,"Compass",-309,224,44,44,AtelierUi.Sprite("Art/UI/Icons/compass"));compass.color=AzureUiControls.RuleGold;
            var nav=UiSkin.Img(body,"Navigation",new Vector2(-442,-28),new Vector2(216,448),AtelierUi.Sprite("Art/UI/AzureSettings/navigation"),Color.white);nav.raycastTarget=false;
            AzureShopSkin.Surface(body,"PreviewPaper",388,-33,306,374,true);
            var preview=UiSkin.Rect(body,"LivePreview",new Vector2(388,-33),new Vector2(306,374));
            var previewBar=UiSkin.Img(preview,"Header",new Vector2(0,149),new Vector2(288,44),AzureMapSkin.Sprite("button-navy"),Color.white);previewBar.type=Image.Type.Sliced;previewBar.pixelsPerUnitMultiplier=8;previewBar.raycastTarget=false;
            var previewTitle=AtelierUi.Text(preview,"Label",0,149,244,26,"LIVE PREVIEW",13,AzureMapSkin.Gold,false,TextAnchor.MiddleCenter);AzureMapSkin.Typography(previewTitle);
            var scene=UiSkin.Img(preview,"Scene",new Vector2(0,52),new Vector2(268,142),AzureShopSkin.Sprite("terrace"),Color.white);scene.raycastTarget=false;
            var subtitleBg=AtelierUi.Panel(preview,"SubtitleBg",0,3,254,74,AzureMapSkin.Navy);AtelierUi.Edge(subtitleBg,AzureMapSkin.Gold);
            var subtitle=AtelierUi.Text(subtitleBg,"Subtitle",0,0,236,68,"「さあ、霧の向こうへ。」",14,Color.white,false,TextAnchor.MiddleCenter);
            AzureMapSkin.Typography(subtitle,true);
            AzureUiControls.Label(preview,"Copy",0,-85,268,70,"読みやすさも、\n冒険の大切な装備。",21,true,TextAnchor.MiddleCenter);
            AzureUiControls.Rule(preview,"SummaryRule",0,-125,244);
            var summary=AzureUiControls.Label(preview,"Summary",0,-152,244,44,"",13,true);
            void Preview()
            {
                subtitleBg.gameObject.SetActive(AtelierPreferences.Subtitles);
                subtitle.fontSize=Mathf.RoundToInt(14*AtelierPreferences.Scale/100f);
                subtitle.text=AtelierPreferences.Scale>=130?"「さあ、\n霧の向こうへ。」":"「さあ、霧の向こうへ。」";
                subtitleBg.GetComponent<Image>().color=AtelierPreferences.Contrast?Color.black:AzureMapSkin.Navy;
                summary.text=$"字幕 {(AtelierPreferences.Subtitles?AtelierPreferences.Scale+"%":"非表示")}\nタイトル演出 {(AtelierPreferences.Motion?"控えめ":"標準")}";
            }
            var roots=new RectTransform[gameControls==null?3:4];var tabs=new Button[roots.Length];
            string[] names={"表示・演出","サウンド","操作ガイド",page4Name};
            string[] icons={"compass","music","book","gear"};
            var reset=AzureMapSkin.Button(body,"ResetDisplay",-50,-232,304,"表示設定を初期値に戻す",()=>{AtelierPreferences.Defaults();BuildDisplay();Preview();},false,46,fontSize:16);
            AzureUiControls.Label(body,"Saved",350,-246,360,26,"変更は端末に保存されます",13,true,TextAnchor.MiddleCenter);
            void Tab(int index)
            {
                for(int j=0;j<roots.Length;j++)
                {
                    roots[j].gameObject.SetActive(j==index);
                    var im=tabs[j].GetComponent<Image>();im.sprite=j==index?AzureMapSkin.Sprite("button-gold"):null;im.type=j==index?Image.Type.Sliced:Image.Type.Simple;im.color=j==index?Color.white:Color.clear;
                    tabs[j].GetComponentInChildren<Text>().color=j==index?AzureMapSkin.Ink:AzureMapSkin.Paper;
                }
                reset.gameObject.SetActive(index==0);Preview();
            }
            for(int i=0;i<roots.Length;i++)
            {
                roots[i]=UiSkin.Rect(body,"Page"+i,new Vector2(-50,-25),new Vector2(480,360));int ix=i;
                tabs[i]=AzureMapSkin.Button(body,"Tab"+i,-442,99-i*62,206,names[i],()=>Tab(ix),false,48,fontSize:17);
                var label=tabs[i].GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(16,0);label.rectTransform.sizeDelta=new Vector2(148,40);
                var icon=AtelierUi.Sprite("Art/UI/Icons/"+icons[i])??AtelierUi.Icon(icons[i]);var image=AtelierUi.Art(tabs[i].transform,"Icon",-76,0,24,24,icon);image.color=AzureMapSkin.Gold;
                if(i<roots.Length-1)AzureUiControls.Rule(body,"TabRule"+i,-442,69-i*62,164);
            }
            void Row(Transform p,string id,float y,string label,string desc,Func<bool> get,Action<bool> set)
            {
                var accent=AtelierUi.Art(p,id+"Accent",-234,y+12,20,24,AtelierUi.Sprite("Art/UI/Icons/diamond_star"));accent.color=AzureUiControls.RuleGold;
                AzureUiControls.Label(p,id+"Label",-56,y+12,318,30,label,18,true);
                var description=AzureUiControls.Label(p,id+"Desc",-56,y-16,318,24,desc,13);description.fontStyle=FontStyle.Normal;description.color=AzureUiControls.Muted;
                AzureUiControls.Switch(p,id,178,y,get,set,Preview);
                AzureUiControls.Rule(p,id+"Line",0,y-40,438);
            }
            void BuildDisplay()
            {
                AtelierUi.Clear(roots[0]);
                Row(roots[0],"Motion",126,"タイトル演出を控えめに","髪・背景・花びらの動きを止めます",()=>AtelierPreferences.Motion,v=>AtelierPreferences.Motion=v);
                Row(roots[0],"Contrast",42,"字幕を高コントラストに","会話の背景を黒、文字を白にします",()=>AtelierPreferences.Contrast,v=>AtelierPreferences.Contrast=v);
                Row(roots[0],"Subtitles",-42,"会話字幕を表示","会話内容を文字で表示します",()=>AtelierPreferences.Subtitles,v=>AtelierPreferences.Subtitles=v);
                AzureUiControls.Label(roots[0],"ScaleLabel",-84,-109,260,30,"字幕サイズ",18,true);
                var value=AzureUiControls.Label(roots[0],"ScaleValue",166,-109,94,30,AtelierPreferences.Scale+"%",18,true,TextAnchor.MiddleRight);
                Slider slider=null;
                slider=AzureUiControls.Slider(roots[0],"SubtitleScale",0,-148,366,(AtelierPreferences.Scale-100)/50f,v=>{
                    AtelierPreferences.Scale=100+Mathf.RoundToInt(v*5)*10;slider.SetValueWithoutNotify((AtelierPreferences.Scale-100)/50f);value.text=AtelierPreferences.Scale+"%";Preview();
                });
                AzureUiControls.Label(roots[0],"SmallA",-207,-148,26,30,"A",14,true,TextAnchor.MiddleCenter);
                AzureUiControls.Label(roots[0],"LargeA",207,-148,28,40,"A",25,true,TextAnchor.MiddleCenter);
            }
            BuildDisplay();
            if(audio!=null)
            {
                void Volume(string id,string label,float y,Func<float> get,Action<float> set)
                {
                    AzureUiControls.Label(roots[1],id+"Label",-100,y+38,224,30,label,20,true);
                    var val=AzureUiControls.Label(roots[1],id+"Value",170,y+38,86,30,Mathf.RoundToInt(get()*100)+"%",18,true,TextAnchor.MiddleRight);
                    var slider=AzureUiControls.Slider(roots[1],id,0,y,426,get(),v=>{set(v);val.text=Mathf.RoundToInt(v*100)+"%";SaveData.SaveAudio(audio);PlayerPrefs.Save();});
                    slider.gameObject.AddComponent<SliderReleaseSound>().OnRelease=()=>{audio.UiPop();SaveData.SaveAudio(audio);PlayerPrefs.Save();};
                    AzureUiControls.Rule(roots[1],id+"Line",0,y-34,438);
                }
                Volume("BgmSlider","BGM音量",94,()=>audio.BgmVolume,v=>audio.BgmVolume=v);
                Volume("SeSlider","効果音の音量",-8,()=>audio.SeVolume,v=>audio.SeVolume=v);
                Row(roots[1],"BgmToggle",-112,"BGMを再生","音楽の再生を切り替えます",()=>audio.BgmEnabled,v=>{if(v!=audio.BgmEnabled)audio.ToggleBgm();SaveData.SaveAudio(audio);PlayerPrefs.Save();});
            }
            else AzureUiControls.Label(roots[1],"AudioUnavailable",0,30,420,90,"サウンドを利用できません。",20,true,TextAnchor.MiddleCenter);
            AzureUiControls.Label(roots[2],"GuideTitle",0,144,426,34,"冒険の操作ガイド",22,true);
            string[] keys={"方向キー / Enter","Esc","Ctrl / Space","Z ・ X ・ C","A / S","E / M"};
            string[] actions={"選択 / 決定","閉じる","BET","リール停止","AUTO / 速度","装備 / マップ"};
            for(int i=0;i<keys.Length;i++)
            {
                float y=95-i*45;
                var key=AzureMapSkin.Button(roots[2],"Key"+i,-106,y,196,keys[i],null,false,36,fontSize:13);key.enabled=false;key.GetComponent<Image>().raycastTarget=false;
                AzureUiControls.Label(roots[2],"Action"+i,121,y,182,32,actions[i],16,true);
                AzureUiControls.Rule(roots[2],"GuideRule"+i,0,y-22,426);
            }
            if(gameControls!=null)
            {
                gameControls(roots[3]);
                // Callback-owned actions keep their listeners, state colours and confirmation semantics.
                foreach(var t in roots[3].GetComponentsInChildren<Text>(true))
                    if(t.GetComponentInParent<Button>()==null)
                    {
                        if(t.color==AtelierUi.Light)t.color=AzureMapSkin.Ink;
                        else if(t.color==AtelierUi.Sub)t.color=AzureUiControls.Muted;
                        else if(t.name=="ResetState"||t.name=="ResetConfirm")t.color=UiSkin.Hex("#9b3b43");
                    }
                foreach(var b in roots[3].GetComponentsInChildren<Button>(true))
                {
                    if(b.name.StartsWith("AutoStop"))continue;
                    var im=b.GetComponent<Image>();im.sprite=AzureMapSkin.Sprite("button-navy");im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=8;im.color=b.name=="ResetSave"?UiSkin.Hex("#d5a7ad"):Color.white;
                    var label=b.GetComponentInChildren<Text>();label.color=AzureMapSkin.Paper;label.rectTransform.sizeDelta=new Vector2(((RectTransform)b.transform).rect.width-34,((RectTransform)b.transform).rect.height-6);
                }
            }
            Tab(0);return overlay;
        }
    }
}
