using UnityEditor;
using UnityEngine;
public sealed class HeroPrototypeImporter : AssetPostprocessor
{
    public override int GetPostprocessOrder()=>210;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/HeroPrototype/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.spriteImportMode=SpriteImportMode.None;
        t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=4096;t.mipmapEnabled=false;t.sRGBTexture=true;t.alphaIsTransparency=true;
        t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;
    }
}
