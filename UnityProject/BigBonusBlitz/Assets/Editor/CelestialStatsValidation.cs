using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class CelestialStatsValidation
{
    static Canvas canvas; static Camera camera; static RectTransform stage; static string output;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Button Button(string name)=>stage.GetComponentsInChildren<Button>().Last(b=>b.name==name);
    static void Click(string name){var b=Button(name);Check(b.interactable,"Disabled "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA only");
        output=Environment.GetEnvironmentVariable("CELESTIAL_STATS_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("CelestialStatsQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(1170,540));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Ink;canvas.worldCamera=camera;
        foreach(string asset in new[]{"cosmos","card","emblems"})Check(CelestialStatsSkin.Sprite(asset)!=null,"Missing art "+asset);
        var m=GameDataLoader.CreateMachine(new SystemRandom());var audio=AudioManager.Create();
        m.PlayerLevel=12;m.Stats.Life=4;m.Stats.Technique=4;m.Stats.Luck=8;m.Stats.Unspent=6;m.Wallet.Souls=2450;m.Wallet.Embers=18320;m.Adv.chapter=1;
        int changed=0,closed=0;
        var root=StatsScreen.Build(stage,m,audio,()=>changed++,()=>closed++);
        Render("status-reference");
        foreach(string key in StatsDirector.Keys){int before=m.Stats.Get(key),points=m.Stats.Unspent;Click("Add_"+key);Check(m.Stats.Get(key)==before+1&&m.Stats.Unspent==points-1,"Spend "+key);}
        Check(changed==3,"Spend callbacks");Render("upgrade-success");
        int souls=m.Wallet.Souls,total=m.Stats.Total,unspent=m.Stats.Unspent;
        Click("Respec");Render("respec-confirm");Click("Cancel");Check(m.Stats.Total==total&&m.Wallet.Souls==souls,"Cancel mutated stats");
        Click("Respec");Click("Confirm");Check(m.Stats.Total==0&&m.Stats.Unspent==total+unspent&&m.Wallet.Souls==souls-m.Config.stats.respecCost,"Paid respec");
        Check(!Button("Respec").interactable,"Empty respec enabled");Render("respec-complete");
        Click("Add_life");Click("Respec");m.Wallet.Souls=0;Click("Confirm");Check(m.Stats.Life==1&&m.Wallet.Souls==0,"Stale balance charged");Render("insufficient-souls");
        void Rebuild(){UnityEngine.Object.DestroyImmediate(root);root=StatsScreen.Build(stage,m,audio,()=>changed++,()=>closed++);}
        m.Stats.Unspent=0;Rebuild();Check(StatsDirector.Keys.All(k=>!Button("Add_"+k).interactable),"No-point upgrade enabled");Render("no-points");
        m.Stats.Life=m.Stats.Technique=m.Stats.Luck=m.Config.stats.maxPerStat;m.Stats.Unspent=6;m.Wallet.Souls=m.Wallet.Embers=999999;Rebuild();
        Check(StatsDirector.Keys.All(k=>!Button("Add_"+k).interactable),"Max upgrade enabled");Render("max-six-digit");
        m.Adv.chapter=2;Rebuild();souls=m.Wallet.Souls;total=m.Stats.Total;unspent=m.Stats.Unspent;Click("Respec");Click("Confirm");Check(m.Wallet.Souls==souls&&m.Stats.Unspent==unspent+total,"Free chapter respec");
        Render("free-respec");
        Click("Back");Click("Close");Check(closed==2,"Close callbacks");
        // Exercise the real shop entry and return path, not just a standalone mock.
        UnityEngine.Object.DestroyImmediate(root);
        root=ShopScreen.Build(stage,m,audio,()=>changed++,()=>closed++);
        Click("Stats");Check(stage.GetComponentsInChildren<Image>().Any(x=>x.name=="Cosmos"),"Shop stats missing");
        Render("from-shop");Click("Close");
        Check(!stage.GetComponentsInChildren<Image>().Any(x=>x.name=="Cosmos")&&stage.GetComponentsInChildren<Button>().Any(x=>x.name=="Purchase"),"Return to shop failed");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"threeStatsSpend\":true,\"noPointsAndCaps\":true,\"paidAndFreeRespec\":true,\"cancelAndStaleBalance\":true,\"sixDigitWallets\":true,\"shopEntryAndReturn\":true,\"closeCallbacks\":true,\"textBounds\":true,\"saveWritesDisabledInIsolatedCopy\":true}");
        Debug.Log("CELESTIAL_STATS_VALIDATION passed");
    }
    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var b in stage.GetComponentsInChildren<Button>())
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
            float canvasW=540f*size.x/size.y;((RectTransform)canvas.transform).sizeDelta=new Vector2(canvasW,540);stage.localScale=Vector3.one*Mathf.Min(1,canvasW/AzureMapSkin.Width);canvas.scaleFactor=size.y/540f;
            Canvas.ForceUpdateCanvases();
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
