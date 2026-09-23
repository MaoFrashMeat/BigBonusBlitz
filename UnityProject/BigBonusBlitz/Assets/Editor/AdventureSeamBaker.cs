using System;
using System.IO;
using BBB.Runtime;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes numerical seam coordinates; never changes the source illustration.</summary>
public static class AdventureSeamBaker
{
    const float Overlap = .22f;
    const float Feather = .012f;
    const string Folder = "Assets/Resources/Art/Adventure/Seams";

    [MenuItem("BBB/Adventure/Rebuild seamless joins")]
    public static void Bake()
    {
        Directory.CreateDirectory(Folder);
        foreach (var profile in AdventureEnvironmentCatalog.All.Values)
        {
            if (profile.UsesModules) continue;
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes("Assets/Resources/" + profile.atlas + ".png")))
                    throw new InvalidDataException("Cannot read atlas: " + profile.id);
                var pixels = source.GetPixels32();
                int width = source.width, height = source.height;
                var coordinates = new Color[height];
                int[] cuts = { 0, Mathf.RoundToInt((1-profile.middleEnd)*height), Mathf.RoundToInt((1-profile.farEnd)*height), height };
                for (int row = 0; row < 3; row++) Solve(pixels, width, cuts[row], cuts[row+1], coordinates);
                string path = Folder + "/" + profile.id + ".asset";
                var lookup = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                bool create = lookup == null;
                if (create) lookup = new Texture2D(1, height, TextureFormat.RGBA32, false, true);
                else if (lookup.height != height) lookup.Reinitialize(1, height);
                lookup.name = profile.id + " seam coordinates";
                lookup.wrapMode = TextureWrapMode.Clamp;
                lookup.filterMode = FilterMode.Bilinear;
                lookup.SetPixels(coordinates);lookup.Apply(false, false);
                if (create) AssetDatabase.CreateAsset(lookup, path);else EditorUtility.SetDirty(lookup);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("ADVENTURE_SEAM_BAKE complete");
    }

    static void Solve(Color32[] pixels, int width, int startY, int endY, Color[] coordinates)
    {
        // Leave room on both sides of the feather so the periodic boundary always samples
        // the same source texel, including its derivative. Avoid cutting opaque silhouettes.
        int shift = Mathf.RoundToInt(width*(1-Overlap));
        int margin = Mathf.CeilToInt(width*(Feather+.008f));
        int count = width-shift-2*margin;
        int height = endY-startY;
        var previous = new float[count];var current = new float[count];
        var parents = new short[height*count];
        for (int y=0; y<height; y++)
        {
            for (int x=0; x<count; x++)
            {
                int px=margin+x;
                float cost=0;
                // A local window evaluates the whole narrow blend, not just one lucky pixel.
                for(int sample=-8;sample<=8;sample+=4)
                    cost+=Difference(pixels[(startY+y)*width+px+sample],pixels[(startY+y)*width+px+shift+sample]);
                cost/=5;
                // A tiny centre preference stabilizes the route through fully transparent rows.
                cost+=.0002f*Mathf.Abs(x-count*.5f)/count;
                int best=x;float value=y==0?0:previous[x];
                if(y>0)for(int dx=-2;dx<=2;dx++)
                {
                    int candidate=x+dx;if(candidate<0||candidate>=count)continue;
                    float v=previous[candidate]+.0015f*Mathf.Abs(dx);
                    if(v<value){value=v;best=candidate;}
                }
                current[x]=cost+value;parents[y*count+x]=(short)best;
            }
            var swap=previous;previous=current;current=swap;
        }
        int cursor=0;for(int x=1;x<count;x++)if(previous[x]<previous[cursor])cursor=x;
        for(int y=height-1;y>=0;y--)
        {
            float position=(margin+cursor+.5f)/(width*Overlap);
            coordinates[startY+y]=new Color(position,position,position,1);
            cursor=parents[y*count+cursor];
        }
    }

    static float Difference(Color32 left, Color32 right)
    {
        float a=left.a/255f,b=right.a/255f;
        float r=(left.r*a-right.r*b)/255f,g=(left.g*a-right.g*b)/255f,bl=(left.b*a-right.b*b)/255f;
        return r*r+g*g+bl*bl+3*(a-b)*(a-b);
    }

    public static void BakeAndValidate(){Bake();AdventureEnvironmentValidation.Run();}
}
