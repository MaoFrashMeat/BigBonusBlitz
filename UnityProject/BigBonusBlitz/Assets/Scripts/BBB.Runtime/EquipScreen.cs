using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 装備画面。左に着ている 3 部位、右に鞄、下に選んだ品の詳細と操作。
    /// 冒険中いつでも開ける（右下の装備ボタン、キーは E。街のショップにもタブを置く）。
    /// 板は細い縁の紺（panel_navy_sm）。題は左上の紺のタブに乗せ、
    /// 中身は縁（左右 22px・上下 20px）を避けて置く。配置は docs/ui_rules.md 1 番に従う。
    /// </summary>
    public static class EquipScreen
    {
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio,
                                       System.Action onChanged, System.Action onClose)
        {
            const float W = 820f, H = 470f;
            var cfg = m.Config.equipment;

            var overlay = UiFactory.Panel(stage, "EquipOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.86f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());

            var card = UiSkin.Card(overlay, "EquipCard", Vector2.zero, new Vector2(W, H), 16, frameOverride: "panel_navy_sm");
            var eat = card.gameObject.AddComponent<Button>();      // 中を押しても閉じない
            eat.transition = Selectable.Transition.None;

            // 題は左上のタブ、閉じるは右上。どちらも板の上辺にまたがる（飾りに隠れない）
            const float tabW = 150f, tabH = 40f, edge = 22f;
            var tab = UiSkin.Img(card, "Tab", new Vector2(-W * 0.5f + edge + tabW * 0.5f, H * 0.5f - 4), new Vector2(tabW, tabH), UiSkin.Frame("pill_navy_sm"), Color.white);
            var title = UiFactory.Label(tab.transform, "Title", new Vector2(0, 1), new Vector2(tabW, tabH), "装備", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - edge, H * 0.5f - 4), 30, "×", () => onClose?.Invoke(), UiSkin.Btn, 16);

            // 左: 着ている 3 部位 / 右: 鞄 / 下: 詳細と操作
            const float leftW = 270f, gap = 16f;
            float innerL = -W * 0.5f + edge + 12f, innerR = W * 0.5f - edge - 12f;
            float leftCx = innerL + leftW * 0.5f;
            float rightX = innerL + leftW + gap;
            float rightW = innerR - rightX;
            float rightCx = rightX + rightW * 0.5f;
            float top = H * 0.5f - 52f;                       // タブの下

            // ---- 詳細（下の窓）。文は左、操作は右 ----
            const float detailH = 116f;
            float detailW = innerR - innerL;
            float detailCy = -H * 0.5f + 20f + detailH * 0.5f;
            var detail = UiSkin.Inset(card, "Detail", new Vector2(0, detailCy), new Vector2(detailW, detailH), 8, null, "panel_navy_sm");
            const float btnW = 132f, btnH = 30f;
            float textW = detailW - 24f - (btnW * 2 + 10f) - 16f;
            float textCx = -detailW * 0.5f + 12f + textW * 0.5f;
            var detailName = UiFactory.Label(detail, "Name", new Vector2(textCx, 30), new Vector2(textW, 22), "品を選んでください", 15, TextAnchor.MiddleLeft, UiSkin.Text);
            detailName.fontStyle = FontStyle.Bold;
            var detailBody = UiFactory.Label(detail, "Body", new Vector2(textCx, -8), new Vector2(textW, 52), "", 12, TextAnchor.UpperLeft, UiSkin.TextSub);
            detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;

            EquipItem selected = null;
            var rows = new List<System.Action>();

            void ShowDetail(EquipItem it)
            {
                selected = it;
                if (it == null)
                {
                    detailName.text = "品を選んでください";
                    detailName.color = UiSkin.TextSub;
                    detailBody.text = "";
                    return;
                }
                var r = EquipDirector.RarityOf(cfg, it);
                detailName.text = $"{it.name}   [{r.name}]   深さ {it.level}";
                detailName.color = UiSkin.Hex(r.color);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < it.effectKeys.Count; i++)
                    sb.Append($"{EquipDirector.EffectName(it.effectKeys[i])} +{it.effectValues[i]}{EquipDirector.EffectUnit(it.effectKeys[i])}    ");
                detailBody.text = sb.ToString().TrimEnd();
            }

            // 装備中の合計。左右の列と詳細の間に 1 行
            var summary = UiFactory.Label(card, "Sum", new Vector2(0, detailCy + detailH * 0.5f + 14f), new Vector2(detailW, 18), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);

            void Refresh()
            {
                foreach (var a in rows) a();
                var sb = new System.Text.StringBuilder();
                foreach (var key in new[] { ShopEffects.DefeatBonus, ShopEffects.BattleDamage, ShopEffects.TorchSpins, ShopEffects.SoulGain })
                {
                    int v = m.Equip.EffectTotal(key);
                    if (v != 0) sb.Append($"{EquipDirector.EffectName(key)} +{v}{EquipDirector.EffectUnit(key)}   ");
                }
                summary.text = sb.Length > 0 ? "合計   " + sb.ToString().TrimEnd() : "装備の効果なし";
                onChanged?.Invoke();
            }

            // ---- 着ている部位（紺のピル 3 段）----
            UiFactory.Label(card, "WornHead", new Vector2(leftCx, top), new Vector2(leftW, 18), "身に着けている", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
            const float rowH = 56f, rowGap = 8f;
            for (int i = 0; i < EquipSlot.All.Length; i++)
            {
                string slot = EquipSlot.All[i];
                float y = top - 14f - rowH * 0.5f - i * (rowH + rowGap);
                var box = UiSkin.Rect(card, "Worn_" + slot, new Vector2(leftCx, y), new Vector2(leftW, rowH));
                UiSkin.Img(box, "Body", Vector2.zero, new Vector2(leftW, rowH), UiSkin.Frame("pill_navy_sm"), Color.white, true);
                UiSkin.Img(box, "SlotBg", new Vector2(-leftW * 0.5f + 36, 0), new Vector2(40, 40), UiSkin.Circle(48), new Color(1, 1, 1, 0.06f));
                var icon = UiSkin.Img(box, "Icon", new Vector2(-leftW * 0.5f + 36, 0), new Vector2(30, 30), UiSkin.Icon("shield", 64), Color.white);
                var nameT = UiFactory.Label(box, "Name", new Vector2(26, 11), new Vector2(leftW - 100, 20), "", 13, TextAnchor.MiddleLeft, UiSkin.Text);
                nameT.fontStyle = FontStyle.Bold;
                var effT = UiFactory.Label(box, "Eff", new Vector2(26, -10), new Vector2(leftW - 100, 18), "", 11, TextAnchor.MiddleLeft, UiSkin.TextSub);
                var btn = box.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                string sl = slot;
                btn.onClick.AddListener(() =>
                {
                    var w = m.Equip.WornOf(sl);
                    if (w != null) { ShowDetail(w); audio?.UiPop(); }
                });

                rows.Add(() =>
                {
                    var it = m.Equip.WornOf(sl);
                    if (it == null)
                    {
                        nameT.text = EquipSlot.DisplayName(sl);
                        nameT.color = UiSkin.TextDim;
                        effT.text = "空き";
                        icon.color = new Color(1, 1, 1, 0.25f);
                        return;
                    }
                    var r = EquipDirector.RarityOf(cfg, it);
                    nameT.text = it.name;
                    nameT.color = UiSkin.Hex(r.color);
                    icon.sprite = UiSkin.Icon(string.IsNullOrEmpty(it.icon) ? "shield" : it.icon, 64);
                    icon.color = Color.white;
                    var sb = new System.Text.StringBuilder();
                    for (int k = 0; k < it.effectKeys.Count; k++)
                        sb.Append($"{EquipDirector.EffectName(it.effectKeys[k])}+{it.effectValues[k]} ");
                    effT.text = sb.ToString().TrimEnd();
                });
            }

            // ---- 鞄（4 列 × 3 段）----
            var bagHead = UiFactory.Label(card, "BagHead", new Vector2(rightCx, top), new Vector2(rightW, 18), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
            const int cols = 4, bagRows = 3;
            float cell = Mathf.Min((rightW - 8) / cols, 62f);
            var cellBtns = new List<Button>();
            var cellIcons = new List<Image>();
            var cellRings = new List<Image>();
            for (int i = 0; i < cols * bagRows; i++)
            {
                int cx = i % cols, cy = i / cols;
                var pos = new Vector2(rightCx - (cols - 1) * cell * 0.5f + cx * cell,
                                      top - 14f - cell * 0.5f - cy * (cell + 4f));
                var slotRt = UiSkin.Rect(card, "Bag" + i, pos, new Vector2(cell - 6, cell - 6));
                var ring = UiSkin.Img(slotRt, "Ring", Vector2.zero, new Vector2(cell - 6, cell - 6), UiSkin.Rounded(8), new Color(1, 1, 1, 0.08f), true);
                UiSkin.Img(slotRt, "Bg", Vector2.zero, new Vector2(cell - 10, cell - 10), UiSkin.Rounded(6), UiSkin.InsetColor);
                var ic = UiSkin.Img(slotRt, "Icon", Vector2.zero, new Vector2(cell - 22, cell - 22), UiSkin.Icon("sword", 64), Color.white);
                var b = ring.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                int idx = i;
                b.onClick.AddListener(() =>
                {
                    if (idx < m.Equip.Bag.Count) { ShowDetail(m.Equip.Bag[idx]); audio?.UiPop(); }
                });
                cellBtns.Add(b); cellIcons.Add(ic); cellRings.Add(ring);
            }

            rows.Add(() =>
            {
                int cap = Mathf.Max(1, cfg?.bagSize ?? 12);
                bagHead.text = $"鞄   {m.Equip.Bag.Count} / {cap}";
                for (int i = 0; i < cellIcons.Count; i++)
                {
                    bool has = i < m.Equip.Bag.Count;
                    cellIcons[i].gameObject.SetActive(has);
                    if (!has) { cellRings[i].color = new Color(1, 1, 1, 0.08f); continue; }
                    var it = m.Equip.Bag[i];
                    var r = EquipDirector.RarityOf(cfg, it);
                    cellIcons[i].sprite = UiSkin.Icon(string.IsNullOrEmpty(it.icon) ? "sword" : it.icon, 64);
                    cellRings[i].color = UiSkin.Hex(r.color) * new Color(1, 1, 1, 0.75f);
                }
            });

            // ---- 詳細の操作（窓の右側に 2 つ並べる）----
            float btnDropCx = detailW * 0.5f - 12f - btnW * 0.5f;
            float btnEquipCx = btnDropCx - btnW - 10f;
            var btnEquip = UiSkin.Button(detail, "Equip", new Vector2(btnEquipCx, 0), new Vector2(btnW, btnH), "身に着ける", () =>
            {
                if (selected == null) return;
                if (m.Equip.WornOf(selected.slot) == selected) EquipDirector.Unequip(m.Equip, selected.slot);
                else EquipDirector.Equip(m.Equip, selected);
                audio?.UiPop();
                Refresh();
                ShowDetail(selected);
            }, UiSkin.Blue, 12, false, 8);
            var btnDrop = UiSkin.Button(detail, "Drop", new Vector2(btnDropCx, 0), new Vector2(btnW, btnH), "捨てる", () =>
            {
                if (selected == null) return;
                if (m.Equip.WornOf(selected.slot) == selected) EquipDirector.Unequip(m.Equip, selected.slot);
                EquipDirector.Drop(m.Equip, selected);
                audio?.UiPop();
                ShowDetail(null);
                Refresh();
            }, new Color(0.45f, 0.18f, 0.22f), 12, false, 8);

            rows.Add(() =>
            {
                bool has = selected != null;
                btnEquip.interactable = has;
                btnDrop.interactable = has;
                if (has)
                    UiSkin.SetButtonText(btnEquip, m.Equip.WornOf(selected.slot) == selected ? "外す" : "身に着ける");
            });

            ShowDetail(null);
            Refresh();
            return overlay.gameObject;
        }
    }
}
