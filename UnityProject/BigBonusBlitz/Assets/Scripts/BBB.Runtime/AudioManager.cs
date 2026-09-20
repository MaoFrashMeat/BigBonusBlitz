using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// BGM 1系統 + SE ワンショット。Web版の Web Audio 合成音は既存素材で代替。
    /// 対応: bet→se_bet, spin→se_spin_start, stop→se_ui_pop, win→se_coin, replay→se_replay,
    ///       attack→se_attack, enemyDeath→se_enemy_death, bbConfirm→se_bb_confirm
    /// </summary>
    public sealed partial class AudioManager : MonoBehaviour
    {
        private AudioSource _bgm;
        private AudioSource _se;
        private AudioSource _sePitched;   // ピッチを変える SE 用
        private AudioLowPassFilter _bgmLpf;
        private float _bgmBaseVolume = 0.5f;
        private Coroutine _focusRoutine;
        private AudioClip _bgmNormal, _bet, _spin, _stop, _win, _replay, _attack, _enemyDeath, _bbConfirm, _uiPop;
        /// <summary>敵出現。素材 se_enemy_appear があればそれ 1 本、無ければ合成音（接近＋着地）。</summary>
        private AudioClip _appearFile, _appearWhoosh, _appearImpact;
        /// <summary>役ごとの音（素材 se_role_bell / se_role_cherry / se_role_suica / se_role_chance があれば優先）。</summary>
        private AudioClip _roleBell, _roleCherry, _roleSuica, _roleChance;
        /// <summary>通常時の控えめな獲得音（素材 se_coin_small があれば優先）。</summary>
        private AudioClip _smallCoin;
        /// <summary>ボーナス引き込み（素材 se_pullin / se_pullin_land があれば優先）。</summary>
        private AudioClip _pullIn, _pullInLand;
        /// <summary>予告（素材 se_precog_weak / se_precog_strong があれば優先）。</summary>
        private AudioClip _precogWeak, _precogStrong;
        /// <summary>実績解除と落とし物（素材 se_achievement / se_pickup_soul / se_pickup_ember / se_pickup_item があれば優先）。</summary>
        private AudioClip _achievement, _pickupSoul, _pickupEmber, _pickupItem;
        private AudioClip _gainExp, _gainGames;
        private AudioClip _rankPerfect;
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _rankClips = new System.Collections.Generic.Dictionary<string, AudioClip>();

        /// <summary>
        /// 効果音の一覧: キー → 既定の素材名（Resources/Audio/SE。無ければ合成音）/ 既定の音量。
        /// Resources/Data/se_config.json（tools/se_viewer.html → se_build.py）で素材・音量・高さを差し替える。
        /// 場面の説明（when）はビューアの表示用（tools/se_sync.py が読む）。
        /// </summary>
        public sealed class SeDef
        {
            public string key, file, group, name, when; public float volume; public System.Func<AudioClip> synth;
            public SeDef(string key, string file, float volume, System.Func<AudioClip> synth, string group, string name, string when)
            { this.key = key; this.file = file; this.volume = volume; this.synth = synth; this.group = group; this.name = name; this.when = when; }
        }
        public static readonly SeDef[] SeDefs =
        {
            new SeDef("bet", "se_bet", 0.6f, null, "レバー・停止", "BET", "MAX BET を押した（リプレイのときは鳴らない）"),
            new SeDef("spin_start", "se_spin_start", 0.5f, null, "レバー・停止", "回転開始", "レバーオン。リールが回り始める"),
            new SeDef("stop", "se_ui_pop", 0.4f, null, "レバー・停止", "停止", "リールを 1 つ止めた。示唆のときは高さが上がる（StopHot）"),
            new SeDef("pullin", "se_pullin", 0.7f, () => SfxSynth.PullIn(), "レバー・停止", "引き込み", "ボーナス図柄を引き込むためにリールが余分に回っている間"),
            new SeDef("pullin_land", "se_pullin_land", 0.9f, () => SfxSynth.PullInLand(), "レバー・停止", "引き込み完了", "引き込みが終わって図柄が止まった「ガコン」"),
            new SeDef("precog_weak", "se_precog_weak", 0.6f, () => SfxSynth.PrecogWeak(), "レバー・停止", "予告（弱）", "レバーオン時の予告音（弱）"),
            new SeDef("precog_strong", "se_precog_strong", 0.8f, () => SfxSynth.PrecogStrong(), "レバー・停止", "予告（強）", "レバーオン時の予告音（強。熱い）"),
            new SeDef("role_bell", "se_role_bell", 0.45f, () => SfxSynth.BellDing(), "役", "ベル", "ベルが揃った（払い出しの音と重なる）"),
            new SeDef("role_cherry", "se_role_cherry", 0.8f, () => SfxSynth.Cherry(), "役", "チェリー", "チェリーが揃った"),
            new SeDef("role_suica", "se_role_suica", 0.85f, () => SfxSynth.Suica(), "役", "スイカ", "スイカが揃った"),
            new SeDef("role_chance", "se_role_chance", 0.9f, () => SfxSynth.ChanceSting(), "役", "チャンス目", "チャンス目が揃った"),
            new SeDef("replay", "se_replay", 0.6f, null, "役", "リプレイ", "リプレイが揃った"),
            new SeDef("win", "se_coin", 1f, null, "払い出し", "払い出し（連打）", "ボーナス中の払い出し。枚数ぶん「デュルデュル」と連打（Payout）"),
            new SeDef("coin_small", "se_coin_small", 0.5f, () => SfxSynth.SmallCoin(), "払い出し", "払い出し（小）", "通常時の払い出し。枚数に応じて 1〜3 発"),
            new SeDef("bb_confirm", "se_bb_confirm", 1f, null, "ボーナス", "ボーナス確定", "BIG / REG が確定したとき（BGM を止めて 1 本）"),
            new SeDef("enemy_appear", "se_enemy_appear", 0.9f, null, "エンゲージ", "敵出現", "敵が画面に滑り込んでくる（素材が無ければ 接近 + 着地 の合成音）"),
            new SeDef("attack", "se_attack", 1f, null, "エンゲージ", "攻撃", "役で敵を斬る。押し順ナビ失敗の被弾にも低くして使う（NaviFail）"),
            new SeDef("enemy_death", "se_enemy_death", 1f, null, "エンゲージ", "討伐", "敵を倒した"),
            new SeDef("navi_success", "se_navi_success", 1f, null, "エンゲージ", "ナビ正解", "押し順ナビに従えた（素材が無ければ リプレイ音を高く + コイン）"),
            new SeDef("escape", "se_enemy_escape", 0.8f, null, "エンゲージ", "逃走", "敵が逃げた（素材が無ければ 停止音を低く）"),
            new SeDef("pickup_soul", "se_pickup_soul", 0.7f, () => SfxSynth.PickupSoul(), "拾い物・GET", "ソウル", "ソウルを拾った / 「n SOUL GET」の帯が止まった"),
            new SeDef("pickup_ember", "se_pickup_ember", 0.8f, () => SfxSynth.PickupEmber(), "拾い物・GET", "エンバー", "エンバーを拾った / 「n EMB GET」の帯が止まった"),
            new SeDef("pickup_item", "se_pickup_item", 0.7f, () => SfxSynth.PickupItem(), "拾い物・GET", "品", "回復薬などの品を拾った"),
            new SeDef("gain_exp", "se_gain_exp", 0.7f, () => SfxSynth.GainExp(), "拾い物・GET", "EXP", "「n EXP GET」の帯が止まった"),
            new SeDef("gain_games", "se_gain_games", 0.8f, () => SfxSynth.GainGames(), "拾い物・GET", "AT の G 数", "「AT +nG GET」の帯が止まった"),
            new SeDef("rank_perfect", "se_rank_perfect", 0.9f, () => SfxSynth.RankPerfect(), "技術介入", "Perfect!!", "技術介入でビタ押し最高ランク"),
            new SeDef("rank", "se_rank", 0.8f, () => SfxSynth.RankChime(1f, 0.3f), "技術介入", "ランク（Cool 等）", "Perfect!! 以外のランク。tech.ranks の sePitch / seSeconds で高さと長さが変わる（素材は se_rank_<id>）"),
            new SeDef("achievement", "se_achievement", 0.9f, () => SfxSynth.Achievement(), "その他", "実績解除", "実績を解除した"),
            new SeDef("ui_pop", "se_ui_pop", 0.8f, null, "その他", "UI", "窓の開け閉め・ボタン（モーション音の元）"),
        };
        /// <summary>BGM の場所: キー → 既定の素材名（Resources/Audio/BGM）/ 説明。se_config.json の bgm で差し替える。無い場所は adventure → bgm_normal。</summary>
        public static readonly SeDef[] BgmDefs =
        {
            new SeDef("title", "bgm_normal", 0.5f, null, "BGM", "タイトル", "タイトル画面（TAP TO START）"),
            new SeDef("town", "bgm_normal", 0.5f, null, "BGM", "街", "街の地図（ショップ / 装備 / 実績 / 設定）"),
            new SeDef("adventure", "bgm_normal", 0.5f, null, "BGM", "冒険（通常時）", "冒険中の通常時。他の場所に指定が無ければこれ"),
            new SeDef("bonus", "bgm_normal", 0.5f, null, "BGM", "ボーナス中", "BIG / REG の消化中（確定音のあとから）"),
            new SeDef("at", "bgm_normal", 0.5f, null, "BGM", "AT（洞窟）", "AT「洞窟」の間"),
            new SeDef("engage", "bgm_normal", 0.5f, null, "BGM", "エンゲージ", "敵が出ている間（前兆〜決着）"),
        };
        [System.Serializable] public sealed class SeSetting { public string file = ""; public float volume = -1f; public float pitch = 1f; }
        [System.Serializable] private sealed class SeConfigFile
        {
            public System.Collections.Generic.Dictionary<string, SeSetting> se = new System.Collections.Generic.Dictionary<string, SeSetting>();
            public System.Collections.Generic.Dictionary<string, SeSetting> bgm = new System.Collections.Generic.Dictionary<string, SeSetting>();
        }
        private System.Collections.Generic.Dictionary<string, SeSetting> _bgmConfig = new System.Collections.Generic.Dictionary<string, SeSetting>();
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _bgmClips = new System.Collections.Generic.Dictionary<string, AudioClip>();
        private string _bgmKey = "adventure";

        /// <summary>その場所の BGM。se_config の bgm[key].file → 既定 → adventure の指定 → bgm_normal。</summary>
        private AudioClip BgmClip(string key, out float vol)
        {
            vol = 0.5f;
            SeSetting st = _bgmConfig != null && _bgmConfig.TryGetValue(key, out var s0) ? s0 : null;
            if (st != null && st.volume >= 0f) vol = st.volume;
            else if (_bgmConfig != null && _bgmConfig.TryGetValue("adventure", out var sa) && sa != null && sa.volume >= 0f) vol = sa.volume;
            string file = st != null && !string.IsNullOrEmpty(st.file) ? st.file
                : _bgmConfig != null && _bgmConfig.TryGetValue("adventure", out var s1) && s1 != null && !string.IsNullOrEmpty(s1.file) ? s1.file : "bgm_normal";
            if (!_bgmClips.TryGetValue(file, out var clip) || clip == null) { clip = Resources.Load<AudioClip>("Audio/BGM/" + file); _bgmClips[file] = clip; }
            return clip ?? _bgmNormal;
        }
        private System.Collections.Generic.Dictionary<string, SeSetting> _seConfig = new System.Collections.Generic.Dictionary<string, SeSetting>();
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _seClips = new System.Collections.Generic.Dictionary<string, AudioClip>();

        private static SeDef DefOf(string key) { foreach (var d in SeDefs) if (d.key == key) return d; return null; }
        private SeSetting Setting(string key) => _seConfig != null && _seConfig.TryGetValue(key, out var s) && s != null ? s : null;

        /// <summary>そのキーの音。se_config.json の file → 既定の素材 → 合成音、の順。無ければ null。</summary>
        private AudioClip Clip(string key)
        {
            if (_seClips.TryGetValue(key, out var c)) return c;
            var d = DefOf(key); var st = Setting(key);
            AudioClip clip = null;
            if (st != null && !string.IsNullOrEmpty(st.file)) clip = Resources.Load<AudioClip>("Audio/SE/" + st.file);
            if (clip == null && d != null && !string.IsNullOrEmpty(d.file)) clip = Resources.Load<AudioClip>("Audio/SE/" + d.file);
            if (clip == null && d?.synth != null) clip = d.synth();
            _seClips[key] = clip;
            return clip;
        }
        /// <summary>そのキーの音量（se_config.json → 既定）。</summary>
        private float Vol(string key)
        {
            var st = Setting(key);
            if (st != null && st.volume >= 0f) return st.volume;
            var d = DefOf(key); return d != null ? d.volume : 1f;
        }
        /// <summary>そのキーの高さ（se_config.json。1 = そのまま）。</summary>
        private float Pitch(string key) { var st = Setting(key); return st != null && st.pitch > 0f ? st.pitch : 1f; }
        /// <summary>キーで鳴らす。高さの指定があれば pitch 用の AudioSource で。</summary>
        private void PlayKey(string key, float volMul = 1f)
        {
            var c = Clip(key); if (c == null) return;
            MarkSound();
            float pitch = Pitch(key);
            if (Mathf.Abs(pitch - 1f) < 0.001f) { _se.PlayOneShot(c, Vol(key) * volMul); return; }
            _sePitched.pitch = pitch; _sePitched.volume = _se.volume; _sePitched.PlayOneShot(c, Vol(key) * volMul);
        }
        private void LoadSeConfig()
        {
            _seConfig = new System.Collections.Generic.Dictionary<string, SeSetting>();
            var ta = Resources.Load<TextAsset>("Data/se_config");
            if (ta == null) return;
            try { var f = Newtonsoft.Json.JsonConvert.DeserializeObject<SeConfigFile>(ta.text); if (f?.se != null) _seConfig = f.se; if (f?.bgm != null) _bgmConfig = f.bgm; }
            catch (System.Exception e) { Debug.LogWarning("se_config.json を読めない: " + e.Message); }
        }

        public float BgmVolume { get => _bgmBaseVolume; set { _bgmBaseVolume = value; _bgm.volume = value; } }
        public float SeVolume { get => _se.volume; set { value = Mathf.Clamp01(value); _se.volume = value; _sePitched.volume = value; SetMotionVolume(value); } }
        public bool BgmEnabled { get; private set; } = true;

        /// <summary>
        /// 画面をまたいで 1 つだけ使う。タイトル → マップ → ゲームの遷移で作り直すと、
        /// 破棄と再生成が重なって BGM が鳴らないことがあるため、既にあれば使い回す。
        /// </summary>
        public static AudioManager Create()
        {
            var exist = FindFirstObjectByType<AudioManager>();
            if (exist != null) return exist;
            var go = new GameObject("AudioManager");
            var am = go.AddComponent<AudioManager>();
            am._bgm = go.AddComponent<AudioSource>();
            am._bgm.loop = true;
            am._bgm.playOnAwake = false;
            am._bgmLpf = go.AddComponent<AudioLowPassFilter>();
            am._bgmLpf.cutoffFrequency = 22000f;
            am._bgmLpf.lowpassResonanceQ = 1f;
            am._se = go.AddComponent<AudioSource>();
            am._se.playOnAwake = false;
            am._sePitched = go.AddComponent<AudioSource>();
            am._sePitched.playOnAwake = false;
            am._bgmNormal = Resources.Load<AudioClip>("Audio/BGM/bgm_normal");
            // 効果音は一覧（SeDefs）と se_config.json から。素材 → 既定の素材 → 合成音
            am.LoadSeConfig();
            am._bet = am.Clip("bet"); am._spin = am.Clip("spin_start"); am._uiPop = am.Clip("ui_pop"); am._stop = am.Clip("stop");
            am._win = am.Clip("win"); am._replay = am.Clip("replay"); am._attack = am.Clip("attack"); am._enemyDeath = am.Clip("enemy_death");
            am._bbConfirm = am.Clip("bb_confirm");
            am._appearFile = am.Clip("enemy_appear");
            if (am._appearFile == null)
            {
                am._appearWhoosh = SfxSynth.AppearWhoosh(0.5f);
                am._appearImpact = SfxSynth.AppearImpact(0.7f);
            }
            am._roleBell = am.Clip("role_bell"); am._roleCherry = am.Clip("role_cherry"); am._roleSuica = am.Clip("role_suica"); am._roleChance = am.Clip("role_chance");
            am._smallCoin = am.Clip("coin_small");
            am._pullIn = am.Clip("pullin"); am._pullInLand = am.Clip("pullin_land");
            am._precogWeak = am.Clip("precog_weak"); am._precogStrong = am.Clip("precog_strong");
            am._achievement = am.Clip("achievement");
            am._pickupSoul = am.Clip("pickup_soul"); am._pickupEmber = am.Clip("pickup_ember"); am._pickupItem = am.Clip("pickup_item");
            am._gainExp = am.Clip("gain_exp"); am._gainGames = am.Clip("gain_games");
            am._rankPerfect = am.Clip("rank_perfect");
            am._bgm.volume = 0.5f;
            am._se.volume = 0.8f;
            am._sePitched.volume = 0.8f;
            am.InitMotion();
            return am;
        }

        private void Play(AudioClip c, float vol = 1f) { if (c != null) { MarkSound(); _se.PlayOneShot(c, vol); } }

        public void Bet() => PlayKey("bet");
        public void SpinStart() => PlayKey("spin_start");
        public void Stop() => PlayKey("stop");
        /// <summary>示唆: 停止音が高くなる（3G目の熱いパターン）。stopIndex 0..2 で段階的に上げる。</summary>
        public void StopHot(int stopIndex)
        {
            MarkSound();
            if (_stop == null) return;
            _sePitched.pitch = (1.3f + 0.25f * stopIndex) * Pitch("stop");
            _sePitched.volume = _se.volume;
            _sePitched.PlayOneShot(_stop, Vol("stop") * 1.75f);
        }
        /// <summary>択の最中: BGM を水中のようにこもらせる（ローパス 500Hz、音量 60%）。</summary>
        public void SetFocus(bool on)
        {
            if (_focusRoutine != null) StopCoroutine(_focusRoutine);
            _focusRoutine = StartCoroutine(FocusRoutine(on));
        }

        private System.Collections.IEnumerator FocusRoutine(bool on)
        {
            float c0 = _bgmLpf.cutoffFrequency, c1 = on ? 500f : 22000f;
            float v0 = _bgm.volume, v1 = on ? _bgmBaseVolume * 0.6f : _bgmBaseVolume;
            float t = 0, d = on ? 0.25f : 0.15f;
            while (t < d)
            {
                t += Time.deltaTime;
                float u = t / d;
                // 対数で補間すると自然
                _bgmLpf.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(c0), Mathf.Log(c1), u));
                _bgm.volume = Mathf.Lerp(v0, v1, u);
                yield return null;
            }
            _bgmLpf.cutoffFrequency = c1;
            _bgm.volume = v1;
        }

        /// <summary>択 正解: 爽快な上昇音（パワーアップ音を高めに＋コイン）。</summary>
        public void NaviSuccess()
        {
            if (Clip("navi_success") != null) { PlayKey("navi_success"); return; }   // 素材があれば 1 本
            MarkSound();
            _sePitched.pitch = 1.25f;
            _sePitched.volume = _se.volume;
            if (_replay != null) _sePitched.PlayOneShot(_replay, 1f);
            if (_win != null) _se.PlayOneShot(_win, 0.8f);
        }

        /// <summary>択 失敗: 攻撃を食らう（斬撃音を低く）。</summary>
        public void NaviFail()
        {
            MarkSound();
            _sePitched.pitch = 0.7f;
            _sePitched.volume = _se.volume;
            if (_attack != null) _sePitched.PlayOneShot(_attack, 0.9f);
        }

        /// <summary>敵が逃げる（低いポップ音）。</summary>
        public void EnemyEscape()
        {
            if (Clip("escape") != null) { PlayKey("escape"); return; }   // 素材があれば 1 本
            MarkSound();
            if (_uiPop == null) return;
            _sePitched.pitch = 0.6f;
            _sePitched.volume = _se.volume;
            _sePitched.PlayOneShot(_uiPop, 0.8f);
        }
        public void Win() => PlayKey("win");

        /// <summary>払い出し音: 枚数ぶん「デュルデュル」と連打。ピッチを少しずつ上げて枚数感を出す。</summary>
        public void Payout(int coins)
        {
            MarkSound();
            if (_win == null || coins <= 0) { Win(); return; }
            StopCoroutine(nameof(PayoutRoutine));
            StartCoroutine(nameof(PayoutRoutine), coins);
        }

        private System.Collections.IEnumerator PayoutRoutine(int coins)
        {
            int n = Mathf.Min(coins, 30);
            float interval = coins >= 15 ? 0.045f : 0.07f;
            // 連打は音が重なって足し合わさる。1 発あたりを下げて合計の音量感を 1 発分に近づける
            float shot = coins >= 15 ? 0.32f : 0.45f;
            var wait = new WaitForSeconds(interval);
            for (int i = 0; i < n; i++)
            {
                _sePitched.pitch = 1.0f + 0.03f * i;
                _sePitched.volume = _se.volume;
                _sePitched.PlayOneShot(_win, shot * Vol("win"));
                yield return wait;
            }
            _sePitched.pitch = 1f;
        }

        /// <summary>通常時の獲得音: 控えめな「チャリ」を枚数に応じて 1〜3 発（ボーナス中の連打音と差をつける）。</summary>
        public void PayoutSmall(int coins)
        {
            MarkSound();
            if (_smallCoin == null) { Win(); return; }
            StopCoroutine(nameof(PayoutSmallRoutine));
            StartCoroutine(nameof(PayoutSmallRoutine), coins);
        }

        private System.Collections.IEnumerator PayoutSmallRoutine(int coins)
        {
            int n = coins >= 8 ? 3 : coins >= 6 ? 2 : 1;
            var wait = new WaitForSeconds(0.07f);
            for (int i = 0; i < n; i++)
            {
                _sePitched.pitch = 1f + 0.04f * i;
                _sePitched.volume = _se.volume;
                _sePitched.PlayOneShot(_smallCoin, Vol("coin_small"));
                yield return wait;
            }
            _sePitched.pitch = 1f;
        }

        /// <summary>実績解除の音（役の音より式典寄りの 3 音）。</summary>
        public void Achievement() => PlayKey("achievement");

        /// <summary>落とし物を拾う音。kind は落とし物の種類（souls / embers / それ以外）。</summary>
        public void Pickup(string kind)
        {
            switch (kind)
            {
                case "souls": PlayKey("pickup_soul"); break;
                case "embers": PlayKey("pickup_ember"); break;
                default: PlayKey("pickup_item"); break;
            }
        }

        /// <summary>「GET」の帯の音。kind は帯の絵（soul / ember / book = EXP / games = AT の G 数）。帯が止まった瞬間に 1 発。</summary>
        public void Gain(string kind)
        {
            switch (kind)
            {
                case "soul": PlayKey("pickup_soul"); break;
                case "ember": PlayKey("pickup_ember"); break;
                case "book": PlayKey("gain_exp"); break;
                case "games": PlayKey("gain_games"); break;
                default: PlayKey("pickup_item"); break;
            }
        }

        /// <summary>技術介入のランクの音。Perfect!!（上乗せ 100% 以上）は専用、他はランクの sePitch / seSeconds で合成した音（ランクごとに 1 回作って持つ）。</summary>
        public void TechRank(TechRankDef rank)
        {
            if (rank == null) return;
            if (rank.bonusPercent >= 100) { PlayKey("rank_perfect"); return; }
            string key = (rank.id ?? "") + ":" + rank.sePitch.ToString("F2") + ":" + rank.seSeconds.ToString("F2");
            if (!_rankClips.TryGetValue(key, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/SE/se_rank_" + rank.id) ?? SfxSynth.RankChime(rank.sePitch, rank.seSeconds);
                _rankClips[key] = clip;
            }
            Play(clip, Vol("rank"));
        }

        /// <summary>予告音。stage 1=弱 2=強。</summary>
        public void Precog(int stage)
        {
            if (stage >= 2) PlayKey("precog_strong");
            else if (stage == 1) PlayKey("precog_weak");
        }

        /// <summary>ボーナス引き込み開始（リールが余分に回っている間の吸い込み音）。</summary>
        public void ReelPullIn() => PlayKey("pullin");
        /// <summary>引き込み完了の「ガコン」。</summary>
        public void ReelPullInLand() => PlayKey("pullin_land");

        // 役ごとの音（演出のタイミング担当。§16.2）
        public void RoleBell() => PlayKey("role_bell");
        public void RoleCherry() => PlayKey("role_cherry");
        public void RoleSuica() => PlayKey("role_suica");
        public void RoleChance() => PlayKey("role_chance");

        public void Replay() => PlayKey("replay");
        public void Attack() => PlayKey("attack");
        public void EnemyDeath() => PlayKey("enemy_death");
        public void UiPop() => Motion(MotionCue.Click);

        /// <summary>敵出現・接近開始（SlideIn の頭で呼ぶ）。素材があれば素材を 1 回だけ鳴らす。</summary>
        public void EnemyAppearStart()
        {
            if (_appearFile != null) { PlayKey("enemy_appear"); return; }
            Play(_appearWhoosh, 0.85f);
        }

        /// <summary>敵の着地（SlideIn 完了時）。素材使用時は鳴らさない（素材側に含める）。</summary>
        public void EnemyAppearLand()
        {
            if (_appearFile != null) return;
            Play(_appearImpact, 1f);
        }

        /// <summary>BB確定音。長さ(秒)を返す（呼び元で操作ロックに使う）。</summary>
        public float BbConfirm()
        {
            MarkSound();
            StopBgm();
            if (_bbConfirm == null) return 0f;
            _se.PlayOneShot(_bbConfirm, Vol("bb_confirm"));
            return _bbConfirm.length;
        }

        /// <summary>BGM を流す。key は場所（BgmDefs。null なら今の場所のまま）。同じ曲なら続けて流す（切れない）。</summary>
        public void StartBgm(string key = null)
        {
            if (key != null) _bgmKey = key;
            var clip = BgmClip(_bgmKey, out float vol);
            if (!BgmEnabled || clip == null) return;
            if (_bgm.clip != clip) { _bgm.clip = clip; _bgm.Play(); }
            else if (!_bgm.isPlaying) _bgm.Play();
            _bgmBaseVolume = vol; _bgm.volume = vol;
        }

        public void StopBgm() { if (_bgm.isPlaying) _bgm.Stop(); }

        public void ToggleBgm()
        {
            BgmEnabled = !BgmEnabled;
            if (BgmEnabled) StartBgm(); else StopBgm();
        }
    }
}
