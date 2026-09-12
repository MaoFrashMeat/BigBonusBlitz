using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 装備画面（紙人形型）。左に立ち絵とその左右に 8 枠（頭 / 武器 / 手 / 足 と 体 / アクセ 1〜3）、
    /// 中央に鞄と装備の合計、右に選んだ品の詳細と「着け替えると」の差分（▲▼）。
    /// 冒険中いつでも開ける（右下の装備ボタン、キーは E）。
    /// 板は細い縁の紺（panel_navy_sm）。題は左上の紺のタブ。配置は docs/ui_rules.md 1 番に従う。
    /// </summary>
    public static class EquipScreen
    {
        // 表示する効果の順（ステータス → 冒険の効果）
        private static readonly string[] EffectOrder =
        {
            ShopEffects.StatLife, ShopEffects.StatTechnique, ShopEffects.StatLuck,
            ShopEffects.DefeatBonus, ShopEffects.BattleDamage, ShopEffects.TorchSpins,
            ShopEffects.SoulGain, ShopEffects.ExpGain, ShopEffects.AtStartPercent, ShopEffects.AtInitialSpins,
        };
        private static readonly Color ColUp = UiSkin.Hex("#7ee0a0"), ColDown = UiSkin.Hex("#ff7a7a");

        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio,
                                       System.Action onChanged, System.Action onClose)
        {
            const float W = 1020f, H = 510f, edge = 22f;
            var cfg = m.Config.equipment;

            var overlay = UiFactory.Panel(stage, "EquipOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.86f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());

            var card = UiSkin.Card(overlay, "EquipCard", Vector2.zero, new Vector2(W, H), 16, frameOverride: "panel_navy_sm");
            var eat = card.gameObject.AddComponent<Button>();      // 中を押しても閉じない
            eat.transition = Selectable.Transition.None;

            // 題は左上のタブ、閉じるは右上
            const float tabW = 150f, tabH = 40f;
            var tab = UiSkin.Img(card, "Tab", new Vector2(-W * 0.5f + edge + tabW * 0.5f, H * 0.5f - 4), new Vector2(tabW, tabH), UiSkin.Frame("pill_navy_sm"), Color.white);
            var title = UiFactory.Label(tab.transform, "Title", new Vector2(0, 1), new Vector2(tabW, tabH), "装備", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - edge, H * 0.5f - 4), 30, "×", () => onClose?.Invoke(), UiSkin.Btn, 16);

            EquipItem selected = null;
            var rows = new List<System.Action>();
            // 下で作る部品。Refresh がこれらを使うので先に宣言しておく（ラムダの中から呼ぶため）
            Text summary = null, empty = null, dName = null, dMeta = null, dEff = null, dNote = null, dHead = null;
            RectTransform body2 = null;
            Image dRing = null, dIcon = null;
            Image[] cmpBg = null; Text[] cmpName = null, cmpOld = null, cmpNew = null;
            Button btnEquip = null;

            // ===== 左: 立ち絵と 8 枠 =====
            const float artCx = -286f;
            var artSprite = PortraitSprite();
            if (artSprite != null)
            {
                // 高さ 330 → 幅 249。左右の枠（-417 / -155）の間に収まる大きさ
                float ah = 330f, aw = ah * artSprite.rect.width / artSprite.rect.height;
                UiSkin.Img(card, "Portrait", new Vector2(artCx, -20), new Vector2(aw, ah), artSprite, Color.white).preserveAspect = true;
            }
            const float slotBox = 58f, slotPitch = 92f;
            float[] slotX = { -446f, -126f };
            for (int i = 0; i < EquipSlot.All.Length; i++)
            {
                string slot = EquipSlot.All[i];
                int col = i / 4, row = i % 4;
                var pos = new Vector2(slotX[col], 170f - row * slotPitch);
                var cell = UiSkin.Rect(card, "Worn_" + slot, pos, new Vector2(slotBox, slotBox));
                UiSkin.Img(cell, "Body", Vector2.zero, new Vector2(slotBox, slotBox), UiSkin.Frame("slot_navy"), Color.white, true);
                var ring = UiSkin.Img(cell, "Ring", Vector2.zero, new Vector2(44, 44), UiSkin.Ring(44, 1), new Color(1, 1, 1, 0));
                var icon = UiSkin.Img(cell, "Icon", Vector2.zero, new Vector2(30, 30), UiSkin.Icon("shield", 64), Color.white);
                // 枠の名前（頭 / アクセ 1 …）は上に小さな札、品の名前は下に
                var tagBg = UiSkin.Img(cell, "TagBg", new Vector2(0, slotBox * 0.5f + 4), new Vector2(38, 12), UiSkin.Rounded(6), new Color(0, 0, 0, 0.6f));
                UiFactory.Label(tagBg.transform, "Tag", Vector2.zero, new Vector2(38, 12), EquipSlot.DisplayName(slot), 8, TextAnchor.MiddleCenter, UiSkin.TextSub);
                var nameT = UiFactory.Label(cell, "Name", new Vector2(0, -slotBox * 0.5f - 9), new Vector2(80, 14), "", 10, TextAnchor.MiddleCenter, UiSkin.Text);
                nameT.horizontalOverflow = HorizontalWrapMode.Wrap; nameT.verticalOverflow = VerticalWrapMode.Truncate;
                var btn = cell.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                string sl = slot;
                btn.onClick.AddListener(() =>
                {
                    var w = m.Equip.WornOf(sl);
                    if (w != null) { selected = w; audio?.UiPop(); Refresh(); }
                });
                rows.Add(() =>
                {
                    var it = m.Equip.WornOf(sl);
                    bool sel = it != null && it == selected;
                    if (it == null)
                    {
                        nameT.text = "空き"; nameT.color = UiSkin.TextDim;
                        icon.sprite = UiSkin.Icon(IconFor(EquipSlot.KindOf(sl)), 64); icon.color = new Color(1, 1, 1, 0.25f);
                        ring.color = new Color(1, 1, 1, 0);
                        return;
                    }
                    var r = EquipDirector.RarityOf(cfg, it);
                    nameT.text = it.name; nameT.color = UiSkin.Hex(r.color);
                    icon.sprite = UiSkin.Icon(string.IsNullOrEmpty(it.icon) ? IconFor(it.slot) : it.icon, 64); icon.color = Color.white;
                    ring.sprite = UiSkin.Ring(44, sel ? 2 : 1);
                    ring.color = sel ? Color.white : UiSkin.Hex(r.color) * new Color(1, 1, 1, 0.7f);
                });
            }

            // ===== 中央: 鞄と合計 =====
            const float bagCx = 40f, bagCell = 54f, bagPitch = 58f;
            const int cols = 4;
            int bagRows = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, cfg?.bagSize ?? 12) / (float)cols), 1, 4);
            var bagHead = UiFactory.Label(card, "BagHead", new Vector2(bagCx, 190), new Vector2(240, 16), "", 11, TextAnchor.MiddleCenter, UiSkin.TextSub);
            var cellIcons = new List<Image>();
            var cellRings = new List<Image>();
            for (int i = 0; i < cols * bagRows; i++)
            {
                int cx = i % cols, cy = i / cols;
                var pos = new Vector2(bagCx - (cols - 1) * bagPitch * 0.5f + cx * bagPitch, 150f - cy * bagPitch);
                var slotRt = UiSkin.Rect(card, "Bag" + i, pos, new Vector2(bagCell, bagCell));
                var body = UiSkin.Img(slotRt, "Body", Vector2.zero, new Vector2(bagCell, bagCell), UiSkin.Frame("slot_navy"), Color.white, true);
                var ring = UiSkin.Img(slotRt, "Ring", Vector2.zero, new Vector2(40, 40), UiSkin.Ring(40, 1), new Color(1, 1, 1, 0));
                var ic = UiSkin.Img(slotRt, "Icon", Vector2.zero, new Vector2(28, 28), UiSkin.Icon("sword", 64), Color.white);
                var b = body.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                int idx = i;
                b.onClick.AddListener(() =>
                {
                    if (idx < m.Equip.Bag.Count) { selected = m.Equip.Bag[idx]; audio?.UiPop(); Refresh(); }
                });
                cellIcons.Add(ic); cellRings.Add(ring);
            }
            rows.Add(() =>
            {
                int cap = Mathf.Max(1, cfg?.bagSize ?? 12);
                bagHead.text = $"鞄   {m.Equip.Bag.Count} / {cap}";
                for (int i = 0; i < cellIcons.Count; i++)
                {
                    bool has = i < m.Equip.Bag.Count;
                    cellIcons[i].gameObject.SetActive(has);
                    if (!has) { cellRings[i].color = new Color(1, 1, 1, 0); continue; }
                    var it = m.Equip.Bag[i];
                    var r = EquipDirector.RarityOf(cfg, it);
                    cellIcons[i].sprite = UiSkin.Icon(string.IsNullOrEmpty(it.icon) ? IconFor(it.slot) : it.icon, 64);
                    bool sel = it == selected;
                    cellRings[i].sprite = UiSkin.Ring(40, sel ? 2 : 1);
                    cellRings[i].color = sel ? Color.white : UiSkin.Hex(r.color) * new Color(1, 1, 1, 0.75f);
                }
            });
            // 装備の合計（鞄の下）
            summary = UiFactory.Label(card, "Sum", new Vector2(bagCx, 150f - bagRows * bagPitch - 8f), new Vector2(240, 44), "", 10, TextAnchor.UpperCenter, UiSkin.TextSub);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ===== 右: 詳細と「着け替えると」 =====
            const float dW = 300f, dH = 400f;
            float dCx = W * 0.5f - edge - 12f - dW * 0.5f;
            var detail = UiSkin.Inset(card, "Detail", new Vector2(dCx, 0), new Vector2(dW, dH), 8, null, "panel_navy_sm");
            empty = UiFactory.Label(detail, "Empty", Vector2.zero, new Vector2(dW - 40, 40), "品を選んでください", 13, TextAnchor.MiddleCenter, UiSkin.TextSub);
            body2 = UiSkin.Rect(detail, "Body", Vector2.zero, new Vector2(dW, dH));
            float top = dH * 0.5f - 22f, left = -dW * 0.5f + 18f;
            UiSkin.Img(body2, "IconBg", new Vector2(left + 24, top - 24), new Vector2(48, 48), UiSkin.Rounded(8), new Color(0, 0, 0, 0.35f));
            dRing = UiSkin.Img(body2, "IconRing", new Vector2(left + 24, top - 24), new Vector2(48, 48), UiSkin.Ring(48, 1), Color.white);
            dIcon = UiSkin.Img(body2, "Icon", new Vector2(left + 24, top - 24), new Vector2(34, 34), UiSkin.Icon("sword", 64), Color.white);
            dName = UiFactory.Label(body2, "Name", new Vector2(left + 60 + (dW - 96) * 0.5f, top - 10), new Vector2(dW - 96, 18), "", 14, TextAnchor.MiddleLeft, UiSkin.Text);
            dName.fontStyle = FontStyle.Bold;
            dName.horizontalOverflow = HorizontalWrapMode.Wrap; dName.verticalOverflow = VerticalWrapMode.Truncate;
            dMeta = UiFactory.Label(body2, "Meta", new Vector2(left + 60 + (dW - 96) * 0.5f, top - 30), new Vector2(dW - 96, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextSub);
            dEff = UiFactory.Label(body2, "Eff", new Vector2(0, top - 68), new Vector2(dW - 36, 32), "", 11, TextAnchor.UpperLeft, UiSkin.Text);
            dEff.horizontalOverflow = HorizontalWrapMode.Wrap;
            dNote = UiFactory.Label(body2, "Note", new Vector2(0, top - 98), new Vector2(dW - 36, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextSub);
            dHead = UiFactory.Label(body2, "CmpHead", new Vector2(0, top - 118), new Vector2(dW - 36, 14), "着け替えると", 11, TextAnchor.MiddleLeft, UiSkin.TextSub);
            const int cmpRows = 7; const float cmpH = 20f, cmpPitch = 22f;
            cmpBg = new Image[cmpRows]; cmpName = new Text[cmpRows]; cmpOld = new Text[cmpRows]; cmpNew = new Text[cmpRows];
            for (int i = 0; i < cmpRows; i++)
            {
                float y = top - 138f - i * cmpPitch;
                cmpBg[i] = UiSkin.Img(body2, "Cmp" + i, new Vector2(0, y), new Vector2(dW - 36, cmpH), UiSkin.Rounded(4), new Color(1, 1, 1, 0.05f));
                cmpName[i] = UiFactory.Label(cmpBg[i].transform, "N", new Vector2(-(dW - 36) * 0.5f + 8 + 60, 0), new Vector2(120, cmpH), "", 11, TextAnchor.MiddleLeft, UiSkin.Text);
                cmpOld[i] = UiFactory.Label(cmpBg[i].transform, "O", new Vector2(14, 0), new Vector2(60, cmpH), "", 11, TextAnchor.MiddleRight, UiSkin.TextSub);
                UiFactory.Label(cmpBg[i].transform, "Arrow", new Vector2(56, 0), new Vector2(20, cmpH), "→", 11, TextAnchor.MiddleCenter, UiSkin.TextDim);
                cmpNew[i] = UiFactory.Label(cmpBg[i].transform, "V", new Vector2((dW - 36) * 0.5f - 8 - 36, 0), new Vector2(72, cmpH), "", 11, TextAnchor.MiddleRight, UiSkin.Text);
            }
            const float btnW = 128f, btnH = 30f;
            btnEquip = UiSkin.Button(body2, "Equip", new Vector2(-btnW * 0.5f - 6, -dH * 0.5f + 34), new Vector2(btnW, btnH), "身に着ける", () =>
            {
                if (selected == null) return;
                var wornAt = m.Equip.WornSlotOf(selected);
                if (wornAt != null) EquipDirector.Unequip(m.Equip, wornAt);
                else EquipDirector.Equip(m.Equip, selected);
                audio?.UiPop();
                Refresh();
            }, UiSkin.Blue, 12, false, 8);
            UiSkin.Button(body2, "Drop", new Vector2(btnW * 0.5f + 6, -dH * 0.5f + 34), new Vector2(btnW, btnH), "捨てる", () =>
            {
                if (selected == null) return;
                var wornAt = m.Equip.WornSlotOf(selected);
                if (wornAt != null) EquipDirector.Unequip(m.Equip, wornAt);
                EquipDirector.Drop(m.Equip, selected);
                selected = null;
                audio?.UiPop();
                Refresh();
            }, new Color(0.45f, 0.18f, 0.22f), 12, false, 8);

            void RefreshDetail()
            {
                bool has = selected != null;
                empty.gameObject.SetActive(!has);
                body2.gameObject.SetActive(has);
                if (!has) return;
                var it = selected;
                var r = EquipDirector.RarityOf(cfg, it);
                var rc = UiSkin.Hex(r.color);
                dIcon.sprite = UiSkin.Icon(string.IsNullOrEmpty(it.icon) ? IconFor(it.slot) : it.icon, 64);
                dRing.color = rc;
                dName.text = it.name; dName.color = rc;
                string wornAt = m.Equip.WornSlotOf(it);
                dMeta.text = $"{r.name}  ·  {EquipSlot.DisplayName(wornAt ?? it.slot)}  ·  深さ {it.level}";
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < it.effectKeys.Count; i++)
                    sb.Append($"{EquipDirector.EffectName(it.effectKeys[i])} +{it.effectValues[i]}{EquipDirector.EffectUnit(it.effectKeys[i])}    ");
                dEff.text = sb.ToString().TrimEnd();

                // 着け替えたあとの合計（外すなら引くだけ、着けるなら入る枠の品と入れ替え）
                var now = Totals(m.Equip, null, null);
                Dictionary<string, int> after;
                if (wornAt != null) { after = Totals(m.Equip, it, null); dNote.text = ""; dHead.text = "外すと"; }
                else
                {
                    string target = EquipDirector.TargetSlotFor(m.Equip, it);
                    var replaced = target != null ? m.Equip.WornOf(target) : null;
                    after = Totals(m.Equip, replaced, it);
                    dNote.text = replaced != null ? $"{EquipSlot.DisplayName(target)} の {replaced.name} と入れ替え" : $"{EquipSlot.DisplayName(target)} に着ける";
                    dHead.text = "着け替えると";
                }
                int row = 0;
                foreach (var key in EffectOrder)
                {
                    now.TryGetValue(key, out int a); after.TryGetValue(key, out int b);
                    if (a == b || row >= cmpRows) continue;
                    string u = EquipDirector.EffectUnit(key);
                    cmpBg[row].gameObject.SetActive(true);
                    cmpName[row].text = EquipDirector.EffectName(key);
                    cmpOld[row].text = $"+{a}{u}";
                    cmpNew[row].text = $"+{b}{u}  {(b > a ? "▲" : "▼")}";
                    cmpNew[row].color = b > a ? ColUp : ColDown;
                    row++;
                }
                if (row == 0) { cmpBg[0].gameObject.SetActive(true); cmpName[0].text = "変わらない"; cmpOld[0].text = ""; cmpNew[0].text = ""; row = 1; }
                for (int i = row; i < cmpRows; i++) cmpBg[i].gameObject.SetActive(false);
                UiSkin.SetButtonText(btnEquip, wornAt != null ? "外す" : "身に着ける");
            }

            void Refresh()
            {
                foreach (var a in rows) a();
                var sb = new System.Text.StringBuilder("合計   ");
                int n = 0;
                foreach (var key in EffectOrder)
                {
                    int v = m.Equip.EffectTotal(key);
                    if (v == 0) continue;
                    sb.Append($"{EquipDirector.EffectName(key)} +{v}{EquipDirector.EffectUnit(key)}   ");
                    n++;
                }
                summary.text = n > 0 ? sb.ToString().TrimEnd() : "装備の効果なし";
                RefreshDetail();
                onChanged?.Invoke();
            }

            Refresh();
            return overlay.gameObject;
        }

        /// <summary>装備中の効果の合計。remove を外し、add を足した状態で数える（null なら何もしない）。</summary>
        private static Dictionary<string, int> Totals(EquipInventory inv, EquipItem remove, EquipItem add)
        {
            var d = new Dictionary<string, int>();
            void AddItem(EquipItem it)
            {
                if (it == null) return;
                for (int i = 0; i < it.effectKeys.Count && i < it.effectValues.Count; i++)
                {
                    d.TryGetValue(it.effectKeys[i], out int v);
                    d[it.effectKeys[i]] = v + it.effectValues[i];
                }
            }
            foreach (var kv in inv.Worn) if (kv.Value != remove) AddItem(kv.Value);
            AddItem(add);
            return d;
        }

        /// <summary>品の種類ごとの既定のアイコン（品に icon が無いとき）。</summary>
        private static string IconFor(string kind)
        {
            switch (EquipSlot.Normalize(kind))
            {
                case EquipSlot.Weapon: return "sword";
                case EquipSlot.Feet: return "boots";
                case EquipSlot.Hands: return "chain";
                case EquipSlot.Accessory: return "amulet";
                default: return "shield";
            }
        }

        private static Sprite _portrait;

        /// <summary>立ち絵（タイトルの元絵から、人物の部分だけ切り出す）。無ければ null。</summary>
        private static Sprite PortraitSprite()
        {
            if (_portrait != null) return _portrait;
            var tex = Resources.Load<Texture2D>("SaliaRig/source");
            if (tex == null) return null;
            // 左へ伸ばした腕と剣先を外し、顔と体を中心に（元絵 1672×941 のうち x 470〜1180）
            int x0 = Mathf.Min(470, tex.width - 1), w = Mathf.Min(710, tex.width - x0);
            _portrait = Sprite.Create(tex, new Rect(x0, 0, w, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _portrait;
        }
    }
}
