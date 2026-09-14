using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBB.Runtime
{
    public enum MotionCue { Hover, Click, Open, Close, Page, Toggle, Slider, Purchase, Equip, Unequip, Sell, Recover, Denied, Travel, Reveal, Victory, Guard, Impact, Cloth, Sword, Dialogue }

    /// <summary>Input gestures only: rebuilding UI and programmatic focus must remain silent.</summary>
    public sealed class MotionSound : MonoBehaviour, IPointerEnterHandler, IMoveHandler, IScrollHandler, IBeginDragHandler
    {
        public static void Attach(Component target)
        {
            if (target.GetComponent<MotionSound>() == null) target.gameObject.AddComponent<MotionSound>();
        }
        public static MotionCue ButtonCue(string name)
        {
            if (name == "Close" || name == "Cancel" || name == "Back" || name == "Refuse") return MotionCue.Close;
            if (name == "Prev" || name == "Next" || name.StartsWith("Tab") || name.StartsWith("Category")) return MotionCue.Page;
            if (name.StartsWith("Node_")) return MotionCue.Reveal;
            if (name == "Motion" || name == "Contrast" || name == "Subtitles" || name.Contains("Toggle")) return MotionCue.Toggle;
            return MotionCue.Click;
        }
        public static void Invoke(string name, Action action)
        {
            var audio = AudioManager.Create(); int before = audio.SoundSequence;
            action?.Invoke();
            // A transaction or existing gameplay callback owns its more specific sound.
            if (audio != null && before == audio.SoundSequence) audio.Motion(ButtonCue(name));
        }
        bool Available() { var s = GetComponent<Selectable>(); return s == null || (s.IsActive() && s.IsInteractable()); }
        public void OnPointerEnter(PointerEventData e) { if (Available() && e.delta.sqrMagnitude > .01f) AudioManager.Create().Motion(MotionCue.Hover); }
        public void OnMove(AxisEventData e) { if (Available()) AudioManager.Create().Motion(MotionCue.Hover); }
        public void OnScroll(PointerEventData e) { if (Available() && e.scrollDelta.sqrMagnitude > 0) AudioManager.Create().Motion(MotionCue.Page); }
        public void OnBeginDrag(PointerEventData e) { if (Available()) AudioManager.Create().Motion(MotionCue.Page); }
    }
}
