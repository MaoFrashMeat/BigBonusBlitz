using UnityEditor;
using UnityEngine;

/// <summary>Keep cropped layer pixels/alpha intact; GPU animation does not need CPU texture copies.</summary>
public sealed class SaliaTexturePostprocessor : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/SaliaRig/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.alphaSource = TextureImporterAlphaSource.FromInput;
        t.alphaIsTransparency = true;
        t.sRGBTexture = true;
        t.mipmapEnabled = false;
        t.isReadable = false;
        t.npotScale = TextureImporterNPOTScale.None;
        t.maxTextureSize = 2048;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
    }
}
