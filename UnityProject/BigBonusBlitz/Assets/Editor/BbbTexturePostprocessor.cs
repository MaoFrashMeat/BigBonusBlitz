using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/Art 配下の png を Sprite として取り込む（手動設定不要にするため）。
/// 背景レイヤーは横スクロール用に Repeat、最大サイズは 8192（キャラのストリップが 5504px）。
/// </summary>
public sealed class BbbTexturePostprocessor : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Resources/Art/";

    // 版を上げると該当アセットが再インポートされる（既に Default で取り込まれた png を Sprite に直すため）
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot)) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 8192;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = 100;
        imp.wrapMode = assetPath.Contains("/Backgrounds/") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
    }
}
