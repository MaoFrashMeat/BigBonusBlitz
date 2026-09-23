using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AzureMapValidation
{
    static Canvas canvas;static Camera camera;static RectTransform stage;static string output;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA project required");
        output=Environment.GetEnvironmentVariable("AZURE_MAP_OUTPUT");Check(!string.IsNullOrEmpty(output),"AZURE_MAP_OUTPUT required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("AzureMapQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        AzureMapSkin.Backdrop(canvas.transform);
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(AzureMapSkin.Width,AzureMapSkin.Height));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Ink;canvas.worldCamera=camera;
        foreach(string asset in new[]{"world","foreground","parchment","button-navy","button-gold"})Check(AzureMapSkin.Sprite(asset)!=null,"Missing generated art "+asset);
        Check(Resources.Load<Font>("Fonts/NotoSerifJP-SemiBold")!=null,"Bundled Japanese heading font missing");
        var m=GameDataLoader.CreateMachine(new SystemRandom());m.Wallet.Souls=3837;m.Credit=1079;
        m.Adv.nodeId="E-2";m.Adv.spinsLeft=11;m.Adv.torches=9;
        m.Adv.visited.Clear();foreach(var n in m.Config.adventure.nodes)if(n.depth<=5&&n.id!="B-1"&&n.id!="C-3"&&n.id!="C-4")m.Adv.visited.Add(n.id);
        int go=0,town=0,equip=0,trophy=0,settings=0,title=0;
        var refresh=AtelierMap.Build(stage,m,()=>go++,()=>town++,()=>equip++,()=>trophy++,()=>settings++,()=>title++,out var message);
        Render("stage-selection");
        var zoom=stage.GetComponentInChildren<AtelierMapZoom>();var scroll=stage.GetComponentInChildren<ScrollRect>();
        Check(zoom!=null&&scroll.horizontal&&scroll.vertical&&scroll.scrollSensitivity==0,"Map drag/wheel configuration changed");
        Click("ZoomIn");Check(Mathf.Abs(zoom.Zoom-1.25f)<.001f,"Zoom in failed");
        Click("ZoomOut");Check(Mathf.Abs(zoom.Zoom-1)<.001f,"Zoom out failed");
        zoom.Step(100);Check(zoom.Zoom==2,"Zoom max failed");zoom.Step(.001f);Check(zoom.Zoom==.5f,"Zoom min failed");Click("Home");Check(zoom.Zoom==1,"Current location reset failed");
        var oldPosition=scroll.content.anchoredPosition;Click("Node_D-1");
        Check(stage.GetComponentsInChildren<Text>().Any(t=>t.name=="Name"&&t.text=="廃坑"),"Stage details do not follow selection");
        Check(m.Adv.nodeId=="E-2","Selection changed adventure progress");Check(Vector2.Distance(oldPosition,scroll.content.anchoredPosition)<.1f,"Selection reset map pan");
        Click("Node_E-3");Render("long-stage-name");Click("Node_E-2");
        Click("Conditions");Check(stage.GetComponentsInChildren<Text>().Any(t=>t.text=="ROUTE CONDITIONS"),"Conditions dialog did not open");
        var dialog=stage.GetComponentsInChildren<RectTransform>().First(t=>t.name=="RouteConditionsOverlay");UnityEngine.Object.DestroyImmediate(dialog.gameObject);
        Click("Town");Click("Equipment");Click("Trophies");Click("Settings");Click("Title");Click("Depart");Click("BackAdventure");
        Check(town==1&&equip==1&&trophy==1&&settings==1&&title==1&&go==2,"Navigation callbacks changed");
        m.Wallet.Souls=999999;m.Credit=999999;refresh();Render("six-digit-resources");
        m.Wallet.Souls=3837;m.Credit=1079;refresh();message.text="新たな道が開かれました。現在地を確認して冒険へ進みましょう。";stage.GetComponentInChildren<AtelierMessageBand>().Refresh();Render("arrival-message");
        AtelierUi.Clear(stage);AtelierMap.Build(stage,m,()=>{},()=>{},()=>{},()=>{},()=>{},()=>{},out _,true);Render("in-adventure-map");
        // Build the real entry screen without calling Start (which reads the user's save).
        UnityEngine.Object.DestroyImmediate(canvas.gameObject);
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
        var host=new GameObject("MapScreenIntegration").AddComponent<MapScreen>();
        typeof(MapScreen).GetField("_m",fields).SetValue(host,m);
        typeof(MapScreen).GetMethod("BuildUi",fields).Invoke(host,null);
        canvas=(Canvas)typeof(MapScreen).GetField("_canvas",fields).GetValue(host);
        ((CanvasGroup)typeof(MapScreen).GetField("_fade",fields).GetValue(host)).alpha=0;
        UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        var safe=canvas.GetComponentInChildren<SafeStage>();safe.enabled=false;
        safe.SafeRoot.anchorMin=Vector2.zero;safe.SafeRoot.anchorMax=Vector2.one;safe.SafeRoot.offsetMin=safe.SafeRoot.offsetMax=Vector2.zero;
        stage=safe.Stage;Check(stage.sizeDelta.x==AzureMapSkin.Width,"MapScreen safe area was not widened");
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;canvas.worldCamera=camera;
        Render("map-screen-integration");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"stageSelection\":true,\"zoomAndHome\":true,\"navigationCallbacks\":true,\"sixDigitBalances\":true,\"saveDataTouched\":false}");
        Debug.Log("AZURE_MAP_VALIDATION passed");
    }
    static void Click(string name){var b=stage.GetComponentsInChildren<Button>().First(t=>t.name==name);Check(b.interactable,"Disabled button "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var b in stage.GetComponentsInChildren<Button>())
        {
            var rt=(RectTransform)b.transform;Check(rt.rect.width>=44&&rt.rect.height>=44,"Small touch target "+b.name);
            if(b.transform.parent.name=="AtelierMap")Check(rt.anchoredPosition.y-rt.rect.height*.5f>=-246.1f,"Footer padding "+b.name);
        }
        foreach(var parent in stage.GetComponentsInChildren<RectTransform>())
        {
            if(parent.name=="MapContent")continue;
            var labels=parent.Cast<Transform>().Select(t=>t.GetComponent<Text>()).Where(t=>t!=null&&t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)).ToArray();
            for(int i=0;i<labels.Length;i++)for(int j=i+1;j<labels.Length;j++)Check(!Bounds(labels[i].rectTransform).Overlaps(Bounds(labels[j].rectTransform)),"Text overlap: "+parent.name+"/"+labels[i].name+"/"+labels[j].name);
        }
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
        {
            float canvasW=540f*size.x/size.y;((RectTransform)canvas.transform).sizeDelta=new Vector2(canvasW,540);stage.localScale=Vector3.one*Mathf.Min(1,canvasW/AzureMapSkin.Width);canvas.scaleFactor=size.y/540f;
            Canvas.ForceUpdateCanvases();
            foreach(var bleed in canvas.GetComponentsInChildren<AzureMapBleed>())bleed.Apply();
            foreach(var text in canvas.GetComponentsInChildren<Text>())text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
            foreach(var t in stage.GetComponentsInChildren<Text>())if(t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)&&t.GetComponentInParent<ScrollRect>()==null&&t.name!="Message")Check(t.cachedTextGenerator.vertexCount>0,"Text produced no visible glyphs: "+t.name+" "+t.text);
            foreach(var t in stage.GetComponentsInChildren<Text>())if(t.name=="Value"||t.name=="Name"||t.name=="Label")Check(t.preferredHeight<=t.rectTransform.rect.height+1,"Clipped label: "+t.name+" "+t.text);
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
    static Rect Bounds(RectTransform rt){var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
}
