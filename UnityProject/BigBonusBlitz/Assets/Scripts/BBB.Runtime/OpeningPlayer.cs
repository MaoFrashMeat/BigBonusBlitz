using System;
using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>OP progression remains owned by the caller; only the shared presentation is hosted here.</summary>
    public sealed class OpeningPlayer : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _dim;
        private RectTransform _box;
        private DialoguePresenter _dialogue;
        private bool _tapped;
        private int _shownFrame;
        private Action _onSkip;
        private string _scene;
        public bool Skipped { get; private set; }
        public string HeroName = "サリア";
        static OpeningPlayer active;
        public static bool IsShowing => active != null && active.gameObject.activeInHierarchy;
        public static bool IsDialogueVisible => IsShowing && active._box != null && active._box.gameObject.activeInHierarchy;
        void Awake(){active=this;}
        void OnDestroy(){if(active==this)active=null;}
        public static OpeningPlayer Create(Action onSkip, string heroName)
        {
            var p = new GameObject("OpeningPlayer").AddComponent<OpeningPlayer>();
            p._onSkip=onSkip;p.HeroName=heroName??"サリア";p.Build();return p;
        }
        private void Build()
        {
            _canvas=UiFactory.CreateCanvas("OpeningCanvas");_canvas.sortingOrder=900;
            _canvas.transform.SetParent(transform,false);
            var root=(RectTransform)_canvas.transform;
            _dim=UiFactory.Panel(root,"Dim",Vector2.zero,new Vector2(4000,4000),Color.clear).GetComponent<Image>();
            var tap=_dim.gameObject.AddComponent<Button>();tap.transition=Selectable.Transition.None;tap.onClick.AddListener(()=>_tapped=true);
            var safe=SafeStage.Create(_canvas,960,540);
            _dialogue=DialoguePresenter.Create(safe.Stage,"OpeningDialogue",850,-254,true);
            _box=_dialogue.Root;_dialogue.Tapped+=()=>_tapped=true;
            AzureMapSkin.Button(safe.Stage,"Skip",387,228,154,"序章を飛ばす",()=>{if(Skipped)return;Skipped=true;_onSkip?.Invoke();},false,44,null,14);
            HideBox();
        }
        void Update(){if(_box!=null&&_box.gameObject.activeSelf&&!Skipped)_dialogue.Tick(Time.unscaledDeltaTime);}
        public void SetDim(float a){if(_dim!=null)_dim.color=new Color(0,0,0,Mathf.Clamp01(a));}
        public IEnumerator FadeDim(float to,float seconds)
        {
            float from=_dim.color.a,t=0;
            while(t<seconds){t+=Time.unscaledDeltaTime;SetDim(Mathf.Lerp(from,to,t/Mathf.Max(.01f,seconds)));yield return null;}
            SetDim(to);
        }
        public void HideBox()
        {
            if(_box!=null)_box.gameObject.SetActive(false);
            // The OP host stays alive through playable escape. Its transparent dim must not eat BET clicks.
            if(_dim!=null)_dim.raycastTarget=false;
        }
        public IEnumerator Play(OpeningScene scene)
        {
            if(scene==null)yield break;
            _scene=scene.id;SetDim(scene.dim);
            foreach(var line in scene.lines)
            {
                if(Skipped)yield break;
                if(line==null||string.IsNullOrEmpty(line.text))continue;
                Show(line);yield return WaitTap();
            }
            HideBox();
        }
        public void Show(StoryLine line)
        {
            if(line==null)return;
            _shownFrame=Time.frameCount;_tapped=false;
            _dim.raycastTarget=true;
            string expression=_scene=="escape"||_scene=="collapse"||_scene=="dark"?"tired":_scene=="inn"?"surprised":"determined";
            _dialogue.Begin(line.speaker,line.text,HeroName,expression);
        }
        private IEnumerator WaitTap()
        {
            yield return null;
            while(!Skipped)
            {
                var kb=Keyboard.current;
                bool pressed=_tapped||(kb!=null&&(kb.spaceKey.wasPressedThisFrame||kb.enterKey.wasPressedThisFrame));
                _tapped=false;
                if(pressed&&Time.frameCount>_shownFrame)
                {
                    _shownFrame=Time.frameCount;
                    if(_dialogue.Advance())yield break;
                }
                yield return null;
            }
        }
        public void Close(){if(this!=null&&gameObject!=null)Destroy(gameObject);}
    }
}
