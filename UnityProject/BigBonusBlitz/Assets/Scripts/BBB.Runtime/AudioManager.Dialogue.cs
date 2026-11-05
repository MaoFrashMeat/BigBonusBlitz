using UnityEngine;

namespace BBB.Runtime
{
    public sealed partial class AudioManager
    {
        double nextDialogueTick;
        /// <summary>Uses the existing dialogue cue pool and SE master, without stealing action voices.</summary>
        public void DialogueTick(string speaker)
        {
            if(motionVoices==null||_se==null||_se.volume<=0||!isActiveAndEnabled||(!Application.isFocused&&!Application.isBatchMode))return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now<nextDialogueTick)return;
            nextDialogueTick=now+Mathf.Max(.04f,AdventurePresentation.Config.typingSoundGap);
            if(!motionClips.TryGetValue(MotionCue.Dialogue,out var clip)||clip==null)return;
            AudioSource voice=null;foreach(var candidate in motionVoices)if(!candidate.isPlaying){voice=candidate;break;}
            if(voice==null)return;
            voice.priority=220;voice.volume=_se.volume;
            voice.pitch=speaker=="声"||speaker=="？？"?.8f:speaker=="灯守"||speaker=="宿の主人"?.92f:1.12f;
            voice.PlayOneShot(clip,Mathf.Clamp01(AdventurePresentation.Config.typingSoundGain));SoundSequence++;
        }
    }
}
