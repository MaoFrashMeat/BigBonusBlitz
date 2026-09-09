using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// BGM 1系統 + SE ワンショット。Web版の Web Audio 合成音は既存素材で代替。
    /// 対応: bet→se_bet, spin→se_spin_start, stop→se_ui_pop, win→se_coin, replay→se_replay,
    ///       attack→se_attack, enemyDeath→se_enemy_death, bbConfirm→se_bb_confirm
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
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

        public float BgmVolume { get => _bgmBaseVolume; set { _bgmBaseVolume = value; _bgm.volume = value; } }
        public float SeVolume { get => _se.volume; set { _se.volume = value; _sePitched.volume = value; } }
        public bool BgmEnabled { get; private set; } = true;

        public static AudioManager Create()
        {
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
            am._bet = Resources.Load<AudioClip>("Audio/SE/se_bet");
            am._spin = Resources.Load<AudioClip>("Audio/SE/se_spin_start");
            am._uiPop = Resources.Load<AudioClip>("Audio/SE/se_ui_pop");
            am._stop = am._uiPop;
            am._win = Resources.Load<AudioClip>("Audio/SE/se_coin");
            am._replay = Resources.Load<AudioClip>("Audio/SE/se_replay");
            am._attack = Resources.Load<AudioClip>("Audio/SE/se_attack");
            am._enemyDeath = Resources.Load<AudioClip>("Audio/SE/se_enemy_death");
            am._bbConfirm = Resources.Load<AudioClip>("Audio/SE/se_bb_confirm");
            am._appearFile = Resources.Load<AudioClip>("Audio/SE/se_enemy_appear");
            if (am._appearFile == null)
            {
                am._appearWhoosh = SfxSynth.AppearWhoosh(0.5f);
                am._appearImpact = SfxSynth.AppearImpact(0.7f);
            }
            am._roleBell = Resources.Load<AudioClip>("Audio/SE/se_role_bell") ?? SfxSynth.BellDing();
            am._roleCherry = Resources.Load<AudioClip>("Audio/SE/se_role_cherry") ?? SfxSynth.Cherry();
            am._roleSuica = Resources.Load<AudioClip>("Audio/SE/se_role_suica") ?? SfxSynth.Suica();
            am._roleChance = Resources.Load<AudioClip>("Audio/SE/se_role_chance") ?? SfxSynth.ChanceSting();
            am._smallCoin = Resources.Load<AudioClip>("Audio/SE/se_coin_small") ?? SfxSynth.SmallCoin();
            am._pullIn = Resources.Load<AudioClip>("Audio/SE/se_pullin") ?? SfxSynth.PullIn();
            am._pullInLand = Resources.Load<AudioClip>("Audio/SE/se_pullin_land") ?? SfxSynth.PullInLand();
            am._precogWeak = Resources.Load<AudioClip>("Audio/SE/se_precog_weak") ?? SfxSynth.PrecogWeak();
            am._precogStrong = Resources.Load<AudioClip>("Audio/SE/se_precog_strong") ?? SfxSynth.PrecogStrong();
            am._bgm.volume = 0.5f;
            am._se.volume = 0.8f;
            am._sePitched.volume = 0.8f;
            return am;
        }

        private void Play(AudioClip c, float vol = 1f) { if (c != null) _se.PlayOneShot(c, vol); }

        public void Bet() => Play(_bet, 0.6f);
        public void SpinStart() => Play(_spin, 0.5f);
        public void Stop() => Play(_stop, 0.4f);
        /// <summary>示唆: 停止音が高くなる（3G目の熱いパターン）。stopIndex 0..2 で段階的に上げる。</summary>
        public void StopHot(int stopIndex)
        {
            if (_stop == null) return;
            _sePitched.pitch = 1.3f + 0.25f * stopIndex;
            _sePitched.volume = _se.volume;
            _sePitched.PlayOneShot(_stop, 0.7f);
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
            _sePitched.pitch = 1.25f;
            _sePitched.volume = _se.volume;
            if (_replay != null) _sePitched.PlayOneShot(_replay, 1f);
            if (_win != null) _se.PlayOneShot(_win, 0.8f);
        }

        /// <summary>択 失敗: 攻撃を食らう（斬撃音を低く）。</summary>
        public void NaviFail()
        {
            _sePitched.pitch = 0.7f;
            _sePitched.volume = _se.volume;
            if (_attack != null) _sePitched.PlayOneShot(_attack, 0.9f);
        }

        /// <summary>敵が逃げる（低いポップ音）。</summary>
        public void EnemyEscape()
        {
            if (_uiPop == null) return;
            _sePitched.pitch = 0.6f;
            _sePitched.volume = _se.volume;
            _sePitched.PlayOneShot(_uiPop, 0.8f);
        }
        public void Win() => Play(_win);

        /// <summary>払い出し音: 枚数ぶん「デュルデュル」と連打。ピッチを少しずつ上げて枚数感を出す。</summary>
        public void Payout(int coins)
        {
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
                _sePitched.PlayOneShot(_win, shot);
                yield return wait;
            }
            _sePitched.pitch = 1f;
        }

        /// <summary>通常時の獲得音: 控えめな「チャリ」を枚数に応じて 1〜3 発（ボーナス中の連打音と差をつける）。</summary>
        public void PayoutSmall(int coins)
        {
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
                _sePitched.PlayOneShot(_smallCoin, 0.5f);
                yield return wait;
            }
            _sePitched.pitch = 1f;
        }

        /// <summary>予告音。stage 1=弱 2=強。</summary>
        public void Precog(int stage)
        {
            if (stage >= 2) Play(_precogStrong, 0.8f);
            else if (stage == 1) Play(_precogWeak, 0.6f);
        }

        /// <summary>ボーナス引き込み開始（リールが余分に回っている間の吸い込み音）。</summary>
        public void ReelPullIn() => Play(_pullIn, 0.7f);
        /// <summary>引き込み完了の「ガコン」。</summary>
        public void ReelPullInLand() => Play(_pullInLand, 0.9f);

        // 役ごとの音（演出のタイミング担当。§16.2）
        public void RoleBell() => Play(_roleBell, 0.45f);
        public void RoleCherry() => Play(_roleCherry, 0.8f);
        public void RoleSuica() => Play(_roleSuica, 0.85f);
        public void RoleChance() => Play(_roleChance, 0.9f);

        public void Replay() => Play(_replay, 0.6f);
        public void Attack() => Play(_attack);
        public void EnemyDeath() => Play(_enemyDeath);
        public void UiPop() => Play(_uiPop, 0.5f);

        /// <summary>敵出現・接近開始（SlideIn の頭で呼ぶ）。素材があれば素材を 1 回だけ鳴らす。</summary>
        public void EnemyAppearStart()
        {
            if (_appearFile != null) { Play(_appearFile, 0.9f); return; }
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
            StopBgm();
            if (_bbConfirm == null) return 0f;
            _se.PlayOneShot(_bbConfirm);
            return _bbConfirm.length;
        }

        public void StartBgm()
        {
            if (!BgmEnabled || _bgmNormal == null) return;
            if (_bgm.clip != _bgmNormal) _bgm.clip = _bgmNormal;
            if (!_bgm.isPlaying) _bgm.Play();
        }

        public void StopBgm() { if (_bgm.isPlaying) _bgm.Stop(); }

        public void ToggleBgm()
        {
            BgmEnabled = !BgmEnabled;
            if (BgmEnabled) StartBgm(); else StopBgm();
        }
    }
}
