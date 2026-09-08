using UnityEngine;
using UnityEngine.EventSystems;

namespace BBB.Runtime
{
    /// <summary>スライダーを離したときに音を鳴らす（SE 音量の試聴用）。</summary>
    public sealed class SliderReleaseSound : MonoBehaviour, IPointerUpHandler
    {
        public System.Action OnRelease;
        public void OnPointerUp(PointerEventData eventData) => OnRelease?.Invoke();
    }
}
