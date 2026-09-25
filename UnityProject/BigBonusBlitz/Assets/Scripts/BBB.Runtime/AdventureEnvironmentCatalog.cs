using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBB.Runtime
{
    public enum AdventureTime { Morning, Day, Evening, Night }
    public enum AdventureWeather { Clear, Rain, Storm, Snow, Fog, Embers, Drips, Spores, Cloudy }

    [Serializable] public sealed class AdventureEnvironmentLayer
    {
        public string texture;
        public float speed=.02f, phase, height=1, bottom;
    }

    [Serializable] public sealed class AdventureEnvironmentProfile
    {
        public string id, name, atlas;
        public bool indoor;
        public string weather="Clear";
        public float hour=10, intensity=.65f, haze=.15f;
        public Color accent=new Color(.85f,.67f,.3f,1);
        // Normalized top-down row boundaries of the original, unmodified illustration atlas.
        public float farEnd=1f/3, middleEnd=2f/3;
        // Optional full-canvas modules; legacy stages continue using their row atlas.
        public AdventureEnvironmentLayer[] layers;
        public Color ground=new Color(.18f,.23f,.16f,1), path=new Color(.46f,.43f,.28f,1);
        public float groundHeight=.24f;
        public bool UsesModules => layers!=null && layers.Length==3;
    }
    public static class AdventureEnvironmentCatalog
    {
        [Serializable] sealed class Data { public AdventureEnvironmentProfile[] stages; }
        static Dictionary<string,AdventureEnvironmentProfile> profiles;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetCache()=>profiles=null;
        public static IReadOnlyDictionary<string,AdventureEnvironmentProfile> All
        {
            get
            {
                if(profiles==null)
                {
                    profiles=new Dictionary<string,AdventureEnvironmentProfile>();
                    var asset=Resources.Load<TextAsset>("Data/adventure_environments");
                    if(asset!=null)foreach(var p in JsonUtility.FromJson<Data>(asset.text).stages)profiles[p.id]=p;
                }
                return profiles;
            }
        }
        public static AdventureEnvironmentProfile Find(string id)
        {
            if(id!=null && All.TryGetValue(id,out var p))return p;
            if(All.TryGetValue("A-1",out p))return p;
            return new AdventureEnvironmentProfile{id="A-1",name="草原の入口",atlas="Art/Adventure/A-1"};
        }
        public static AdventureWeather WeatherFor(AdventureEnvironmentProfile p,AdventureWeather requested)
        {
            if(p.indoor && requested==AdventureWeather.Cloudy)return AdventureWeather.Clear;
            if(p.indoor && (requested==AdventureWeather.Rain||requested==AdventureWeather.Storm||requested==AdventureWeather.Snow))return AdventureWeather.Drips;
            return requested;
        }
        static readonly float[] hours={0,5,7,11,16,18.5f,21,24};
        static readonly Color[] tops={C("#080f2f"),C("#38324f"),C("#829eb9"),C("#438bb5"),C("#609dc1"),C("#514065"),C("#101735"),C("#080f2f")};
        static readonly Color[] bottoms={C("#263d5a"),C("#d89480"),C("#f1d6a4"),C("#d3e7df"),C("#e6dcc1"),C("#f6a16b"),C("#354868"),C("#263d5a")};
        static readonly Color[] lights={C("#687da5"),C("#c7958d"),C("#fff0cf"),Color.white,C("#fff2da"),C("#e9a78b"),C("#7584ab"),C("#687da5")};
        public static void Palette(float hour,out Color top,out Color bottom,out Color light)
        {
            hour=Mathf.Repeat(hour,24);
            int i=0;while(i<hours.Length-2&&hour>hours[i+1])i++;
            float t=Mathf.InverseLerp(hours[i],hours[i+1],hour);t=t*t*(3-2*t);
            top=Color.Lerp(tops[i],tops[i+1],t);bottom=Color.Lerp(bottoms[i],bottoms[i+1],t);light=Color.Lerp(lights[i],lights[i+1],t);
        }
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex,out var c);return c; }
    }
}
