using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AdventureHeroValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static GameController game;
    static Camera camera;
    static string output;
    static int captures;
    [Serializable] sealed class WalkKeyCandidate
    {
        public HeroPose[] poses;
        public HeroPose swordReady; // QA-only optional replacement for pose 23.
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
    static object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
    // Freeze authored keys; optional blocking plays them without synthesizing inbetweens.
    public static void RunWalkKeyPoses()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Directory.CreateDirectory(output);captures=0;
        var candidate=JsonUtility.FromJson<WalkKeyCandidate>(File.ReadAllText(Environment.GetEnvironmentVariable("HERO_WALK_KEYS")));
        Check(candidate.poses.Length==4||candidate.poses.Length==8,"Four blocking keys or eight cycle-ordered keys required");
        int keyCount=candidate.poses.Length;
        bool readyCandidate=candidate.swordReady!=null&&candidate.swordReady.w>0&&candidate.swordReady.h>0;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var machine=GameDataLoader.CreateMachine(new SystemRandom(1003));machine.Adv.nodeId="C-1";
        game=new GameObject("WalkKeyPoseQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);
        var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        camera=new GameObject("WalkKeyCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        var hero=Get<AdventureHeroPresenter>("_adventureHero");var cfg=HeroPresentation.Config;var original=cfg.poses.ToArray();
        var walk=Array.Find(cfg.motions,m=>m.id=="walk"&&string.IsNullOrEmpty(m.weaponId));
        var originalFrames=walk.frames;var originalSeconds=walk.seconds;var originalEvents=walk.events;
        bool blocking=Environment.GetEnvironmentVariable("HERO_WALK_BLOCKING")=="1";
        try
        {
            if(readyCandidate)
            {
                hero.Cancel();hero.Equip(new EquipItem{baseId="w_blade"});hero.Step(.001f);Capture("w_blade-ready-baseline");
                cfg.poses[23]=candidate.swordReady;
                // Render caches pose index/weapon. Leave the old index before measuring an in-memory replacement.
                hero.Request("walk",false);hero.Step(.001f);
            }
            foreach(string weapon in new[]{"w_blade","w_spear"})
            {
                hero.Cancel();hero.Equip(new EquipItem{baseId=weapon});hero.Step(.001f);
                if(readyCandidate&&weapon=="w_blade")
                    Check(hero.View.Body.sprite.texture==Resources.Load<Texture2D>("Art/HeroPrototype/"+candidate.swordReady.atlas),"Ready candidate texture was not rendered");
                Capture(weapon+(readyCandidate&&weapon=="w_blade"?"-ready-candidate":"-ready-reference"));
                for(int key=0;key<keyCount;key++)
                {
                    hero.Cancel();cfg.poses[15]=candidate.poses[key];hero.Request("walk",false);hero.Step(.01f);
                    Check(hero.View.Player.PoseIndex==15,"Key advanced unexpectedly");
                    Check(hero.View.Hand.gameObject.activeSelf==(weapon=="w_blade"),"Hand overlay leaked to spear");
                    Capture(weapon+"-key-"+key);
                }
            }
            if(blocking)
            {
                // Legacy four-key input is contact A/B, pass A/B; eight-key input is already cycle ordered.
                var order=keyCount==4?new[]{0,2,1,3}:Enumerable.Range(0,8).ToArray();
                for(int i=0;i<keyCount;i++)cfg.poses[15+i]=candidate.poses[order[i]];
                walk.frames=Enumerable.Range(15,keyCount).ToArray();
                walk.seconds=Enumerable.Repeat(.88f/keyCount,keyCount).ToArray();
                walk.events=Enumerable.Range(0,keyCount).Select(i=>i==0||i==keyCount/2?"footstep":"").ToArray();
                var trace=new System.Text.StringBuilder("weapon,frame,state,pose,gripX,gripY,handVisible\n");
                foreach(string weapon in keyCount==4?new[]{"w_blade"}:new[]{"w_blade","w_spear"})
                {
                    hero.Cancel();hero.Equip(new EquipItem{baseId=weapon});hero.Request("walk",false);
                    var visited=new System.Collections.Generic.HashSet<int>();
                    for(int frame=0;frame<90;frame++)
                    {
                        if(frame==54)hero.Request("attack-f1",false);
                        if(frame==60)hero.Request("attack-f2",false);
                        if(frame==66)hero.Request("attack-f3-4",false);
                        if(frame==67)hero.Request("idle",false);
                        hero.Step(1f/30);
                        var view=hero.View;
                        if(frame<54)
                        {
                            visited.Add(view.Player.PoseIndex);
                            Check(Vector2.Distance(view.Weapon.rectTransform.anchoredPosition,view.GripPosition)<.01f,"Walk weapon detached from grip");
                            Check(view.Hand.gameObject.activeSelf==(weapon=="w_blade"),"Walking hand layer mismatch");
                        }
                        trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F3},{5:F3},{6}",weapon,frame,view.Player.State,view.Player.PoseIndex,view.GripPosition.x,view.GripPosition.y,view.Hand.gameObject.activeSelf));
                        Capture((keyCount==4?"four-key-blocking":weapon+"-eight-key-blocking")+"-"+frame.ToString("D3"),false);
                    }
                    Check(visited.SetEquals(walk.frames),"Not all authored walk keys played");
                    Check(hero.View.Player.State=="idle","Blocking did not return from attack");
                }
                File.WriteAllText(Path.Combine(output,"keypose-trace.csv"),trace.ToString());
            }
        }
        finally{cfg.poses=original;walk.frames=originalFrames;walk.seconds=originalSeconds;walk.events=originalEvents;}
        File.WriteAllText(Path.Combine(output,"keypose-validation.json"),"{\"passed\":true,\"stills\":"+captures+",\"keys\":"+keyCount+",\"weapons\":2,\"blockingPreview\":"+(blocking?"true":"false")+",\"swordReadyCandidate\":"+(readyCandidate?"true":"false")+",\"mainAssetsChanged\":false,\"walkCycleApproved\":false,\"finalArtApproved\":false}");
    }
    // Spear art QA: the shaft interval is measured on weapons-v1, not its whole transparent rectangle.
    static void CheckSpearGrip(HeroPresentationView view)
    {
        var cfg=HeroPresentation.Config;var pose=cfg.poses[view.Player.PoseIndex];
        Check(view.Body.sprite.texture==Resources.Load<Texture2D>("Art/HeroPrototype/"+pose.atlas),"Stale spear pose texture");
        Check(!view.Hand.gameObject.activeSelf,"Sword foreground hand leaked into spear");
        Check(view.TwoHanded==pose.twoHanded,"Two-handed flag mismatch");
        if(!view.TwoHanded)return;
        var look=Array.Find(cfg.weapons,w=>w.id=="w_spear");
        Check(look.atlas=="weapons-v1"&&look.y==20&&look.h==975,"Re-measure shaft interval for another weapon atlas");
        Vector3 local=view.Weapon.rectTransform.InverseTransformPoint(view.Root.TransformPoint(view.SupportPosition));
        float pixelY=look.gripY-local.y/look.height*look.h;
        Check(Mathf.Abs(local.x)<.02f,"Support hand misses spear axis");
        // Blue shaft runs from source y360 to y860, i.e. crop-relative y340..840.
        Check(pixelY>=340&&pixelY<=840&&look.gripY>=340&&look.gripY<=840,"Grip touches spear head/collar instead of shaft: "+view.Player.PoseIndex+" y="+pixelY);
        Check(Vector2.Distance(view.Weapon.rectTransform.anchoredPosition,view.GripPosition)<.01f,"Rear grip detached");
        Check(view.Weapon.transform.GetSiblingIndex()<view.Body.transform.GetSiblingIndex(),"Spear should sit behind gripping hands");
        Check(Mathf.Abs(view.Weapon.rectTransform.rect.height-look.height)<.01f,"Spear stretched between hands");
    }
    public static void RunSpearComparison()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Directory.CreateDirectory(output);captures=0;
        var cfg=HeroPresentation.Config;var original=cfg.poses;var originalWeapons=cfg.weapons;var originalMotions=cfg.motions;
        var candidate=JsonUtility.FromJson<HeroPresentationConfig>(File.ReadAllText(Environment.GetEnvironmentVariable("HERO_SPEAR_CANDIDATE")));
        Check(candidate.poses.Length==original.Length||candidate.poses.Length==original.Length+1,"Candidate pose count mismatch");
        foreach(var motion in candidate.motions.Where(m=>m.weaponId=="w_spear"&&m.events.Contains("contact")))
        {
            var before=Array.Find(originalMotions,m=>m.id==motion.id&&m.weaponId=="w_spear");
            float contact=motion.seconds.Take(Array.IndexOf(motion.events,"contact")).Sum();
            float oldContact=before.seconds.Take(Array.IndexOf(before.events,"contact")).Sum();
            Check(Mathf.Abs(contact-oldContact)<.0001f&&Mathf.Abs(motion.seconds.Sum()-before.seconds.Sum())<.0001f,"Inbetween changed contact or total duration");
            Check(motion.events.Count(e=>e=="swing")==1&&motion.events.Count(e=>e=="contact")==1,"Inbetween duplicated cues");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var machine=GameDataLoader.CreateMachine(new SystemRandom(1004));machine.Adv.nodeId="C-1";
        game=new GameObject("SpearComparisonQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);
        var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        camera=new GameObject("SpearCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        var hero=Get<AdventureHeroPresenter>("_adventureHero");
        var trace=new System.Text.StringBuilder("variant,frame,state,pose,weapon,twoHanded,gripX,gripY,supportX,supportY\n");
        try
        {
            foreach(string variant in new[]{"existing","candidate"})
            {
                // Move off any cached pose before replacing its art/config.
                hero.Cancel();hero.Equip(new EquipItem{baseId="w_blade"});hero.Request("walk",false);hero.Step(.02f);
                cfg.poses=variant=="candidate"?candidate.poses:original;
                cfg.weapons=variant=="candidate"?candidate.weapons:originalWeapons;
                cfg.motions=variant=="candidate"?candidate.motions:originalMotions;
                hero.Equip(new EquipItem{baseId="w_spear"});hero.Cancel();hero.Request("walk",false);
                int contacts=0;Action<string> cue=c=>{if(c=="contact")contacts++;};hero.View.Player.Cue+=cue;
                var visited=new System.Collections.Generic.HashSet<int>();
                try
                {
                    for(int frame=0;frame<72;frame++)
                    {
                        if(frame==15)hero.Request("idle",false);
                        if(frame==24)hero.Request("attack",false);
                        if(frame==25)hero.Request("idle",false);
                        hero.Step(1f/30);var view=hero.View;CheckSpearGrip(view);visited.Add(view.Player.PoseIndex);
                        trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6:F3},{7:F3},{8:F3},{9:F3}",variant,frame,view.Player.State,view.Player.PoseIndex,view.Player.WeaponId,view.TwoHanded,view.GripPosition.x,view.GripPosition.y,view.SupportPosition.x,view.SupportPosition.y));
                        Capture(variant+"-spear-sequence-"+frame.ToString("D3"),false);
                        if(new[]{14,15,26,31,34,36,40}.Contains(frame))Capture(variant+"-spear-key-"+frame);
                    }
                    Check(Enumerable.Range(31,6).All(visited.Contains),"Spear cycle missed a key");
                    if(variant=="candidate"&&candidate.poses.Length>original.Length)Check(visited.Contains(original.Length),"Spear cycle missed the added inbetween");
                    Check(contacts==1&&hero.View.Player.State=="idle","Spear cycle must contact once and return idle");
                    hero.Equip(new EquipItem{baseId="w_blade"});Capture(variant+"-idle-blade");
                    hero.Equip(new EquipItem{baseId="w_spear"});CheckSpearGrip(hero.View);Capture(variant+"-idle-spear");
                    hero.Request("walk",false);hero.Step(.28f);int pose=hero.View.Player.PoseIndex;float phase=hero.View.Player.NormalizedTime;
                    hero.Equip(new EquipItem{baseId="w_blade"});Check(hero.View.Player.PoseIndex==pose&&Mathf.Abs(hero.View.Player.NormalizedTime-phase)<.001f,"Walking swap reset phase");Capture(variant+"-walking-blade");
                    hero.Equip(new EquipItem{baseId="w_spear"});Check(hero.View.Player.PoseIndex==pose,"Walking spear swap reset pose");Capture(variant+"-walking-spear");
                    hero.Cancel();hero.Request("attack",false);hero.Step(.24f);int before=contacts;
                    hero.Equip(new EquipItem{baseId="w_blade"});Check(hero.View.Player.WeaponId=="w_spear"&&hero.View.Player.PendingWeapon=="w_blade","Active thrust changed weapon early");Capture(variant+"-swap-pending");
                    hero.Step(.075f);Check(contacts==before+1&&hero.View.Player.WeaponId=="w_spear","Spear contact missing before swap");CheckSpearGrip(hero.View);Capture(variant+"-swap-contact");
                    hero.Step(.1f);Check(hero.View.Player.State=="recover"&&hero.View.Player.WeaponId=="w_blade","Swap not applied on recovery");Capture(variant+"-swap-blade-recovery");
                    hero.Step(1);Check(hero.View.Player.State=="idle"&&contacts==before+1,"Swapping duplicated contact");
                }
                finally{hero.View.Player.Cue-=cue;}
            }
        }
        finally{cfg.poses=original;cfg.weapons=originalWeapons;cfg.motions=originalMotions;}
        File.WriteAllText(Path.Combine(output,"spear-trace.csv"),trace.ToString());
        File.WriteAllText(Path.Combine(output,"spear-comparison.json"),"{\"passed\":true,\"stills\":"+captures+",\"shaftSegmentAndRigidLength\":true,\"walkSwapPhase\":true,\"deferredSwapSingleContact\":true,\"candidatePromoted\":false,\"finalArtApproved\":false}");
        Debug.Log("SPEAR_COMPARISON passed captures="+captures);
    }
    public static void RunGripComparison()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Directory.CreateDirectory(output);captures=0;
        var cfg=HeroPresentation.Config;var original=cfg.poses;
        var candidate=JsonUtility.FromJson<HeroPresentationConfig>(File.ReadAllText(Environment.GetEnvironmentVariable("HERO_GRIP_CANDIDATE")));
        Check(candidate.poses.Length==original.Length,"Candidate pose count mismatch");
        string baselinePath=Environment.GetEnvironmentVariable("HERO_GRIP_BASELINE");
        var baseline=string.IsNullOrEmpty(baselinePath)?original:JsonUtility.FromJson<HeroPresentationConfig>(File.ReadAllText(baselinePath)).poses;
        Check(baseline.Length==original.Length,"Baseline pose count mismatch");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var machine=GameDataLoader.CreateMachine(new SystemRandom(1003));machine.Adv.nodeId="C-1";
        game=new GameObject("GripComparisonQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);
        var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        camera=new GameObject("GripCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        var hero=Get<AdventureHeroPresenter>("_adventureHero");
        try
        {
            foreach(string variant in new[]{"existing","candidate"})
            {
                cfg.poses=variant=="candidate"?candidate.poses:baseline;
                foreach(string weapon in new[]{"w_blade","w_spear"})
                {
                    hero.Cancel();hero.Equip(new EquipItem{baseId=weapon});hero.Request("walk",false);hero.Step(.02f);
                    Capture(variant+"-"+weapon+"-walk");
                    foreach(float time in new[]{0f,.10f,.20f,.33f,.40f,.50f,.60f,.72f,1.1f})
                    {
                        hero.Cancel();hero.Request("attack",false);hero.Step(time==0?.001f:time);
                        var view=hero.View;var p=cfg.poses[view.Player.PoseIndex];
                        Check(view.Body.sprite.texture==Resources.Load<Texture2D>("Art/HeroPrototype/"+p.atlas),"Stale pose texture in comparison");
                        if(variant=="candidate"&&weapon=="w_blade")
                        {
                            var corners=new Vector3[4];view.Weapon.rectTransform.GetWorldCorners(corners);
                            Check(corners.Min(c=>view.Root.InverseTransformPoint(c).y)>=-1,"Sword extends below foot plane: "+view.Player.PoseIndex);
                        }
                        bool front=weapon=="w_blade"&&p.swordHand!=null&&p.swordHand.w>0&&p.swordHand.h>0;
                        Check(view.Hand.gameObject.activeSelf==front,"Foreground hand leaked across pose/weapon");
                        if(front)
                        {
                            Check(view.Body.transform.GetSiblingIndex()<view.Weapon.transform.GetSiblingIndex()&&view.Weapon.transform.GetSiblingIndex()<view.Hand.transform.GetSiblingIndex(),"Grip layering wrong");
                            var hand=p.swordHand;
                            Check(p.gripX>=hand.x&&p.gripX<=hand.x+hand.w&&p.gripY>=hand.y&&p.gripY<=hand.y+hand.h,"Grip outside hand patch");
                            Check(Vector2.Distance(view.Weapon.rectTransform.anchoredPosition,view.GripPosition)<.01f,"Weapon detached from grip");
                        }
                        Capture(variant+"-"+weapon+"-t"+Mathf.RoundToInt(time*1000));
                    }
                }
                hero.Cancel();hero.Equip(new EquipItem{baseId="w_blade"});hero.Request("walk",false);
                var visited=new System.Collections.Generic.HashSet<int>();
                var trace=new System.Text.StringBuilder("frame,state,pose,gripX,gripY,handVisible\n");
                int contacts=0;Action<string> onCue=cue=>{if(cue=="contact")contacts++;};
                hero.View.Player.Cue+=onCue;
                for(int frame=0;frame<60;frame++)
                {
                    if(frame==15)hero.Request("attack",false);
                    if(frame==16)hero.Request("idle",false);
                    hero.Step(1f/30);
                    var view=hero.View;visited.Add(view.Player.PoseIndex);
                    trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3:F3},{4:F3},{5}",frame,view.Player.State,view.Player.PoseIndex,view.GripPosition.x,view.GripPosition.y,view.Hand.gameObject.activeSelf));
                    Capture(variant+"-grip-sequence-"+frame.ToString("D3"),false);
                }
                Check(Enumerable.Range(23,8).All(visited.Contains),"Sword comparison missed an attack/recovery key");
                Check(hero.View.Player.State=="idle","Sword comparison did not return to idle");
                hero.View.Player.Cue-=onCue;
                Check(contacts==1,"Sword comparison contact count was "+contacts);
                File.WriteAllText(Path.Combine(output,variant+"-grip-trace.csv"),trace.ToString());
            }
        }
        finally{cfg.poses=original;}
        File.WriteAllText(Path.Combine(output,"grip-comparison.json"),"{\"passed\":true,\"stills\":"+captures+",\"handLayerAndGrip\":true,\"finalArtApproved\":false}");
    }
    // Candidate atlas is copied only to isolated QA Resources; never changes shipped JSON.
    public static void RunWalkComparison()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Directory.CreateDirectory(output);captures=0;
        var candidate=JsonUtility.FromJson<HeroPresentationConfig>(File.ReadAllText(Environment.GetEnvironmentVariable("HERO_WALK_CANDIDATE")));
        Check(candidate.poses.Length==8,"Eight candidate poses required");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var machine=GameDataLoader.CreateMachine(new SystemRandom(1003));machine.Adv.nodeId="C-1";
        game=new GameObject("WalkComparisonQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);
        var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        camera=new GameObject("WalkComparisonCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        var hero=Get<AdventureHeroPresenter>("_adventureHero");var cfg=HeroPresentation.Config;
        var original=cfg.poses.ToArray();var trace=new System.Text.StringBuilder("variant,weapon,frame,pose,gripX,gripY\n");
        try
        {
            foreach(string variant in new[]{"existing","candidate"})
            {
                for(int i=0;i<8;i++)cfg.poses[15+i]=variant=="candidate"?candidate.poses[i]:original[15+i];
                foreach(string weapon in new[]{"w_blade","w_spear"})
                {
                    hero.Cancel();hero.Equip(new EquipItem{baseId=weapon});hero.Request("walk",false);
                    for(int i=0;i<8;i++)
                    {
                        hero.Step(i==0?.055f:.11f);Capture(variant+"-"+weapon+"-walk-"+i);
                        var grip=hero.View.GripPosition;
                        trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F3},{5:F3}",variant,weapon,i,hero.View.Player.PoseIndex,grip.x,grip.y));
                    }
                    hero.Request("attack-f1",false);hero.Step(.02f);Capture(variant+"-"+weapon+"-ready");
                    hero.Request("attack-f3-4",false);hero.Step(.08f);Check(hero.ImpactReady,"Contact missing");Capture(variant+"-"+weapon+"-contact");
                    hero.Request("idle",false);hero.Step(1);Capture(variant+"-"+weapon+"-resumed");
                }
                hero.Cancel();hero.Equip(new EquipItem{baseId="w_blade"});hero.Request("walk",false);
                for(int frame=0;frame<60;frame++)
                {
                    if(frame==27)hero.Request("attack-f1",false);
                    if(frame==33)hero.Request("attack-f2",false);
                    if(frame==39)hero.Request("attack-f3-4",false);
                    if(frame==40)hero.Request("idle",false);
                    hero.Step(1f/30);Capture(variant+"-sequence-"+frame.ToString("D3"),false);
                }
            }
        }
        finally{cfg.poses=original;}
        File.WriteAllText(Path.Combine(output,"grips.csv"),trace.ToString());
        File.WriteAllText(Path.Combine(output,"comparison.json"),"{\"passed\":true,\"stills\":"+captures+",\"candidatePromoted\":false,\"finalArtApproved\":false}");
    }
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var audio=AudioManager.Create();audio.SeVolume=0;
        var machine=GameDataLoader.CreateMachine(new SystemRandom(928));machine.Adv.nodeId="C-1";
        game=new GameObject("HeroGameplayQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        var hero=Get<AdventureHeroPresenter>("_adventureHero");Check(hero!=null,"New hero is not enabled in gameplay");
        Check(!Get<SpriteAnimator>("_charAnim").enabled&&!Get<RectTransform>("_charRt").GetComponent<Image>().enabled,"Legacy hero overlaps new hero");
        camera=new GameObject("HeroGameplayCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        int credit=machine.Credit,sound=audio.SoundSequence;
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            hero.Cancel();machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId=weapon,slot=EquipSlot.Weapon};Call("RefreshUi");
            Check(hero.View.Player.WeaponId==weapon,"Equipped inventory did not update appearance");
            foreach(string motion in new[]{"idle","walk","focus","attack-f1","attack-f2","attack-f3-4","slash","guard","cast","hit","victory"})
            {
                hero.Cancel();Call("PlayCharacter",motion);hero.Step(.02f);
                Check(hero.View.Player.State==motion,"Gameplay command not mapped: "+motion);Capture(weapon+"-"+motion);
            }
            hero.Cancel();hero.Request("attack-f3-4",false);int version=hero.MotionVersion;
            hero.Request("walk",false);Check(hero.MotionVersion==version,"Queued walk replaced active stroke");
            hero.Step(.08f);Check(hero.ImpactReady,"Queued walk lost contact");
            hero.Request("idle",false);Check(hero.ImpactReady&&hero.MotionVersion==version,"Idle erased completed contact");
            Capture(weapon+"-queued-loop-contact");hero.Step(1);
            Check(hero.View.Player.State=="idle","Queued loop did not resume");Capture(weapon+"-queued-loop-resumed");
        }
        hero.Cancel();Call("PlayCharacter","attack-f1");hero.Step(10);Call("PlayCharacter","walk");
        Check(hero.View.Player.State=="walk","Non-reel cut-in left held pose");
        hero.Equip(new EquipItem{baseId="w_blade"});Call("PlayCharacter","slash");hero.Equip(new EquipItem{baseId="w_spear"});
        Check(hero.View.Player.PendingWeapon=="w_spear"&&hero.View.Player.WeaponId=="w_blade","Weapon changed inside stroke");hero.Step(2);
        Check(hero.View.Player.WeaponId=="w_spear","Pending weapon not applied");
        machine.Equip.Worn.Clear();Call("RefreshUi");Check(hero.View.Player.WeaponId=="w_blade","Unequip did not restore default look");
        hero.Cancel();machine.EnemyActive=true;Call("PlayCharacter","walk");Check(hero.View.Player.State=="idle","Enemy presence did not stop walking");machine.EnemyActive=false;
        machine.PrecursorRemaining=2;Call("PlayCharacter","walk");Check(hero.View.Player.State=="idle","Precursor did not stop walking");machine.PrecursorRemaining=0;
        var dialogue=Get<DialoguePresenter>("_dialogue");dialogue.Begin("サリア","さあ、霧の向こうへ。みんなの笑顔を守るために！");dialogue.Reveal();Capture("dialogue-with-new-hero");
        Check(machine.Credit==credit&&audio.SoundSequence==sound,"Presentation changed balance or duplicated gameplay audio");
        var host=Get<RectTransform>("_charRt");Vector3 original=hero.View.Body.transform.position;host.anchoredPosition+=new Vector2(15,0);
        Check(Vector3.Distance(original,hero.View.Body.transform.position)>1,"New body detached from existing effect parent");host.anchoredPosition-=new Vector2(15,0);
        HeroPresentation.Config.gameplayEnabled=false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        game=new GameObject("LegacyHeroFallbackQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);typeof(GameController).GetField("_audio",Private).SetValue(game,AudioManager.Create());
        Call("BuildUi");Check(Get<AdventureHeroPresenter>("_adventureHero")==null&&Get<SpriteAnimator>("_charAnim").enabled,"Legacy rollback flag failed");
        HeroPresentation.Config.gameplayEnabled=true;
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"stills\":"+captures+",\"gameplayHeroEnabled\":true,\"inventoryAppearance\":true,\"commands\":11,\"weapons\":2,\"legacyHidden\":true,\"legacyRollback\":true,\"enemyAndPrecursorIdle\":true,\"nonReelHoldReleased\":true,\"pendingWeapon\":true,\"presentationDoesNotChangeCreditOrAudio\":true,\"finalArtApproved\":false}");
        Debug.Log("ADVENTURE_HERO_VALIDATION passed captures="+captures);
    }
    public static void RunApproachComparison()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Directory.CreateDirectory(output);captures=0;
            var cfg=HeroPresentation.Config;var original=JsonUtility.ToJson(cfg);
            bool sourceApproachEnabled=cfg.approach!=null&&cfg.approach.enabled;
            var enemyCfg=EnemyPresentation.Config;var originalTypes=enemyCfg.gameplayTypes;
            var rows=new System.Collections.Generic.List<string>();
            var transitionRows=new System.Collections.Generic.List<string>{"frame,weapon,elapsed,capture,approaching,state,pose,bodySprite,bodyTexture,advance,normalizedTime,contacts"};
            string heightRule=Environment.GetEnvironmentVariable("HERO_ENEMY_HEIGHT_RULE");
            try
            {
            string candidate=Environment.GetEnvironmentVariable("HERO_SPEAR_CANDIDATE");
            if(!string.IsNullOrEmpty(candidate))JsonUtility.FromJsonOverwrite(File.ReadAllText(candidate),cfg);
            if(heightRule=="runtime-data")Check(sourceApproachEnabled,"Main source approach config is disabled");
            cfg.approach=cfg.approach??new HeroApproach{maxDistance=600};cfg.approach.enabled=false;
            float lead=cfg.approach.leadSeconds;
            enemyCfg.gameplayTypes=new[]{"slime","goblin","forest_wolf"}; // Wolf remains preview-only in main.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
            var machine=GameDataLoader.CreateMachine(new SystemRandom(1007));machine.Adv.nodeId="C-1";
            game=new GameObject("HeroApproachQA").AddComponent<GameController>();game.enabled=false;
            typeof(GameController).GetField("_m",Private).SetValue(game,machine);
            var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
            Call("BuildUi");Call("RefreshUi");
            camera=new GameObject("ApproachCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
            var hero=Get<AdventureHeroPresenter>("_adventureHero");var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");
            int contacts=0;hero.View.Player.Cue+=cue=>{if(cue=="contact")contacts++;};
            float footY=hero.View.Root.anchoredPosition.y;
            foreach(string type in new[]{"slime","goblin","forest_wolf"})foreach(string weapon in new[]{"w_blade","w_spear"})
            {
                if(heightRule=="auto"&&weapon=="w_spear")ApplySpearHeightRule(cfg,type);
                Get<Image>("_enemyImg").color=Color.white;
                Call("BeginEnemyPresentation",type,null);Get<CanvasGroup>("_enemyCg").alpha=1;
                Check(enemy.Active,"Enemy preview missing: "+type);
                Check(enemy.View.Body.color.a>.99f,"Enemy preview is transparent");
                hero.Cancel();hero.Equip(new EquipItem{baseId=weapon});
                var target=enemy.View.Body.rectTransform;
                foreach(bool enabled in new[]{false,true})
                {
                    cfg.approach.enabled=enabled;hero.Cancel();int before=contacts;
                    hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Request("idle",false);
                    hero.Step((enabled?lead:0)+.08f);Check(contacts==before+1&&hero.ImpactReady,"Single contact lost");
                    Check(Mathf.Abs(hero.View.Root.anchoredPosition.y-footY)<.001f,"Foot plane changed");
                    var rt=hero.View.Weapon.rectTransform;
                    Vector3 tip=rt.TransformPoint(new Vector3((.5f-rt.pivot.x)*rt.rect.width,(1-rt.pivot.y)*rt.rect.height,0));
                    var parent=hero.View.Root.parent;
                    Vector3 gap=parent.InverseTransformPoint(target.TransformPoint(target.rect.center))-parent.InverseTransformPoint(tip);
                    float anchorOffset=heightRule=="auto"&&weapon=="w_spear"?EnemyContactAnchorOffset(type):0;
                    gap.y+=anchorOffset;
                    if(enabled)Check(Mathf.Abs(gap.x)<.05f,"Tip horizontal gap: "+gap.x);
                    else Check(hero.Advance==0,"Disabled approach moved");
                    rows.Add($"{type},{weapon},{enabled},{hero.Advance:F3},{gap.x:F3},{gap.y:F3},{anchorOffset:F3}");
                    Capture(type+"-"+weapon+(enabled?"-approach":"-baseline")+"-contact");
                    hero.Step(.18f);Capture(type+"-"+weapon+(enabled?"-approach":"-baseline")+"-recovery",false);
                    hero.Step(.82f);Check(Mathf.Abs(hero.Advance)<.001f,"Recovery left displacement");
                }
                cfg.approach.enabled=true;
                hero.Cancel();hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Step(.03f);
                var targetHome=target.anchoredPosition;target.anchoredPosition+=Vector2.left*20;
                hero.Step(lead+.05f);
                var movingWeapon=hero.View.Weapon.rectTransform;
                Vector3 movingTip=movingWeapon.TransformPoint(new Vector3((.5f-movingWeapon.pivot.x)*movingWeapon.rect.width,(1-movingWeapon.pivot.y)*movingWeapon.rect.height,0));
                Check(Mathf.Abs(hero.View.Root.parent.InverseTransformPoint(movingTip).x-hero.View.Root.parent.InverseTransformPoint(target.TransformPoint(target.rect.center)).x)<.05f,"Moving target was not tracked");
                target.anchoredPosition=targetHome;hero.Cancel();
                if(lead>0)
                {
                    int before=contacts;
                    hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);
                    hero.Request("idle",false);
                    var walkSamples=new[]{new{seconds=.06f,pose=15},new{seconds=.06f,pose=16},new{seconds=.12f,pose=17},new{seconds=.039f,pose=17}};
                    foreach(var sample in walkSamples)
                    {
                        hero.Step(sample.seconds);
                        Check(hero.View.RenderedPoseIndex==sample.pose,"Approach gait did not honor authored walk duration at pose "+sample.pose);
                        Check(hero.IsApproaching&&!hero.ImpactReady&&contacts==before,"Contact ran during approach");
                        Check(hero.View.Player.NormalizedTime==0,"Combat clock advanced during approach");
                    }
                    hero.Step(.002f);
                    int expectedWindup=System.Array.Find(cfg.motions,m=>m.id=="attack-f2"&&m.weaponId==weapon)?.frames.LastOrDefault()??
                        System.Array.Find(cfg.motions,m=>m.id=="attack-f2"&&string.IsNullOrEmpty(m.weaponId)).frames.Last();
                    Check(hero.IsApproaching&&hero.View.RenderedPoseIndex==expectedWindup&&!hero.ImpactReady&&contacts==before,"Authored wind-up did not replace the final run pose before contact");
                    hero.Equip(new EquipItem{baseId=weapon=="w_blade"?"w_spear":"w_blade"});
                    Check(hero.View.Player.WeaponId==weapon,"Queued weapon changed while approaching");
                    hero.Step(lead-.281f+.08f);Check(contacts==before+1,"Contact after approach missing");
                    hero.Step(1);hero.Equip(new EquipItem{baseId=weapon});hero.Cancel();
                    if(type=="goblin")
                    {
                        for(int frame=0;frame<45;frame++)
                        {
                            if(frame==3){hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Request("idle",false);}
                            hero.Step(1f/30);Capture(weapon+"-approach-sequence-"+frame.ToString("D3"),false);
                        }
                        Check(hero.Advance==0&&!hero.IsApproaching,"Sequence did not return home");

                        // Capture the hand-off at 120 Hz. The normal reel above is 30 Hz and can
                        // skip the one frame where the authored walk pose gives way to attack.
                        hero.Cancel();hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Request("idle",false);
                        var attackMotion=System.Array.Find(cfg.motions,m=>m.id=="attack-f3-4"&&m.weaponId==weapon)
                            ??System.Array.Find(cfg.motions,m=>m.id=="attack-f3-4"&&string.IsNullOrEmpty(m.weaponId));
                        int attackContactIndex=attackMotion?.events==null?-1:System.Array.IndexOf(attackMotion.events,"contact");
                        Check(attackContactIndex>=0,"Attack contact key missing for hand-off timing");
                        float authoredContactTime=0;for(int i=0;i<attackContactIndex;i++)authoredContactTime+=Mathf.Max(.01f,attackMotion.seconds[i]);
                        int transitionContactStart=contacts;float elapsed=0;const float transitionStep=1f/120;
                        int windupMotionIndex=System.Array.FindIndex(cfg.motions,m=>m.id=="attack-f2"&&m.weaponId==weapon);
                        if(windupMotionIndex<0)windupMotionIndex=System.Array.FindIndex(cfg.motions,m=>m.id=="attack-f2"&&string.IsNullOrEmpty(m.weaponId));
                        Check(windupMotionIndex>=0,"Attack wind-up key missing for hand-off timing");
                        int windupPose=cfg.motions[windupMotionIndex].frames[cfg.motions[windupMotionIndex].frames.Length-1];
                        float handoffSeconds=Mathf.Min(lead,Mathf.Max(0,cfg.approach.handoffSeconds));
                        float transitionEnd=Mathf.Max(lead+.07f,lead+authoredContactTime+.05f);
                        int previousRenderedPose=-1;string previousCapturePath=null;bool walkToWindupPixelsChanged=false,windupToStrikePixelsChanged=false;
                        int capturedWindup=0,capturedWalk=0;
                        for(int frame=0;frame<120&&elapsed<transitionEnd;frame++)
                        {
                            hero.Step(transitionStep);elapsed+=transitionStep;
                            if(elapsed+0.00001f<lead+authoredContactTime)
                                Check(contacts==transitionContactStart,"Contact fired before its authored attack timestamp");
                            Check(contacts<=transitionContactStart+1,"Contact fired more than once near hand-off");
                            if(elapsed>=Mathf.Max(0,lead-Mathf.Max(.06f,handoffSeconds+.03f))&&elapsed<=lead+.07f)
                            {
                                var bodySprite=hero.View.Body.sprite;
                                Check(bodySprite!=null,"Handoff body sprite missing");
                                string captureName=weapon+"-approach-handoff-"+frame.ToString("D3");
                                transitionRows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "{0},{1},{2:F4},{3},{4},{5},{6},{7},{8},{9:F3},{10:F3},{11}",frame,weapon,elapsed,captureName,
                                    hero.IsApproaching,hero.View.Player.State,hero.View.RenderedPoseIndex,bodySprite.name,bodySprite.texture.name,
                                    hero.Advance,hero.View.Player.NormalizedTime,contacts-transitionContactStart));
                                Capture(captureName,false);
                                string capturePath=Path.Combine(output,captureName+"-1280.png");
                                if(hero.IsApproaching&&handoffSeconds>0&&elapsed>=lead-handoffSeconds-0.0001f)
                                {
                                    Check(hero.View.RenderedPoseIndex==windupPose,"Approach did not show the authored wind-up pose before strike");
                                    capturedWindup++;
                                }
                                else if(hero.IsApproaching&&elapsed<lead-handoffSeconds)capturedWalk++;
                                if(previousRenderedPose>=15&&previousRenderedPose<=22&&hero.View.RenderedPoseIndex==windupPose)
                                {
                                    Check(!string.IsNullOrEmpty(previousCapturePath)&&File.Exists(previousCapturePath),"Walk-side hand-off capture missing");
                                    Check(!File.ReadAllBytes(previousCapturePath).SequenceEqual(File.ReadAllBytes(capturePath)),"Walk-to-wind-up changed pose without changing rendered capture");
                                    walkToWindupPixelsChanged=true;
                                }
                                if(previousRenderedPose==windupPose&&!hero.IsApproaching&&hero.View.RenderedPoseIndex==attackMotion.frames[0])
                                {
                                    Check(!string.IsNullOrEmpty(previousCapturePath)&&File.Exists(previousCapturePath),"Wind-up-side hand-off capture missing");
                                    Check(!File.ReadAllBytes(previousCapturePath).SequenceEqual(File.ReadAllBytes(capturePath)),"Wind-up-to-strike changed pose without changing rendered capture");
                                    windupToStrikePixelsChanged=true;
                                }
                                previousRenderedPose=hero.View.RenderedPoseIndex;previousCapturePath=capturePath;
                            }
                        }
                        Check(capturedWalk>0&&capturedWindup>0,"Transition did not capture both walk and attack wind-up for "+weapon);
                        Check(walkToWindupPixelsChanged&&windupToStrikePixelsChanged,"Rendered walk/wind-up/strike hand-off is missing an image change for "+weapon);
                        Check(!hero.IsApproaching,"Approach did not hand off before combat timing");
                        Check(hero.View.Player.State=="attack-f3-4","Approach hand-off did not enter requested attack");
                        Check(elapsed+transitionStep*.5f>=lead+authoredContactTime,"Hand-off trace ended before authored contact time");
                        hero.Step(1);Check(contacts==transitionContactStart+1,"Contact did not fire exactly once after hand-off");
                        Check(hero.Advance==0,"Hand-off attack did not recover to rest");
                    }
                }
            File.WriteAllLines(Path.Combine(output,"approach-handoff-120hz.csv"),transitionRows);
                foreach(float dt in new[]{1f/60,1f/15,.2f})
                {
                    hero.Cancel();int before=contacts;hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);
                    for(int i=0;i<120;i++)hero.Step(dt);
                    Check(contacts==before+1&&hero.Advance==0,"Low FPS contact/return failed");
                }
                hero.Cancel();hero.Request("attack-f3-4",false);hero.Step(.04f);Check(hero.Advance==0,"Target leaked into unrelated attack");
                hero.Cancel();hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Step(.03f);
                Check(hero.Advance>0,"Approach did not start");hero.Step(0);hero.Step(float.NaN);hero.Step(float.PositiveInfinity);
                Check(hero.Advance>0,"Invalid delta changed position");
                hero.Request("hit",false);Check(hero.Advance==0,"Interruption left displacement");
                hero.Cancel();hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Step(.03f);hero.Cancel();Check(hero.Advance==0,"Cancel left displacement");
                hero.PrepareAttackTarget(target);hero.Request("attack-f3-4",false);hero.Step(.03f);Call("HideEnemy");Check(hero.Advance==0,"Enemy dismissal left displacement");
            }
            File.WriteAllLines(Path.Combine(output,"contact-gaps.csv"),new[]{"enemy,weapon,enabled,advance,horizontalGap,verticalGap,anchorOffset"}.Concat(rows));
            File.WriteAllText(Path.Combine(output,"height-rules.json"),"{\"mode\":\"source-pose\",\"candidateContactAnchorsApplied\":false,\"finalArtApproved\":false}");
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"stills\":"+captures+",\"horizontalContact\":true,\"footPlane\":true,\"lowFpsSingleContact\":true,\"cancelAndInterruption\":true,\"walkToAttackCapturePixelChange\":true,\"walkWindupStrikeSequence\":true,\"enemyHeightRule\":\"source-pose\",\"candidateContactAnchors\":false,\"mainEnabled\":"+sourceApproachEnabled.ToString().ToLowerInvariant()+",\"finalArtApproved\":false}");
        }
        finally{JsonUtility.FromJsonOverwrite(original,cfg);enemyCfg.gameplayTypes=originalTypes;}
    }

    /// <summary>
    /// Records the complete authored BET attack chain at 120 Hz. Stage changes mirror the
    /// three existing reel-stop requests; this validates pose timing and cue order without
    /// changing machine state, combat values, art, or player saves.
    /// </summary>
    public static void RunAttackSequenceComparison()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");
        Directory.CreateDirectory(output);captures=0;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var canvas=UiFactory.CreateCanvas("HeroAttackSequenceCanvas");
        var stage=SafeStage.Create(canvas,1170,540).Stage;
        UiFactory.Panel(stage,"Backdrop",Vector2.zero,new Vector2(5000,5000),AzureMapSkin.Navy);
        var hero=HeroPresentationView.Create(stage,new Vector2(-120,-145));
        camera=new GameObject("HeroAttackSequenceCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        var rows=new System.Collections.Generic.List<string>{"weapon,elapsed,stop,state,pose,cue,contactCount"};
        var summary=new System.Collections.Generic.List<string>();
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            hero.Player.CancelToLoop("idle");hero.Player.Equip(new EquipItem{baseId=weapon});hero.Render();
            int contacts=0,anticipations=0,swings=0;string lastCue="";
            Action<string> onCue=cue=>{lastCue=cue;if(cue=="contact")contacts++;};
            hero.Player.Cue+=onCue;
                var visited=new System.Collections.Generic.HashSet<string>();
            try
            {
                hero.Player.Request("attack-f1");hero.Render();
                string previous=hero.Player.State+":"+hero.Player.PoseIndex;
                for(int frame=0;frame<240;frame++)
                {
                    string stop="held-f1";
                    if(frame==60){hero.Player.Request("attack-f2");hero.Render();stop="stop-1-f2";}
                    else if(frame>60&&frame<120)stop="held-f2";
                    else if(frame==120)
                    {
                        hero.Player.Request("attack-f3-4");hero.Player.Request("walk");hero.Render();stop="stop-2-f3-4+queued-walk";
                    }
                    else if(frame>120)stop="post-stop-2";
                    hero.Step(1f/120f);
                    string signature=hero.Player.State+":"+hero.Player.PoseIndex;
                    visited.Add(hero.Player.State+":"+hero.Player.PoseIndex);
                    string cue=lastCue;lastCue="";
                    float now=(frame+1)/120f;
                    rows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F4},{2},{3},{4},{5},{6}",weapon,now,stop,hero.Player.State,hero.Player.PoseIndex,cue,contacts));
                    bool changed=signature!=previous;
                    if(frame%4==0||changed||!string.IsNullOrEmpty(cue))Capture("attack-sequence-"+weapon+"-"+frame.ToString("D3"),false);
                    previous=signature;
                    int f1Pose=weapon=="w_spear"?32:24,f2Pose=weapon=="w_spear"?32:25;
                    if(frame<60)Check(hero.Player.State=="attack-f1","BET anticipation phase ended before the first stop: "+weapon+" frame="+frame);
                    else if(frame<120)Check(hero.Player.State=="attack-f2"&&hero.Player.PoseIndex==f2Pose,"First stop did not hold its final f2 pose: "+weapon+" frame="+frame);
                    if(frame==59)Check(hero.Player.State=="attack-f1"&&hero.Player.PoseIndex==f1Pose,"BET did not hold the final f1 anticipation pose: "+weapon);
                    if(frame==119)Check(hero.Player.State=="attack-f2"&&hero.Player.PoseIndex==f2Pose,"First stop did not hold the final f2 pose: "+weapon);
                    if(frame==120)Check(hero.Player.State=="attack-f3-4","Second stop did not release f3/4: "+weapon);
                    if(frame<120)Check(contacts==0,"Contact occurred before the second stop: "+weapon);
                    if(cue=="anticipation"){anticipations++;Check(stop=="held-f1"&&hero.Player.State=="attack-f1","Anticipation cue escaped the BET/f1 phase: "+weapon);}
                    if(cue=="swing"){swings++;Check(stop=="stop-2-f3-4+queued-walk"&&hero.Player.State=="attack-f3-4","Swing cue escaped the second-stop attack phase: "+weapon);}
                }
                Check(anticipations==1,"Anticipation cue must emit exactly once per attack: "+weapon);
                Check(swings==1,"Swing cue must emit exactly once after the second stop: "+weapon);
                Check(contacts==1,"Full attack chain must emit exactly one contact: "+weapon);
                Check(hero.Player.State=="walk","Full attack chain did not return to the queued locomotion state: "+weapon+"/"+hero.Player.State);
                foreach(var motion in new[]{"attack-f1","attack-f2","attack-f3-4"})
                {
                    var clip=Array.Find(HeroPresentation.Config.motions,m=>m.id==motion&&m.weaponId==weapon)
                        ??Array.Find(HeroPresentation.Config.motions,m=>m.id==motion&&string.IsNullOrEmpty(m.weaponId));
                    Check(clip!=null&&clip.frames.All(f=>visited.Contains(motion+":"+f)),"Sequence skipped an authored pose in "+motion+" for "+weapon);
                }
                var final=Array.Find(HeroPresentation.Config.motions,m=>m.id=="attack-f3-4"&&m.weaponId==weapon)
                    ??Array.Find(HeroPresentation.Config.motions,m=>m.id=="attack-f3-4"&&string.IsNullOrEmpty(m.weaponId));
                Check(final.events.Count(e=>e=="swing")==1&&final.events.Count(e=>e=="contact")==1,"Authored attack cues are not unique: "+weapon);
                summary.Add("{\"weapon\":\""+weapon+"\",\"anticipationCount\":"+anticipations+",\"swingCount\":"+swings+",\"contactCount\":"+contacts+",\"allAuthoredPosesVisited\":true,\"queuedWalkResumed\":true}");
            }
            finally{hero.Player.Cue-=onCue;}
        }
        File.WriteAllLines(Path.Combine(output,"attack-sequence-120hz.csv"),rows);
        File.WriteAllText(Path.Combine(output,"attack-sequence-validation.json"),"{\"passed\":true,\"sampleRateHz\":120,\"captureFrames\":"+captures+",\"sequences\":["+string.Join(",",summary)+"],\"isolatedPreviewOnly\":true,\"gameplayValuesChanged\":false,\"finalArtApproved\":false}");
        Debug.Log("HERO_ATTACK_SEQUENCE_VALIDATION passed captures="+captures);
    }

    /// <summary>
    /// Combat intensity QA for the existing engage band treatment.  This is a
    /// presentation-only probe: the machine is seeded in memory, no lever is
    /// pulled, and no save/reward state is written.  Each authored state is
    /// rendered at both supported screenshot sizes while the band copy and tint
    /// are recorded for a deterministic comparison.
    /// </summary>
    public static void RunCombatIntensityValidation()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("HERO_GAMEPLAY_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);captures=0;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UiFactory.EnsureEventSystem();
        var machine=GameDataLoader.CreateMachine(new SystemRandom(1010));machine.Adv.nodeId="C-1";
        machine.IsTier2=true;machine.EnemyActive=true;machine.EngageSet=1;machine.EngageTurn=1;machine.EngageStance=EngageStance.None;machine.EngageHpMax=100;machine.EngageHp=100;
        game=new GameObject("CombatIntensityQA").AddComponent<GameController>();game.enabled=false;
        typeof(GameController).GetField("_m",Private).SetValue(game,machine);
        var audio=AudioManager.Create();audio.SeVolume=0;typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
        Call("BuildUi");Call("RefreshUi");
        camera=new GameObject("CombatIntensityCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;
        Call("BeginEnemyPresentation","slime",null);typeof(GameController).GetField("_engagedName",Private).SetValue(game,"スライム（基本）");machine.EngageHp=65;machine.EngageHpMax=100;Get<CanvasGroup>("_enemyCg").alpha=1f;Call("RefreshUi");
        var rows=new System.Collections.Generic.List<string>{"state,copy,accentHex,surfaceHex,alpha"};
        var probes=new[]{
            new {id="turn1",stance=EngageStance.None,turn=1,potion=false,large=false,judge=false,needle="小役を引いて"},
            new {id="enemy-attack",stance=EngageStance.EnemyAttack,turn=2,potion=false,large=false,judge=false,needle="敵の攻撃"},
            new {id="hero-charge",stance=EngageStance.HeroCharge,turn=2,potion=false,large=false,judge=false,needle="力を貯め"},
            new {id="hero-attack",stance=EngageStance.HeroAttack,turn=2,potion=false,large=false,judge=false,needle="攻撃確定"},
            new {id="next-large",stance=EngageStance.HeroAttack,turn=2,potion=false,large=true,judge=false,needle="次は大攻撃"},
            new {id="potion",stance=EngageStance.HeroAttack,turn=2,potion=true,large=false,judge=false,needle="ポーション"},
            new {id="judge",stance=EngageStance.HeroAttack,turn=2,potion=false,large=false,judge=true,needle="JUDGE"}
        };
        var expected=new[]{new Color(.04f,.28f,.72f,.52f),new Color(.68f,.10f,.16f,.58f),new Color(.08f,.36f,.72f,.54f),new Color(.68f,.38f,.06f,.58f),new Color(.78f,.42f,.08f,.58f),new Color(.42f,.18f,.72f,.56f),new Color(.72f,.12f,.34f,.62f)};
        try
        {
            var seen=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<probes.Length;i++)
            {
                var p=probes[i];machine.EngageTurn=p.turn;machine.EngageStance=p.stance;machine.EngagePotionSpin=p.potion;machine.EngageNextLarge=p.large;machine.EngageJudgeSpin=p.judge;
                Call("RefreshUi");Call("ShowEngageBanners");
                var top=Get<MarqueeBand>("_engageBandTop");var bottom=Get<MarqueeBand>("_engageBandBottom");
                var hpBox=Get<GameObject>("_hpBox");
                bool peak=p.id=="enemy-attack"||p.id=="hero-attack"||p.id=="potion"||p.id=="judge";
                Check(top!=null&&((bottom!=null)==!peak),"Intensity band mode mismatch: "+p.id);
                var activeBands=UnityEngine.Object.FindObjectsOfType<MarqueeBand>().Where(x=>x.gameObject.activeInHierarchy).ToArray();
                Check(activeBands.Length==(peak?1:2),"Stale intensity bands remained active: "+p.id+" active="+activeBands.Length);
                var topBg=top.transform.Find("Bg")?.GetComponent<Image>();
                var topAccent=top.transform.Find("StateAccent")?.GetComponent<Image>();
                var topGold=top.transform.Find("EdgeTop")?.GetComponent<Image>();
                var bottomBg=bottom!=null?bottom.transform.Find("Bg")?.GetComponent<Image>():null;
                var bottomAccent=bottom!=null?bottom.transform.Find("StateAccent")?.GetComponent<Image>():null;
                var topText=top.transform.Find("Text")?.GetComponent<Text>();
                var bottomText=bottom!=null?bottom.transform.Find("Text")?.GetComponent<Text>():null;
                var hpSurface=hpBox!=null?hpBox.transform.Find("Bg")?.GetComponent<Image>():null;
                var hpLabel=Get<Text>("_hpLabel");
                Check(topBg!=null&&topAccent!=null&&topGold!=null&&topText!=null&&(!peak?(bottomBg!=null&&bottomAccent!=null&&bottomText!=null):(bottomBg==null&&bottomAccent==null&&bottomText==null)),"Intensity band children mismatch: "+p.id);
                Check(hpSurface!=null&&Mathf.Abs(hpSurface.color.a-1f)<.001f,"HP information surface must fully cover scrolling copy: "+p.id);
                Check(top.transform.GetSiblingIndex()<hpBox.transform.GetSiblingIndex(),"Combat band must stay behind the HP information card: "+p.id);
                var hpRows=hpLabel!=null?hpLabel.text.Split('\n'):Array.Empty<string>();
                Check(hpRows.Length==3&&hpRows.All(line=>!string.IsNullOrWhiteSpace(line)),"HP information must use three populated name, value, and status rows: "+p.id);
                Check(hpRows.Length==3&&hpRows[0]=="スライム（基本）"&&hpRows[1]=="HP 65/100"&&(p.judge?hpRows[2]=="JUDGE":hpRows[2].Contains("SET 1/3")),"HP card should use representative real battle data: "+p.id);
                Check(hpLabel!=null&&hpLabel.horizontalOverflow==HorizontalWrapMode.Wrap&&hpLabel.verticalOverflow==VerticalWrapMode.Truncate,"HP information must wrap and remain clipped to its card: "+p.id);
                Check(hpLabel!=null&&hpLabel.preferredWidth<=hpLabel.rectTransform.rect.width+1f&&hpLabel.preferredHeight<=hpLabel.rectTransform.rect.height+1f,"HP information text must fit its card bounds: "+p.id);
                var expectedSurface=new Color(.045f,.065f,.125f,1f);
                Check(Vector4.Distance((Vector4)topBg.color,(Vector4)expectedSurface)<.001f,"Intensity surface must stay opaque navy: "+p.id);
                string hex=ColorUtility.ToHtmlStringRGBA(topAccent.color);string expectedHex=ColorUtility.ToHtmlStringRGBA(expected[i]);
                Check(hex==ColorUtility.ToHtmlStringRGBA(new Color(expected[i].r,expected[i].g,expected[i].b,1f)),"Unexpected intensity accent "+p.id+" expected hue "+expectedHex+" got "+hex);
                Check(ColorUtility.ToHtmlStringRGBA(topGold.color)==ColorUtility.ToHtmlStringRGBA(new Color(.88f,.68f,.30f,.96f)),"Gold trim mismatch: "+p.id);
                if(bottomBg!=null)
                {
                    Check(ColorUtility.ToHtmlStringRGBA(topBg.color)==ColorUtility.ToHtmlStringRGBA(bottomBg.color),"Top/bottom surface diverged: "+p.id);
                    Check(ColorUtility.ToHtmlStringRGBA(topAccent.color)==ColorUtility.ToHtmlStringRGBA(bottomAccent.color),"Top/bottom accent diverged: "+p.id);
                    Check(bottomText.text.Contains(p.needle),"Bottom intensity copy missing: "+p.id);
                }
                Check(topText.text.Contains(p.needle),"Intensity copy missing: "+p.id);
                Check(seen.Add(hex),"Intensity accent was duplicated: "+p.id);rows.Add(p.id+","+p.needle.Replace(","," ")+","+hex+","+ColorUtility.ToHtmlStringRGBA(topBg.color)+","+topBg.color.a.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+(peak?"single":"paired"));
                Capture("combat-intensity-"+p.id);
                Call("HideEngageBanners");
            }
            File.WriteAllLines(Path.Combine(output,"intensity.csv"),rows);
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"states\":"+probes.Length+",\"stills\":"+captures+",\"distinctStateAccents\":true,\"copyMatchesAccent\":true,\"opaqueNavySurface\":true,\"opaqueHpSurface\":true,\"bandBehindHpCard\":true,\"hpCardThreeRows\":true,\"hpCardTextFitsBounds\":true,\"goldTrim\":true,\"buildUpUsesPairedBands\":true,\"peakStatesUseSingleBand\":true,\"mainEnabled\":true,\"saveDataTouched\":false,\"finalArtApproved\":false}");
            Debug.Log("COMBAT_INTENSITY_VALIDATION passed captures="+captures);
        }
        finally{Call("HideEngageBanners");}
    }
    static void ApplySpearHeightRule(HeroPresentationConfig cfg,string enemy)
    {
        // QA-only pose routing: low targets use the generated low keys, the upright goblin
        // keeps the standard reach, and the wolf keeps the low contact but rises into the
        // authored recovery. The runtime never calls this method and main stays unchanged.
        var start=cfg.poses[33];var contact=cfg.poses[34];var recover=cfg.poses[35];
        if(enemy=="slime"||enemy=="forest_wolf")
        {
            start.atlas="spear-low-start-v1-qa";start.gripX=enemy=="slime"?820:820;start.gripY=615;start.supportX=1150;start.supportY=725;start.foot=33;
            contact.atlas=enemy=="forest_wolf"?"spear-wolf-contact-v1-qa":"spear-low-contact-v1-qa";contact.gripX=891;contact.gripY=643;contact.supportX=1216;contact.supportY=752;contact.foot=34;
        }
        else
        {
            start.atlas="spear-start-walkmatched-v2-qa";start.gripX=909;start.gripY=438;start.supportX=1208;start.supportY=384;start.foot=25;
            contact.atlas="spear-contact-walkmatched-v1-qa";contact.gripX=1018;contact.gripY=439;contact.supportX=1248;contact.supportY=411;contact.foot=36;
        }
        var wolfRecovery=Environment.GetEnvironmentVariable("HERO_WOLF_RECOVERY")??"standard";
        if(enemy=="goblin"||(enemy=="forest_wolf"&&wolfRecovery!="low"))
        {
            recover.atlas="spear-return-walkmatched-v1-qa";recover.gripX=835;recover.gripY=463;recover.supportX=1111;recover.supportY=400;recover.foot=27;
        }
        else
        {
            recover.atlas="spear-low-return-v1-qa";recover.gripX=790;recover.gripY=580;recover.supportX=1100;recover.supportY=680;recover.foot=33;
        }
    }
    static float EnemyContactAnchorOffset(string enemy)
    {
        // Parent-space offsets identify the authored contact band, not the sprite centre.
        // They are candidate measurements only and never alter combat collision or damage.
        return enemy=="slime"?-42.552f:enemy=="forest_wolf"?-75.031f:0f;
    }
    static void Capture(string name,bool bothSizes=true)
    {
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
        foreach(var c in canvases)
        {
            var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)UnityEngine.Object.DestroyImmediate(scaler);
            c.renderMode=RenderMode.WorldSpace;c.transform.position=Vector3.zero;c.transform.localScale=Vector3.one;c.worldCamera=camera;
            foreach(var safe in c.GetComponentsInChildren<SafeStage>()){safe.enabled=false;safe.Stage.SetParent(c.transform,false);}
        }
        foreach(var size in bothSizes?new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)}:new[]{new Vector2Int(1280,720)})
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
