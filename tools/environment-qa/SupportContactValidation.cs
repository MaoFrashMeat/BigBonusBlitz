using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SupportContactValidation
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated QA required");
        string output=Environment.GetEnvironmentVariable("SUPPORT_CONTACT_OUTPUT");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
        var canvas=UiFactory.CreateCanvas("SupportContactQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(960,400);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.08f);canvas.worldCamera=camera;
        var stage=UiSkin.Rect(canvas.transform,"Stage",Vector2.zero,new Vector2(960,400));
        var host=UiSkin.Rect(stage,"Hero",new Vector2(-300,0),new Vector2(200,240));host.gameObject.AddComponent<Image>();
        var hero=AdventureHeroPresenter.Attach(host);
        var enemyHost=UiSkin.Rect(stage,"Enemy",new Vector2(150,-60),new Vector2(150,120));enemyHost.gameObject.AddComponent<Image>();
        var enemy=AdventureEnemyPresenter.Attach(enemyHost,-60);
        Action<string> render=name=>{
            Canvas.ForceUpdateCanvases();foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1440,600,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1440,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,600),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
        };
        int cases=0;var trace=new System.Collections.Generic.List<string>{"weapon,enemy,frame,state,pose,low,tipX,tipY,targetX,targetY"};
        foreach(string weapon in new[]{"w_blade","w_spear"})foreach(string id in new[]{"slime","beetle","goblin"})
        {
            hero.Cancel();hero.Equip(new EquipItem{baseId=weapon,slot=EquipSlot.Weapon});enemy.Show(id,1,400);enemy.Step(.001f);
            int contacts=0;Action<string> cue=s=>{if(s=="contact")contacts++;};hero.View.Player.Cue+=cue;
            hero.PrepareAttackTarget(enemy.View.Body.rectTransform);hero.Request("attack",false);hero.Request("walk",false);
            bool sawLow=false;
            for(int frame=0;frame<180;frame++)
            {
                hero.Step(1f/120);int pose=hero.View.RenderedPoseIndex;sawLow|=pose>=75;
                var tip=hero.View.WeaponTipWorld(pose,weapon);var target=enemy.View.Body.rectTransform.TransformPoint(enemy.View.Body.rectTransform.rect.center);
                trace.Add(string.Join(",",weapon,id,frame,hero.View.Player.State,pose,hero.View.LowTarget,tip.x,tip.y,target.x,target.y));
                if(frame%4==0&&frame<132)render(weapon+"-"+id+"-"+frame.ToString("D3"));
            }
            Check(contacts==1,"Contact duplicated "+weapon+id);Check(Mathf.Abs(hero.Advance)<.01f&&!hero.View.LowTarget,"Low pose leaked into locomotion");
            if(id=="slime")Check(sawLow,"Short enemy did not select low attack");
            hero.View.Player.Cue-=cue;cases++;
        }
        hero.Cancel();hero.Equip(new EquipItem{baseId="w_spear",slot=EquipSlot.Weapon});
        var walk=HeroPresentation.Config.motions.First(m=>m.id=="walk"&&m.weaponId=="w_spear");
        foreach(int pose in walk.frames){hero.View.RenderPose(pose);Check(hero.View.TwoHanded,"Spear lost support hand");render("spear-walk-"+pose);}
        File.WriteAllLines(Path.Combine(output,"contacts.csv"),trace);
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"contactCases\":"+cases+",\"spearWalkFrames\":8,\"productionSaveTouched\":false,\"finalArtApproved\":false}");
    }
}
