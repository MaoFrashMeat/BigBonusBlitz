using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 検証用（scratchpad のバッチ複製だけに置く）。設定・グラフ・地図の窓を描く
public static class ModalUiProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("batch only");
        string output = Environment.GetEnvironmentVariable("MODAL_UI_OUTPUT");
        string suffix = Environment.GetEnvironmentVariable("BBB_THIN_FRAME") == "1" ? "-thin" : "-now";
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("ModalProbe").AddComponent<GameController>();
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
        if (frames.Length > 0) hero.GetComponent<Image>().sprite = frames[0];
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
        var size = new Vector2Int(2556, 1179);
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540);
        // 実績: いくつか進めておく
        machine.Ach.Counters["spins"] = 1234; machine.Ach.Counters["big"] = 3; machine.Ach.Unlocked.Add("first_spin"); machine.Ach.Unlocked.Add("spins_1000"); machine.Ach.Unlocked.Add("big_1");
        // 図鑑: いくつか拾っておく
        int k = 0;
        foreach (var b in machine.Config.equipment.bases) { if (k++ % 3 == 0) machine.Ach.Counters[AchievementCounters.SeenPrefix + b.id] = k; }
        machine.Ach.Counters[AchievementCounters.TreasurePrefix + "souls_s"] = 12;
        machine.Ach.Counters[AchievementCounters.TreasurePrefix + "cave_map"] = 2;
        foreach (var name in new[] { "ToggleSettings", "ToggleGraph", "ToggleMap", "ToggleDebug", "ToggleTrophy" })
        {
            typeof(GameController).GetMethod(name, flags).Invoke(host, null);
            Shot(canvas, camera, size, Path.Combine(output, name.Substring(6).ToLower() + suffix + ".png"));
            typeof(GameController).GetMethod(name, flags).Invoke(host, null);
        }
        // 図鑑のタブ（TrophyScreen を直接作る）
        var stage = (RectTransform)typeof(GameController).GetField("_stage", flags).GetValue(host);
        var audio = (AudioManager)typeof(GameController).GetField("_audio", flags).GetValue(host);
        for (int tab = 1; tab <= 2; tab++)
        {
            var box = TrophyScreen.Build(stage, machine, audio, null, tab);
            Shot(canvas, camera, size, Path.Combine(output, "trophy-tab" + tab + suffix + ".png"));
            UnityEngine.Object.DestroyImmediate(box);
        }
        Debug.Log("MODAL_UI_PROBE done");
    }

    static void Shot(Canvas canvas, Camera camera, Vector2Int size, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; }
        Canvas.ForceUpdateCanvases();
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
