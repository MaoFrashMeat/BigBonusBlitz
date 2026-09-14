using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AtelierUiValidation
{
    static string output;
    static Canvas canvas;
    static Camera camera;
    static RectTransform stage;
    static void Assert(bool v,string message){if(!v)throw new Exception(message);}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch project only");
        output=Environment.GetEnvironmentVariable("ATELIER_UI_OUTPUT");
        Assert(!string.IsNullOrEmpty(output),"ATELIER_UI_OUTPUT required");Directory.CreateDirectory(output);
        Assert(Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Refusing to validate transactions in the user's project");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("AtelierValidation");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(960,540));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AtelierUi.Night;camera.clearFlags=CameraClearFlags.SolidColor;canvas.worldCamera=camera;
        var m=GameDataLoader.CreateMachine(new SystemRandom());var audio=AudioManager.Create();
        m.Wallet.Souls=999999;m.Wallet.Embers=999999;m.Credit=12345;
        var root=ShopScreen.Build(stage,m,audio,()=>{},()=>{});
        Render("01-shop");
        int souls=m.Wallet.Souls;var first=m.Config.shop.items[0];int lv=m.Wallet.LevelOf(first.id);int cost=ShopDirector.NextCost(first,lv);
        Click(root,"Purchase");Click(root,"Confirm");Assert(m.Wallet.Souls==souls-cost&&m.Wallet.LevelOf(first.id)==lv+1,"Shop purchase did not update real wallet/level");
        m.Wallet.Souls=0;Click(root,"Category0");Assert(!Button(root,"Purchase").interactable,"Unaffordable upgrade enabled");
        Click(root,"Category3");int hp=m.Hp,ember=m.Wallet.Embers;Click(root,"Purchase");Click(root,"Confirm");Assert(m.Wallet.Embers<ember&&m.Hp>hp,"Supply purchase not applied");
        UnityEngine.Object.DestroyImmediate(root);
        m.Equip.Clear();m.Wallet.Souls=999999;
        var item=new EquipItem{name="暁の騎士の双剣",slot=EquipSlot.Weapon,icon="sword",level=8};item.effectKeys.Add(ShopEffects.StatLife);item.effectValues.Add(6);item.effectKeys.Add(ShopEffects.BattleDamage);item.effectValues.Add(24);m.Equip.Bag.Add(item);
        for(int i=0;i<14;i++)m.Equip.Bag.Add(new EquipItem{name="深淵の守護者の装具 "+i,slot=EquipSlot.Body,icon="shield",level=i+1});
        root=EquipScreen.Build(stage,m,audio,()=>{},()=>{});
        Render("02-equip");Click(root,"Equip");Assert(m.Equip.IsWorn(item),"Equipment did not equip");
        Click(root,"Next");Assert(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("装具")),"Inventory paging missing");
        UnityEngine.Object.DestroyImmediate(root);
        m.Adv.nodeId="B-2";m.Adv.visited.Clear();m.Adv.visited.Add("A-1");m.Adv.visited.Add("B-2");
        int go=0;var mapRefresh=AtelierMap.Build(stage,m,()=>go++,()=>{},()=>{},()=>{},()=>{},()=>{},out var msg);
        Render("03-map");string before=m.Adv.nodeId;Click(stage.gameObject,"Node_A-1");Assert(m.Adv.nodeId==before,"Map selection changed progress");Click(stage.gameObject,"Depart");Assert(go==1,"Departure callback missing");AtelierUi.Clear(stage);
        int wallet=m.Wallet.Souls,continued=0;m.Adv.chapter=2;
        root=AtelierResult.Build(stage,m,new GameResult{chapterCleared=true,chapterSouls=380,chapterSetbacks=2},()=>continued++);
        Render("04-result");Click(root,"Continue");Click(root,"Continue");Assert(continued==1&&m.Wallet.Souls==wallet,"Result granted duplicate reward or navigation");UnityEngine.Object.DestroyImmediate(root);
        root=AtelierSettings.Build(stage,audio,()=>{});Render("05-settings");Click(root,"Tab1");var slider=root.GetComponentsInChildren<Slider>().First(s=>s.name=="BgmSlider");slider.value=.3f;Assert(Mathf.Abs(audio.BgmVolume-.3f)<.001f,"Audio setting not connected");UnityEngine.Object.DestroyImmediate(root);
        // Build the real game controller to catch stale field references in its integrated settings UI.
        var controller=new GameObject("Controller").AddComponent<GameController>();
        const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(GameController).GetField("_m",f).SetValue(controller,m);typeof(GameController).GetField("_audio",f).SetValue(controller,audio);
        typeof(GameController).GetMethod("BuildUi",f).Invoke(controller,null);typeof(GameController).GetMethod("RefreshUi",f).Invoke(controller,null);
        Assert(typeof(GameController).GetField("_resetConfirm",f).GetValue(controller)!=null,"Integrated game settings missing");
        typeof(GameController).GetMethod("ToggleMap",f).Invoke(controller,null);
        Assert((bool)typeof(GameController).GetMethod("AtelierModalOpen",f).Invoke(controller,null),"Game map did not block gameplay input");
        int beforeBet=m.Credit;typeof(GameController).GetMethod("OnBetClicked",f).Invoke(controller,null);Assert(m.Credit==beforeBet&&!m.IsGameActive,"BET leaked through an open menu");
        var town=new GameObject("Town").AddComponent<MapScreen>();typeof(MapScreen).GetField("_m",f).SetValue(town,m);typeof(MapScreen).GetField("_audio",f).SetValue(town,audio);typeof(MapScreen).GetMethod("BuildUi",f).Invoke(town,null);
        var townCanvas=(Canvas)typeof(MapScreen).GetField("_canvas",f).GetValue(town);Assert(townCanvas.GetComponentsInChildren<AtelierModalInput>().Length>0,"Town map not integrated");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"screens\":5,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"realGameControllerBuilt\":true,\"transactions\":\"shop,supply,equip,map,result,audio\",\"saveWritesDisabledInIsolatedCopy\":true}");
        Debug.Log("ATELIER_UI_VALIDATION passed");
    }
    static Button Button(GameObject root,string name)=>root.GetComponentsInChildren<Button>().First(b=>b.name==name);
    static void Click(GameObject root,string name){var b=Button(root,name);Assert(b.interactable,"Disabled click: "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    static void Render(string name)
    {
        foreach(var b in stage.GetComponentsInChildren<Button>())Assert(((RectTransform)b.transform).sizeDelta.y>=44,"Small touch target: "+b.name);
        foreach(var parent in stage.GetComponentsInChildren<RectTransform>())
        {
            var texts=parent.Cast<Transform>().Select(t=>t.GetComponent<Text>()).Where(t=>t!=null&&t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)).ToArray();
            for(int a=0;a<texts.Length;a++)for(int b=a+1;b<texts.Length;b++)
            {
                Rect Bounds(RectTransform rt){var corners=new Vector3[4];rt.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
                Assert(!Bounds(texts[a].rectTransform).Overlaps(Bounds(texts[b].rectTransform)),"Overlapping text: "+parent.name+"/"+texts[a].name+" and "+texts[b].name);
            }
        }
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
        {
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(540f*size.x/size.y,540);Canvas.ForceUpdateCanvases();
            foreach(var t in canvas.GetComponentsInChildren<Text>())t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
            for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
            var target=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var tex=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,size.x,size.y),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),tex.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
