using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BBB.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// リール配列エディタ。BBB > Reel Editor で開く。
/// 3リール × N コマ（既定20、15〜25）に図柄を置き、制御ルールで検証してから game_config.json に保存する。
///
/// 検証の意味:
///   順押し保証 … 左→中→右で、どこで押しても「必ず揃える役は揃い」「揃えてはいけない役は揃わない」
///   全順保証   … どの押し順・どこで押しても同上
/// NG の役があると「破綻する最初の押し位置」を赤でハイライトする。
/// </summary>
public sealed class ReelEditorWindow : EditorWindow
{
    private const string ConfigPath = "Assets/Resources/Data/game_config.json";
    private const int MinLen = 15, MaxLen = 25;

    private List<Symbol>[] _reels = new List<Symbol>[3];
    private Vector2 _scroll;
    private string _status = "";
    private readonly Dictionary<Flag, (bool ordered, bool any, List<int> badLeft, List<(int reel, int push)> badAny)> _results
        = new Dictionary<Flag, (bool, bool, List<int>, List<(int, int)>)>();
    private bool _dirty;
    private bool _showAll;
    private Dictionary<Symbol, Texture2D> _icons;
    private HashSet<(int reel, int idx)> _highlight = new HashSet<(int, int)>();

    [MenuItem("BBB/Reel Editor")]
    public static void Open() => GetWindow<ReelEditorWindow>("BBB Reel Editor");

    private void OnEnable()
    {
        Load();
        LoadIcons();
    }

    // ------------------------------------------------------------------ IO
    private void Load()
    {
        try
        {
            var jo = JObject.Parse(File.ReadAllText(ConfigPath));
            var arr = (JArray)jo["reelStrips"];
            for (int r = 0; r < 3; r++)
                _reels[r] = arr[r].Select(t => (Symbol)Enum.Parse(typeof(Symbol), (string)t)).ToList();
            _status = "読み込み: " + ConfigPath;
        }
        catch (Exception e)
        {
            for (int r = 0; r < 3; r++) _reels[r] = Enumerable.Repeat(Symbol.BLANK, 20).ToList();
            _status = "読み込み失敗（空で開始）: " + e.Message;
        }
        _dirty = false;
        _results.Clear();
        _highlight.Clear();
    }

