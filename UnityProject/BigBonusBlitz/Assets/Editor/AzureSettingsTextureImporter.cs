using UnityEditor;
using UnityEngine;
public sealed class AzureSettingsTextureImporter : AssetPostprocessor
{
    public override int GetPostprocessOrder()=>200;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/UI/AzureSettings/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;
        t.sRGBTexture=true;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=4096;
        t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.CompressedHQ;
    }
}
