using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 装備画面。左に着ている 3 部位、右に鞄、下に選んだ品の詳細。
    /// 潜行中に開く（ゲーム中は E キー、街のショップにもタブを置く）。
    /// 配置は docs/ui_rules.md 1 番に従い、端から順に領域を取る。
    /// </summary>
    public static class EquipScreen
    {
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio,
                                       System.Action onChanged, System.Action onClose)
        {
            const float W = 800f, H = 470f;
            var cfg = m.Config.equipment;

            var overlay = UiFactory.Panel(stage, "EquipOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.86f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());

            var card = UiSkin.Card(overlay, "EquipCard", Vector2.zero, new Vector2(W, H), 16);
            var eat = card.gameObject.AddComponent<Button>();
            eat.transition = Selectable.Transition.None;

            // 見出し行: 左から題名、右端に閉じる
            const float closeD = 30f;
            float closeCx = W * 0.5f - 14f - closeD * 0.5f;
            var title = UiFactory.Label(card, "Title", new Vector2(-W * 0.5f + 20 + 100, H * 0.5f - 28), new Vector2(200, 28),
                                        "装備", 20, TextAnchor.MiddleLeft, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            var summary = UiFactory.Label(card, "Sum", new Vector2(closeCx - closeD * 0.5f - 12 - 190, H * 0.5f - 28), new Vector2(380, 28),
                                          "", 12, TextAnchor.MiddleRight, UiSkin.TextSub);
            UiSkin.Img(card, "Line", new Vector2(0, H * 0.5f - 48), new Vector2(W - 36, 1), null, new Color(1, 1, 1, 0.09f));
            UiSkin.IconButton(card, "Close", new Vector2(closeCx, H * 0.5f - 26), closeD, "×", () => onClose?.Invoke(), UiSkin.Btn, 17);

            // 左: 着ている 3 部位 / 右: 鞄 / 下: 詳細
            const float leftW = 250f, gap = 12f;
            float leftCx = -W * 0.5f + 18 + leftW * 0.5f;
            float rightX = leftCx + leftW * 0.5f + gap;
            float rightW = W * 0.5f - 18 - rightX;
            float rightCx = rightX + rightW * 0.5f;

            var detail = UiSkin.Card(card, "Detail", new Vector2(0, -H * 0.5f + 18 + 46), new Vector2(W - 36, 92), 10, UiSkin.Panel, true, false, false);
            var detailName = UiFactory.Label(detail, "Name", new Vector2(0, 30), new Vector2(W - 60, 22), "品を選んでください", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            detailName.fontStyle = FontStyle.Bold;
            var detailBody = UiFactory.Label(detail, "Body", new Vector2(0, -4), new Vector2(W - 60, 44), "", 12, TextAnchor.UpperCenter, UiSkin.TextSub);

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

            void Refresh()
            {
                foreach (var a in rows) a();
                var sb = new System.Text.StringBuilder();
                foreach (var key in new[] { ShopEffects.DefeatBonus, ShopEffects.BattleDamage, ShopEffects.TorchSpins, ShopEffects.SoulGain })
                {
                    int v = m.Equip.EffectTotal(key);
                    if (v != 0) sb.Append($"{EquipDirector.EffectName(key)} +{v}{EquipDirector.EffectUnit(key)}   ");
                }
                summary.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "装備の効果なし";
                onChanged?.Invoke();
            }

            // ---- 着ている部位 ----
            UiFactory.Label(card, "WornHead", new Vector2(leftCx, H * 0.5f - 62), new Vector2(leftW, 18), "身に着けている", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
            for (int i = 0; i < EquipSlot.All.Length; i++)
            {
                string slot = EquipSlot.All[i];
                float y = H * 0.5f - 82 - 34 - i * 76;
                var box = UiSkin.Card(card, "Worn_" + slot, new Vector2(leftCx, y), new Vector2(leftW, 68), 10, UiSkin.PanelHi, true, false, false);
                UiSkin.Img(box, "SlotBg", new Vector2(-leftW * 0.5f + 34, 0), new Vector2(48, 48), UiSkin.Circle(48), new Color(1, 1, 1, 0.05f));
                var icon = UiSkin.Img(box, "Icon", new Vector2(-leftW * 0.5f + 34, 0), new Vector2(36, 36), UiSkin.Icon("shield", 64), Color.white);
                var nameT = UiFactory.Label(box, "Name", new Vector2(22, 14), new Vector2(leftW - 90, 20), "", 13, TextAnchor.MiddleLeft, UiSkin.Text);
                nameT.fontStyle = FontStyle.Bold;
                var effT = UiFactory.Label(box, "Eff", new Vector2(22, -8), new Vector2(leftW - 90, 18), "", 11, TextAnchor.MiddleLeft, UiSkin.TextSub);
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

            // ---- 鞄 ----
            UiFactory.Label(card, "BagHead", new Vector2(rightCx, H * 0.5f - 62), new Vector2(rightW, 18), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
            var bagHead = card.Find("BagHead").GetComponent<Text>();
            const int cols = 4, bagRows = 3;
            float cell = Mathf.Min((rightW - 8) / cols, 60f);
            var cellBtns = new List<Button>();
            var cellIcons = new List<Image>();
            var cellRings = new List<Image>();
            for (int i = 0; i < cols * bagRows; i++)
            {
                int cx = i % cols, cy = i / cols;
                var pos = new Vector2(rightCx - (cols - 1) * cell * 0.5f + cx * cell,
                                      H * 0.5f - 96 - cell * 0.5f - cy * (cell + 6));
                var slotRt = UiSkin.Rect(card, "Bag" + i, pos, new Vector2(cell - 6, cell - 6));
                var ring = UiSkin.Img(slotRt, "Ring", Vector2.zero, new Vector2(cell - 6, cell - 6), UiSkin.Rounded(8), new Color(1, 1, 1, 0.08f));
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

            // ---- 詳細の操作 ----
            var btnEquip = UiSkin.Button(detail, "Equip", new Vector2(-W * 0.5f + 100, -30), new Vector2(150, 26), "身に着ける", () =>
            {
                if (selected == null) return;
                if (m.Equip.WornOf(selected.slot) == selected) EquipDirector.Unequip(m.Equip, selected.slot);
                else EquipDirector.Equip(m.Equip, selected);
                audio?.UiPop();
                Refresh();
                ShowDetail(selected);
            }, UiSkin.Blue, 12, false, 8);
            var btnDrop = UiSkin.Button(detail, "Drop", new Vector2(W * 0.5f - 100, -30), new Vector2(150, 26), "捨てる", () =>
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
