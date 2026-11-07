using System;
using System.Collections.Generic;
using System.Text;
using BBB.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Run-local curse HUD. Grouping preserves each curse/blessing pair in the tooltip.</summary>
    public sealed class CurseHudPresenter : MonoBehaviour
    {
        SlotMachine machine;
        RectTransform stage, root, tooltip;
        int? signature;
        string hoverId;
        float hoverAt;
        bool blocked, pinned;
        readonly Dictionary<string,List<CurseInstance>> groups=new Dictionary<string,List<CurseInstance>>();
        public int GroupCount => groups.Count;
        public bool TooltipVisible => tooltip!=null&&tooltip.gameObject.activeSelf;
        public void BindMachine(SlotMachine current)
        {
            if(machine==current)return;
            machine=current;signature=null;HideTooltip();Refresh(false);
        }
        public static CurseHudPresenter Create(Transform parent,RectTransform stage,SlotMachine machine,Vector2 position)
        {
            var r=UiSkin.Rect(parent,"CurseHud",position,new Vector2(290,50));
            var view=r.gameObject.AddComponent<CurseHudPresenter>();
            view.root=r;view.stage=stage;view.machine=machine;view.Refresh(false);return view;
        }
        public void Refresh(bool modalOpen)
        {
            blocked=modalOpen;
            if(blocked){HideTooltip();hoverId=null;}
            int now=TakenHash();
            if(now!=signature)
            {
                signature=now;HideTooltip();hoverId=null;groups.Clear();
                foreach(var c in machine.Curse.Taken)
                {
                    if(c==null)continue;
                    string id=string.IsNullOrEmpty(c.curseId)?"unknown:"+c.curseEffect:c.curseId;
                    if(!groups.TryGetValue(id,out var list))groups[id]=list=new List<CurseInstance>();list.Add(c);
                }
                for(int i=root.childCount-1;i>=0;i--){var go=root.GetChild(i).gameObject;go.SetActive(false);if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
                Build();
            }
            root.gameObject.SetActive(groups.Count>0&&!blocked);
            if(!blocked&&!pinned&&hoverId!=null&&Time.unscaledTime>=hoverAt)ShowTooltip(hoverId,false);
        }
        // Refresh runs every frame, so the change check must not allocate.
        int TakenHash()
        {
            unchecked
            {
                int h=17;
                foreach(var c in machine.Curse.Taken)
                {
                    if(c==null)continue;
                    h=h*31+(c.curseId?.GetHashCode()??0);h=h*31+(c.curseName?.GetHashCode()??0);h=h*31+(c.curseEffect?.GetHashCode()??0);h=h*31+c.curseValue;
                    h=h*31+(c.blessId?.GetHashCode()??0);h=h*31+(c.blessName?.GetHashCode()??0);h=h*31+(c.blessEffect?.GetHashCode()??0);h=h*31+c.blessValue;h=h*31+c.depth;
                }
                return h;
            }
        }
        void Build()
        {
            if(groups.Count==0)return;
            var bg=UiSkin.Img(root,"Panel",Vector2.zero,root.sizeDelta,AzureMapSkin.Sprite("button-navy"),Color.white);
            bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=8;
            var label=UiFactory.Label(root,"Caption",new Vector2(-112,0),new Vector2(44,32),"呪い",12,TextAnchor.MiddleCenter,AzureMapSkin.Gold);
            AzureMapSkin.Typography(label);
            int i=0;
            float step=groups.Count>4?48:52; // Tighter when the "+N" button has to fit inside the panel.
            foreach(var pair in groups)
            {
                if(i>=4)break;
                string id=pair.Key;
                var b=UiSkin.Button(root,"Curse_"+id,new Vector2(-67+i*step,0),new Vector2(46,46),"",()=>ShowTooltip(id,true),AzureMapSkin.Navy,12,false,8);
                var image=(Image)b.targetGraphic;image.sprite=UiSkin.Rounded(9);image.color=AzureMapSkin.Gold;
                var mask=UiSkin.Img(b.transform,"Mask",Vector2.zero,new Vector2(42,42),UiSkin.Rounded(7),AzureMapSkin.Navy);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                UiSkin.Img(mask.transform,"Icon",Vector2.zero,new Vector2(42,42),AdventurePresentation.CurseIcon(pair.Value[0].curseId),Color.white);
                if(pair.Value.Count>1)
                {
                    var badge=UiSkin.Img(b.transform,"CountBg",new Vector2(16,-15),new Vector2(20,18),UiSkin.Rounded(6),AzureMapSkin.Navy);
                    UiFactory.Label(badge.transform,"Count",Vector2.zero,new Vector2(20,18),pair.Value.Count.ToString(),11,TextAnchor.MiddleCenter,Color.white);
                }
                var trigger=b.gameObject.AddComponent<CurseHudFocus>();trigger.Enter=()=>Hover(id);trigger.Exit=()=>Leave(id);i++;
            }
            if(groups.Count>4)UiSkin.Button(root,"AllCurses",new Vector2(123,0),new Vector2(44,46),"+"+(groups.Count-4),()=>ShowTooltip(null,true),AzureMapSkin.Navy,12,false,6);
        }
        void Hover(string id){if(blocked||pinned)return;hoverId=id;hoverAt=Time.unscaledTime+Mathf.Max(0,AdventurePresentation.Config.tooltipDelay);}
        void Leave(string id){if(hoverId==id)hoverId=null;if(!pinned)HideTooltip();}
        public void HideTooltip()
        {
            pinned=false;hoverId=null;
            if(tooltip==null)return;
            tooltip.gameObject.SetActive(false);
            if(Application.isPlaying)Destroy(tooltip.gameObject);else DestroyImmediate(tooltip.gameObject);
            tooltip=null;
        }
        public string Description(string id)
        {
            var lines=new StringBuilder();
            foreach(var pair in groups)
            {
                if(id!=null&&pair.Key!=id)continue;
                var first=pair.Value[0];int sum=0;foreach(var c in pair.Value)sum+=c.curseValue;
                int effective=machine.Curse.CurseTotal(first.curseEffect);
                if(first.curseEffect==CurseEffects.PayoutCut)effective=Mathf.Min(100,effective);
                lines.Append(first.curseName).Append(pair.Value.Count>1?" ×"+pair.Value.Count:"").Append('\n');
                lines.Append("現在の合計: ").Append(CurseScreen.Text(first.curseEffect,effective,true)).Append('\n');
                if(pair.Value.Count>1){lines.Append("この呪いの内訳: ");for(int i=0;i<pair.Value.Count;i++){if(i>0)lines.Append(" + ");lines.Append(pair.Value[i].curseValue);}lines.Append(" = ").Append(sum).Append('\n');}
                foreach(var c in pair.Value)lines.Append("深さ ").Append(c.depth).Append(" の祝福: ").Append(c.blessName).Append('\n').Append(CurseScreen.Text(c.blessEffect,c.blessValue,false)).Append('\n');
                lines.Append('\n');
            }
            return lines.ToString().TrimEnd()+"\n\nこの冒険の間有効・街へ戻ると消えます";
        }
        public void ShowTooltip(string id,bool pin)
        {
            if(blocked||(id!=null&&!groups.ContainsKey(id)))return;
            if(TooltipVisible&&hoverId==id&&pinned==pin)return;
            HideTooltip();pinned=pin;hoverId=id;
            tooltip=UiSkin.Rect(stage,"CurseTooltip",Vector2.zero,stage.sizeDelta);
            if(pin)
            {
                var dismiss=UiFactory.Panel(tooltip,"Dismiss",Vector2.zero,stage.sizeDelta,Color.clear);
                var b=dismiss.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;b.onClick.AddListener(HideTooltip);
            }
            float w=350,h=300;
            var pos=(Vector2)stage.InverseTransformPoint(root.position);
            pos.x=Mathf.Clamp(pos.x, -stage.rect.width/2+w/2+12,stage.rect.width/2-w/2-12);
            pos.y=Mathf.Clamp(pos.y-184,-stage.rect.height/2+h/2+12,stage.rect.height/2-h/2-12);
            var card=UiFactory.Panel(tooltip,"Card",pos,new Vector2(w,h),Color.clear);
            card.GetComponent<Image>().raycastTarget=pin;
            AzureShopSkin.Surface(card,"Paper",0,0,w,h,false);
            var title=AzureShopSkin.Label(card,"Title",0,h/2-31,w-44,28,"呪いと、対の祝福",20);
            var viewport=UiFactory.Panel(card,"Viewport",new Vector2(0,-3),new Vector2(w-44,h-92),Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Image>().raycastTarget=pin;
            var content=UiSkin.Rect(viewport,"Content",Vector2.zero,new Vector2(w-44,h-92));
            content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
            var body=UiFactory.Label(content,"Effects",Vector2.zero,new Vector2(w-44,h-92),Description(id),14,TextAnchor.UpperLeft,AzureMapSkin.Ink);
            body.font=AzureMapSkin.Serif;
            body.horizontalOverflow=HorizontalWrapMode.Wrap;body.verticalOverflow=VerticalWrapMode.Truncate;
            float height=Mathf.Max(h-92,body.preferredHeight+6);
            content.sizeDelta=new Vector2(w-44,height);body.rectTransform.sizeDelta=content.sizeDelta;body.rectTransform.anchorMin=body.rectTransform.anchorMax=new Vector2(.5f,1);body.rectTransform.pivot=new Vector2(.5f,1);body.rectTransform.anchoredPosition=Vector2.zero;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            AzureShopSkin.Label(card,"Help",0,-h/2+23,w-44,20,pin?"上下スクロール / 外側をタップして閉じる":"タップで説明を固定",11);
        }
        void OnDestroy(){HideTooltip();}
    }
    public sealed class CurseHudFocus : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public Action Enter,Exit;
        public void OnPointerEnter(PointerEventData e)=>Enter?.Invoke();
        public void OnPointerExit(PointerEventData e)=>Exit?.Invoke();
        public void OnSelect(BaseEventData e)=>Enter?.Invoke();
        public void OnDeselect(BaseEventData e)=>Exit?.Invoke();
    }
}
