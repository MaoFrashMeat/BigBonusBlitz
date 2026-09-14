using System;
using System.IO;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 検証用（scratchpad のバッチ複製だけに置く）。アイコン入りのメッセージと、説明の吹き出しを描く
public static class ExplainUiProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("batch only");
        string output = Environment.GetEnvironmentVariable("EXPLAIN_UI_OUTPUT");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
        var setMessage = typeof(GameController).GetMethod("SetMessage", flags);
        setMessage.Invoke(host, new object[] { "技術介入 成功！  {soul}+90  {ember}+50  EXP +60", true, (Color?)UiSkin.Gold });
        var put = typeof(GameController).GetMethod("PutExplainLine", flags);
        put.Invoke(host, new object[] { "スライムを倒した。EXP +10、{soul}+10", 1f });
        // 帯（SlamTitle と同じ作り）を静止画で
        var area = (RectTransform)typeof(GameController).GetField("_area", flags).GetValue(host);
        var band = UiSkin.Rect(area, "SlamBand", Vector2.zero, new Vector2(area.sizeDelta.x * 1.2f, 96));
        UiSkin.Img(band, "Bg", Vector2.zero, new Vector2(area.sizeDelta.x * 1.2f, 96), null, new Color(0, 0, 0, 0.78f));
        var title = UiSkin.Rect(area, "SlamTitle", new Vector2(0, 2), new Vector2(area.sizeDelta.x, 96));
        IconText.Render(title, "はじめての地   {soul}+30", 36, Color.white, FontStyle.Bold, 36 * 1.05f, 1f, 4f, true, new Color(0.35f, 0.3f, 0.1f, 1f), new Vector2(0, -3));
        Shot(canvas, camera, size, Path.Combine(output, "explain-icons.png"));
        put.Invoke(host, new object[] { "リプレイでライフが 12 回復した", 1f });
        setMessage.Invoke(host, new object[] { "ENEMY DEFEATED!  EXP +10", true, (Color?)UiSkin.Gold });
        Shot(canvas, camera, size, Path.Combine(output, "explain-plain.png"));
        // エンゲージ中の帯（Web 版の写し）。敵の絵も出しておく
        UnityEngine.Object.DestroyImmediate(band.gameObject); UnityEngine.Object.DestroyImmediate(title.gameObject);
        typeof(GameController).GetMethod("HideDialogue", flags).Invoke(host, null);
        setMessage.Invoke(host, new object[] { "3G", false, null });
        var enemyImg = (Image)typeof(GameController).GetField("_enemyImg", flags).GetValue(host);
        var enemyCg = (CanvasGroup)typeof(GameController).GetField("_enemyCg", flags).GetValue(host);
        enemyImg.sprite = ArtLoader.EnemySprite("slime"); enemyCg.alpha = 1f;
        typeof(GameController).GetMethod("ShowEngageBanners", flags).Invoke(host, null);
        var top = area.Find("EngageBandTop");
        var dump = new System.Text.StringBuilder();
        if (top != null)
        {
            var rt = (RectTransform)top; dump.AppendLine($"band pos {rt.anchoredPosition} size {rt.sizeDelta} rot {rt.localEulerAngles} active {top.gameObject.activeInHierarchy} sibling {top.GetSiblingIndex()} / {area.childCount}");
            foreach (Transform c in top) { var crt = (RectTransform)c; var tx = c.GetComponent<Text>(); dump.AppendLine($"  {c.name} pos {crt.anchoredPosition} size {crt.sizeDelta} text {(tx != null ? tx.text.Length.ToString() + " pw " + tx.preferredWidth : "-")}"); }
        }
        else dump.AppendLine("band not found");
        File.WriteAllText(Path.Combine(output, "engage-dump.txt"), dump.ToString());
        Shot(canvas, camera, size, Path.Combine(output, "engage-banners.png"));
        // 継続ジャッジ中のチップ
        typeof(GameController).GetMethod("HideEngageBanners", flags).Invoke(host, null);
        machine.InAt = true; machine.InJudge = true; machine.JudgeRemaining = 5; machine.JudgeTotal = 8; machine.AtPayout = 312; machine.AtSet = 2;
        typeof(GameController).GetMethod("RefreshUi", flags).Invoke(host, null);
        Shot(canvas, camera, size, Path.Combine(output, "judge-chip.png"));
        // ジャッジのマス: 3G 進んだ状態（白 / 無し / 赤）
        machine.JudgeRemaining = 8;
        typeof(GameController).GetMethod("ShowJudgeTrack", flags).Invoke(host, null);
        var upd = typeof(GameController).GetMethod("UpdateJudgeTrack", flags);
        machine.JudgeRemaining = 7; upd.Invoke(host, new object[] { new GameResult { judgeHint = "white" } });
        machine.JudgeRemaining = 6; upd.Invoke(host, new object[] { new GameResult { judgeHint = null } });
        machine.JudgeRemaining = 5; upd.Invoke(host, new object[] { new GameResult { judgeHint = "red" } });
        foreach (var rt in canvas.GetComponentsInChildren<RectTransform>()) if (rt.name.StartsWith("Cell")) rt.localScale = Vector3.one;
        Shot(canvas, camera, size, Path.Combine(output, "judge-track.png"));
        // 技術介入の「狙え！」（左リールに赤7 を中段ビタ）
        typeof(GameController).GetMethod("HideJudgeTrack", flags).Invoke(host, null);
        machine.InAt = false; machine.InJudge = false;
        machine.Tech = new TechChallenge { kind = TechKind.Vita, id = "vita", reel = 0, symbol = Symbol.RED7, row = 1 };
        typeof(GameController).GetMethod("ShowTechAim", flags).Invoke(host, null);
        foreach (var rt in canvas.GetComponentsInChildren<RectTransform>()) if (rt.name == "TechAim" || rt.name == "Aim") { rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity; }
        Shot(canvas, camera, size, Path.Combine(output, "tech-aim.png"));
        // 役の点滅（暗い側の瞬間）: 中段の 3 コマを暗く
        typeof(GameController).GetMethod("HideTechAim", flags).Invoke(host, null);
        var reelsArr = (ReelView[])typeof(GameController).GetField("_reels", flags).GetValue(host);
        foreach (var rv in reelsArr) rv.SetRowBrightness(1, 0.28f);
        Shot(canvas, camera, size, Path.Combine(output, "blink-dim.png"));
        // 「8 EMB 獲得！」の帯: 本物のコルーチンを 1 手だけ進めて（帯を作った直後）中央に置く
        {
            var en = (System.Collections.IEnumerator)typeof(GameController).GetMethod("GainSlide", flags).Invoke(host, new object[] { "ember", 8, null, 0f, 0f, 1f });
            en.MoveNext();
            var eband = area.Find("EmberGain") as RectTransform;
            if (eband != null) eband.anchoredPosition = new Vector2(0, -8f);
            Shot(canvas, camera, size, Path.Combine(output, "ember-gain.png"));
            if (eband != null) UnityEngine.Object.DestroyImmediate(eband.gameObject);
            // 後ろの炎 ON、炎と「獲得」をずらし、数え上げの途中（15 枚のうち 1 桁）で止めた絵
            var fx = machine.Config.reelFx.emberGain;
            fx.backIcon = true; fx.backIconRot = 25f; fx.iconY = 6f; fx.picX = -8f; fx.picY = -4f; fx.countUp = false; fx.style = "pop"; fx.digitGap = -8f; fx.iconGap = 0f; fx.picGap = 2f; fx.numRot = 6f; fx.iconRot = 25f; fx.picRot = -5f; fx.shadow = true; fx.shadowX = 4f; fx.shadowY = -4f; fx.outline = true; fx.outlineSize = 2.5f;
            en = (System.Collections.IEnumerator)typeof(GameController).GetMethod("GainSlide", flags).Invoke(host, new object[] { "book", 15, null, 0f, 0f, 1f });
            en.MoveNext();
            eband = area.Find("EmberGain") as RectTransform;
            if (eband != null) { eband.anchoredPosition = new Vector2(0, -8f); eband.localScale = Vector3.one; }
            Shot(canvas, camera, size, Path.Combine(output, "ember-gain-back.png"));
            if (eband != null) UnityEngine.Object.DestroyImmediate(eband.gameObject);
            // 単位つき（AT +5G）: 数字の右に金の「G」
            fx.digitBounce = false; fx.countUp = false;
            en = (System.Collections.IEnumerator)typeof(GameController).GetMethod("GainSlide", flags).Invoke(host, new object[] { null, 5, "G", 0f, 0f, 1f });
            en.MoveNext();
            eband = area.Find("EmberGain") as RectTransform;
            if (eband != null) { eband.anchoredPosition = new Vector2(0, -8f); eband.localScale = Vector3.one; }
            Shot(canvas, camera, size, Path.Combine(output, "gain-games.png"));
            if (eband != null) UnityEngine.Object.DestroyImmediate(eband.gameObject);
        }
        // 技術介入のランク「Perfect!!」を中リールの上に（1 手進めてから見える状態に整える）
        {
            var stage = (RectTransform)typeof(GameController).GetField("_stage", flags).GetValue(host);
            var rank = machine.Config.tech.ranks[0];
            var en = (System.Collections.IEnumerator)typeof(GameController).GetMethod("TechRankPop", flags).Invoke(host, new object[] { 1, rank, 0f });
            en.MoveNext();
            var rk = stage.Find("TechRank") as RectTransform;
            if (rk != null) { rk.localScale = Vector3.one; var cg = rk.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = 1f; }
            Shot(canvas, camera, size, Path.Combine(output, "tech-rank.png"));
        }
        // 中ボスの体力バー（棚 c04）: エンゲージ 2G 目、体力 40%
        {
            var boss = new EnemyTable { name = "ゴブリンの王", group = "boss", enemyType = "goblin" };
            machine.ActiveEnemyTable = boss; machine.IsTier2 = true; machine.EnemyActive = true; machine.Tier2SpinCount = 1;
            typeof(GameController).GetField("_engagedBoss", flags).SetValue(host, true);
            typeof(GameController).GetField("_engagedName", flags).SetValue(host, boss.name);
            typeof(GameController).GetField("_bossHp", flags).SetValue(host, 0.4f);
            typeof(GameController).GetMethod("RefreshUi", flags).Invoke(host, null);
            Shot(canvas, camera, size, Path.Combine(output, "boss-bar.png"));
            machine.IsTier2 = false; machine.EnemyActive = false;
        }
        Debug.Log("EXPLAIN_UI_PROBE done");
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
