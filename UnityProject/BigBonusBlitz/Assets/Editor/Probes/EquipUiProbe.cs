using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 検証用（scratchpad のバッチ複製だけに置く）。冒険画面の装備ボタンと、開いた装備画面を描く
public static class EquipUiProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("batch only");
        string output = Environment.GetEnvironmentVariable("EQUIP_UI_OUTPUT");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerPrefs.SetInt("bbb_graph_always", 1);   // ミニのスランプを出した状態で描く
        var host = new GameObject("EquipProbe").AddComponent<GameController>();
        var machine = GameDataLoader.CreateMachine(new SystemRandom());
        machine.Wallet.Souls = 12345; machine.Credit = 1081;
        machine.Adv.nodeId = "B-2"; machine.Adv.spinsLeft = 9;
        machine.Adv.visited.Clear(); machine.Adv.visited.Add("A-1"); machine.Adv.visited.Add("B-2");
        // 鞄に品を入れ、1 つ着けておく
        var rng = new SystemRandom(5);
        for (int i = 0; i < 14; i++) { var it = EquipDirector.Roll(machine.Config.equipment, 1 + i % 7, rng); if (it != null) machine.Equip.Bag.Add(it); }
        for (int i = 0; i < 9 && machine.Equip.Bag.Count > 0; i++) EquipDirector.AutoEquipIfBetter(machine.Equip, machine.Equip.Bag[0]);
        if (machine.Equip.Bag.Count > 1) machine.Equip.Bag[1].rarity = 8;   // プリズム（虹）を 1 つ
        // 一番長い名前（12 字）を着けた状態で見る
        foreach (var kv in new System.Collections.Generic.List<string>(machine.Equip.Worn.Keys)) { var w = machine.Equip.Worn[kv]; if (w != null) { w.name = "深淵の達人の耳飾りの狩人"; break; } }
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(GameController).GetField("_m", flags).SetValue(host, machine);
        typeof(GameController).GetField("_audio", flags).SetValue(host, AudioManager.Create());
        machine.Stats.Unspent = 3; machine.Stats.Life = 2;
        machine.Curse.Taken.Add(new CurseInstance { curseId = "c1", curseName = "重い足", curseEffect = CurseEffects.TorchDrain, curseValue = 20, blessId = "b1", blessName = "鷹の目", blessEffect = ShopEffects.DefeatBonus, blessValue = 6, depth = 3 });
        machine.Curse.Taken.Add(new CurseInstance { curseId = "c2", curseName = "欠けた器", curseEffect = CurseEffects.PayoutCut, curseValue = 8, blessId = "b2", blessName = "商人の縁", blessEffect = ShopEffects.SoulGain, blessValue = 15, depth = 4 });
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
        ((Text)typeof(GameController).GetField("_message", flags).GetValue(host)).text = "BETで冒険を進めよう";
        Shot(canvas, camera, size, Path.Combine(output, "adventure-tools.png"));
        // 装備ボタンを押す
        var btn = canvas.transform.Find("SafeRoot/Stage/BtnEquip")?.GetComponent<Button>() ?? FindButton(canvas.transform, "BtnEquip");
        if (btn == null) throw new Exception("BtnEquip not found");
        btn.onClick.Invoke();
        Shot(canvas, camera, size, Path.Combine(output, "equip-screen.png"));
        // 鞄の 2 つ目を押して詳細を出す
        Button bag1 = null; foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) if (t.name == "Bag1") { bag1 = t.GetComponentInChildren<Button>(true); break; }
        if (bag1 != null) { bag1.onClick.Invoke(); Shot(canvas, camera, size, Path.Combine(output, "equip-detail.png")); }
        // ステータスの窓（装備画面の題の右）
        // 呪いの一覧（2 組受けている状態で）
        var cuBtn = FindButton(canvas.transform, "BtnCurses");
        if (cuBtn != null) { cuBtn.onClick.Invoke(); Shot(canvas, camera, size, Path.Combine(output, "curse-list.png")); }
        var stBtn = FindButton(canvas.transform, "BtnStats");
        if (stBtn != null) { stBtn.onClick.Invoke(); Shot(canvas, camera, size, Path.Combine(output, "stats-screen.png")); }
        Debug.Log("EQUIP_UI_PROBE done");
    }

    static Button FindButton(Transform root, string name)
    {
        foreach (var b in root.GetComponentsInChildren<Button>(true)) if (b.name == name) return b;
        return null;
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
