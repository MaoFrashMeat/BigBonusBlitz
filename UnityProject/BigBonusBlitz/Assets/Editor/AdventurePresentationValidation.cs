using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AdventurePresentationValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static GameController game;
    static SlotMachine machine;
    static RectTransform stage;
    static Camera camera;
    static string output;
    static int captures;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
    static object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA project required");
        output=Environment.GetEnvironmentVariable("ADVENTURE_PRESENTATION_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();AtelierPreferences.Defaults();
        foreach(var atlas in new[]{"town","salia","curses"})for(int i=0;i<4;i++)Check(AdventurePresentation.Cell(atlas,i)!=null,"Missing atlas "+atlas+" cell="+i+" resource="+Resources.Load<Texture2D>("Art/UI/AdventureDialogue/"+atlas)+" asset="+UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/UI/AdventureDialogue/"+atlas+".png"));
        camera=new GameObject("QACamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=AzureMapSkin.Navy;
        for(int i=0;i<6;i++)Check(AdventurePresentation.Cell("travelers",i)!=null,"Missing traveler portrait");
        var audio=AudioManager.Create();audio.SeVolume=0;
        machine=GameDataLoader.CreateMachine(new SystemRandom(9123));
        game=new GameObject("Controller").AddComponent<GameController>();typeof(GameController).GetField("_m",Private).SetValue(game,machine);typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");stage=Get<RectTransform>("_stage");
        // EditMode does not invoke a MonoBehaviour's Awake; use the runtime animator after initializing it.
        var hero=Get<SpriteAnimator>("_charAnim");typeof(SpriteAnimator).GetMethod("Awake",Private).Invoke(hero,null);hero.Play(Get<Sprite[]>("_idleFrames"),1,true);
        var dialogue=Get<DialoguePresenter>("_dialogue");var hud=Get<CurseHudPresenter>("_curseHud");
        hud.Refresh(false);Check(hud.GroupCount==0&&!hud.gameObject.activeSelf,"Empty curse HUD");
        dialogue.Begin("サリア","さあ、霧の向こうへ。みんなの笑顔を守るために！");dialogue.Tick(.25f,false);Check(!dialogue.Revealed&&dialogue.VisibleElements>0,"Typing did not progress");Capture("dialogue-typing");
        string layout=dialogue.CurrentPage;float height=dialogue.Body.preferredHeight;dialogue.Reveal();Check(dialogue.CurrentPage==layout&&Mathf.Abs(dialogue.Body.preferredHeight-height)<1,"Typewriter layout changed");Capture("dialogue-salia");
        foreach(string speaker in new[]{"宿の主人","灯守","店主","門番","行商人","巡礼者","狩人","占い師","王の使者","声","？？","未登録の旅人"})
        {dialogue.Begin(speaker,"灯を分けよう。次の冒険が、きみを待っている。");dialogue.Reveal();Check(dialogue.Portrait.sprite!=null,"No portrait fallback "+speaker);}
        dialogue.Begin("行商人","次の冒険に、確かな備えを。");dialogue.Reveal();Capture("dialogue-traveler");
        dialogue.Begin("灯守","灯を分けよう。尽きる前に戻れ。");dialogue.Reveal();Capture("dialogue-keeper");
        dialogue.Begin("*","遠くで、小さな灯が揺れる。");dialogue.Reveal();Check(!dialogue.Portrait.gameObject.activeInHierarchy,"Narration portrait");Capture("dialogue-narration");
        // A grapheme cannot be split between visible text and the transparent suffix.
        dialogue.Begin("サリア","か\u3099🙂灯");dialogue.Tick(.035f,false);Check(dialogue.Body.text.StartsWith("か\u3099<color"),"Combining character split");
        AtelierPreferences.Scale=150;AtelierPreferences.Contrast=true;
        string longLine=string.Concat(Enumerable.Repeat("灯を分けながら、霧の向こうへ進もう。",12));
        dialogue.Begin("サリア",longLine);Check(dialogue.PageCount>1,"Long text not paginated");
        int pages=0;while(true){dialogue.Reveal();Check(dialogue.Body.preferredHeight<=dialogue.Body.rectTransform.rect.height+1,"Page text clipped");if(pages==0)Capture("dialogue-150-long");pages++;if(dialogue.Advance())break;Check(pages<40,"Page loop");}
        Check(pages==dialogue.PageCount,"Page progression");AtelierPreferences.Contrast=false;AtelierPreferences.Scale=100;
        Call("PutExplainLine","技術介入 成功！ {soul}+120  {book}+30",1f);Capture("explanation");Check(!dialogue.Body.text.Contains("{soul}"),"Unconverted reward token");
        var fx=machine.Config.reelFx.emberGain;fx.countUp=false;
        var gains=Get<RectTransform>("_gainLayer");var area=Get<RectTransform>("_area");
        Check(gains.parent==dialogue.Root.parent&&gains.GetSiblingIndex()>dialogue.Root.GetSiblingIndex(),"Reward layer not after dialogue");
        Check(gains.GetComponentInParent<Mask>()==null,"Reward clipped by character mask");
        for(int n=1;n<=4;n+=3)
        {
            for(int i=gains.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(gains.GetChild(i).gameObject);
            for(int i=0;i<n;i++)
            {
                var routine=(IEnumerator)Call("GainSlide",i==1?"soul":i==2?"book":"ember",120+i*35,null,0f,0f,n==1?1f:fx.stackScale,float.NaN,i,n);
                Check(routine.MoveNext(),"Gain creation");
                var band=(RectTransform)gains.GetChild(gains.childCount-1);
                Call("PlaceGain",band,Vector2.zero,fx.y,n==1?1f:fx.stackScale,float.NaN,i,n,Vector2.one,fx);
                band.GetComponent<CanvasGroup>().alpha=1;
                foreach(Transform digit in band.Find("Row"))if(digit.name.StartsWith("Num"))digit.localScale=Vector3.one;
            }
            Capture("dialogue-get-"+n);
            var db=Bounds(dialogue.Root);
            foreach(RectTransform band in gains)Check(!Bounds(band).Overlaps(db),"GET overlaps dialogue "+n);
            var bands=gains.Cast<RectTransform>().ToArray();for(int i=0;i<bands.Length;i++)for(int j=i+1;j<bands.Length;j++)Check(!Bounds(bands[i]).Overlaps(Bounds(bands[j])),"GET rows overlap");
        }
        // Dialogue opens/resizes while reward bands are already alive.
        AtelierPreferences.Scale=150;dialogue.Begin("サリア","立ち止まって、灯を確かめよう。");dialogue.Reveal();
        int slot=0;foreach(RectTransform band in gains){Call("PlaceGain",band,Vector2.zero,fx.y,fx.stackScale,float.NaN,slot++,4,Vector2.one,fx);Check(!Bounds(band).Overlaps(Bounds(dialogue.Root)),"Late large dialogue overlap");}Capture("get-with-late-large-dialogue");
        machine.Curse.Taken.Add(new CurseInstance{curseId="c_drain",curseName="燃え急ぎ",curseEffect=CurseEffects.TorchDrain,curseValue=12,blessId="b_soul",blessName="魂寄せ",blessEffect="soulGain",blessValue=8,depth=2});
        hud.Refresh(false);Check(hud.GroupCount==1,"One curse");Capture("curse-one");
        foreach(var id in new[]{"c_hard","c_cut","c_toll"}){var def=machine.Config.curse.curses.Find(c=>c.id==id);machine.Curse.Taken.Add(new CurseInstance{curseId=id,curseName=def.name,curseEffect=def.effect,curseValue=3,blessName="導き",blessEffect="atStartPercent",blessValue=5,depth=3});}
        hud.Refresh(false);Check(hud.GroupCount==4,"Four curses");Capture("curse-four");hud.ShowTooltip("c_toll",true);Check(hud.Description("c_toll").Contains("エンバー"),"Wrong toll unit");Capture("curse-toll-tooltip");
        machine.Curse.Taken[1]=new CurseInstance{curseId="c_drain",curseName="燃え急ぎ",curseEffect=CurseEffects.TorchDrain,curseValue=18,blessName="長い灯",blessEffect="torchSpins",blessValue=5,depth=3};hud.Refresh(false);
        Check(hud.GroupCount==3&&hud.Description("c_drain").Contains("30%")&&hud.Description("c_drain").Contains("魂寄せ")&&hud.Description("c_drain").Contains("長い灯"),"Lost duplicate curse pair");hud.ShowTooltip("c_drain",true);Capture("curse-stacked-tooltip");
        hud.Refresh(true);Check(!hud.TooltipVisible&&!hud.gameObject.activeSelf,"Curse tooltip crossed modal");hud.Refresh(false);
        var settings=Get<GameObject>("_settingsBox");settings.SetActive(true);Call("Update");Check(Get<CanvasGroup>("_gainVisibility").alpha==0,"Rewards cover modal");settings.SetActive(false);
        machine.Curse.Clear();hud.Refresh(false);Check(hud.GroupCount==0&&!hud.TooltipVisible,"Curses persisted after return");
        for(int i=gains.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(gains.GetChild(i).gameObject);
        dialogue.gameObject.SetActive(false);AtelierPreferences.Subtitles=false;
        var ambient=(IEnumerator)Call("ShowLine","サリア","非表示の字幕",Color.white,Color.white,2f,false);Check(!ambient.MoveNext(),"Subtitles OFF ignored");
        var opening=OpeningPlayer.Create(null,"サリア");
        opening.Show(new StoryLine{speaker="",text="灯が……もう、指先ほどしかない"});
        var op=opening.GetComponentInChildren<DialoguePresenter>();Check(op.gameObject.activeSelf,"Mandatory OP hidden by subtitle setting");Check(!op.Advance()&&op.Revealed,"First tap did not reveal");Check(op.Advance(),"Second tap did not advance");Capture("opening-150");
        int count=0;foreach(var scene in machine.Config.story.op.scenes)foreach(var line in scene.lines){opening.Show(line);op.Reveal();Check(op.Body.preferredHeight<=op.Body.rectTransform.rect.height+1,"OP clipped");count++;}
        Check(count==29,"OP script changed");opening.Show(new StoryLine{speaker="？？",text="……まだ温かい"});op.Reveal();Capture("opening-anonymous");
        Check(AdventurePresentation.Portrait("声","サリア",null)!=AdventurePresentation.Portrait("？？","サリア",null),"Anonymous identities not distinguished");
        int sound=audio.SoundSequence;audio.DialogueTick("サリア");Check(sound==audio.SoundSequence,"Silent SE emitted");
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"captures\":"+captures+",\"opLines\":29,\"typing\":true,\"stableLayout\":true,\"graphemes\":true,\"pagination150\":true,\"portraitsAndFallback\":true,\"getLayerAndGeometry\":true,\"get1And4\":true,\"lateDialogueResize\":true,\"modalHide\":true,\"curse0_1_4\":true,\"pairedBlessings\":true,\"returnClears\":true,\"subtitlesOffKeepsOP\":true,\"seMute\":true,\"isolatedPreferences\":true,\"runtimeInputAndListening\":\"not tested\"}");
        Debug.Log("ADVENTURE_PRESENTATION_VALIDATION passed");
    }
    static Rect Bounds(RectTransform r){var v=new Vector3[4];r.GetWorldCorners(v);return Rect.MinMaxRect(v[0].x,v[0].y,v[2].x,v[2].y);}
    static void Capture(string name)
    {
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
        foreach(var c in canvases)
        {
            var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)UnityEngine.Object.DestroyImmediate(scaler);
            c.renderMode=RenderMode.WorldSpace;c.transform.position=Vector3.zero;c.transform.localScale=Vector3.one;c.worldCamera=camera;
            foreach(var safe in c.GetComponentsInChildren<SafeStage>()){safe.enabled=false;safe.Stage.SetParent(c.transform,false);}
        }
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
        {
            float w=540f*size.x/size.y;
            foreach(var c in canvases){((RectTransform)c.transform).sizeDelta=new Vector2(w,540);c.scaleFactor=size.y/540f;foreach(Transform child in c.transform)if(child.name=="Stage"){var r=(RectTransform)child;r.localScale=Vector3.one*Mathf.Min(1,w/r.sizeDelta.x);}}
            Canvas.ForceUpdateCanvases();
            foreach(var c in canvases)foreach(var t in c.GetComponentsInChildren<Text>())t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
            for(int pass=0;pass<3;pass++){foreach(var c in canvases)foreach(var graphic in c.GetComponentsInChildren<Graphic>()){graphic.SetAllDirty();graphic.Rebuild(CanvasUpdate.PreRender);graphic.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
            foreach(var view in UnityEngine.Object.FindObjectsByType<DialoguePresenter>(FindObjectsSortMode.None))if(view.isActiveAndEnabled)Check(view.Body.preferredHeight<=view.Body.rectTransform.rect.height+1,"Visible dialogue clipped: "+name);
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);captures++;
        }
    }
}
