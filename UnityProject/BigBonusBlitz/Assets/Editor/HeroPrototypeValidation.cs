using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HeroPrototypeValidation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Use isolated work/shop-qa");
        string output=Environment.GetEnvironmentVariable("HERO_PROTOTYPE_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cfg=HeroPresentation.Config;
        foreach(var m in cfg.motions){Check(m.frames.Length>0&&m.frames.Length==m.seconds.Length,"Motion durations: "+m.id);foreach(var d in m.seconds)Check(d>0,"Positive durations");foreach(var f in m.frames)Check(f>=0&&f<cfg.poses.Length,"Pose index");}
        foreach(var p in cfg.poses)Check(HeroPresentation.Sprite(p.atlas,p.x,p.y,p.w,p.h)!=null,"Body texture");
        foreach(var sound in cfg.sounds)
        {
            Check(sound.gain>=0&&sound.gain<=1,"Preview sound gain");
            Check(Enum.TryParse<MotionCue>(sound.fallback,out _),"Unknown sound fallback");
            if(!string.IsNullOrEmpty(sound.key))Check(AudioManager.SeDefs.Any(s=>s.key==sound.key),"Unknown SE key");
        }
        Check(HeroPresentation.Sound("enemy","contact")==null&&HeroPresentation.Sound("hero","hurt")!=null,"Enemy contact would double-play hit audio");
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            var cueClock=new HeroMotionPlayer(cfg);cueClock.Equip(new EquipItem{baseId=weapon});
            int swings=0,guards=0,magic=0;cueClock.Cue+=c=>{if(c=="swing")swings++;if(c=="guard")guards++;if(c=="magic")magic++;};
            cueClock.Request("attack-f3-4");cueClock.Tick(0);cueClock.Tick(float.NaN);Check(swings==0,"Invalid delta emitted cue");
            cueClock.Tick(.001f);cueClock.Tick(.001f);Check(swings==1,"First frame cue missing or duplicated");
            cueClock.Tick(2);Check(swings==1,"Recovery replayed swing");
            cueClock.Request("slash");cueClock.Request("hit");cueClock.Tick(2);Check(swings==1,"Cancelled first frame fired late");
            cueClock.Request("focus");cueClock.Tick(.7f);int focusPose=cueClock.PoseIndex;cueClock.Request("focus");Check(cueClock.PoseIndex==focusPose,"Focus restarted breath");
            cueClock.Request("guard");cueClock.Tick(2);Check(guards==1&&cueClock.State=="focus","Guard did not return to focus");
            cueClock.Request("cast");cueClock.Tick(2);Check(magic==1&&cueClock.State=="focus","Cast did not return to focus");
            cueClock.Request("slash");cueClock.Tick(2);Check(swings==2&&cueClock.State=="focus","Slash alias lost weapon-specific motion");
        }
        var player=new HeroMotionPlayer(cfg);int contact=0;player.Cue+=c=>{if(c=="contact")contact++;};
        player.Request("walk");player.Tick(.2f);int pose=player.PoseIndex;player.Request("walk");Check(player.PoseIndex==pose,"Repeated walk restarted");
        player.Request("attack");player.Equip(new EquipItem{baseId="w_spear"});player.Request("walk");Check(player.State=="attack"&&player.WeaponId=="w_blade","Attack lost its starting weapon");
        player.Tick(2);Check(contact==1&&player.State=="walk"&&player.WeaponId=="w_spear","Low FPS recovery or contact event");
        player.Equip(new EquipItem{baseId="unrecognized",craftId="old_save_item"});Check(player.WeaponId=="w_blade","Old save fallback");
        player.Request("attack");player.Request("hit");player.Tick(1);Check(player.State=="walk","Hit did not recover");
        player.Request("idle");player.Equip(new EquipItem{baseId="w_spear"});
        Check(cfg.poses[player.PoseIndex].twoHanded,"Spear did not select its own stance");
        player.Request("attack");player.Tick(.3f);Check(cfg.poses[player.PoseIndex].twoHanded,"Spear used sword attack");
        player.Equip(new EquipItem{baseId="w_blade"});player.Tick(1);
        Check(player.WeaponId=="w_blade"&&!cfg.poses[player.PoseIndex].twoHanded,"Recovery used previous weapon stance");
        player.Request("walk");player.Tick(.25f);pose=player.PoseIndex;player.Equip(new EquipItem{baseId="w_spear"});Check(player.PoseIndex==pose,"Equipping restarted walk phase");
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            var held=new HeroMotionPlayer(cfg);held.Equip(new EquipItem{baseId=weapon});var reels=new HeroReelPreview(held);int strikes=0;held.Cue+=c=>{if(c=="contact")strikes++;};
            reels.Begin(true);held.Request("walk");held.Tick(30);Check(held.State=="attack-f1"&&strikes==0,"BET stance did not hold");
            reels.Stop(1);held.Tick(30);Check(held.State=="attack-f2"&&strikes==0,"First stop released attack");
            held.Equip(new EquipItem{baseId=weapon=="w_blade"?"w_spear":"w_blade"});Check(held.WeaponId==weapon&&held.PendingWeapon!=null,"Held phase changed weapon early");
            reels.Stop(2);held.Tick(2);Check(strikes==1&&held.State=="walk"&&held.WeaponId!=weapon,"Second stop or weapon recovery failed");
            reels.Stop(2);reels.Stop(3);held.Tick(2);Check(strikes==1&&!reels.Active,"Duplicate/third stop repeated attack");
            reels.Begin(true);reels.Cancel();held.Tick(2);Check(held.State=="idle"&&strikes==1,"Cancel emitted late contact");
            reels.Begin(false);reels.Stop(3);held.Tick(2);Check(held.State=="walk"&&strikes==1,"Non-bell played attack");
        }
        foreach(var enemy in EnemyPresentation.Config.enemies)
        {
            foreach(var m in enemy.motions)
            {
                Check(m.frames.Length==m.seconds.Length&&m.events.Length==m.frames.Length&&m.phases.Length==m.frames.Length&&m.eventAt.Length==m.frames.Length,"Enemy clip data "+enemy.id);
                foreach(float fraction in m.eventAt)Check(fraction>=0&&fraction<=1,"Enemy cue time");
                foreach(int f in m.frames)Check(f>=0&&f<enemy.poses.Length,"Enemy pose index");
                foreach(float d in m.seconds)Check(d>0,"Enemy positive duration");
            }
            var clock=new EnemyMotionPlayer(enemy);int strikes=0;clock.Cue+=c=>{if(c=="contact")strikes++;};
            clock.Request("attack");clock.Tick(.05f);float progress=clock.Progress;clock.Request("attack");Check(clock.Progress==progress,"Enemy repeated request restarted windup");
            var attackClock=enemy.motions.First(m=>m.id=="attack");
            clock.Tick(attackClock.seconds[0]+attackClock.seconds[1]+attackClock.seconds[2]*.5f-.05f);
            Check(strikes==0,"Enemy contact fired before lunge approached target");
            clock.Tick(3);Check(strikes==1&&clock.State=="idle","Enemy low FPS attack or recovery");
            clock.Request("attack");clock.Request("hit");clock.Tick(3);Check(strikes==1&&clock.State=="idle","Interrupted enemy struck late");
            clock.Request("defeat");clock.Tick(20);clock.Request("attack");Check(clock.State=="defeat"&&clock.PoseIndex==7,"Defeated enemy revived");
            clock.Request("idle");Check(clock.State=="idle"&&clock.Offset.sqrMagnitude<.01f,"Explicit preview reset failed");
        }
        var lab=HeroPresentationLab.Open();
        Check(HeroPresentationLab.Open()==lab,"Opening the preview twice duplicated the surface");
        Check((lab.gameObject.hideFlags&HideFlags.DontSave)==HideFlags.DontSave,"Preview can be serialized");
        Check(lab.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>()!=null,"Preview leaked its new EventSystem into the user scene");
        var canvas=lab.GetComponentInChildren<Canvas>();UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        var safe=canvas.GetComponentInChildren<SafeStage>();safe.enabled=false;lab.Stage.SetParent(canvas.transform,false);
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        var camera=new GameObject("HeroReviewCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;canvas.worldCamera=camera;
        int captures=0;
        void Capture(string name,int width=1280,int height=720)
        {
            float stageWidth=540f*width/height;((RectTransform)canvas.transform).sizeDelta=new Vector2(stageWidth,540);lab.Stage.localScale=Vector3.one*Mathf.Min(1,stageWidth/1170f);
            Canvas.ForceUpdateCanvases();
            foreach(var t in canvas.GetComponentsInChildren<Text>())t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
            for(int k=0;k<2;k++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
            var rt=RenderTexture.GetTemporary(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var img=new Texture2D(width,height,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,width,height),0,0);img.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);captures++;
        }
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            lab.Request("idle");lab.Tick(3);lab.Equip(weapon);
            foreach(string motion in new[]{"idle","walk","attack","hit","victory"})
            {
                float sampleTime=.02f;
                if(motion=="attack")
                {
                    var attack=cfg.motions.FirstOrDefault(m=>m.id==motion&&m.weaponId==weapon)
                        ??cfg.motions.First(m=>m.id==motion&&string.IsNullOrEmpty(m.weaponId));
                    int contactFrame=Array.IndexOf(attack.events,"contact");Check(contactFrame>=0,"Attack lacks contact cue");
                    sampleTime=attack.seconds.Take(contactFrame).Sum()+attack.seconds[contactFrame]*.5f;
                }
                lab.Request(motion);lab.Tick(sampleTime);Check(lab.Hero.Player.State==motion,"Capture missed requested motion");
                Check(lab.Hero.Weapon.rectTransform.anchoredPosition==lab.Hero.GripPosition,"Grip separated");
                if(weapon=="w_spear"&&(motion=="idle"||motion=="attack"))
                {
                    Check(lab.Hero.TwoHanded,"Missing support hand");
                    var along=(Vector2)(lab.Hero.Weapon.rectTransform.localRotation*Vector3.up);
                    var support=lab.Hero.SupportPosition-lab.Hero.GripPosition;
                    Check(Vector2.Dot(along,support)>0&&Mathf.Abs(along.x*support.y-along.y*support.x)<.05f,"Support hand missed rigid shaft axis");
                }
                foreach(int width in new[]{1280,2556})Capture(weapon+"-"+motion+"-"+width,width,width==1280?720:1179);
                lab.Tick(3);
            }
        }
        for(int i=0;i<3;i++){lab.SetEnemy(i);Capture("enemy-"+i);}
        lab.Request("idle");lab.Tick(3);lab.Equip("w_blade");lab.Request("attack");lab.Equip("w_spear");Check(lab.Hero.Player.PendingWeapon=="w_spear","No pending equipment");Capture("weapon-pending");lab.Tick(1);Check(lab.Hero.Player.WeaponId=="w_spear","Pending equipment never committed");Capture("weapon-applied");
        string movie=Path.Combine(output,"frames");Directory.CreateDirectory(movie);
        lab.Request("idle");lab.Tick(3);lab.Equip("w_blade");lab.SetEnemy(1);
        for(int i=0;i<144;i++)
        {
            if(i==0)lab.Request("walk");if(i==30)lab.Request("attack");if(i==50)lab.Request("hit");if(i==72){lab.Equip("w_spear");lab.SetEnemy(2);lab.Request("walk");}if(i==100)lab.Request("attack");if(i==120)lab.Request("victory");
            lab.Tick(1f/12);Capture("frames/"+i.ToString("D4"),960,540);
        }
        for(int i=0;i<3;i++)
        {
            lab.Request("idle");lab.Tick(3);lab.SetEnemy(i);
            var attack=lab.Enemy.Look.motions.First(m=>m.id=="attack");
            lab.Stage.Find("EnemyAttack").GetComponent<Button>().onClick.Invoke();Check(lab.Enemy.Player.State=="attack","Enemy attack button disconnected");
            lab.Tick(attack.seconds[0]*.5f);
            foreach(int width in new[]{1280,2556})Capture("enemy-"+i+"-windup-"+width,width,width==1280?720:1179);
            lab.Tick(attack.seconds[0]*.5f+attack.seconds[1]+attack.seconds[2]*.9f);
            Check(lab.Enemy.Player.Phase=="攻撃"&&lab.Hero.Player.State=="hit","Enemy contact reaction missing");
            foreach(int width in new[]{1280,2556})Capture("enemy-"+i+"-contact-"+width,width,width==1280?720:1179);
            lab.Tick(3);lab.RequestEnemy("hit");lab.Tick(.05f);
            foreach(int width in new[]{1280,2556})Capture("enemy-"+i+"-hit-"+width,width,width==1280?720:1179);
            lab.RequestEnemy("defeat");lab.Tick(1);
            foreach(int width in new[]{1280,2556})Capture("enemy-"+i+"-defeat-"+width,width,width==1280?720:1179);
        }
        lab.SetEnemy(0);lab.RequestEnemy("attack");lab.Paused=true;int frozenPose=lab.Enemy.Player.PoseIndex;lab.Tick(2);Check(lab.Enemy.Player.PoseIndex==frozenPose,"Pause failed for enemy");lab.Paused=false;
        Directory.CreateDirectory(Path.Combine(output,"enemy-frames"));
        for(int i=0;i<288;i++)
        {
            int beat=i%96;
            if(beat==0){lab.Request("idle");lab.Hero.Step(3);lab.SetEnemy(i/96);}
            if(beat==12)lab.RequestEnemy("attack");if(beat==40)lab.Request("attack");if(beat==72)lab.RequestEnemy("defeat");if(beat==84)lab.Request("victory");
            lab.Tick(1f/12);Capture("enemy-frames/"+i.ToString("D4"),960,540);
        }
        lab.Reels.Begin(true);lab.Tick(2);lab.ShowReelControls(false);
        Check(lab.Hero.Player.State=="idle"&&!lab.Reels.Active,"Mode switch left a held pose");
        lab.Reels.Begin(true);lab.Request("idle");Check(lab.Hero.Player.State=="idle","Manual idle could not exit held pose");
        lab.ShowReelControls(true);
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            lab.Reels.Cancel();lab.Equip(weapon);lab.SetEnemy(1);
            lab.Stage.Find("ReelBet").GetComponent<Button>().onClick.Invoke();lab.Tick(2);
            Check(lab.Hero.Player.State=="attack-f1","BET preview button disconnected");
            foreach(int width in new[]{1280,2556})Capture("reel-"+weapon+"-bet-"+width,width,width==1280?720:1179);
            lab.Stage.Find("ReelStop1").GetComponent<Button>().onClick.Invoke();lab.Tick(2);
            foreach(int width in new[]{1280,2556})Capture("reel-"+weapon+"-first-"+width,width,width==1280?720:1179);
            lab.Stage.Find("ReelStop2").GetComponent<Button>().onClick.Invoke();lab.Tick(.1f);
            Check(lab.Hero.Player.State=="attack-f3-4","Second stop did not release");
            foreach(int width in new[]{1280,2556})Capture("reel-"+weapon+"-second-"+width,width,width==1280?720:1179);
        }
        Directory.CreateDirectory(Path.Combine(output,"reel-frames"));
        for(int i=0;i<144;i++)
        {
            int beat=i%72;
            if(beat==0){lab.Reels.Cancel();lab.Equip(i<72?"w_blade":"w_spear");lab.SetEnemy(1);lab.Reels.Begin(true);}
            if(beat==18)lab.Reels.Stop(1);if(beat==36)lab.Reels.Stop(2);if(beat==40)lab.Reels.Stop(3);
            lab.Tick(1f/12);Capture("reel-frames/"+i.ToString("D4"),960,540);
        }
        lab.ShowSupportControls();
        foreach(string weapon in new[]{"w_blade","w_spear"})
        {
            lab.Hero.Player.CancelToLoop();lab.Equip(weapon);lab.SetEnemy(1);
            foreach(string motion in new[]{"focus","guard","cast"})
            {
                lab.Stage.Find("Support_"+motion).GetComponent<Button>().onClick.Invoke();
                Check(lab.Hero.Player.State==motion,"Support button disconnected");
                for(int frame=0;frame<2;frame++)
                {
                    lab.Tick(frame==0?.01f:motion=="focus"?.65f:motion=="guard"?.18f:.25f);
                    Check(cfg.poses[lab.Hero.Player.PoseIndex].atlas=="support-v1","Support capture missed pose");
                    foreach(int width in new[]{1280,2556})Capture("support-"+weapon+"-"+motion+"-"+frame+"-"+width,width,width==1280?720:1179);
                }
                lab.Tick(2);
            }
        }
        Directory.CreateDirectory(Path.Combine(output,"support-frames"));
        for(int i=0;i<192;i++)
        {
            int beat=i%96;
            if(beat==0){lab.Hero.Player.CancelToLoop();lab.Equip(i<96?"w_blade":"w_spear");lab.SetEnemy(1);lab.Request("walk");}
            if(beat==16)lab.Request("focus");if(beat==36)lab.Request("guard");if(beat==50)lab.Request("cast");if(beat==66)lab.Request("slash");if(beat==84)lab.Request("idle");
            lab.Tick(1f/12);Capture("support-frames/"+i.ToString("D4"),960,540);
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"stills\":85,\"supportMovieFrames\":192,\"firstFrameCueOnce\":true,\"interruptedFirstCueCancelled\":true,\"supportReturnToFocus\":true,\"soundRoutingData\":true,\"audioListeningVerified\":false,\"reelMovieFrames\":144,\"heldReelPhases\":true,\"secondStopContact\":true,\"cancelNoLateContact\":true,\"nonBellWalk\":true,\"movieFrames\":144,\"enemyMovieFrames\":288,\"enemyPoses\":24,\"enemyClips\":12,\"enemyContactOnce\":true,\"enemyInterruptCancelsContact\":true,\"enemyDefeatHolds\":true,\"enemyPause\":true,\"poses\":"+cfg.poses.Length+",\"motions\":13,\"clipsIncludingWeaponVariants\":"+cfg.motions.Length+",\"weapons\":2,\"enemyPreviews\":3,\"loopPreservesPhase\":true,\"pendingWeapon\":true,\"lowFpsContactOnce\":true,\"unknownWeaponFallback\":true,\"hitRecovery\":true,\"twoHandedSpearAxis\":true,\"weaponSpecificRecovery\":true,\"equipmentPreservesWalkPhase\":true,\"productionIntegrated\":false}");
        Debug.Log("HERO_PROTOTYPE_VALIDATION passed captures="+captures);
    }
}
