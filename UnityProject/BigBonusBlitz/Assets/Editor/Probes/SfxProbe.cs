using System;
using System.IO;
using BBB.Runtime;
using UnityEditor;
using UnityEngine;

// 検証用（scratchpad のバッチ複製だけに置く）。合成 SE を WAV に書き出して、ピークと実効値を出す
public static class SfxProbe
{
    /// <summary>効果音の一覧（AudioManager.SeDefs）の合成音を全部 wav に書く（tools/se/synth。se_viewer の試聴用）。SFX_SYNTH_OUTPUT に。</summary>
    public static void ExportSynth()
    {
        string output = Environment.GetEnvironmentVariable("SFX_SYNTH_OUTPUT");
        Directory.CreateDirectory(output);
        foreach (var d in AudioManager.SeDefs)
        {
            if (d.synth == null) continue;
            var clip = d.synth();
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            WriteWav(Path.Combine(output, d.key + ".wav"), data, clip.frequency);
            Debug.Log($"SYNTH {d.key}: {clip.length:F2}s");
        }
        // UI の動きの音（MotionSoundSynth）
        foreach (var m in AudioManager.MotionDefs)
        {
            var clip = MotionSoundSynth.Build(m.cue);
            var data = new float[clip.samples * clip.channels]; clip.GetData(data, 0);
            WriteWav(Path.Combine(output, "motion_" + m.cue.ToString().ToLowerInvariant() + ".wav"), data, clip.frequency);
        }
        // 素材が無いときの合成（敵出現の 接近 + 着地）
        foreach (var (name, clip) in new (string, AudioClip)[] { ("enemy_appear_whoosh", SfxSynth.AppearWhoosh(0.5f)), ("enemy_appear_impact", SfxSynth.AppearImpact(0.7f)) })
        {
            var data = new float[clip.samples]; clip.GetData(data, 0); WriteWav(Path.Combine(output, name + ".wav"), data, clip.frequency);
        }
    }

    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("SFX_OUTPUT");
        Directory.CreateDirectory(output);
        var clips = new (string, AudioClip)[]
        {
            ("achievement", SfxSynth.Achievement()), ("pickup_soul", SfxSynth.PickupSoul()),
            ("pickup_ember", SfxSynth.PickupEmber()), ("pickup_item", SfxSynth.PickupItem()),
            ("ref_bell", SfxSynth.BellDing()), ("ref_small_coin", SfxSynth.SmallCoin()),
            ("gain_exp", SfxSynth.GainExp()), ("gain_games", SfxSynth.GainGames()),
            ("rank_perfect", SfxSynth.RankPerfect()), ("rank_excellent", SfxSynth.RankChime(1.5f, 0.5f)), ("rank_cool", SfxSynth.RankChime(1f, 0.3f)), ("rank_koma2", SfxSynth.RankChime(0.71f, 0.2f)),
        };
        foreach (var (name, clip) in clips)
        {
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0, sum = 0;
            foreach (var v in data) { peak = Mathf.Max(peak, Mathf.Abs(v)); sum += v * v; }
            float rms = Mathf.Sqrt(sum / data.Length);
            Debug.Log($"SFX {name}: {clip.length:F2}s peak {peak:F3} rms {rms:F3}");
            File.WriteAllText(Path.Combine(output, name + ".txt"), $"{clip.length:F2} {peak:F3} {rms:F3}");
            WriteWav(Path.Combine(output, name + ".wav"), data, clip.frequency);
        }
    }

    static void WriteWav(string path, float[] data, int sr)
    {
        using var bw = new BinaryWriter(File.Create(path));
        int bytes = data.Length * 2;
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); bw.Write(36 + bytes); bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); bw.Write(16); bw.Write((short)1); bw.Write((short)1); bw.Write(sr); bw.Write(sr * 2); bw.Write((short)2); bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data")); bw.Write(bytes);
        foreach (var v in data) bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
    }
}
