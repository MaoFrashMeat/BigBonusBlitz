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
        public void Replay() => Play(_replay, 0.6f);
        public void Attack() => Play(_attack);
        public void EnemyDeath() => Play(_enemyDeath);
        public void UiPop() => Play(_uiPop, 0.5f);

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
