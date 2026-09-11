using UnityEditor;
using UnityEngine;

public sealed class MapUiTexturePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/UI/MapV2/")) return;
        var importer = (TextureImporter)assetImporter;
        bool frame = assetPath.Contains("panel_") || assetPath.Contains("btn_");
        importer.textureType = TextureImporterType.Default;
        importer.maxTextureSize = frame ? 1024 : 256;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = false;
    }
}
