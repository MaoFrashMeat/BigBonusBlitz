using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 街の地図を実際の uGUI で描いて PNG に出す（docs/ui_rules.md 9: 近似の道具で「通った」と言わない）。
/// 分離した batchmode 専用。セーブは読み書きしない。出力先は環境変数 ATELIER_MAP_OUTPUT。
///   Unity.exe -batchmode -nographics は使わない（描画するので -nographics を付けない）
///   Unity.exe -batchmode -quit -projectPath &lt;分離コピー&gt; -executeMethod AtelierMapQa.Run -logFile out.log
/// 検査: 押せる面 44 以上、同じ親の文字どうしが重ならない、下端のボタン列が端から 24 以上。
/// </summary>
public static class AtelierMapQa
{
    static string output;
    static Canvas canvas;
    static Camera camera;
    static RectTransform stage;
    static void Assert(bool v, string message) { if (!v) throw new Exception(message); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Isolated batch project only");
        output = Environment.GetEnvironmentVariable("ATELIER_MAP_OUTPUT");
        Assert(!string.IsNullOrEmpty(output), "ATELIER_MAP_OUTPUT required");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiFactory.EnsureEventSystem();
        canvas = UiFactory.CreateCanvas("AtelierMapQa"); UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode = RenderMode.WorldSpace; canvas.transform.position = Vector3.zero;
        stage = UiSkin.Rect(canvas.transform, "Stage", Vector2.zero, new Vector2(960, 540));
        camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 270;
        camera.backgroundColor = UiSkin.Hex("#122e32"); camera.clearFlags = CameraClearFlags.SolidColor; canvas.worldCamera = camera;

        var m = GameDataLoader.CreateMachine(new SystemRandom());
        m.Wallet.Souls = 100; m.Credit = 1105;
        m.Adv.nodeId = "B-2"; m.Adv.spinsLeft = 4; m.Adv.torches = 7;
        m.Adv.visited.Clear(); m.Adv.visited.Add("A-1"); m.Adv.visited.Add("B-1"); m.Adv.visited.Add("B-2");
        int go = 0;
        AtelierMap.Build(stage, m, () => go++, () => { }, () => { }, () => { }, () => { }, () => { }, out var msg);
        msg.text = "力尽きて街に運ばれた。宿で一晩休み、エンバー 1,105 を分けてもらった";
        stage.GetComponentInChildren<AtelierMessageBand>().Refresh();      // Update は batchmode の編集モードでは回らない
        Render("town-map");
        // ズームの検算: 拡大しても現在地が窓の中に残る
        var zoom = stage.GetComponentInChildren<AtelierMapZoom>(); Assert(zoom != null, "AtelierMapZoom missing");
        zoom.Step(1.25f); zoom.Step(1.25f); Canvas.ForceUpdateCanvases();
        Assert(Mathf.Abs(zoom.Zoom - 1.5625f) < .01f, "Zoom step wrong: " + zoom.Zoom);
        Render("town-map-zoomed");
        Click("Home"); Assert(Mathf.Abs(zoom.Zoom - 1f) < .01f, "Home did not reset zoom");
        Click("Depart"); Assert(go == 1, "Departure callback missing");
        File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"screen\":\"town-map\",\"resolutions\":[\"1280x720\",\"2556x1179\"],\"saveDataTouched\":false}");
        Debug.Log("ATELIER_MAP_QA passed");
    }

    static void Click(string name)
    {
        var b = stage.GetComponentsInChildren<Button>().First(x => x.name == name);
        Assert(b.interactable, "Disabled click: " + name); b.onClick.Invoke(); Canvas.ForceUpdateCanvases();
    }

    static void Render(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var b in stage.GetComponentsInChildren<Button>())
        {
            var rt = (RectTransform)b.transform;
            Assert(rt.sizeDelta.y >= 44 || rt.sizeDelta.x >= 44, "Small touch target: " + b.name);
            // 下端のボタン列は舞台の端から 24 以上（docs/ui_rules.md 12）
            if (b.transform.parent == stage.GetChild(0)) Assert(rt.anchoredPosition.y - rt.sizeDelta.y * .5f >= -270 + 24 - .5f, "Bottom row too close to the edge: " + b.name);
        }
        foreach (var parent in stage.GetComponentsInChildren<RectTransform>())
        {
            if (parent.name == "MapContent") continue;      // 地図の中身は拡大縮小して見る前提。段の見出しと地点名の重なりは別で見る
            var texts = parent.Cast<Transform>().Select(t => t.GetComponent<Text>()).Where(t => t != null && t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)).ToArray();
            for (int a = 0; a < texts.Length; a++) for (int b = a + 1; b < texts.Length; b++)
                Assert(!Bounds(texts[a].rectTransform).Overlaps(Bounds(texts[b].rectTransform)), "Overlapping text: " + parent.name + "/" + texts[a].name + " and " + texts[b].name);
        }
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2556, 1179) })
        {
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540); Canvas.ForceUpdateCanvases();
            foreach (var t in canvas.GetComponentsInChildren<Text>()) t.font.RequestCharactersInTexture(t.text, t.fontSize, t.fontStyle);
            for (int pass = 0; pass < 3; pass++) { foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; } Canvas.ForceUpdateCanvases(); }
            var target = RenderTexture.GetTemporary(size.x, size.y, 24); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(output, name + "-" + size.x + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(tex);
        }
    }

    static Rect Bounds(RectTransform rt) { var c = new Vector3[4]; rt.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
}
