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
            legacy.enabled=false;View.Body.color=legacy.color;View.Render();
        }
        void OnCue(string cue){if(cue=="contact")ImpactReady=true;}
        public void Hide()
        {
            if(View.Player!=null)View.Player.Cue-=OnCue;
            // An Image without a sprite draws a white rectangle; unregistered art stays invisible instead.
            View.gameObject.SetActive(false);legacy.enabled=legacy.sprite!=null;ImpactReady=false;
        }
        public void Request(string motion)
        {
            if(!Active)return;
            ImpactReady=false;View.Request(motion);
        }
        public void Step(float seconds){if(Active)View.Step(seconds);}
        void LateUpdate(){if(Active)View.Body.color=legacy.color;}
    }
}
