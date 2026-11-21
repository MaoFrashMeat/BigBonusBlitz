using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlantedContactValidation
{
    [Serializable] sealed class Case
    {
        public string weapon,motion;
        public int fps,contacts;
        public float recoil,maxRootDrift,restError;
        public bool passed;
    }
    [Serializable] sealed class Report
    {
        public bool passed,productionSaveTouched=false,finalArtApproved=false;
        public List<Case> cases=new List<Case>();
    }
    public static void Run()
    {
        if(!Application.isBatchMode||Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))throw new Exception("Isolated QA required");
        string output=Environment.GetEnvironmentVariable("PLANTED_CONTACT_OUTPUT");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
        var canvas=UiFactory.CreateCanvas("PlantedContactQA");UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode=RenderMode.WorldSpace;canvas.transform.position=Vector3.zero;canvas.transform.localScale=Vector3.one;
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(960,400);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.08f);canvas.worldCamera=camera;
        var host=UiSkin.Rect(canvas.transform,"Hero",new Vector2(-300,0),new Vector2(200,240));host.gameObject.AddComponent<Image>();
        var hero=AdventureHeroPresenter.Attach(host);
        var enemyHost=UiSkin.Rect(canvas.transform,"Enemy",new Vector2(150,-60),new Vector2(150,120));enemyHost.gameObject.AddComponent<Image>();
        var enemy=AdventureEnemyPresenter.Attach(enemyHost,-60);
        Action<string> render=name=>{
            Canvas.ForceUpdateCanvases();foreach(var g in canvas.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1440,600,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1440,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,600),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
        };
        var report=new Report();
        foreach(var weapon in HeroPresentation.Config.weapons.Where(w=>!w.ranged))
        foreach(string motion in new[]{"attack","attack-f3-4"})
        foreach(int fps in new[]{15,60,120})foreach(float recoil in new[]{-48f,48f})
        {
            hero.Cancel();hero.Equip(new EquipItem{baseId=weapon.id,slot=EquipSlot.Weapon});enemy.Show("slime",1,400);enemy.Step(.001f);
            var target=enemy.View.Body.rectTransform;
            var result=new Case{weapon=weapon.id,motion=motion,fps=fps,recoil=recoil};
            Vector2 planted=Vector2.zero;bool moved=false;
            Action<string> cue=s=>{if(s=="contact")result.contacts++;};hero.View.Player.Cue+=cue;
            hero.PrepareAttackTarget(target);hero.Request(motion,false);hero.Request("walk",false);
            bool capture=fps==60&&recoil>0&&motion=="attack"&&(weapon.id=="w_blade"||weapon.id=="w_spear");
            for(int i=0;i<fps*3;i++)
            {
                hero.Step(1f/fps);
                if(result.contacts>0&&!moved)
                {
                    planted=hero.View.Root.anchoredPosition;
                    if(capture)render(weapon.id+"-contact");
                    target.anchoredPosition+=Vector2.right*recoil;moved=true;
                }
                else if(moved&&hero.View.Player.State==motion)
                {
                    result.maxRootDrift=Mathf.Max(result.maxRootDrift,Vector2.Distance(planted,hero.View.Root.anchoredPosition));
                    if(capture){render(weapon.id+"-recoil");capture=false;}
                }
            }
            result.restError=Mathf.Abs(hero.Advance);
            result.passed=result.contacts==1&&result.maxRootDrift<.01f&&result.restError<.01f&&!hero.View.LowTarget;
            hero.View.Player.Cue-=cue;report.cases.Add(result);
        }
        report.passed=report.cases.All(c=>c.passed);
        File.WriteAllText(Path.Combine(output,"validation.json"),JsonUtility.ToJson(report,true));
        if(!report.passed)throw new Exception("Contact recoil drags the planted hero; see validation.json");
    }
}
