using System.Linq;
using BBB.Runtime;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParallaxBackground))]
public sealed class AdventureEnvironmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();var bg=(ParallaxBackground)target;
        if(bg.Profile==null){EditorGUILayout.HelpBox("Play中のCharacterAreaまたはBgOuterで環境をプレビューできます。",MessageType.Info);return;}
        EditorGUILayout.Space();EditorGUILayout.LabelField("背景・時間・天気のライブプレビュー",EditorStyles.boldLabel);
        var stages=AdventureEnvironmentCatalog.All.Values.ToArray();int current=System.Array.FindIndex(stages,p=>p.id==bg.CurrentStageId);
        int next=EditorGUILayout.Popup("ステージ",Mathf.Max(0,current),stages.Select(p=>p.id+" "+p.name).ToArray());
        if(next!=current)bg.SetStage(stages[next].id,!Application.isPlaying);
        EditorGUILayout.BeginHorizontal();foreach(AdventureTime t in System.Enum.GetValues(typeof(AdventureTime)))if(GUILayout.Button(t.ToString()))bg.SetTimeOfDay(t,Application.isPlaying?2:0);EditorGUILayout.EndHorizontal();
        var weather=(AdventureWeather)EditorGUILayout.EnumPopup("天気",bg.CurrentWeather);if(weather!=bg.CurrentWeather)bg.SetWeather(weather,1,Application.isPlaying?2:0);
        if(GUILayout.Button("時間の自動進行を再開"))bg.AutoCycle=true;
        EditorGUILayout.HelpBox("地下では雨・雷雨・雪を水滴に変換。背景は3枚の透過イラスト＋空・天体・雲・霧・天候の多層構成です。",MessageType.Info);
    }
}

public sealed class AdventureEnvironmentTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 3;
    public override int GetPostprocessOrder() => 100;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/Adventure/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.npotScale=TextureImporterNPOTScale.None;t.sRGBTexture=true;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;t.wrapModeU=TextureWrapMode.Mirror;t.wrapModeV=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.CompressedHQ;
        if(assetPath.Contains("/Modules/"))
        {
            // Preserve the matte and painted edges; compression can introduce key-colored fringes.
            t.maxTextureSize=4096;t.wrapModeU=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
