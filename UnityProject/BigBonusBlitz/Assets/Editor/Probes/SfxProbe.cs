using System;
using System.IO;
using BBB.Runtime;
using UnityEditor;
using UnityEngine;

// 検証用（scratchpad のバッチ複製だけに置く）。合成 SE を WAV に書き出して、ピークと実効値を出す
public static class SfxProbe
{
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("SFX_OUTPUT");
        Directory.CreateDirectory(output);
        var clips = new (string, AudioClip)[]
        {
            ("achievement", SfxSynth.Achievement()), ("pickup_soul", SfxSynth.PickupSoul()),
            ("pickup_ember", SfxSynth.PickupEmber()), ("pickup_item", SfxSynth.PickupItem()),
            ("ref_bell", SfxSynth.BellDing()), ("ref_small_coin", SfxSynth.SmallCoin()),
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
