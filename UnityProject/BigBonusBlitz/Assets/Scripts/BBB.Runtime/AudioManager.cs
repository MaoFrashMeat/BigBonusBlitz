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
        private AudioClip _bgmNormal, _bet, _spin, _stop, _win, _replay, _attack, _enemyDeath, _bbConfirm, _uiPop;

        public float BgmVolume { get => _bgm.volume; set => _bgm.volume = value; }
        public float SeVolume { get => _se.volume; set { _se.volume = value; _sePitched.volume = value; } }
        public bool BgmEnabled { get; private set; } = true;

        public static AudioManager Create()
        {
            var go = new GameObject("AudioManager");
            var am = go.AddComponent<AudioManager>();
            am._bgm = go.AddComponent<AudioSource>();
            am._bgm.loop = true;
            am._bgm.playOnAwake = false;
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
