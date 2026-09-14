using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BBB.Runtime;
using UnityEditor;
using UnityEngine;

public static class MotionSoundValidation
{
    static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
    public static void Run()
    {
        Check(Application.isBatchMode && Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"),"Use isolated shop-qa project only");
        string output=Environment.GetEnvironmentVariable("MOTION_SOUND_OUTPUT");Check(!string.IsNullOrEmpty(output),"MOTION_SOUND_OUTPUT required");Directory.CreateDirectory(output);
        var rows=new List<string>();
        foreach(MotionCue cue in Enum.GetValues(typeof(MotionCue)))
        {
            var clip=MotionSoundSynth.Build(cue);var samples=new float[clip.samples];Check(clip.GetData(samples,0),"Cannot read "+cue);
            double energy=0;float peak=0;
            foreach(float v in samples){Check(!float.IsNaN(v)&&!float.IsInfinity(v),"Non-finite "+cue);peak=Mathf.Max(peak,Mathf.Abs(v));energy+=v*v;}
            Check(peak>.001f && peak<.8f && energy>0,"Silent or clipping "+cue);
            Check(Mathf.Abs(samples[0])<.0001f && Mathf.Abs(samples[samples.Length-1])<.0001f,"Discontinuous endpoints "+cue);
            var repeat=MotionSoundSynth.Build(cue);var same=new float[repeat.samples];repeat.GetData(same,0);Check(samples.SequenceEqual(same),"Non-deterministic synthesis "+cue);
            string name=cue.ToString().ToLowerInvariant();
            using(var file=new BinaryWriter(File.Create(Path.Combine(output,name+".wav"))))
            {
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+samples.Length*2);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(44100);file.Write(88200);file.Write((short)2);file.Write((short)16);file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(samples.Length*2);foreach(float v in samples)file.Write((short)Mathf.RoundToInt(v*32767));
            }
            rows.Add(name);UnityEngine.Object.DestroyImmediate(clip);UnityEngine.Object.DestroyImmediate(repeat);
        }
        var audio=AudioManager.Create();const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var limits=(Dictionary<MotionCue,double>)typeof(AudioManager).GetField("nextMotion",flags).GetValue(audio);
        var voices=(AudioSource[])typeof(AudioManager).GetField("motionVoices",flags).GetValue(audio);
        Check(voices.Length==8,"Unbounded voice pool");
        audio.SeVolume=.7f;int before=audio.SoundSequence;
        audio.Motion(MotionCue.Click);audio.Motion(MotionCue.Click);Check(audio.SoundSequence==before+1,"Repeated click not throttled");
        audio.SeVolume=0;before=audio.SoundSequence;audio.Motion(MotionCue.Victory);Check(audio.SoundSequence==before && voices.All(s=>s.volume==0),"Mute did not cover motion voices");
        audio.SeVolume=.4f;Check(voices.All(s=>Mathf.Abs(s.volume-.4f)<.001f),"Live SE gain not propagated");
        limits.Clear();before=audio.SoundSequence;MotionSound.Invoke("Confirm",()=>audio.Motion(MotionCue.Purchase));Check(audio.SoundSequence==before+1,"Generic click doubled transaction sound");
        limits.Clear();before=audio.SoundSequence;MotionSound.Invoke("Next",()=>{});Check(audio.SoundSequence==before+1,"Silent navigation callback");
        var flush=typeof(AudioManager).GetMethod("FlushMotion",flags);
        var lastSound=typeof(AudioManager).GetField("lastActionSound",flags);
        var lastFrame=typeof(AudioManager).GetField("lastActionFrame",flags);
        limits.Clear();audio.Attack();before=audio.SoundSequence;audio.MotionIfQuiet(MotionCue.Impact);flush.Invoke(audio,null);Check(audio.SoundSequence==before,"Duplicate hit Foley over existing attack");
        limits.Clear();lastSound.SetValue(audio,-100d);audio.MotionIfQuiet(MotionCue.Impact);audio.Attack();before=audio.SoundSequence;flush.Invoke(audio,null);Check(audio.SoundSequence==before,"FX-before-attack produced duplicate sound");
        limits.Clear();lastSound.SetValue(audio,-100d);lastFrame.SetValue(audio,-1);before=audio.SoundSequence;audio.MotionIfQuiet(MotionCue.Reveal);audio.MotionIfQuiet(MotionCue.Impact);audio.MotionIfQuiet(MotionCue.Recover);flush.Invoke(audio,null);Check(audio.SoundSequence==before+1 && limits.ContainsKey(MotionCue.Impact),"Concurrent fallback FX were not coalesced by priority");
        before=audio.SoundSequence;flush.Invoke(audio,null);Check(audio.SoundSequence==before,"Fallback was replayed next frame");
        audio.SeVolume=-2;Check(audio.SeVolume==0,"Negative SE gain");audio.SeVolume=2;Check(audio.SeVolume==1,"Excessive SE gain");audio.SeVolume=.8f;
        AtelierUiValidation.Run();
        File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"cues\":"+rows.Count+",\"waveforms\":\"finite,non-silent,headroom,continuous endpoints,deterministic\",\"voicePool\":8,\"checks\":[\"cooldown\",\"mute\",\"live volume\",\"no doubled transaction\",\"navigation fallback\",\"existing attack precedence\",\"five real Unity screens and transactions\"]}");
        Debug.Log("MOTION_SOUND_VALIDATION passed: "+rows.Count+" cues");
    }
}
