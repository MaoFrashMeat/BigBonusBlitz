using System;
using System.IO;
using BBB.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bounded batch validation for the actual imported textures, mesh, shader and blend path.</summary>
public static class SaliaValidation
{
    [MenuItem("Tools/Big Bonus Blitz/Validate Salia Layers")]
    public static void Run()
    {
        var previous = EditorSceneManager.GetActiveScene();
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string output = Environment.GetEnvironmentVariable("SALIA_VALIDATION_OUTPUT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/salia-viewer/qa"));
        Directory.CreateDirectory(output);
        try
        {
            var cameraGo = new GameObject("SaliaValidationCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 470.5f;
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .09f, .13f, 0);
            var canvasGo = new GameObject("SaliaValidationCanvas", typeof(RectTransform), typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera; ((RectTransform)canvas.transform).sizeDelta = new Vector2(1672, 941);
            var model = SaliaTitleModel.Create(canvas.transform);
            if (model == null || model.LayerCount != 15) throw new Exception("Layer model did not load all 15 textures.");
            model.paused = true;
            UnityEngine.Object.DestroyImmediate(model.GetComponent<AspectRatioFitter>());
            var rt = (RectTransform)model.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            model.breath = model.hair = model.cloth = 0;
            var neutral = Capture(camera, model, output, "unity-neutral.png", 0, 0);
            var closed = Capture(camera, model, output, "unity-blink.png", 0, 1);
            model.breath = .65f; model.hair = .7f; model.cloth = .65f; model.motionRange = 1.35f;
            var moving = Capture(camera, model, output, "unity-motion.png", 1.25f, 0);
            long blinkDelta = Difference(neutral, closed), motionDelta = Difference(neutral, moving);
            int visible = 0; foreach (var p in neutral) if (p.a > 127) visible++;
            if (visible < 200000) throw new Exception("Character render is missing or mostly transparent.");
            if (blinkDelta < 10000 || motionDelta < 10000) throw new Exception("Eye or motion uniforms did not change the render.");
            // Exercise opposite wind phases at the highest exposed motion settings.
            model.breath = model.hair = model.cloth = 1.5f; model.motionRange = 2f;
            foreach (float time in new[] { .8f, 2.4f, 4.1f })
            {
                var stress = Capture(camera, model, output, "unity-seams-" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", time, 0);
                if (RegionDifference(neutral, stress, 475, 505, 225, 200) != 0 || RegionDifference(neutral, stress, 1220, 375, 90, 80) != 0)
                    throw new Exception("Pinned glove pixels moved under maximum motion.");
            }
            model.breath = model.cloth = 0; model.hair = 1; model.motionRange = 1.35f;
            var fringe = Capture(camera, model, output, "unity-fringe.png", 2.4f, 0);
            long fringeDelta = RegionDifference(neutral, fringe, 680, 120, 95, 70);
            if (fringeDelta < 10000) throw new Exception("Front fringe does not move with hair control.");
            var shader = Resources.Load<Shader>("SaliaRig/SaliaLayer");
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity.ToString() == "Error") throw new Exception(message.message);
            var report = "{\"passed\":true,\"layers\":15,\"visiblePixels\":" + visible + ",\"blinkDelta\":" + blinkDelta + ",\"motionDelta\":" + motionDelta + "}";
            File.WriteAllText(Path.Combine(output, "unity-validation.json"), report);
            Debug.Log("SALIA_VALIDATION " + report);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(output, "unity-validation-error.txt"), e.ToString());
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
        finally { if (!Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    private static Color32[] Capture(Camera camera, SaliaTitleModel model, string folder, string name, float time, float blink)
    {
        model.SetPose(time, blink, blink);
        Canvas.ForceUpdateCanvases();
        foreach (var graphic in model.GetComponentsInChildren<SaliaLayerGraphic>())
        {
            graphic.SetAllDirty();
            graphic.Rebuild(CanvasUpdate.PreRender);
            graphic.canvasRenderer.cull = false;
        }
        Canvas.ForceUpdateCanvases();
        var target = RenderTexture.GetTemporary(1672, 941, 24, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        var image = new Texture2D(1672, 941, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1672, 941), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally { camera.targetTexture = null; RenderTexture.active = old; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }

    private static long Difference(Color32[] a, Color32[] b)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++) sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) + Math.Abs(a[i].a - b[i].a);
        return sum;
    }
    private static long RegionDifference(Color32[] a, Color32[] b, int x, int top, int width, int height)
    {
        long sum = 0;
        for (int y = top; y < top + height; y++) for (int xx = x; xx < x + width; xx++)
        {
            int i = (940 - y) * 1672 + xx;
            sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) + Math.Abs(a[i].a - b[i].a);
        }
        return sum;
    }
}
