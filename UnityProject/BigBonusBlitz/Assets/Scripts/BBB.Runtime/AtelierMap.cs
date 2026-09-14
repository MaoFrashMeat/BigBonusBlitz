using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    public static class AtelierMap
    {
        static IEnumerable<string> Targets(StageNode n)
        {
            var set=new HashSet<string>();
            if(n.routeConditions!=null)foreach(var c in n.routeConditions)if(c!=null&&!c.hidden&&!string.IsNullOrEmpty(c.to)&&set.Add(c.to))yield return c.to;
            if(n.routeDefault!=null)foreach(var id in n.routeDefault.Keys)if(id!="NONE"&&set.Add(id))yield return id;
            if(n.routeByFlag!=null)foreach(var routes in n.routeByFlag.Values)if(routes!=null)foreach(var id in routes.Keys)if(id!="NONE"&&set.Add(id))yield return id;
        }
        public static Action Build(Transform stage,SlotMachine m,Action adventure,Action shop,Action equip,Action trophy,Action settings,Action title,out Text message,bool inRun=false)
        {
            var bg=AtelierUi.Panel(stage,"AtelierMap",0,0,960,540,UiSkin.Hex("#122e32"));bg.gameObject.AddComponent<AtelierModalInput>();
            AtelierUi.Header(bg,"CHAPTER / BEYOND THE MIST",m.Config.adventure?.chapterName??"冒険の準備",AtelierUi.Light);
            var purse=AtelierUi.Text(bg,"Purse",304,224,302,62,"",16,AtelierUi.Gold,true,TextAnchor.MiddleRight);
            var view=AtelierUi.Panel(bg,"MapViewport",-158,-14,592,352,UiSkin.Hex("#193a3d"));view.GetComponent<Image>().raycastTarget=true;view.gameObject.AddComponent<RectMask2D>();
            var terrain=AtelierUi.Art(view,"Terrain",0,0,626,352,AtelierUi.Sprite("Art/UI/Title/sky_mountains_cloudsea"));terrain.color=new Color(.21f,.32f,.28f,1);
            var contours=UiSkin.Rect(view,"Contours",Vector2.zero,new Vector2(592,352)).gameObject.AddComponent<AtelierContourGraphic>();contours.color=new Color(.67f,.79f,.66f,.15f);contours.raycastTarget=false;
            var content=UiSkin.Rect(view,"MapContent",Vector2.zero,new Vector2(960,600));content.anchorMin=content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,1);
            var scroll=view.gameObject.AddComponent<ScrollRect>();MotionSound.Attach(scroll);scroll.viewport=view;scroll.content=content;scroll.horizontal=true;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
            var detail=AtelierUi.Panel(bg,"Destination",313,-14,282,352,UiSkin.Hex("#10292e"));
            var resource=AtelierUi.Text(bg,"Resources",-158,-208,592,22,"",13,AtelierUi.Sub);
            message=AtelierUi.Text(bg,"Message",313,-211,282,34,"",12,AtelierUi.Gold);
            AtelierUi.Button(bg,"Town",-376,-247,156,inRun?"街へ戻る":"街のショップ",shop,AtelierUi.Gold,AtelierUi.Ink);
            AtelierUi.Button(bg,"Equipment",-226,-247,120,"装備",equip,UiSkin.Hex("#284449"));
            AtelierUi.Button(bg,"Trophies",-94,-247,120,"実績",trophy,UiSkin.Hex("#284449"));
            AtelierUi.Button(bg,"Settings",38,-247,120,"設定",settings,UiSkin.Hex("#284449"));
            AtelierUi.Button(bg,"Title",197,-247,174,inRun?"マップを閉じる":"タイトルへ",title,UiSkin.Hex("#284449"));
            AtelierUi.Text(bg,"MapHint",-158,172,592,20,"ROUTE MAP / ドラッグ・ホイールで地図を移動",11,AtelierUi.Sub);
            string selected=m.Adv.nodeId;bool first=true;
            void Refresh()
            {
                purse.text=$"ソウル {m.Wallet.Souls:N0}\n第 {Mathf.Max(1,m.Adv.chapter)} 章";
                resource.text=$"{m.Config.adventure?.resource?.hpName??"ライフ"} {m.Hp:N0}/{m.HpMax:N0}   補給 {m.Adv.torches}   冒険用エンバー {m.Credit:N0}";
                AtelierUi.Clear(content);AtelierUi.Clear(detail);
                var cfg=m.Config.adventure;var nodes=cfg?.nodes??new List<StageNode>();
                var groups=new Dictionary<string,List<StageNode>>();var columns=new List<string>();
                foreach(var n in nodes){if(n==null)continue;string c=n.column??"?";if(!groups.ContainsKey(c)){groups[c]=new List<StageNode>();columns.Add(c);}groups[c].Add(n);}
                int maxRows=1;foreach(var col in columns)maxRows=Mathf.Max(maxRows,groups[col].Count);
                float w=Mathf.Max(592,columns.Count*114+40),h=Mathf.Max(352,maxRows*72+44);content.sizeDelta=new Vector2(w,h);
                var positions=new Dictionary<string,Vector2>();
                for(int ci=0;ci<columns.Count;ci++){var ns=groups[columns[ci]];for(int j=0;j<ns.Count;j++)positions[ns[j].id]=new Vector2(64+ci*114,-h*.5f+(ns.Count-1)*36-j*72);}
                var visited=new HashSet<string>(m.Adv.visited);visited.Add(m.Adv.nodeId);var revealed=new HashSet<string>(visited);
                foreach(var n in nodes)if(n!=null&&visited.Contains(n.id))foreach(var id in Targets(n))revealed.Add(id);
                if(!string.IsNullOrEmpty(m.Adv.nextId))revealed.Add(m.Adv.nextId);
                foreach(var n in nodes)
                {
                    if(n==null||!positions.ContainsKey(n.id)||(cfg.hideRoute&&!visited.Contains(n.id)))continue;
                    foreach(var id in Targets(n)){if(!positions.TryGetValue(id,out var b))continue;var a=positions[n.id];var d=b-a;var line=AtelierUi.Panel(content,"Route",(a.x+b.x)*.5f,(a.y+b.y)*.5f,d.magnitude,2,visited.Contains(id)?AtelierUi.Gold:UiSkin.Hex("#55736b"));line.anchorMin=line.anchorMax=new Vector2(0,1);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
                }
                foreach(var n in nodes)
                {
                    if(n==null||!positions.ContainsKey(n.id))continue;if(cfg.hideRoute&&!revealed.Contains(n.id))continue;
                    bool known=!cfg.hideRoute||visited.Contains(n.id);var p=positions[n.id];var node=n;
                    var b=AtelierUi.Button(content,"Node_"+n.id,p.x,p.y,48,n.id==m.Adv.nodeId?"◆":visited.Contains(n.id)?"✓":"?",()=>{selected=node.id;Refresh();},n.id==selected?AtelierUi.Gold:UiSkin.Hex("#2f5451"),n.id==selected?AtelierUi.Ink:AtelierUi.Light,48);
                    var rt=(RectTransform)b.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);
                    var label=AtelierUi.Text(content,"NodeLabel_"+n.id,p.x,p.y-31,104,18,known?n.id+" "+n.name:n.id+" 未発見",10,AtelierUi.Light,false,TextAnchor.MiddleCenter);label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1);
                }
                if(first&&positions.TryGetValue(m.Adv.nodeId,out var current)){Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=Mathf.Clamp01((current.x-296)/Mathf.Max(1,w-592));scroll.verticalNormalizedPosition=Mathf.Clamp01(1-(-current.y-176)/Mathf.Max(1,h-352));first=false;}
                var chosen=cfg?.Find(selected);bool isKnown=chosen!=null&&(!cfg.hideRoute||visited.Contains(chosen.id));
                AtelierUi.Art(detail,"Landscape",0,115,250,110,AtelierUi.Sprite("Art/UI/Title/sky_mountains_cloudsea"));
                AtelierUi.Text(detail,"Name",0,34,246,58,isKnown?chosen.name:chosen!=null?"未発見の地点":"冒険へ",24,AtelierUi.Light,true);
                AtelierUi.Text(detail,"Facts",0,-43,246,82,isKnown?$"{chosen.id}  /  深さ {chosen.depth}\n滞在 {chosen.spins} G  /  初到達 {chosen.firstVisitSouls} ソウル\n{(chosen.id==m.Adv.nodeId?"現在地：残り "+m.Adv.spinsLeft+" G":"通過ルートを確認中")}":"先の地点は冒険を進めると判明します。\n進路は小役と達成条件で決まります。",15,AtelierUi.Sub);
                string routeInfo="進路は冒険中に決定します。";
                if(isKnown&&chosen.id==m.Adv.nodeId) routeInfo=m.Adv.nextId!=null?"次の進路："+m.Adv.nextId:"進路はまだ決まっていません";
                AtelierUi.Button(detail,"Conditions",0,-97,246,"分岐条件を見る",()=>
                {
                    var text=new System.Text.StringBuilder(routeInfo);
                    if(isKnown&&chosen.routeConditions!=null) foreach(var c in chosen.routeConditions)
                    {
                        if(c==null)continue;bool met=AdventureDirector.Meets(c,m.Adv,m.Credit,m.Wallet.Souls,m.PlayerLevel);
                        if(c.hidden&&!met)continue;
                        text.Append("\n\n→ ").Append(c.to).Append("  ").Append(AdventureDirector.DescribeCondition(c));
                        if(chosen.id==m.Adv.nodeId)text.Append("\n").Append(met?"達成":AdventureDirector.ProgressText(c,m.Adv,m.Credit,m.Wallet.Souls,m.PlayerLevel));
                    }
                    var panel=AtelierUi.Screen(bg,"RouteConditions",AtelierUi.Night,null,out var dialog);
                    AtelierUi.Header(panel,"ROUTE CONDITIONS",isKnown?chosen.name:"未発見の地点",AtelierUi.Light);
                    var viewport=UiSkin.Rect(panel,"Viewport",new Vector2(0,-6),new Vector2(860,352));viewport.gameObject.AddComponent<RectMask2D>();viewport.gameObject.AddComponent<Image>().color=Color.clear;
                    var label=AtelierUi.Text(viewport,"Conditions",0,0,828,800,text.ToString(),18,AtelierUi.Light);label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,1);label.rectTransform.pivot=new Vector2(.5f,1);label.alignment=TextAnchor.UpperLeft;label.rectTransform.sizeDelta=new Vector2(828,Mathf.Max(352,label.preferredHeight+20));
                    var sc=viewport.gameObject.AddComponent<ScrollRect>();MotionSound.Attach(sc);sc.viewport=viewport;sc.content=label.rectTransform;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=32;
                    AtelierUi.Button(panel,"Back",0,-231,260,"地図へ戻る",()=>UnityEngine.Object.Destroy(dialog),AtelierUi.Gold,AtelierUi.Ink);
                },UiSkin.Hex("#284449"));
                AtelierUi.Button(detail,"Depart",0,-148,246,inRun?"冒険に戻る  →":"冒険へ進む  →",adventure,AtelierUi.Gold,AtelierUi.Ink);
            }
            Refresh();return Refresh;
        }
    }
}
