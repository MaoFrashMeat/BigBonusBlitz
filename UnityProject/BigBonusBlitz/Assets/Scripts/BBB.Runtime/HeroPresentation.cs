using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    [Serializable] public sealed class HeroHandPatch
    {
        public int x,y,w,h; // Relative to the body pose, from its top-left.
    }
    [Serializable] public sealed class HeroPose
    {
        public string atlas;
        public int x,y,w,h; // Source pixels, y measured from the top; PNG remains unchanged.
        public float foot, height, gripX, gripY, swordAngle, spearAngle;
        public bool twoHanded;
        public float supportX,supportY;
        public HeroHandPatch swordHand; // Optional foreground grip; original atlas pixels stay unchanged.
        public HeroHandPatch supportHand;
        public bool bowDraw;
    }
    [Serializable] public sealed class HeroMotion
    {
        public string id,next,weaponId;
        public int[] frames;
        public float[] seconds;
        public string[] events;
        public bool loop,hold;
    }
    [Serializable] public sealed class HeroWeaponLook
    {
        public string id,craftId,atlas;
        public bool foregroundGrip;
        public bool alignToSupport, ranged, bowString;
        public float stringTopX,stringTopY,stringBottomX,stringBottomY,flightSeconds;
        public string projectileAtlas;
        public int projectileX,projectileY,projectileW,projectileH;
        public float projectileWidth;
        public int x,y,w,h;
        public float gripX,gripY,height;
    }
    [Serializable] public sealed class HeroEnemyLook
    {
        public string id,name,atlas;
        public int x,y,w,h;
        public float height;
    }
    [Serializable] public sealed class HeroSound
    {
        public string source,cue,key,fallback;
        public float gain;
    }
    [Serializable] public sealed class HeroApproach
    {
        public bool enabled;
        public float maxDistance;
        public float leadSeconds;
        public float handoffSeconds;
    }
    [Serializable] public sealed class HeroPresentationConfig
    {
        public string status;
        public bool gameplayEnabled;
        public float gameplayHeight=190;
        public float height=230,walkSeconds=0.85f;
        public HeroApproach approach;
        public HeroPose[] poses;
        public HeroMotion[] motions;
        public HeroWeaponLook[] weapons;
        public HeroEnemyLook[] enemies;
        public HeroSound[] sounds;
    }
    public static class HeroPresentation
    {
        static HeroPresentationConfig config;
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public static HeroPresentationConfig Config => config ??= JsonUtility.FromJson<HeroPresentationConfig>(Resources.Load<TextAsset>("Data/hero_presentation_prototype").text);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;sprites.Clear();}
        public static Sprite Sprite(string atlas,int x,int y,int w,int h)
        {
            string key=$"{atlas}:{x},{y},{w},{h}";
            if(sprites.TryGetValue(key,out var cached)&&cached!=null)return cached;
            var t=Resources.Load<Texture2D>("Art/HeroPrototype/"+atlas);
            if(t==null||x<0||y<0||x+w>t.width||y+h>t.height)throw new InvalidOperationException("Invalid hero atlas rectangle: "+key);
            return sprites[key]=UnityEngine.Sprite.Create(t,new Rect(x,t.height-y-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }
        /// <summary>Drops the cache only. A sprite still shown elsewhere stays valid until nothing references it.</summary>
        public static void ReleaseAtlas(string atlas)
        {
            string prefix=atlas+":";var stale=new List<string>();
            foreach(var key in sprites.Keys)if(key.StartsWith(prefix,StringComparison.Ordinal))stale.Add(key);
            foreach(var key in stale)sprites.Remove(key);
        }
        public static HeroWeaponLook Weapon(EquipItem item)
        {
            if(item!=null&&!string.IsNullOrEmpty(item.craftId))foreach(var w in Config.weapons)if(!string.IsNullOrEmpty(w.craftId)&&w.craftId==item.craftId)return w;
            if(item!=null)foreach(var w in Config.weapons)if(w.id==item.baseId)return w;
            return Config.weapons[0];
        }
        public static HeroSound Sound(string source,string cue)=>Array.Find(Config.sounds,s=>s.source==source&&s.cue==cue);
    }
    /// <summary>Presentation-only clock. No machine, random source, damage, rewards or save access.</summary>
    public sealed class HeroMotionPlayer
    {
        readonly HeroPresentationConfig data;
        readonly HashSet<string> emitted=new HashSet<string>();
        HeroMotion motion;
        int frame;
        float elapsed;
        bool frameCueSent;
        string requestedLoop="idle",pendingWeapon;
        public string State=>motion.id;
        public bool IsLoop=>motion.loop;
        public int PoseIndex=>motion.frames[frame];
        /// <summary>Normalized time inside the current clip, used to keep locomotion continuous across a weapon swap.</summary>
        public float NormalizedTime
        {
            get
            {
                float total=0,elapsedInClip=elapsed;
                for(int i=0;i<motion.seconds.Length;i++)
                {
                    total+=Mathf.Max(.01f,motion.seconds[i]);
                    if(i<frame)elapsedInClip+=Mathf.Max(.01f,motion.seconds[i]);
                }
                return total>0?Mathf.Clamp01(elapsedInClip/total):0;
            }
        }
        public string WeaponId {get;private set;}
        public string PendingWeapon=>pendingWeapon;
        public event Action<string> Cue;
        public HeroMotionPlayer(HeroPresentationConfig data)
        {
            this.data=data;WeaponId=data.weapons[0].id;Start("idle");
        }
        HeroMotion Find(string id)=>Array.Find(data.motions,m=>m.id==id&&m.weaponId==WeaponId)
            ??Array.Find(data.motions,m=>m.id==id&&string.IsNullOrEmpty(m.weaponId))
            ??throw new ArgumentException("Unknown hero motion "+id);
        void Start(string id)
        {
            var next=Find(id);
            if((next.loop||id=="recover")&&pendingWeapon!=null){WeaponId=pendingWeapon;pendingWeapon=null;next=Find(id);}
            motion=next;frame=0;elapsed=0;frameCueSent=false;
        }
        public void Request(string id)
        {
            var next=Find(id);
            if(next.loop)
            {
                requestedLoop=id;
                if(!motion.loop||State==id)return; // Same loop never restarts its foot phase.
            }
            Start(id); // Explicit actions can interrupt; passive locomotion cannot erase an action.
        }
        public void Equip(EquipItem item)
        {
            string id=HeroPresentation.Weapon(item).id;
            if(motion.loop||State=="recover")
            {
                // Preserve normalized clip time when a weapon has a different stance/clip length.
                float time=elapsed,total=0;for(int i=0;i<motion.seconds.Length;i++){total+=motion.seconds[i];if(i<frame)time+=motion.seconds[i];}
                string state=State;WeaponId=id;pendingWeapon=null;motion=Find(state);
                float nextTotal=0;foreach(float seconds in motion.seconds)nextTotal+=seconds;
                elapsed=total>0?time/total*nextTotal:0;frame=0;
                while(frame<motion.seconds.Length-1&&elapsed>=motion.seconds[frame]){elapsed-=motion.seconds[frame];frame++;}
                frameCueSent=true; // Replacing the weapon is not a new footfall/action.
            }
            else pendingWeapon=id; // Finish the current stroke with its starting weapon.
        }
        public void CancelToLoop(string id="idle")
        {
            if(!Find(id).loop)throw new ArgumentException("Cancel target must be a loop");
            requestedLoop=id;Start(id);
        }
        public void Tick(float delta)
        {
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            elapsed+=delta;emitted.Clear();
            for(int guard=0;guard<4096;guard++)
            {
                if(!frameCueSent)
                {
                    frameCueSent=true;
                    string cue=motion.events!=null&&frame<motion.events.Length?motion.events[frame]:null;
                    if(!string.IsNullOrEmpty(cue)&&emitted.Add(cue))Cue?.Invoke(cue);
                }
                float duration=Mathf.Max(.01f,motion.seconds[frame]);
                if(elapsed<duration)return;
                if(motion.hold&&frame==motion.frames.Length-1){elapsed=duration;return;}
                float remainder=elapsed-duration;
                frame++;
                if(frame>=motion.frames.Length)
                {
                    if(motion.loop)frame=0;
                    else Start(string.IsNullOrEmpty(motion.next)?requestedLoop:motion.next);
                }
                elapsed=remainder;
                frameCueSent=false;
            }
            elapsed=0; // A suspended editor must not replay minutes of decorative events.
        }
    }
    /// <summary>Separate body and rigid weapon, joined using authored per-pose grip coordinates.</summary>
    public sealed class HeroPresentationView : MonoBehaviour
    {
        public RectTransform Root {get;private set;}
        public HeroMotionPlayer Player {get;private set;}
        public Image Body {get;private set;}
        public Image Weapon {get;private set;}
        public Image Hand {get;private set;}
        public Image SupportHand {get;private set;}
        public Vector2 GripPosition {get;private set;}
        public Vector2 SupportPosition {get;private set;}
        public bool TwoHanded {get;private set;}
        Image shadow,marker,supportMarker,stringTop,stringBottom,nockedArrow;
        int lastPose=-1;
        string lastWeapon;
        public static HeroPresentationView Create(Transform parent,Vector2 feet)
        {
            var root=UiSkin.Rect(parent,"SaliaPrototype",feet,new Vector2(330,330));
            var v=root.gameObject.AddComponent<HeroPresentationView>();v.Root=root;v.Player=new HeroMotionPlayer(HeroPresentation.Config);
            v.shadow=UiSkin.Img(root,"ContactShadow",new Vector2(0,2),new Vector2(115,19),UiSkin.Glow(64),new Color(.015f,.025f,.05f,.6f));
            v.Weapon=UiSkin.Img(root,"RigidWeapon",Vector2.zero,Vector2.one,null,Color.white);
            v.Body=UiSkin.Img(root,"Body",Vector2.zero,new Vector2(300,300),null,Color.white);
            v.Hand=UiSkin.Img(root,"ForegroundGrip",Vector2.zero,Vector2.one,null,Color.white);v.Hand.gameObject.SetActive(false);
            v.SupportHand=UiSkin.Img(root,"ForegroundSupportGrip",Vector2.zero,Vector2.one,null,Color.white);
            v.stringTop=UiSkin.Img(root,"BowStringTop",Vector2.zero,Vector2.one,null,new Color(.85f,.76f,.58f));
            v.stringBottom=UiSkin.Img(root,"BowStringBottom",Vector2.zero,Vector2.one,null,new Color(.85f,.76f,.58f));
            v.nockedArrow=UiSkin.Img(root,"NockedArrow",Vector2.zero,Vector2.one,null,Color.white);
            v.marker=UiSkin.Img(root,"GripMarker",Vector2.zero,new Vector2(7,7),UiSkin.Circle(16),Color.cyan);v.marker.gameObject.SetActive(false);
            v.supportMarker=UiSkin.Img(root,"SupportMarker",Vector2.zero,new Vector2(7,7),UiSkin.Circle(16),Color.yellow);v.supportMarker.gameObject.SetActive(false);
            v.Render();return v;
        }
        public void ShowGrip(bool show){marker.gameObject.SetActive(show);supportMarker.gameObject.SetActive(show&&TwoHanded);}
        public void Step(float seconds){Player.Tick(seconds);Render();}
        // Predict the rigid tip at contact without changing the active pose or emitting cues.
        public Vector3 WeaponTipWorld(int poseIndex,string weaponId)
        {
            var cfg=HeroPresentation.Config;var p=cfg.poses[poseIndex];
            var look=Array.Find(cfg.weapons,w=>w.id==weaponId)??cfg.weapons[0];
            float scale=cfg.height/p.height;
            var grip=new Vector3((p.gripX-p.w*.5f)*scale,(p.h-p.gripY-p.foot)*scale,0);
            bool spear=weaponId=="w_spear";
            float angle=spear?p.spearAngle:p.swordAngle;
            if((spear||look.alignToSupport)&&p.twoHanded)angle=Mathf.Atan2(p.gripY-p.supportY,p.supportX-p.gripX)*Mathf.Rad2Deg-90;
            var tip=new Vector3((look.w*.5f-look.gripX)*look.height/look.h,look.gripY*look.height/look.h,0);
            return Root.TransformPoint(grip+Quaternion.Euler(0,0,angle)*tip);
        }
        public int RenderedPoseIndex=>lastPose;
        public void Render(){RenderPose(Player.PoseIndex);}
        // Spatial approach can show authored walk keys while the combat clock waits.
        public void RenderPose(int i)
        {
            if(i==lastPose&&lastWeapon==Player.WeaponId)return;
            lastPose=i;lastWeapon=Player.WeaponId;
            var cfg=HeroPresentation.Config;var p=cfg.poses[i];
            Body.sprite=HeroPresentation.Sprite(p.atlas,p.x,p.y,p.w,p.h);
            float scale=cfg.height/Mathf.Max(1,p.height);
            Body.rectTransform.sizeDelta=new Vector2(p.w,p.h)*scale;
            Body.rectTransform.anchoredPosition=new Vector2(0,(p.h*.5f-p.foot)*scale);
            GripPosition=new Vector2(p.gripX-p.w*.5f,p.h-p.gripY-p.foot)*scale;
            var look=Array.Find(cfg.weapons,w=>w.id==Player.WeaponId)??cfg.weapons[0];
            Weapon.sprite=HeroPresentation.Sprite(look.atlas,look.x,look.y,look.w,look.h);
            Weapon.rectTransform.pivot=new Vector2(look.gripX/look.w,1-look.gripY/look.h);
            Weapon.rectTransform.sizeDelta=new Vector2(look.w/(float)look.h*look.height,look.height);
            Weapon.rectTransform.anchoredPosition=GripPosition;
            TwoHanded=(look.id=="w_spear"||look.alignToSupport)&&p.twoHanded;
            SupportPosition=new Vector2(p.supportX-p.w*.5f,p.h-p.supportY-p.foot)*scale;
            float angle=look.id=="w_spear"?p.spearAngle:p.swordAngle;
            if(TwoHanded){var direction=SupportPosition-GripPosition;angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;}
            Weapon.rectTransform.localRotation=Quaternion.Euler(0,0,angle);
            var hand=look.id=="w_blade"||look.foregroundGrip?p.swordHand:null;
            bool front=hand!=null&&hand.w>0&&hand.h>0;
            if(front)
            {
                if(hand.x<0||hand.y<0||hand.x+hand.w>p.w||hand.y+hand.h>p.h)
                    throw new InvalidOperationException("Hand patch outside hero pose: "+i);
                Body.transform.SetSiblingIndex(1);Weapon.transform.SetSiblingIndex(2);Hand.transform.SetSiblingIndex(3);
                Hand.sprite=HeroPresentation.Sprite(p.atlas,p.x+hand.x,p.y+hand.y,hand.w,hand.h);
                Hand.rectTransform.sizeDelta=new Vector2(hand.w,hand.h)*scale;
                Hand.rectTransform.anchoredPosition=new Vector2(hand.x+hand.w*.5f-p.w*.5f,p.h-hand.y-hand.h*.5f-p.foot)*scale;
            }
            else {Weapon.transform.SetSiblingIndex(1);Body.transform.SetSiblingIndex(2);}
            Hand.gameObject.SetActive(front);
            var support=p.supportHand;
            bool supportFront=TwoHanded&&front&&support!=null&&support.w>0&&support.h>0;
            SupportHand.gameObject.SetActive(supportFront);
            if(supportFront)
            {
                SupportHand.sprite=HeroPresentation.Sprite(p.atlas,p.x+support.x,p.y+support.y,support.w,support.h);
                SupportHand.rectTransform.sizeDelta=new Vector2(support.w,support.h)*scale;
                SupportHand.rectTransform.anchoredPosition=new Vector2(support.x+support.w*.5f-p.w*.5f,p.h-support.y-support.h*.5f-p.foot)*scale;
                SupportHand.transform.SetSiblingIndex(Hand.transform.GetSiblingIndex()+1);
            }
            stringTop.gameObject.SetActive(look.bowString);stringBottom.gameObject.SetActive(look.bowString);
            nockedArrow.gameObject.SetActive(look.bowString&&p.bowDraw);
            if(look.bowString)
            {
                Vector2 top=WeaponPoint(look,look.stringTopX,look.stringTopY);
                Vector2 bottom=WeaponPoint(look,look.stringBottomX,look.stringBottomY);
                Vector2 nock=p.bowDraw?SupportPosition:(top+bottom)*.5f;
                Line(stringTop,top,nock);Line(stringBottom,nock,bottom);
                stringTop.transform.SetSiblingIndex(1);stringBottom.transform.SetSiblingIndex(1);
                if(p.bowDraw)
                {
                    nockedArrow.sprite=HeroPresentation.Sprite(look.projectileAtlas,look.projectileX,look.projectileY,look.projectileW,look.projectileH);
                    nockedArrow.rectTransform.pivot=new Vector2(0,.5f);
                    nockedArrow.rectTransform.sizeDelta=new Vector2(look.projectileWidth,look.projectileWidth*look.projectileH/look.projectileW);
                    nockedArrow.rectTransform.anchoredPosition=nock;
                    nockedArrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(GripPosition.y-nock.y,GripPosition.x-nock.x)*Mathf.Rad2Deg);
                    nockedArrow.transform.SetSiblingIndex(1);
                }
            }
            marker.rectTransform.anchoredPosition=GripPosition;
            supportMarker.rectTransform.anchoredPosition=SupportPosition;supportMarker.gameObject.SetActive(marker.gameObject.activeSelf&&TwoHanded);
        }
        Vector2 WeaponPoint(HeroWeaponLook look,float x,float y)
        {
            var local=new Vector3((x-look.gripX)*look.height/look.h,(look.gripY-y)*look.height/look.h,0);
            return GripPosition+(Vector2)(Weapon.rectTransform.localRotation*local);
        }
        static void Line(Image image,Vector2 from,Vector2 to)
        {
            var delta=to-from;image.rectTransform.anchoredPosition=(from+to)*.5f;
            image.rectTransform.sizeDelta=new Vector2(delta.magnitude,1.2f);
            image.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
