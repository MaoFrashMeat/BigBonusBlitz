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
/// 告知（zoneFx の エンゲージ / BIG / REG）を、本物のゲーム画面の上に実際の uGUI で描いて PNG に出す
/// （docs/ui_rules.md 9: 近似の道具で「通った」と言わない）。分離した batchmode 専用。セーブは読み書きしない。
///   ZONE_FX_OUTPUT=&lt;出力先&gt; Unity.exe -batchmode -quit -projectPath &lt;分離コピー&gt; -executeMethod ZoneFxQa.Run -logFile out.log
/// 動きは時間が要るので、ここでは「保持中」の一枚だけを見る（ZoneFx.Preview）。動きは tools/zone_viewer.html で確かめる。
/// </summary>
public static class ZoneFxQa
{
    static string output;
    static Canvas canvas;
    static Camera camera;

    static void Assert(bool v, string message) { if (!v) throw new Exception(message); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Isolated batch project only");
        output = Environment.GetEnvironmentVariable("ZONE_FX_OUTPUT");
        Assert(!string.IsNullOrEmpty(output), "ZONE_FX_OUTPUT required");
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

        var zones = m.Config.zoneFx;
        int made = 0;
        foreach (var (name, z) in new[] { ("engage", zones?.engage), ("big", zones?.big), ("reg", zones?.reg) })
        {
            if (!ZoneFx.Has(z)) { Debug.Log("ZONE_FX_QA skip " + name + "（絵が無い）"); continue; }
            var root = ZoneFx.Preview(safe.Stage, z);
            Assert(root != null, "Preview failed: " + name);
            // 告知は舞台の中に収まっているか（はみ出すと実機で切れる）
            var art = (RectTransform)root.Find("Art");
            Assert(Mathf.Abs(z.x) + art.sizeDelta.x * .5f <= safe.Stage.sizeDelta.x * .5f + 1f, "告知が舞台の横幅からはみ出す: " + name);
            Assert(Mathf.Abs(z.y) + art.sizeDelta.y * .5f <= safe.Stage.sizeDelta.y * .5f + 1f, "告知が舞台の高さからはみ出す: " + name);
            Render("zone-" + name);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            made++;
        }
        File.WriteAllText(Path.Combine(output, "validation.json"),
            "{\"passed\":true,\"zones\":" + made + ",\"resolutions\":[\"1280x720\",\"2556x1179\"],\"saveDataTouched\":false}");
        Debug.Log("ZONE_FX_QA passed " + made);
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
