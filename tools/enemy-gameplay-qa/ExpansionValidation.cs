using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExpansionValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Application.productName=="shop-qa","Isolated project required");
        var output=Environment.GetEnvironmentVariable("EXPANSION_QA_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("QA Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=360;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.2f,.28f);
        var canvas=new GameObject("QA Canvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Action<string> capture=name=>{Canvas.ForceUpdateCanvases();var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);};
        var hero=HeroPresentationView.Create(canvas.transform,new Vector2(-200,-130));
        var enemy=EnemyPresentationView.Create(canvas.transform,new Vector2(200,-130));enemy.SetEnemy("lantern_moth");
        int enemyContacts=0;enemy.Player.Cue+=cue=>{if(cue=="contact")enemyContacts++;};
        enemy.Request("ready");enemy.Step(10);Check(enemyContacts==0&&enemy.Player.State=="ready","Moth ready holds");
        enemy.Request("strike");enemy.Step(.15f);Check(enemyContacts==0,"Moth premature contact");enemy.Step(.01f);Check(enemyContacts==1,"Moth contact timing");capture("moth-contact");enemy.Step(4);Check(enemyContacts==1,"Moth contact duplicated");
        enemy.Request("attack");enemy.Request("hit");enemy.Step(4);Check(enemyContacts==1,"Moth interrupted contact");
        foreach(var motion in new[]{"idle","attack","hit","defeat"})
        {
            enemy.SetEnemy("lantern_moth");enemy.Request(motion);
            for(int i=0;i<24;i++){enemy.Step(1f/24);capture("moth-"+motion+"-"+i.ToString("D2"));}
        }
        enemy.SetEnemy("lantern_moth");
        foreach(var id in new[]{"w_dagger","w_holy"})
        {
            hero.Player.CancelToLoop();hero.Player.Equip(new EquipItem{baseId=id});hero.Render();
            Check(hero.Player.WeaponId==id,"Equipment resolver "+id);
            foreach(var motion in new[]{"idle","walk","attack-f1","attack-f2","attack-f3-4","recover","hit","victory"})
            {
                hero.Player.CancelToLoop();hero.Player.Request(motion);hero.Step(.001f);
                Check(hero.Weapon.sprite!=null,"Missing sprite");
                Check(Vector2.Distance(hero.Weapon.rectTransform.anchoredPosition,hero.GripPosition)<.001f,"Detached grip");
                var pose=HeroPresentation.Config.poses[hero.Player.PoseIndex];
                if(pose.swordHand!=null&&pose.swordHand.w>0)Check(hero.Hand.gameObject.activeSelf,"Missing foreground hand");
                capture(id+"-"+motion);
            }
            int contacts=0;Action<string> onCue=cue=>{if(cue=="contact")contacts++;};hero.Player.Cue+=onCue;
            hero.Player.CancelToLoop();hero.Player.Request("attack");hero.Step(3);Check(contacts==1,"Hero contact once "+id);
            hero.Player.Request("attack");hero.Player.Equip(new EquipItem{baseId="w_spear"});Check(hero.Player.WeaponId==id&&hero.Player.PendingWeapon=="w_spear","Switch mid stroke");hero.Step(3);Check(hero.Player.WeaponId=="w_spear"&&hero.Player.PendingWeapon==null,"Pending equip not applied");
            hero.Player.Cue-=onCue;
        }
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"enemy\":\"lantern_moth\",\"enemySequenceFrames\":96,\"weapons\":[\"w_dagger\",\"w_holy\"],\"weaponStatesEach\":8,\"contactOnce\":true,\"interruptCancelsContact\":true,\"pendingEquipment\":true,\"foregroundGrip\":true,\"saveDataTouched\":false,\"finalArtApproved\":false}");
        Debug.Log("EXPANSION_VALIDATION passed");
    }
}
