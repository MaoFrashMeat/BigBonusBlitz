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
/// 序章の台詞窓（OpeningPlayer）を本物のゲーム画面の上に描いて PNG に出す（docs/ui_rules.md 9）。分離した batchmode 専用。
///   OPENING_QA_OUTPUT=&lt;出力先&gt; Unity.exe -batchmode -quit -projectPath &lt;分離コピー&gt; -executeMethod OpeningQa.Run -logFile out.log
/// </summary>
public static class OpeningQa
{
    static string output; static Canvas canvas; static Camera camera;
    static void Assert(bool v, string message) { if (!v) throw new Exception(message); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Isolated batch project only");
        output = Environment.GetEnvironmentVariable("OPENING_QA_OUTPUT");
        Assert(!string.IsNullOrEmpty(output), "OPENING_QA_OUTPUT required"); Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiFactory.EnsureEventSystem();
        var m = GameDataLoader.CreateMachine(new SystemRandom());
        var audio = AudioManager.Create();
        var controller = new GameObject("Controller").AddComponent<GameController>();
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(GameController).GetField("_m", f).SetValue(controller, m);
        typeof(GameController).GetField("_audio", f).SetValue(controller, audio);
        typeof(GameController).GetMethod("BuildUi", f).Invoke(controller, null);
        typeof(GameController).GetMethod("RefreshUi", f).Invoke(controller, null);
        canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>(); if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);
        var safe = canvas.GetComponentInChildren<SafeStage>();
        safe.SafeRoot.anchorMin = Vector2.zero; safe.SafeRoot.anchorMax = Vector2.one;
        safe.SafeRoot.offsetMin = safe.SafeRoot.offsetMax = Vector2.zero; safe.Stage.localScale = Vector3.one; safe.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace; canvas.transform.position = Vector3.zero; canvas.transform.localScale = Vector3.one;
        camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 270;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UiSkin.Bg; canvas.worldCamera = camera;
        // 台詞窓を同じ Canvas の上に置く（描き出しの都合。実機は別 Canvas）
        var op = m.Config.story.op; Assert(op != null && op.scenes.Count >= 8, "story.op が無い");
        var player = OpeningPlayer.Create(() => { }, "サリア");
        var pc = player.GetComponentInChildren<Canvas>();
        var root = (RectTransform)pc.transform;
        foreach (Transform c in new System.Collections.Generic.List<Transform>(System.Linq.Enumerable.Cast<Transform>(root))) c.SetParent(canvas.transform, false);
        UnityEngine.Object.DestroyImmediate(pc.gameObject);
        var esc = op.Find("escape"); player.SetDim(esc.dim); player.Show(esc.lines[2]);
        Render("op-escape");
        var dark = op.Find("dark"); player.SetDim(1f); player.Show(dark.lines[1]);
        Render("op-dark");
        var keeper = op.Find("keeper"); player.SetDim(keeper.dim); player.Show(keeper.lines[5]);
        Render("op-keeper");
        File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"screens\":3,\"scenes\":" + op.scenes.Count + "}");
        Debug.Log("OPENING_QA passed");
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
