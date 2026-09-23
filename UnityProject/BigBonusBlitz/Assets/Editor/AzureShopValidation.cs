using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class AzureShopValidation
{
    static Canvas canvas;static Camera camera;static RectTransform stage;static string output;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Button Button(string name)=>stage.GetComponentsInChildren<Button>().First(t=>t.name==name);
    static void Click(string name){var b=Button(name);Check(b.interactable,"Disabled button "+name);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA project required");
        output=Environment.GetEnvironmentVariable("AZURE_SHOP_OUTPUT");Check(!string.IsNullOrEmpty(output),"AZURE_SHOP_OUTPUT required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        canvas=UiFactory.CreateCanvas("AzureShopQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(AzureMapSkin.Width,AzureMapSkin.Height));
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Ink;canvas.worldCamera=camera;
        foreach(string asset in new[]{"terrace","card","panel-gold"})Check(AzureShopSkin.Sprite(asset)!=null,"Missing generated shop art "+asset);
        foreach(string icon in new[]{"eye","lantern","amulet","oil"})Check(AzureShopSkin.Icon(icon)!=null,"Missing generated icon "+icon);
        var m=GameDataLoader.CreateMachine(new SystemRandom());var audio=AudioManager.Create();
        m.Wallet.Souls=3837;m.Wallet.Embers=3108;m.Adv.torches=0;
        var first=m.Config.shop.items[0];
        foreach(var item in m.Config.shop.items.Take(4).Where(i=>i.icon!="eye"))m.Wallet.Owned[item.id]=item.maxLevel;
        int changed=0,closed=0;
        var root=ShopScreen.Build(stage,m,audio,()=>changed++,()=>closed++);
        Render("shop-reference-state");Check(!Button("Purchase").interactable,"Maxed item purchase enabled");
        var scroll=stage.GetComponentInChildren<ScrollRect>();
        Check(Mathf.Abs(scroll.content.anchoredPosition.y)<.1f,"Initial list did not start at the top");
        Check(scroll!=null&&scroll.vertical&&!scroll.horizontal&&scroll.viewport.GetComponent<RectMask2D>()!=null,"Vertical masked scroll missing");
        var products=stage.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Product_")).ToArray();
        Check(products.Length==9&&products.All(b=>Mathf.Abs(((RectTransform)b.transform).anchoredPosition.x)<.01f),"Products are not a single complete column");
        Check(!stage.GetComponentsInChildren<Button>().Any(b=>b.name=="Previous"||b.name=="Next"),"Paging controls remain");
        Check(VisibleRows(scroll)==9,"Current nine products are not all visible");
        float offset=0;
        Click("Category1");Check(Mathf.Abs(scroll.content.anchoredPosition.y)<.1f,"Category did not reset scroll to top");Render("equipment-category");Click("Category2");Check(stage.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Product_"))==4,"Category filter failed");
        m.Wallet.Owned[first.id]=0;Click("Category0");
        int souls=m.Wallet.Souls,cost=ShopDirector.NextCost(first,0);
        Click("Purchase");Click("Cancel");Check(m.Wallet.Souls==souls&&m.Wallet.LevelOf(first.id)==0,"Cancelled purchase changed inventory");
        scroll.verticalNormalizedPosition=.5f;offset=scroll.content.anchoredPosition.y;Click("Purchase");Click("Confirm");Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<.1f,"Purchase reset scroll position");Check(m.Wallet.Souls==souls-cost&&m.Wallet.LevelOf(first.id)==1&&changed==1,"Upgrade transaction failed");Render("upgrade-success");
        Click("Purchase");m.Wallet.Souls=0;Click("Confirm");Check(m.Wallet.LevelOf(first.id)==1&&changed==1,"Confirmation did not recheck affordability");Render("insufficient-souls");
        Click("Category3");int ember=m.Wallet.Embers,hp=m.Hp;Click("Purchase");Click("Confirm");Check(m.Wallet.Embers<ember&&m.Hp>hp&&changed==2,"Supply transaction failed");Render("supply-category");
        Click("Product_1");int credit=m.Credit;ember=m.Wallet.Embers;Click("Purchase");Click("Confirm");Check(m.Credit>credit&&m.Wallet.Embers<ember,"Adventure ember refill failed");
        m.Wallet.Embers=0;Click("Category3");Check(!Button("Purchase").interactable,"Unaffordable supply enabled");
        m.Wallet.Embers=999999;m.Adv.torches=m.Config.adventure.resource.maxTorches;Click("Category3");Check(!Button("Purchase").interactable,"Capped supply enabled");Render("supply-cap");
        m.Wallet.Souls=999999;Click("Category0");Render("six-digit-wallet");
        string name=first.name;first.name="暁の騎士の斬撃の心得";UnityEngine.Object.DestroyImmediate(root);root=ShopScreen.Build(stage,m,audio,()=>changed++,()=>closed++);Check(stage.GetComponentsInChildren<Text>().Count(t=>t.name=="Name"&&t.text==first.name)==2,"Long name fixture was not rendered");Render("long-product-name");first.name=name;
        Click("Stats");Check(stage.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("ステータス")),"Stats screen missing");
        Click("Close");Check(stage.GetComponentsInChildren<Button>().Any(t=>t.name=="Purchase"),"Stats did not return to shop");
        // Add catalog entries only to the isolated in-memory fixture, never the game configuration.
        var originalItems=m.Config.shop.items.ToArray();
        void Extra(int n)=>m.Config.shop.items.Add(new ShopItem{id="qa_dense_"+n,name="検証用の品 "+n,icon="book",kind="gear",desc="検証用",maxLevel=5,baseCost=100});
        Extra(1);UnityEngine.Object.DestroyImmediate(root);root=ShopScreen.Build(stage,m,audio,()=>changed++,()=>closed++);
        scroll=stage.GetComponentInChildren<ScrollRect>();Canvas.ForceUpdateCanvases();Check(VisibleRows(scroll)==10,"Ten rows do not fit");Render("ten-products");
        for(int i=2;i<=10;i++)Extra(i);
        UnityEngine.Object.DestroyImmediate(root);root=ShopScreen.Build(stage,m,audio,()=>changed++,()=>closed++);scroll=stage.GetComponentInChildren<ScrollRect>();
        Canvas.ForceUpdateCanvases();Check(VisibleRows(scroll)==10,"Large catalog does not show ten complete rows");Render("large-catalog-top");
        var pointer=new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-1)};
        scroll.OnScroll(pointer);Check(scroll.content.anchoredPosition.y>0,"Mouse wheel did not move products");offset=scroll.content.anchoredPosition.y;
        var point=RectTransformUtility.WorldToScreenPoint(camera,scroll.viewport.position);
        pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left,pointerPressRaycast=new RaycastResult{module=canvas.GetComponent<GraphicRaycaster>(),gameObject=scroll.viewport.gameObject}};
        scroll.OnInitializePotentialDrag(pointer);scroll.OnBeginDrag(pointer);pointer.position=point+new Vector2(0,80);scroll.OnDrag(pointer);scroll.OnEndDrag(pointer);
        Check(scroll.content.anchoredPosition.y>offset,"Upward drag did not reveal later products");
        scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();offset=scroll.content.anchoredPosition.y;
        Click("Product_18");Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<.1f,"Selection reset scroll position");Render("scroll-bottom");
        Check(stage.GetComponentsInChildren<Text>().Any(t=>t.name=="Name"&&t.transform.parent.name=="Details"&&t.text=="灯火を補充"),"Bottom product detail missing");
        scroll.verticalNormalizedPosition=1;Button("Product_18").GetComponent<AzureShopScrollItem>().OnSelect(new BaseEventData(EventSystem.current));
        Check(scroll.content.anchoredPosition.y>0,"Keyboard-selected row was not brought into view");
        Click("Product_0");scroll.verticalNormalizedPosition=.5f;offset=scroll.content.anchoredPosition.y;
        Click("Purchase");Click("Confirm");Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<.1f,"Purchase reset nonzero scroll position");
        Click("Category1");Check(Mathf.Abs(scroll.content.anchoredPosition.y)<.1f,"Category did not reset overflowing list");
        m.Config.shop.items.Clear();m.Config.shop.items.AddRange(originalItems);
        Click("BackAdventure");Click("CloseTop");Check(closed==2,"Close callbacks changed");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"purchaseAndCancel\":true,\"confirmationRecheck\":true,\"supplyAndCredit\":true,\"tenVisibleRows\":true,\"nineteenItemFixture\":true,\"verticalScrollAndCategories\":true,\"wheelAndDrag\":true,\"selectionAndPurchasePreserveScroll\":true,\"keyboardReveal\":true,\"maxLevelAndSupplyCap\":true,\"sixDigitBalances\":true,\"longProductName\":true,\"statsAndClose\":true,\"saveWritesDisabledInIsolatedCopy\":true}");
        Debug.Log("AZURE_SHOP_VALIDATION passed");
    }
    static int VisibleRows(ScrollRect scroll)
    {
        var view=Bounds(scroll.viewport);
        return scroll.content.GetComponentsInChildren<Button>().Count(b=>{var row=Bounds((RectTransform)b.transform);return row.yMin>=view.yMin-.1f&&row.yMax<=view.yMax+.1f;});
    }
    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var b in stage.GetComponentsInChildren<Button>())
        {
            var rt=(RectTransform)b.transform;Check(rt.rect.width>=44&&rt.rect.height>=(b.name.StartsWith("Product_")?32:44),"Small touch target "+b.name);
            if(b.name=="Previous"||b.name=="Next"||b.name=="Purchase")Check(rt.anchoredPosition.y-rt.rect.height*.5f>=-246.1f,"Footer padding "+b.name);
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
            foreach(var t in stage.GetComponentsInChildren<Text>())if(t.name=="Value"||t.name=="Name"||t.name=="Label"||t.name=="Description"||t.name=="Price")Check(t.preferredHeight<=t.rectTransform.rect.height+1,"Clipped label: "+t.name+" "+t.text);
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
    static Rect Bounds(RectTransform rt){var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
}
