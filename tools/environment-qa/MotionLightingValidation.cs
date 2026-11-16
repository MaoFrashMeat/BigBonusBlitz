using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MotionLightingValidation
{
    static Camera camera;static Canvas canvas;static string output;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static Color32[] Render(string name,int width=1280,int height=720)
    {
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(camera.orthographicSize*2*width/height,camera.orthographicSize*2);
        Canvas.ForceUpdateCanvases();
        for(int pass=0;pass<3;pass++){foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
        var rt=RenderTexture.GetTemporary(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        var pixels=image.GetPixels32();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);return pixels;
    }
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        output=Environment.GetEnvironmentVariable("MOTION_LIGHTING_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
        canvas=UiFactory.CreateCanvas("MotionLightingQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.05f);canvas.worldCamera=camera;
        var area=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(960,320));area.gameObject.AddComponent<RectMask2D>();
        var bg=ParallaxBackground.Create(area);bg.AutoCycle=false;
        var host=UiSkin.Rect(area,"Hero",new Vector2(-300,-12),new Vector2(200,240));host.gameObject.AddComponent<Image>();
        var hero=AdventureHeroPresenter.Attach(host);hero.Environment=bg;
        var target=UiSkin.Rect(area,"Target",new Vector2(170,-36),new Vector2(100,120));
        int lightConditions=0;
        foreach(var id in new[]{"A-1","E-2","H-1"})
        {
            bg.SetStage(id,true);
            foreach(var hour in new[]{7f,12f,18f,23f})foreach(var weather in new[]{AdventureWeather.Clear,AdventureWeather.Cloudy})
            {
                bg.SetHour(hour,0);bg.SetWeather(weather,1,0);bg.Preview(7);hero.Step(.001f);
                Render(id+"-"+hour+"-"+weather);lightConditions++;
                Check(hero.View.Body.color==bg.ActorTint,"Hero ignored environment");
            }
        }
        bg.SetStage("A-1",true);bg.SetWeather(AdventureWeather.Clear,1,0);bg.SetHour(7,0);
        Check(bg.ContactShadowOffset.x>0,"Morning shadow direction");float clearAlpha=bg.ContactShadowColor.a;
        bg.SetHour(17,0);Check(bg.ContactShadowOffset.x<0,"Evening shadow direction");
        bg.SetHour(12,0);float day=bg.ActorTint.r;bg.SetHour(23,0);Check(bg.ActorTint.r<day,"Night actor tint");
        bg.SetWeather(AdventureWeather.Cloudy,1,0);Check(bg.ContactShadowColor.a<clearAlpha,"Overcast contact shadow");
        int changedPixels=0;
        foreach(var id in new[]{"E-2","H-1"})
        {
            bg.SetStage(id,true);bg.SetHour(23,0);bg.Preview(7);hero.Step(.001f);
            var on=Render(id+"-lights-on");
            var lamps=bg.Profile.layers.Select(l=>l.lights).ToArray();foreach(var l in bg.Profile.layers)l.lights=null;
            bg.SetStage(id,true);bg.SetHour(23,0);bg.Preview(7);var off=Render(id+"-lights-off");
            int difference=0;for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)+Math.Abs(on[i].g-off[i].g)+Math.Abs(on[i].b-off[i].b)>6)difference++;
            Check(difference>100,"Local light has no rendered effect: "+id);changedPixels+=difference;
            for(int i=0;i<lamps.Length;i++)bg.Profile.layers[i].lights=lamps[i];bg.Preview(7);
        }
        bg.SetStage("A-1",true);bg.SetHour(12,0);bg.SetWeather(AdventureWeather.Clear,0,0);bg.Preview(7);
        int families=0;var trace=new List<string>{"weapon,frame,state,pose,advance,returning,contact"};
        foreach(var weapon in HeroPresentation.Config.weapons)
        {
            hero.Cancel();hero.Equip(new EquipItem{baseId=weapon.id,slot=EquipSlot.Weapon});hero.Request("walk",false);
            int contacts=0;Action<string> cue=s=>{if(s=="contact")contacts++;};hero.View.Player.Cue+=cue;
            hero.PrepareAttackTarget(target);hero.Request("attack",false);hero.Request("walk",false);
            var poses=new HashSet<int>();float previous=float.MaxValue;
            for(int frame=0;frame<360;frame++)
            {
                hero.Step(1f/120);
                if(hero.IsReturning){poses.Add(hero.View.RenderedPoseIndex);Check(hero.Advance<=previous+.001f,"Retreat moved forward");previous=hero.Advance;}
                var pose=HeroPresentation.Config.poses[hero.View.RenderedPoseIndex];
                float sole=hero.View.Body.rectTransform.anchoredPosition.y-hero.View.Body.rectTransform.sizeDelta.y*.5f+pose.foot*HeroPresentation.Config.height/pose.height;
                Check(Mathf.Abs(sole)<.01f,"Foot baseline drift");
                trace.Add(weapon.id+","+frame+","+hero.View.Player.State+","+hero.View.RenderedPoseIndex+","+hero.Advance.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+hero.IsReturning+","+contacts);
                if(frame%3==0&&frame<270)Render(weapon.id+"-motion-"+frame.ToString("D3"),960,540);
            }
            Check(contacts==1,"Attack contact count "+weapon.id);Check(hero.View.Player.State=="walk"&&Mathf.Abs(hero.Advance)<.01f,"Failed walk return "+weapon.id);
            if(!weapon.ranged)Check(poses.Count>=3,"Retreat retained fixed pose "+weapon.id);
            hero.View.Player.Cue-=cue;families++;
            // Large delta and cancellation must not leave spatial offsets or repeat contact.
            hero.PrepareAttackTarget(target);hero.Request("attack",false);hero.Step(5);hero.Cancel();Check(Mathf.Abs(hero.Advance)<.001f&&!hero.IsReturning&&!hero.IsProjectileFlying,"Cancellation residue");
            foreach(var reaction in new[]{"hit","guard"})
            {
                // Interrupt an approach: reaction recovery must keep its authored duration.
                hero.PrepareAttackTarget(target);hero.Request("attack",false);hero.Step(.45f);
                hero.Request(reaction,false);hero.Request("walk",false);
                Func<string,HeroMotion> clip=id=>Array.Find(HeroPresentation.Config.motions,m=>m.id==id&&m.weaponId==weapon.id)
                    ??Array.Find(HeroPresentation.Config.motions,m=>m.id==id&&string.IsNullOrEmpty(m.weaponId));
                float original=clip(reaction).seconds.Sum()+clip("recover").seconds.Sum();
                hero.Step(original+.001f);
                Check(hero.View.Player.State=="walk"&&!hero.IsReturning&&Mathf.Abs(hero.Advance)<.001f,"Retreat timing leaked into "+reaction+" "+weapon.id);
            }

        }
        File.WriteAllLines(Path.Combine(output,"motion.csv"),trace);
        var enemyHost=UiSkin.Rect(area,"Enemy",new Vector2(150,-72),new Vector2(150,120));var legacy=enemyHost.gameObject.AddComponent<Image>();
        var enemy=AdventureEnemyPresenter.Attach(enemyHost,-60);enemy.View.Environment=bg;int enemies=0;
        bg.SetHour(23,0);hero.Step(.001f);
        foreach(var look in EnemyPresentation.Config.enemies)
        {
            enemy.Show(look.id,1,400);Check(enemy.Active,"Enemy not connected "+look.id);
            Check(enemy.View.Body.color==bg.ActorTint,"First-frame enemy ambient mismatch "+look.id);enemy.Step(.01f);
            Check(enemy.View.Body.color==bg.ActorTint,"Enemy ambient mismatch "+look.id);
            Render("enemy-night-"+look.id);
            legacy.color=new Color(1,1,1,.35f);enemy.Step(.01f);Check(Mathf.Abs(enemy.View.Body.color.a-.35f)<.001f,"Ambient erased defeat alpha");legacy.color=Color.white;
            enemy.Request("attack");enemy.Step(.1f);enemy.Hide();enemies++;
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"weaponFamilies\":"+families+",\"enemyAmbientCases\":"+enemies+",\"lightingConditions\":"+lightConditions+",\"localLightChangedPixels\":"+changedPixels+",\"contactOnce\":true,\"retreatGait\":true,\"reactionTimingCases\":14,\"firstFrameEnemyTint\":true,\"footBaseline\":true,\"finalArtApproved\":false,\"productionSaveTouched\":false}");
        Debug.Log("MOTION_LIGHTING_VALIDATION passed");
    }
}
