using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace BBB.Runtime
{
    public static class AtelierShop
    {
        sealed class Offer
        {
            public string Name, Icon, Category;
            public Func<string> Description, Status;
            public Func<int> Price;
            public Func<bool> CanBuy, Purchase;
            /// <summary>工房の図面（作る）。Purchase が作成、Reason が作れない理由。</summary>
            public bool Craft; public Func<string> Reason; public int Tier;
        }
        static string Describe(ShopItem item,int level)
        {
            string Current(int lv)
            {
                var text=ShopDirector.Describe(item,lv);
                int preview=text.IndexOf("（次のレベル",StringComparison.Ordinal);
                return preview<0?text:text.Substring(0,preview).TrimEnd();
            }
            return Current(level)+(level>=item.maxLevel?"\n\nこの品は最大まで強化されています。":"\n\n強化後：\n"+Current(level+1));
        }
        public static GameObject Build(Transform stage,SlotMachine m,AudioManager audio,Action onSoulsChanged,Action onClose)
        {
            var body=AtelierUi.Screen(stage,"ShopCard",Color.clear,null,out var overlay);
            body.sizeDelta=new Vector2(AzureMapSkin.Width,AzureMapSkin.Height);
            var bg=UiSkin.Img(body,"GuildTerrace",Vector2.zero,body.sizeDelta,AzureShopSkin.Sprite("terrace"),Color.white);
            bg.raycastTarget=false;bg.gameObject.AddComponent<AzureMapBleed>().Apply();
            AzureMapSkin.Button(body,"BackAdventure",-490,235,150,"←  冒険へ",onClose);
            AzureShopSkin.Label(body,"Eyebrow",-237,254,340,16,"MERCHANTS GUILD / TOWN",9,TextAnchor.MiddleLeft,AzureMapSkin.Gold);
            var heading=AzureShopSkin.Label(body,"Heading",-237,215,340,54,"旅支度の商店",32,TextAnchor.MiddleLeft,Color.white);
            AzureMapSkin.Typography(heading,true);
            var souls=AzureShopSkin.Wallet(body,"SoulWallet",124,156,AtelierUi.Icon("soul"),"所持ソウル");
            var embers=AzureShopSkin.Wallet(body,"EmberWallet",296,156,AtelierUi.Icon("ember"),"所持エンバー");
            AzureShopSkin.Label(body,"Motto",450,232,132,46,"新たな出会いが、\nあなたの旅を強くする",11,TextAnchor.MiddleCenter,AzureMapSkin.Ink);
            AzureMapSkin.Button(body,"CloseTop",550,236,44,"×",onClose);
            const float railX=-398, railW=176, listX=-78, listW=456, detailX=318, detailW=328;
            const float panelY=-30,panelH=428;
            AzureShopSkin.Surface(body,"CategoryRail",railX,panelY,railW,panelH,true);
            AzureShopSkin.Surface(body,"ProductsSurface",listX,panelY,listW,panelH,true);
            AzureShopSkin.Surface(body,"DetailSurface",detailX,panelY,detailW,panelH,true);
            var list=UiSkin.Rect(body,"Products",new Vector2(listX,0),new Vector2(listW,panelH));
            var listHeading=AzureShopSkin.Label(list,"ListHeading",0,151,410,34,"",17);
            var viewport=AtelierUi.Panel(list,"ProductsViewport",-19,-43,386,344,Color.clear);
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var content=UiSkin.Rect(viewport,"ProductContent",Vector2.zero,new Vector2(386,344));
            content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
            var scroll=list.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.inertia=true;scroll.decelerationRate=.12f;scroll.scrollSensitivity=32;
            var track=AtelierUi.Panel(list,"ScrollTrack",202,-43,44,344,Color.clear);track.GetComponent<Image>().raycastTarget=true;
            AtelierUi.Panel(track,"Rail",0,0,3,344,UiSkin.Hex("#d4c5ab"));
            var sliding=UiSkin.Rect(track,"SlidingArea",Vector2.zero,new Vector2(44,344));
            var handle=AtelierUi.Panel(sliding,"Handle",0,0,44,44,Color.clear);handle.GetComponent<Image>().raycastTarget=true;
            handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.sizeDelta=Vector2.zero;
            var thumb=AtelierUi.Panel(handle,"Thumb",0,0,8,44,UiSkin.Hex("#b79a60"));
            thumb.anchorMin=new Vector2(.5f,0);thumb.anchorMax=new Vector2(.5f,1);thumb.sizeDelta=new Vector2(8,0);
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=thumb.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            var scrollHint=AtelierUi.Text(list,"ScrollHint",0,-227,400,18,"ドラッグ・ホイールで商品を見る",11,AtelierUi.Muted,false,TextAnchor.MiddleCenter);
            var detail=UiSkin.Rect(body,"Details",new Vector2(detailX,0),new Vector2(detailW,panelH));
            // 「次の冒険に…」の札は工房タブに場所を譲った（2026-09-21）
            var note=AtelierUi.Text(body,"Status",0,-257,900,20,"",11,AzureMapSkin.Paper,false,TextAnchor.MiddleCenter);
            var noteBand=AtelierUi.Panel(body,"StatusBand",0,-257,940,22,AzureMapSkin.Navy);
            noteBand.gameObject.SetActive(false);note.transform.SetAsLastSibling();
            var offers=new List<Offer>();
            foreach(var item in m.Config.shop?.items ?? new List<ShopItem>())
            {
                if(item==null)continue;var it=item;
                offers.Add(new Offer{Name=it.name,Icon=string.IsNullOrEmpty(it.icon)?"sword":it.icon,Category=it.kind=="gear"?"装備品":"スキル",
                    Description=()=>Describe(it,m.Wallet.LevelOf(it.id)),
                    Status=()=>$"強化 Lv {m.Wallet.LevelOf(it.id)} / {it.maxLevel}",Price=()=>ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id)),
                    CanBuy=()=>ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id))>=0&&m.Wallet.Souls>=ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id)),Purchase=()=>ShopDirector.Buy(m.Wallet,it)});
            }
            // 工房（本人 2026-09-21: 案C + 案A'）: 図面はソウルと素材で作る。作った品は恒久で、＋ボーナスは付かない
            var craft=m.Config.craft;
            if(craft!=null&&craft.enabled&&craft.recipes!=null)
            {
                string MatLine(CraftRecipe r)
                {
                    var parts=new List<string>();
                    for(int i=0;i<r.materialIds.Count;i++){int need=i<r.materialCounts.Count?r.materialCounts[i]:1;var md=craft.Material(r.materialIds[i]);int have=m.Wallet.MaterialCount(r.materialIds[i]);parts.Add($"{md?.name??r.materialIds[i]} {have}/{need}");}
                    return parts.Count==0?"素材なし":string.Join("　",parts);
                }
                var sorted=new List<CraftRecipe>(craft.recipes);sorted.Sort((a,b)=>a.tier!=b.tier?a.tier.CompareTo(b.tier):Array.IndexOf(EquipSlot.Kinds,a.slot).CompareTo(Array.IndexOf(EquipSlot.Kinds,b.slot)));
                foreach(var recipe in sorted)
                {
                    if(recipe==null)continue;var r=recipe;
                    offers.Add(new Offer{Name=r.name,Icon=string.IsNullOrEmpty(r.icon)?"sword":r.icon,Category="工房",Craft=true,Tier=r.tier,
                        Description=()=>$"{EquipSlot.DisplayName(r.slot)} / 段 {"ⅠⅡⅢⅣ"[Mathf.Clamp(r.tier,1,4)-1]}\n{r.desc}\n\n素材: {MatLine(r)}\n恒久・＋ボーナス無し",
                        Status=()=>CraftDirector.Owns(m.Equip,r.id)?"作成済み（装備画面で着け替え）":CraftDirector.Unlocked(r,m.ChaptersCleared)?"未作成":$"第{r.unlockChapter}章の踏破で解放",
                        Price=()=>CraftDirector.Owns(m.Equip,r.id)?-1:r.souls,
                        Reason=()=>CraftDirector.Missing(craft,m.Wallet,m.Equip,r,m.ChaptersCleared),
                        CanBuy=()=>CraftDirector.CanCraft(craft,m.Wallet,m.Equip,r,m.ChaptersCleared),
                        Purchase=()=>m.Craft(r,out _)!=null});
                }
            }
            var res=m.Config.adventure?.resource;
            if(m.AdventureEnabled&&res!=null&&res.enabled)
            {
                offers.Add(new Offer{Name=res.name,Icon="potion",Category="補給",Description=()=>$"1個で{res.hpName}を{m.TorchSpinsPerUnit}回復。\n冒険の備えを整えましょう。",Status=()=>$"所持 {m.Adv.torches} / {res.maxTorches} 個",Price=()=>Mathf.Max(0,res.torchCost),CanBuy=()=>m.Wallet.Embers>=Mathf.Max(0,res.torchCost)&&m.Adv.torches<res.maxTorches,Purchase=()=>{int cost=Mathf.Max(0,res.torchCost);if(m.Wallet.Embers<cost||AdventureDirector.AddTorch(m.Config.adventure,m.Adv,1,m.TorchSpinsPerUnit)<=0)return false;m.Wallet.Embers-=cost;return true;}});
                offers.Add(new Offer{Name="灯火を補充",Icon="ember",Category="補給",Description=()=>$"冒険用エンバーを{res.creditAmount:N0}補充します。\n支払いには所持品のエンバーを使用します。",Status=()=>$"冒険用 {m.Credit:N0} エンバー",Price=()=>Mathf.Max(0,res.creditCost),CanBuy=()=>m.Wallet.Embers>=Mathf.Max(0,res.creditCost),Purchase=()=>{int cost=Mathf.Max(0,res.creditCost);if(m.Wallet.Embers<cost)return false;m.Wallet.Embers-=cost;m.Credit+=Mathf.Max(0,res.creditAmount);return true;}});
            }
            string category="すべて";Offer selected=offers.Count>0?offers[0]:null;
            var tabs=new List<Button>();var tabImages=new List<Image>();var tabLabels=new List<Text>();string[] categories={"すべて","装備品","スキル","補給","工房"};
            void Refresh(bool resetScroll=false)
            {
                souls.text=m.Wallet.Souls.ToString("N0");embers.text=m.Wallet.Embers.ToString("N0");
                noteBand.gameObject.SetActive(!string.IsNullOrEmpty(note.text));
                float offset=resetScroll?0:content.anchoredPosition.y;scroll.StopMovement();
                AtelierUi.Clear(content);AtelierUi.Clear(detail);
                var filtered=offers.FindAll(o=>category=="すべて"||o.Category==category);

                for(int i=0;i<tabs.Count;i++)
                {
                    bool active=categories[i]==category;
                    tabImages[i].sprite=active?AzureMapSkin.Sprite("button-navy"):AzureShopSkin.Sprite("card");
                    tabImages[i].pixelsPerUnitMultiplier=8;tabLabels[i].color=active?AzureMapSkin.Paper:AzureMapSkin.Ink;
                }
                listHeading.text=$"冒険者のための品々   {filtered.Count} 点";
                // Compact catalog requested by the user: ten rows in the available viewport.
                const float rowHeight=32,rowGap=2,padding=2;
                float contentHeight=Mathf.Max(viewport.rect.height,filtered.Count*(rowHeight+rowGap)-rowGap+padding*2);
                content.sizeDelta=new Vector2(386,contentHeight);
                for(int i=0;i<filtered.Count;i++)
                {
                    var offer=filtered[i];float y=-padding-rowHeight*.5f-i*(rowHeight+rowGap);
                    bool active=selected==offer;
                    var foreground=active?AzureMapSkin.Paper:AzureMapSkin.Ink;
                    var b=AtelierUi.Button(content,"Product_"+i,0,y,378,"",()=>{selected=offer;audio?.UiPop();Refresh();},active?AzureMapSkin.Navy:UiSkin.Hex(i%2==0?"#fffdf7":"#f1ede4"),foreground,rowHeight);
                    var rt=(RectTransform)b.transform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);
                    if(active)AtelierUi.Panel(b.transform,"Selection",-187,0,3,rowHeight,AzureMapSkin.Gold);
                    b.gameObject.AddComponent<AzureShopScrollItem>().Scroll=scroll;
                    AtelierUi.Art(b.transform,"Icon",-167,0,26,26,AzureShopSkin.Icon(offer.Icon));
                    AzureShopSkin.Label(b.transform,"Name",-47,0,202,28,offer.Name,15,TextAnchor.MiddleLeft,foreground);
                    int price=offer.Price();
                    if(price>=0)AtelierUi.Art(b.transform,"Currency",84,0,16,20,AtelierUi.Icon(offer.Category=="補給"?"ember":"soul"));
                    AzureShopSkin.Label(b.transform,"Price",142,0,76,28,price<0?(offer.Craft?"作成済":"強化完了"):price.ToString("N0"),14,TextAnchor.MiddleRight,foreground);
                    if(offer.Craft){var tierCol=offer.Tier>=4?UiSkin.Hex("#5fe08a"):offer.Tier==3?UiSkin.Hex("#ffcf3f"):offer.Tier==2?UiSkin.Hex("#4da3ff"):UiSkin.Hex("#f2f4f8");AtelierUi.Panel(b.transform,"Tier",-183,0,4,rowHeight-6,tierCol);}

                }
                if(filtered.Count==0)AzureShopSkin.Label(content,"Empty",0,0,370,60,"この分類の商品はありません",18);
                content.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,contentHeight-viewport.rect.height));
                float range=contentHeight-viewport.rect.height;
                scrollHint.text=range>0?"ドラッグ・ホイールで商品を見る":$"全 {filtered.Count} 点を表示";
                track.gameObject.SetActive(range>.5f);
                scrollbar.size=Mathf.Clamp01(viewport.rect.height/contentHeight);
                scrollbar.SetValueWithoutNotify(range>0?1-content.anchoredPosition.y/range:1);
                if(selected==null){AzureShopSkin.Label(detail,"Empty",0,0,270,100,"商品を選択してください",17);return;}
                var s=selected;
                var compass=AtelierUi.Art(detail,"Compass",0,103,178,178,AtelierUi.Sprite("Art/UI/Icons/compass"));compass.color=new Color(.78f,.66f,.42f,.22f);
                AtelierUi.Art(detail,"Art",0,99,150,142,AzureShopSkin.Icon(s.Icon));
                AzureShopSkin.Label(detail,"Name",0,6,280,44,s.Name,24);
                AtelierUi.Panel(detail,"Rule",0,-20,272,1,UiSkin.Hex("#c6b18b"));
                AtelierUi.Text(detail,"Description",0,-68,270,84,s.Description(),14,AzureMapSkin.Ink);
                AzureShopSkin.Label(detail,"Owned",0,-127,270,26,s.Status(),15,TextAnchor.MiddleLeft);
                int cost=s.Price();string unit=s.Category=="補給"?"エンバー":"ソウル";
                bool canBuy=s.CanBuy();
                AzureShopSkin.Label(detail,"Total",0,-156,270,26,cost<0?(s.Craft?"作成済み":"最大まで強化済み"):s.Craft?$"{cost:N0} ソウル + 素材":$"{cost:N0} {unit} / 1{(s.Category=="補給"?"口":"段階")}",14,TextAnchor.MiddleRight);
                string action=s.Craft?(cost<0?"作成済み":canBuy?"作る  →":s.Reason()):cost<0?"強化完了":canBuy?(s.Category=="補給"?"購入する  →":"強化する  →"):(s.Category=="補給"&&s.Icon=="potion"&&m.Adv.torches>=res.maxTorches?"所持上限に到達":unit+"が不足しています");
                var buy=AzureMapSkin.Button(detail,"Purchase",0,-207,280,action,()=>AtelierUi.Confirm(body,s.Craft?$"{s.Name}\n{cost:N0} ソウルと素材で作ります。":$"{s.Name}\n{cost:N0} {unit}で1{(s.Category=="補給"?"口購入":"段階強化")}します。",()=>{if(s.CanBuy()&&s.Purchase()){audio?.Motion(s.Category=="補給"?MotionCue.Recover:MotionCue.Purchase);SaveData.Save(m,audio);note.text=s.Name+(s.Category=="補給"?" を購入しました":" を強化しました");onSoulsChanged?.Invoke();}else {audio?.Motion(MotionCue.Denied);note.text="購入できません。所持数と通貨をご確認ください。";}Refresh();}),canBuy,52,fontSize:cost<0||canBuy?20:14);
                var colors=buy.colors;colors.disabledColor=Color.white;buy.colors=colors;buy.interactable=canBuy;
            }
            string[] categoryIcons={"star_emblem","swords_dark","compass","orb","gear"};
            for(int i=0;i<categories.Length;i++)
            {
                string cat=categories[i];var b=AtelierUi.Button(body,"Category"+i,railX,132-i*62,156,cat,()=>{category=cat;selected=offers.Find(o=>cat=="すべて"||o.Category==cat);Refresh(true);},Color.white,AzureMapSkin.Ink,54,fontSize:18);
                var im=b.GetComponent<Image>();im.type=Image.Type.Sliced;
                var label=b.GetComponentInChildren<Text>();AzureMapSkin.Typography(label);label.rectTransform.anchoredPosition=new Vector2(14,0);label.rectTransform.sizeDelta=new Vector2(114,46);
                AtelierUi.Art(b.transform,"Icon",-54,0,25,25,i==3?AzureShopSkin.Icon("oil"):AtelierUi.Sprite("Art/UI/Icons/"+categoryIcons[i]));
                tabs.Add(b);tabImages.Add(im);tabLabels.Add(label);
            }
            GameObject stats=null;
            if(m.Config.stats!=null&&m.Config.stats.enabled)
            {
                var b=AtelierUi.Button(body,"Stats",railX,-178,156,"ステータス",()=>{if(stats!=null)return;stats=StatsScreen.Build(body,m,audio,()=>{SaveData.Save(m,audio);onSoulsChanged?.Invoke();},()=>{if(stats!=null){stats.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(stats);else UnityEngine.Object.DestroyImmediate(stats);}stats=null;Refresh();});},Color.white,AzureMapSkin.Ink,54,fontSize:16);
                AzureShopSkin.Card(b.GetComponent<Image>(),false);var label=b.GetComponentInChildren<Text>();AzureMapSkin.Typography(label);
            }
            Refresh();return overlay;
        }
    }
    /// <summary>Keep controller/keyboard-selected rows visible without moving the detail panel.</summary>
    public sealed class AzureShopScrollItem : MonoBehaviour,ISelectHandler
    {
        public ScrollRect Scroll;
        public void OnSelect(BaseEventData eventData)
        {
            if(Scroll==null)return;
            var corners=new Vector3[4];((RectTransform)transform).GetWorldCorners(corners);
            var view=Scroll.viewport;float top=view.InverseTransformPoint(corners[2]).y,bottom=view.InverseTransformPoint(corners[0]).y;
            float shift=top>view.rect.yMax?view.rect.yMax-top:bottom<view.rect.yMin?view.rect.yMin-bottom:0;
            if(Mathf.Abs(shift)<.01f)return;
            Scroll.StopMovement();var p=Scroll.content.anchoredPosition;
            p.y=Mathf.Clamp(p.y+shift,0,Mathf.Max(0,Scroll.content.rect.height-view.rect.height));Scroll.content.anchoredPosition=p;
        }
    }

}
