using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace BBB.Runtime
{
    /// <summary>
    /// キーが押された「正確な時刻」を覚える（Input System のイベント時刻。1 フレームより細かい）。
    /// 技術介入のビタのランクは押した瞬間のコマ内の位置で決めるので、フレーム単位の wasPressedThisFrame では足りない
    /// （リールは 1 コマ 37ms、1 フレームは 8〜17ms）。時刻の軸は Time.realtimeSinceStartup と同じ。
    /// </summary>
    public static class PressTimer
    {
        private static readonly Dictionary<Key, double> _last = new Dictionary<Key, double>();
        private static bool _hooked;

        public static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            InputSystem.onEvent += OnEvent;
        }

        private static void OnEvent(InputEventPtr e, InputDevice device)
        {
            if (!(device is Keyboard)) return;
            if (!e.IsA<StateEvent>() && !e.IsA<DeltaStateEvent>()) return;
            foreach (var c in e.EnumerateChangedControls(device))
            {
                if (c is KeyControl k && k.ReadValueFromEvent(e, out float v) && v > 0.5f) _last[k.keyCode] = e.time;
            }
        }

        /// <summary>そのキーが最後に押された時刻。無ければ -1。</summary>
        public static double LastPress(Key key) => _last.TryGetValue(key, out var t) ? t : -1;

        /// <summary>いくつかのキーのうち、最も新しく押されたものの時刻（同じフレームで押されたキーの時刻を取る用）。</summary>
        public static double Latest(params Key[] keys)
        {
            double best = -1;
            foreach (var k in keys) { var t = LastPress(k); if (t > best) best = t; }
            return best;
        }
    }
}
