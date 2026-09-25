using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.ParticleSystem;

// 火花の見本を batchmode で連番 PNG に描き出す。
// 層: あかり / 床あかり / 閃光 / 横の光条 / 火花（粒＋尾＋床で跳ねる） / 弾け / 床の散り / 残り火 / 煙
public static class SparkLab
{
    const int W = 1280, H = 720, FPS = 60;
    const float Duration = 2.8f, HitAt = 0.12f;
    static readonly Vector3 P = new Vector3(-2.3f, 0.06f, 0f);

    public static void Run()
    {
        try { Render(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void Render()
    {
        string outDir = Environment.GetEnvironmentVariable("SPARK_OUT");
        if (string.IsNullOrEmpty(outDir)) outDir = Path.GetFullPath("out");
        Directory.CreateDirectory(outDir);
        foreach (var f in Directory.GetFiles(outDir, "f_*.png")) File.Delete(f);
        bool bright = Environment.GetEnvironmentVariable("SPARK_BG") == "bright";

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var root = new GameObject("Lab").transform;

        // テクスチャ
        var texDot = Radial(64, r => Mathf.Exp(-r * r * 14f) * Smooth(1f, 0.7f, r));
        var texGlow = Radial(128, r => Mathf.Pow(1f - r, 2.2f) * (0.35f + 0.65f * Mathf.Exp(-r * r * 9f)));
        var texTrail = Tex(8, 32, (u, v) => { float y = v * 2f - 1f; return Mathf.Exp(-y * y * 6f) * Smooth(1f, 0.8f, Mathf.Abs(y)); });
        var texStreak = Tex(256, 32, (u, v) => { float x = u * 2f - 1f, y = v * 2f - 1f; return Mathf.Exp(-y * y * 30f) * Mathf.Pow(Mathf.Max(0, 1f - Mathf.Abs(x)), 2.5f); });
        var texSmoke = Smoke(128);

        var matDot = Mat("Lab/FxAdd", texDot, 5.5f);
        var matTrail = Mat("Lab/FxAdd", texTrail, 3.2f);
        var matFlash = Mat("Lab/FxAdd", texDot, 14f);
        var matStreak = Mat("Lab/FxAdd", texStreak, 2.6f);
        var matGlow = Mat("Lab/FxAdd", texGlow, 0.6f);
        var matSmoke = Mat("Lab/FxAlpha", texSmoke, 1f);

        // 舞台（暗い工場の床と壁）
        Color wallTop = bright ? new Color(0.85f, 0.86f, 0.9f) : new Color(0.010f, 0.012f, 0.018f);
        Color wallLow = bright ? new Color(0.75f, 0.75f, 0.78f) : new Color(0.028f, 0.026f, 0.026f);
        Color floorNear = bright ? new Color(0.6f, 0.6f, 0.62f) : new Color(0.035f, 0.033f, 0.032f);
        Color floorFar = bright ? new Color(0.7f, 0.7f, 0.72f) : new Color(0.018f, 0.018f, 0.02f);
        Quad("Wall", root, new Vector3(0, 7f, 8f), Quaternion.identity, new Vector3(40, 14, 1), Tex(4, 64, (u, v) => 1f, (u, v) => Color.Lerp(wallLow, wallTop, Mathf.Pow(v, 0.6f))));
        Quad("Floor", root, new Vector3(0, 0, 3f), Quaternion.Euler(90, 0, 0), new Vector3(40, 22, 1), Tex(4, 64, (u, v) => 1f, (u, v) => Color.Lerp(floorNear, floorFar, v)));
        var floorPlane = new GameObject("FloorPlane").transform; floorPlane.SetParent(root);

        var tops = new List<ParticleSystem>();

        // あかり（周囲を照らす）
        {
            var ps = NewPS("Glow", root, P + Vector3.up * 0.3f, matGlow, 11); tops.Add(ps);
            var m = ps.main; m.startLifetime = 0.55f; m.startSpeed = 0; m.startSize = 6.5f; m.startColor = new Color(1f, 0.5f, 0.18f);
            ps.emission.SetBursts(new[] { new Burst(0, 1) });
            ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (0.12f, 0.75f), (1f, 0f) }));
        }
        // 床あかり
        {
            var ps = NewPS("FloorGlow", root, P + Vector3.up * 0.02f, matGlow, 12); tops.Add(ps);
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            var m = ps.main; m.startLifetime = 0.7f; m.startSpeed = 0; m.startSize = 5f; m.startColor = new Color(1f, 0.45f, 0.15f);
            ps.emission.SetBursts(new[] { new Burst(0, 1) });
            ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (0.2f, 0.6f), (1f, 0f) }));
        }
        // 閃光（発光点）
        {
            var ps = NewPS("Flash", root, P + Vector3.up * 0.08f, matFlash, 13); tops.Add(ps);
            var m = ps.main; m.startLifetime = 0.12f; m.startSpeed = 0; m.startSize = 0.8f; m.startColor = new Color(1f, 0.95f, 0.85f);
            ps.emission.SetBursts(new[] { new Burst(0, 1) });
            ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, new Color(1f, 0.6f, 0.3f)) }, new[] { (0f, 1f), (1f, 0f) }));
            SizeLife(ps, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.35f)));
        }
        // 横の光条
        {
            var ps = NewPS("Streak", root, P + Vector3.up * 0.08f, matStreak, 14); tops.Add(ps);
            var m = ps.main; m.startLifetime = 0.09f; m.startSpeed = 0; m.startSize3D = true;
            m.startSizeX = 4.2f; m.startSizeY = 0.14f; m.startSizeZ = 1f; m.startColor = new Color(1f, 0.8f, 0.55f);
            ps.emission.SetBursts(new[] { new Burst(0, 1) });
            ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (1f, 0f) }));
        }

        // 火花（主役）と、少数の速い長い火花
        var dir = Quaternion.LookRotation(new Vector3(0.72f, 0.68f, 0.05f));
        tops.Add(Sparks("Sparks", root, dir, floorPlane, matDot, matTrail, 21, new MinMaxCurve(3f, 10.5f), new MinMaxCurve(0.9f, 2.0f),
            new MinMaxCurve(0.03f, 0.062f), 32f, new[] { new Burst(0f, 120), new Burst(0.03f, 50), new Burst(0.07f, 24) }, new MinMaxCurve(0.05f, 0.075f), 0.7f, 0.2f));
        tops.Add(Sparks("Hero", root, dir, floorPlane, matDot, matTrail, 22, new MinMaxCurve(10.5f, 14f), new MinMaxCurve(1.3f, 2.1f),
            new MinMaxCurve(0.06f, 0.085f), 16f, new[] { new Burst(0f, 10), new Burst(0.02f, 6) }, new MinMaxCurve(0.06f, 0.08f), 0.55f, 0.5f));

        // 残り火（ゆっくり漂って消える。余韻）
        {
            var ps = NewPS("Embers", root, P, matDot, 41); tops.Add(ps);
            ps.transform.rotation = dir;
            var m = ps.main; m.startLifetime = new MinMaxCurve(1.2f, 2.2f); m.startSpeed = new MinMaxCurve(0.6f, 3.2f);
            m.startSize = new MinMaxCurve(0.012f, 0.028f); m.gravityModifier = 0.12f; m.startColor = new Color(1f, 0.7f, 0.35f);
            ps.emission.SetBursts(new[] { new Burst(0.02f, 26) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55; sh.radius = 0.08f;
            Drag(ps, 2.2f);
            var n = ps.noise; n.enabled = true; n.strength = 0.35f; n.frequency = 1.2f; n.scrollSpeed = 0.4f; n.quality = ParticleSystemNoiseQuality.Medium;
            ColorLife(ps, Grad(new[] { (0f, new Color(1f, 0.8f, 0.45f)), (0.5f, new Color(1f, 0.4f, 0.1f)), (1f, new Color(0.6f, 0.1f, 0.02f)) },
                               new[] { (0f, 1f), (0.3f, 0.9f), (0.6f, 1f), (0.75f, 0.5f), (1f, 0f) }));
            Trail(ps, matTrail, new MinMaxCurve(0.04f, 0.06f));
        }
        // 煙（閃光に照らされて暖色から灰へ。上へ抜けながら広がる）
        {
            var ps = NewPS("Smoke", root, P + Vector3.up * 0.2f, matSmoke, 51); // 丸い塊に見えるので外す
            var m = ps.main; m.startLifetime = new MinMaxCurve(1.8f, 2.6f); m.startSpeed = new MinMaxCurve(0.5f, 1.1f);
            m.startSize = new MinMaxCurve(0.8f, 1.4f); m.startRotation = new MinMaxCurve(0, Mathf.PI * 2); m.gravityModifier = -0.06f;
            ps.emission.SetBursts(new[] { new Burst(0.02f, 9), new Burst(0.12f, 7) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30; sh.radius = 0.12f;
            ps.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, 1f, 0f));
            ColorLife(ps, Grad(new[] { (0f, new Color(1f, 0.62f, 0.35f)), (0.12f, new Color(0.5f, 0.49f, 0.52f)), (1f, new Color(0.42f, 0.42f, 0.47f)) },
                               new[] { (0f, 0f), (0.1f, bright ? 0.12f : 0.035f), (0.5f, bright ? 0.08f : 0.022f), (1f, 0f) }));
            SizeLife(ps, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 3.5f)));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new MinMaxCurve(-0.5f, 0.5f);
            var n = ps.noise; n.enabled = true; n.strength = 0.25f; n.frequency = 0.5f; n.scrollSpeed = 0.2f;
            Drag(ps, 0.5f);
        }

        // カメラ
        var cam = new GameObject("Cam").AddComponent<Camera>();
        cam.transform.position = new Vector3(0.3f, 2.8f, -9f); cam.transform.LookAt(new Vector3(0.3f, 1.35f, 0));
        cam.fieldOfView = 40; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.allowHDR = true; cam.allowMSAA = false; cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;

        var hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { antiAliasing = 4 };
        var ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = hdr;
        var bloom = new Material(Shader.Find("Hidden/LabBloom"));
        bloom.SetFloat("_Threshold", 1.0f); bloom.SetFloat("_Knee", 0.6f); bloom.SetFloat("_BloomIntensity", 0.55f); bloom.SetFloat("_Exposure", 1.0f);
        const int levels = 6;
        var mips = new RenderTexture[levels];
        for (int k = 0; k < levels; k++)
            mips[k] = new RenderTexture(Mathf.Max(1, W >> (k + 1)), Mathf.Max(1, H >> (k + 1)), 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Bilinear };
        var shot = new Texture2D(W, H, TextureFormat.RGB24, false);

        int frames = Mathf.RoundToInt(Duration * FPS);
        float dt = 1f / FPS;
        bool started = false;
        for (int i = 0; i < frames; i++)
        {
            float t = i * dt;
            if (t >= HitAt)
            {
                foreach (var ps in tops) ps.Simulate(started ? dt : dt * 0.5f, true, !started, false);
                started = true;
            }
            cam.Render();
            Graphics.Blit(hdr, mips[0], bloom, 0);
            for (int k = 1; k < levels; k++) Graphics.Blit(mips[k - 1], mips[k], bloom, 1);
            for (int k = levels - 1; k > 0; k--) Graphics.Blit(mips[k], mips[k - 1], bloom, 2);
            bloom.SetTexture("_BloomTex", mips[0]);
            Graphics.Blit(hdr, ldr, bloom, 3);
            RenderTexture.active = ldr;
            shot.ReadPixels(new Rect(0, 0, W, H), 0, 0); shot.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outDir, $"f_{i:D4}.png"), shot.EncodeToPNG());
        }
        Debug.Log($"SparkLab: {frames} frames -> {outDir}");
    }

    // ---- 部品 ----
    static ParticleSystem NewPS(string name, Transform parent, Vector3 pos, Material mat, uint seed)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.useAutoRandomSeed = false; ps.randomSeed = seed;
        var m = ps.main; m.playOnAwake = false; m.loop = false; m.duration = 3f; m.maxParticles = 3000;
        m.simulationSpace = ParticleSystemSimulationSpace.World; m.startSpeed = 0;
        var em = ps.emission; em.rateOverTime = 0;
        var sh = ps.shape; sh.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    // 火花の本体。尾・冷え・空気抵抗・床で跳ねる・飛行中の弾け・床の散り
    static ParticleSystem Sparks(string name, Transform root, Quaternion dir, Transform floor, Material dot, Material trail, uint seed,
        MinMaxCurve speed, MinMaxCurve life, MinMaxCurve size, float angle, Burst[] bursts, MinMaxCurve trailLife, float drag, float popChance)
    {
        var sp = NewPS(name, root, P, dot, seed); sp.transform.rotation = dir;
        var m = sp.main; m.startLifetime = life; m.startSpeed = speed; m.startSize = size; m.gravityModifier = 1.1f;
        m.startColor = new MinMaxGradient(Color.white, new Color(1f, 0.88f, 0.65f));
        sp.emission.SetBursts(bursts);
        var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = angle; sh.radius = 0.04f; sh.randomDirectionAmount = 0.12f;
        Cooling(sp);
        SizeLife(sp, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.5f)));
        Drag(sp, drag);
        Trail(sp, trail, trailLife);
        var col = sp.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes; col.SetPlane(0, floor);
        col.bounce = new MinMaxCurve(0.3f, 0.55f); col.dampen = new MinMaxCurve(0.15f, 0.35f); col.lifetimeLoss = 0.12f; col.radiusScale = 0.3f;

        var pop = NewPS(name + "Pop", sp.transform, P, dot, seed + 100);
        {
            var pm = pop.main; pm.startLifetime = new MinMaxCurve(0.08f, 0.24f); pm.startSpeed = new MinMaxCurve(1.5f, 4.5f);
            pm.startSize = new MinMaxCurve(0.015f, 0.032f); pm.gravityModifier = 0.6f; pm.startColor = Color.white;
            pop.emission.SetBursts(new[] { new Burst(0f, 3, 7) });
            var ps = pop.shape; ps.enabled = true; ps.shapeType = ParticleSystemShapeType.Sphere; ps.radius = 0.005f;
            ColorLife(pop, Grad(new[] { (0f, Color.white), (0.4f, new Color(1f, 0.8f, 0.4f)), (1f, new Color(1f, 0.35f, 0.08f)) }, new[] { (0f, 1f), (0.5f, 1f), (1f, 0f) }));
            Trail(pop, trail, new MinMaxCurve(0.25f, 0.4f));
        }
        var skid = NewPS(name + "Skid", sp.transform, P, dot, seed + 200);
        {
            var km = skid.main; km.startLifetime = new MinMaxCurve(0.12f, 0.35f); km.startSpeed = new MinMaxCurve(0.8f, 2.8f);
            km.startSize = new MinMaxCurve(0.012f, 0.025f); km.gravityModifier = 1f;
            skid.emission.SetBursts(new[] { new Burst(0f, 2, 4) });
            var ks = skid.shape; ks.enabled = true; ks.shapeType = ParticleSystemShapeType.Hemisphere; ks.radius = 0.005f;
            skid.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ColorLife(skid, Grad(new[] { (0f, new Color(1f, 0.85f, 0.5f)), (1f, new Color(1f, 0.3f, 0.06f)) }, new[] { (0f, 1f), (1f, 0f) }));
            Trail(skid, trail, new MinMaxCurve(0.3f, 0.45f));
        }
        var sub = sp.subEmitters; sub.enabled = true;
        sub.AddSubEmitter(pop, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritColor, popChance);
        sub.AddSubEmitter(skid, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritColor, 0.35f);
        return sp;
    }

    static void Cooling(ParticleSystem ps) => ColorLife(ps, Grad(
        new[] { (0f, Color.white), (0.1f, new Color(1f, 0.95f, 0.8f)), (0.35f, new Color(1f, 0.8f, 0.42f)), (0.6f, new Color(1f, 0.52f, 0.16f)), (0.82f, new Color(0.85f, 0.22f, 0.05f)), (1f, new Color(0.45f, 0.06f, 0.02f)) },
        new[] { (0f, 1f), (0.75f, 1f), (1f, 0f) }));

    static void Trail(ParticleSystem ps, Material mat, MinMaxCurve life)
    {
        var tr = ps.trails; tr.enabled = true; tr.mode = ParticleSystemTrailMode.PerParticle; tr.ratio = 1f;
        tr.lifetime = life; tr.minVertexDistance = 0.015f; tr.textureMode = ParticleSystemTrailTextureMode.Stretch;
        tr.worldSpace = true; tr.dieWithParticles = true; tr.sizeAffectsWidth = true; tr.inheritParticleColor = true;
        tr.widthOverTrail = new MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
        tr.colorOverTrail = new MinMaxGradient(Grad(new[] { (0f, Color.white), (1f, new Color(1f, 0.6f, 0.3f)) }, new[] { (0f, 1f), (1f, 0f) }));
        ps.GetComponent<ParticleSystemRenderer>().trailMaterial = mat;
    }

    static void Drag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1000f; lv.drag = drag;
        lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false;
    }

    static void ColorLife(ParticleSystem ps, Gradient g) { var c = ps.colorOverLifetime; c.enabled = true; c.color = new MinMaxGradient(g); }
    static void SizeLife(ParticleSystem ps, AnimationCurve curve) { var s = ps.sizeOverLifetime; s.enabled = true; s.size = new MinMaxCurve(1f, curve); }

    static Gradient Grad((float t, Color c)[] cols, (float t, float a)[] alphas)
    {
        var g = new Gradient();
        var ck = new GradientColorKey[cols.Length]; for (int i = 0; i < cols.Length; i++) ck[i] = new GradientColorKey(cols[i].c, cols[i].t);
        var ak = new GradientAlphaKey[alphas.Length]; for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i].a, alphas[i].t);
        g.SetKeys(ck, ak); return g;
    }

    static Material Mat(string shader, Texture tex, float intensity)
    {
        var s = Shader.Find(shader); if (s == null) throw new Exception("shader not found: " + shader);
        var m = new Material(s) { mainTexture = tex }; m.SetFloat("_Intensity", intensity); return m;
    }

    static void Quad(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Texture tex)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(pos, rot); go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Unlit/Texture")) { mainTexture = tex };
    }

    static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }

    static Texture2D Radial(int n, Func<float, float> alpha) => Tex(n, n, (u, v) =>
    {
        float x = u * 2f - 1f, y = v * 2f - 1f; float r = Mathf.Sqrt(x * x + y * y);
        return r >= 1f ? 0f : alpha(r);
    });

    static Texture2D Tex(int w, int h, Func<float, float, float> alpha, Func<float, float, Color> color = null)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                var c = color != null ? color(u, v) : Color.white;
                c.a = Mathf.Clamp01(alpha(u, v));
                px[y * w + x] = c;
            }
        t.SetPixels(px); t.Apply(false, false); return t;
    }

    static Texture2D Smoke(int n) => Tex(n, n, (u, v) =>
    {
        float x = u * 2f - 1f, y = v * 2f - 1f; float r = Mathf.Sqrt(x * x + y * y);
        float f = 0, amp = 0.5f, fr = 3f;
        for (int o = 0; o < 5; o++) { f += amp * Mathf.PerlinNoise(u * fr + 11.3f * o, v * fr + 7.1f * o); amp *= 0.5f; fr *= 2f; }
        float body = Smooth(1f, 0.25f, r);
        return Mathf.Clamp01(body * (f * 1.6f - 0.25f));
    });
}
