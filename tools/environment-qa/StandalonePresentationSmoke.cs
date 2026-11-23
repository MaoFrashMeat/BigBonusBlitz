using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEngine;
using UnityEngine.UI;

// Copy into Assets only in the isolated validation player.
public sealed class StandalonePresentationSmoke : MonoBehaviour
{
    static string output;
    static Camera captureCamera;
    static RenderTexture surface;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        output=Environment.GetEnvironmentVariable("BBB_SMOKE_OUTPUT");
        if(string.IsNullOrEmpty(output)||Application.isEditor||Application.companyName!="BBBPresentationQA"||
           Application.productName!="shop-qa")return;
        Application.runInBackground=true;
        var go=new GameObject("StandalonePresentationSmoke");DontDestroyOnLoad(go);
        go.AddComponent<StandalonePresentationSmoke>();
    }

    static Button ButtonNamed(string id)=>FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==id);
    static IEnumerator AwaitButton(string id)
    {
        float deadline=Time.realtimeSinceStartup+30;
        while(ButtonNamed(id)==null&&Time.realtimeSinceStartup<deadline)yield return null;
        if(ButtonNamed(id)==null)throw new Exception("Button not reached: "+id);
    }

    static void AttachSurface()
    {
        if(captureCamera==null)
        {
            var go=new GameObject("PlayerCaptureCamera");DontDestroyOnLoad(go);
            captureCamera=go.AddComponent<Camera>();
            captureCamera.clearFlags=CameraClearFlags.SolidColor;captureCamera.backgroundColor=Color.black;
            captureCamera.orthographic=true;captureCamera.orthographicSize=270;captureCamera.depth=100;
            captureCamera.transform.position=Vector3.back*10;
            surface=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            surface.Create();captureCamera.targetTexture=surface;
        }
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if(!canvas.isRootCanvas||canvas.renderMode!=RenderMode.ScreenSpaceOverlay)continue;
            var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)scaler.enabled=false;
            canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=captureCamera;
            var root=(RectTransform)canvas.transform;
            root.position=Vector3.zero;root.rotation=Quaternion.identity;root.localScale=Vector3.one;root.sizeDelta=new Vector2(960,540);
        }
        Canvas.ForceUpdateCanvases();
    }

    static IEnumerator Capture(string name)
    {
        AttachSurface();
        for(int i=0;i<4;i++)yield return null;
        yield return new WaitForEndOfFrame();
        foreach(var graphic in FindObjectsByType<Graphic>(FindObjectsSortMode.None))
        {
            graphic.SetAllDirty();graphic.Rebuild(CanvasUpdate.PreRender);graphic.canvasRenderer.cull=false;
        }
        Canvas.ForceUpdateCanvases();
        var request=new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=surface};
        if(UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(captureCamera,request))
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(captureCamera,request);
        else captureCamera.Render();
        var previous=RenderTexture.active;RenderTexture.active=surface;
        var image=new Texture2D(surface.width,surface.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();RenderTexture.active=previous;
        if(image.GetPixels32().Count(c=>Math.Max(c.r,Math.Max(c.g,c.b))>32)<image.width*image.height/20)
            throw new Exception("Empty player render "+name);
        File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        yield return AwaitButton("TapArea");
        var fixture=GameDataLoader.CreateMachine(new SystemRandom(931));fixture.Adv.opDone=true;
        fixture.Adv.torches=120;fixture.Credit=500;SaveData.Save(fixture,AudioManager.Create());
        yield return new WaitForSecondsRealtime(1);
        yield return Capture("windows-title");
        ButtonNamed("TapArea").onClick.Invoke();
        yield return AwaitButton("BackAdventure");
        yield return new WaitForSecondsRealtime(1);
        yield return Capture("windows-map");
        ButtonNamed("BackAdventure").onClick.Invoke();
        yield return AwaitButton("BtnBet");yield return new WaitForSecondsRealtime(2);
        var game=FindFirstObjectByType<GameController>();
        var machine=(SlotMachine)typeof(GameController).GetField("_m",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
        var hero=FindFirstObjectByType<AdventureHeroPresenter>();
        if(hero==null||hero.View.Body.sprite==null)throw new Exception("Missing gameplay hero");
        yield return Capture("windows-adventure");
        var elapsed=new float[240];
        for(int i=0;i<elapsed.Length;i++){yield return null;elapsed[i]=Time.unscaledDeltaTime*1000;}
        var audio=AudioManager.Create().GetComponents<AudioSource>().FirstOrDefault(s=>s.clip!=null&&s.isPlaying);
        ButtonNamed("BtnBet").onClick.Invoke();yield return null;
        if(!machine.IsGameActive)throw new Exception("BET did not start reels");
        yield return new WaitForSecondsRealtime(.2f);yield return Capture("windows-bet");
        Array.Sort(elapsed);
        File.WriteAllText(Path.Combine(output,"player.json"),JsonUtility.ToJson(new Result{
            capture="Explicit camera render of live canvases to 1280x720 texture",gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceType.ToString(),
            quality=QualitySettings.names[QualitySettings.GetQualityLevel()],width=Screen.width,height=Screen.height,
            frames=elapsed.Length,p50ms=elapsed[119],p95ms=elapsed[227],managedMB=GC.GetTotalMemory(false)/1048576,
            audioClip=audio!=null?audio.clip.name:"",betAccepted=true},true));
        Application.Quit(0);
    }
    [Serializable] sealed class Result
    {
        public string capture,gpu,cpu,graphics,quality,audioClip;
        public int width,height,frames;public float p50ms,p95ms;public long managedMB;public bool betAccepted;
    }
}
