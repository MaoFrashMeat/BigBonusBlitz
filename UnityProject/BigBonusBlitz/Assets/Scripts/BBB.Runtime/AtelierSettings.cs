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
        public static GameObject Build(Transform stage,AudioManager audio,Action close,Action<RectTransform> gameControls=null)
        {
            var body=AtelierUi.Screen(stage,"Settings",AtelierUi.Night,close,out var overlay);
            AtelierUi.Header(body,"MAKE IT YOURS / PREFERENCES","あなたのための冒険",AtelierUi.Light);
            var preview=AtelierUi.Panel(body,"LivePreview",337,-18,236,352,UiSkin.Hex("#222940"));
            AtelierUi.Text(preview,"Label",0,145,200,22,"LIVE PREVIEW",10,UiSkin.Hex("#c3b4e9"),true);
            AtelierUi.Art(preview,"Scene",0,49,212,148,AtelierUi.Sprite("Art/UI/Title/sky_mountains_cloudsea"));
            var subtitleBg=AtelierUi.Panel(preview,"SubtitleBg",0,3,204,74,AtelierUi.Ink);
            var subtitle=AtelierUi.Text(subtitleBg,"Subtitle",0,0,188,68,"「さあ、霧の向こうへ。」",14,AtelierUi.Light,false,TextAnchor.MiddleCenter);
            AtelierUi.Text(preview,"Copy",0,-80,200,74,"読みやすさも、\n冒険の大切な装備。",19,AtelierUi.Light,true);
            var summary=AtelierUi.Text(preview,"Summary",0,-144,200,44,"",12,AtelierUi.Sub);
            void Preview(){subtitle.gameObject.SetActive(AtelierPreferences.Subtitles);subtitle.fontSize=Mathf.RoundToInt(14*AtelierPreferences.Scale/100f);subtitleBg.GetComponent<Image>().color=AtelierPreferences.Contrast?Color.black:AtelierUi.Ink;summary.text=$"字幕 {AtelierPreferences.Scale}%\nタイトル演出 {(AtelierPreferences.Motion?"控えめ":"標準")}";}
            var roots=new RectTransform[gameControls==null?3:4];var tabs=new Button[roots.Length];
            string[] names={"表示・演出","サウンド","操作ガイド","ゲーム設定"};
            for(int i=0;i<roots.Length;i++){roots[i]=UiSkin.Rect(body,"Page"+i,new Vector2(-36,-15),new Vector2(450,360));int ix=i;tabs[i]=AtelierUi.Button(body,"Tab"+i,-378,131-i*56,148,names[i],()=>{for(int j=0;j<roots.Length;j++){roots[j].gameObject.SetActive(j==ix);tabs[j].GetComponent<Image>().color=j==ix?UiSkin.Hex("#4c4265"):AtelierUi.Night;}Preview();},AtelierUi.Night);}
            void Row(Transform p,string id,float y,string label,string desc,Func<bool> get,Action<bool> set)
            {
                AtelierUi.Text(p,id+"Label",-52,y+12,312,24,label,17,AtelierUi.Light,true);
                AtelierUi.Text(p,id+"Desc",-52,y-14,312,25,desc,12,AtelierUi.Sub);
                Button b=null; b=AtelierUi.Button(p,id,175,y,76,get()?"ON":"OFF",()=>{set(!get());AtelierUi.SetText(b,get()?"ON":"OFF");b.GetComponent<Image>().color=get()?UiSkin.Hex("#bba8e6"):UiSkin.Hex("#586175");Preview();},get()?UiSkin.Hex("#bba8e6"):UiSkin.Hex("#586175"),AtelierUi.Ink);
                var switchImage=b.GetComponent<Image>();switchImage.sprite=UiSkin.Rounded(22);switchImage.type=Image.Type.Sliced;
                AtelierUi.Panel(p,id+"Line",0,y-37,426,1,UiSkin.Hex("#2d3548"));
            }
            void BuildDisplay()
            {
                AtelierUi.Clear(roots[0]);
                Row(roots[0],"Motion",126,"タイトル演出を控えめに","髪・背景・花びらの動きを止めます",()=>AtelierPreferences.Motion,v=>AtelierPreferences.Motion=v);
                Row(roots[0],"Contrast",42,"字幕を高コントラストに","会話の背景を黒、文字を白にします",()=>AtelierPreferences.Contrast,v=>AtelierPreferences.Contrast=v);
                Row(roots[0],"Subtitles",-42,"会話字幕を表示","会話内容を文字で表示します",()=>AtelierPreferences.Subtitles,v=>AtelierPreferences.Subtitles=v);
                AtelierUi.Text(roots[0],"ScaleLabel",-82,-105,260,26,"字幕サイズ",16,AtelierUi.Light,true);
                var value=AtelierUi.Text(roots[0],"ScaleValue",165,-105,96,26,AtelierPreferences.Scale+"%",18,AtelierUi.Light,true,TextAnchor.MiddleRight);
                var slider=UiFactory.Slider(roots[0],"SubtitleScale",new Vector2(0,-145),new Vector2(426,44),(AtelierPreferences.Scale-100)/50f,v=>{AtelierPreferences.Scale=100+Mathf.RoundToInt(v*5)*10;value.text=AtelierPreferences.Scale+"%";Preview();});StyleSlider(slider);
            }
            BuildDisplay();
            if(audio!=null)
            {
                void Volume(string id,string label,float y,Func<float> get,Action<float> set)
                {
                    AtelierUi.Text(roots[1],id+"Label",-100,y+38,224,26,label,18,AtelierUi.Light,true);
                    var val=AtelierUi.Text(roots[1],id+"Value",170,y+38,86,26,Mathf.RoundToInt(get()*100)+"%",17,AtelierUi.Light,true,TextAnchor.MiddleRight);
                    var slider=UiFactory.Slider(roots[1],id,new Vector2(0,y),new Vector2(426,44),get(),v=>{set(v);val.text=Mathf.RoundToInt(v*100)+"%";SaveData.SaveAudio(audio);});
                    StyleSlider(slider);slider.gameObject.AddComponent<SliderReleaseSound>().OnRelease=()=>{audio.UiPop();SaveData.SaveAudio(audio);PlayerPrefs.Save();};
                }
                Volume("BgmSlider","BGM音量",94,()=>audio.BgmVolume,v=>audio.BgmVolume=v);
                Volume("SeSlider","効果音の音量",-8,()=>audio.SeVolume,v=>audio.SeVolume=v);
                Row(roots[1],"BgmToggle",-100,"BGMを再生","音楽の再生を切り替えます",()=>audio.BgmEnabled,v=>{if(v!=audio.BgmEnabled)audio.ToggleBgm();SaveData.SaveAudio(audio);PlayerPrefs.Save();});
            }
            AtelierUi.Text(roots[2],"Keys",0,30,426,260,"方向キー：メニュー選択\nEnter：決定\nEsc：閉じる\n\n冒険：Ctrl / SpaceでBET\nZ・X・Cでリール停止\nA：AUTO   S：速度\nE：装備   M：マップ",18,AtelierUi.Light);
            if(gameControls!=null)gameControls(roots[3]);
            AtelierUi.Button(body,"ResetDisplay",-150,-237,220,"表示設定を初期値に戻す",()=>{AtelierPreferences.Defaults();BuildDisplay();Preview();},UiSkin.Hex("#30394f"));
            AtelierUi.Text(body,"Saved",236,-237,430,28,"変更は端末に保存されます",13,AtelierUi.Sub,false,TextAnchor.MiddleRight);
            for(int i=0;i<roots.Length;i++)roots[i].gameObject.SetActive(i==0);tabs[0].GetComponent<Image>().color=UiSkin.Hex("#4c4265");
            Preview();return overlay;
        }
        static void StyleSlider(Slider slider)
        {
            var handle=slider.handleRect;handle.anchorMin=handle.anchorMax=new Vector2(0,.5f);handle.sizeDelta=new Vector2(18,18);
            var im=handle.GetComponent<Image>();im.sprite=UiSkin.Circle(32);im.color=UiSkin.Hex("#c4b2eb");
            slider.fillRect.GetComponent<Image>().color=UiSkin.Hex("#b49ddf");
        }
    }
}
