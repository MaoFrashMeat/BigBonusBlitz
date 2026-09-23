using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBB.Runtime
{
    [Serializable] public sealed class DialoguePortraitEntry
    {
        public string speaker, atlas;
        public int cell;
    }
    [Serializable] public sealed class AdventurePresentationConfig
    {
        public float charactersPerSecond = 30, punctuationPause = .13f;
        public float readSecondsPerCharacter = .035f, minimumReadSeconds = 1.2f;
        public float typingSoundGap = .085f, typingSoundGain = .12f, tooltipDelay = .2f;
        /// <summary>セリフ枠の大きさ（1 = 元の大きさ）。枠・顔・文字をまとめて縮める。下端の位置はそのまま（本人 2026-09-24「一回り小さく、30% 減」）。</summary>
        public float dialogueScale = 0.7f;
        public DialoguePortraitEntry[] portraits = Array.Empty<DialoguePortraitEntry>();
    }
    /// <summary>Presentation-only data. Never consumes the game's random generator or modifies rewards.</summary>
    public static class AdventurePresentation
    {
        static AdventurePresentationConfig config;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static AdventurePresentationConfig Config
        {
            get
            {
                if (config != null) return config;
                var asset = Resources.Load<TextAsset>("Data/adventure_presentation");
                try { config = asset != null ? JsonUtility.FromJson<AdventurePresentationConfig>(asset.text) : null; }
                catch (Exception e) { Debug.LogWarning("Adventure presentation defaults: " + e.Message); }
                return config ??= new AdventurePresentationConfig();
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { config = null; sprites.Clear(); }

        public static Sprite Cell(string atlas, int cell)
        {
            string key = atlas + "/" + cell;
            if (sprites.TryGetValue(key, out var found) && found != null) return found;
            var t = Resources.Load<Texture2D>("Art/UI/AdventureDialogue/" + atlas);
            if (t == null || cell < 0 || cell > 3) return null;
            float w = t.width / 2f, h = t.height / 2f;
            return sprites[key] = Sprite.Create(t, new Rect((cell % 2) * w, (1 - cell / 2) * h, w, h), new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect);
        }
        public static Sprite Portrait(string speaker, string heroName, string expression)
        {
            if (string.IsNullOrEmpty(speaker) || speaker == heroName || speaker == "主人公" || speaker == "サリア")
            {
                int cell = expression == "tired" ? 2 : expression == "determined" ? 1 : expression == "surprised" ? 3 : 0;
                return Cell("salia", cell) ?? UiSkin.Icon("compass",64);
            }
            if (speaker == "声") return UiSkin.Icon("star_emblem",64);
            if (speaker == "？？") return UiSkin.Icon("feather",64);
            foreach (var entry in Config.portraits ?? Array.Empty<DialoguePortraitEntry>())
                if (entry != null && entry.speaker == speaker) return Cell(entry.atlas,entry.cell) ?? UiSkin.Icon("compass",64);
            string[] names = {"行商人","巡礼者","狩人","占い師","王の使者"};
            int i = Array.IndexOf(names, speaker);
            if (i >= 0)
            {
                // A portrait window on the existing first sprite frame; source art is unchanged.
                string key = "traveler" + i;
                if (sprites.TryGetValue(key, out var sp) && sp != null) return sp;
                var t = Resources.Load<Texture2D>("Art/Travelers/traveler_" + (char)('A' + i));
                if (t != null)
                {
                    float w = t.width / 4f, side = Mathf.Min(w, t.height * .65f);
                    return sprites[key] = Sprite.Create(t,new Rect((w-side)/2,t.height-side,side,side),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                }
            }
            return UiSkin.Icon("compass",64);
        }
        public static Sprite CurseIcon(string id)
        {
            int cell = Array.IndexOf(new[]{"c_drain","c_hard","c_cut","c_toll"},id);
            return cell >= 0 ? Cell("curses",cell) ?? UiSkin.Icon("chain",64) : UiSkin.Icon("compass",64);
        }
    }
}
