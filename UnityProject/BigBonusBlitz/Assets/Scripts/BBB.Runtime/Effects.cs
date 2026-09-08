using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>style.css の各 @keyframes を Unity で再現する小物集（RectTransform ベース）。</summary>
    public static class Effects
    {
        /// <summary>screenShake 0.4s。</summary>
        public static IEnumerator Shake(RectTransform target, float duration = 0.4f, float amp = 8f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - t / duration;
                target.anchoredPosition = origin + new Vector2(Random.Range(-amp, amp), Random.Range(-amp, amp)) * k;
                yield return null;
            }
            target.anchoredPosition = origin;
        }

        /// <summary>enemyAnimHit 0.3s: scale 1→1.2(rot10°)→1。</summary>
        public static IEnumerator Hit(RectTransform target, float duration = 0.3f)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                target.localScale = Vector3.one * (1f + 0.2f * p);
                target.localRotation = Quaternion.Euler(0, 0, 10f * p);
                yield return null;
            }
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        /// <summary>enemyAnimSquash 0.5s: 潰れて消える（forwards）。</summary>
        public static IEnumerator Squash(RectTransform target, CanvasGroup cg, float duration = 0.5f)
        {
            float t = 0;
            var origin = target.anchoredPosition;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float sy = Mathf.Lerp(1f, 0.2f, Mathf.Min(1f, p * 2f));
                float sx = Mathf.Lerp(1f, 1.5f, Mathf.Min(1f, p * 2f));
                target.localScale = new Vector3(sx, sy, 1);
                target.anchoredPosition = origin + new Vector2(0, -22f * Mathf.Min(1f, p * 2f));
                if (cg != null && p > 0.5f) cg.alpha = 1f - (p - 0.5f) * 2f;
                yield return null;
            }
            if (cg != null) cg.alpha = 0f;
            target.localScale = Vector3.one;
            target.anchoredPosition = origin;
        }

        /// <summary>enemySlideIn 0.5s: 右300pxから、透明→不透明。</summary>
        public static IEnumerator SlideIn(RectTransform target, CanvasGroup cg, float duration = 0.5f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
                target.anchoredPosition = origin + new Vector2(225f * (1f - p), 0);
                if (cg != null) cg.alpha = p;
                yield return null;
            }
            target.anchoredPosition = origin;
            if (cg != null) cg.alpha = 1f;
        }

        /// <summary>逃走: 右へ走り去って消える。</summary>
        public static IEnumerator SlideOut(RectTransform target, CanvasGroup cg, float duration = 0.45f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                target.anchoredPosition = origin + new Vector2(260f * p * p, 10f * Mathf.Sin(p * Mathf.PI * 4f));
                if (cg != null) cg.alpha = 1f - p;
                yield return null;
            }
            if (cg != null) cg.alpha = 0f;
            target.anchoredPosition = origin;
        }

        /// <summary>虹色: 色相を回し続ける。</summary>
        public static IEnumerator Rainbow(Image img)
        {
            while (true)
            {
                img.color = Color.HSVToRGB((Time.time * 0.8f) % 1f, 0.6f, 1f);
                yield return null;
            }
        }

        /// <summary>勝利: 白くフラッシュしてから潰れて消える。</summary>
        public static IEnumerator Defeat(RectTransform target, Image img, CanvasGroup cg)
        {
            var c0 = img.color;
            for (int i = 0; i < 3; i++)
            {
                img.color = Color.white * 3f;
                yield return new WaitForSeconds(0.06f);
                img.color = c0;
                yield return new WaitForSeconds(0.06f);
            }
            yield return Squash(target, cg);
        }

        /// <summary>idleEnemyAnim: 2秒周期で少し潰れる。無限。</summary>
        public static IEnumerator IdleBob(RectTransform target)
        {
            var origin = target.anchoredPosition;
            while (true)
            {
                float p = (1f - Mathf.Cos(Time.time * Mathf.PI)) * 0.5f;   // 0→1→0 / 2s
                target.localScale = new Vector3(1, 1f - 0.1f * p, 1);
                target.anchoredPosition = origin + new Vector2(0, -3f * p);
                yield return null;
            }
        }

        /// <summary>animMiss 0.5s: 左に仰け反る。</summary>
        public static IEnumerator Miss(RectTransform target, float duration = 0.5f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float k = Mathf.Sin(p * Mathf.PI);
                target.anchoredPosition = origin + new Vector2(-15f * k, 0);
                target.localRotation = Quaternion.Euler(0, 0, 8f * k);
                yield return null;
            }
            target.anchoredPosition = origin;
            target.localRotation = Quaternion.identity;
        }

        /// <summary>animCherry 0.7s: 横薙ぎ。</summary>
        public static IEnumerator Cherry(RectTransform target, float duration = 0.7f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float x = p < 0.3f ? Mathf.Lerp(0, -22f, p / 0.3f) : p < 0.4f ? Mathf.Lerp(-22f, 45f, (p - 0.3f) / 0.1f) : p < 0.6f ? 40f : Mathf.Lerp(40f, 0, (p - 0.6f) / 0.4f);
                float r = p < 0.3f ? Mathf.Lerp(0, -20f, p / 0.3f) : p < 0.6f ? 25f : Mathf.Lerp(25f, 0, (p - 0.6f) / 0.4f);
                target.anchoredPosition = origin + new Vector2(x, 0);
                target.localRotation = Quaternion.Euler(0, 0, r);
                yield return null;
            }
            target.anchoredPosition = origin;
            target.localRotation = Quaternion.identity;
        }

        /// <summary>animWatermelon 0.8s: ジャンプして叩きつけ。</summary>
        public static IEnumerator Watermelon(RectTransform target, float duration = 0.8f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float y = p < 0.3f ? Mathf.Lerp(0, 60f, p / 0.3f) : p < 0.45f ? Mathf.Lerp(60f, -15f, (p - 0.3f) / 0.15f) : p < 0.7f ? -15f : Mathf.Lerp(-15f, 0, (p - 0.7f) / 0.3f);
                float r = p > 0.3f && p < 0.7f ? -15f : 0f;
                target.anchoredPosition = origin + new Vector2(0, y);
                target.localRotation = Quaternion.Euler(0, 0, r);
                yield return null;
            }
            target.anchoredPosition = origin;
            target.localRotation = Quaternion.identity;
        }

        /// <summary>animReplay 0.8s: 少し前に出てガード。</summary>
        public static IEnumerator Replay(RectTransform target, float duration = 0.8f)
        {
            var origin = target.anchoredPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float x = p < 0.2f ? Mathf.Lerp(0, 11f, p / 0.2f) : p < 0.8f ? 11f : Mathf.Lerp(11f, 0, (p - 0.8f) / 0.2f);
                target.anchoredPosition = origin + new Vector2(x, 0);
                yield return null;
            }
            target.anchoredPosition = origin;
        }

        /// <summary>cutinZoom 0.5s(オーバーシュート) → 表示保持 → 消す。</summary>
        public static IEnumerator Cutin(RectTransform target, CanvasGroup cg, float hold = 3f)
        {
            cg.alpha = 1f;
            float t = 0, d = 0.5f;
            while (t < d)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / d);
                float s = 1f + 0.3f * Mathf.Sin(p * Mathf.PI) ;   // 0→1.3→1 風
                target.localScale = Vector3.one * Mathf.Lerp(0f, 1f, p) * s;
                yield return null;
            }
            target.localScale = Vector3.one;
            float ft = 0;
            while (ft < hold)
            {
                ft += Time.deltaTime;
                target.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(ft * Mathf.PI));
                yield return null;
            }
            cg.alpha = 0f;
            target.localScale = Vector3.one;
        }

        /// <summary>hint-red-glow: 画面縁が赤く点滅。</summary>
        public static IEnumerator RedGlow(Image overlay, float duration = 1.5f)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 12f)) * (1f - t / duration);
                overlay.color = new Color(1f, 0.1f, 0.1f, a);
                yield return null;
            }
            overlay.color = new Color(1f, 0.1f, 0.1f, 0f);
        }
    }
}
