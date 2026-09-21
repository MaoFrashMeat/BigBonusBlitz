using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンのホバー（HoverGlow）を本物のゲーム画面で描いて PNG に出す（docs/ui_rules.md 9）。分離した batchmode 専用。
/// マウスは動かせないので、MAX BET と AUTO の光を「乗っている」状態（HoverGlow.Alpha）に固定して 1 枚、素のまま 1 枚。
///   HOVER_QA_OUTPUT=&lt;出力先&gt; Unity.exe -batchmode -quit -projectPath &lt;分離コピー&gt; -executeMethod HoverQa.Run -logFile out.log
/// </summary>
public static class HoverQa
{
    static string output;
    static Canvas canvas;
    static Camera camera;

    static void Assert(bool v, string message) { if (!v) throw new Exception(message); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Isolated batch project only");
        output = Environment.GetEnvironmentVariable("HOVER_QA_OUTPUT");
        Assert(!string.IsNullOrEmpty(output), "HOVER_QA_OUTPUT required");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiFactory.EnsureEventSystem();

        // 本物のゲーム画面を組む（GameController.BuildUi）
        var m = GameDataLoader.CreateMachine(new SystemRandom());
        var audio = AudioManager.Create();
        m.Credit = 1081; m.Wallet.Souls = 12345;
        var controller = new GameObject("Controller").AddComponent<GameController>();
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(GameController).GetField("_m", f).SetValue(controller, m);
        typeof(GameController).GetField("_audio", f).SetValue(controller, audio);
        typeof(GameController).GetMethod("BuildUi", f).Invoke(controller, null);
        typeof(GameController).GetMethod("RefreshUi", f).Invoke(controller, null);

        canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Assert(canvas != null, "Canvas not built");
        var scaler = canvas.GetComponent<CanvasScaler>(); if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);
        var safe = canvas.GetComponentInChildren<SafeStage>();
        Assert(safe != null, "SafeStage missing");
        safe.SafeRoot.anchorMin = Vector2.zero; safe.SafeRoot.anchorMax = Vector2.one;
        safe.SafeRoot.offsetMin = safe.SafeRoot.offsetMax = Vector2.zero; safe.Stage.localScale = Vector3.one;
        safe.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.position = Vector3.zero; canvas.transform.localScale = Vector3.one;
        camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 270;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UiSkin.Bg; canvas.worldCamera = camera;

        Render("hover-off");
        int lit = 0;
        foreach (var h in canvas.GetComponentsInChildren<HoverGlow>(true))
        {
            var btn = h.GetComponent<Button>();
            if (btn == null || h.glow == null) continue;
            if (btn.name == "BtnBet" || btn.name == "BtnAuto" || btn.name.StartsWith("BtnStop")) { h.glow.color = new Color(1, 1, 1, HoverGlow.Alpha); lit++; }
        }
        Assert(lit >= 2, "MAX BET / AUTO のボタンに HoverGlow が付いていない: " + lit);
        Render("hover-on");
        File.WriteAllText(Path.Combine(output, "validation.json"),
            "{\"passed\":true,\"lit\":" + lit + ",\"resolutions\":[\"1280x720\",\"2556x1179\"],\"saveDataTouched\":false}");
        Debug.Log("HOVER_QA passed " + lit);
    }

    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2556, 1179) })
        {
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540);
            Canvas.ForceUpdateCanvases();
            foreach (var t in canvas.GetComponentsInChildren<Text>()) t.font.RequestCharactersInTexture(t.text, t.fontSize, t.fontStyle);
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; }
                Canvas.ForceUpdateCanvases();
            }
            var target = RenderTexture.GetTemporary(size.x, size.y, 24);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(output, name + "-" + size.x + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
