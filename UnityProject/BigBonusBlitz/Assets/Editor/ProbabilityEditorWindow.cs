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

    private static readonly string[] Tabs = { "小役確率", "天井・モード移行", "ワークフロー", "エネミー", "エンゲージ示唆", "旅人" };
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
            _tab = GUILayout.Toolbar(_tab, Tabs, GUILayout.Width(620));
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
        }
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ 小役確率
    private void DrawSymbolProbs()
    {
        _table = GUILayout.Toolbar(_table, TableNames.Concat(new[] { "Tier2(共通)" }).ToArray(), GUILayout.Width(560));
        EditorGUILayout.Space(4);
        bool tier2 = _table == TableNames.Length;

        if (tier2)
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
        EditorGUILayout.LabelField("天井（G数。到達で BB_A / RB_A を 1:1 で強制当選）", EditorStyles.boldLabel);
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
        EditorGUILayout.HelpBox("BIG/REG は「獲得枚数で終了」する目標枚数。REPLAY は再遊技のコスト相当（クレジット表示用）。", MessageType.None);
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
            GUILayout.Label("雑談（; 区切り）", EditorStyles.boldLabel, GUILayout.Width(300));
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
                string nch = EditorGUILayout.TextField(ch, GUILayout.Width(300));
                if (nch != ch) { t["chatter"] = new JArray(nch.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0)); _dirty = true; }
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
}
