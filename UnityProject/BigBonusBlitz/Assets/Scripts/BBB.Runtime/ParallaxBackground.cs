using System;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Illustrated depth planes with independent walking speeds, a continuous sky clock and Unity weather geometry.</summary>
    public sealed class ParallaxBackground : MonoBehaviour
    {
        public bool IsWalking;
        public ParallaxBackground EnvironmentSource { get; set; }
        public bool AutoCycle = true;
        [Min(60)] public float DayLengthSeconds = 720;
        [Range(0,24)] [SerializeField] float hour = 7;
        [SerializeField] AdventureWeather weather;
        [Range(0,1)] [SerializeField] float intensity = .65f;
        public string CurrentStageId => Profile.id;
        public AdventureEnvironmentProfile Profile { get; private set; }
        public float Hour => hour;
        public float Elapsed { get; private set; }
        public int IllustratedLayerCount => 3;
        public AdventureWeather CurrentWeather => weather;
        public bool HasIllustration => banks != null && banks[activeBank][0].texture != null;
        RawImage[][] banks;
        Material[][] layerMaterials;
        CanvasGroup[] groups;
        AdventureAtmosphereGraphic sky, atmosphere;
        RectTransform area;
        RectTransform scenery;
        int activeBank;
        float stageBlend=1, weatherBlend=1, oldIntensity;
        AdventureWeather oldWeather;
        float weatherSeconds=2, hourFrom, hourTarget, hourBlend=1, hourSeconds=2;
        float offset, meshTimer;
        readonly float[] speeds={.007f,.024f,.065f};
        // Tall middle-plane trees/columns touch the atlas crop. Keep that crop above the
        // viewport instead of exposing a horizontal line at 84% of the screen height.
        readonly Vector2[] heights={new Vector2(.9f,.13f),new Vector2(1.06f,-.01f),new Vector2(.34f,-.035f)};
        public static ParallaxBackground Create(RectTransform area)
        {
            var bg=area.gameObject.AddComponent<ParallaxBackground>();bg.Initialize(area);return bg;
        }
        void Initialize(RectTransform parent)
        {
            area=parent;Profile=AdventureEnvironmentCatalog.Find("A-1");
            scenery=UiSkin.Rect(area,"EnvironmentWorld",Vector2.zero,Vector2.zero);UiSkin.Stretch(scenery);
            sky=Graphic("EnvironmentSky",false);
            banks=new RawImage[2][];layerMaterials=new Material[2][];groups=new CanvasGroup[2];
            var layerShader=Resources.Load<Shader>("Art/Adventure/AdventureSeamless");
            for(int b=0;b<2;b++)
            {
                var root=UiSkin.Rect(scenery,"EnvironmentPlanes"+b,Vector2.zero,Vector2.zero);UiSkin.Stretch(root);
                groups[b]=root.gameObject.AddComponent<CanvasGroup>();groups[b].blocksRaycasts=false;groups[b].interactable=false;banks[b]=new RawImage[3];layerMaterials[b]=new Material[3];
                for(int i=0;i<3;i++)
                {
                    var rt=UiSkin.Rect(root,"Depth"+i,Vector2.zero,Vector2.zero);
                    rt.anchorMin=new Vector2(0,heights[i].y);rt.anchorMax=new Vector2(1,heights[i].y+heights[i].x);rt.offsetMin=rt.offsetMax=Vector2.zero;
                    var image=rt.gameObject.AddComponent<RawImage>();image.raycastTarget=false;banks[b][i]=image;
                    if(layerShader!=null){layerMaterials[b][i]=new Material(layerShader);image.material=layerMaterials[b][i];}
                }
            }
            SetStage("A-1",true);
            atmosphere=Graphic("EnvironmentWeather",true);
        }
        AdventureAtmosphereGraphic Graphic(string name,bool front)
        {
            var rt=UiSkin.Rect(front?area:scenery,name,Vector2.zero,Vector2.zero);UiSkin.Stretch(rt);
            var g=rt.gameObject.AddComponent<AdventureAtmosphereGraphic>();g.owner=this;g.foreground=front;g.raycastTarget=false;return g;
        }
        public void PlaceWeatherAboveCharacters() { if(atmosphere!=null && atmosphere.transform.GetSiblingIndex()!=area.childCount-1)atmosphere.transform.SetAsLastSibling(); }
        public void SetStage(string nodeId,bool instant=false)
        {
            var next=AdventureEnvironmentCatalog.Find(nodeId);
            if(!instant && Profile!=null && Profile.id==next.id && HasIllustration)return;
            Profile=next;
            var tex=Resources.Load<Texture2D>(next.atlas);
            var seam=Resources.Load<Texture2D>("Art/Adventure/Seams/"+next.id);
            if(tex==null)Debug.LogWarning("Adventure illustration missing: "+next.atlas);
            if(tex!=null){tex.wrapModeU=TextureWrapMode.Clamp;tex.wrapModeV=TextureWrapMode.Clamp;}
            activeBank=1-activeBank;
            float[] cuts={0,next.farEnd,next.middleEnd,1};
            for(int i=0;i<3;i++)
            {
                var im=banks[activeBank][i];im.texture=tex;im.enabled=tex!=null;
                float padding=tex!=null?4f/tex.height:0;
                im.uvRect=new Rect(offset*speeds[i],1-cuts[i+1]+padding,1,Mathf.Max(.01f,cuts[i+1]-cuts[i]-2*padding));
                if(layerMaterials[activeBank][i]!=null)
                {
                    // MaskableGraphic caches stencil material copies. Use a new base when atlas rows change.
                    var old=layerMaterials[activeBank][i];var material=new Material(old.shader);
                    material.SetVector("_Row",new Vector4(im.uvRect.y,im.uvRect.height,.07f,i==2?0:.035f));
                    if(seam!=null)
                    {
                        material.SetTexture("_SeamTex",seam);material.SetFloat("_UseSeam",1);
                        material.SetFloat("_Overlap",.22f);material.SetFloat("_SeamFeather",.012f);
                    }
                    layerMaterials[activeBank][i]=material;im.material=material;
                    if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);
                }
            }
            groups[activeBank].transform.SetAsLastSibling();stageBlend=instant?1:0;
            groups[activeBank].alpha=stageBlend;groups[1-activeBank].alpha=1-stageBlend;
            Enum.TryParse(next.weather,out AdventureWeather baseWeather);SetWeather(baseWeather,next.intensity,instant?0:2);
            if(instant){hour=next.hour;hourBlend=1;} // stage transitions preserve a continuous day clock
            ApplyLighting();
        }
        public void SetTimeOfDay(AdventureTime time,float transitionSeconds=2)
        { SetHour(time==AdventureTime.Morning?6.5f:time==AdventureTime.Day?12:time==AdventureTime.Evening?18.5f:23,transitionSeconds); }
        public void SetHour(float value,float transitionSeconds=2)
        {
            AutoCycle=false;hourFrom=hour;hourTarget=Mathf.Repeat(value,24);hourSeconds=Mathf.Max(.01f,transitionSeconds);hourBlend=transitionSeconds<=0?1:0;if(hourBlend==1)hour=hourTarget;ApplyLighting();
        }
        public void SetWeather(AdventureWeather value,float strength=1,float transitionSeconds=2)
        {
            value=AdventureEnvironmentCatalog.WeatherFor(Profile,value);
            oldWeather=AdventureEnvironmentCatalog.WeatherFor(Profile,weather);oldIntensity=intensity;weather=value;intensity=Mathf.Clamp01(strength);
            weatherSeconds=Mathf.Max(.01f,transitionSeconds);weatherBlend=transitionSeconds<=0?1:0;Dirty();
        }
        public float WeatherStrength(AdventureWeather kind)
        { return (weather==kind?intensity*weatherBlend:0)+(oldWeather==kind?oldIntensity*(1-weatherBlend):0); }
        public void Preview(float elapsed)
        { Elapsed=Mathf.Max(0,elapsed);offset=Elapsed;ApplyLighting(); }
        void Update()
        {
            if(banks==null)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);
            if(EnvironmentSource!=null){var src=EnvironmentSource;hour=src.hour;weather=src.weather;oldWeather=src.oldWeather;intensity=src.intensity;oldIntensity=src.oldIntensity;weatherBlend=src.weatherBlend;Elapsed=src.Elapsed;offset=src.offset;AutoCycle=false;hourBlend=1;}
            else Elapsed+=dt;
            if(IsWalking && EnvironmentSource==null)offset+=dt;
            if(hourBlend<1){hourBlend=Mathf.Min(1,hourBlend+dt/hourSeconds);hour=Mathf.Repeat(hourFrom+Mathf.DeltaAngle(hourFrom*15,hourTarget*15)/15*Mathf.SmoothStep(0,1,hourBlend),24);}
            else if(AutoCycle)hour=Mathf.Repeat(hour+dt*24/Mathf.Max(60,DayLengthSeconds),24);
            weather=AdventureEnvironmentCatalog.WeatherFor(Profile,weather);
            if(EnvironmentSource==null)weatherBlend=Mathf.Min(1,weatherBlend+dt/weatherSeconds);
            stageBlend=Mathf.Min(1,stageBlend+dt/1.4f);groups[activeBank].alpha=stageBlend;groups[1-activeBank].alpha=1-stageBlend;
            meshTimer-=dt;if(meshTimer<=0){meshTimer=1f/30;ApplyLighting();}
        }
        void ApplyLighting()
        {
            if(banks==null)return;
            AdventureEnvironmentCatalog.Palette(hour,out var skyColor,out var horizon,out var light);
            if(Profile.indoor)light=Color.Lerp(new Color(.63f,.7f,.8f),Color.white,.42f);
            float darkWeather=WeatherStrength(AdventureWeather.Rain)+WeatherStrength(AdventureWeather.Storm);light=Color.Lerp(light,new Color(.67f,.73f,.79f),darkWeather*.3f);
            for(int b=0;b<2;b++)for(int i=0;i<3;i++)
            {
                var im=banks[b][i];im.color=Color.Lerp(light,Color.Lerp(light,horizon,.24f),i==0?.7f:i==1?.22f:0);
                var uv=im.uvRect;uv.x=Mathf.Repeat(offset*speeds[i],2);im.uvRect=uv;
            }
            Dirty();
        }
        void Dirty(){if(sky!=null)sky.SetVerticesDirty();if(atmosphere!=null)atmosphere.SetVerticesDirty();}
        void OnDestroy(){if(layerMaterials==null)return;foreach(var bank in layerMaterials)foreach(var m in bank)if(m!=null){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
