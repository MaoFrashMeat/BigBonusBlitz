using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// 素材が無い効果音を実行時に合成する。全て 44.1kHz モノラル。
    /// 素材が用意されたら AudioManager 側で Resources のクリップを優先する。
    /// </summary>
    public static class SfxSynth
    {
        private const int Sr = 44100;

        /// <summary>
        /// 合成音の全体ゲイン。素材 SE（録音物は正規化後も実効値が低い）と音量感を揃えるため、
        /// フルスケールで作った波形をここで落とす。0.4 ≒ -8dB。
        /// </summary>
        public static float MasterGain = 0.4f;

        private static AudioClip Make(string name, float[] data)
        {
            float g = Mathf.Clamp01(MasterGain);
            for (int i = 0; i < data.Length; i++) data[i] *= g;
            var clip = AudioClip.Create(name, data.Length, 1, Sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Soft(float x) => (float)System.Math.Tanh(x);

        /// <summary>
        /// 敵出現・接近の「ヒュゥゥ…」: ノイズをローパスで籠らせ、近づくほど明るく・大きく。
        /// 長さは SlideIn（0.5 秒）に合わせる。
        /// </summary>
        public static AudioClip AppearWhoosh(float seconds = 0.5f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(12345);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;                       // 0..1
                float noise = (float)(rng.NextDouble() * 2 - 1);
                // カットオフ: 低→高（接近感）。2段の一次ローパス
                float k = Mathf.Lerp(0.015f, 0.35f, t * t);
                lp += k * (noise - lp);
                lp2 += k * (lp - lp2);
                // 音量: なめらかに立ち上がり、最後の 8% で急に切る（着地音に受け渡す）
                float env = Mathf.SmoothStep(0f, 1f, t / 0.92f);
                if (t > 0.92f) env = 1f - (t - 0.92f) / 0.08f;
                // うっすら唸り（55Hz）を混ぜて「重さ」を足す
                float growl = Mathf.Sin(2f * Mathf.PI * 55f * i / Sr) * 0.12f * env;
                d[i] = Soft((lp2 * 2.2f + growl) * env * 1.6f) * 0.9f;
            }
            return Make("synth_appear_whoosh", d);
        }

        /// <summary>
        /// 着地の「ドンッ」＋短い不協和スティング: 低音スイープ（140→45Hz）の減衰、
        /// 打撃のノイズ、半音でぶつかる 2 音の短い刺し。
        /// </summary>
        public static AudioClip AppearImpact(float seconds = 0.7f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(777);
            float phase = 0f;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float ts = i / (float)Sr;                      // 秒
                // 低音: 周波数を指数で下げ、振幅も指数減衰
                float f = Mathf.Lerp(45f, 140f, Mathf.Exp(-ts / 0.06f));
                phase += 2f * Mathf.PI * f / Sr;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-ts / 0.22f);
                // 打撃ノイズ: 最初の 40ms
                float noise = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.25f * (noise - lp);
                float crack = lp * Mathf.Exp(-ts / 0.03f) * 1.2f;
                // スティング: 220Hz と 233Hz（半音）の鋸波っぽい音。80ms 遅れて入り 0.3 秒で消える
                float sting = 0f;
                if (ts > 0.08f)
                {
                    float u = ts - 0.08f;
                    float a = Mathf.Exp(-u / 0.12f) * 0.28f;
                    sting = (Saw(220f * u) + Saw(233.1f * u) * 0.8f) * a;
                }
                d[i] = Soft(thump * 1.4f + crack + sting) * 0.95f;
            }
            return Make("synth_appear_impact", d);
        }

        /// <summary>周期 1 の鋸波（-1..1）。</summary>
        private static float Saw(float x) => 2f * (x - Mathf.Floor(x + 0.5f));

        /// <summary>減衰する正弦音（倍音つき）。t は秒、開始からの経過。</summary>
        private static float Pluck(float t, float freq, float decay, float harm2 = 0.35f)
        {
            if (t < 0) return 0f;
            float env = Mathf.Exp(-t / decay);
            return (Mathf.Sin(2f * Mathf.PI * freq * t) + harm2 * Mathf.Sin(4f * Mathf.PI * freq * t)) * env;
        }

        /// <summary>ベル: 明るい「チン」。2 音の倍音で鐘っぽく。</summary>
        public static AudioClip BellDing(float seconds = 0.35f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 1760f, 0.12f, 0.2f) * 0.6f + Pluck(t, 2637f, 0.09f, 0.1f) * 0.35f + Pluck(t, 880f, 0.2f, 0.1f) * 0.25f;
                d[i] = Soft(v) * 0.8f;
            }
            return Make("synth_bell_ding", d);
        }

        /// <summary>チェリー: 「ぽん、ぽん」と弾む 2 音（A5 → C#6）。</summary>
        public static AudioClip Cherry(float seconds = 0.4f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 880f, 0.07f, 0.5f) * 0.8f + Pluck(t - 0.11f, 1108.7f, 0.08f, 0.5f) * 0.8f;
                // 少しだけ「ぷ」の丸みを足す（低い倍音）
                v += Pluck(t, 440f, 0.05f, 0f) * 0.3f + Pluck(t - 0.11f, 554.4f, 0.05f, 0f) * 0.3f;
                d[i] = Soft(v) * 0.85f;
            }
            return Make("synth_cherry", d);
        }

        /// <summary>スイカ: 「ぱしゃっ」というしぶき（ローパスノイズが落ちる＋しずくの高音 2 粒）。</summary>
        public static AudioClip Suica(float seconds = 0.5f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(4242);
            float lp = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float k = Mathf.Lerp(0.5f, 0.04f, Mathf.Clamp01(t / 0.3f));   // 明るい→こもる
                lp += k * (noise - lp);
                float splash = lp * Mathf.Exp(-t / 0.13f) * 1.6f;
                // 水のうねり: 500→180Hz
                float f = Mathf.Lerp(180f, 500f, Mathf.Exp(-t / 0.05f));
                phase += 2f * Mathf.PI * f / Sr;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t / 0.1f) * 0.5f;
                float drops = Pluck(t - 0.14f, 1500f, 0.04f, 0f) * 0.35f + Pluck(t - 0.24f, 1900f, 0.04f, 0f) * 0.3f;
                d[i] = Soft(splash + body + drops) * 0.9f;
            }
            return Make("synth_suica", d);
        }

        /// <summary>チャンス目: 上昇アルペジオ（C5 E5 G5 C6）＋きらめきの持続音。</summary>
        public static AudioClip ChanceSting(float seconds = 0.9f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            var rng = new System.Random(99);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float st = t - k * 0.075f;
                    if (st < 0) continue;
                    float env = k == notes.Length - 1 ? Mathf.Exp(-st / 0.45f) : Mathf.Exp(-st / 0.16f);
                    v += (Mathf.Sin(2f * Mathf.PI * notes[k] * st) + 0.25f * Saw(notes[k] * st)) * env * 0.5f;
                }
                // きらめき: 高い倍音にトレモロ
                float shimmer = Mathf.Sin(2f * Mathf.PI * 2093f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 9f * t)) * Mathf.Exp(-Mathf.Max(0, t - 0.2f) / 0.35f) * (t > 0.2f ? 0.18f : 0f);
                // 「シャーン」の上昇ノイズ
                float noise = (float)(rng.NextDouble() * 2 - 1);
                lp += Mathf.Lerp(0.05f, 0.5f, Mathf.Clamp01(t / 0.3f)) * (noise - lp);
                float shing = lp * Mathf.Exp(-Mathf.Abs(t - 0.28f) / 0.12f) * 0.35f;
                d[i] = Soft(v + shimmer + shing) * 0.9f;
            }
            return Make("synth_chance", d);
        }

        /// <summary>引き込み開始: 低い「ヴン…」という吸い込みのうねり（回転が続く間に鳴る）。</summary>
        public static AudioClip PullIn(float seconds = 0.6f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float u = t / seconds;
                float f = Mathf.Lerp(90f, 220f, u * u);                 // 上がっていく唸り
                phase += 2f * Mathf.PI * f / Sr;
                float env = Mathf.Sin(Mathf.PI * u);                     // 山型
                float v = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * env * 0.5f;
                v *= 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 28f * t);  // 回転のトレモロ
                d[i] = Soft(v) * 0.8f;
            }
            return Make("synth_pullin", d);
        }

        /// <summary>引き込み完了: 「ガコン」と重い停止音（低い打撃＋金属の短い響き）。</summary>
        public static AudioClip PullInLand(float seconds = 0.4f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(555);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float f = Mathf.Lerp(60f, 180f, Mathf.Exp(-t / 0.03f));
                phase += 2f * Mathf.PI * f / Sr;
                float thud = Mathf.Sin(phase) * Mathf.Exp(-t / 0.12f);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.4f * (noise - lp);
                float click = lp * Mathf.Exp(-t / 0.015f) * 1.5f;
                float ring = Pluck(t - 0.02f, 1320f, 0.08f, 0.3f) * 0.3f;
                d[i] = Soft(thud * 1.3f + click + ring) * 0.9f;
            }
            return Make("synth_pullin_land", d);
        }

        /// <summary>予告（弱）: 短い「ピッ」。</summary>
        public static AudioClip PrecogWeak(float seconds = 0.16f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 1320f, 0.05f, 0.4f) * 0.7f + Pluck(t, 1980f, 0.04f, 0f) * 0.3f;
                d[i] = Soft(v) * 0.7f;
            }
            return Make("synth_precog_weak", d);
        }

        /// <summary>予告（強）: 「ピピッ↑」上昇 2 音＋きらめき。</summary>
        public static AudioClip PrecogStrong(float seconds = 0.45f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 1174.7f, 0.06f, 0.4f) * 0.7f + Pluck(t - 0.09f, 1760f, 0.09f, 0.4f) * 0.8f;
                float shimmer = t > 0.12f ? Mathf.Sin(2f * Mathf.PI * 3520f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 11f * t)) * Mathf.Exp(-(t - 0.12f) / 0.15f) * 0.2f : 0f;
                d[i] = Soft(v + shimmer) * 0.8f;
            }
            return Make("synth_precog_strong", d);
        }

        /// <summary>実績解除: 上昇の 3 音（G5 C6 E6）を鐘で鳴らし、最後の音を長く残す。役の音より「式典」寄り。</summary>
        public static AudioClip Achievement(float seconds = 1.1f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float[] notes = { 783.99f, 1046.5f, 1318.5f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float st = t - k * 0.13f;
                    if (st < 0) continue;
                    float decay = k == notes.Length - 1 ? 0.5f : 0.18f;
                    v += Pluck(st, notes[k], decay, 0.2f) * 0.55f + Pluck(st, notes[k] * 2f, decay * 0.5f, 0f) * 0.2f;
                }
                // 最後の音に重ねる高いきらめき（ゆっくり揺れる）
                float st3 = t - 0.26f;
                if (st3 > 0) v += Mathf.Sin(2f * Mathf.PI * 2637f * st3) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 6f * st3)) * Mathf.Exp(-st3 / 0.4f) * 0.15f;
                d[i] = Soft(v) * 0.9f;
            }
            return Make("synth_achievement", d);
        }

        /// <summary>ソウルを拾う: 息のような高い持続音がふわっと上がって消える（金属音ではなく「魂」らしく）。</summary>
        public static AudioClip PickupSoul(float seconds = 0.5f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float phase = 0f, phase2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float f = Mathf.Lerp(880f, 1760f, 1f - Mathf.Exp(-t / 0.12f));   // 上がっていく
                phase += 2f * Mathf.PI * f / Sr;
                phase2 += 2f * Mathf.PI * (f * 1.5f) / Sr;                     // 5 度上を薄く
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));   // ふわっと出てふわっと消える
                float vib = 1f + 0.01f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                float v = Mathf.Sin(phase * vib) * 0.5f + Mathf.Sin(phase2) * 0.18f;
                d[i] = Soft(v * env) * 0.8f;
            }
            return Make("synth_pickup_soul", d);
        }

        /// <summary>エンバー（灯）を拾う: 低い「ぼっ」と火のはぜる粒（ノイズの短い粒を散らす）。</summary>
        public static AudioClip PickupEmber(float seconds = 0.4f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(77);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float f = Mathf.Lerp(90f, 260f, Mathf.Exp(-t / 0.06f));         // 「ぼっ」: 高めから低く落ちる
                phase += 2f * Mathf.PI * f / Sr;
                float puff = Mathf.Sin(phase) * Mathf.Exp(-t / 0.09f) * 0.9f;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.12f * (noise - lp);
                float whoosh = lp * Mathf.Exp(-t / 0.12f) * 1.2f;
                // はぜる粒: まばらに短い高音のパチッ
                float crackle = (rng.NextDouble() < 0.004 && t > 0.05f) ? 0.6f : 0f;
                d[i] = Soft(puff + whoosh + crackle * Mathf.Exp(-t / 0.3f)) * 0.9f;
            }
            return Make("synth_pickup_ember", d);
        }

        /// <summary>回復薬や地図など: 「ぽこっ」と弾む 2 音（低→高）。役の音と紛れない丸い音。</summary>
        public static AudioClip PickupItem(float seconds = 0.3f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 587.3f, 0.06f, 0.1f) * 0.8f + Pluck(t - 0.09f, 880f, 0.08f, 0.1f) * 0.8f;
                d[i] = Soft(v) * 0.8f;
            }
            return Make("synth_pickup_item", d);
        }

        /// <summary>EXP を得る: 上がっていく 3 音（レベルが近づく感じ。魂の「ふわっ」より角がある）。</summary>
        public static AudioClip GainExp(float seconds = 0.45f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, 659.3f, 0.10f, 0.25f) * 0.7f + Pluck(t - 0.08f, 830.6f, 0.10f, 0.25f) * 0.7f + Pluck(t - 0.16f, 1108.7f, 0.16f, 0.2f) * 0.8f;
                d[i] = Soft(v) * 0.8f;
            }
            return Make("synth_gain_exp", d);
        }

        /// <summary>AT の G 数が増える: 高い「キン」と、その上に薄い倍音のきらめき（ベルの払い出し音と区別がつくように短く鋭い）。</summary>
        public static AudioClip GainGames(float seconds = 0.4f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float shimmer = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 11f * t);
                float v = Pluck(t, 2093f * shimmer, 0.09f, 0.15f) * 0.6f + Pluck(t, 3136f, 0.06f, 0.1f) * 0.35f + Pluck(t - 0.05f, 1568f, 0.18f, 0.2f) * 0.5f;
                d[i] = Soft(v) * 0.75f;
            }
            return Make("synth_gain_games", d);
        }

        /// <summary>技術介入のランクの音: 短い「タッ」の後に「ティン」。pitch で高さ（1 = 880Hz）、seconds で長さが変わる（上のランクほど高く長く）。</summary>
        public static AudioClip RankChime(float pitch, float seconds)
        {
            pitch = Mathf.Clamp(pitch, 0.3f, 3f); seconds = Mathf.Clamp(seconds, 0.1f, 1.5f);
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float f = 880f * pitch, tail = Mathf.Max(0.05f, seconds * 0.45f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = Pluck(t, f * 0.5f, 0.03f, 0.4f) * 0.5f                                  // 「タッ」（低く短く）
                        + Pluck(t - 0.05f, f, tail, 0.2f) * 0.7f + Pluck(t - 0.05f, f * 1.5f, tail * 0.6f, 0.1f) * 0.25f;   // 「ティン」
                float fade = Mathf.Clamp01((seconds - t) / 0.05f);
                d[i] = Soft(v) * 0.8f * fade;
            }
            return Make("synth_rank_" + pitch.ToString("F2") + "_" + seconds.ToString("F2"), d);
        }

        /// <summary>Perfect!! 専用: 上がる 4 音の短いファンファーレと、上に薄いきらめき（実績の音より速く、高い）。</summary>
        public static AudioClip RankPerfect(float seconds = 0.9f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            float[] notes = { 1046.5f, 1318.5f, 1568f, 2093f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float st = t - k * 0.08f;
                    if (st < 0) continue;
                    float decay = k == notes.Length - 1 ? 0.45f : 0.12f;
                    v += Pluck(st, notes[k], decay, 0.2f) * 0.5f + Pluck(st, notes[k] * 2f, decay * 0.5f, 0f) * 0.15f;
                }
                float st3 = t - 0.24f;
                if (st3 > 0) v += Mathf.Sin(2f * Mathf.PI * 4186f * st3) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 8f * st3)) * Mathf.Exp(-st3 / 0.35f) * 0.12f;
                d[i] = Soft(v) * 0.9f;
            }
            return Make("synth_rank_perfect", d);
        }

        /// <summary>通常時の小さな獲得音: 短い「チャリ」1 発（枚数少なめの控えめな音）。</summary>
        public static AudioClip SmallCoin(float seconds = 0.18f)
        {
            int n = Mathf.RoundToInt(Sr * seconds);
            var d = new float[n];
            var rng = new System.Random(31);
            float hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                // 簡易ハイパス（金属っぽいシャリ感）
                float cur = noise; hp = 0.85f * (hp + cur - prev); prev = cur;
                float v = hp * Mathf.Exp(-t / 0.03f) * 0.5f + Pluck(t, 3200f, 0.05f, 0f) * 0.5f + Pluck(t, 4300f, 0.04f, 0f) * 0.3f;
                d[i] = Soft(v) * 0.7f;
            }
            return Make("synth_small_coin", d);
        }
    }
}
