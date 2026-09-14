using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace BBB.Runtime
{
    /// <summary>Keep menu navigation within the foremost Atelier screen, including confirmation dialogs.</summary>
    public sealed class AtelierModalInput : MonoBehaviour
    {
        static readonly List<AtelierModalInput> Open=new List<AtelierModalInput>();
        int signature;
        bool soundPending, sounded;
        GameObject previous;
        string pendingName;
        GameObject lastFocused;
        public void RememberSelection(Transform cleared)
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected!=null&&selected.transform.IsChildOf(cleared))pendingName=selected.name;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetState()=>Open.Clear();
        void OnEnable(){soundPending=true;previous=EventSystem.current?.currentSelectedGameObject;Open.Remove(this);Open.Add(this);signature=0;}
        void OnDisable(){if(sounded && Application.isPlaying)FindFirstObjectByType<AudioManager>()?.Motion(MotionCue.Close);sounded=false;Open.Remove(this);if(previous!=null&&previous.activeInHierarchy)EventSystem.current?.SetSelectedGameObject(previous);}
        void LateUpdate()
        {
            if(soundPending){soundPending=false;sounded=true;AudioManager.Create().Motion(name.Contains("ChapterResult")?MotionCue.Victory:MotionCue.Open);}
            if(Open.Count==0||Open[Open.Count-1]!=this||EventSystem.current==null)return;
            var controls=new List<Selectable>();int hash=17;
            foreach(var s in GetComponentsInChildren<Selectable>())if(s.IsInteractable()&&s.IsActive()){controls.Add(s);unchecked{hash=hash*31+s.GetHashCode();}}
            if(controls.Count==0)return;
            if(hash!=signature)
            {
                signature=hash;
                foreach(var s in controls)
                {
                    Selectable Near(Vector2 direction)
                    {
                        Selectable best=null;float score=float.MaxValue;var origin=((RectTransform)s.transform).TransformPoint(((RectTransform)s.transform).rect.center);
                        foreach(var candidate in controls){if(candidate==s)continue;var rt=(RectTransform)candidate.transform;Vector2 d=rt.TransformPoint(rt.rect.center)-origin;float along=Vector2.Dot(d,direction);if(along<=1)continue;float cost=d.sqrMagnitude/along;if(cost<score){score=cost;best=candidate;}}return best;
                    }
                    s.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=Near(Vector2.up),selectOnDown=Near(Vector2.down),selectOnLeft=Near(Vector2.left),selectOnRight=Near(Vector2.right)};
                }
            }
            var current=EventSystem.current.currentSelectedGameObject;
            int index=controls.FindIndex(s=>s.gameObject==current);
            if(pendingName!=null){int restored=controls.FindIndex(s=>s.name==pendingName);pendingName=null;if(restored>=0){index=restored;EventSystem.current.SetSelectedGameObject(controls[index].gameObject);}}
            if(index<0){index=0;EventSystem.current.SetSelectedGameObject(controls[0].gameObject);}
            var kb=Keyboard.current;
            if(kb!=null&&kb.tabKey.wasPressedThisFrame){AudioManager.Create().Motion(MotionCue.Hover);int step=kb.leftShiftKey.isPressed||kb.rightShiftKey.isPressed?-1:1;index=(Mathf.Max(0,index)+step+controls.Count)%controls.Count;EventSystem.current.SetSelectedGameObject(controls[index].gameObject);}
            current=EventSystem.current.currentSelectedGameObject;
            if(current!=null&&current!=lastFocused)
            {
                lastFocused=current;
                var scroll=current.GetComponentInParent<ScrollRect>();
                if(scroll!=null&&scroll.content!=null&&scroll.viewport!=null)
                {
                    Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,(RectTransform)current.transform);var view=scroll.viewport.rect;Vector2 delta=Vector2.zero;
                    if(scroll.horizontal){if(bounds.min.x<view.xMin)delta.x=view.xMin-bounds.min.x;else if(bounds.max.x>view.xMax)delta.x=view.xMax-bounds.max.x;}
                    if(scroll.vertical){if(bounds.min.y<view.yMin)delta.y=view.yMin-bounds.min.y;else if(bounds.max.y>view.yMax)delta.y=view.yMax-bounds.max.y;}
                    scroll.content.anchoredPosition+=delta;
                }
            }
        }
    }
}
