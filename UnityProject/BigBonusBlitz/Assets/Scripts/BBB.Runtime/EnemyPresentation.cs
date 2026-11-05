using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    [Serializable] public sealed class EnemyPose
    {
        public int x,y,w,h;
        public float foot,offsetX,offsetY;
    }
    [Serializable] public sealed class EnemyMotion
    {
        public string id,next;
        public int[] frames;
        public float[] seconds,eventAt;
        public string[] events,phases;
        public bool loop,hold;
    }
    [Serializable] public sealed class EnemyLook
    {
        public string id,atlas;
        public float referenceHeight,displayHeight,shadowWidth,gameplayHeight;
        public EnemyPose[] poses;
        public EnemyMotion[] motions;
    }
    [Serializable] public sealed class EnemyPresentationConfig
    {
        public bool gameplayEnabled;
        public float gameplayContactGap;
        public string[] gameplayTypes;
        public EnemyLook[] enemies;
    }
    public static class EnemyPresentation
    {
        static EnemyPresentationConfig config;
        public static EnemyPresentationConfig Config=>config??=JsonUtility.FromJson<EnemyPresentationConfig>(Resources.Load<TextAsset>("Data/enemy_presentation_prototype").text);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;}
        public static EnemyLook Find(string id)=>Array.Find(Config.enemies,e=>e.id==id)??throw new ArgumentException("Unknown preview enemy "+id);
    }
    /// <summary>Visual timing only. Never chooses an encounter, applies damage, or writes a save.</summary>
    public sealed class EnemyMotionPlayer
    {
        readonly EnemyLook data;
        readonly HashSet<string> emitted=new HashSet<string>();
        EnemyMotion motion;
        int frame;
        float elapsed;
        bool frameCueSent;
        Vector2 movementFrom;
        public string State=>motion.id;
        public string Phase=>motion.phases[frame];
        public int PoseIndex=>motion.frames[frame];
        public float Progress=>Mathf.Clamp01(elapsed/motion.seconds[frame]);
        public Vector2 Offset=>Vector2.Lerp(movementFrom,Target,Mathf.SmoothStep(0,1,Progress));
        Vector2 Target=>new Vector2(data.poses[PoseIndex].offsetX,data.poses[PoseIndex].offsetY);
        public event Action<string> Cue;
        public EnemyMotionPlayer(EnemyLook data){this.data=data;Start("idle");movementFrom=Target;}
        void Start(string id){motion=Array.Find(data.motions,m=>m.id==id)??throw new ArgumentException("Unknown enemy motion "+id);frame=0;elapsed=0;frameCueSent=false;}
        public void Request(string id)
        {
            if(State==id||State=="defeat"&&id!="idle")return;
            movementFrom=Offset;Start(id);
        }
        public void Tick(float delta)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            elapsed+=delta;emitted.Clear();
            for(int guard=0;guard<4096;guard++)
            {
                float duration=motion.seconds[frame];
                if(!frameCueSent&&elapsed>=duration*motion.eventAt[frame])
                {
                    frameCueSent=true;string cue=motion.events[frame];
                    if(!string.IsNullOrEmpty(cue)&&emitted.Add(cue))Cue?.Invoke(cue);
                }
                if(elapsed<duration)return;
                if(motion.hold&&frame==motion.frames.Length-1){elapsed=duration;return;}
                float remainder=elapsed-duration;movementFrom=Target;frame++;
                if(frame==motion.frames.Length){if(motion.loop)frame=0;else Start(motion.next??"idle");}
                elapsed=remainder;frameCueSent=false;
            }
            elapsed=0;
        }
    }
    public sealed class EnemyPresentationView : MonoBehaviour
    {
        public EnemyMotionPlayer Player {get;private set;}
        public Image Body {get;private set;}
        public EnemyLook Look {get;private set;}
        public float MovementScaleX {get;set;}=1f;
        RectTransform root;
        Image shadow;
        int lastPose=-1;
        public static EnemyPresentationView Create(Transform parent,Vector2 feet)
        {
            var rect=UiSkin.Rect(parent,"EnemyPreview",feet,Vector2.one);
            var view=rect.gameObject.AddComponent<EnemyPresentationView>();view.root=rect;
            view.shadow=UiSkin.Img(rect,"ContactShadow",Vector2.zero,new Vector2(100,16),UiSkin.Glow(64),new Color(.015f,.025f,.05f,.6f));
            view.Body=UiSkin.Img(rect,"Body",Vector2.zero,Vector2.one,null,Color.white);return view;
        }
        public void SetEnemy(string id){Look=EnemyPresentation.Find(id);Player=new EnemyMotionPlayer(Look);lastPose=-1;Render();}
        public void Request(string id){Player.Request(id);Render();}
        public void Step(float delta){Player.Tick(delta);Render();}
        public void Render()
        {
            var pose=Look.poses[Player.PoseIndex];float scale=Look.displayHeight/Look.referenceHeight;
            if(lastPose!=Player.PoseIndex)
            {
                lastPose=Player.PoseIndex;Body.sprite=HeroPresentation.Sprite(Look.atlas,pose.x,pose.y,pose.w,pose.h);
                Body.rectTransform.sizeDelta=new Vector2(pose.w,pose.h)*scale;
            }
            var offset=Player.Offset;offset.x*=MovementScaleX;
            Body.rectTransform.anchoredPosition=new Vector2(offset.x,(pose.h*.5f-pose.foot)*scale+offset.y);
            shadow.rectTransform.anchoredPosition=new Vector2(offset.x,0);
            shadow.rectTransform.sizeDelta=new Vector2(Look.shadowWidth/(1+Mathf.Max(0,offset.y)/100),16);
        }
    }
}
