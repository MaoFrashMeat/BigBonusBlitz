using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>Deterministic original Foley/UI cues; no downloaded audio or random gameplay state.</summary>
    public static class MotionSoundSynth
    {
        public static AudioClip Build(MotionCue cue)
        {
            const int rate=44100;
            bool cloth=cue==MotionCue.Cloth, metal=cue==MotionCue.Sword||cue==MotionCue.Equip||cue==MotionCue.Unequip||cue==MotionCue.Guard;
            bool sweep=cue==MotionCue.Open||cue==MotionCue.Close||cue==MotionCue.Travel||cue==MotionCue.Page;
            float duration=cue==MotionCue.Victory?1.2f:cloth?.65f:cue==MotionCue.Travel?.48f:metal?.30f:sweep?.19f:.16f;
            var data=new float[Mathf.CeilToInt(rate*duration)];var rng=new System.Random(217+(int)cue);float lp=0,phase=0;
            float baseHz=cue==MotionCue.Denied?180:cue==MotionCue.Impact?95:cue==MotionCue.Dialogue?420:cue==MotionCue.Hover?1300:cue==MotionCue.Slider?780:cue==MotionCue.Toggle?880:cue==MotionCue.Reveal?1046:660;
            float metalHz=cue==MotionCue.Unequip?850:cue==MotionCue.Sword?1200:cue==MotionCue.Guard?2000:1450;
            float notePitch=cue==MotionCue.Recover?.75f:cue==MotionCue.Sell?1.2f:1;
            float[] notes={523.25f,659.25f,783.99f,1046.5f};
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate,u=t/duration;
                float noise=(float)rng.NextDouble()*2-1;lp+=.16f*(noise-lp);
                float env=Mathf.Min(1,t/.008f)*Mathf.Pow(1-u,2);
                float dir=cue==MotionCue.Close||cue==MotionCue.Unequip?-1:1;
                phase+=2*Mathf.PI*baseHz*(1+dir*.3f*u)/rate;
                float value;
                if(cloth)value=lp*Mathf.Sin(Mathf.PI*u)*.9f;
                else if(metal)value=(Mathf.Sin(t*2*Mathf.PI*metalHz)+.45f*Mathf.Sin(t*2*Mathf.PI*metalHz*1.634f))*.25f+noise*Mathf.Exp(-t*70)*.4f;
                else if(sweep)value=lp*Mathf.Sin(Mathf.PI*u)*1.4f+Mathf.Sin(phase)*.14f;
                else if(cue==MotionCue.Victory||cue==MotionCue.Purchase||cue==MotionCue.Sell||cue==MotionCue.Recover)
                {
                    value=0;float step=cue==MotionCue.Victory?.18f:.035f;
                    for(int n=0;n<4;n++){float age=t-step*n;if(age>=0)value+=Mathf.Sin(age*2*Mathf.PI*notes[n]*notePitch)*Mathf.Min(1,age/.006f)*Mathf.Exp(-age*9)*.28f;}
                }
                else value=Mathf.Sin(phase)*.5f+lp*Mathf.Exp(-t*60)*.15f;
                data[i]=Mathf.Clamp(value*env*.4f,-.8f,.8f);
            }
            var clip=AudioClip.Create("motion_"+cue.ToString().ToLowerInvariant(),data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
