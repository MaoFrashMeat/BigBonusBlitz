using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BBB.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 確率エディタ。BBB > Probability Editor。
/// タブ: 小役確率（A/B/C/D/BB/RB × 設定1〜6、Tier2）／天井・モード移行／ワークフロー／エネミー。
/// 編集対象は Resources/Data の JSON（game_config / workflow_config / enemy_tables）。
/// </summary>
public sealed class ProbabilityEditorWindow : EditorWindow
{
    private const string GamePath = "Assets/Resources/Data/game_config.json";
    private const string WorkflowPath = "Assets/Resources/Data/workflow_config.json";
    private const string EnemyPath = "Assets/Resources/Data/enemy_tables.json";
    private const int Denom = 65536;

    private static readonly string[] Tabs = { "小役確率", "天井・モード移行", "ワークフロー", "エネミー", "エンゲージ示唆", "旅人", "予告", "主人公", "AT期待度", "AT洞窟", "冒険", "ステータス", "物語" };
    private static readonly string[] TableKeys = { "probabilities_A", "probabilities_B", "probabilities_C", "probabilities_D", "probabilities_BB", "probabilities_RB" };
    private static readonly string[] TableNames = { "モードA", "モードB", "モードC", "モードD", "BB中", "RB中" };
    private static readonly string[] FlagKeys = Enum.GetNames(typeof(Flag));
    private static readonly string[] Roles = { "HAZE", "BELL", "REPLAY", "CHERRY", "SUICA", "CHANCE", "BONUS" };
    private static readonly string[] Cats = { "SERIF", "ENEMY", "ZONE", "ACTION" };
    private static readonly string[] Vars = { "A", "B", "C", "D", "E", "F" };
    private static readonly string[] DefeatKeys = { "BELL", "CHERRY", "WATERMELON", "CHANCE", "REPLAY" };

    private JObject _game, _wf, _enemy;
    private int _tab, _table;
    private Vector2 _scroll;
    private bool _dirty;
    private string _status = "";
    private GUIStyle _num, _numBad;

    [MenuItem("BBB/Probability Editor")]
    public static void Open() => GetWindow<ProbabilityEditorWindow>("BBB Probability Editor");

    private void OnEnable() => Load();

    private void Load()
    {
        try
        {
            _game = JObject.Parse(File.ReadAllText(GamePath));
            _wf = JObject.Parse(File.ReadAllText(WorkflowPath));
            _enemy = JObject.Parse(File.ReadAllText(EnemyPath));
            _status = "読み込み完了";
        }
        catch (Exception e) { _status = "読み込み失敗: " + e.Message; }
        _dirty = false;
    }

    private void Save()
    {
        File.WriteAllText(GamePath, _game.ToString(Formatting.Indented) + "\n");
        File.WriteAllText(WorkflowPath, _wf.ToString(Formatting.Indented) + "\n");
        File.WriteAllText(EnemyPath, _enemy.ToString(Formatting.Indented) + "\n");
        AssetDatabase.ImportAsset(GamePath); AssetDatabase.ImportAsset(WorkflowPath); AssetDatabase.ImportAsset(EnemyPath);
        _dirty = false;
        _status = "保存しました（次の Play から反映）";
    }

