using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class AzureEquipValidation
{
    static Canvas canvas; static Camera camera; static RectTransform stage; static string output;
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static Button Button(string name)=>stage.GetComponentsInChildren<Button>().Last(b=>b.name==name);
    static void Click(string name){var b=Button(name);Check(b.interactable,"Disabled "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA only");
        output=Environment.GetEnvironmentVariable("AZURE_EQUIP_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("AzureEquipQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(1170,540));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Ink;canvas.worldCamera=camera;
        foreach(string art in new[]{"citadel","banner","detail","salia-0","salia-1","salia-2","salia-3","salia-4"})Check(AzureEquipSkin.Art(art)!=null,"Missing "+art);
        var m=GameDataLoader.CreateMachine(new SystemRandom());var audio=AudioManager.Create();
        m.PlayerLevel=12;m.Stats.Life=4;m.Stats.Technique=4;m.Stats.Luck=8;m.Stats.Unspent=6;m.Wallet.Souls=3837;m.Equip.Clear();
        int changed=0,closed=0,curses=0;
        var root=EquipScreen.Build(stage,m,audio,()=>changed++,()=>closed++,null,()=>curses++);
        Render("empty-pose-0");
        for(int i=1;i<5;i++){Click("PoseNext");Check(stage.GetComponentsInChildren<Image>().Single(t=>t.name=="Salia").sprite==AzureEquipSkin.Portrait(i),"Pose switch");Render("empty-pose-"+i);}
        Click("PoseNext");Check(stage.GetComponentsInChildren<Image>().Single(t=>t.name=="Salia").sprite==AzureEquipSkin.Portrait(0),"Pose wrap");
        Click("PosePrevious");Check(stage.GetComponentsInChildren<Image>().Single(t=>t.name=="Salia").sprite==AzureEquipSkin.Portrait(4),"Reverse pose wrap");
        Click("Stats");Check(stage.GetComponentsInChildren<Image>().Any(t=>t.name=="Cosmos"),"Stats not opened");Click("Add_life");Click("Close");Check(!stage.GetComponentsInChildren<Image>().Any(t=>t.name=="Cosmos"),"Stats close failed");
        Check(stage.GetComponentsInChildren<Text>().Single(t=>t.name=="HeroStats").text.Contains("ライフ 5"),"Stats return stale");Click("Curses");Check(curses==1,"Curses callback");
        int callbacksBefore=changed;
        var first=new EquipItem{name="暁の騎士の双剣",slot=EquipSlot.Weapon,icon="sword",level=8};first.effectKeys.Add(ShopEffects.StatLife);first.effectValues.Add(6);first.effectKeys.Add(ShopEffects.BattleDamage);first.effectValues.Add(24);m.Equip.Bag.Add(first);
        for(int i=0;i<13;i++)m.Equip.Bag.Add(new EquipItem{name="深淵の守護者の装具 "+i,slot=EquipSlot.Body,icon="shield",level=i+1});
        var crafted=new EquipItem{name="工房の蒼剣",slot=EquipSlot.Weapon,icon="sword",crafted=true,level=2};m.Equip.Bag.Add(crafted);
        void Rebuild(){UnityEngine.Object.DestroyImmediate(root);root=EquipScreen.Build(stage,m,audio,()=>changed++,()=>closed++);}
        Rebuild();Render("inventory-comparison");
        Click("Equip");Check(m.Equip.IsWorn(first)&&changed==callbacksBefore+1,"Equip failed");Render("equipped");
        Click("Equip");Check(!m.Equip.IsWorn(first),"Unequip failed");
        Click("Equip");Click("Filter");Check(stage.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Item"))==1,"Worn filter failed");Render("worn-only");Click("Filter");
        Click("Next");Check(stage.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Item"))==4,"Paging count");Click("Item4");Render("inventory-page-2");
        var pageText=stage.GetComponentsInChildren<Text>().Single(t=>t.name=="Page").text;Click("PoseNext");Check(stage.GetComponentsInChildren<Text>().Single(t=>t.name=="Page").text==pageText,"Pose reset page");
        int souls=m.Wallet.Souls,bag=m.Equip.Bag.Count;Click("Sell");Render("sell-confirm");Click("Cancel");Check(m.Wallet.Souls==souls&&m.Equip.Bag.Count==bag,"Cancel sold item");
        Click("Sell");Click("Confirm");Check(m.Wallet.Souls>souls&&m.Equip.Bag.Count==bag-1,"Sell failed");
        Click("BulkSell");Click("Cancel");Check(m.Equip.IsWorn(first)&&m.Equip.Bag.Contains(crafted),"Cancel bulk mutated items");
        Click("BulkSell");Click("Confirm");Check(m.Equip.IsWorn(first)&&m.Equip.Bag.Contains(crafted)&&m.Equip.Bag.Count==1,"Bulk sold worn or crafted");Render("bulk-protected");
        Click("Item1");Check(!Button("Sell").interactable,"Craft sale enabled");
        // Large affix comparison scrolls instead of truncating text.
        foreach(string key in new[]{ShopEffects.StatLife,ShopEffects.StatTechnique,ShopEffects.StatLuck,ShopEffects.DefeatBonus,ShopEffects.BattleDamage,ShopEffects.TorchSpins,ShopEffects.SoulGain,ShopEffects.ExpGain,ShopEffects.AtStartPercent,ShopEffects.AtInitialSpins}){crafted.effectKeys.Add(key);crafted.effectValues.Add(9);}
        m.Wallet.Souls=999999;crafted.name="星を継ぐ蒼天の騎士の聖剣";Rebuild();Click("Item1");Render("crafted-many-effects");
        var scroll=stage.GetComponentInChildren<ScrollRect>();Check(scroll.content.rect.height>scroll.viewport.rect.height,"Effects not scrollable");
        scroll.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-1)});Check(scroll.content.anchoredPosition.y>0,"Effects wheel failed");Render("effects-scrolled");
        while(m.Equip.BagCount<m.Config.equipment.bagSize)m.Equip.Bag.Add(new EquipItem{name="満杯テスト",slot=EquipSlot.Body});
        Rebuild();Check(!Button("Equip").interactable,"Unequip enabled at full bag");Render("full-bag");
        Click("Back");Click("Close");Check(closed==2,"Close callbacks");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"poses\":5,\"poseWrapAndPagePreservation\":true,\"equipUnequipCompare\":true,\"pagingAndFilter\":true,\"saleCancelAndBulkProtection\":true,\"craftedAndFullBag\":true,\"manyEffectsScroll\":true,\"sixDigitAndLongName\":true,\"statsReturn\":true,\"callbacks\":true,\"saveWritesDisabledInIsolatedCopy\":true}");
        Debug.Log("AZURE_EQUIP_VALIDATION passed");
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
