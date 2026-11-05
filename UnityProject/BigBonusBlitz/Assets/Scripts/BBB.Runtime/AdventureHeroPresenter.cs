using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>GameController owns outcomes and audio. This adapter only presents its commands.</summary>
    public sealed class AdventureHeroPresenter : MonoBehaviour
    {
        public HeroPresentationView View {get;private set;}
        public int MotionVersion {get;private set;}
        public bool ImpactReady {get;private set;}
        string requestedWeapon;
        RectTransform queuedTarget,attackTarget;
        Vector2 restFeet;
        float approachElapsed,contactTime,advance;
        int contactPose;
        int approachHandoffPose=-1;
        string attackWeapon,approachMotion;
        float leadElapsed,leadSeconds;
        HeroMotion approachWalk;
        RectTransform rangedTarget;
        Image projectile;
        Vector3 projectileStart;
        HeroWeaponLook rangedLook;
        float flightElapsed;
        public bool IsProjectileFlying=>projectile!=null&&projectile.gameObject.activeSelf;
        public bool IsApproaching=>attackTarget!=null&&leadElapsed<leadSeconds;
        public float Advance=>View.Root.anchoredPosition.x-restFeet.x;
        public static AdventureHeroPresenter Attach(RectTransform host)
        {
            var adapter=host.gameObject.AddComponent<AdventureHeroPresenter>();
            adapter.View=HeroPresentationView.Create(host,new Vector2(0,-host.sizeDelta.y*.5f));
            adapter.restFeet=adapter.View.Root.anchoredPosition;
            adapter.View.Player.Cue+=adapter.OnCue;
            adapter.View.Root.localScale=Vector3.one*(HeroPresentation.Config.gameplayHeight/HeroPresentation.Config.height);
            host.GetComponent<Image>().enabled=false;
            var legacy=host.GetComponent<SpriteAnimator>();if(legacy!=null)legacy.enabled=false;
            foreach(var image in adapter.View.GetComponentsInChildren<Image>())image.raycastTarget=false;
            return adapter;
        }
        public void Equip(EquipItem item)
        {
            string id=HeroPresentation.Weapon(item).id;
            if(requestedWeapon==id)return;
            requestedWeapon=id;View.Player.Equip(item);View.Render();RenderApproach();
        }
        public void Request(string id,bool waitingForReels)
        {
            // Other cut-ins also request f1; they must release when their sequence asks for a loop.
            bool held=View.Player.State=="attack-f1"||View.Player.State=="attack-f2";
            bool loop=id=="idle"||id=="walk"||id=="focus";
            // A queued return to locomotion does not replace the active stroke or its contact.
            bool releasesHold=held&&loop&&!waitingForReels;
            bool startsMotion=releasesHold||!loop||(View.Player.IsLoop&&View.Player.State!=id);
            if(startsMotion){MotionVersion++;ImpactReady=false;BeginApproach(id);}
            if(held&&loop&&!waitingForReels)View.Player.CancelToLoop(id);
            else View.Player.Request(id);
            View.Render();RenderApproach();
        }
        public void PrepareAttackTarget(RectTransform target){queuedTarget=target;}
        public void ClearAttackTarget(){queuedTarget=null;ResetApproach();CancelProjectile();}
        void CancelProjectile(){if(projectile!=null)projectile.gameObject.SetActive(false);rangedTarget=null;rangedLook=null;flightElapsed=0;}
        void OnCue(string cue)
        {
            if(cue!="contact")return;
            if(rangedLook==null){ImpactReady=true;return;}
            if(rangedTarget==null)return;
            View.Render();
            if(projectile==null){projectile=UiSkin.Img(transform,"HeroProjectile",Vector2.zero,Vector2.one,null,Color.white);projectile.raycastTarget=false;}
            bool arrow=!string.IsNullOrEmpty(rangedLook.projectileAtlas);
            projectile.sprite=arrow?HeroPresentation.Sprite(rangedLook.projectileAtlas,rangedLook.projectileX,rangedLook.projectileY,rangedLook.projectileW,rangedLook.projectileH):UiSkin.Glow(64);
            projectile.color=arrow?Color.white:new Color(.6f,.3f,1f,.95f);
            float width=rangedLook.projectileWidth;
            projectile.rectTransform.sizeDelta=arrow?new Vector2(width,width*rangedLook.projectileH/rangedLook.projectileW):new Vector2(width,width);
            projectileStart=arrow?View.Root.TransformPoint(View.GripPosition):View.WeaponTipWorld(View.Player.PoseIndex,View.Player.WeaponId);
            projectile.rectTransform.position=projectileStart;projectile.gameObject.SetActive(true);flightElapsed=0;
        }
        void StepProjectile(float seconds)
        {
            if(!IsProjectileFlying)return;
            if(rangedTarget==null){CancelProjectile();return;}
            flightElapsed+=seconds;
            Vector3 end=rangedTarget.TransformPoint(rangedTarget.rect.center);
            projectile.rectTransform.position=Vector3.Lerp(projectileStart,end,Mathf.Clamp01(flightElapsed/Mathf.Max(.01f,rangedLook.flightSeconds)));
            Vector3 localDirection=transform.InverseTransformVector(end-projectileStart);
            projectile.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(localDirection.y,localDirection.x)*Mathf.Rad2Deg);
            if(flightElapsed>=rangedLook.flightSeconds){ImpactReady=true;CancelProjectile();}
        }
        void ResetApproach(){attackTarget=null;advance=0;approachElapsed=0;leadElapsed=leadSeconds=0;approachWalk=null;approachHandoffPose=-1;View.Root.anchoredPosition=restFeet;View.Render();}
        void BeginApproach(string id)
        {
            ResetApproach();
            CancelProjectile();
            var cfg=HeroPresentation.Config;
            if(queuedTarget!=null&&cfg.approach!=null&&cfg.approach.enabled)
            {
                string weapon=View.Player.WeaponId;
                var motion=System.Array.Find(cfg.motions,m=>m.id==id&&m.weaponId==weapon)
                    ??System.Array.Find(cfg.motions,m=>m.id==id&&string.IsNullOrEmpty(m.weaponId));
                int contact=motion?.events==null?-1:System.Array.IndexOf(motion.events,"contact");
                if(contact>=0)
                {
                    var look=System.Array.Find(cfg.weapons,w=>w.id==weapon);
                    if(look!=null&&look.ranged){rangedLook=look;rangedTarget=queuedTarget;queuedTarget=null;return;}
                    contactTime=0;for(int i=0;i<contact;i++)contactTime+=Mathf.Max(.01f,motion.seconds[i]);
                    contactPose=motion.frames[contact];attackWeapon=weapon;approachMotion=id;
                    attackTarget=queuedTarget;advance=DesiredAdvance();
                    leadSeconds=advance>0?Mathf.Max(0,cfg.approach.leadSeconds):0;leadElapsed=0;
                    approachWalk=System.Array.Find(cfg.motions,m=>m.id=="walk"&&m.weaponId==weapon)
                        ??System.Array.Find(cfg.motions,m=>m.id=="walk"&&string.IsNullOrEmpty(m.weaponId));
                    if(leadSeconds>0&&cfg.approach.handoffSeconds>0)
                    {
                        var windup=System.Array.Find(cfg.motions,m=>m.id=="attack-f2"&&m.weaponId==weapon)
                            ??System.Array.Find(cfg.motions,m=>m.id=="attack-f2"&&string.IsNullOrEmpty(m.weaponId));
                        if(windup?.frames!=null&&windup.frames.Length>0)approachHandoffPose=windup.frames[windup.frames.Length-1];
                    }
                }
            }
            queuedTarget=null;
        }
        float DesiredAdvance()
        {
            var parent=View.Root.parent;
            float target=parent.InverseTransformPoint(attackTarget.TransformPoint(attackTarget.rect.center)).x;
            float tip=parent.InverseTransformPoint(View.WeaponTipWorld(contactPose,attackWeapon)).x;
            return Mathf.Clamp(Advance+target-tip,0,Mathf.Max(0,HeroPresentation.Config.approach.maxDistance));
        }
        public void Step(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;
            StepProjectile(seconds);
            if(IsApproaching)
            {
                float consumed=Mathf.Min(seconds,leadSeconds-leadElapsed);
                leadElapsed+=consumed;seconds-=consumed;
                advance=DesiredAdvance();
                View.Root.anchoredPosition=restFeet+Vector2.right*(advance*Mathf.SmoothStep(0,1,leadElapsed/leadSeconds));
                if(IsApproaching){RenderApproach();return;}
                View.Render();
                if(seconds<=0)return;
            }
            View.Step(seconds);
            if(attackTarget==null){ResetApproach();return;}
            approachElapsed+=seconds;
            float progress;
            if(View.Player.State==approachMotion)
            {
                advance=DesiredAdvance();
                progress=leadSeconds>0?1:contactTime>0?Mathf.Clamp01(approachElapsed/contactTime):1;
            }
            else if(View.Player.State=="recover")progress=1-View.Player.NormalizedTime;
            else {ResetApproach();return;}
            View.Root.anchoredPosition=restFeet+Vector2.right*(advance*Mathf.SmoothStep(0,1,progress));
        }
        void RenderApproach()
        {
            if(!IsApproaching||approachWalk==null)return;
            float handoff=Mathf.Min(leadSeconds,Mathf.Max(0,HeroPresentation.Config.approach.handoffSeconds));
            if(approachHandoffPose>=0&&handoff>0&&leadElapsed>=leadSeconds-handoff)
            {
                View.RenderPose(approachHandoffPose);return;
            }
            float cycle=0;
            for(int i=0;i<approachWalk.frames.Length;i++)
                cycle+=Mathf.Max(.01f,approachWalk.seconds[i]);
            if(cycle<=0)return;
            float phase=Mathf.Repeat(leadElapsed,cycle);
            for(int i=0;i<approachWalk.frames.Length;i++)
            {
                float duration=Mathf.Max(.01f,approachWalk.seconds[i]);
                if(phase<duration){View.RenderPose(approachWalk.frames[i]);return;}
                phase-=duration;
            }
            View.RenderPose(approachWalk.frames[approachWalk.frames.Length-1]);
        }
        public void Cancel(){ClearAttackTarget();MotionVersion++;ImpactReady=false;View.Player.CancelToLoop();View.Render();}
    }
}
