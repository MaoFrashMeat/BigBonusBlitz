using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// style.css の lcd-layer（奥→手前の6層）。歩行中だけ横スクロール。
    /// RawImage の uvRect をずらして無限ループさせる（テクスチャは Repeat 取り込み）。
    /// </summary>
    public sealed class ParallaxBackground : MonoBehaviour
    {
        private sealed class Layer
        {
            public RawImage image;
            public float speed;    // 1秒あたりの UV 移動量
            public float offset;
        }

        private readonly List<Layer> _layers = new List<Layer>();
        public bool IsWalking;

        /// <summary>area は背景を敷く矩形。</summary>
        public static ParallaxBackground Create(RectTransform area)
        {
            var bg = area.gameObject.AddComponent<ParallaxBackground>();
            // (path, 高さ比, 下端位置比, 1000px 流れるのにかかる秒, 横タイル倍率)  ※CSS の scrollLayerN の秒数
            var defs = new (string path, float heightRatio, float bottom, float loopSec, float tile)[]
            {
                ("Art/Backgrounds/bg_layer6_sky",                1.00f,  0.00f, 0f,   1.0f),
                ("Art/Backgrounds/layer5_mountains_transparent", 0.65f,  0.30f, 120f, 1.5f),
                ("Art/Backgrounds/bg_layer4_distant",            0.60f,  0.20f, 60f,  1.5f),
                ("Art/Backgrounds/bg_layer3_woods",              0.45f,  0.08f, 30f,  2.0f),
                ("Art/Backgrounds/bg_layer2_debris",             0.16f,  0.02f, 20f,  3.0f),
                ("Art/Backgrounds/bg_ground",                    0.30f, -0.10f, 15f,  2.5f),
            };
            float W = area.rect.width, H = area.rect.height;
            foreach (var d in defs)
            {
                var tex = ArtLoader.Texture(d.path);
                if (tex == null) continue;
                var go = new GameObject(System.IO.Path.GetFileName(d.path), typeof(RectTransform), typeof(RawImage));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(area, false);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                float h = H * d.heightRatio;
                rt.sizeDelta = new Vector2(0, h);
                rt.anchoredPosition = new Vector2(0, H * d.bottom);
                var img = go.GetComponent<RawImage>();
                img.texture = tex;
                img.raycastTarget = false;
                float aspect = (float)tex.width / tex.height;
                float tilesX = W / (h * aspect) * d.tile;
                img.uvRect = new Rect(0, 0, tilesX, 1);
                // CSS: 1000px / loopSec。ここでは 1000px ≒ 全幅相当とみなす
                bg._layers.Add(new Layer { image = img, speed = d.loopSec > 0 ? tilesX / d.loopSec : 0f });
            }
            return bg;
        }

        private void Update()
        {
            if (!IsWalking) return;
            for (int i = 0; i < _layers.Count; i++)
            {
                var l = _layers[i];
                if (l.speed <= 0f) continue;
                l.offset = (l.offset + l.speed * Time.deltaTime) % 1f;
                var r = l.image.uvRect;
                r.x = l.offset;
                l.image.uvRect = r;
            }
        }
    }
}
