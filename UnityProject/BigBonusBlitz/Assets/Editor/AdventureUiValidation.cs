using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AdventureUiValidation
{
    // Batch-only: no save data is loaded or written, and the user's open scene is untouched.
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Run in an isolated batch project.");
        string output = Environment.GetEnvironmentVariable("ADVENTURE_UI_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("ADVENTURE_UI_OUTPUT is required.");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("AdventureValidation").AddComponent<GameController>();
        var machine = GameDataLoader.CreateMachine(new SystemRandom());
        machine.Wallet.Souls = 12345; machine.Credit = 1081;
        machine.Adv.nodeId = "B-2"; machine.Adv.spinsLeft = 9;
        machine.Adv.visited.Clear(); machine.Adv.visited.Add("A-1"); machine.Adv.visited.Add("B-2");
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(GameController).GetField("_m", flags).SetValue(host, machine);
        typeof(GameController).GetField("_audio", flags).SetValue(host, AudioManager.Create());
        typeof(GameController).GetMethod("BuildUi", flags).Invoke(host, null);
        var hero = (RectTransform)typeof(GameController).GetField("_charRt", flags).GetValue(host);
        var frames = (Sprite[])typeof(GameController).GetField("_idleFrames", flags).GetValue(host);
        if (frames.Length == 0) throw new Exception("Hero sprites are missing.");
        hero.GetComponent<Image>().sprite = frames[0];
        typeof(GameController).GetMethod("RefreshUi", flags).Invoke(host, null);
        var canvas = ((RectTransform)typeof(GameController).GetField("_stage", flags).GetValue(host)).GetComponentInParent<Canvas>();
        
        UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        var safe = canvas.GetComponentInChildren<SafeStage>();
        safe.SafeRoot.anchorMin = Vector2.zero; safe.SafeRoot.anchorMax = Vector2.one;
        safe.SafeRoot.offsetMin = safe.SafeRoot.offsetMax = Vector2.zero; safe.Stage.localScale = Vector3.one;
        safe.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.position = Vector3.zero; canvas.transform.localScale = Vector3.one;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 270;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UiSkin.Bg; canvas.worldCamera = camera;
        foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(2556,1179) })
        {
            if (size.x == 2556) { machine.BonusMode = BonusMode.RB; machine.BonusEarned = 51; machine.BonusPayoutTarget = 90; machine.BonusGamesTotal = 30; machine.BonusGamesPlayed = 18; }
            typeof(GameController).GetMethod("RefreshUi", flags).Invoke(host, null);
            typeof(GameController).GetMethod("UpdateAtFrame", flags).Invoke(host, null);
            ((Text)typeof(GameController).GetField("_message", flags).GetValue(host)).text = "BETで冒険を進めよう";
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540);
            // 舞台より狭い画面では実機と同じく舞台ごと縮める（SafeStage と同じ式）
            safe.Stage.localScale = Vector3.one * Mathf.Min(1f, (540f * size.x / size.y) / safe.Stage.sizeDelta.x);
            Canvas.ForceUpdateCanvases();
            foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; }
            Canvas.ForceUpdateCanvases();
            var target = RenderTexture.GetTemporary(size.x, size.y, 24);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(output,"adventure-v2-"+size.x+".png"),image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=null; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
        }
        foreach (var button in canvas.GetComponentsInChildren<Button>())
            if ((button.name == "BtnBet" || button.name == "BtnAuto" || button.name == "BtnSettings" || button.name == "BtnGraph") && ((RectTransform)button.transform).rect.height < 44) throw new Exception("Touch target smaller than 44: " + button.name);
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"saveDataTouched\":false}");
        Debug.Log("ADVENTURE_UI_VALIDATION passed");
    }
}
