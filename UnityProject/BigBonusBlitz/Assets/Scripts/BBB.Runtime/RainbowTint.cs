using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 文字や枠の色を虹に回す（プリズムのレア度用）。付けた Graphic の色を毎フレーム色相で回す。
    /// 外すときは Remove を呼ぶ（色は呼び手が入れ直す）。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class RainbowTint : MonoBehaviour
    {
        /// <summary>1 秒に回る色相の量（1 = 1 周）。</summary>
        public float speed = 0.45f;
        public float saturation = 0.7f;
        public float value = 1f;
        public float alpha = 1f;

        private Graphic _g;
        private float _phase;

        private void Awake()
        {
            _g = GetComponent<Graphic>();
            _phase = Random.value;   // 隣どうしが同じ色にならないように
        }

        private void Update()
        {
            if (_g == null) return;
            var c = Color.HSVToRGB((Time.time * speed + _phase) % 1f, saturation, value);
            c.a = alpha;
            _g.color = c;
        }

        /// <summary>虹にする。すでに付いていれば設定だけ更新。</summary>
        public static RainbowTint Apply(Graphic g, float alpha = 1f, float saturation = 0.7f)
        {
            if (g == null) return null;
            var t = g.GetComponent<RainbowTint>() ?? g.gameObject.AddComponent<RainbowTint>();
            t.alpha = alpha; t.saturation = saturation;
            return t;
        }

        /// <summary>虹をやめる（色はそのままなので、呼び手が入れ直す）。</summary>
        public static void Remove(Graphic g)
        {
            if (g == null) return;
            var t = g.GetComponent<RainbowTint>();
            if (t != null) Object.Destroy(t);
        }
    }
}
