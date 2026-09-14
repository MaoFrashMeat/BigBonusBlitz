using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Unity uGUI geometry: sky, celestial bodies, clouds, depth haze and weather. No weather baked into art.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AdventureAtmosphereGraphic : MaskableGraphic
    {
        public ParallaxBackground owner;
        public bool foreground;
        static float Hash(int n) { unchecked { uint x=(uint)(n*747796405+2891336453L);x=(x^(x>>16))*2246822519u;x^=x>>13;return (x&0xffffff)/16777215f; } }
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd)
        {
            int i=vh.currentVertCount;vh.AddVert(a,ca,Vector2.zero);vh.AddVert(b,cb,Vector2.zero);vh.AddVert(c,cc,Vector2.zero);vh.AddVert(d,cd,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        void Rect(VertexHelper vh,float x,float y,float w,float h,Color bottom,Color top)
        { Quad(vh,new Vector2(x-w/2,y-h/2),new Vector2(x-w/2,y+h/2),new Vector2(x+w/2,y+h/2),new Vector2(x+w/2,y-h/2),bottom,top,top,bottom); }
        void Ellipse(VertexHelper vh,float x,float y,float rx,float ry,Color c,bool soft=true)
        {
            int center=vh.currentVertCount;vh.AddVert(new Vector2(x,y),c,Vector2.zero);var edge=c;if(soft)edge.a=0;
            const int n=24;for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;vh.AddVert(new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),edge,Vector2.zero);if(i>0)vh.AddTriangle(center,center+i,center+i+1);}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(owner==null)return;var r=rectTransform.rect;float w=r.width,h=r.height,t=owner.Elapsed;var p=owner.Profile;
            AdventureEnvironmentCatalog.Palette(owner.Hour,out var sky,out var horizon,out var light);
            float night=1-Mathf.Clamp01((light.r-.4f)*1.7f);
            if(!foreground)
            {
                if(p.indoor){sky=Color.Lerp(new Color(.035f,.055f,.085f),p.accent,.08f);horizon=Color.Lerp(sky,p.accent,.22f);}
                float overcast=owner.WeatherStrength(AdventureWeather.Rain)+owner.WeatherStrength(AdventureWeather.Storm);
                sky=Color.Lerp(sky,new Color(.19f,.25f,.32f),overcast*.65f);horizon=Color.Lerp(horizon,new Color(.43f,.49f,.51f),overcast*.6f);
                Rect(vh,0,0,w,h,horizon,sky);
                if(!p.indoor)
                {
                    for(int i=0;i<48;i++){float a=night*(1-overcast)*(.3f+.3f*Mathf.Sin(t*.65f+i));Rect(vh,(Hash(i)-.5f)*w,(Hash(i+73)*.6f+.2f)*h-h*.2f,1.3f,1.3f,new Color(.8f,.88f,1,a),new Color(.8f,.88f,1,a));}
                    float sunDay=Mathf.Clamp01(Mathf.Sin((owner.Hour-6)/12*Mathf.PI));float x=(owner.Hour/24-.5f)*w*.8f;
                    Color sun=Color.Lerp(new Color(1,.67f,.39f,.8f),new Color(1,.96f,.75f,.8f),sunDay);if(night>.55f)sun=new Color(.75f,.84f,1,.65f);
                    sun.a*=1-overcast*.9f;float y=h*(.14f+.2f*sunDay);
                    Ellipse(vh,x,y,h*.16f,h*.16f,new Color(sun.r,sun.g,sun.b,sun.a*.24f));Ellipse(vh,x,y,h*.035f,h*.035f,sun,false);
                    for(int i=0;i<7;i++){float cx=(Mathf.Repeat(Hash(i+500)+t*.003f,1.4f)-.7f)*w;float cy=h*(.16f+Hash(i+600)*.27f);Ellipse(vh,cx,cy,w*(.12f+Hash(i)*.1f),h*.09f,new Color(.93f,.95f,1,.22f*(1-night)+overcast*.24f));}
                }
                return;
            }
            // Haze veil is thin enough to preserve hero silhouettes; opaque HUD sits outside this masked area.
            float fog=Mathf.Clamp01(p.haze+owner.WeatherStrength(AdventureWeather.Fog)*.55f);
            for(int i=0;i<4;i++){float x=Mathf.Sin(t*.08f+i*2)*w*.4f;Ellipse(vh,x,h*(-.38f+i*.09f),w*.82f,h*(.09f+i*.015f),new Color(horizon.r,horizon.g,horizon.b,fog*.28f));}
            DrawWeather(vh,w,h,t,AdventureWeather.Rain,owner.WeatherStrength(AdventureWeather.Rain));
            DrawWeather(vh,w,h,t,AdventureWeather.Storm,owner.WeatherStrength(AdventureWeather.Storm));
            DrawWeather(vh,w,h,t,AdventureWeather.Snow,owner.WeatherStrength(AdventureWeather.Snow));
            DrawWeather(vh,w,h,t,AdventureWeather.Embers,owner.WeatherStrength(AdventureWeather.Embers));
            DrawWeather(vh,w,h,t,AdventureWeather.Drips,owner.WeatherStrength(AdventureWeather.Drips));
            DrawWeather(vh,w,h,t,AdventureWeather.Spores,owner.WeatherStrength(AdventureWeather.Spores));
            float storm=owner.WeatherStrength(AdventureWeather.Storm);float phase=Mathf.Repeat(t,19);if(storm>0&&phase<.24f){float a=storm*.15f*(1-phase/.24f);Rect(vh,0,0,w,h,new Color(.85f,.9f,1,a),new Color(.85f,.9f,1,a));}
            if(p.indoor || p.weather=="Embers")for(int i=0;i<4;i++)Ellipse(vh,(Hash(i+701)-.5f)*w,-h*.25f,w*.13f,h*.26f,new Color(p.accent.r,p.accent.g,p.accent.b,.12f+.025f*Mathf.Sin(t*.9f+i)));
        }
        void DrawWeather(VertexHelper vh,float w,float h,float time,AdventureWeather kind,float strength)
        {
            if(strength<.001f)return;bool rain=kind==AdventureWeather.Rain||kind==AdventureWeather.Storm||kind==AdventureWeather.Drips;
            int count=kind==AdventureWeather.Storm?150:kind==AdventureWeather.Rain?90:kind==AdventureWeather.Drips?24:60;
            int seed=(int)kind*1237;
            for(int i=0;i<count;i++)
            {
                float depth=.35f+.65f*Hash(i+seed+21);float speed=rain?(.55f+depth*.85f):(.025f+depth*.1f);
                bool rising=kind==AdventureWeather.Embers||kind==AdventureWeather.Spores;
                float yy=Mathf.Repeat(Hash(i+seed+1)+time*speed*(rising?1:-1),1);
                float wind=kind==AdventureWeather.Drips?0:rain?-.08f:.025f;
                float xx=Mathf.Repeat(Hash(i+seed+2)+time*wind+(!rain?Mathf.Sin(time*.6f+i)*.025f:0),1);
                float x=(xx-.5f)*w,y=(yy-.5f)*h;float edge=Mathf.Clamp01(Mathf.Min(yy,1-yy)*12);float a=strength*edge*(.17f+depth*.3f);
                Color c=kind==AdventureWeather.Embers?new Color(1,.54f,.14f,a):kind==AdventureWeather.Spores?new Color(.64f,.95f,.78f,a):new Color(.75f,.87f,1,a);
                if(rain){float len=h*(.025f+depth*.04f);float slant=kind==AdventureWeather.Drips?0:len*.3f;float width=.55f+depth*.6f;Quad(vh,new Vector2(x,y),new Vector2(x+slant,y+len),new Vector2(x+slant+width,y+len),new Vector2(x+width,y),c,new Color(c.r,c.g,c.b,0),new Color(c.r,c.g,c.b,0),c);}
                else Ellipse(vh,x,y,1+depth*2,1+depth*2,c);
            }
        }
    }
}
