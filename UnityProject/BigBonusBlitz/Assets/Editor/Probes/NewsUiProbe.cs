using System;
using System.IO;
using System.Reflection;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 検証用（scratchpad のバッチ複製だけに置く）。タイトルのお知らせ窓を一覧と中身の 2 状態で描く
public static class NewsUiProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("batch only");
        string output = Environment.GetEnvironmentVariable("NEWS_UI_OUTPUT");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("TitleProbe").AddComponent<TitleScreen>();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(TitleScreen).GetField("_audio", flags).SetValue(host, AudioManager.Create());
        // 未読の印: 「dev 152 の項目まで読んだ」状態にしておく（それより新しい行に点が付く）
        PlayerPrefs.SetString("bbb_news_seen", "dev 152|2026-09-13");
        typeof(TitleScreen).GetMethod("BuildUi", flags).Invoke(host, null);
        var canvas = (Canvas)typeof(TitleScreen).GetField("_canvas", flags).GetValue(host);
        ((CanvasGroup)typeof(TitleScreen).GetField("_fade", flags).GetValue(host)).alpha = 0f;
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
        var newsBox = (GameObject)typeof(TitleScreen).GetField("_newsBox", flags).GetValue(host);
        var size0 = new Vector2Int(2556, 1179);
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size0.x / size0.y, 540);
        Shot(canvas, camera, size0, Path.Combine(output, "title-badge.png"));
        newsBox.SetActive(true);
        if (Environment.GetEnvironmentVariable("NEWS_UI_NOMASK") == "1")
            foreach (var mk in newsBox.GetComponentsInChildren<RectMask2D>(true)) mk.enabled = false;
        var size = new Vector2Int(2556, 1179);
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540);
        Shot(canvas, camera, size, Path.Combine(output, "news-list.png"));
        var sb = new System.Text.StringBuilder();
        Dump(newsBox.transform, 0, sb);
        File.WriteAllText(Path.Combine(output, "tree.txt"), sb.ToString());
        // 先頭の行を押して中身へ
        var row = newsBox.transform.Find("Card/List/Content/Row")?.GetComponent<Button>();
        if (row == null) throw new Exception("Row not found");
        row.onClick.Invoke();
        Shot(canvas, camera, size, Path.Combine(output, "news-detail.png"));
        Debug.Log("NEWS_UI_PROBE done");
    }

    static void Dump(Transform t, int depth, System.Text.StringBuilder sb)
    {
        var rt = t as RectTransform;
        var txt = t.GetComponent<Text>();
        sb.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " (off)");
        if (rt != null) sb.Append($" pos={rt.anchoredPosition} size={rt.rect.size} aMin={rt.anchorMin} aMax={rt.anchorMax}");
        if (txt != null) sb.Append($" text='{(txt.text.Length > 30 ? txt.text.Substring(0, 30) : txt.text)}' col={txt.color} font={(txt.font == null ? "null" : txt.font.name)}");
        sb.AppendLine();
        foreach (Transform c in t) Dump(c, depth + 1, sb);
    }

    static void Shot(Canvas canvas, Camera camera, Vector2Int size, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; }
        Canvas.ForceUpdateCanvases();
        // レイアウト（ContentSizeFitter）を確定させる
        foreach (var rt in canvas.GetComponentsInChildren<RectTransform>()) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
        var target = RenderTexture.GetTemporary(size.x, size.y, 24);
        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
    }
}
