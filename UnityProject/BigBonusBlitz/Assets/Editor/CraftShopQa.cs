using System;
using System.IO;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 工房（ショップの 5 つ目のタブ）を本物の uGUI で描いて PNG に出し、作成の取引を確かめる（docs/ui_rules.md 9）。分離した batchmode 専用。
///   CRAFT_QA_OUTPUT=&lt;出力先&gt; Unity.exe -batchmode -quit -projectPath &lt;分離コピー&gt; -executeMethod CraftShopQa.Run -logFile out.log
/// セーブは読み書きしない（SaveData.Save はショップの購入で呼ばれるが、分離コピーの Application.persistentDataPath に書く）。
/// </summary>
public static class CraftShopQa
{
    static string output; static Canvas canvas; static Camera camera; static RectTransform stage;
    static void Assert(bool v, string message) { if (!v) throw new Exception(message); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Isolated batch project only");
        output = Environment.GetEnvironmentVariable("CRAFT_QA_OUTPUT");
        Assert(!string.IsNullOrEmpty(output), "CRAFT_QA_OUTPUT required"); Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiFactory.EnsureEventSystem();
        canvas = UiFactory.CreateCanvas("CraftQa"); UnityEngine.Object.DestroyImmediate(canvas.GetComponent<CanvasScaler>());
        canvas.renderMode = RenderMode.WorldSpace; canvas.transform.position = Vector3.zero;
        stage = UiSkin.Rect(canvas.transform, "Stage", Vector2.zero, new Vector2(960, 540));
        camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 270;
        camera.backgroundColor = AtelierUi.Night; camera.clearFlags = CameraClearFlags.SolidColor; canvas.worldCamera = camera;
        var m = GameDataLoader.CreateMachine(new SystemRandom()); var audio = AudioManager.Create();
        m.Wallet.Souls = 1500; m.Wallet.Embers = 999; m.Credit = 12345;
        m.Wallet.AddMaterial("iron", 6); m.Wallet.AddMaterial("hide", 3); m.Wallet.AddMaterial("gem", 2);
        var root = ShopScreen.Build(stage, m, audio, () => { }, () => { });
        Click(root, "Category4");
        Render("craft-shop");
        // 段 I の最初の図面（麻の帽子: 魔石 2）を作る → ソウルと素材が減り、着いている
        var recipe = m.Config.craft.recipes.First(r => r.unlockChapter == 0 && CraftDirector.CanCraft(m.Config.craft, m.Wallet, m.Equip, r, m.ChaptersCleared));
        var product = root.GetComponentsInChildren<Button>().First(b => b.name.StartsWith("Product_") && b.GetComponentsInChildren<Text>().Any(t => t.text == recipe.name));
        product.onClick.Invoke(); Canvas.ForceUpdateCanvases();
        int souls = m.Wallet.Souls; int mat = m.Wallet.MaterialCount(recipe.materialIds[0]);
        Click(root, "Purchase"); Click(root, "Confirm");
        Assert(m.Wallet.Souls == souls - recipe.souls, "ソウルが減っていない");
        Assert(m.Wallet.MaterialCount(recipe.materialIds[0]) == mat - recipe.materialCounts[0], "素材が減っていない");
        Assert(CraftDirector.Owns(m.Equip, recipe.id), "作った品が持ち物に無い");
        Assert(m.Equip.IsWorn(m.Equip.Bag.Concat(m.Equip.Worn.Values).First(i => i.crafted)), "空き枠なのに着いていない");
        Assert(!Button(root, "Purchase").interactable, "作成済みなのにもう一度作れる");
        Render("craft-shop-made");
        UnityEngine.Object.DestroyImmediate(root);
        // 装備画面: 工房の品は売れず、鞄の数に入らない
        root = EquipScreen.Build(stage, m, audio, () => { }, () => { });
        Render("craft-equip");
        Assert(!Button(root, "Sell").interactable, "工房の品が売れる");
        Assert(root.GetComponentsInChildren<Text>().Any(t => t.text.Contains("鞄 0/")), "工房の品が鞄の数に入っている");
        File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"screens\":3,\"resolutions\":[\"1280x720\",\"2556x1179\"],\"transactions\":\"craft\"}");
        Debug.Log("CRAFT_QA passed");
    }

    static Button Button(GameObject root, string name) => root.GetComponentsInChildren<Button>().First(b => b.name == name);
    static void Click(GameObject root, string name) { var b = Button(root, name); Assert(b.interactable, "Disabled click: " + name); b.onClick.Invoke(); Canvas.ForceUpdateCanvases(); }

    static void Render(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2556, 1179) })
        {
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(540f * size.x / size.y, 540); Canvas.ForceUpdateCanvases();
            foreach (var t in canvas.GetComponentsInChildren<Text>()) t.font.RequestCharactersInTexture(t.text, t.fontSize, t.fontStyle);
            for (int pass = 0; pass < 3; pass++) { foreach (var g in canvas.GetComponentsInChildren<Graphic>()) { g.SetAllDirty(); g.Rebuild(CanvasUpdate.PreRender); g.canvasRenderer.cull = false; } Canvas.ForceUpdateCanvases(); }
            var target = RenderTexture.GetTemporary(size.x, size.y, 24); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); tex.Apply(); File.WriteAllBytes(Path.Combine(output, name + "-" + size.x + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
