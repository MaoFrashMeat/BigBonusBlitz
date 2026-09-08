using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Image に対するコマ送りアニメ（CSS steps() 相当）。</summary>
    public sealed class SpriteAnimator : MonoBehaviour
    {
        private Image _image;
        private Sprite[] _frames;
        private float _frameTime;
        private bool _loop;
        private int _index;
        private float _t;
        private bool _playing;
        private System.Action _onComplete;

        public bool IsPlaying => _playing;

        private void Awake() { _image = GetComponent<Image>(); }

        public void Play(Sprite[] frames, float totalSeconds, bool loop, System.Action onComplete = null)
        {
            if (frames == null || frames.Length == 0) return;
            _frames = frames;
            _frameTime = totalSeconds / frames.Length;
            _loop = loop;
            _index = 0;
            _t = 0;
            _playing = true;
            _onComplete = onComplete;
            Apply();
        }

        /// <summary>1枚固定表示。</summary>
        public void Show(Sprite s)
        {
            _playing = false;
            if (s != null) _image.sprite = s;
        }

        private void Update()
        {
            if (!_playing) return;
            _t += Time.deltaTime;
            while (_t >= _frameTime)
            {
                _t -= _frameTime;
                _index++;
                if (_index >= _frames.Length)
                {
                    if (_loop) _index = 0;
                    else { _index = _frames.Length - 1; _playing = false; Apply(); _onComplete?.Invoke(); return; }
                }
                Apply();
            }
        }

        private void Apply()
        {
            if (_image != null && _frames[_index] != null) _image.sprite = _frames[_index];
        }
    }
}
