using System.Collections.Generic;
using UnityEngine;

namespace BBB.Runtime
{
    public sealed partial class AudioManager
    {
        readonly Dictionary<MotionCue, AudioClip> motionClips = new Dictionary<MotionCue, AudioClip>();
        readonly Dictionary<MotionCue, double> nextMotion = new Dictionary<MotionCue, double>();
        readonly List<AudioClip> generatedMotionClips = new List<AudioClip>();
        AudioSource[] motionVoices;
        public int SoundSequence { get; private set; }
        double lastActionSound = -100;
        int lastActionFrame = -1;
        MotionCue? pendingMotion;
        void MarkSound() { SoundSequence++; lastActionSound = Time.realtimeSinceStartupAsDouble; lastActionFrame=Time.frameCount; }
        void InitMotion()
        {
            motionVoices = new AudioSource[8];
            for (int i=0;i<motionVoices.Length;i++) { var s=gameObject.AddComponent<AudioSource>(); s.playOnAwake=false;s.spatialBlend=0;s.priority=160;motionVoices[i]=s; }
            foreach (MotionCue cue in System.Enum.GetValues(typeof(MotionCue)))
            {
                var clip=Resources.Load<AudioClip>("Audio/SE/se_motion_"+cue.ToString().ToLowerInvariant());
                if (clip==null) { clip=MotionSoundSynth.Build(cue); generatedMotionClips.Add(clip); }
                motionClips.Add(cue,clip);
            }
        }
        void SetMotionVolume(float value) { if(motionVoices!=null)foreach(var s in motionVoices)s.volume=value; }
        public void MotionIfQuiet(MotionCue cue)
        {
            // Resolve after gameplay callbacks: some callers spawn FX before playing their attack sound.
            if (!pendingMotion.HasValue || FallbackPriority(cue) > FallbackPriority(pendingMotion.Value)) pendingMotion=cue;
        }
        static int FallbackPriority(MotionCue cue) => cue==MotionCue.Impact || cue==MotionCue.Guard ? 3 : cue==MotionCue.Reveal || cue==MotionCue.Recover ? 2 : 1;
        void LateUpdate() => FlushMotion();
        void FlushMotion()
        {
            var cue=pendingMotion;pendingMotion=null;
            if(cue.HasValue && lastActionFrame!=Time.frameCount && Time.realtimeSinceStartupAsDouble-lastActionSound>.09)Motion(cue.Value);
        }
        void OnDisable() { pendingMotion=null; }
        public void Motion(MotionCue cue)
        {
            if(motionVoices==null || !isActiveAndEnabled || (!Application.isFocused && !Application.isBatchMode) || _se.volume<=0) return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(nextMotion.TryGetValue(cue,out double next)&&now<next)return;
            bool ambient=cue==MotionCue.Cloth||cue==MotionCue.Sword;
            bool small=cue==MotionCue.Hover||cue==MotionCue.Slider||cue==MotionCue.Page;
            double gap=ambient?2.8:small?.12:.065;
            nextMotion[cue]=now+gap;
            AudioSource voice=null;
            foreach(var s in motionVoices)if(!s.isPlaying){voice=s;break;}
            // Drop decorative sounds at capacity; important actions can replace a quiet voice.
            if(voice==null) { if(ambient||small)return;voice=motionVoices[0];foreach(var s in motionVoices)if(s.priority>voice.priority)voice=s;voice.Stop(); }
            voice.priority=ambient?230:small?200:100;
            voice.pitch=1f;voice.volume=_se.volume;
            voice.clip=motionClips[cue];
            float gain=ambient?.12f:small?.28f:.65f;
            // Clip gain is separate from the master, so changing volume also affects playing voices.
            voice.PlayOneShot(voice.clip,gain);
            SoundSequence++;
            if(!ambient && !small){lastActionSound=now;lastActionFrame=Time.frameCount;}
        }
        void OnDestroy() { foreach(var clip in generatedMotionClips)if(clip!=null){if(Application.isPlaying)Destroy(clip);else DestroyImmediate(clip);} }
    }
}
