using UnityEditor;
using UnityEngine;
public sealed class AdventureDialogueTextureImporter : AssetPostprocessor
{
    public override int GetPostprocessOrder()=>200;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/UI/AdventureDialogue/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.npotScale=TextureImporterNPOTScale.None;
        t.sRGBTexture=true;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;
        t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.CompressedHQ;
    }
}
