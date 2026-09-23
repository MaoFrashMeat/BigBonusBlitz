using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Original generated shop art, sliced without modifying the source PNGs.</summary>
    public static class AzureShopSkin
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>cache.Clear();
        public static Sprite Sprite(string name)
        {
            if(cache.TryGetValue(name,out var s)&&s!=null)return s;
            var t=Resources.Load<Texture2D>("Art/UI/AzureShop/"+name);if(t==null)return null;
            var rect=new Rect(0,0,t.width,t.height);
            // Discard only the transparent export margin using a sprite subrect.
            if(name=="card"||name=="panel-gold")rect=new Rect(0,t.height*.025f,t.width,t.height*.945f);
            var border=name=="terrace"?Vector4.zero:new Vector4(rect.width*.10f,rect.height*.13f,rect.width*.10f,rect.height*.13f);
            return cache[name]=UnityEngine.Sprite.Create(t,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
        }
        public static Image Surface(Transform p,string name,float x,float y,float w,float h,bool gold)
        {
            var im=UiSkin.Img(p,name,new Vector2(x,y),new Vector2(w,h),Sprite(gold?"panel-gold":"card"),Color.white);
            im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=8;im.raycastTarget=false;return im;
        }
        public static void Card(Image im,bool selected)
        {im.sprite=Sprite(selected?"panel-gold":"card");im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=8;im.color=Color.white;}
        public static Text Wallet(Transform p,string name,float x,float width,Sprite icon,string caption)
        {
            var value=AzureMapSkin.Resource(p,name,x,width,icon,caption);
            var panel=value.transform.parent;var im=panel.GetComponent<Image>();
            im.sprite=AzureMapSkin.Sprite("button-navy");im.color=Color.white;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=8;
            panel.Find("GoldRule").gameObject.SetActive(false);
            return value;
        }
        public static Text Label(Transform p,string name,float x,float y,float w,float h,string value,int size=16,TextAnchor align=TextAnchor.MiddleCenter,Color? color=null)
        {
            var t=AtelierUi.Text(p,name,x,y,w,h,value,size,color??AzureMapSkin.Ink,false,align);AzureMapSkin.Typography(t);return t;
        }
        public static Sprite Icon(string name)
        {
            int i=System.Array.IndexOf(new[]{"eye","lantern","amulet","oil"},name);
            if(i<0)return AtelierUi.Icon(name);
            string key="icon-"+name;if(cache.TryGetValue(key,out var s)&&s!=null)return s;
            var t=Resources.Load<Texture2D>("Art/UI/AzureShop/items");if(t==null)return AtelierUi.Icon(name);
            var rect=new Rect((i%2)*t.width*.5f,(i<2?1:0)*t.height*.5f,t.width*.5f,t.height*.5f);
            return cache[key]=UnityEngine.Sprite.Create(t,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }
    }
}
