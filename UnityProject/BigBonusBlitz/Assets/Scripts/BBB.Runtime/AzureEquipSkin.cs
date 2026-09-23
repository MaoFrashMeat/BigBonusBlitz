using System;
using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    public static class AzureEquipSkin
    {
        public static readonly Color Line=UiSkin.Hex("#998567"), Muted=UiSkin.Hex("#c4cedd");
        public static readonly string[] PoseNames={"一緒に行こう","ピース！","準備万端","飛び出そう","元気いっぱい"};
        public static Sprite Art(string name)=>AtelierUi.Sprite("Art/UI/AzureEquip/"+name);
        public static Sprite Portrait(int index)=>Art("salia-"+index);
        public static Text Text(Transform p,string name,float x,float y,float w,float h,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var t=AtelierUi.Text(p,name,x,y,w,h,value,size,color,false,align);
            AzureMapSkin.Typography(t,true);return t;
        }
        public static RectTransform Panel(Transform p,string name,float x,float y,float w,float h)
        {
            var panel=AtelierUi.Panel(p,name,x,y,w,h,UiSkin.Hex("#111e30"));
            AtelierUi.Edge(panel,Line);
            // Delicate gold corners match the reference without baking readable content into the artwork.
            foreach(float sx in new[]{-1f,1f})foreach(float sy in new[]{-1f,1f})
            {
                var corner=AtelierUi.Panel(panel,"Corner",sx*(w*.5f-4),sy*(h*.5f-4),5,5,AzureMapSkin.Gold);
                corner.localRotation=Quaternion.Euler(0,0,45);
            }
            return panel;
        }
        public static void Nav(Transform p,string name,float y,string label,string icon,Action action,bool active)
        {
            var b=AtelierUi.Button(p,name,-542,y,64,"",action,active?UiSkin.Hex("#44372b"):UiSkin.Hex("#111e30"),AzureMapSkin.Paper,80,active?AzureMapSkin.Gold:Line);
            AtelierUi.Art(b.transform,"Icon",0,14,32,32,AtelierUi.Icon(icon));
            Text(b.transform,"Caption",0,-23,62,26,label,11,AzureMapSkin.Paper);
            if(active)b.interactable=false;
        }
    }
}
