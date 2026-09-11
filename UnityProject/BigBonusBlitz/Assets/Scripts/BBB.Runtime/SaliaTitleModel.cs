using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Painted overlapping layers animated on the GPU, without a Cubism dependency.</summary>
    public sealed class SaliaTitleModel : MonoBehaviour
    {
        [Serializable] public sealed class Layer
        {
            public string id, file;
            public int x, y, width, height, motionClass;
        }
        [Serializable] public sealed class Definition
        {
            public int width, height;
            public Layer[] parts;
        }

        [Range(0, 1.5f)] public float breath = .65f, hair = .7f, cloth = .65f;
        [Range(.5f, 2f)] public float motionRange = 1.35f;
        public bool autoBlink = true, paused;
        [Range(.25f, 2f)] public float speed = 1f;
        public int LayerCount => Mathf.Max(0, _materials.Count - 1);
        public float MotionTime => _time;
        private readonly List<Material> _materials = new List<Material>();
        private float _time, _nextBlink = 3.3f, _blinkStart = -10f;
        private uint _seed = 512;
        private static readonly int MotionTimeId = Shader.PropertyToID("_MotionTime");
        private static readonly int MotionId = Shader.PropertyToID("_Motion");
        private static readonly int BlinkId = Shader.PropertyToID("_Blink");

        public static SaliaTitleModel Create(Transform parent)
        {
            const string folder = "SaliaRig/";
            var json = Resources.Load<TextAsset>(folder + "model");
            var shader = Resources.Load<Shader>(folder + "SaliaLayer");
            var closed = Resources.Load<Texture2D>(folder + "eyes-closed");
            var source = Resources.Load<Texture2D>(folder + "source");
            if (json == null || shader == null || closed == null || source == null) return null;
            var data = JsonUtility.FromJson<Definition>(json.text);
            if (data == null || data.width <= 0 || data.height <= 0 || data.parts == null || data.parts.Length == 0)
            {
                Debug.LogError("Invalid Salia model definition.");
                return null;
            }
            var loaded = new Texture2D[data.parts.Length];
            for (int i = 0; i < data.parts.Length; i++)
            {
                var p = data.parts[i];
                if (p.width <= 0 || p.height <= 0 || p.x < 0 || p.y < 0 || p.x + p.width > data.width || p.y + p.height > data.height)
                    return null;
                loaded[i] = Resources.Load<Texture2D>(folder + p.file);
                if (loaded[i] == null) { Debug.LogError("Missing Salia layer: " + p.file); return null; }
            }
            var go = new GameObject("SaliaTitleModel", typeof(RectTransform), typeof(SaliaTitleModel));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(.77f, 0f);
            rt.anchorMax = new Vector2(.77f, 1.04f);
            rt.sizeDelta = Vector2.zero;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = (float)data.width / data.height;
            var model = go.GetComponent<SaliaTitleModel>();
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            for (int i = -1; i < data.parts.Length; i++)
            {
                var p = i < 0 ? new Layer { id = "SeamBacking", width = data.width, height = data.height, motionClass = -1 } : data.parts[i];
                var texture = i < 0 ? source : loaded[i];
                var child = new GameObject(p.id, typeof(RectTransform), typeof(SaliaLayerGraphic));
                var crt = (RectTransform)child.transform;
                crt.SetParent(rt, false); crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.sizeDelta = Vector2.zero;
                var mat = new Material(shader) { name = "Salia/" + p.id };
                mat.SetTexture("_MainTex", texture); mat.SetTexture("_ClosedTex", closed);
                mat.SetTexture("_SourceTex", source);
                mat.SetVector("_ImageSize", new Vector4(data.width, data.height, 0, 0));
                mat.SetFloat("_PartClass", p.motionClass);
                model._materials.Add(mat);
                child.GetComponent<SaliaLayerGraphic>().Initialize(texture, mat, p, data.width, data.height);
            }
            model.SetPose(0f, 0f, 0f);
            return model;
        }

        public static float BlinkEnvelope(float age)
        {
            if (age < 0 || age >= .21f) return 0;
            if (age < .055f) return Mathf.SmoothStep(0, 1, age / .055f);
            if (age < .085f) return 1;
            return 1 - Mathf.SmoothStep(0, 1, (age - .085f) / .125f);
        }

        public void Blink() { _blinkStart = _time; }

        /// <summary>Deterministic pose API for previews, Timeline adapters and rendering checks.</summary>
        public void SetPose(float time, float leftEyeClosed, float rightEyeClosed)
        {
            _time = Mathf.Max(0, time);
            var amount = new Vector4(breath, hair, cloth, motionRange);
            var eyes = new Vector4(Mathf.Clamp01(leftEyeClosed), Mathf.Clamp01(rightEyeClosed), 0, 0);
            foreach (var mat in _materials)
            {
                mat.SetFloat(MotionTimeId, _time); mat.SetVector(MotionId, amount); mat.SetVector(BlinkId, eyes);
            }
        }

        private void Update()
        {
            if (paused) return;
            // Independent of gameplay timeScale; cap resume jumps after focus loss.
            _time += Mathf.Min(Time.unscaledDeltaTime, .05f) * speed;
            if (autoBlink && _time >= _nextBlink)
            {
                _blinkStart = _time;
                unchecked { _seed = _seed * 1664525u + 1013904223u; }
                _nextBlink = _time + 3.2f + _seed / (float)uint.MaxValue * 2.4f;
            }
            SetPose(_time, BlinkEnvelope(_time - _blinkStart), BlinkEnvelope(_time - _blinkStart - .006f));
        }

        private void OnDestroy()
        {
            foreach (var mat in _materials)
                if (mat != null) { if (Application.isPlaying) Destroy(mat); else DestroyImmediate(mat); }
            _materials.Clear();
        }
    }
}
