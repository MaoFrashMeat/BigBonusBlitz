using System;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Displays existing battle results. Cues never apply damage, audio or rewards.</summary>
    public sealed class AdventureEnemyPresenter : MonoBehaviour
    {
        public EnemyPresentationView View {get;private set;}
        public bool Active=>View!=null&&View.gameObject.activeSelf;
        public bool ImpactReady {get;private set;}
        Image legacy;
        string heldAtlas;
        RectTransform shotTarget;
        Image projectile;
        Vector3 launch;
        float flight;
        public bool IsProjectileFlying=>projectile!=null&&projectile.gameObject.activeSelf;
        public static AdventureEnemyPresenter Attach(RectTransform host,float feetY)
        {
            var p=host.gameObject.AddComponent<AdventureEnemyPresenter>();p.legacy=host.GetComponent<Image>();
            p.View=EnemyPresentationView.Create(host,new Vector2(0,feetY));
            foreach(var img in p.View.GetComponentsInChildren<Image>())img.raycastTarget=false;
            p.Hide();return p;
        }
        public void Show(string type,float size,float heroDistance)
        {
            Hide();
            var cfg=EnemyPresentation.Config;
            if(!cfg.gameplayEnabled||cfg.gameplayTypes==null||Array.IndexOf(cfg.gameplayTypes,type)<0)return;
            var look=EnemyPresentation.Find(type);
            View.gameObject.SetActive(true);View.SetEnemy(type);View.Player.Cue+=OnCue;
            if(heldAtlas!=null&&heldAtlas!=look.atlas){HeroPresentation.ReleaseAtlas(heldAtlas);AdventureAssets.RequestUnload();}
            heldAtlas=look.atlas;
            float scale=look.gameplayHeight/look.displayHeight*size;
            View.transform.localScale=Vector3.one*scale;
            float min=0;foreach(var pose in look.poses)min=Mathf.Min(min,pose.offsetX);
            View.MovementScaleX=min<0?Mathf.Max(0,(heroDistance+cfg.gameplayContactGap)/(min*scale)):1;
            if(look.HasShot)View.MovementScaleX*=look.shot.reachScale;
            legacy.enabled=false;ApplyTint();View.Render();
        }
        public void RequestStrike(RectTransform target)
        {
            Request("strike");
            if(Active&&View.Look.HasShot)shotTarget=target;
        }

        void OnCue(string cue)
        {
            if(cue!="contact")return;
            if(shotTarget==null){ImpactReady=true;return;}
            var shot=View.Look.shot;
            View.Render();
            if(projectile==null)
            {
                projectile=UiSkin.Img(transform,"EnemyProjectile",Vector2.zero,Vector2.one,null,Color.white);
                projectile.raycastTarget=false;
            }
            projectile.sprite=HeroPresentation.Sprite(shot.atlas,0,0,shot.w,shot.h);
            projectile.rectTransform.sizeDelta=new Vector2(shot.width,shot.height);
            projectile.color=ColorUtility.TryParseHtmlString("#"+shot.color,out var tint)?tint:Color.white;
            var body=View.Body.rectTransform;
            launch=body.TransformPoint(body.rect.center+Vector2.left*body.rect.width*.28f);
            projectile.gameObject.SetActive(true);
            flight=0;
            AdvanceProjectile(0);
        }

        void AdvanceProjectile(float seconds)
        {
            if(!IsProjectileFlying)return;
            if(shotTarget==null){CancelShot();return;}
            var shot=View.Look.shot;
            var end=shotTarget.TransformPoint(shotTarget.rect.center);
            var direction=transform.InverseTransformVector(end-launch);
            flight+=seconds;
            projectile.rectTransform.position=Vector3.Lerp(launch,end,flight/Mathf.Max(.001f,shot.seconds));
            projectile.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg+shot.rotation);
            if(flight>=shot.seconds){ImpactReady=true;CancelShot();}
        }

        void CancelShot()
        {
            shotTarget=null;
            if(projectile!=null)projectile.gameObject.SetActive(false);
        }
        public void Hide()
        {
            CancelShot();
            if(View.Player!=null)View.Player.Cue-=OnCue;
            // An Image without a sprite draws a white rectangle; unregistered art stays invisible instead.
            View.gameObject.SetActive(false);legacy.enabled=legacy.sprite!=null;ImpactReady=false;
        }
        public void Request(string motion)
        {
            if(!Active)return;
            CancelShot();ImpactReady=false;View.Request(motion);
        }
        public void Step(float seconds){if(Active&&seconds>0&&!float.IsInfinity(seconds)){AdvanceProjectile(seconds);View.Step(seconds);ApplyTint();}}
        void ApplyTint(){View.Body.color=legacy.color*(View.Environment!=null?View.Environment.ActorTint:Color.white);}
        void LateUpdate(){if(Active)ApplyTint();}
    }
}
