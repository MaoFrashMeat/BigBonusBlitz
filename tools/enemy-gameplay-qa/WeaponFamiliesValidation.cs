using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class WeaponFamiliesValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa"&&Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Isolated project required");
        string output=Environment.GetEnvironmentVariable("WEAPON_FAMILIES_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=360;camera.backgroundColor=new Color(.13f,.19f,.27f);
        var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var host=UiSkin.Img(canvas.transform,"HeroHost",new Vector2(-220,-20),new Vector2(220,220),null,Color.white);
        var hero=AdventureHeroPresenter.Attach(host.rectTransform);
        var enemy=EnemyPresentationView.Create(canvas.transform,new Vector2(240,-130));enemy.SetEnemy("goblin");
        Action<string> capture=name=>{Canvas.ForceUpdateCanvases();var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);};
        Check(HeroPresentation.Config.weapons.Length==7,"Seven families required");
        foreach(var look in HeroPresentation.Config.weapons)
        {
            string id=look.id;hero.Cancel();hero.Equip(new EquipItem{baseId=id});
            foreach(string state in new[]{"idle","walk","attack-f1","attack-f2","recover","hit","victory","guard","focus","cast"})
            {
                hero.Cancel();hero.Request(state,false);hero.Step(.01f);capture(id+"-"+state);
                Check(hero.View.Weapon.sprite!=null,"Missing weapon "+id);
                Check(Vector2.Distance(hero.View.GripPosition,hero.View.Weapon.rectTransform.anchoredPosition)<.001f,"Detached grip "+id);
                if(hero.View.TwoHanded)
                {
                    Vector3 shaft=hero.View.Weapon.rectTransform.up;
                    Vector3 hands=hero.View.Root.TransformVector(hero.View.SupportPosition-hero.View.GripPosition).normalized;
                    Check(Vector3.Dot(shaft,hands)>.999f,"Support hand misses shaft "+id);
                }
            }
            hero.Cancel();hero.PrepareAttackTarget(enemy.Body.rectTransform);hero.Request("attack-f3-4",false);
            int impacts=0;bool prior=false,sawFlight=false;float flightStart=-1;
            for(int frame=0;frame<72;frame++)
            {
                hero.Step(1f/60);if(hero.IsProjectileFlying){sawFlight=true;if(flightStart<0)flightStart=frame/60f;Check(!hero.ImpactReady,"Impact before arrival");}
                if(hero.ImpactReady&&!prior)impacts++;prior=hero.ImpactReady;
                if(frame%3==0)capture(id+"-attack-"+frame.ToString("D3"));
                if(look.ranged)Check(Mathf.Abs(hero.Advance)<.001f,"Ranged weapon walks into enemy");
            }
            Check(impacts==1,"Contact must complete once "+id);
            if(look.ranged)
            {
                Check(sawFlight,"Missing ranged projectile "+id);
                hero.Cancel();hero.PrepareAttackTarget(enemy.Body.rectTransform);hero.Request("attack-f3-4",false);hero.Step(.01f);
                Check(hero.IsProjectileFlying,"No projectile to cancel");hero.Cancel();hero.Step(2);Check(!hero.IsProjectileFlying&&!hero.ImpactReady,"Canceled projectile hit target");
                hero.PrepareAttackTarget(enemy.Body.rectTransform);hero.Request("attack-f3-4",false);hero.Step(.01f);
                hero.Equip(new EquipItem{baseId="w_blade"});Check(hero.View.Player.WeaponId==id,"Mid-flight equipment replaced stroke");hero.Step(.5f);hero.Step(.5f);Check(hero.ImpactReady&&hero.View.Player.WeaponId=="w_blade","Equipment swap lost projectile impact");
                hero.Cancel();hero.Equip(new EquipItem{baseId=id});
                var disappearing=new GameObject("DisappearingTarget",typeof(RectTransform)).GetComponent<RectTransform>();
                hero.PrepareAttackTarget(disappearing);hero.Request("attack-f3-4",false);hero.Step(.01f);UnityEngine.Object.DestroyImmediate(disappearing.gameObject);hero.Step(1);Check(!hero.IsProjectileFlying&&!hero.ImpactReady,"Destroyed target still hit");
            }
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"families\":7,\"statesEach\":10,\"attackFramesEach\":24,\"contactOnce\":true,\"rangedNoApproach\":true,\"arrivalGate\":true,\"cancelFlight\":true,\"destroyedTarget\":true,\"pendingEquipment\":true,\"finalArtApproved\":false,\"productionSaveTouched\":false}");
        Debug.Log("WEAPON_FAMILIES_VALIDATION passed");
    }
}
