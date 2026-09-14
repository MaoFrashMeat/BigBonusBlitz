using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    public static class AtelierEquip
    {
        static readonly string[] Effects={ShopEffects.StatLife,ShopEffects.StatTechnique,ShopEffects.StatLuck,ShopEffects.DefeatBonus,ShopEffects.BattleDamage,ShopEffects.TorchSpins,ShopEffects.SoulGain,ShopEffects.ExpGain,ShopEffects.AtStartPercent,ShopEffects.AtInitialSpins};
        public static GameObject Build(Transform stage,SlotMachine m,AudioManager audio,Action onChanged,Action onClose,Action onStats=null,Action onCurses=null)
        {
            var body=AtelierUi.Screen(stage,"EquipCard",UiSkin.Hex("#e1e7e3"),onClose,out var overlay);
            AtelierUi.Header(body,"CHARACTER / LOADOUT","騎士の装備");
            var portrait=UiSkin.Rect(body,"PortraitClip",new Vector2(-294,-30),new Vector2(372,300));
            portrait.gameObject.AddComponent<RectMask2D>();
            AtelierUi.Art(portrait,"Salia",0,-38,680,383,AtelierUi.Sprite("SaliaRig/source"));
            AtelierUi.Text(body,"HeroName",-341,145,234,40,"Salia.",34,AtelierUi.Ink,true);
            var stats=AtelierUi.Text(body,"HeroStats",-291,-208,334,44,"",16,AtelierUi.Ink,true);
            var bag=UiSkin.Rect(body,"Inventory",new Vector2(27,-12),new Vector2(244,362));
            var detail=AtelierUi.Panel(body,"Detail",310,-23,292,396,AtelierUi.Ink);
            var footer=AtelierUi.Text(body,"Status",90,-247,690,24,"品を選ぶと、着け替え後の効果を比較できます。",13,AtelierUi.Muted);
            int page=0;bool wornOnly=false;EquipItem selected=null;
            void Refresh()
            {
                AtelierUi.Clear(bag);AtelierUi.Clear(detail);
                stats.text=$"LEVEL {m.PlayerLevel}    HP {m.Hp:N0} / {m.HpMax:N0}\nライフ {m.LifeStat}   テクニック {m.TechniqueStat}   ラック {m.LuckStat}";
                var items=new List<EquipItem>();foreach(var slot in EquipSlot.All){var it=m.Equip.WornOf(slot);if(it!=null)items.Add(it);}if(!wornOnly)items.AddRange(m.Equip.Bag);
                if(selected==null||!items.Contains(selected))selected=items.Count>0?items[0]:null;
                page=Mathf.Clamp(page,0,Mathf.Max(0,(items.Count-1)/4));
                AtelierUi.Text(bag,"BagTitle",0,175,244,24,$"装備 {m.Equip.Worn.Count}/8  鞄 {m.Equip.Bag.Count}/{m.Config.equipment?.bagSize ?? 12}",14,AtelierUi.Ink,true);
                AtelierUi.Button(bag,"Filter",0,135,244,wornOnly?"装備中のみ  /  すべてを見る":"すべて  /  装備中のみ見る",()=>{wornOnly=!wornOnly;page=0;Refresh();},UiSkin.Hex("#cbd8d4"),AtelierUi.Ink);
                for(int i=page*4;i<Math.Min(items.Count,page*4+4);i++)
                {
                    var it=items[i];float y=76-(i-page*4)*65;
                    var b=AtelierUi.Button(bag,"Item"+i,0,y,244,"",()=>{selected=it;audio?.UiPop();Refresh();},it==selected?UiSkin.Hex("#b8d1cb"):UiSkin.Hex("#f0f3ed"),AtelierUi.Ink,58);
                    AtelierUi.Art(b.transform,"Icon",-91,0,42,42,AtelierUi.Icon(string.IsNullOrEmpty(it.icon)?"sword":it.icon));
                    AtelierUi.Text(b.transform,"Name",29,11,174,30,it.name,14,AtelierUi.Ink,true);
                    AtelierUi.Text(b.transform,"State",29,-18,174,18,m.Equip.IsWorn(it)?"✓ 装備中 / "+EquipSlot.DisplayName(m.Equip.WornSlotOf(it)):EquipSlot.DisplayName(it.slot)+" / 鞄",11,AtelierUi.Muted);
                }
                if(items.Count==0)AtelierUi.Text(bag,"Empty",0,5,224,140,"まだ装備がありません。\n\n冒険中に敵や宝箱から入手できます。帰還すると探索装備は失われます。",17,AtelierUi.Muted);
                var prev=AtelierUi.Button(bag,"Previous",-83,-183,78,"前",()=>{page--;Refresh();},UiSkin.Hex("#cbd8d4"),AtelierUi.Ink);prev.interactable=page>0;
                AtelierUi.Text(bag,"Page",0,-183,72,30,$"{page+1}/{Math.Max(1,(items.Count+3)/4)}",13,AtelierUi.Muted,false,TextAnchor.MiddleCenter);
                var next=AtelierUi.Button(bag,"Next",83,-183,78,"次",()=>{page++;Refresh();},UiSkin.Hex("#cbd8d4"),AtelierUi.Ink);next.interactable=(page+1)*4<items.Count;
                if(selected==null){AtelierUi.Text(detail,"Empty",0,0,244,100,"装備を選んで\n強さを確かめよう。",23,AtelierUi.Light,true);return;}
                var s=selected;string worn=m.Equip.WornSlotOf(s);var rarity=EquipDirector.RarityOf(m.Config.equipment,s);
                AtelierUi.Text(detail,"Name",0,157,244,58,s.name,22,AtelierUi.Light,true);
                AtelierUi.Text(detail,"Meta",0,113,244,24,$"{rarity?.name} / {EquipSlot.DisplayName(worn??s.slot)} / 深さ{s.level}",12,AtelierUi.Gold);
                AtelierUi.Text(detail,"CompareTitle",0,76,244,24,worn!=null?"外したあとの効果":"着け替え後の効果",16,AtelierUi.Light,true);
                var replaced=worn!=null?s:m.Equip.WornOf(EquipDirector.TargetSlotFor(m.Equip,s)??"");
                var lines=new List<string>();var colors=new List<Color>();
                foreach(string key in Effects){int a=m.Equip.EffectTotal(key),b=a-(replaced?.Of(key)??0)+(worn==null?s.Of(key):0);if(a==0&&b==0)continue;string unit=EquipDirector.EffectUnit(key),sign=b>a?"+":b<a?"":"±";lines.Add($"{EquipDirector.EffectName(key)}  {a}{unit} → {b}{unit} ({sign}{b-a}{unit})");colors.Add(b>a?AtelierUi.Mint:b<a?UiSkin.Hex("#ffb2b4"):AtelierUi.Sub);}
                // Scroll instead of truncating equipment with many affixes.
                var view=UiSkin.Rect(detail,"EffectsViewport",new Vector2(0,-20),new Vector2(244,150));view.gameObject.AddComponent<RectMask2D>();view.gameObject.AddComponent<Image>().color=Color.clear;
                float h=Mathf.Max(150,lines.Count*44);var content=UiSkin.Rect(view,"Content",Vector2.zero,new Vector2(244,h));content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
                var scroll=view.gameObject.AddComponent<ScrollRect>();MotionSound.Attach(scroll);scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
                for(int i=0;i<lines.Count;i++){var t=AtelierUi.Text(content,"Effect"+i,0,-22-i*44,244,44,lines[i],14,colors[i]);t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(.5f,1);}
                if(lines.Count==0)AtelierUi.Text(view,"NoEffect",0,0,244,40,"効果の変化なし",16,AtelierUi.Sub);
                var equip=AtelierUi.Button(detail,"Equip",0,-127,244,worn!=null?"装備を外す":"この装備を身に着ける",()=>{if(worn!=null)EquipDirector.Unequip(m.Equip,worn);else EquipDirector.Equip(m.Equip,s);SaveData.Save(m,audio);audio?.Motion(worn!=null?MotionCue.Unequip:MotionCue.Equip);onChanged?.Invoke();Refresh();},AtelierUi.Mint,AtelierUi.Ink);
                equip.interactable=worn==null||m.Equip.Bag.Count<(m.Config.equipment?.bagSize??12);
                AtelierUi.Button(detail,"Sell",0,-177,244,$"売却  +{EquipDirector.SellValue(m.Config.equipment,s):N0} ソウル",()=>AtelierUi.Confirm(body,$"{s.name} を売却します。\n装備中の場合は外れます。",()=>{int got=EquipDirector.Sell(m.Config.equipment,m.Equip,m.Wallet,s);selected=null;SaveData.Save(m,audio);audio?.Motion(MotionCue.Sell);onChanged?.Invoke();footer.text=$"売却しました  +{got:N0} ソウル";Refresh();}),UiSkin.Hex("#384453"),AtelierUi.Light);
            }
            if(onStats!=null)AtelierUi.Button(body,"Stats",198,231,112,"ステータス",onStats,UiSkin.Hex("#cad8d2"),AtelierUi.Ink);
            if(onCurses!=null)AtelierUi.Button(body,"Curses",328,231,112,"呪い",onCurses,UiSkin.Hex("#cad8d2"),AtelierUi.Ink);
            Refresh();return overlay;
        }
    }
}
