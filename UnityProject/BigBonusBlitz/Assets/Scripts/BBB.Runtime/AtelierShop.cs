using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;
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
        }
        public static GameObject Build(Transform stage,SlotMachine m,AudioManager audio,Action onSoulsChanged,Action onClose)
        {
            var body=AtelierUi.Screen(stage,"ShopCard",AtelierUi.Paper,null,out var overlay);
            AtelierUi.Panel(body,"HeaderBand",0,218,960,104,AtelierUi.Ink);
            AtelierUi.Header(body,"MERCHANTS GUILD / TOWN","旅支度の商店",AtelierUi.Light);
            var wallet=AtelierUi.Text(body,"Wallet",254,215,280,64,"",17,AtelierUi.Light,true,TextAnchor.MiddleRight);
            AtelierUi.Button(body,"CloseTop",436,232,44,"×",onClose);
            AtelierUi.Panel(body,"CategoryRail",-397,-25,166,382,UiSkin.Hex("#e5e0d4"));
            AtelierUi.Panel(body,"DetailSurface",329,-25,302,382,UiSkin.Hex("#e1dfd3"));
            var list=UiSkin.Rect(body,"Products",new Vector2(-69,-12),new Vector2(464,340));
            var detail=UiSkin.Rect(body,"Details",new Vector2(329,-25),new Vector2(302,382));
            var note=AtelierUi.Text(body,"Status",0,-243,908,26,"次の冒険に、確かな備えを。",14,AtelierUi.Muted);
            var offers=new List<Offer>();
            foreach(var item in m.Config.shop?.items ?? new List<ShopItem>())
            {
                if(item==null)continue;var it=item;
                offers.Add(new Offer{Name=it.name,Icon=string.IsNullOrEmpty(it.icon)?"sword":it.icon,Category=it.kind=="gear"?"装備品":"スキル",
                    Description=()=>ShopDirector.Describe(it,m.Wallet.LevelOf(it.id))+"\n\n強化後："+ShopDirector.Describe(it,Math.Min(it.maxLevel,m.Wallet.LevelOf(it.id)+1)),
                    Status=()=>$"強化 Lv {m.Wallet.LevelOf(it.id)} / {it.maxLevel}",Price=()=>ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id)),
                    CanBuy=()=>ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id))>=0&&m.Wallet.Souls>=ShopDirector.NextCost(it,m.Wallet.LevelOf(it.id)),Purchase=()=>ShopDirector.Buy(m.Wallet,it)});
            }
            var res=m.Config.adventure?.resource;
            if(m.AdventureEnabled&&res!=null&&res.enabled)
            {
                offers.Add(new Offer{Name=res.name,Icon="potion",Category="補給",Description=()=>$"1個で{res.hpName}を{m.TorchSpinsPerUnit}回復。\n冒険の備えを整えましょう。",Status=()=>$"所持 {m.Adv.torches} / {res.maxTorches} 個",Price=()=>Mathf.Max(0,res.torchCost),CanBuy=()=>m.Wallet.Embers>=Mathf.Max(0,res.torchCost)&&m.Adv.torches<res.maxTorches,Purchase=()=>{int cost=Mathf.Max(0,res.torchCost);if(m.Wallet.Embers<cost||AdventureDirector.AddTorch(m.Config.adventure,m.Adv,1,m.TorchSpinsPerUnit)<=0)return false;m.Wallet.Embers-=cost;return true;}});
                offers.Add(new Offer{Name="灯火を補充",Icon="ember",Category="補給",Description=()=>$"冒険用エンバーを{res.creditAmount:N0}補充します。\n支払いには所持品のエンバーを使用します。",Status=()=>$"冒険用 {m.Credit:N0} エンバー",Price=()=>Mathf.Max(0,res.creditCost),CanBuy=()=>m.Wallet.Embers>=Mathf.Max(0,res.creditCost),Purchase=()=>{int cost=Mathf.Max(0,res.creditCost);if(m.Wallet.Embers<cost)return false;m.Wallet.Embers-=cost;m.Credit+=Mathf.Max(0,res.creditAmount);return true;}});
            }
            string category="すべて";int page=0;Offer selected=offers.Count>0?offers[0]:null;
            var tabs=new List<Button>();string[] categories={"すべて","装備品","スキル","補給"};
            void Refresh()
            {
                wallet.text=$"所持ソウル  {m.Wallet.Souls:N0}\nエンバー  {m.Wallet.Embers:N0}";
                AtelierUi.Clear(list);AtelierUi.Clear(detail);
                var filtered=offers.FindAll(o=>category=="すべて"||o.Category==category);
                page=Mathf.Clamp(page,0,Mathf.Max(0,(filtered.Count-1)/4));
                for(int i=0;i<tabs.Count;i++){tabs[i].GetComponent<Image>().color=categories[i]==category?AtelierUi.Ink:UiSkin.Hex("#ddd9ce");tabs[i].GetComponentInChildren<Text>().color=categories[i]==category?AtelierUi.Light:AtelierUi.Ink;}
                AtelierUi.Text(list,"ListHeading",0,155,440,28,$"冒険者のための品々   {filtered.Count} 点",16,AtelierUi.Ink,true);
                for(int i=page*4;i<Math.Min(filtered.Count,page*4+4);i++)
                {
                    var offer=filtered[i];int j=i-page*4;float x=-116+(j%2)*232,y=64-(j/2)*126;
                    var b=AtelierUi.Button(list,"Product_"+i,x,y,218,"",()=>{selected=offer;audio?.UiPop();Refresh();},selected==offer?UiSkin.Hex("#fffbee"):UiSkin.Hex("#f8f5ef"),AtelierUi.Ink,114);
                    if(selected==offer){var ol=b.gameObject.AddComponent<Outline>();ol.effectColor=UiSkin.Hex("#97814c");ol.effectDistance=new Vector2(1,1);}
                    AtelierUi.Art(b.transform,"Icon",-70,12,50,50,AtelierUi.Icon(offer.Icon));
                    AtelierUi.Text(b.transform,"Name",30,22,134,48,offer.Name,16,AtelierUi.Ink,true);
                    int price=offer.Price();AtelierUi.Text(b.transform,"Price",30,-18,134,24,price<0?"強化完了":price.ToString("N0"),20,AtelierUi.Ink,true);
                    AtelierUi.Text(b.transform,"Unit",0,-43,194,18,offer.Category=="補給"?"エンバー / 1口":"ソウル / 1段階",11,AtelierUi.Muted);
                }
                if(filtered.Count==0)AtelierUi.Text(list,"Empty",0,0,440,60,"この分類の商品はありません",18);
                var prev=AtelierUi.Button(list,"Previous",-160,-155,110,"前の品",()=>{page--;Refresh();},UiSkin.Hex("#ddd9ce"),AtelierUi.Ink);prev.interactable=page>0;
                AtelierUi.Text(list,"Page",0,-155,90,24,$"{page+1} / {Math.Max(1,(filtered.Count+3)/4)}",13,AtelierUi.Muted,false,TextAnchor.MiddleCenter);
                var next=AtelierUi.Button(list,"Next",160,-155,110,"次の品",()=>{page++;Refresh();},UiSkin.Hex("#ddd9ce"),AtelierUi.Ink);next.interactable=(page+1)*4<filtered.Count;
                if(selected==null)return;
                var s=selected;AtelierUi.Art(detail,"Art",0,136,96,96,AtelierUi.Icon(s.Icon));
                AtelierUi.Text(detail,"Name",0,50,254,48,s.Name,23,AtelierUi.Ink,true);
                AtelierUi.Text(detail,"Description",0,-20,254,86,s.Description(),14,AtelierUi.Muted);
                AtelierUi.Text(detail,"Owned",0,-80,254,24,s.Status(),15,AtelierUi.Ink,true);
                int cost=s.Price();string unit=s.Category=="補給"?"エンバー":"ソウル";
                AtelierUi.Text(detail,"Total",0,-113,254,30,cost<0?"最大まで強化済み":$"{cost:N0} {unit} / 1{(s.Category=="補給"?"口":"段階")}",19,AtelierUi.Ink,true,TextAnchor.MiddleRight);
                var buy=AtelierUi.Button(detail,"Purchase",0,-157,254,cost<0?"強化完了":s.CanBuy()?"購入する  →":"不足 / 上限に到達",()=>AtelierUi.Confirm(body,$"{s.Name}\n{cost:N0} {unit}で1{(s.Category=="補給"?"口購入":"段階強化")}します。",()=>{if(s.CanBuy()&&s.Purchase()){audio?.Motion(s.Category=="補給"?MotionCue.Recover:MotionCue.Purchase);SaveData.Save(m,audio);note.text=s.Name+" を購入しました";onSoulsChanged?.Invoke();}else { audio?.Motion(MotionCue.Denied); note.text="購入できません。所持数と通貨をご確認ください。"; }Refresh();}));
                buy.interactable=s.CanBuy();
            }
            for(int i=0;i<categories.Length;i++){string cat=categories[i];tabs.Add(AtelierUi.Button(body,"Category"+i,-397,124-i*56,138,cat,()=>{category=cat;page=0;selected=offers.Find(o=>cat=="すべて"||o.Category==cat);Refresh();},UiSkin.Hex("#ddd9ce"),AtelierUi.Ink));}
            GameObject stats=null;
            if(m.Config.stats!=null&&m.Config.stats.enabled)AtelierUi.Button(body,"Stats",-397,-136,138,"ステータス",()=>{if(stats!=null)return;stats=StatsScreen.Build(body,m,audio,()=>{SaveData.Save(m,audio);onSoulsChanged?.Invoke();},()=>{UnityEngine.Object.Destroy(stats);stats=null;Refresh();});},UiSkin.Hex("#ddd9ce"),AtelierUi.Ink);
            Refresh();return overlay;
        }
    }
}