    private void EnsureStyles()
    {
        if (_num != null) return;
        _num = new GUIStyle(EditorStyles.numberField) { alignment = TextAnchor.MiddleRight };
        _numBad = new GUIStyle(_num);
        _numBad.normal.textColor = new Color(1f, 0.4f, 0.4f);
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (_game == null) { EditorGUILayout.HelpBox(_status, MessageType.Error); if (GUILayout.Button("再読込")) Load(); return; }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("再読込", GUILayout.Width(70))) { if (!_dirty || EditorUtility.DisplayDialog("再読込", "未保存の変更を捨てますか？", "捨てる", "戻る")) Load(); }
            using (new EditorGUI.DisabledScope(!_dirty)) if (GUILayout.Button("保存", GUILayout.Width(70))) Save();
            GUILayout.Space(12);
            _tab = GUILayout.Toolbar(_tab, Tabs, GUILayout.Width(1160));
        }
        EditorGUILayout.HelpBox(_status + (_dirty ? "  [未保存]" : ""), MessageType.None);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: DrawSymbolProbs(); break;
            case 1: DrawCeilingAndModes(); break;
            case 2: DrawWorkflow(); break;
            case 3: DrawEnemies(); break;
            case 4: DrawEngageHints(); break;
            case 5: DrawTravelers(); break;
            case 6: DrawPrecog(); break;
            case 7: DrawHero(); break;
            case 8: DrawAtExpect(); break;
            case 9: DrawAtCave(); break;
            case 10: DrawAdventure(); break;
            case 11: DrawStats(); break;
            case 12: DrawStory(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ 小役確率
    private void DrawSymbolProbs()
    {
        _table = GUILayout.Toolbar(_table, TableNames.Concat(new[] { "Tier2(共通)", "AT(洞窟)" }).ToArray(), GUILayout.Width(680));
        EditorGUILayout.Space(4);
        bool tier2 = _table == TableNames.Length;
        bool atTable = _table == TableNames.Length + 1;

        if (atTable)
        {
            var t = (JObject)_game["probabilities_AT"];
            if (t == null) { EditorGUILayout.HelpBox("probabilities_AT が無い。game_config.json に追加してください。", MessageType.Warning); return; }
            DrawTable(new[] { ("AT(洞窟)", t) });
            EditorGUILayout.HelpBox("AT 中はこの表を使う。ボーナス役を 0 にしておくと AT 中にボーナスが割り込まない。", MessageType.None);
        }
        else if (tier2)
        {
            var t = (JObject)_game["probabilities_Tier2"];
            DrawTable(new[] { ("共通", t) });
        }
        else
        {
            var set = (JObject)_game[TableKeys[_table]];
            var cols = Enumerable.Range(1, 6).Select(i => ($"設定{i}", (JObject)set[i.ToString()])).ToArray();
            DrawTable(cols);
        }
    }

    /// <summary>フラグ行 × 列（設定）の表。合計 65536 チェックと合算表示付き。</summary>
    private void DrawTable((string name, JObject obj)[] cols)
    {
        const float w = 86f;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("フラグ", EditorStyles.boldLabel, GUILayout.Width(90));
            foreach (var c in cols) GUILayout.Label(c.name, EditorStyles.boldLabel, GUILayout.Width(w));
        }
        foreach (var key in FlagKeys)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, GUILayout.Width(90));
                foreach (var c in cols)
                {
                    int v = c.obj[key]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(w));
                    if (nv != v) { c.obj[key] = Math.Max(0, nv); _dirty = true; }
                }
            }
        }
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("合計", EditorStyles.boldLabel, GUILayout.Width(90));
            foreach (var c in cols)
            {
                int sum = Sum(c.obj);
                var prev = GUI.contentColor;
                GUI.contentColor = sum == Denom ? Color.green : new Color(1f, 0.4f, 0.4f);
                GUILayout.Label(sum.ToString(), GUILayout.Width(w));
                GUI.contentColor = prev;
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("", GUILayout.Width(90));
            foreach (var c in cols)
                if (GUILayout.Button("HAZE で調整", GUILayout.Width(w)))
                {
                    int others = Sum(c.obj) - (c.obj["HAZE"]?.Value<int>() ?? 0);
                    c.obj["HAZE"] = Math.Max(0, Denom - others);
                    _dirty = true;
                }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("合算（分母 65536、1/x 表示）", EditorStyles.boldLabel);
        foreach (var (label, pred) in new (string, Func<Flag, bool>)[]
        {
            ("BB合算", f => f.IsBB()), ("RB合算", f => f.IsRB()), ("ボーナス合算", f => f.IsBonus()),
            ("REPLAY", f => f.IsReplay()), ("BELL(STAR)", f => f.IsBell()), ("CHERRY", f => f.IsCherry()),
            ("SUICA", f => f.IsSuica()), ("CHANCE", f => f.IsReachMe() || f == Flag.CHANCE_D), ("HAZE", f => f == Flag.HAZE),
        })
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(90));
                foreach (var c in cols)
                {
                    int n = 0;
                    foreach (Flag f in Enum.GetValues(typeof(Flag))) if (pred(f)) n += c.obj[f.ToString()]?.Value<int>() ?? 0;
                    GUILayout.Label(n == 0 ? "-" : $"{n} (1/{(double)Denom / n:F1})", GUILayout.Width(w + 30));
                }
            }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("各列の合計が 65536 でないと抽選テーブルが作れません（赤表示）。「HAZE で調整」でハズレに残りを吸収させます。", MessageType.None);
    }

    private static int Sum(JObject o) => o.Properties().Sum(p => p.Value.Type == JTokenType.Integer ? p.Value.Value<int>() : 0);

    // ------------------------------------------------------ 天井・モード移行
    private void DrawCeilingAndModes()
    {
        EditorGUILayout.LabelField("天井（G数。到達で下の重みからボーナスを強制当選）", EditorStyles.boldLabel);
        var ceil = (JObject)_game["ceilings"];
        using (new EditorGUILayout.HorizontalScope())
            foreach (var m in new[] { "A", "B", "C", "D" })
            {
                GUILayout.Label("モード" + m, GUILayout.Width(60));
                int v = ceil[m]?.Value<int>() ?? 999;
                int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(70));
                if (nv != v) { ceil[m] = Math.Max(1, nv); _dirty = true; }
                GUILayout.Space(12);
            }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("天井時のボーナス重み（フラグ名 → 重み。比率で抽選）", EditorStyles.boldLabel);
        var cb = (JObject)_game["ceilingBonus"];
        if (cb == null) { cb = new JObject { ["BB_A"] = 50, ["RB_A"] = 50 }; _game["ceilingBonus"] = cb; }
        DrawWeightRow(cb, new[] { "BB_A", "BB_B", "BB_C", "BB_D", "RB_A", "RB_B" }, 60);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ボーナス引き込み・エンゲージ", EditorStyles.boldLabel);
        int pull = _game["bonusPullInSlip"]?.Value<int>() ?? 20;
        int npull = EditorGUILayout.IntSlider("持ち越し狙い時の最大滑り（4=目押し, 20=必ず揃う）", pull, 4, 20);
        if (npull != pull) { _game["bonusPullInSlip"] = npull; _dirty = true; }
        int streak = _game["defeatStreakBonus"]?.Value<int>() ?? 15;
        int nstreak = EditorGUILayout.IntSlider("小役連続ボーナス（討伐率 +% / 連続1回）", streak, 0, 50);
        if (nstreak != streak) { _game["defeatStreakBonus"] = nstreak; _dirty = true; }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Tier2（敵エンゲージ）決着までのG数", EditorStyles.boldLabel);
        int t2 = _game["tier2MaxSpins"]?.Value<int>() ?? 3;
        int nt2 = EditorGUILayout.IntField(t2, _num, GUILayout.Width(70));
        if (nt2 != t2) { _game["tier2MaxSpins"] = Math.Max(1, nt2); _dirty = true; }
        int pre = _game["enemyPrecursorSpins"]?.Value<int>() ?? 3;
        int npre = EditorGUILayout.IntField("敵出現までの前兆G数", pre, GUILayout.Width(260));
        if (npre != pre) { _game["enemyPrecursorSpins"] = Math.Max(1, npre); _dirty = true; }
        var bc = (JObject)_game["bellCommand"];
        if (bc == null) { bc = new JObject { ["successGuaranteesDefeat"] = true, ["successExp"] = 25, ["failPenalty"] = 0 }; _game["bellCommand"] = bc; }
        EditorGUILayout.LabelField("ベル択ナビ（エンゲージ中・第一停止=中・左右どちらかが正解）", EditorStyles.boldLabel);
        bool sg = bc["successGuaranteesDefeat"]?.Value<bool>() ?? true;
        bool nsg = EditorGUILayout.ToggleLeft("正解で討伐を内部確定（告知は3G目）", sg);
        if (nsg != sg) { bc["successGuaranteesDefeat"] = nsg; _dirty = true; }
        int se = bc["successExp"]?.Value<int>() ?? 25;
        int nse = EditorGUILayout.IntField("正解時 EXP", se, GUILayout.Width(260));
        if (nse != se) { bc["successExp"] = Math.Max(0, nse); _dirty = true; }
        int cl = bc["correctLeftRate"]?.Value<int>() ?? 50;
        int ncl = EditorGUILayout.IntSlider("正解が左になる確率 %（残りは右）", cl, 0, 100);
        if (ncl != cl) { bc["correctLeftRate"] = ncl; _dirty = true; }
        EditorGUILayout.Space(6);
        int exp = _game["expPerDefeat"]?.Value<int>() ?? 50;
        int nexp = EditorGUILayout.IntField("討伐時 EXP", exp, GUILayout.Width(200));
        if (nexp != exp) { _game["expPerDefeat"] = Math.Max(0, nexp); _dirty = true; }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("モード移行（% / 行ごとに合計100）", EditorStyles.boldLabel);
        var trans = (JObject)_game["modeTransitions"];
        foreach (var timing in new[] { ("initial", "起動時・リセット時"), ("bonus", "ボーナス当選時") })
        {
            EditorGUILayout.LabelField(timing.Item2, EditorStyles.miniBoldLabel);
            var bySetting = (JObject)trans[timing.Item1];
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(50));
                foreach (var m in new[] { "A", "B", "C", "D" }) GUILayout.Label("→" + m, EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("合計", EditorStyles.boldLabel, GUILayout.Width(50));
            }
            for (int s = 1; s <= 6; s++)
            {
                var row = (JObject)bySetting[s.ToString()];
                if (row == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("設定" + s, GUILayout.Width(50));
                    int sum = 0;
                    foreach (var m in new[] { "A", "B", "C", "D" })
                    {
                        int v = row[m]?.Value<int>() ?? 0;
                        int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(60));
                        if (nv != v) { row[m] = Math.Max(0, nv); _dirty = true; }
                        sum += Math.Max(0, nv);
                    }
                    var prev = GUI.contentColor; GUI.contentColor = sum == 100 ? Color.green : new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label(sum.ToString(), GUILayout.Width(50));
                    GUI.contentColor = prev;
                }
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("払い出し（通常 / BB中 / RB中）", EditorStyles.boldLabel);
        var pays = new[] { ("payouts", "通常"), ("payouts_BB", "BB中"), ("payouts_RB", "RB中") };
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("", GUILayout.Width(100));
            foreach (var p in pays) GUILayout.Label(p.Item2, EditorStyles.boldLabel, GUILayout.Width(70));
        }
        foreach (var key in new[] { "BIG", "REG", "STAR", "WATERMELON", "CHERRY", "REPLAY" })
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, GUILayout.Width(100));
                foreach (var p in pays)
                {
                    var o = (JObject)_game[p.Item1];
                    int v = o[key]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(70));
                    if (nv != v) { o[key] = Math.Max(0, nv); _dirty = true; }
                }
            }
        }
        EditorGUILayout.HelpBox("BIG/REG は「獲得量で終了」する目標エンバー。REPLAY は再遊技のコスト相当（エンバー表示用）。", MessageType.None);
    }

    // ----------------------------------------------------------- ワークフロー
    private void DrawWorkflow()
    {
        EditorGUILayout.HelpBox("役成立時にイベント（SERIF / ENEMY / ZONE / ACTION）を抽選。NONE + 各 rate = 100%。カテゴリ内の A〜F は 100% または 0%。", MessageType.None);
        foreach (var role in Roles)
        {
            var r = (JObject)_wf[role];
            if (r == null) continue;
            EditorGUILayout.Space(6);
            int none = r["NONE"]?.Value<int>() ?? 0;
            int sum = none + Cats.Sum(c => ((JObject)r[c])?["rate"]?.Value<int>() ?? 0);
            var prev = GUI.contentColor; GUI.contentColor = sum == 100 ? Color.green : new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.LabelField($"{role}   （合計 {sum}%）", EditorStyles.boldLabel);
            GUI.contentColor = prev;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("NONE %", GUILayout.Width(80));
                int nv = EditorGUILayout.IntField(none, _num, GUILayout.Width(60));
                if (nv != none) { r["NONE"] = Mathf.Clamp(nv, 0, 100); _dirty = true; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(80));
                GUILayout.Label("rate %", EditorStyles.boldLabel, GUILayout.Width(60));
                foreach (var v in Vars) GUILayout.Label(v + " %", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("A-F合計", EditorStyles.boldLabel, GUILayout.Width(60));
            }
            foreach (var cat in Cats)
            {
                var c = (JObject)r[cat];
                if (c == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(cat, GUILayout.Width(80));
                    int rate = c["rate"]?.Value<int>() ?? 0;
                    int nr = EditorGUILayout.IntField(rate, _num, GUILayout.Width(60));
                    if (nr != rate) { c["rate"] = Mathf.Clamp(nr, 0, 100); _dirty = true; }
                    int vs = 0;
                    foreach (var v in Vars)
                    {
                        int x = c[v]?.Value<int>() ?? 0;
                        int nx = EditorGUILayout.IntField(x, _num, GUILayout.Width(50));
                        if (nx != x) { c[v] = Mathf.Clamp(nx, 0, 100); _dirty = true; }
                        vs += Mathf.Clamp(nx, 0, 100);
                    }
                    var p2 = GUI.contentColor; GUI.contentColor = (vs == 100 || vs == 0) ? Color.green : new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label(vs.ToString(), GUILayout.Width(60));
                    GUI.contentColor = p2;
                }
            }
        }
    }

    // ---------------------------------------------------------------- 旅人
    private void DrawTravelers()
    {
        var tv = (JObject)_game["travelers"];
        if (tv == null) { EditorGUILayout.HelpBox("travelers が無い。一度 Play すると既定値で動く。保存するには game_config.json に travelers を追加してください。", MessageType.Warning); return; }
        int ar = tv["appearanceRate"]?.Value<int>() ?? 6;
        int nar = EditorGUILayout.IntField("通常時の出現率 %/G", ar, GUILayout.Width(260));
        if (nar != ar) { tv["appearanceRate"] = Mathf.Clamp(nar, 0, 100); _dirty = true; }
        string hn = (string)tv["heroName"] ?? "主人公";
        string nhn = EditorGUILayout.TextField("主人公の名前（会話UIのタグ）", hn, GUILayout.Width(360));
        if (nhn != hn) { tv["heroName"] = nhn; _dirty = true; }
        string hr = string.Join(";", ((JArray)tv["hintReplies"] ?? new JArray()).Select(x => (string)x));
        string nhr = EditorGUILayout.TextField("示唆セリフへの主人公の返事（; 区切り）", hr, GUILayout.Width(760));
        if (nhr != hr) { tv["hintReplies"] = new JArray(nhr.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("旅人", EditorStyles.boldLabel);
        var list = (JArray)tv["travelers"];
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("id", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.Label("名前", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("重み", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("秒", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("喋る%", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("示唆%", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("色", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("雑談（; 区切り）", EditorStyles.boldLabel, GUILayout.Width(260));
            GUILayout.Label("主人公の返事（; 区切り）", EditorStyles.boldLabel, GUILayout.Width(260));
        }
        foreach (JObject t in list)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((string)t["id"], GUILayout.Width(30));
                string nm = (string)t["name"] ?? ""; string nnm = EditorGUILayout.TextField(nm, GUILayout.Width(90)); if (nnm != nm) { t["name"] = nnm; _dirty = true; }
                int w = t["weight"]?.Value<int>() ?? 0; int nw = EditorGUILayout.IntField(w, _num, GUILayout.Width(50)); if (nw != w) { t["weight"] = Math.Max(0, nw); _dirty = true; }
                float ws = t["walkSeconds"]?.Value<float>() ?? 6f; float nws = EditorGUILayout.FloatField(ws, _num, GUILayout.Width(50)); if (Math.Abs(nws - ws) > 1e-3f) { t["walkSeconds"] = Math.Max(1f, nws); _dirty = true; }
                int sr = t["serifRate"]?.Value<int>() ?? 0; int nsr = EditorGUILayout.IntField(sr, _num, GUILayout.Width(50)); if (nsr != sr) { t["serifRate"] = Mathf.Clamp(nsr, 0, 100); _dirty = true; }
                int mh = t["modeHintRate"]?.Value<int>() ?? 0; int nmh = EditorGUILayout.IntField(mh, _num, GUILayout.Width(50)); if (nmh != mh) { t["modeHintRate"] = Mathf.Clamp(nmh, 0, 100); _dirty = true; }
                string col = (string)t["color"] ?? "#ffffff"; string ncol = EditorGUILayout.TextField(col, GUILayout.Width(80)); if (ncol != col) { t["color"] = ncol; _dirty = true; }
                string ch = string.Join(";", ((JArray)t["chatter"] ?? new JArray()).Select(x => (string)x));
                string nch = EditorGUILayout.TextField(ch, GUILayout.Width(260));
                if (nch != ch) { t["chatter"] = new JArray(nch.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
                string rp = string.Join(";", ((JArray)t["replies"] ?? new JArray()).Select(x => (string)x));
                string nrp = EditorGUILayout.TextField(rp, GUILayout.Width(260));
                if (nrp != rp) { t["replies"] = new JArray(nrp.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
            }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("モード示唆セリフ（各モードでの重み。比率で抽選）", EditorStyles.boldLabel);
        var ms = (JArray)tv["modeSerifs"];
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("セリフ", EditorStyles.boldLabel, GUILayout.Width(260));
            foreach (var m in new[] { "A", "B", "C", "D" }) GUILayout.Label("モード" + m, EditorStyles.boldLabel, GUILayout.Width(60));
        }
        for (int i = 0; i < ms.Count; i++)
        {
            var o = (JObject)ms[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                string tx = (string)o["text"] ?? ""; string ntx = EditorGUILayout.TextField(tx, GUILayout.Width(260)); if (ntx != tx) { o["text"] = ntx; _dirty = true; }
                foreach (var m in new[] { "A", "B", "C", "D" })
                {
                    int v = o[m]?.Value<int>() ?? 0; int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(60)); if (nv != v) { o[m] = Math.Max(0, nv); _dirty = true; }
                }
                if (GUILayout.Button("×", GUILayout.Width(24))) { ms.RemoveAt(i); _dirty = true; return; }
            }
        }
        if (GUILayout.Button("セリフを追加", GUILayout.Width(120))) { ms.Add(new JObject { ["text"] = "新しいセリフ", ["A"] = 25, ["B"] = 25, ["C"] = 25, ["D"] = 25 }); _dirty = true; }
        EditorGUILayout.HelpBox("信頼度の目安: そのセリフの重みが特定モードに偏るほど強い示唆。モードD 100 / 他 0 なら「D 確定」。", MessageType.None);
    }

    // ------------------------------------------------------- エンゲージ示唆
    private void DrawEngageHints()
    {
        EditorGUILayout.HelpBox("案B（潜伏当否型）の段階示唆。各行は重み（合計は何でもよい。比率で抽選）。won=内部当選している時 / lost=していない時。\n1G目: 敵の色   2G目: セリフ   3G目: 停止音", MessageType.None);
        var eh = (JObject)_game["engageHints"];
        if (eh == null) { eh = new JObject(); _game["engageHints"] = eh; }
        DrawHintStage(eh, "stage1Color", "1G目: 敵の色", new[] { "none", "blue", "yellow", "green", "red", "rainbow" }, new[] { "なし", "青", "黄", "緑", "赤", "虹" });
        DrawHintStage(eh, "stage2Serif", "2G目: セリフ", new[] { "none", "weak", "mid", "strong" }, new[] { "なし", "…なにか来る", "もらった…！", "激熱ッ！" });
        DrawHintStage(eh, "stage3Sound", "3G目: 停止音", new[] { "normal", "hot" }, new[] { "通常", "高音" });
    }

    private void DrawHintStage(JObject eh, string key, string title, string[] keys, string[] labels)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        var st = (JObject)eh[key];
        if (st == null) { st = new JObject { ["won"] = new JObject(), ["lost"] = new JObject() }; eh[key] = st; }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("", GUILayout.Width(60));
            foreach (var l in labels) GUILayout.Label(l, EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("当選時%", EditorStyles.boldLabel, GUILayout.Width(80));
        }
        foreach (var side in new[] { ("won", "当選中"), ("lost", "非当選") })
        {
            var row = (JObject)st[side.Item1];
            if (row == null) { row = new JObject(); st[side.Item1] = row; }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(side.Item2, GUILayout.Width(60));
                int sum = 0;
                foreach (var k in keys)
                {
                    int v = row[k]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(90));
                    if (nv != v) { row[k] = Math.Max(0, nv); _dirty = true; }
                    sum += Math.Max(0, nv);
                }
                GUILayout.Label(sum == 0 ? "-" : "計 " + sum, GUILayout.Width(80));
            }
        }
        // 信頼度（その示唆が出たとき当選している確率）: 事前確率は不明なので won/lost が等確率と仮定した参考値
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("信頼度*", GUILayout.Width(60));
            var won = (JObject)st["won"]; var lost = (JObject)st["lost"];
            int ws = keys.Sum(k => won[k]?.Value<int>() ?? 0), ls = keys.Sum(k => lost[k]?.Value<int>() ?? 0);
            foreach (var k in keys)
            {
                double pw = ws > 0 ? (double)(won[k]?.Value<int>() ?? 0) / ws : 0, pl = ls > 0 ? (double)(lost[k]?.Value<int>() ?? 0) / ls : 0;
                string txt = pw + pl > 0 ? $"{pw / (pw + pl) * 100:F0}%" : "-";
                GUILayout.Label(txt, GUILayout.Width(90));
            }
        }
        EditorGUILayout.LabelField("* 当選/非当選が同確率と仮定した参考値。実際の信頼度は討伐率と役の出現率で変わる", EditorStyles.miniLabel);
    }

    // ------------------------------------------------------------- エネミー
    private void DrawEnemies()
    {
        var tables = (JArray)_enemy["tables"];
        EditorGUILayout.HelpBox("討伐率: 3G の間にその役を引いたとき一撃討伐（内部当選）する %。Tier2 小役確率: 敵と対峙中の専用テーブル（合計 65536）。variant: どのバリアントで出現するか（空/ANY=指定なし）。", MessageType.None);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("テーブルを追加", GUILayout.Width(120)))
            {
                var t = new JObject
                {
                    ["id"] = "table_new_" + DateTime.Now.Ticks % 10000,
                    ["name"] = "新しい敵",
                    ["enemyType"] = "slime",
                    ["defeatProbabilities"] = new JObject { ["BELL"] = 5, ["CHERRY"] = 20, ["WATERMELON"] = 40, ["CHANCE"] = 100, ["REPLAY"] = 1 },
                    ["tier2Probabilities"] = (JObject)_game["probabilities_Tier2"].DeepClone(),
                    ["variant"] = "ANY",
                };
                tables.Add(t); _dirty = true;
            }
        }
        for (int i = 0; i < tables.Count; i++)
        {
            var t = (JObject)tables[i];
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string name = (string)t["name"] ?? "";
                    string nn = EditorGUILayout.TextField("名前", name);
                    if (nn != name) { t["name"] = nn; _dirty = true; }
                    if (GUILayout.Button("削除", GUILayout.Width(50)) && EditorUtility.DisplayDialog("削除", $"{name} を削除しますか？", "削除", "戻る")) { tables.RemoveAt(i); _dirty = true; return; }
                }
                string id = (string)t["id"] ?? "";
                string nid = EditorGUILayout.TextField("id", id);
                if (nid != id) { t["id"] = nid; _dirty = true; }
                string et = (string)t["enemyType"] ?? "slime";
                int ei = Array.IndexOf(new[] { "slime", "goblin", "bat" }, et); if (ei < 0) ei = 0;
                int nei = EditorGUILayout.Popup("見た目", ei, new[] { "slime", "goblin", "bat" });
                if (nei != ei) { t["enemyType"] = new[] { "slime", "goblin", "bat" }[nei]; _dirty = true; }
                string variant = (string)t["variant"] ?? "ANY";
                int vi = Array.IndexOf(new[] { "ANY", "A", "B", "C", "D", "E", "F" }, variant); if (vi < 0) vi = 0;
                int nvi = EditorGUILayout.Popup("出現バリアント", vi, new[] { "ANY", "A", "B", "C", "D", "E", "F" });
                if (nvi != vi) { t["variant"] = new[] { "ANY", "A", "B", "C", "D", "E", "F" }[nvi]; _dirty = true; }

                EditorGUILayout.LabelField("討伐率 %", EditorStyles.miniBoldLabel);
                var dp = (JObject)t["defeatProbabilities"] ?? new JObject();
                t["defeatProbabilities"] = dp;
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var k in DefeatKeys)
                    {
                        GUILayout.Label(k, GUILayout.Width(80));
                        int v = dp[k]?.Value<int>() ?? 0;
                        int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(50));
                        if (nv != v) { dp[k] = Mathf.Clamp(nv, 0, 100); _dirty = true; }
                    }

                var hc = (JObject)t["hintConfig"];
                bool hasHint = hc != null;
                bool nh = EditorGUILayout.ToggleLeft("示唆演出を個別設定（未設定なら既定: 発生70% / 赤発光40 / セリフ30 / 両方30）", hasHint);
                if (nh != hasHint)
                {
                    if (nh) t["hintConfig"] = new JObject { ["appearanceRate"] = 70, ["distribution"] = new JObject { ["redGlow"] = 40, ["textOnly"] = 30, ["both"] = 30 } };
                    else t.Remove("hintConfig");
                    _dirty = true; hc = (JObject)t["hintConfig"];
                }
                if (hc != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        int ar = hc["appearanceRate"]?.Value<int>() ?? 70;
                        int nar = EditorGUILayout.IntField("発生率 %", ar, GUILayout.Width(200));
                        if (nar != ar) { hc["appearanceRate"] = Mathf.Clamp(nar, 0, 100); _dirty = true; }
                        var d = (JObject)hc["distribution"] ?? new JObject(); hc["distribution"] = d;
                        foreach (var k in new[] { ("redGlow", "赤発光"), ("textOnly", "セリフ"), ("both", "両方") })
                        {
                            GUILayout.Label(k.Item2, GUILayout.Width(44));
                            int v = d[k.Item1]?.Value<int>() ?? 0;
                            int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(44));
                            if (nv != v) { d[k.Item1] = Math.Max(0, nv); _dirty = true; }
                        }
                    }
                }

                var t2 = (JObject)t["tier2Probabilities"];
                if (t2 == null) { t2 = (JObject)_game["probabilities_Tier2"].DeepClone(); t["tier2Probabilities"] = t2; _dirty = true; }
                bool open = EditorPrefs.GetBool("bbb.enemy.open." + id, false);
                bool nopen = EditorGUILayout.Foldout(open, $"Tier2 小役確率（合計 {Sum(t2)}）", true);
                if (nopen != open) EditorPrefs.SetBool("bbb.enemy.open." + id, nopen);
                if (nopen) DrawTable(new[] { ("この敵", t2) });
            }
        }
    }

    // ------------------------------------------------------------ 共通
    /// <summary>キー → 重み の 1 行（比率で抽選するもの用）。合計と各キーの % を併記。</summary>
    private void DrawWeightRow(JObject o, string[] keys, float w = 70)
    {
        int total = keys.Sum(k => Math.Max(0, o[k]?.Value<int>() ?? 0));
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var k in keys)
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(w)))
                {
                    GUILayout.Label(k, EditorStyles.miniBoldLabel, GUILayout.Width(w));
                    int v = o[k]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(w));
                    if (nv != v) { o[k] = Math.Max(0, nv); _dirty = true; }
                    GUILayout.Label(total > 0 ? $"{100f * Math.Max(0, v) / total:F1}%" : "-", EditorStyles.miniLabel, GUILayout.Width(w));
                }
            }
        }
    }

    // ------------------------------------------------------------ 予告
    private static readonly string[] PrecogKeys = { "BELL", "REPLAY", "CHERRY", "SUICA", "CHANCE", "BONUS", "HAZE" };
    private static readonly string[] PrecogNames = { "ベル", "リプレイ", "チェリー", "スイカ", "チャンス目", "ボーナス", "ハズレ(ガセ)" };

    private void DrawPrecog()
    {
        var pc = (JObject)_game["precog"];
        if (pc == null)
        {
            pc = new JObject { ["byFlag"] = new JObject(), ["fakeRole"] = new JObject { ["BELL"] = 40, ["REPLAY"] = 30, ["CHERRY"] = 15, ["SUICA"] = 10, ["CHANCE"] = 5 } };
            _game["precog"] = pc; _dirty = true;
        }
        var by = (JObject)pc["byFlag"] ?? (JObject)(pc["byFlag"] = new JObject());
        EditorGUILayout.LabelField("事前察知（レバーオン時の予告）。役ごとに 無し / 弱 / 強 の重み。右は出現率", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("役", EditorStyles.boldLabel, GUILayout.Width(110));
            foreach (var h in new[] { "無し", "弱", "強" }) GUILayout.Label(h, EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("何か出る%", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("強%", EditorStyles.boldLabel, GUILayout.Width(60));
        }
        for (int i = 0; i < PrecogKeys.Length; i++)
        {
            var row = (JObject)by[PrecogKeys[i]];
            if (row == null) { row = new JObject { ["none"] = 100, ["weak"] = 0, ["strong"] = 0 }; by[PrecogKeys[i]] = row; _dirty = true; }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(PrecogNames[i], GUILayout.Width(110));
                foreach (var k in new[] { "none", "weak", "strong" })
                {
                    int v = row[k]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(70));
                    if (nv != v) { row[k] = Math.Max(0, nv); _dirty = true; }
                }
                int n0 = row["none"]?.Value<int>() ?? 0, n1 = row["weak"]?.Value<int>() ?? 0, n2 = row["strong"]?.Value<int>() ?? 0;
                int tot = n0 + n1 + n2;
                GUILayout.Label(tot > 0 ? $"{100f * (n1 + n2) / tot:F1}%" : "-", GUILayout.Width(80));
                GUILayout.Label(tot > 0 ? $"{100f * n2 / tot:F1}%" : "-", GUILayout.Width(60));
            }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ハズレで予告が出たとき、どの役の色に化けるか（ガセの内訳）", EditorStyles.boldLabel);
        var fake = (JObject)pc["fakeRole"] ?? (JObject)(pc["fakeRole"] = new JObject());
        DrawWeightRow(fake, new[] { "BELL", "REPLAY", "CHERRY", "SUICA", "CHANCE" }, 70);
        EditorGUILayout.HelpBox("信頼度 = その色の予告が出たとき本当にその役である確率。ガセ（ハズレ行）の重みを上げるほど信頼度は落ちる。色は役ごとに固定（金/青/桃/緑/虹/白）。", MessageType.None);
    }

    // ------------------------------------------------------------ AT 洞窟
    private static readonly string[] AtRoles2 = { "BELL", "REPLAY", "CHERRY", "SUICA", "CHANCE", "HAZE" };
    private static readonly string[] AtRoleNames2 = { "ベル", "リプレイ", "チェリー", "スイカ", "チャンス目", "ハズレ" };

    private void DrawAtCave()
    {
        var at = (JObject)_game["at"];
        if (at == null) { EditorGUILayout.HelpBox("at が無い。game_config.json に追加してください。", MessageType.Warning); return; }

        EditorGUILayout.LabelField("AT「洞窟」の基本", EditorStyles.boldLabel);
        int init = at["initialSpins"]?.Value<int>() ?? 50;
        int ninit = EditorGUILayout.IntField("初期G数", init, GUILayout.Width(300));
        if (ninit != init) { at["initialSpins"] = Math.Max(1, ninit); _dirty = true; }
        bool ue = at["useExpectAsRate"]?.Value<bool>() ?? true;
        bool nue = EditorGUILayout.ToggleLeft("AT 期待度 % をそのまま当選率にする（枠の色が嘘にならない）", ue);
        if (nue != ue) { at["useExpectAsRate"] = nue; _dirty = true; }
        using (new EditorGUI.DisabledScope(nue))
        {
            int fr = at["flatRate"]?.Value<int>() ?? 20;
            int nfr = EditorGUILayout.IntSlider("固定当選率 %（上のチェックを外したとき）", fr, 0, 100);
            if (nfr != fr) { at["flatRate"] = nfr; _dirty = true; }
        }
        int cp = at["naviCorrectPayout"]?.Value<int>() ?? 15;
        int ncp = EditorGUILayout.IntField("押し順ナビ 正解の払い出し（エンバー）", cp, GUILayout.Width(300));
        if (ncp != cp) { at["naviCorrectPayout"] = Math.Max(0, ncp); _dirty = true; }
        int wp = at["naviWrongPayout"]?.Value<int>() ?? 3;
        int nwp = EditorGUILayout.IntField("ナビを外したときの払い出し（こぼし）", wp, GUILayout.Width(300));
        if (nwp != wp) { at["naviWrongPayout"] = Math.Max(0, nwp); _dirty = true; }
        bool bc = at["battleConsumesAtSpins"]?.Value<bool>() ?? false;
        bool nbc = EditorGUILayout.ToggleLeft("狩猟中も AT の残りGを消費する", bc);
        if (nbc != bc) { at["battleConsumesAtSpins"] = nbc; _dirty = true; }
        int fs = at["failRewardSpins"]?.Value<int>() ?? 0;
        int nfs = EditorGUILayout.IntField("討伐失敗時の救済上乗せG", fs, GUILayout.Width(300));
        if (nfs != fs) { at["failRewardSpins"] = Math.Max(0, nfs); _dirty = true; }

        // 純増の目安
        var atTable = (JObject)_game["probabilities_AT"];
        if (atTable != null)
        {
            double D = Denom;
            double bell = 0, rep = 0, che = 0, sui = 0;
            foreach (var p in atTable.Properties())
            {
                int v = p.Value.Type == JTokenType.Integer ? p.Value.Value<int>() : 0;
                if (p.Name.StartsWith("BELL")) bell += v;
                else if (p.Name.StartsWith("REPLAY")) rep += v;
                else if (p.Name.StartsWith("CHERRY")) che += v;
                else if (p.Name.StartsWith("SUICA")) sui += v;
            }
            var pay = (JObject)_game["payouts"];
            double cherryPay = pay?["CHERRY"]?.Value<int>() ?? 4, suicaPay = pay?["WATERMELON"]?.Value<int>() ?? 6;
            double gain = ncp * bell / D + cherryPay * che / D + suicaPay * sui / D - 3 * (1 - rep / D);
            double lose = nwp * bell / D + cherryPay * che / D + suicaPay * sui / D - 3 * (1 - rep / D);
            EditorGUILayout.HelpBox(
                $"AT中の小役: ベル 1/{(bell > 0 ? D / bell : 0):F2}  リプレイ 1/{(rep > 0 ? D / rep : 0):F2}\n" +
                $"純増: ナビ従 {gain:+0.00;-0.00} /G（{ninit}G で {gain * ninit:+0;-0}）  /  ナビ無視 {lose:+0.00;-0.00} /G",
                MessageType.Info);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("バトル当選率 %（役ごと）", EditorStyles.boldLabel);
        var br = (JObject)at["battleRate"];
        if (br == null) { br = new JObject(); at["battleRate"] = br; _dirty = true; }
        using (new EditorGUILayout.HorizontalScope())
            for (int i = 0; i < AtRoles2.Length; i++)
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(78)))
                {
                    GUILayout.Label(AtRoleNames2[i], EditorStyles.miniBoldLabel, GUILayout.Width(78));
                    int v = br[AtRoles2[i]]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(78));
                    if (nv != v) { br[AtRoles2[i]] = Mathf.Clamp(nv, 0, 100); _dirty = true; }
                }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ダメージ（役ごとに「ダメージ量 → 重み」。999 は一撃討伐）", EditorStyles.boldLabel);
        var dmg = (JObject)at["damage"];
        if (dmg == null) { dmg = new JObject(); at["damage"] = dmg; _dirty = true; }
        for (int i = 0; i < AtRoles2.Length; i++)
        {
            var row = (JObject)dmg[AtRoles2[i]];
            if (row == null) { row = new JObject { ["0"] = 100 }; dmg[AtRoles2[i]] = row; _dirty = true; }
            int total = row.Properties().Sum(p => Math.Max(0, p.Value.Value<int>()));
            double avg = 0;
            foreach (var p in row.Properties())
                if (int.TryParse(p.Name, out var v)) avg += Math.Min(v, 999) * Math.Max(0, p.Value.Value<int>());
            avg = total > 0 ? avg / total : 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{AtRoleNames2[i]}（平均 {avg:F1}）", GUILayout.Width(130));
                foreach (var k in row.Properties().Select(p => p.Name).OrderBy(k => int.TryParse(k, out var v) ? v : 0).ToArray())
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(58)))
                    {
                        GUILayout.Label(k == "999" ? "一撃" : k, EditorStyles.miniBoldLabel, GUILayout.Width(58));
                        int v = row[k]?.Value<int>() ?? 0;
                        int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(58));
                        if (nv != v) { row[k] = Math.Max(0, nv); _dirty = true; }
                        GUILayout.Label(total > 0 ? $"{100f * Math.Max(0, v) / total:F0}%" : "-", EditorStyles.miniLabel, GUILayout.Width(58));
                    }
                if (GUILayout.Button("+", GUILayout.Width(26)))
                {
                    int next = 5;
                    while (row[next.ToString()] != null) next += 5;
                    row[next.ToString()] = 10; _dirty = true;
                }
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("モンスター", EditorStyles.boldLabel);
        var mons = (JArray)at["monsters"];
        if (mons == null) { mons = new JArray(); at["monsters"] = mons; _dirty = true; }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("名前", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("見た目", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("重み", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("HP", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("狩猟G", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("討伐で+G", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("出現率", EditorStyles.boldLabel, GUILayout.Width(56));
        }
        int wTotal = mons.Sum(x => Math.Max(0, ((JObject)x)["weight"]?.Value<int>() ?? 0));
        for (int i = 0; i < mons.Count; i++)
        {
            var o = (JObject)mons[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                string nm = (string)o["name"] ?? ""; string nnm = EditorGUILayout.TextField(nm, GUILayout.Width(120)); if (nnm != nm) { o["name"] = nnm; _dirty = true; }
                string et = (string)o["enemyType"] ?? ""; string net = EditorGUILayout.TextField(et, GUILayout.Width(80)); if (net != et) { o["enemyType"] = net; _dirty = true; }
                int w = o["weight"]?.Value<int>() ?? 0; int nw = EditorGUILayout.IntField(w, _num, GUILayout.Width(50)); if (nw != w) { o["weight"] = Math.Max(0, nw); _dirty = true; }
                int hp = o["hp"]?.Value<int>() ?? 1; int nhp = EditorGUILayout.IntField(hp, _num, GUILayout.Width(50)); if (nhp != hp) { o["hp"] = Math.Max(1, nhp); _dirty = true; }
                int sp = o["spins"]?.Value<int>() ?? 1; int nsp2 = EditorGUILayout.IntField(sp, _num, GUILayout.Width(50)); if (nsp2 != sp) { o["spins"] = Math.Max(1, nsp2); _dirty = true; }
                int rw = o["rewardSpins"]?.Value<int>() ?? 0; int nrw = EditorGUILayout.IntField(rw, _num, GUILayout.Width(60)); if (nrw != rw) { o["rewardSpins"] = Math.Max(0, nrw); _dirty = true; }
                GUILayout.Label(wTotal > 0 ? $"{100f * w / wTotal:F1}%" : "-", GUILayout.Width(56));
                if (GUILayout.Button("×", GUILayout.Width(24))) { mons.RemoveAt(i); _dirty = true; return; }
            }
        }
        if (GUILayout.Button("モンスターを追加", GUILayout.Width(140)))
        {
            mons.Add(new JObject { ["id"] = "m_new", ["name"] = "新モンスター", ["enemyType"] = "slime", ["weight"] = 20, ["hp"] = 100, ["spins"] = 5, ["rewardSpins"] = 30 });
            _dirty = true;
        }
        EditorGUILayout.HelpBox("AT 中の小役確率は「小役確率」タブの AT(洞窟) で編集する。狩猟中は既定でATのGが減らない。", MessageType.None);
    }

    // ------------------------------------------------------------ AT期待度
    private static readonly string[] AtRoles = { "BELL", "REPLAY", "CHERRY", "SUICA", "CHANCE", "HAZE" };
    private static readonly string[] AtRoleNames = { "ベル", "リプレイ", "チェリー", "スイカ", "チャンス目", "ハズレ" };

    private void DrawAtExpect()
    {
        var at = (JObject)_game["atExpect"];
        if (at == null) { EditorGUILayout.HelpBox("atExpect が無い。game_config.json に追加してください。", MessageType.Warning); return; }
        EditorGUILayout.LabelField("ボーナス中の AT 期待度（枠の点滅色）。役ごとに「加算値 → 重み」で抽選", EditorStyles.boldLabel);
        int sp = at["startPercent"]?.Value<int>() ?? 0;
        int nsp = EditorGUILayout.IntSlider("ボーナス開始時の期待度 %", sp, 0, 100);
        if (nsp != sp) { at["startPercent"] = nsp; _dirty = true; }
        int mp = at["maxPercent"]?.Value<int>() ?? 100;
        int nmp = EditorGUILayout.IntField("期待度の上限 %", mp, GUILayout.Width(300));
        if (nmp != mp) { at["maxPercent"] = Math.Max(1, nmp); _dirty = true; }

        EditorGUILayout.Space(8);
        var gain = (JObject)at["gain"];
        if (gain == null) { gain = new JObject(); at["gain"] = gain; _dirty = true; }
        for (int i = 0; i < AtRoles.Length; i++)
        {
            var row = (JObject)gain[AtRoles[i]];
            if (row == null) { row = new JObject { ["0"] = 100 }; gain[AtRoles[i]] = row; _dirty = true; }
            int total = row.Properties().Sum(p => Math.Max(0, p.Value.Value<int>()));
            int zero = row["0"]?.Value<int>() ?? 0;
            float avg = 0f;
            foreach (var p in row.Properties())
                if (int.TryParse(p.Name, out var v)) avg += v * Math.Max(0, p.Value.Value<int>());
            avg = total > 0 ? avg / total : 0f;
            EditorGUILayout.LabelField($"{AtRoleNames[i]}    上がる確率 {(total > 0 ? 100f * (total - zero) / total : 0f):F1}%    平均 +{avg:F2}%/回", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var keys = row.Properties().Select(p => p.Name).OrderBy(k => int.TryParse(k, out var v) ? v : 0).ToArray();
                foreach (var k in keys)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(64)))
                    {
                        GUILayout.Label(k == "0" ? "上がらず" : "+" + k + "%", EditorStyles.miniBoldLabel, GUILayout.Width(64));
                        int v = row[k]?.Value<int>() ?? 0;
                        int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(64));
                        if (nv != v) { row[k] = Math.Max(0, nv); _dirty = true; }
                        GUILayout.Label(total > 0 ? $"{100f * Math.Max(0, v) / total:F1}%" : "-", EditorStyles.miniLabel, GUILayout.Width(64));
                    }
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(90)))
                {
                    GUILayout.Label("加算値を追加", EditorStyles.miniLabel, GUILayout.Width(90));
                    if (GUILayout.Button("+", GUILayout.Width(30)))
                    {
                        int next = 1;
                        while (row[next.ToString()] != null) next++;
                        row[next.ToString()] = 10; _dirty = true;
                    }
                }
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ランク（期待度 % → 枠の色。業界慣習: 白 < 青 < 黄 < 緑 < 赤 < 虹）", EditorStyles.boldLabel);
        var ranks = (JArray)at["ranks"];
        if (ranks == null) { ranks = new JArray(); at["ranks"] = ranks; _dirty = true; }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("名前", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("下限 %", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("色（HTML / rainbow）", EditorStyles.boldLabel, GUILayout.Width(180));
        }
        for (int i = 0; i < ranks.Count; i++)
        {
            var o = (JObject)ranks[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                string nm = (string)o["name"] ?? ""; string nnm = EditorGUILayout.TextField(nm, GUILayout.Width(70)); if (nnm != nm) { o["name"] = nnm; _dirty = true; }
                int mn = o["min"]?.Value<int>() ?? 0; int nmn = EditorGUILayout.IntField(mn, _num, GUILayout.Width(60)); if (nmn != mn) { o["min"] = Mathf.Clamp(nmn, 0, 1000); _dirty = true; }
                string col = (string)o["color"] ?? "#ffffff"; string ncol = EditorGUILayout.TextField(col, GUILayout.Width(180)); if (ncol != col) { o["color"] = ncol; _dirty = true; }
                if (col != "rainbow" && ColorUtility.TryParseHtmlString(col, out var swatch))
                {
                    var rc = GUILayoutUtility.GetRect(28, 16, GUILayout.Width(28));
                    EditorGUI.DrawRect(rc, swatch);
                }
                if (GUILayout.Button("×", GUILayout.Width(24))) { ranks.RemoveAt(i); _dirty = true; return; }
            }
        }
        if (GUILayout.Button("ランクを追加", GUILayout.Width(120))) { ranks.Add(new JObject { ["name"] = "新", ["min"] = 50, ["color"] = "#ffffff" }); _dirty = true; }
        EditorGUILayout.HelpBox("枠は期待度が高いほど速く・濃く点滅する。ランクが上がったGは「昇格！」を出す。AT 本体（当選・突入）はまだ未実装なので、いまは期待度の表示だけが動く。", MessageType.Info);
    }

    // ------------------------------------------------------------ 主人公
    private static readonly string[] HeroCtx = { "idle", "miss", "win", "longRun" };
    private static readonly string[] HeroCtxNames = { "何もない時", "ハズレ直後", "小役獲得直後", "ボーナス無しが長い" };

    private void DrawHero()
    {
        var h = (JObject)_game["hero"];
        if (h == null) { h = new JObject { ["monologueRate"] = 4, ["longRunSpins"] = 200, ["longRunMix"] = 50, ["lines"] = new JObject() }; _game["hero"] = h; _dirty = true; }
        EditorGUILayout.LabelField("主人公のひとりごと（通常時・敵なし・前兆なし・会話中でないとき）", EditorStyles.boldLabel);
        int mr = h["monologueRate"]?.Value<int>() ?? 4;
        int nmr = EditorGUILayout.IntSlider("1G ごとに喋る確率 %", mr, 0, 100);
        if (nmr != mr) { h["monologueRate"] = nmr; _dirty = true; }
        int ls = h["longRunSpins"]?.Value<int>() ?? 200;
        int nls = EditorGUILayout.IntField("「長い」とみなす G 数（ボーナス無し）", ls, GUILayout.Width(360));
        if (nls != ls) { h["longRunSpins"] = Math.Max(1, nls); _dirty = true; }
        int lm = h["longRunMix"]?.Value<int>() ?? 50;
        int nlm = EditorGUILayout.IntSlider("その時 longRun のセリフに差し替える確率 %", lm, 0, 100);
        if (nlm != lm) { h["longRunMix"] = nlm; _dirty = true; }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("リプレイ時のアクション重み（bento=弁当 drink=水 stretch=背伸び map=地図 hum=口笛 rest=一息 none=無し）", EditorStyles.boldLabel);
        var ra = (JObject)h["replayActions"];
        if (ra == null) { ra = new JObject { ["bento"] = 25, ["drink"] = 20, ["stretch"] = 20, ["map"] = 15, ["hum"] = 15, ["rest"] = 15, ["none"] = 10 }; h["replayActions"] = ra; _dirty = true; }
        DrawWeightRow(ra, new[] { "bento", "drink", "stretch", "map", "hum", "rest", "none" }, 64);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("セリフ（; 区切り。同じ文脈内は均等に選ばれる）", EditorStyles.boldLabel);
        var lines = (JObject)h["lines"] ?? (JObject)(h["lines"] = new JObject());
        for (int i = 0; i < HeroCtx.Length; i++)
        {
            string cur = string.Join(";", ((JArray)lines[HeroCtx[i]] ?? new JArray()).Select(x => (string)x));
            string nv = EditorGUILayout.TextField(HeroCtxNames[i], cur, GUILayout.Width(900));
            if (nv != cur) { lines[HeroCtx[i]] = new JArray(nv.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("敵出現の前兆セリフ（段階ごと。; 区切りでその中からランダム）", EditorStyles.boldLabel);
        var pl = (JObject)h["precursorLines"];
        if (pl == null)
        {
            pl = new JObject {
                ["1"] = new JArray("ん……？", "……なんだ、今の", "気のせいか……？"),
                ["2"] = new JArray("……なにかいるな", "……近づいてくる", "この気配は……"),
                ["3"] = new JArray("……来るぞ", "……そこか", "構えろ……") };
            h["precursorLines"] = pl; _dirty = true;
        }
        int stages = _game["enemyPrecursorSpins"]?.Value<int>() ?? 3;
        for (int st = 1; st <= Math.Max(3, stages); st++)
        {
            string key = st.ToString();
            string cur = string.Join(";", ((JArray)pl[key] ?? new JArray()).Select(x => (string)x));
            string nv = EditorGUILayout.TextField($"{st} G 目", cur, GUILayout.Width(900));
            if (nv != cur) { pl[key] = new JArray(nv.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
        }
        EditorGUILayout.HelpBox("主人公の名前は「旅人」タブの「主人公の名前」で変更。前兆セリフは会話UI（リールのすぐ上）に出る。", MessageType.None);
    }

    // ------------------------------------------------------------ 物語
    private static readonly string[] ToneKeys = { "high", "mid", "low" };
    private static readonly string[] ToneNames = { "上の枝（灯が強い）", "真ん中", "下の枝（灯が弱い）" };
    private int _storyChapter;

    private void DrawStory()
    {
        var st = (JObject)_game["story"];
        if (st == null) { EditorGUILayout.HelpBox("story が無い。game_config.json に追加してください。", MessageType.Warning); return; }
        var chapters = (JArray)st["chapters"];
        if (chapters == null || chapters.Count == 0) { EditorGUILayout.HelpBox("章がありません。", MessageType.Warning); return; }

        bool en = st["enabled"]?.Value<bool>() ?? true;
        bool nen = EditorGUILayout.ToggleLeft("物語を使う（OFF ならステージ固有の一言だけ）", en);
        if (nen != en) { st["enabled"] = nen; _dirty = true; }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("枝の高さの分け方", GUILayout.Width(110));
            int hb = st["highBranchMax"]?.Value<int>() ?? 2;
            int nhb = EditorGUILayout.IntField("上とみなす枝数", hb, GUILayout.Width(220));
            if (nhb != hb) { st["highBranchMax"] = Math.Max(1, nhb); _dirty = true; }
            int lb = st["lowBranchFromBottom"]?.Value<int>() ?? 1;
            int nlb = EditorGUILayout.IntField("下とみなす枝数", lb, GUILayout.Width(220));
            if (nlb != lb) { st["lowBranchFromBottom"] = Math.Max(1, nlb); _dirty = true; }
        }

        _storyChapter = Math.Min(_storyChapter, chapters.Count - 1);
        var names = chapters.Select(c => (string)c["title"] ?? "章").ToArray();
        _storyChapter = GUILayout.Toolbar(_storyChapter, names, GUILayout.Width(Math.Min(940, names.Length * 220)));
        var ch = (JObject)chapters[_storyChapter];

        string title = (string)ch["title"] ?? "";
        string ntitle = EditorGUILayout.TextField("章の題", title, GUILayout.Width(600));
        if (ntitle != title) { ch["title"] = ntitle; _dirty = true; }
        string theme = (string)ch["theme"] ?? "";
        string ntheme = EditorGUILayout.TextField("テーマ（問い。画面には出ない）", theme, GUILayout.Width(600));
        if (ntheme != theme) { ch["theme"] = ntheme; _dirty = true; }

        EditorGUILayout.Space(8);
        DrawStoryLines(ch, "opening", "章の始め");
        DrawStoryLines(ch, "onBack", "引き返したとき");
        DrawStoryLines(ch, "onTorchOut", "松明が尽きたとき");
        DrawStoryLines(ch, "onDeath", "灯が尽きたとき");
        DrawStoryLines(ch, "onClear", "章を踏破したとき");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("段ごとの到達台詞（枝の高さで出し分ける）", EditorStyles.boldLabel);
        var stages = (JArray)ch["stages"] ?? (JArray)(ch["stages"] = new JArray());
        for (int i = 0; i < stages.Count; i++)
        {
            var sg = (JObject)stages[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"段 {(string)sg["column"]}", EditorStyles.boldLabel);
                for (int t = 0; t < ToneKeys.Length; t++) DrawStoryLines(sg, ToneKeys[t], ToneNames[t]);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("周回で足す台詞（キーは周回数）", EditorStyles.boldLabel);
        var laps = (JObject)ch["laps"] ?? (JObject)(ch["laps"] = new JObject());
        foreach (var prop in laps.Properties().ToList()) DrawStoryLines(laps, prop.Name, prop.Name + " 周目");

        EditorGUILayout.HelpBox("話者を空にすると主人公。それ以外は名前がそのまま出る（「声」など）。\n"
            + "「;」で行を分ける。前が話者、後ろが台詞（例: 声;……また来たのか）。話者を省くなら台詞だけ書く。", MessageType.None);
    }

    /// <summary>台詞の配列を 1 行のテキストで編集する（; 区切り、話者は : の前）。</summary>
    private void DrawStoryLines(JObject owner, string key, string label)
    {
        var arr = (JArray)owner[key] ?? (JArray)(owner[key] = new JArray());
        string cur = string.Join(" ; ", arr.Select(x =>
        {
            var o = (JObject)x;
            string sp = (string)o["speaker"] ?? "";
            string tx = (string)o["text"] ?? "";
            return string.IsNullOrEmpty(sp) ? tx : sp + ":" + tx;
        }));
        string nv = EditorGUILayout.TextField(label, cur, GUILayout.Width(960));
        if (nv == cur) return;
        var next = new JArray();
        foreach (var part in nv.Split(';'))
        {
            string t = part.Trim();
            if (t.Length == 0) continue;
            int colon = t.IndexOf(':');
            string sp = colon > 0 ? t.Substring(0, colon).Trim() : "";
            string tx = colon > 0 ? t.Substring(colon + 1).Trim() : t;
            next.Add(new JObject { ["speaker"] = sp, ["text"] = tx });
        }
        owner[key] = next;
        _dirty = true;
    }

    // ------------------------------------------------------------ ステータス
    private void DrawStats()
    {
        var st = (JObject)_game["stats"];
        if (st == null)
        {
            st = new JObject { ["enabled"] = true, ["pointsPerLevel"] = 2, ["maxPerStat"] = 20,
                ["freeRespecOnChapterClear"] = true, ["respecCost"] = 200,
                ["life"] = new JObject { ["freeBetRate"] = 0.6, ["rescueBonus"] = 10.0, ["torchSpins"] = 0.6 },
                ["technique"] = new JObject { ["engageSpins"] = 0.12, ["defeatBonus"] = 1.4, ["battleDamage"] = 2.5 },
                ["luck"] = new JObject { ["rareRate"] = 0.35, ["replayRate"] = 0.5, ["treasureBonus"] = 0.8 } };
            _game["stats"] = st; _dirty = true;
        }
        EditorGUILayout.LabelField("レベルアップで振るステータス", EditorStyles.boldLabel);
        bool en = st["enabled"]?.Value<bool>() ?? true;
        bool nen = EditorGUILayout.ToggleLeft("ステータスを使う", en);
        if (nen != en) { st["enabled"] = nen; _dirty = true; }
        foreach (var (key, label, min, max) in new[] { ("pointsPerLevel", "レベルアップ 1 回で配るポイント", 0, 10), ("maxPerStat", "1 系統の上限", 1, 99), ("respecCost", "振り直しのソウル", 0, 99999) })
        {
            int v = st[key]?.Value<int>() ?? 0;
            int nv = EditorGUILayout.IntField(label, v, GUILayout.Width(360));
            if (nv != v) { st[key] = Math.Max(min, Math.Min(max, nv)); _dirty = true; }
        }
        bool fr = st["freeRespecOnChapterClear"]?.Value<bool>() ?? true;
        bool nfr = EditorGUILayout.ToggleLeft("章をクリアしていれば振り直し無料", fr);
        if (nfr != fr) { st["freeRespecOnChapterClear"] = nfr; _dirty = true; }

        EditorGUILayout.Space(10);
        DrawStatGroup(st, "life", "ライフ（延命）", new[] {
            ("freeBetRate", "BET が無料になる率 %/pt"),
            ("torchSpins", "松明 1 本のG数 +/pt"),
            ("rescueBonus", "力尽きたときの補填 +/pt") });
        DrawStatGroup(st, "technique", "テクニック（戦闘）", new[] {
            ("engageSpins", "エンゲージのG数 +/pt"),
            ("defeatBonus", "討伐率 +%/pt"),
            ("battleDamage", "狩猟のダメージ +%/pt") });
        DrawStatGroup(st, "luck", "ラック（引き）", new[] {
            ("rareRate", "ハズレがレア役に化ける %/pt"),
            ("replayRate", "ハズレがリプレイに化ける %/pt"),
            ("treasureBonus", "宝の発見率 +%/pt") });

        int mx = st["maxPerStat"]?.Value<int>() ?? 20;
        var lk = (JObject)st["luck"];
        float rr = lk?["rareRate"]?.Value<float>() ?? 0f, rp = lk?["replayRate"]?.Value<float>() ?? 0f;
        EditorGUILayout.HelpBox($"ラックは通常時のハズレだけを引き上げる（設定差を壊さないため）。\n"
            + $"上限 {mx} まで振ると、ハズレの {mx * rr:F1}% がレア役、{mx * rp:F1}% がリプレイになる。\n"
            + "ライフの「BET 無料」とラックは出玉（機械割）に直結する。振り切った状態で機械割を測り直すこと。", MessageType.Info);
    }

    private void DrawStatGroup(JObject st, string key, string title, (string, string)[] fields)
    {
        var o = (JObject)st[key];
        if (o == null) { o = new JObject(); st[key] = o; _dirty = true; }
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        foreach (var (f, label) in fields)
        {
            float v = o[f]?.Value<float>() ?? 0f;
            float nv = EditorGUILayout.FloatField(label, v, GUILayout.Width(400));
            if (Math.Abs(nv - v) > 0.0001f) { o[f] = Math.Max(0f, nv); _dirty = true; }
        }
        EditorGUILayout.Space(6);
    }

    // ------------------------------------------------------------ 冒険
    private static readonly string[] CounterKeys = { "REPLAY", "BELL", "CHERRY", "SUICA", "CHANCE", "BONUS", "HAZE", "WIN", "SPINS", "DEFEAT", "TREASURE", "REPLAY_CHAIN", "PAYOUT" };
    private static readonly string[] CounterNames = { "リプレイ", "ベル", "チェリー", "スイカ", "チャンス目", "ボーナス", "ハズレ", "小役", "G数", "討伐", "宝", "リプ連", "獲得" };
    private static readonly string[] StateKeys = { "ember", "souls", "level", "torches" };
    private static readonly string[] StateNames = { "エンバー", "ソウル", "レベル", "松明" };

    /// <summary>条件の入力欄を横並びで出す（0 なら条件として使わない）。</summary>
    private void DrawCondRow(JObject o, string[] keys, string[] names)
    {
        int perRow = 7;
        for (int start = 0; start < keys.Length; start += perRow)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = start; i < Math.Min(start + perRow, keys.Length); i++)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(76)))
                    {
                        GUILayout.Label(names[i], EditorStyles.miniLabel, GUILayout.Width(76));
                        int v = o[keys[i]]?.Value<int>() ?? 0;
                        int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(76));
                        if (nv != v) { if (nv <= 0) o.Remove(keys[i]); else o[keys[i]] = nv; _dirty = true; }
                    }
                }
            }
        }
    }

    private static readonly string[] StageKinds = { "normal", "high", "treasure", "boss", "rest" };
    private static readonly string[] StageKindNames = { "通常", "高確", "宝", "ボス", "休息" };
    private static readonly string[] StageModes = { "", "A", "B", "C", "D" };
    private static readonly string[] StageModeNames = { "内部モードのまま", "A", "B", "C", "D" };
    private static readonly string[] TreasureKinds = { "souls", "atSpins", "atExpect", "exp" };
    private static readonly string[] TreasureKindNames = { "ソウル", "次のAT +G", "次のボーナス 期待度+%", "EXP" };
    private readonly HashSet<string> _openStages = new HashSet<string>();

    private void DrawAdventure()
    {
        var adv = (JObject)_game["adventure"];
        if (adv == null)
        {
            adv = new JObject { ["enabled"] = true, ["chapterName"] = "第1章", ["start"] = "A-1", ["hideRoute"] = true, ["chapterClearSouls"] = 100, ["nodes"] = new JArray(), ["treasures"] = new JArray() };
            _game["adventure"] = adv; _dirty = true;
        }
        var nodes = (JArray)adv["nodes"] ?? (JArray)(adv["nodes"] = new JArray());
        var treasures = (JArray)adv["treasures"] ?? (JArray)(adv["treasures"] = new JArray());
        var ids = nodes.Select(n => (string)n["id"]).Where(x => !string.IsNullOrEmpty(x)).ToArray();

        EditorGUILayout.LabelField("冒険（ステージ制マップ）", EditorStyles.boldLabel);
        bool en = adv["enabled"]?.Value<bool>() ?? true;
        bool nen = EditorGUILayout.ToggleLeft("冒険を有効にする（OFF で従来通りステージ無し）", en);
        if (nen != en) { adv["enabled"] = nen; _dirty = true; }
        string cn = (string)adv["chapterName"] ?? "";
        string ncn = EditorGUILayout.TextField("章の名前", cn, GUILayout.Width(500));
        if (ncn != cn) { adv["chapterName"] = ncn; _dirty = true; }
        string start = (string)adv["start"] ?? "";
        int si = Math.Max(0, Array.IndexOf(ids, start));
        if (ids.Length > 0)
        {
            int nsi = EditorGUILayout.Popup("開始ステージ", si, ids, GUILayout.Width(300));
            if (ids[nsi] != start) { adv["start"] = ids[nsi]; _dirty = true; }
        }
        bool hr = adv["hideRoute"]?.Value<bool>() ?? true;
        bool nhr = EditorGUILayout.ToggleLeft("先のルートをマップで隠す（通った所と、そこから伸びる道の先だけ見せる）", hr);
        if (nhr != hr) { adv["hideRoute"] = nhr; _dirty = true; }
        int cs = adv["chapterClearSouls"]?.Value<int>() ?? 100;
        int ncs = EditorGUILayout.IntField("章クリアのソウル", cs, GUILayout.Width(300));
        if (ncs != cs) { adv["chapterClearSouls"] = Math.Max(0, ncs); _dirty = true; }
        int ct = adv["chapterClearTorches"]?.Value<int>() ?? 2;
        int nct = EditorGUILayout.IntField("章クリアでもらえる松明", ct, GUILayout.Width(300));
        if (nct != ct) { adv["chapterClearTorches"] = Math.Max(0, nct); _dirty = true; }

        // ---- 資源（松明・エンバー・力尽き）----
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("資源と帰還", EditorStyles.boldLabel);
        var res = (JObject)adv["resource"];
        if (res == null)
        {
            res = new JObject { ["enabled"] = true, ["name"] = "松明", ["startTorches"] = 3, ["maxTorches"] = 9, ["spinsPerTorch"] = 60,
                ["torchCost"] = 40, ["creditCost"] = 30, ["creditAmount"] = 50, ["rescueCredit"] = 50,
                ["resetOnDeath"] = true, ["deathSoulPenalty"] = 0, ["refillByFlag"] = new JObject() };
            adv["resource"] = res; _dirty = true;
        }
        bool re = res["enabled"]?.Value<bool>() ?? true;
        bool nre = EditorGUILayout.ToggleLeft("資源制を使う（尽きたら街へ引き返す）", re);
        if (nre != re) { res["enabled"] = nre; _dirty = true; }
        using (new EditorGUI.DisabledScope(!nre))
        {
            string rn = (string)res["name"] ?? "松明";
            string nrn = EditorGUILayout.TextField("資源の名前", rn, GUILayout.Width(300));
            if (nrn != rn) { res["name"] = nrn; _dirty = true; }
            foreach (var (key, label, min) in new[] {
                ("startTorches", "はじめの所持本数", 0), ("maxTorches", "最大所持本数", 1), ("spinsPerTorch", "1 本で進めるG数", 1),
                ("torchCost", "1 本のソウル価格", 0), ("creditCost", "エンバー 1 口のソウル価格", 0), ("creditAmount", "エンバー 1 口の量", 0),
                ("rescueCredit", "力尽きたとき補填するエンバー（0 で無し）", 0) })
            {
                int v = res[key]?.Value<int>() ?? 0;
                int nv = EditorGUILayout.IntField(label, v, GUILayout.Width(360));
                if (nv != v) { res[key] = Math.Max(min, nv); _dirty = true; }
            }
            bool rod = res["resetOnDeath"]?.Value<bool>() ?? true;
            bool nrod = EditorGUILayout.ToggleLeft("力尽きたら章の最初へ戻す", rod);
            if (nrod != rod) { res["resetOnDeath"] = nrod; _dirty = true; }
            int dsp = res["deathSoulPenalty"]?.Value<int>() ?? 0;
            int ndsp = EditorGUILayout.IntSlider("力尽きたときに失うソウル %", dsp, 0, 100);
            if (ndsp != dsp) { res["deathSoulPenalty"] = ndsp; _dirty = true; }
            EditorGUILayout.LabelField("道中で松明が 1 本増える率 %（役ごと）", EditorStyles.miniBoldLabel);
            var rbf2 = (JObject)res["refillByFlag"] ?? (JObject)(res["refillByFlag"] = new JObject());
            using (new EditorGUILayout.HorizontalScope())
                foreach (var role in Roles)
                {
                    GUILayout.Label(role, EditorStyles.miniLabel, GUILayout.Width(52));
                    int v = rbf2[role]?.Value<int>() ?? 0;
                    int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(40));
                    if (nv != v) { if (nv <= 0) rbf2.Remove(role); else rbf2[role] = Math.Min(100, nv); _dirty = true; }
                }
        }

        // ---- 宝 ----
        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("宝物（重みで 1 つ選ばれる）", EditorStyles.boldLabel, GUILayout.Width(300));
            if (GUILayout.Button("＋ 宝を追加", GUILayout.Width(100)))
            {
                treasures.Add(new JObject { ["id"] = "t" + DateTime.Now.Ticks % 10000, ["name"] = "新しい宝", ["kind"] = "souls", ["amount"] = 30, ["weight"] = 10 });
                _dirty = true;
            }
        }
        int tw = treasures.Sum(t => Math.Max(0, t["weight"]?.Value<int>() ?? 0));
        for (int i = 0; i < treasures.Count; i++)
        {
            var t = (JObject)treasures[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                string id = (string)t["id"] ?? ""; string nid = EditorGUILayout.TextField(id, GUILayout.Width(90)); if (nid != id) { t["id"] = nid; _dirty = true; }
                string nm = (string)t["name"] ?? ""; string nnm = EditorGUILayout.TextField(nm, GUILayout.Width(140)); if (nnm != nm) { t["name"] = nnm; _dirty = true; }
                int ki = Math.Max(0, Array.IndexOf(TreasureKinds, (string)t["kind"] ?? "souls"));
                int nki = EditorGUILayout.Popup(ki, TreasureKindNames, GUILayout.Width(170)); if (nki != ki) { t["kind"] = TreasureKinds[nki]; _dirty = true; }
                GUILayout.Label("量", GUILayout.Width(20));
                int am = t["amount"]?.Value<int>() ?? 0; int nam = EditorGUILayout.IntField(am, _num, GUILayout.Width(60)); if (nam != am) { t["amount"] = Math.Max(0, nam); _dirty = true; }
                GUILayout.Label("重み", GUILayout.Width(30));
                int w = t["weight"]?.Value<int>() ?? 0; int nw = EditorGUILayout.IntField(w, _num, GUILayout.Width(60)); if (nw != w) { t["weight"] = Math.Max(0, nw); _dirty = true; }
                GUILayout.Label(tw > 0 ? $"{100f * Math.Max(0, w) / tw:F1}%" : "-", GUILayout.Width(50));
                if (GUILayout.Button("削除", GUILayout.Width(50))) { treasures.RemoveAt(i); _dirty = true; return; }
            }
        }

        // ---- ステージ ----
        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("ステージ（上から順にマップの列に並ぶ。同じ列は縦に並ぶ）", EditorStyles.boldLabel, GUILayout.Width(420));
            if (GUILayout.Button("＋ ステージを追加", GUILayout.Width(120)))
            {
                nodes.Add(new JObject { ["id"] = "X-" + (nodes.Count + 1), ["name"] = "新しいステージ", ["column"] = "A", ["kind"] = "normal", ["spins"] = 40, ["mode"] = "", ["rank"] = 0,
                    ["engageBoost"] = 0, ["bossRate"] = 0, ["treasure"] = new JObject(), ["routeByFlag"] = new JObject(), ["routeDefault"] = new JObject(), ["enterLines"] = new JArray(), ["desc"] = "", ["color"] = "" });
                _dirty = true;
            }
        }
        var targets = ids.Concat(new[] { "NONE" }).ToArray();
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = (JObject)nodes[i];
            string id = (string)n["id"] ?? "";
            string kind = (string)n["kind"] ?? "normal";
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool open = _openStages.Contains(id);
                    bool nopen = EditorGUILayout.Foldout(open, $"{id}  {(string)n["name"]}   [{StageKindNames[Math.Max(0, Array.IndexOf(StageKinds, kind))]}]  {n["spins"]} G", true);
                    if (nopen != open) { if (nopen) _openStages.Add(id); else _openStages.Remove(id); }
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("↑", GUILayout.Width(26))) { nodes.RemoveAt(i); nodes.Insert(i - 1, n); _dirty = true; return; }
                    using (new EditorGUI.DisabledScope(i == nodes.Count - 1)) if (GUILayout.Button("↓", GUILayout.Width(26))) { nodes.RemoveAt(i); nodes.Insert(i + 1, n); _dirty = true; return; }
                    if (GUILayout.Button("削除", GUILayout.Width(50)) && EditorUtility.DisplayDialog("削除", $"{id} を削除しますか？", "削除", "戻る")) { nodes.RemoveAt(i); _dirty = true; return; }
                }
                if (!_openStages.Contains(id)) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("id", GUILayout.Width(20)); string nid = EditorGUILayout.TextField(id, GUILayout.Width(70)); if (nid != id) { n["id"] = nid; _dirty = true; }
                    GUILayout.Label("名前", GUILayout.Width(30)); string nm = (string)n["name"] ?? ""; string nnm = EditorGUILayout.TextField(nm, GUILayout.Width(140)); if (nnm != nm) { n["name"] = nnm; _dirty = true; }
                    GUILayout.Label("列", GUILayout.Width(20)); string col = (string)n["column"] ?? "A"; string ncol = EditorGUILayout.TextField(col, GUILayout.Width(30)); if (ncol != col) { n["column"] = ncol; _dirty = true; }
                    int ki = Math.Max(0, Array.IndexOf(StageKinds, kind)); int nki = EditorGUILayout.Popup(ki, StageKindNames, GUILayout.Width(70)); if (nki != ki) { n["kind"] = StageKinds[nki]; _dirty = true; }
                    GUILayout.Label("G数", GUILayout.Width(30)); int sp = n["spins"]?.Value<int>() ?? 40; int nsp = EditorGUILayout.IntField(sp, _num, GUILayout.Width(50)); if (nsp != sp) { n["spins"] = Math.Max(1, nsp); _dirty = true; }
                    GUILayout.Label("ランク", GUILayout.Width(40)); int rk = n["rank"]?.Value<int>() ?? 0; int nrk = EditorGUILayout.IntField(rk, _num, GUILayout.Width(40)); if (nrk != rk) { n["rank"] = nrk; _dirty = true; }
                    GUILayout.Label("色", GUILayout.Width(20)); string c = (string)n["color"] ?? ""; string nc = EditorGUILayout.TextField(c, GUILayout.Width(70)); if (nc != c) { n["color"] = nc; _dirty = true; }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("小役テーブル", GUILayout.Width(80));
                    int mi = Math.Max(0, Array.IndexOf(StageModes, (string)n["mode"] ?? "")); int nmi = EditorGUILayout.Popup(mi, StageModeNames, GUILayout.Width(140)); if (nmi != mi) { n["mode"] = StageModes[nmi]; _dirty = true; }
                    GUILayout.Label("追加エンゲージ %/G", GUILayout.Width(120)); int eb = n["engageBoost"]?.Value<int>() ?? 0; int neb = EditorGUILayout.IntField(eb, _num, GUILayout.Width(50)); if (neb != eb) { n["engageBoost"] = Math.Max(0, Math.Min(100, neb)); _dirty = true; }
                    GUILayout.Label("到達時ボス %", GUILayout.Width(80)); int br = n["bossRate"]?.Value<int>() ?? 0; int nbr = EditorGUILayout.IntField(br, _num, GUILayout.Width(50)); if (nbr != br) { n["bossRate"] = Math.Max(0, Math.Min(100, nbr)); _dirty = true; }
                }
                string desc = (string)n["desc"] ?? ""; string ndesc = EditorGUILayout.TextField("説明", desc, GUILayout.Width(900)); if (ndesc != desc) { n["desc"] = ndesc; _dirty = true; }
                string el = string.Join(";", ((JArray)n["enterLines"] ?? new JArray()).Select(x => (string)x));
                string nel = EditorGUILayout.TextField("到着セリフ（; 区切り）", el, GUILayout.Width(900));
                if (nel != el) { n["enterLines"] = new JArray(nel.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }

                EditorGUILayout.LabelField("宝発見率 %（役ごと・1G あたり）", EditorStyles.miniBoldLabel);
                var tr = (JObject)n["treasure"] ?? (JObject)(n["treasure"] = new JObject());
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var role in Roles)
                    {
                        GUILayout.Label(role, EditorStyles.miniLabel, GUILayout.Width(52));
                        int v = tr[role]?.Value<int>() ?? 0; int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(40));
                        if (nv != v) { if (nv <= 0) tr.Remove(role); else tr[role] = Math.Min(100, nv); _dirty = true; }
                    }

                EditorGUILayout.LabelField("ステージ終了時の行き先（重み。全部 0 なら終点＝章クリア）", EditorStyles.miniBoldLabel);
                var rd = (JObject)n["routeDefault"] ?? (JObject)(n["routeDefault"] = new JObject());
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var t in ids)
                    {
                        if (t == id) continue;
                        GUILayout.Label(t, EditorStyles.miniLabel, GUILayout.Width(40));
                        int v = rd[t]?.Value<int>() ?? 0; int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(40));
                        if (nv != v) { if (nv <= 0) rd.Remove(t); else rd[t] = nv; _dirty = true; }
                    }

                // ---- 達成条件のルート ----
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("達成条件で決まるルート（確率抽選より強い）", EditorStyles.miniBoldLabel, GUILayout.Width(300));
                    if (GUILayout.Button("＋ 条件を追加", GUILayout.Width(110)))
                    {
                        var arr = (JArray)n["routeConditions"] ?? (JArray)(n["routeConditions"] = new JArray());
                        arr.Add(new JObject { ["to"] = ids.FirstOrDefault(x => x != id) ?? "", ["label"] = "", ["counters"] = new JObject(), ["state"] = new JObject(), ["priority"] = 10, ["hidden"] = false });
                        _dirty = true;
                    }
                }
                var conds = (JArray)n["routeConditions"] ?? (JArray)(n["routeConditions"] = new JArray());
                for (int ci = 0; ci < conds.Count; ci++)
                {
                    var c = (JObject)conds[ci];
                    using (new EditorGUILayout.VerticalScope("helpbox"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("行き先", GUILayout.Width(44));
                            string to = (string)c["to"] ?? "";
                            var targetIds = ids.Where(x => x != id).ToArray();
                            int ti = Math.Max(0, Array.IndexOf(targetIds, to));
                            if (targetIds.Length > 0)
                            {
                                int nti = EditorGUILayout.Popup(ti, targetIds, GUILayout.Width(80));
                                if (targetIds[nti] != to) { c["to"] = targetIds[nti]; _dirty = true; }
                            }
                            GUILayout.Label("名前", GUILayout.Width(32));
                            string lb = (string)c["label"] ?? "";
                            string nlb = EditorGUILayout.TextField(lb, GUILayout.Width(240));
                            if (nlb != lb) { c["label"] = nlb; _dirty = true; }
                            GUILayout.Label("優先度", GUILayout.Width(44));
                            int pr = c["priority"]?.Value<int>() ?? 10;
                            int npr = EditorGUILayout.IntField(pr, _num, GUILayout.Width(44));
                            if (npr != pr) { c["priority"] = npr; _dirty = true; }
                            bool hd = c["hidden"]?.Value<bool>() ?? false;
                            bool nhd = EditorGUILayout.ToggleLeft("隠す", hd, GUILayout.Width(50));
                            if (nhd != hd) { c["hidden"] = nhd; _dirty = true; }
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("削除", GUILayout.Width(46))) { conds.RemoveAt(ci); _dirty = true; return; }
                        }
                        var cc = (JObject)c["counters"] ?? (JObject)(c["counters"] = new JObject());
                        EditorGUILayout.LabelField("滞在中に数える回数（0 は条件にしない）", EditorStyles.miniLabel);
                        DrawCondRow(cc, CounterKeys, CounterNames);
                        var cst = (JObject)c["state"] ?? (JObject)(c["state"] = new JObject());
                        EditorGUILayout.LabelField("そのGの持ち物・状態（以上）", EditorStyles.miniLabel);
                        DrawCondRow(cst, StateKeys, StateNames);
                    }
                }

                EditorGUILayout.LabelField("道中のルート抽選（役ごと。NONE=決まらない。決まっていても上位ランクなら書き換わる）", EditorStyles.miniBoldLabel);
                var rbf = (JObject)n["routeByFlag"] ?? (JObject)(n["routeByFlag"] = new JObject());
                foreach (var role in Roles)
                {
                    var row = (JObject)rbf[role];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(role, EditorStyles.miniBoldLabel, GUILayout.Width(60));
                        int total = row == null ? 0 : row.Properties().Sum(pp => Math.Max(0, pp.Value.Value<int>()));
                        foreach (var t in targets)
                        {
                            if (t == id) continue;
                            GUILayout.Label(t, EditorStyles.miniLabel, GUILayout.Width(40));
                            int v = row?[t]?.Value<int>() ?? 0; int nv = EditorGUILayout.IntField(v, _num, GUILayout.Width(40));
                            if (nv != v)
                            {
                                if (row == null) { row = new JObject(); rbf[role] = row; }
                                if (nv <= 0) row.Remove(t); else row[t] = nv;
                                if (!row.HasValues) rbf.Remove(role);
                                _dirty = true;
                            }
                        }
                        GUILayout.Label(total > 0 ? $"計 {total}" : "抽選なし", EditorStyles.miniLabel, GUILayout.Width(60));
                    }
                }
            }
        }
        EditorGUILayout.HelpBox("ステージのG数と松明は通常時だけ減る（ボーナス・持ち越し・AT 中は止まる）。移動は敵戦闘や前兆を抱えていないGの終わりに起きる。\n宝・ルートの抽選はこぼしても「フラグ」で行う。ゲーム中は M キー／左上のステージ札でマップが開き、達成条件の進み具合もそこに出る。\n松明切れは進行を残して帰還、エンバー切れは章の最初へ戻る。補給は街のショップの「補給」タブ。", MessageType.None);
    }
}
