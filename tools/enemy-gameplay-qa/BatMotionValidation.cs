using System;
using System.IO;
using System.Linq;
using BBB.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BatMotionValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode && Application.productName=="shop-qa", "Isolated shop-qa required");
        var output=Environment.GetEnvironmentVariable("BAT_QA_OUTPUT");Check(!string.IsNullOrEmpty(output),"Output required");Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("QA Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=360;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.2f,.28f);
        var canvas=new GameObject("QA Canvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var look=EnemyPresentation.Find("bat");Check(look.poses.Length==8,"Eight poses");
        var view=EnemyPresentationView.Create(canvas.transform,new Vector2(100,-100));view.SetEnemy("bat");
        Action<string> capture=name=>{Canvas.ForceUpdateCanvases();var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);};
        int contacts=0;view.Player.Cue+=cue=>{if(cue=="contact")contacts++;};
        view.Request("ready");view.Step(30);Check(view.Player.State=="ready"&&contacts==0,"Held anticipation");capture("ready");
        view.Request("strike");view.Step(.13f);Check(contacts==1,"Contact exactly once");capture("contact");view.Step(3);Check(contacts==1&&view.Player.State=="idle","Recover to idle");
        view.Request("attack");view.Request("hit");view.Step(3);Check(contacts==1,"Interrupted attack does not strike");
        view.Request("defeat");view.Step(10);view.Request("attack");Check(view.Player.State=="defeat"&&view.Player.PoseIndex==7,"Defeat holds");capture("defeat");
        foreach(var motion in new[]{"idle","attack","hit","defeat"})
        {
            view.SetEnemy("bat");view.Request(motion);
            for(int i=0;i<24;i++){view.Step(1f/24);capture(motion+"-"+i.ToString("D2"));}
        }
        view.gameObject.SetActive(false);
        var host=new GameObject("Legacy enemy",typeof(RectTransform),typeof(Image));host.transform.SetParent(canvas.transform,false);
        var legacy=host.GetComponent<Image>();legacy.color=new Color(.8f,.9f,1,1);
        var presenter=AdventureEnemyPresenter.Attach(host.GetComponent<RectTransform>(),-100);
        var cfg=EnemyPresentation.Config;var before=cfg.gameplayTypes;cfg.gameplayTypes=before.Concat(new[]{"bat"}).Distinct().ToArray();
        presenter.Show("bat",1,400);Check(presenter.Active&&!legacy.enabled,"Bat runtime presenter activated");presenter.Request("ready");presenter.Step(30);Check(!presenter.ImpactReady,"Ready cannot damage");presenter.Request("strike");presenter.Step(.13f);Check(presenter.ImpactReady,"Runtime contact");capture("runtime-contact");
        presenter.Show("unsupported",1,400);Check(!presenter.Active&&legacy.enabled&&!presenter.ImpactReady,"Legacy fallback and cue reset");presenter.Show("bat",1,400);presenter.Hide();Check(!presenter.Active&&legacy.enabled,"Hide restores legacy");cfg.gameplayTypes=before;
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"enemy\":\"bat\",\"poses\":8,\"motions\":6,\"sequenceFrames\":96,\"runtimePresenter\":true,\"contactOnce\":true,\"interruptCancelsContact\":true,\"defeatHolds\":true,\"saveDataTouched\":false,\"finalArtApproved\":false}");
        Debug.Log("BAT_MOTION_VALIDATION passed");
    }
}
