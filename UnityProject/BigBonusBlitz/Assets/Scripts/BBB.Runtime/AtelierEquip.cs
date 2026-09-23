using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    public static class AtelierEquip
    {
        static readonly string[] Effects={ShopEffects.StatLife,ShopEffects.StatTechnique,ShopEffects.StatLuck,ShopEffects.DefeatBonus,ShopEffects.BattleDamage,ShopEffects.TorchSpins,ShopEffects.SoulGain,ShopEffects.ExpGain,ShopEffects.AtStartPercent,ShopEffects.AtInitialSpins,
            ShopEffects.EngageDamage,ShopEffects.EngageLargeDamage,ShopEffects.EngageCounterDamage,ShopEffects.LifeDamageCut,ShopEffects.ChargeGuardRate,ShopEffects.ChargeDodgeRate,ShopEffects.JudgeBonus,ShopEffects.PotionHeal,ShopEffects.PotionDefeatSmall,ShopEffects.EngageSets,ShopEffects.TreasureRate,ShopEffects.ReplayHeal};

        public static GameObject Build(Transform stage,SlotMachine m,AudioManager audio,Action onChanged,Action onClose,Action onStats=null,Action onCurses=null)
        {
            var body=AtelierUi.Screen(stage,"EquipCard",Color.clear,null,out var overlay);
            body.sizeDelta=new Vector2(1170,540);
            var bg=UiSkin.Img(body,"Citadel",Vector2.zero,body.sizeDelta,AzureEquipSkin.Art("citadel"),Color.white);
            bg.raycastTarget=false;bg.gameObject.AddComponent<AzureMapBleed>().Apply();
            AzureMapSkin.Button(body,"Back",-478,236,178,"←  冒険者",onClose);
            AzureEquipSkin.Text(body,"Motto",0,237,500,30,"強さは、誰かを守るために",16,AzureMapSkin.Gold);
            var souls=AzureShopSkin.Wallet(body,"SoulWallet",407,214,AtelierUi.Icon("soul"),"所持ソウル");
            AzureMapSkin.Button(body,"Close",550,236,44,"×",onClose);
            AzureEquipSkin.Panel(body,"Navigation",-542,-23,68,434);
            var portrait=UiSkin.Rect(body,"PortraitClip",new Vector2(-305,-21),new Vector2(384,432));
            portrait.gameObject.AddComponent<RectMask2D>();
            // Native aspect ratio and one rigid image per pose: no mesh warping of eyes, arms or sword.
            var hero=AtelierUi.Art(portrait,"Salia",15,-100,480,640,AzureEquipSkin.Portrait(0));
            AzureEquipSkin.Text(body,"Heading",-320,188,348,44,"騎士の装備",27,AzureMapSkin.Paper,TextAnchor.MiddleLeft);
            AtelierUi.Text(body,"Eyebrow",-320,154,348,18,"CHARACTER / LOADOUT",10,AzureMapSkin.Gold);
            AzureEquipSkin.Text(body,"HeroName",-427,110,128,46,"Salia.",30,AzureMapSkin.Paper,TextAnchor.MiddleLeft);
            AzureEquipSkin.Text(body,"HeroKana",-434,73,114,22,"サリア",13,AzureMapSkin.Paper,TextAnchor.MiddleLeft);
            var portraitIndex=0;
            var poseName=AzureEquipSkin.Text(body,"PoseName",-303,-168,206,30,"",14,AzureMapSkin.Paper);
            AzureEquipSkin.Panel(body,"PoseNameBand",-303,-168,220,36).SetSiblingIndex(poseName.transform.GetSiblingIndex());
            void Pose(int delta)
            {
                portraitIndex=(portraitIndex+delta+AzureEquipSkin.PoseNames.Length)%AzureEquipSkin.PoseNames.Length;
                hero.sprite=AzureEquipSkin.Portrait(portraitIndex);
                // Raised hands and shoulder-held sword need their own framing, not a shared crop.
                hero.rectTransform.sizeDelta=portraitIndex==4?new Vector2(430,573.3333f):new Vector2(480,640);
                hero.rectTransform.anchoredPosition=portraitIndex==4?new Vector2(55,-132):
                    portraitIndex==2?new Vector2(60,-100):new Vector2(15,-100);
                poseName.text=$"{AzureEquipSkin.PoseNames[portraitIndex]}   {portraitIndex+1} / {AzureEquipSkin.PoseNames.Length}";
            }
            AzureMapSkin.Button(body,"PosePrevious",-455,-168,44,"‹",()=>{Pose(-1);audio?.UiPop();});
            AzureMapSkin.Button(body,"PoseNext",-151,-168,44,"›",()=>{Pose(1);audio?.UiPop();});
            Pose(0);
            AzureEquipSkin.Panel(body,"HeroStatsBand",-303,-222,368,48);
            var stats=AtelierUi.Text(body,"HeroStats",-303,-222,344,44,"",15,AzureMapSkin.Paper,true);
            AzureEquipSkin.Panel(body,"InventoryPanel",70,-22,308,434);
            var bag=UiSkin.Rect(body,"Inventory",new Vector2(70,0),new Vector2(308,480));
            var banner=UiSkin.Img(body,"EquipmentBanner",new Vector2(408,-24),new Vector2(310,434),AzureEquipSkin.Art("banner"),Color.white);banner.raycastTarget=false;
            var detail=UiSkin.Rect(body,"Detail",new Vector2(408,-24),new Vector2(300,432));
            AzureEquipSkin.Panel(body,"StatusBand",224,-258,680,20);
            var footer=AtelierUi.Text(body,"Status",224,-258,660,18,"品を選ぶと、着け替え後の効果を比較できます。",12,AzureMapSkin.Paper,false,TextAnchor.MiddleCenter);
            int page=0;bool wornOnly=false;EquipItem selected=null;int bulkRarity=0;
            GameObject statsOverlay=null;
            void Refresh()
            {
                AtelierUi.Clear(bag);AtelierUi.Clear(detail);
                souls.text=m.Wallet.Souls.ToString("N0");
                stats.text=$"LV. {m.PlayerLevel}    HP {m.Hp:N0} / {m.HpMax:N0}\nライフ {m.LifeStat}   テクニック {m.TechniqueStat}   ラック {m.LuckStat}";
                var items=new List<EquipItem>();
                foreach(var slot in EquipSlot.All){var it=m.Equip.WornOf(slot);if(it!=null)items.Add(it);}
                if(!wornOnly)items.AddRange(m.Equip.Bag);
                if(selected==null||!items.Contains(selected))selected=items.Count>0?items[0]:null;
                banner.sprite=AzureEquipSkin.Art(selected==null?"banner":"detail");
                page=Mathf.Clamp(page,0,Mathf.Max(0,(items.Count-1)/4));
                AzureEquipSkin.Text(bag,"BagTitle",0,183,280,28,$"装備 {m.Equip.Worn.Count}/8   鞄 {m.Equip.BagCount}/{m.Config.equipment?.bagSize??12}",17,AzureMapSkin.Paper,TextAnchor.MiddleLeft);
                AzureMapSkin.Button(bag,"Filter",0,140,284,wornOnly?"装備中のみ  ▾":"すべて  ▾",()=>{wornOnly=!wornOnly;page=0;Refresh();},false,44,fontSize:16);
                var rarities=m.Config.equipment?.rarities;
                var rr=rarities!=null&&bulkRarity<rarities.Count?rarities[bulkRarity]:new EquipRarity();
                var sellable=EquipDirector.SellableBelow(m.Equip,bulkRarity);
                int bulkSum=0;foreach(var it in sellable)bulkSum+=EquipDirector.SellValue(m.Config.equipment,it);
                var bulk=AtelierUi.Button(bag,"BulkSell",-28,90,228,
                    sellable.Count>0?$"{rr.name}以下を売る {sellable.Count}個\n+{bulkSum:N0} ソウル":$"{rr.name}以下を売る  なし",
                    ()=>AtelierUi.Confirm(body,$"{rr.name}以下の装備 {sellable.Count} 個を売却します。\n+{bulkSum:N0} ソウル。装備中と工房の品は売りません。",()=>{
                        int got=m.SellEquipBelow(bulkRarity,out int n);if(n==0)return;
                        if(selected!=null&&!m.Equip.Bag.Contains(selected)&&!m.Equip.IsWorn(selected))selected=null;
                        SaveData.Save(m,audio);audio?.Motion(MotionCue.Sell);onChanged?.Invoke();
                        footer.text=$"{n} 個 売却しました  +{got:N0} ソウル";Refresh();
                    }),UiSkin.Hex("#302333"),AzureMapSkin.Paper,44,AzureEquipSkin.Line,12);
                bulk.interactable=sellable.Count>0;
                AzureMapSkin.Button(bag,"BulkNext",120,90,44,"›",()=>{bulkRarity=(bulkRarity+1)%Math.Max(1,rarities?.Count??1);audio?.UiPop();Refresh();});
                for(int i=page*4;i<Math.Min(items.Count,page*4+4);i++)
                {
                    var it=items[i];float y=32-(i-page*4)*60;
                    bool active=it==selected;
                    var b=AtelierUi.Button(bag,"Item"+i,0,y,284,"",()=>{selected=it;audio?.UiPop();Refresh();},
                        active?UiSkin.Hex("#2b4260"):UiSkin.Hex("#142238"),AzureMapSkin.Paper,54,active?AzureMapSkin.Gold:AzureEquipSkin.Line);
                    AtelierUi.Art(b.transform,"Icon",-115,0,36,36,AtelierUi.Icon(string.IsNullOrEmpty(it.icon)?"sword":it.icon));
                    AtelierUi.Text(b.transform,"Name",21,10,222,30,it.name,14,AzureMapSkin.Paper,true);
                    AtelierUi.Text(b.transform,"State",21,-18,222,18,(m.Equip.IsWorn(it)?"✓ 装備中 / "+EquipSlot.DisplayName(m.Equip.WornSlotOf(it)):EquipSlot.DisplayName(it.slot)+" / 鞄")+(it.crafted?"  工房":""),11,AzureMapSkin.Gold);
                }
                if(items.Count==0)
                {
                    var icon=AtelierUi.Art(bag,"EmptySwords",0,4,94,94,AtelierUi.Icon("sword"));icon.color=new Color(.8f,.72f,.5f,.32f);
                    AzureEquipSkin.Text(bag,"EmptyTitle",0,-76,280,38,wornOnly?"装備中の品がありません":"まだ装備がありません",18,AzureMapSkin.Paper);
                    AtelierUi.Text(bag,"Empty",0,-139,266,76,wornOnly?"「すべて」で鞄の装備を確認できます。":"冒険中に敵や宝箱から入手できます。\n帰還すると探索装備は失われます。\n工房で作った品は残ります。",14,AzureEquipSkin.Muted,false,TextAnchor.MiddleCenter);
                }
                var prev=AzureMapSkin.Button(bag,"Previous",-103,-214,78,"‹",()=>{page--;Refresh();});prev.interactable=page>0;
                AzureEquipSkin.Text(bag,"Page",0,-214,104,34,$"{page+1} / {Math.Max(1,(items.Count+3)/4)}",17,AzureMapSkin.Paper);
                var next=AzureMapSkin.Button(bag,"Next",103,-214,78,"›",()=>{page++;Refresh();});next.interactable=(page+1)*4<items.Count;
                if(selected==null)
                {
                    AzureEquipSkin.Text(detail,"EmptyHeading",0,-75,252,80,"装備を選んで\n強さを確かめよう",23,AzureMapSkin.Paper);
                    AzureEquipSkin.Text(detail,"EmptyFootnote",0,-153,232,36,"GEAR MAKES\nA STRONGER TOMORROW",10,AzureMapSkin.Gold);
                    return;
                }
                var s=selected;string worn=m.Equip.WornSlotOf(s);
                var rarity=EquipDirector.RarityOf(m.Config.equipment,s);
                AzureEquipSkin.Text(detail,"Name",0,157,204,66,s.name,20,AzureMapSkin.Paper);
                AtelierUi.Text(detail,"Meta",0,104,244,34,s.crafted?$"工房の品（恒久）/ {EquipSlot.DisplayName(worn??s.slot)} / 段 {s.level}":$"{rarity?.name} / {EquipSlot.DisplayName(worn??s.slot)} / 深さ {s.level}",13,AzureMapSkin.Gold,false,TextAnchor.MiddleCenter);
                AtelierUi.Panel(detail,"Rule",0,79,238,1,AzureEquipSkin.Line);
                AtelierUi.Text(detail,"CompareTitle",0,58,210,26,worn!=null?"外したあとの効果":"着け替え後の効果",16,AzureMapSkin.Paper,true);
                var replaced=worn!=null?s:m.Equip.WornOf(EquipDirector.TargetSlotFor(m.Equip,s)??"");
                var lines=new List<string>();var colors=new List<Color>();
                foreach(string key in Effects)
                {
                    int a=m.Equip.EffectTotal(key),b=a-(replaced?.Of(key)??0)+(worn==null?s.Of(key):0);if(a==0&&b==0)continue;
                    string unit=EquipDirector.EffectUnit(key),sign=b>a?"+":b<a?"":"±";
                    lines.Add($"{EquipDirector.EffectName(key)}\n{a}{unit} → {b}{unit} ({sign}{b-a}{unit})");
                    colors.Add(b>a?UiSkin.Hex("#90ecc2"):b<a?UiSkin.Hex("#ffb2b4"):AzureEquipSkin.Muted);
                }
                var view=UiSkin.Rect(detail,"EffectsViewport",new Vector2(0,-28),new Vector2(210,124));
                view.gameObject.AddComponent<RectMask2D>();view.gameObject.AddComponent<Image>().color=Color.clear;
                float h=Mathf.Max(124,lines.Count*46);
                var content=UiSkin.Rect(view,"Content",Vector2.zero,new Vector2(210,h));content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
                var scroll=view.gameObject.AddComponent<ScrollRect>();MotionSound.Attach(scroll);scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
                for(int i=0;i<lines.Count;i++){var t=AtelierUi.Text(content,"Effect"+i,0,-23-i*46,202,46,lines[i],14,colors[i]);t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(.5f,1);}
                if(lines.Count==0)AtelierUi.Text(view,"NoEffect",0,0,244,40,"効果の変化なし",16,AzureEquipSkin.Muted);
                if(lines.Count*46>124)AtelierUi.Text(detail,"ScrollHint",0,-103,244,18,"↕ スクロールで効果を見る",11,AzureMapSkin.Gold,false,TextAnchor.MiddleCenter);
                var equip=AzureMapSkin.Button(detail,"Equip",0,-136,254,worn!=null?"装備を外す":"この装備を身に着ける",()=>{
                    if(!m.Equip.Bag.Contains(s)&&!m.Equip.IsWorn(s))return;
                    if(worn!=null){if(!s.crafted&&m.Equip.BagCount>=(m.Config.equipment?.bagSize??12))return;EquipDirector.Unequip(m.Equip,worn);}
                    else EquipDirector.Equip(m.Equip,s);
                    SaveData.Save(m,audio);audio?.Motion(worn!=null?MotionCue.Unequip:MotionCue.Equip);onChanged?.Invoke();
                    footer.text=worn!=null?s.name+" を外しました":s.name+" を装備しました";Refresh();
                },true,44,fontSize:15);
                equip.interactable=worn==null||s.crafted||m.Equip.BagCount<(m.Config.equipment?.bagSize??12);
                var sell=AzureMapSkin.Button(detail,"Sell",0,-187,254,s.crafted?"工房の品は売れない":$"売却  +{EquipDirector.SellValue(m.Config.equipment,s):N0} ソウル",
                    ()=>AtelierUi.Confirm(body,$"{s.name} を売却します。\n装備中の場合は外れます。",()=>{
                        if(s.crafted||(!m.Equip.Bag.Contains(s)&&!m.Equip.IsWorn(s)))return;
                        int got=m.SellEquip(s);selected=null;SaveData.Save(m,audio);audio?.Motion(MotionCue.Sell);onChanged?.Invoke();footer.text=$"売却しました  +{got:N0} ソウル";Refresh();
                    }),false,44,fontSize:14);
                sell.interactable=!s.crafted;
            }
            void ShowStats()
            {
                if(onStats!=null){onStats();return;}
                if(statsOverlay!=null)return;
                statsOverlay=StatsScreen.Build(body,m,audio,()=>{onChanged?.Invoke();Refresh();},()=>{
                    statsOverlay.SetActive(false);
                    if(Application.isPlaying)UnityEngine.Object.Destroy(statsOverlay);else UnityEngine.Object.DestroyImmediate(statsOverlay);
                    statsOverlay=null;Refresh();
                });
            }
            AzureEquipSkin.Nav(body,"Stats",118,"ステータス","book",ShowStats,false);
            AzureEquipSkin.Nav(body,"Equipment",24,"装備","sword",()=>{},true);
            if(onCurses!=null)AzureEquipSkin.Nav(body,"Curses",-70,"呪い","soul",onCurses,false);
            Refresh();return overlay;
        }
    }
}
