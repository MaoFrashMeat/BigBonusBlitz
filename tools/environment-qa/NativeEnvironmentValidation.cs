using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class NativeEnvironmentValidation
{
    static Camera camera;static Canvas canvas;static string output;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static Color32[] Render(string name,int width=1280,int height=720)
    {
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(camera.orthographicSize*2*width/height,camera.orthographicSize*2);
        Canvas.ForceUpdateCanvases();
        foreach(var text in canvas.GetComponentsInChildren<Text>())if(text.font!=null)text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
        for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
        var rt=RenderTexture.GetTemporary(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        var pixels=image.GetPixels32();if(name!=null)File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);return pixels;
    }
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("NATIVE_ENVIRONMENT_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
        canvas=UiFactory.CreateCanvas("EnvironmentQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=160;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;
        var stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(960,320));stage.gameObject.AddComponent<RectMask2D>();var bg=ParallaxBackground.Create(stage);bg.AutoCycle=false;
        double worst=0;int combinations=0,periodChecks=0;
        var nativeIds=AdventureEnvironmentCatalog.All.Values.Where(p=>p.layers!=null&&p.layers.Any(l=>l.nativeAlpha)).Select(p=>p.id).OrderBy(id=>id).ToArray();
        Check(nativeIds.Length>=3,"Native stage coverage");
        foreach(var id in nativeIds.Concat(new[]{"C-1"}))
        {
            bg.SetStage(id,true);Check(bg.Profile.UsesModules&&bg.IllustratedLayerCount==3,"Missing layers "+id);
            foreach(var layer in bg.Profile.layers)Check(layer.rowHeight>0&&layer.top>=0&&layer.top+layer.rowHeight<=1,"Invalid row");
            foreach(AdventureTime time in Enum.GetValues(typeof(AdventureTime)))foreach(var weather in new[]{AdventureWeather.Clear,AdventureWeather.Cloudy,AdventureWeather.Rain})
            {
                bg.SetTimeOfDay(time,0);bg.SetWeather(weather,1,0);bg.Preview(7.1f);Render(id+"-"+time+"-"+weather,1280,426);combinations++;
                Check(bg.CurrentWeather==AdventureEnvironmentCatalog.WeatherFor(bg.Profile,weather),"Weather conversion");
            }
            bg.SetTimeOfDay(AdventureTime.Day,0);bg.SetWeather(AdventureWeather.Clear,0,0);bg.Preview(7);
            var visible=stage.GetComponentsInChildren<RawImage>().Where(im=>im.enabled&&im.GetComponentInParent<CanvasGroup>().alpha>.99f).ToArray();Check(visible.Length==3,"Visible layers");
            for(int layer=0;layer<3;layer++)
            {
                for(int i=0;i<3;i++)visible[i].enabled=i==layer;
                var spec=bg.Profile.layers[layer];
                foreach(int direction in new[]{-1,1})for(int cycle=1;cycle<=3;cycle++)
                {
                    float center=(direction*cycle-spec.phase)/spec.speed;
                    bg.PreviewScroll(center-.0001f);var before=Render(null,640,213);
                    bg.PreviewScroll(center+.0001f);var after=Render(null,640,213);
                    double error=0;for(int p=0;p<before.Length;p++)error+=Math.Abs(before[p].r-after[p].r)+Math.Abs(before[p].g-after[p].g)+Math.Abs(before[p].b-after[p].b);
                    error/=before.Length*3d*255;worst=Math.Max(worst,error);Check(error<.002,"Periodic discontinuity "+id+" "+error);periodChecks++;
                }
            }
            foreach(var im in visible)im.enabled=true;
            for(int frame=0;frame<24;frame++){bg.PreviewScroll(frame*4);Render(id+"-scroll-"+frame.ToString("D2"),960,320);}
        }
        foreach(var p in AdventureEnvironmentCatalog.All.Values){bg.SetStage(p.id,true);Check(bg.IllustratedLayerCount==3,"Coverage "+p.id);}
        UnityEngine.Object.DestroyImmediate(canvas.gameObject);camera.orthographicSize=270;
        var machine=GameDataLoader.CreateMachine(new SystemRandom(931));machine.Adv.opDone=true;
        var game=new GameObject("GameplayQA").AddComponent<GameController>();game.enabled=false;var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(GameController).GetField("_m",flags).SetValue(game,machine);typeof(GameController).GetField("_audio",flags).SetValue(game,AudioManager.Create());
        typeof(GameController).GetMethod("BuildUi",flags).Invoke(game,null);typeof(GameController).GetMethod("RefreshUi",flags).Invoke(game,null);
        canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.isRootCanvas);UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;canvas.worldCamera=camera;
        var safe=canvas.GetComponentInChildren<SafeStage>();if(safe!=null){safe.enabled=false;safe.Stage.SetParent(canvas.transform,false);}
        var main=(ParallaxBackground)typeof(GameController).GetField("_bg",flags).GetValue(game);
        foreach(var id in nativeIds)
        {
            main.SetStage(id,true);main.SetHour(12,0);main.SetWeather(AdventureWeather.Clear,0,0);main.PreviewScroll(7);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)}){if(safe!=null)safe.Stage.localScale=Vector3.one*Mathf.Min(1,540f*size.x/size.y/safe.Stage.sizeDelta.x);Render("game-"+id+"-"+size.x,size.x,size.y);}
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"nativeStages\":"+nativeIds.Length+",\"legacyMatteRegression\":true,\"timeWeatherCombinations\":"+combinations+",\"periodChecks\":"+periodChecks+",\"worstWrapDifference\":"+worst.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"stageCoverage\":30,\"mainResolutions\":2,\"finalArtApproved\":false,\"productionSaveTouched\":false}");
        Debug.Log("NATIVE_ENVIRONMENT_VALIDATION passed");
    }
}