    private void Save()
    {
        var jo = JObject.Parse(File.ReadAllText(ConfigPath));
        jo["reelStrips"] = new JArray(_reels.Select(r => new JArray(r.Select(s => s.ToString()))));
        File.WriteAllText(ConfigPath, jo.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        AssetDatabase.ImportAsset(ConfigPath);
        SlipController.ClearCache();
        _dirty = false;
        _status = "保存しました: " + ConfigPath;
    }

    private void LoadIcons()
    {
        _icons = new Dictionary<Symbol, Texture2D>();
        foreach (Symbol s in Enum.GetValues(typeof(Symbol)))
        {
            string name = s switch
            {
                Symbol.RED7 => "red7", Symbol.BLUE7 => "blue7", Symbol.BAR => "bar", Symbol.STAR => "star",
                Symbol.WATERMELON => "watermelon", Symbol.CHERRY => "cherry", Symbol.REPLAY => "replay", _ => "remix"
            };
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Art/Symbols/{name}.png");
            _icons[s] = sp != null ? sp.texture : null;
        }
    }

    // ---------------------------------------------------------------- GUI
    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("再読込", GUILayout.Width(70))) { if (!_dirty || EditorUtility.DisplayDialog("再読込", "未保存の変更を捨てますか？", "捨てる", "戻る")) Load(); }
            using (new EditorGUI.DisabledScope(!_dirty))
                if (GUILayout.Button("保存", GUILayout.Width(70))) Save();
            if (GUILayout.Button("検証", GUILayout.Width(70))) Validate();
            GUILayout.Space(10);
            int len = _reels[0].Count;
            EditorGUILayout.LabelField("コマ数", GUILayout.Width(40));
            int newLen = EditorGUILayout.IntSlider(len, MinLen, MaxLen, GUILayout.Width(200));
            if (newLen != len) Resize(newLen);
            GUILayout.FlexibleSpace();
            _showAll = GUILayout.Toggle(_showAll, "全フラグ表示", GUILayout.Width(100));
        }
        EditorGUILayout.HelpBox(_status + (_dirty ? "  [未保存]" : ""), MessageType.None);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawReels();
            GUILayout.Space(16);
            DrawResults();
        }
        EditorGUILayout.EndScrollView();
    }

    private void Resize(int newLen)
    {
        for (int r = 0; r < 3; r++)
        {
            while (_reels[r].Count < newLen) _reels[r].Add(Symbol.BLANK);
            while (_reels[r].Count > newLen) _reels[r].RemoveAt(_reels[r].Count - 1);
        }
        _dirty = true; _results.Clear(); _highlight.Clear();
    }

    private void DrawReels()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(3 * 150 + 40)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("#", GUILayout.Width(28));
                GUILayout.Label("左", EditorStyles.boldLabel, GUILayout.Width(150));
                GUILayout.Label("中", EditorStyles.boldLabel, GUILayout.Width(150));
                GUILayout.Label("右", EditorStyles.boldLabel, GUILayout.Width(150));
            }
            int len = _reels[0].Count;
            for (int i = 0; i < len; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(i.ToString(), GUILayout.Width(28));
                    for (int r = 0; r < 3; r++)
                    {
                        bool bad = _highlight.Contains((r, i));
                        var prev = GUI.backgroundColor;
                        if (bad) GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(150)))
                        {
                            var icon = _icons != null && _icons.TryGetValue(_reels[r][i], out var t) ? t : null;
                            if (icon != null) GUILayout.Label(icon, GUILayout.Width(44), GUILayout.Height(20));
                            else GUILayout.Space(44);
                            var nv = (Symbol)EditorGUILayout.EnumPopup(_reels[r][i], GUILayout.Width(100));
                            if (nv != _reels[r][i]) { _reels[r][i] = nv; _dirty = true; _results.Clear(); _highlight.Clear(); }
                        }
                        GUI.backgroundColor = prev;
                    }
                }
            }
            GUILayout.Space(8);
            EditorGUILayout.LabelField("図柄数", EditorStyles.boldLabel);
            foreach (Symbol s in Enum.GetValues(typeof(Symbol)))
                EditorGUILayout.LabelField($"{s,-11} 左{_reels[0].Count(x => x == s),2}  中{_reels[1].Count(x => x == s),2}  右{_reels[2].Count(x => x == s),2}");
        }
    }

    private void DrawResults()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("検証結果（順押し保証 / 全順保証）", EditorStyles.boldLabel);
            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox("「検証」を押すと、全フラグについて制御で保証できるかを調べます（数秒）。", MessageType.Info);
                return;
            }
            foreach (var kv in _results)
            {
                var (ordered, any, badLeft, badAny) = kv.Value;
                bool must = SlipController.MustAlign(kv.Key) || kv.Key == Flag.HAZE || kv.Key.IsReachMe();
                if (!_showAll && ordered && any) continue;
                string tag = kv.Key.IsBonus() ? "目押し" : must ? "必須" : "こぼし可";
                var color = ordered && any ? Color.green : ordered ? Color.yellow : Color.red;
                var prev = GUI.contentColor; GUI.contentColor = color;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{kv.Key,-9} [{tag}]  順押し:{(ordered ? "OK" : "NG")}  全順:{(any ? "OK" : "NG")}", GUILayout.Width(300));
                    GUI.contentColor = prev;
                    if (!any && GUILayout.Button("位置を表示", GUILayout.Width(80)))
                    {
                        _highlight = new HashSet<(int, int)>(badAny);
                        _status = $"{kv.Key}: 赤いコマで第一停止すると（滑り0〜4のどこに止めても）その後どこかで破綻します";
                    }
                }
                GUI.contentColor = prev;
                if (!ordered && badLeft.Count > 0)
                    EditorGUILayout.LabelField($"   順押しで破綻する左の押し位置: {string.Join(",", badLeft)}");
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "必須 = ベル・リプレイ（必ず揃える）／ハズレ・リーチ目（何も揃えない）。ここが NG だと制御でルール違反を避けられない位置がある。\n" +
                "こぼし可 = スイカ・チェリー・チャンス目。NG は「ルール違反を避けられない位置がある」ことを意味する（こぼれ自体は可）。\n" +
                "目押し = ボーナス。揃うかは目押し次第。NG は同上。", MessageType.None);
        }
    }

    // ----------------------------------------------------------- VALIDATE
    private void Validate()
    {
        var strips = _reels.Select(r => r.ToArray()).ToArray();
        if (strips.Any(s => s.Length != strips[0].Length)) { _status = "3リールのコマ数が違います"; return; }
        SlipController.ClearCache();
        _results.Clear();
        _highlight.Clear();
        var flags = (Flag[])Enum.GetValues(typeof(Flag));
        try
        {
            for (int i = 0; i < flags.Length; i++)
            {
                var f = flags[i];
                EditorUtility.DisplayProgressBar("検証中", f.ToString(), (float)i / flags.Length);
                bool ordered = SlipController.IsGuaranteedOrdered(strips, f);
                bool any = SlipController.IsGuaranteed(strips, f);
                var badLeft = ordered ? new List<int>() : SlipController.FindUnguaranteedLeftPushesOrdered(strips, f);
                var badAny = any ? new List<(int, int)>() : SlipController.FindUnguaranteedPushes(strips, f);
                _results[f] = (ordered, any, badLeft, badAny);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        int ngOrdered = _results.Count(kv => !kv.Value.ordered);
        int ngAny = _results.Count(kv => !kv.Value.any);
        _status = $"検証完了: 順押しNG {ngOrdered} / 全順NG {ngAny}（{flags.Length}フラグ）";
        Repaint();
    }
}
