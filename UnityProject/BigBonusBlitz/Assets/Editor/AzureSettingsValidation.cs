using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class AzureSettingsValidation
{
    static Canvas canvas;static Camera camera;static RectTransform stage;static string output;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>stage.GetComponentsInChildren<Button>().Last(b=>b.name==name);
    static void Click(string name){var b=Button(name);Check(b.interactable,"Disabled "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    static Slider Slider(string name)=>stage.GetComponentsInChildren<Slider>().Single(s=>s.name==name);
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/")&&Application.productName=="shop-qa","Isolated project and preferences only");
        output=Environment.GetEnvironmentVariable("AZURE_SETTINGS_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("AzureSettingsQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(1170,540));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Ink;canvas.worldCamera=camera;
        Check(AtelierUi.Sprite("Art/UI/AzureSettings/navigation")!=null,"Missing navigation art");
        AtelierPreferences.Defaults();var audio=AudioManager.Create();audio.BgmVolume=.6f;audio.SeVolume=.7f;
        int closed=0;var root=AtelierSettings.Build(stage,audio,()=>closed++);
        Render("display-default");
        Click("Motion");Click("Contrast");
        Check(AtelierPreferences.Motion&&AtelierPreferences.Contrast,"Switch values");
        Check(Button("Motion").transform.Find("Knob").GetComponent<RectTransform>().anchoredPosition.x>0,"ON knob placement");
        Check(stage.GetComponentsInChildren<Image>().Single(i=>i.name=="SubtitleBg").color==Color.black,"Contrast preview");
        Slider("SubtitleScale").value=.6f;Check(AtelierPreferences.Scale==130,"Scale 130");Render("display-130-contrast");
        Slider("SubtitleScale").value=1;Check(AtelierPreferences.Scale==150,"Scale max");Render("display-150");
        foreach(float v in new[]{0f,.4f,1f})
        {var s=Slider("SubtitleScale");s.value=v;Canvas.ForceUpdateCanvases();Check(Mathf.Abs(s.handleRect.rect.height-28)<.01f,"Slider handle stretched after value change");}
        Click("Subtitles");Check(!AtelierPreferences.Subtitles&&!stage.GetComponentsInChildren<Image>().Any(i=>i.name=="SubtitleBg"),"Hidden subtitles left a blank plate");Render("subtitles-hidden");
        UnityEngine.Object.DestroyImmediate(root);
        typeof(AtelierPreferences).GetMethod("ResetCache",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        root=AtelierSettings.Build(stage,audio,()=>closed++);Check(AtelierPreferences.Scale==150&&!AtelierPreferences.Subtitles&&AtelierPreferences.Contrast,"Preferences persistence");
        Click("ResetDisplay");Check(AtelierPreferences.Scale==100&&AtelierPreferences.Subtitles&&!AtelierPreferences.Motion&&!AtelierPreferences.Contrast,"Display reset");
        Check(Mathf.Abs(audio.BgmVolume-.6f)<.001f&&Mathf.Abs(audio.SeVolume-.7f)<.001f,"Display reset changed sound");
        Click("Tab1");Slider("BgmSlider").value=.3f;Slider("SeSlider").value=.8f;
        Check(Mathf.Abs(audio.BgmVolume-.3f)<.001f&&Mathf.Abs(audio.SeVolume-.8f)<.001f,"Audio connection");
        bool playing=audio.BgmEnabled;Click("BgmToggle");Check(audio.BgmEnabled!=playing,"BGM toggle");Render("sound");
        Check(!root.GetComponentsInChildren<Button>().Any(b=>b.name=="ResetDisplay"),"Display reset visible on sound");
        Click("Tab2");Render("controls-guide");
        Click("Back");Click("Close");Check(closed==2,"Close callbacks");UnityEngine.Object.DestroyImmediate(root);
        root=AtelierSettings.Build(stage,null,()=>closed++);Click("Tab1");Render("audio-unavailable");UnityEngine.Object.DestroyImmediate(root);
        // Exercise real caller-owned data/settings controls, including the first confirmation step only.
        stage.sizeDelta=new Vector2(960,540);
        var title=new GameObject("Title").AddComponent<TitleScreen>();typeof(TitleScreen).GetField("_audio",Private).SetValue(title,audio);
        typeof(TitleScreen).GetMethod("BuildSettings",Private).Invoke(title,new object[]{stage});
        root=(GameObject)typeof(TitleScreen).GetField("_settingsBox",Private).GetValue(title);root.SetActive(true);Click("Tab3");Render("title-data");
        typeof(TitleScreen).GetField("_confirmUntil",Private).SetValue(title,-100f);
        Click("ResetSave");Check(root.GetComponentsInChildren<Text>().Any(t=>t.name=="ResetState"&&t.text.Contains("もう一度")),"Data confirmation missing");Render("title-data-confirm");UnityEngine.Object.DestroyImmediate(root);
        stage.sizeDelta=new Vector2(1170,540);
        var controller=new GameObject("Controller").AddComponent<GameController>();var machine=GameDataLoader.CreateMachine(new SystemRandom());
        typeof(GameController).GetField("_m",Private).SetValue(controller,machine);typeof(GameController).GetField("_audio",Private).SetValue(controller,audio);
        typeof(GameController).GetMethod("BuildUi",Private).Invoke(controller,null);typeof(GameController).GetMethod("RefreshUi",Private).Invoke(controller,null);
        root=(GameObject)typeof(GameController).GetField("_settingsBox",Private).GetValue(controller);root.transform.SetParent(stage,false);root.SetActive(true);
        foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c!=canvas)c.gameObject.SetActive(false);
        Click("Tab3");Render("adventure-settings");Click("GraphAlways");Check(Button("GraphAlways").GetComponentInChildren<Text>().text.Contains("ON")||Button("GraphAlways").GetComponentInChildren<Text>().text.Contains("OFF"),"Graph label");
        int oldMask=SaveData.LoadAutoStop();Click("AutoStop0");Check(SaveData.LoadAutoStop()==(oldMask^SaveData.AutoStopAchievement),"AUTO mask persistence");Render("adventure-options-changed");
        typeof(GameController).GetField("_resetConfirmUntil",Private).SetValue(controller,0f);
        Click("ResetSave");Check(root.GetComponentsInChildren<Text>().Any(t=>t.name=="ResetConfirm"&&t.text.Contains("もう一度")),"Game confirmation missing");Render("adventure-delete-confirm");
        int credit=machine.Credit;typeof(GameController).GetMethod("OnBetClicked",Private).Invoke(controller,null);Check(machine.Credit==credit,"BET leaked through settings");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"displaySwitches\":true,\"scale100to150\":true,\"handleGeometry\":true,\"preferencesReload\":true,\"resetKeepsAudio\":true,\"audioControls\":true,\"guide\":true,\"realTitleAndAdventureCallers\":true,\"firstDeleteConfirmationOnly\":true,\"betBlocked\":true,\"isolatedPreferences\":true}");
        Debug.Log("AZURE_SETTINGS_VALIDATION passed");
    }
    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var b in stage.GetComponentsInChildren<Button>().Where(b=>b.enabled))
        {
            var rt=(RectTransform)b.transform;Check(rt.rect.width>=44&&rt.rect.height>=(b.name.StartsWith("Product_")?32:44),"Small touch target "+b.name);
            if(b.name=="Respec")Check(rt.anchoredPosition.y-rt.rect.height*.5f>=-246.1f,"Footer padding "+b.name);
        }
        foreach(var parent in stage.GetComponentsInChildren<RectTransform>())
        {
            if(parent.name=="MapContent")continue;
            var labels=parent.Cast<Transform>().Select(t=>t.GetComponent<Text>()).Where(t=>t!=null&&t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)).ToArray();
            for(int i=0;i<labels.Length;i++)for(int j=i+1;j<labels.Length;j++)Check(!Bounds(labels[i].rectTransform).Overlaps(Bounds(labels[j].rectTransform)),"Text overlap: "+parent.name+"/"+labels[i].name+"/"+labels[j].name);
        }
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
        {
            float canvasW=540f*size.x/size.y;((RectTransform)canvas.transform).sizeDelta=new Vector2(canvasW,540);stage.localScale=Vector3.one*Mathf.Min(1,canvasW/stage.sizeDelta.x);canvas.scaleFactor=size.y/540f;
            Canvas.ForceUpdateCanvases();
            foreach(var fit in canvas.GetComponentsInChildren<AzureScreenFit>())fit.Apply();
            foreach(var bleed in canvas.GetComponentsInChildren<AzureMapBleed>())bleed.Apply();
            foreach(var text in canvas.GetComponentsInChildren<Text>())text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
            foreach(var t in stage.GetComponentsInChildren<Text>())if(t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)&&t.GetComponentInParent<ScrollRect>()==null&&t.name!="Message")Check(t.cachedTextGenerator.vertexCount>0,"Text produced no visible glyphs: "+t.name+" "+t.text);
            foreach(var t in stage.GetComponentsInChildren<Text>())if(t.name!="Message")Check(t.preferredHeight<=t.rectTransform.rect.height+1,"Clipped label: "+t.name+" "+t.text);
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
    static Rect Bounds(RectTransform rt){var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
}
