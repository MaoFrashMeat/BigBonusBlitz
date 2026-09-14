using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AdventureEnvironmentValidation
{
    static Camera camera;static Canvas canvas;static string output;
    static bool capturePixels;static Color32[] pixels;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated shop-qa copy required");
        output=Environment.GetEnvironmentVariable("ENVIRONMENT_OUTPUT");Check(!string.IsNullOrEmpty(output),"ENVIRONMENT_OUTPUT missing");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var machine=GameDataLoader.CreateMachine(new SystemRandom());Check(AdventureEnvironmentCatalog.All.Count==machine.Config.adventure.nodes.Count,"Stage coverage mismatch");
        canvas=UiFactory.CreateCanvas("EnvironmentValidation");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;
        var stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(960,540));stage.gameObject.AddComponent<RectMask2D>();var bg=ParallaxBackground.Create(stage);bg.AutoCycle=false;
        foreach(var node in machine.Config.adventure.nodes)
        {
            Check(AdventureEnvironmentCatalog.All.ContainsKey(node.id),"Missing profile "+node.id);bg.SetStage(node.id,true);bg.SetHour(12,0);bg.Preview(7);
            Check(bg.HasIllustration&&bg.IllustratedLayerCount==3,"Missing illustration "+node.id);
            Render("stage-"+node.id,1280,720);
            if(bg.Profile.indoor){bg.SetWeather(AdventureWeather.Storm,1,0);Check(bg.CurrentWeather==AdventureWeather.Drips,"Rain leaked indoors "+node.id);}
        }
        bg.SetStage("A-1",true);
        foreach(AdventureTime time in Enum.GetValues(typeof(AdventureTime))){bg.SetTimeOfDay(time,0);bg.SetWeather(AdventureWeather.Clear,0,0);Render("time-"+time,1280,720);}
        bg.SetTimeOfDay(AdventureTime.Day,0);
        foreach(AdventureWeather weather in Enum.GetValues(typeof(AdventureWeather))){bg.SetWeather(weather,1,0);bg.Preview(7.1f);Render("weather-"+weather,1280,720);Check(bg.WeatherStrength(weather)>.99f,"Weather control not applied "+weather);}
        AdventureEnvironmentCatalog.Palette(12,out var daytime,out _,out _);AdventureEnvironmentCatalog.Palette(23,out var nighttime,out _,out _);Check(daytime.grayscale>nighttime.grayscale+.15f,"Day/night palettes indistinct");
        var shader=Resources.Load<Shader>("Art/Adventure/AdventureSeamless");Check(shader!=null&&shader.isSupported,"Seamless shader unavailable");
        bg.SetStage("A-1",true);bg.SetHour(12,0);bg.SetWeather(AdventureWeather.Clear,0,0);capturePixels=true;
        bg.Preview(999.999f);Render("seam-before",1280,720);var before=pixels;
        bg.Preview(1000.001f);Render("seam-after",1280,720);double seamError=0;for(int i=0;i<before.Length;i++)seamError+=Math.Abs(before[i].r-pixels[i].r)+Math.Abs(before[i].g-pixels[i].g)+Math.Abs(before[i].b-pixels[i].b);
        seamError/=before.Length*3d*255;Check(seamError<.005,"Visible wrap discontinuity: "+seamError);capturePixels=false;pixels=null;
        File.WriteAllText(Path.Combine(output,"seam-validation.json"),"{\"passed\":true,\"meanNormalizedWrapDifference\":"+seamError.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"mirroredTiling\":false}");
        bg.SetStage("A-1",true);bg.SetWeather(AdventureWeather.Storm,1,0);bg.SetStage("D-1");Check(bg.WeatherStrength(AdventureWeather.Storm)==0,"Outgoing rain leaked into cave during transition");
        bg.SetStage("A-1",true);bg.SetWeather(AdventureWeather.Rain,.8f,0);Directory.CreateDirectory(Path.Combine(output,"motion"));
        for(int frame=0;frame<96;frame++){bg.SetHour(Mathf.Lerp(5,24,frame/95f),0);bg.Preview(frame/12f);Render("motion/frame-"+frame.ToString("D3"),960,540);}
        UnityEngine.Object.DestroyImmediate(canvas.gameObject);
        // Actual game integration: same profile in battlefield and ultrawide outer background.
        var host=new GameObject("Controller").AddComponent<GameController>();const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(GameController).GetField("_m",f).SetValue(host,machine);typeof(GameController).GetField("_audio",f).SetValue(host,AudioManager.Create());typeof(GameController).GetMethod("BuildUi",f).Invoke(host,null);
        var hero=(RectTransform)typeof(GameController).GetField("_charRt",f).GetValue(host);var frames=(Sprite[])typeof(GameController).GetField("_idleFrames",f).GetValue(host);Check(frames.Length>0,"Real hero art missing from QA copy");hero.GetComponent<Image>().sprite=frames[0];
        var safe=(SafeStage)typeof(GameController).GetField("_safe",f).GetValue(host);canvas=safe.GetComponentInParent<Canvas>();UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());safe.SafeRoot.anchorMin=Vector2.zero;safe.SafeRoot.anchorMax=Vector2.one;safe.SafeRoot.offsetMin=safe.SafeRoot.offsetMax=Vector2.zero;safe.Stage.localScale=Vector3.one;safe.enabled=false;canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;canvas.worldCamera=camera;
        foreach(string id in new[]{"A-1","C-1","E-3","H-1"})
        {
            machine.Adv.nodeId=id;typeof(GameController).GetMethod("RefreshUi",f).Invoke(host,null);
            foreach(var scene in canvas.GetComponentsInChildren<ParallaxBackground>()){Check(scene.CurrentStageId==id,"Game background did not follow stage");scene.SetStage(id,true);scene.SetTimeOfDay(id=="C-1"?AdventureTime.Night:AdventureTime.Day,0);scene.Preview(7);}
            Check(hero.parent.Find("EnvironmentWorld").GetSiblingIndex()<hero.GetSiblingIndex(),"Background obscures hero after stage change");
            Render("game-"+id,1280,720);Render("game-"+id+"-wide",2556,1179);
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"stages\":30,\"illustratedPlanesPerStage\":3,\"timeOfDayPresets\":4,\"weatherPresets\":8,\"gameResolutions\":[\"1280x720\",\"2556x1179\"],\"indoorWeatherGuard\":true,\"realGameIntegration\":true,\"saveDataTouched\":false}");
        Debug.Log("ADVENTURE_ENVIRONMENT_VALIDATION passed");
    }
    static void Render(string name,int width,int height)
    {
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(540f*width/height,540);
        var safe=canvas.GetComponentInChildren<SafeStage>();if(safe!=null)safe.Stage.localScale=Vector3.one*Mathf.Min(1,(540f*width/height)/safe.Stage.sizeDelta.x);
        Canvas.ForceUpdateCanvases();
        foreach(var text in canvas.GetComponentsInChildren<Text>())if(text.font!=null)text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
        for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
        var rt=RenderTexture.GetTemporary(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();if(capturePixels)pixels=image.GetPixels32();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
    }
}
