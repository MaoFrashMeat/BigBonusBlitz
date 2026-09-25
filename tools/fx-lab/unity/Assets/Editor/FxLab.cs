using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.ParticleSystem;

// エフェクト見本を batchmode で連番 PNG に描き出す。1 クリップ = 1 フォルダ。
// LAB_OUT: 出力先  LAB_ART: 背景とキャラの PNG  LAB_ONLY: "slash1,coins" のように絞る
public static class FxLab
{
    const int W = 1280, H = 720, FPS = 60;
    const float GroundY = -2.3f;
    static readonly Vector3 HeroHome = new Vector3(-3.2f, -0.84f, 0f);
    static readonly Vector3 GoblinHome = new Vector3(3.3f, -1.07f, 0f);
    static readonly Vector3 G = new Vector3(3.3f, -0.95f, -1f);

    public static void Run()
    {
        try
        {
            string only = Environment.GetEnvironmentVariable("LAB_ONLY");
            var names = string.IsNullOrEmpty(only) ? null : only.Split(',');
            foreach (var c in Clips())
                if (names == null || names.Contains(c.name)) RenderClip(c);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    // ================= クリップ定義 =================
    class Clip { public string name; public float dur; public int hold = 1; public Action<Ctx> build; }

    static IEnumerable<Clip> Clips()
    {
        yield return new Clip { name = "slash1", dur = 1.4f, hold = 2, build = Slash1 };
        yield return new Clip { name = "slash2", dur = 1.4f, hold = 2, build = Slash2 };
        yield return new Clip { name = "slash3", dur = 1.5f, hold = 2, build = Slash3 };
        yield return new Clip { name = "slash4", dur = 1.5f, hold = 2, build = Slash4 };
        yield return new Clip { name = "slash5", dur = 1.9f, hold = 2, build = Slash5 };
        yield return new Clip { name = "shake", dur = 2.4f, hold = 2, build = ShakeClip };
        yield return new Clip { name = "lines", dur = 3.0f, hold = 3, build = LinesClip };
        yield return new Clip { name = "nega", dur = 2.4f, hold = 2, build = NegaClip };
        yield return new Clip { name = "coins", dur = 3.2f, hold = 1, build = CoinsClip };
        yield return new Clip { name = "heal", dur = 3.2f, hold = 1, build = HealClip };
        yield return new Clip { name = "attack", dur = 3.2f, hold = 1, build = AttackClip };
        yield return new Clip { name = "shield", dur = 3.2f, hold = 1, build = ShieldClip };
    }

    // 3 段（docs/FX_RESEARCH.md 2・3）: 暗い縁（背景から切り離す）／飽和した本体（1 未満で光らせない）／細い白芯（ここだけ HDR で光る）
    static readonly Style Steel = new Style(new Color(0.03f, 0.07f, 0.28f, 0.9f), new Color(0.3f, 0.62f, 1f, 0.85f), Color.white * 2.2f);
    static readonly Style Cyan = new Style(new Color(0.0f, 0.1f, 0.28f, 0.9f), new Color(0.12f, 0.82f, 1f, 0.85f), Color.white * 2.2f);
    static readonly Style Gold = new Style(new Color(0.32f, 0.08f, 0.0f, 0.9f), new Color(1f, 0.68f, 0.1f, 0.85f), new Color(1f, 0.96f, 0.82f) * 2.2f);
    static readonly Style Flame = new Style(new Color(0.28f, 0.02f, 0.0f, 0.9f), new Color(1f, 0.38f, 0.04f, 0.85f), new Color(1f, 0.92f, 0.68f) * 2.2f);
    static readonly Style Crimson = new Style(new Color(0.16f, 0.0f, 0.28f, 0.9f), new Color(1f, 0.18f, 0.52f, 0.85f), Color.white * 2.2f);

    // 1. 一文字: 横一線。平たい弧＋残像＋横の斬線
    static void Slash1(Ctx c)
    {
        Glint(c, HeroHome + new Vector3(0.7f, 0.25f, -1f), 0.24f, Steel.mid);
        SlashThrough(c, G, 0, 70, 2.5f, 1.1f, 150, Steel, 0.3f);
        SlashThrough(c, G + new Vector3(0, -0.14f, 0), 0, 70, 2.5f, 0.6f, 150, Steel.Ghost(), 0.33f, thick: 0.8f);
        Hit(c, G, 0.36f, Steel.mid, 1f, 0, 50, 26, 11);
        CutLine(c, G, 0, 5f, 0.36f, Steel.mid);
        Impact(c, 0.36f, 1, 0);
    }

    // 2. 袈裟斬り: 左上から右下。実写寄りの火花を混ぜる
    static void Slash2(Ctx c)
    {
        Glint(c, HeroHome + new Vector3(0.5f, 0.9f, -1f), 0.24f, Cyan.mid);
        SlashThrough(c, G, -45, 25, 2.3f, 1.0f, 160, Cyan, 0.3f);
        SlashThrough(c, G + new Vector3(0.1f, 0.1f, 0), -45, 25, 2.3f, 0.7f, 160, Cyan.Ghost(), 0.333f, fade: 0.45f, thick: 0.85f);   // かすれの層: 2F 遅れ、1.5 倍長く残る
        Hit(c, G, 0.36f, Cyan.mid, 1.1f, -45, 60, 28, 12);
        RealSparks(c, G, -35, 0.36f, 60, 11f, 13);
        CutLine(c, G, 45, 4.2f, 0.36f, Cyan.mid);
        Impact(c, 0.36f, 1, -45);
    }

    // 3. 十字斬り: ↘ のあと ↙。交点で大きく光る
    static void Slash3(Ctx c)
    {
        SlashThrough(c, G, -45, 20, 2.2f, 0.95f, 150, Gold, 0.25f);
        SlashThrough(c, G, -135, 20, 2.2f, 0.95f, 150, Gold, 0.4f);
        SlashThrough(c, G + new Vector3(-0.1f, 0.1f, 0), -135, 20, 2.2f, 0.7f, 150, Gold.Ghost(), 0.433f, fade: 0.45f, thick: 0.85f);
        Hit(c, G, 0.3f, Gold.mid, 0.7f, -45, 50, 14, 21);
        Hit(c, G, 0.46f, Gold.mid, 1.5f, 0, 360, 36, 22);
        CutLine(c, G, 45, 4.5f, 0.46f, Gold.mid);
        CutLine(c, G, -45, 4.5f, 0.46f, Gold.mid);
        Impact(c, 0.3f, 0, -45);
        Impact(c, 0.46f, 2, 0);
    }

    // 4. 回転斬り: 主人公の周りを一周。後ろ半分はキャラに隠れる
    static void Slash4(Ctx c)
    {
        var center = HeroHome + new Vector3(0, -0.35f, 0);
        var slash = MakeArc(c, center, Quaternion.Euler(74, 0, 6), Vector3.zero, 2.2f, 1.0f, -80, 330, Flame);
        Animate(c, slash, 0.3f, 0.24f, 0.4f, 0.62f);
        var ring = PS(c, "GroundRing", new Vector3(center.x, GroundY + 0.15f, -0.5f), AddMat(c.tx.ring, 2f), 41);
        {
            var m = ring.main; m.startLifetime = 0.4f; m.startSize3D = true; m.startSizeX = 6f; m.startSizeY = 1.3f; m.startSizeZ = 1;
            m.startColor = Flame.mid; ring.emission.SetBursts(new[] { new Burst(0, 1) });
            SizeLife(ring, Curve((0, 0.2f), (0.3f, 0.8f), (1, 1f)));
            ColorLife(ring, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (1f, 0f) }));
            c.Play(ring, 0.5f);
        }
        Shards(c, center, 0.5f, Flame.mid, 1.2f, 0, 360, 34, 42);
        Glow(c, center, 0.5f, Flame.mid, 3f, 43);
        Impact(c, 0.5f, 0, 0, enemy: false);
    }

    // 5. 乱舞: 細い斬撃を6本、最後に大きな一撃
    static void Slash5(Ctx c)
    {
        var rnd = new System.Random(7);
        float t = 0.25f;
        for (int k = 0; k < 6; k++)
        {
            float rot = (float)rnd.NextDouble() * 360f;
            var off = new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, 0) * 0.8f;
            SlashThrough(c, G + off, rot, (float)rnd.NextDouble() * 50 - 25, 1.7f, 0.55f, 110, Crimson, t, dur: 0.05f, fade: 0.14f, len: 0.9f, thick: 0.85f);
            Shards(c, G + off, t + 0.03f, Crimson.mid, 0.6f, rot, 40, 8, (uint)(60 + k));
            Impact(c, t + 0.03f, -1, rot);
            t += 0.085f;
        }
        float fin = t + 0.12f;
        SlashThrough(c, G, 10, 55, 2.8f, 1.15f, 160, Crimson, fin, dur: 0.1f, fade: 0.35f);
        Hit(c, G, fin + 0.06f, Crimson.mid, 1.6f, 10, 360, 40, 71);
        CutLine(c, G, -10, 6f, fin + 0.06f, Crimson.mid);
        Impact(c, fin + 0.06f, 2, 10);
    }

    // 6. 画面の揺れ: 弱い一撃と強い一撃
    static void ShakeClip(Ctx c)
    {
        SlashThrough(c, G, 0, 70, 2.2f, 0.9f, 140, Steel, 0.3f);
        Hit(c, G, 0.35f, Steel.mid, 0.7f, 0, 50, 12, 81);
        Impact(c, 0.35f, 0, 0);

        SlashThrough(c, G, -45, 25, 2.5f, 1.05f, 160, Cyan, 1.15f);
        Hit(c, G, 1.21f, Cyan.mid, 1.4f, -45, 70, 34, 82);
        RealSparks(c, G, -35, 1.21f, 70, 12f, 83);
        Impact(c, 1.21f, 2, -45);
    }

    // 7. 流線: 前半は平行の流線（背景は横ブラー）、後半は敵への集中線
    static void LinesClip(Ctx c)
    {
        var gUv = WorldToUv(G);
        c.OnUpdate(t =>
        {
            int step = Mathf.RoundToInt(t * 20);
            if (t < 1.45f)
            {
                c.post.lines = 0.95f; c.post.linesMode = 0; c.post.linesSeed = step; c.post.linesDensity = 0.5f;
                c.post.blur = 0.6f; c.post.blurDir = new Vector2(0.035f, 0); c.post.darken = 0.3f;
            }
            else
            {
                float k = Mathf.Clamp01((t - 1.5f) / 0.3f);
                c.post.lines = 0.95f; c.post.linesMode = 1; c.post.linesSeed = step; c.post.linesDensity = 0.5f;
                c.post.linesCenter = gUv; c.post.darken = 0.5f;
                c.post.zoom = 1f + 0.12f * EaseOut(k); c.post.zoomCenter = gUv;
                if (t < 1.55f) c.post.flash = 0.5f;
            }
        });
    }

    // 8. ネガ反転: 短く点滅する型と、白黒で溜めてから戻る型
    static void NegaClip(Ctx c)
    {
        SlashThrough(c, G, -45, 25, 2.3f, 1.0f, 160, Cyan, 0.3f);
        Hit(c, G, 0.36f, Cyan.mid, 0.8f, -45, 60, 20, 91);
        HitFlash(c, c.goblinT, c.goblin, 0.36f);
        SlashThrough(c, G, 0, 70, 2.6f, 1.0f, 160, Crimson, 1.3f);
        Hit(c, G, 1.36f, Crimson.mid, 1.0f, 0, 360, 28, 92);
        HitFlash(c, c.goblinT, c.goblin, 1.36f);
        c.OnUpdate(t =>
        {
            float a = t - 0.36f;
            if (a >= 0 && a < 0.2f)
            {
                int f = Mathf.FloorToInt(a * 30f + 0.001f);   // 30fps の 1 コマ単位
                if (f == 0 || f == 1) c.post.invert = 1;
                else if (f == 3) { c.post.invert = 1; c.post.mono = 1; }
                else if (f == 4) c.post.flash = 0.8f;
            }
            float b = t - 1.36f;
            if (b >= 0 && b < 0.34f) { c.post.invert = 1; c.post.mono = 1; }
            else if (b >= 0.34f && b < 0.4f) c.post.flash = 0.7f;
            else if (b >= 0.4f && b < 0.8f) c.post.mono = 1 - (b - 0.4f) / 0.4f;
        });
    }

    // 9. コインが飛び散る: 敵が消えて金貨が噴き上がり、地面で跳ねる
    static void CoinsClip(Ctx c)
    {
        float t0 = 0.3f;
        Impact(c, t0 - 0.05f, 1, 0);
        c.OnUpdate(t => c.goblinT.gameObject.SetActive(t < t0));
        var gold = new Color(1f, 0.75f, 0.3f);
        Glow(c, G, t0, gold, 2.2f, 101);
        Flare(c, G, t0, gold, 3f, 102);
        Ring(c, G, t0, gold, 3.5f, 103, delay: 0.03f);
        Bokeh(c, G, t0, gold, 1.5f, 106);

        var coins = PS(c, "Coins", G, new Material(Shader.Find("Lab/Coin")) { mainTexture = c.tx.coinFace }, 104);
        coins.transform.rotation = Quaternion.LookRotation(new Vector3(-0.3f, 1f, 0f));
        {
            var m = coins.main; m.startLifetime = new MinMaxCurve(2.6f, 3.2f); m.startSpeed = new MinMaxCurve(6.5f, 11f);
            m.startSize = new MinMaxCurve(0.3f, 0.42f); m.gravityModifier = 1.5f;
            m.startRotation3D = true; m.startRotationX = new MinMaxCurve(0, 6.28f); m.startRotationY = new MinMaxCurve(0, 6.28f); m.startRotationZ = new MinMaxCurve(0, 6.28f);
            coins.emission.SetBursts(new[] { new Burst(0f, 18), new Burst(0.08f, 14), new Burst(0.16f, 12), new Burst(0.3f, 10), new Burst(0.45f, 6) });
            var sh = coins.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30; sh.radius = 0.25f;
            var rot = coins.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = new MinMaxCurve(7f, 16f); rot.y = new MinMaxCurve(1.5f, 5f); rot.z = new MinMaxCurve(-2f, 2f);
            var col = coins.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes; col.SetPlane(0, Plane(c, GroundY));
            col.bounce = new MinMaxCurve(0.3f, 0.5f); col.dampen = new MinMaxCurve(0.15f, 0.3f); col.radiusScale = 0.4f;
            Drag(coins, 0.3f);
            var r = coins.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = CoinMesh(); r.alignment = ParticleSystemRenderSpace.World; r.enableGPUInstancing = false;
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            // きらめき（コインから時々出る）
            var tw = PS(c, "Twinkle", G, AddMat(c.tx.star4, 6f), 105, coins.transform);
            var tm = tw.main; tm.startLifetime = new MinMaxCurve(0.14f, 0.26f); tm.startSize = new MinMaxCurve(0.25f, 0.45f); tm.startRotation = new MinMaxCurve(-0.3f, 0.3f);
            tm.startColor = new Color(1f, 0.92f, 0.7f);
            var te = tw.emission; te.rateOverTime = 2.5f;
            SizeLife(tw, Curve((0, 0), (0.3f, 1), (1, 0)));
            var sub = coins.subEmitters; sub.enabled = true; sub.AddSubEmitter(tw, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
            c.Play(coins, t0);
        }
    }

    // 10. 回復のオーラ: 光の柱・足元の輪・立ちのぼる粒・螺旋・十字のきらめき・シルエットの淡い光
    static void HealClip(Ctx c)
    {
        var hero = HeroHome + new Vector3(2.0f, 0, 0); c.heroT.position = hero;
        var feet = new Vector3(hero.x, GroundY + 0.1f, 0);
        var green = new Color(0.35f, 1f, 0.45f); var lime = new Color(0.75f, 1f, 0.45f);
        float on = 0.2f, off = 2.7f;
        Func<float, float> env = t => Mathf.Clamp01((t - on) / 0.08f) * (1 - Mathf.Clamp01((t - off) / 0.5f));   // 立ち上がりは 5F（発動の山と重ねる）

        Surge(c, on, hero, feet, green);
        if (c.tx.magic != null)
        {
            // 足元の魔法陣: 寝かせて回す。発動で強く光り、持続は控えめ
            var mc = Quad(c.root, "MagicCircle", feet + new Vector3(0, 0.02f, 0.2f), new Vector2(3.4f, 3.4f), QMat(c, c.tx.magic, 0, Vector2.one, 0));
            var mm = mc.GetComponent<MeshRenderer>().sharedMaterial;
            c.OnUpdate(t =>
            {
                float e = env(t), burst = t < on ? 0 : Mathf.Exp(-(t - on) * 7f);
                mc.rotation = Quaternion.Euler(72, 0, 0) * Quaternion.Euler(0, 0, t * 40f);
                mm.SetColor("_Tint", green * (e * (0.8f + 1.6f * burst)));
            });
        }
        var colQuad = Quad(c.root, "Column", feet + new Vector3(0, 2.1f, 0.25f), new Vector2(2.6f, 4.6f), QMat(c, c.tx.column, 0.9f, new Vector2(3, 1.2f), 0.5f));
        var colMat = colQuad.GetComponent<MeshRenderer>().sharedMaterial;
        var sil = Quad(c.root, "Sil", hero + new Vector3(0, 0, -0.03f), SpriteSize(c.hero, 3.3f), QMat(c, c.hero, 0, Vector2.one, 0));
        var silMat = sil.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t =>
        {
            float e = env(t);
            float burst = t < on ? 0 : Mathf.Exp(-(t - on) * 7f);   // 発動の山 → 持続は控えめ
            colMat.SetColor("_Tint", new Color(0.55f, 1f, 0.6f) * (e * (0.9f + 2.4f * burst)));
            silMat.SetColor("_Tint", new Color(0.6f, 1f, 0.6f) * (e * (0.35f + 0.15f * Mathf.Sin(t * 7f))));
        });

        var ring = PS(c, "Ring", feet + new Vector3(0, 0.05f, -0.6f), AddMat(c.tx.ring, 3.5f), 111);
        {
            var m = ring.main; m.startLifetime = 0.9f; m.startSize3D = true; m.startSizeX = 3.4f; m.startSizeY = 0.8f; m.startSizeZ = 1; m.startColor = green;
            ring.emission.SetBursts(new[] { new Burst(0, 1, 3, 0.7f) });
            SizeLife(ring, Curve((0, 0.3f), (0.4f, 0.85f), (1, 1.05f)));
            ColorLife(ring, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.1f, 1f), (1f, 0f) }));
            c.Play(ring, on);
        }
        var motes = PS(c, "Motes", feet, AddMat(c.tx.dot, 6f), 112);
        {
            var m = motes.main; m.duration = off - on; m.startLifetime = new MinMaxCurve(1.1f, 1.9f); m.startSpeed = 0;
            m.startSize = new MinMaxCurve(0.07f, 0.16f); m.startColor = new MinMaxGradient(lime, new Color(1f, 1f, 0.8f));
            var em = motes.emission; em.rateOverTime = 60;
            var sh = motes.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.0f; sh.rotation = new Vector3(90, 0, 0);
            var v = motes.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new MinMaxCurve(0f, 0f); v.y = new MinMaxCurve(0.6f, 1.6f); v.z = new MinMaxCurve(0f, 0f);
            var n = motes.noise; n.enabled = true; n.strength = 0.35f; n.frequency = 0.9f; n.scrollSpeed = 0.5f;
            ColorLife(motes, Grad(new[] { (0f, Color.white), (1f, green) }, new[] { (0f, 0f), (0.12f, 1f), (0.5f, 0.55f), (0.68f, 1f), (1f, 0f) }));
            c.Play(motes, on);
        }
        var spiral = PS(c, "Spiral", feet, AddMat(c.tx.dot, 7f), 113);
        {
            var m = spiral.main; m.duration = off - on - 0.3f; m.simulationSpace = ParticleSystemSimulationSpace.Local;
            m.startLifetime = 1.3f; m.startSpeed = 0; m.startSize = 0.16f; m.startColor = lime;
            var em = spiral.emission; em.rateOverTime = 2.4f;
            var sh = spiral.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.1f; sh.radiusThickness = 0; sh.rotation = new Vector3(90, 0, 0);
            var v = spiral.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
            v.x = new MinMaxCurve(0f, 0f); v.y = new MinMaxCurve(1.9f, 1.9f); v.z = new MinMaxCurve(0f, 0f);
            v.orbitalX = 0; v.orbitalY = 3.2f; v.orbitalZ = 0; v.radial = -0.35f;
            ColorLife(spiral, Grad(new[] { (0f, Color.white), (1f, green) }, new[] { (0f, 0f), (0.1f, 1f), (0.8f, 1f), (1f, 0f) }));
            Trail(spiral, AddMat(c.tx.trail, 4.5f), new MinMaxCurve(0.45f, 0.45f), worldSpace: false);
            c.Play(spiral, on + 0.1f);
        }
        var cross = PS(c, "Cross", hero + new Vector3(0, 0.2f, -0.5f), AddMat(c.tx.plus, 5f), 114);
        {
            var m = cross.main; m.duration = off - on; m.startLifetime = 0.6f; m.startSpeed = 0.3f; m.startSize = new MinMaxCurve(0.26f, 0.46f);
            m.startColor = new MinMaxGradient(new Color(0.7f, 1f, 0.7f), new Color(1f, 1f, 0.85f));
            var em = cross.emission; em.rateOverTime = 7;
            var sh = cross.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.2f, 0.2f, 3.0f);
            sh.rotation = new Vector3(-90, 0, 0);
            SizeLife(cross, Curve((0, 0), (0.25f, 1), (1, 0.3f)));
            ColorLife(cross, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.2f, 1f), (1f, 0f) }));
            c.Play(cross, on + 0.2f);
        }
    }

    // 11. 攻撃のオーラ: シルエットから立つ炎・体から昇る炎の粒・火の粉・足元の赤いあかり
    static void AttackClip(Ctx c)
    {
        var hero = HeroHome + new Vector3(2.0f, 0, 0); c.heroT.position = hero;
        var feet = new Vector3(hero.x, GroundY + 0.1f, 0);
        float on = 0.2f;
        var size = SpriteSize(c.hero, 3.3f);
        var auraMat = new Material(Shader.Find("Lab/AuraFlame"));
        auraMat.SetTexture("_CharTex", c.hero); auraMat.SetTexture("_NoiseTex", c.tx.noise); auraMat.SetFloat("_Scale", 1.5f); auraMat.SetFloat("_Rise", 0.22f);
        auraMat.SetColor("_ColCore", new Color(1f, 0.7f, 0.25f) * 1.4f); auraMat.SetColor("_ColMid", new Color(1f, 0.22f, 0.03f) * 1.1f); auraMat.SetColor("_ColEdge", new Color(0.5f, 0.02f, 0.01f));
        Quad(c.root, "Aura", hero + new Vector3(0, 0, 0.3f), size * 1.5f, auraMat);
        var glowQ = Quad(c.root, "FloorGlow", feet + new Vector3(0, 0.05f, 0.35f), new Vector2(5f, 1.4f), QMat(c, c.tx.glow, 0, Vector2.one, 0));
        var glowMat = glowQ.GetComponent<MeshRenderer>().sharedMaterial;
        var sil = Quad(c.root, "Sil", hero + new Vector3(0, 0, -0.03f), size, QMat(c, c.hero, 0, Vector2.one, 0));
        var silMat = sil.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t =>
        {
            float e = Mathf.Clamp01((t - on) / 0.08f);
            float flick = 0.8f + 0.4f * Mathf.PerlinNoise(t * 9f, 0.3f);
            float burst = t < on ? 0 : Mathf.Exp(-(t - on) * 6f);
            auraMat.SetFloat("_Intensity", e * (0.75f + 0.2f * flick + 0.8f * burst));
            glowMat.SetColor("_Tint", new Color(1f, 0.25f, 0.05f) * (0.9f * e * flick));
            silMat.SetColor("_Tint", new Color(1f, 0.15f, 0.03f) * (0.12f * e * flick));
        });
        // 溜め（予備）→ 発動の瞬間
        if (c.tx.fbCharge != null) Flip(c, "Charge", hero + new Vector3(0, 0.1f, -0.7f), c.tx.fbCharge, 7, 6, 0, 11, 0.35f, 3.6f, new Color(1f, 0.55f, 0.2f), 2.2f, 125, t: on - 0.35f, randomRot: false);
        Surge(c, on, hero, feet, new Color(1f, 0.35f, 0.06f), 1.2f);
        if (c.tx.fbFireRing != null) Flip(c, "FireRing", feet + new Vector3(0, 0.3f, -0.6f), c.tx.fbFireRing, 6, 5, 0, 30, 0.5f, 4.2f, Color.white, 1.4f, 126, t: on, randomRot: false);
        if (c.tx.fbFlame != null)
        {
            // 炎の連番（64 コマ）を体の周りに並べて、それぞれ違うコマから流す
            var fl = PS(c, "FlameSheet", hero + new Vector3(0, -0.2f, 0.4f), AddMat(c.tx.fbFlame, 1.1f), 127);
            var fm = fl.main; fm.duration = 3f; fm.startLifetime = new MinMaxCurve(0.5f, 0.8f); fm.startSize3D = true;
            fm.startSizeX = new MinMaxCurve(0.7f, 1.1f); fm.startSizeY = new MinMaxCurve(1.6f, 2.4f); fm.startSizeZ = 1; fm.startColor = Color.white;
            var fe = fl.emission; fe.rateOverTime = 22;
            var fs = fl.shape; fs.enabled = true; fs.shapeType = ParticleSystemShapeType.Box; fs.scale = new Vector3(1.3f, 1.6f, 0.3f);
            var fv = fl.velocityOverLifetime; fv.enabled = true; fv.space = ParticleSystemSimulationSpace.World;
            fv.x = new MinMaxCurve(-0.1f, 0.1f); fv.y = new MinMaxCurve(0.6f, 1.2f); fv.z = new MinMaxCurve(0f, 0f);
            ColorLife(fl, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.15f, 1f), (0.7f, 0.8f), (1f, 0f) }));
            var ft = fl.textureSheetAnimation; ft.enabled = true; ft.mode = ParticleSystemAnimationMode.Grid; ft.numTilesX = 16; ft.numTilesY = 4;
            ft.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ft.startFrame = new MinMaxCurve(0, 63); ft.cycleCount = 1;
            c.Play(fl, on);
        }
        var flames = PS(c, "Flames", hero + new Vector3(0, -0.3f, 0.45f), AddMat(c.tx.flame, 0.7f), 123);
        {
            var m = flames.main; m.duration = 3f; m.startLifetime = new MinMaxCurve(0.45f, 0.85f); m.startSpeed = 0;
            m.startSize = new MinMaxCurve(0.45f, 0.9f); m.startRotation = new MinMaxCurve(-0.25f, 0.25f);
            var em = flames.emission; em.rateOverTime = 75;
            var sh = flames.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.5f, 2.6f, 0.4f);
            var v = flames.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new MinMaxCurve(-0.2f, 0.2f); v.y = new MinMaxCurve(1.3f, 2.6f); v.z = new MinMaxCurve(0f, 0f);
            SizeLife(flames, Curve((0, 0.6f), (0.3f, 1f), (1, 0.15f)));
            var rot = flames.rotationOverLifetime; rot.enabled = true; rot.z = new MinMaxCurve(-0.4f, 0.4f);
            ColorLife(flames, Grad(new[] { (0f, new Color(1f, 0.75f, 0.35f)), (0.3f, new Color(1f, 0.3f, 0.05f)), (0.7f, new Color(0.7f, 0.06f, 0.01f)), (1f, new Color(0.3f, 0.02f, 0.01f)) },
                                   new[] { (0f, 0f), (0.15f, 0.8f), (0.6f, 0.5f), (1f, 0f) }));
            c.Play(flames, on);
            if (c.tx.flame2 != null)
            {
                // 2 種の炎の舌を混ぜる（同じ形が並ばないように）
                var f2 = UnityEngine.Object.Instantiate(flames.gameObject, flames.transform.parent).GetComponent<ParticleSystem>();
                f2.randomSeed = 223; f2.GetComponent<ParticleSystemRenderer>().sharedMaterial = AddMat(c.tx.flame2, 0.7f);
                c.Play(f2, on);
            }
        }
        var embers = PS(c, "Embers", hero + new Vector3(0, -0.6f, 0), AddMat(c.tx.dot, 5f), 124);
        {
            var m = embers.main; m.duration = 3f; m.startLifetime = new MinMaxCurve(0.6f, 1.3f); m.startSpeed = 0;
            m.startSize = new MinMaxCurve(0.03f, 0.07f); m.startColor = new Color(1f, 0.75f, 0.35f);
            var em = embers.emission; em.rateOverTime = 35;
            var sh = embers.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.8f, 2.4f, 0.8f);
            var v = embers.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new MinMaxCurve(-0.3f, 0.3f); v.y = new MinMaxCurve(2.0f, 4.0f); v.z = new MinMaxCurve(0f, 0f);
            var n = embers.noise; n.enabled = true; n.strength = 0.8f; n.frequency = 1.5f; n.scrollSpeed = 1f;
            ColorLife(embers, Grad(new[] { (0f, Color.white), (0.5f, new Color(1f, 0.5f, 0.15f)), (1f, new Color(0.8f, 0.1f, 0.02f)) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) }));
            Trail(embers, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.08f, 0.12f));
            c.Play(embers, on);
        }
    }

    // 12. シールドのオーラ: 下から格子が埋まって張られ、敵の弾を2発受ける
    static void ShieldClip(Ctx c)
    {
        var hero = HeroHome + new Vector3(1.4f, 0, 0); c.heroT.position = hero;
        var center = hero + new Vector3(0, 0.05f, 0);
        var blue = new Color(0.25f, 0.75f, 1f);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(c.root, false); sphere.transform.position = center;
        var sm = new Material(Shader.Find("Lab/Shield")); sm.SetColor("_Color", blue * 1.15f); sphere.GetComponent<MeshRenderer>().sharedMaterial = sm;
        var baseScale = new Vector3(3.4f, 3.9f, 3.4f);
        float on = 0.25f;
        c.OnUpdate(t =>
        {
            float k = Mathf.Clamp01((t - on) / 0.45f);
            sm.SetFloat("_Appear", k);
            float s = t < on ? 0.001f : Mathf.Lerp(0.85f, 1f, EaseOutBack(Mathf.Clamp01((t - on) / 0.3f)));
            sphere.transform.localScale = baseScale * s;
        });
        Surge(c, on, center, new Vector3(center.x, GroundY + 0.02f, 0), blue, 0.9f);
        var glowQ = Quad(c.root, "FloorGlow", new Vector3(center.x, GroundY + 0.1f, 0.35f), new Vector2(4.5f, 1.1f), QMat(c, c.tx.glow, 0, Vector2.one, 0));
        var gm = glowQ.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => gm.SetColor("_Tint", blue * (0.6f * Mathf.Clamp01((t - on) / 0.3f))));

        // 敵の弾（2発）
        var shots = new[] { (launch: 0.85f, from: GoblinHome + new Vector3(-0.6f, 0.4f, 0), aim: center + new Vector3(0.2f, 0.5f, 0)),
                            (launch: 1.75f, from: GoblinHome + new Vector3(-0.6f, -0.2f, 0), aim: center + new Vector3(0.2f, -0.6f, 0)) };
        for (int i = 0; i < shots.Length; i++)
        {
            var sh = shots[i];
            var dir = (sh.aim - sh.from); dir.z = 0;
            var toCenter = (sh.from - center); toCenter.z = 0;
            // 楕円体の表面に当たる位置（外から中心に向かう直線と、縦長の球の交点を近似）
            var hitDirLocal = new Vector3(toCenter.x / baseScale.x, toCenter.y / baseScale.y, 0).normalized;
            hitDirLocal = (hitDirLocal + new Vector3(0, 0, -0.45f)).normalized;
            var hitPoint = center + Vector3.Scale(hitDirLocal, baseScale * 0.5f);
            var travel = hitPoint - sh.from; float speed = 11f; float time = travel.magnitude / speed;
            var orb = PS(c, "Orb" + i, sh.from + new Vector3(0, 0, -0.5f), AddMat(c.tx.dot, 7f), (uint)(140 + i));
            orb.transform.rotation = Quaternion.LookRotation(travel.normalized);
            var m = orb.main; m.startLifetime = time; m.startSpeed = speed; m.startSize = 0.32f; m.startColor = new Color(1f, 0.35f, 0.9f);
            orb.emission.SetBursts(new[] { new Burst(0, 1) });
            Trail(orb, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.35f, 0.35f));
            c.Play(orb, sh.launch);
            Glow(c, sh.from, sh.launch, new Color(1f, 0.3f, 0.8f), 1.6f, (uint)(150 + i));
            float hitT = sh.launch + time;
            sm.SetVector(i == 0 ? "_HitDir0" : "_HitDir1", hitDirLocal);
            string hitKey = i == 0 ? "_HitT0" : "_HitT1";
            c.OnUpdateReal(rt => sm.SetFloat(hitKey, c.Real(hitT)));   // 波紋はシェーダーが描き出しの時刻（_LabT）で動くので、止めの分をずらす
            Flare(c, hitPoint, hitT, blue, 1.6f, (uint)(160 + i));
            Shards(c, hitPoint, hitT, blue, 0.9f, Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg, 100, 16, (uint)(170 + i));
            Impact(c, hitT, -1, Mathf.Atan2(-toCenter.y, -toCenter.x) * Mathf.Rad2Deg, enemy: false);
            if (c.tx.fbElecRing != null) Flip(c, "ElecRing" + i, hitPoint + new Vector3(0, 0, -0.6f), c.tx.fbElecRing, 6, 5, 0, 12, 0.3f, 2.2f, Color.white, 1.3f, (uint)(180 + i), t: hitT);
        }
    }

    // ================= 部品: 斬撃 =================
    struct Style
    {
        public Color outer, mid, core;
        public Style(Color o, Color m, Color c) { outer = o; mid = m; core = c; }
        public Style Ghost() => new Style(new Color(outer.r, outer.g, outer.b, 0.4f), new Color(mid.r, mid.g, mid.b, 0.35f), new Color(mid.r, mid.g, mid.b));
    }

    class Arc { public Material mat; }

    static Arc MakeArc(Ctx c, Vector3 pivot, Quaternion rot, Vector3 localOffset, float R, float band, float a0, float a1, Style st, float thick = 1f)
    {
        var p = new GameObject("SlashPivot").transform; p.SetParent(c.root, false); p.SetPositionAndRotation(pivot, rot);
        var go = new GameObject("Slash"); go.transform.SetParent(p, false); go.transform.localPosition = localOffset;
        go.AddComponent<MeshFilter>().sharedMesh = ArcMesh(R, band, a0, a1);
        var mat = new Material(Shader.Find("Lab/Slash"));
        mat.SetTexture("_NoiseTex", c.tx.noise); if (c.tx.fibers != null) mat.SetTexture("_FiberTex", c.tx.fibers);
        mat.SetColor("_ColOuter", st.outer); mat.SetColor("_ColMid", st.mid); mat.SetColor("_ColCore", st.core);
        mat.SetFloat("_Thick", thick); mat.SetFloat("_Alpha", 0); mat.SetFloat("_Seed", UnityEngine.Random.value * 10f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return new Arc { mat = mat };
    }

    // 刃が進み（ease-out）、振り切ったら尾が追いつきながら削れて消える
    static void Animate(Ctx c, Arc a, float t0, float dur, float fade, float len)
    {
        c.OnUpdate(t =>
        {
            float p = (t - t0) / dur;
            if (p < 0) { a.mat.SetFloat("_Alpha", 0); return; }
            float head = EaseOut(Mathf.Clamp01(p));
            float tail = Mathf.Max(0, head - len);
            float fp = Mathf.Clamp01((t - t0 - dur) / fade);
            if (fp > 0) tail = Mathf.Lerp(tail, head, EaseIn(fp) * 0.85f);
            a.mat.SetFloat("_Head", head); a.mat.SetFloat("_Tail", tail); a.mat.SetFloat("_Fade", fp);
            a.mat.SetFloat("_Alpha", fp >= 1 ? 0 : 1);
        });
    }

    // target を弧の帯の真ん中が通る斬撃。rotZ=0 で左→右、弧は上に膨らむ
    static void SlashThrough(Ctx c, Vector3 target, float rotZ, float tiltX, float R, float band, float sweep, Style st, float t0,
        float dur = 0.09f, float fade = 0.3f, float len = 0.8f, float thick = 1f)
    {
        var rot = Quaternion.Euler(0, 0, rotZ) * Quaternion.Euler(tiltX, 0, 0);
        var arc = MakeArc(c, target + new Vector3(0, 0, -0.3f), rot, new Vector3(0, -(R - band * 0.5f), 0), R, band, 90 + sweep / 2, 90 - sweep / 2, st, thick);
        Animate(c, arc, t0, dur, fade, len);
    }

    static Mesh ArcMesh(float R, float band, float a0, float a1, int n = 120)
    {
        var v = new Vector3[(n + 1) * 2]; var uv = new Vector2[v.Length]; var tri = new int[n * 6];
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n, a = Mathf.Lerp(a0, a1, u) * Mathf.Deg2Rad;
            var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            v[i * 2] = d * (R - band); v[i * 2 + 1] = d * R;
            uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
            if (i < n) { int b = i * 2, k = i * 6; tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 2; tri[k + 3] = b + 1; tri[k + 4] = b + 3; tri[k + 5] = b + 2; }
        }
        var m = new Mesh { vertices = v, uv = uv, triangles = tri }; m.RecalculateBounds(); return m;
    }

    // 斬線: 20F で細くなりながら伸びて消える（Effekseer の斬撃ヒット）
    static void CutLine(Ctx c, Vector3 pos, float angleDeg, float length, float t, Color col)
    {
        var ps = PS(c, "CutLine", pos + new Vector3(0, 0, -0.6f), AddMat(c.tx.streak, 2.6f), 900);
        var m = ps.main; m.startLifetime = 0.33f; m.startSize3D = true; m.startSizeX = length; m.startSizeY = 0.14f; m.startSizeZ = 1;
        m.startRotation = angleDeg * Mathf.Deg2Rad; m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        var s = ps.sizeOverLifetime; s.enabled = true; s.separateAxes = true;
        s.x = new MinMaxCurve(1f, Curve((0, 0.6f), (0.2f, 1f), (1, 1.1f))); s.y = new MinMaxCurve(1f, Curve((0, 1f), (1, 0f))); s.z = new MinMaxCurve(1f, Curve((0, 1), (1, 1)));
        ColorLife(ps, Grad(new[] { (0f, Color.white), (0.3f, col), (1f, col) }, new[] { (0f, 1f), (1f, 0f) }));
        c.Play(ps, t);
    }

    // ヒット 5 層（docs/FX_RESEARCH.md 1）: 閃光 6F（大きく出て縮む）→ 主グローはすぐ消す → 衝撃波は 3F 遅れ → 火花 20〜40F → 細かい粒が一番長く残る
    static void Hit(Ctx c, Vector3 pos, float t, Color col, float scale, float dirDeg, float spread, int shards, uint seed)
    {
        Flare(c, pos, t, col, 2.6f * scale, seed * 10 + 1);
        if (c.tx.fbHitLines != null) Flip(c, "HitLines", pos + new Vector3(0, 0, -0.69f), c.tx.fbHitLines, 6, 4, 1, 12, 0.3f, 3.6f * scale, Color.Lerp(col, Color.white, 0.6f), 2.2f, seed * 10 + 6, t: t);
        else if (c.tx.burst != null) Burst(c, pos, t, col, 2.8f * scale, seed * 10 + 6);
        if (scale >= 1.3f && c.tx.fbBigHit != null)
        {
            Flip(c, "BigHit", pos + new Vector3(0, 0, -0.71f), c.tx.fbBigHit, 6, 5, 1, 12, 0.32f, 3.4f * scale, Color.white, 1.6f, seed * 10 + 8, t: t);
            if (c.tx.fbSmoke != null)
                for (int k = 0; k < 3; k++)
                    Flip(c, "Dust" + k, pos + new Vector3((k - 1) * 0.5f, -0.3f + k * 0.15f, -0.4f), c.tx.fbSmoke, 8, 8, k * 7, 40 + k * 7, 0.9f, 2.2f * scale,
                         new Color(0.55f, 0.5f, 0.45f, 0.4f), 1f, seed * 10 + 20 + (uint)k, delay: 0.04f, alpha: true, t: t);
        }
        if (c.tx.air != null) Air(c, pos, t, col, 2.0f * scale, seed * 10 + 7);
        Glow(c, pos, t, col, 1.8f * scale, seed * 10 + 3);
        Ring(c, pos, t, col, 3.2f * scale, seed * 10 + 2, delay: 0.05f);
        Shards(c, pos, t, col, scale, dirDeg, spread, shards, seed * 10 + 4);
        Bokeh(c, pos, t, col, scale, seed * 10 + 5);
    }

    static ParticleSystem Flare(Ctx c, Vector3 pos, float t, Color col, float size, uint seed)
    {
        var ps = PS(c, "Flare", pos + new Vector3(0, 0, -0.7f), AddMat(c.tx.star4, 3.2f), seed);
        var m = ps.main; m.startLifetime = 0.1f; m.startSize = size; m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 1f), (1f, 0f) }));
        SizeLife(ps, Curve((0, 1), (0.3f, 0.5f), (1, 0.15f)));
        c.Play(ps, t); return ps;
    }

    // 衝撃波: 半径は一気に広げて後はゆっくり。太さ（＝明るさ）は細→太→細
    static ParticleSystem Ring(Ctx c, Vector3 pos, float t, Color col, float size, uint seed, float squash = 1f, float delay = 0f)
    {
        var ps = PS(c, "Ring", pos + new Vector3(0, 0, -0.65f), AddMat(c.tx.ring, 1.4f), seed);
        var m = ps.main; m.startLifetime = 0.25f; m.startSize3D = true; m.startSizeX = size; m.startSizeY = size * squash; m.startSizeZ = 1; m.startColor = col;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        SizeLife(ps, Curve((0, 0.25f), (0.25f, 0.8f), (1, 1f)));
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0.2f), (0.15f, 1f), (1f, 0f) }));
        c.Play(ps, t, delay); return ps;
    }

    // 放射の爆ぜ: 棘の形が 5F だけ開いて消える（向きはばらす）
    static ParticleSystem Burst(Ctx c, Vector3 pos, float t, Color col, float size, uint seed)
    {
        var ps = PS(c, "Burst", pos + new Vector3(0, 0, -0.68f), AddMat(c.tx.burst, 2.4f), seed);
        var m = ps.main; m.startLifetime = 0.09f; m.startSize = size; m.startRotation = new MinMaxCurve(0, 6.28f); m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 1f), (1f, 0f) }));
        SizeLife(ps, Curve((0, 0.7f), (0.4f, 1f), (1, 1.1f)));
        c.Play(ps, t); return ps;
    }

    // 空気: 薄い煙が広がって消える（1 未満。光らせない）。当たりの重さを足す層
    static ParticleSystem Air(Ctx c, Vector3 pos, float t, Color col, float size, uint seed)
    {
        var ps = PS(c, "Air", pos + new Vector3(0, 0, -0.45f), AddMat(c.tx.air, 0.18f), seed);
        var m = ps.main; m.startLifetime = 0.4f; m.startSize = size; m.startRotation = new MinMaxCurve(0, 6.28f); m.startColor = col;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0.9f), (1f, 0f) }));
        SizeLife(ps, Curve((0, 0.5f), (0.3f, 0.9f), (1, 1.15f)));
        c.Play(ps, t, 0.02f); return ps;
    }

    // 主グロー: 1 未満（光らせない）ですぐ消す
    static ParticleSystem Glow(Ctx c, Vector3 pos, float t, Color col, float size, uint seed)
    {
        var ps = PS(c, "Glow", pos + new Vector3(0, 0, -0.5f), AddMat(c.tx.glow, 0.3f), seed);
        var m = ps.main; m.startLifetime = 0.2f; m.startSize = size; m.startColor = col;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (1f, 0f) }));
        c.Play(ps, t); return ps;
    }

    // 余韻: 細かい粒がゆっくり漂って一番長く残る
    static ParticleSystem Bokeh(Ctx c, Vector3 pos, float t, Color col, float scale, uint seed)
    {
        var ps = PS(c, "Bokeh", pos + new Vector3(0, 0, -0.6f), AddMat(c.tx.sparkle ?? c.tx.dot, 1.8f), seed);
        var m = ps.main; m.startLifetime = new MinMaxCurve(0.7f, 1.1f); m.startSpeed = new MinMaxCurve(0.5f * scale, 2f * scale);
        m.startSize = c.tx.sparkle != null ? new MinMaxCurve(0.18f, 0.34f) : new MinMaxCurve(0.04f, 0.09f); m.gravityModifier = -0.05f; m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, (short)Mathf.RoundToInt(10 * scale)) });
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.3f * scale;
        Drag(ps, 1.5f);
        ColorLife(ps, Grad(new[] { (0f, Color.white), (0.3f, col), (1f, col) }, new[] { (0f, 1f), (0.6f, 0.8f), (1f, 0f) }));
        c.Play(ps, t, 0.03f); return ps;
    }

    // 予備: 刃の出だしに小さなきらめき（2〜4F）
    static void Glint(Ctx c, Vector3 pos, float t, Color col)
    {
        var ps = PS(c, "Glint", pos, AddMat(c.tx.star4, 2.5f), 950);
        var m = ps.main; m.startLifetime = 0.07f; m.startSize = 0.9f; m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 1f), (1f, 0f) }));
        SizeLife(ps, Curve((0, 0.3f), (0.4f, 1f), (1, 0.2f)));
        c.Play(ps, t);
    }

    // アニメ寄りの破片: 菱形を速度方向に伸ばす。尾は付けない
    static ParticleSystem Shards(Ctx c, Vector3 pos, float t, Color col, float scale, float dirDeg, float spread, int count, uint seed)
    {
        var ps = PS(c, "Shards", pos + new Vector3(0, 0, -0.6f), AddMat(c.tx.diamond, 2.4f), seed);
        var m = ps.main; m.startLifetime = new MinMaxCurve(0.33f, 0.66f); m.startSpeed = new MinMaxCurve(5f * scale, 15f * scale);
        m.startSize = new MinMaxCurve(0.06f * scale, 0.14f * scale); m.gravityModifier = 0.4f; m.startColor = Color.white;
        ps.emission.SetBursts(new[] { new Burst(0, (short)count) });
        var sh = ps.shape; sh.enabled = true;
        if (spread >= 360) { sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0; }
        else
        {
            sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = spread * 0.5f; sh.radius = 0.05f;
            float a = dirDeg * Mathf.Deg2Rad; ps.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0));
        }
        Drag(ps, 5f);
        ColorLife(ps, Grad(new[] { (0f, Color.white), (0.25f, col), (1f, col) }, new[] { (0f, 1f), (0.5f, 1f), (1f, 0f) }));
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.035f; r.lengthScale = 1.6f;
        c.Play(ps, t); return ps;
    }

    // 実写寄りの火花（spark 見本と同じ作り）
    static void RealSparks(Ctx c, Vector3 pos, float dirDeg, float t, int count, float speedMax, uint seed)
    {
        var sp = PS(c, "Sparks", pos + new Vector3(0, 0, -0.6f), AddMat(c.tx.dot, 5.5f), seed);
        float a = dirDeg * Mathf.Deg2Rad; sp.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0));
        var m = sp.main; m.startLifetime = new MinMaxCurve(0.5f, 1.3f); m.startSpeed = new MinMaxCurve(3f, speedMax);
        m.startSize = new MinMaxCurve(0.03f, 0.06f); m.gravityModifier = 1.1f; m.startColor = new MinMaxGradient(Color.white, new Color(1f, 0.88f, 0.65f));
        sp.emission.SetBursts(new[] { new Burst(0, (short)(count * 0.7f)), new Burst(0.03f, (short)(count * 0.3f)) });
        var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35; sh.radius = 0.05f; sh.randomDirectionAmount = 0.15f;
        ColorLife(sp, Grad(new[] { (0f, Color.white), (0.1f, new Color(1f, 0.95f, 0.8f)), (0.35f, new Color(1f, 0.8f, 0.42f)), (0.6f, new Color(1f, 0.52f, 0.16f)), (0.82f, new Color(0.85f, 0.22f, 0.05f)), (1f, new Color(0.5f, 0.07f, 0.02f)) },
                           new[] { (0f, 1f), (0.75f, 1f), (1f, 0f) }));
        SizeLife(sp, Curve((0, 1), (1, 0.5f)));
        Drag(sp, 0.8f);
        Trail(sp, AddMat(c.tx.trail, 3.2f), new MinMaxCurve(0.05f, 0.08f));
        var col = sp.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes; col.SetPlane(0, Plane(c, GroundY));
        col.bounce = new MinMaxCurve(0.3f, 0.55f); col.dampen = new MinMaxCurve(0.15f, 0.35f); col.lifetimeLoss = 0.15f; col.radiusScale = 0.3f;
        c.Play(sp, t);
    }

    // 敵の反応（Nuclear Throne の作り）: 2F だけ真っ白 → 2F 被弾色 → 消える。1 未満で光らせない
    static void HitFlash(Ctx c, Transform sprite, Texture2D tex, float t, float strength = 1f, Color? after = null)
    {
        var col2 = after ?? new Color(1f, 0.25f, 0.2f);
        var q = Quad(c.root, "HitFlash", sprite.position + new Vector3(0, 0, -0.03f), SpriteSize(tex, sprite.localScale.y), QMat(c, tex, 0, Vector2.one, 0));
        var m = q.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdateReal(rt =>
        {
            q.position = sprite.position + new Vector3(0, 0, -0.03f);
            float a = rt - c.Real(t);
            Color k = a < 0 ? Color.black : a < 2 / 60f ? Color.white * 0.95f : a < 4 / 60f ? col2 * 0.55f
                    : col2 * (0.55f * Mathf.Clamp01(1 - (a - 4 / 60f) / 0.1f));
            m.SetColor("_Tint", k * Mathf.Min(strength, 1f));
        });
    }

    // 発動（バフ・オーラ）: 止めはしない。背景を沈めて色を抜く・閃光（大きく出て縮む）・足元の衝撃波・主人公を 2F 白く・軽い揺れ
    static void Surge(Ctx c, float t, Vector3 center, Vector3 feet, Color col, float power = 1f)
    {
        c.heroT.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DimMul", 0.1f);   // 主役は沈めない
        Flare(c, center, t, col, 3.4f * power, 961);
        Ring(c, feet + new Vector3(0, 0.1f, -0.6f), t, col, 5.5f * power, 962, squash: 0.25f, delay: 0.03f);
        Ring(c, center + new Vector3(0, 0, -0.6f), t, col, 4f * power, 963, delay: 0.06f);
        Bokeh(c, center, t, col, 1.4f * power, 964);
        HitFlash(c, c.heroT, c.hero, t, 1f, col);
        c.OnUpdateReal(rt =>
        {
            float a = rt - c.Real(t);
            if (a < 0) return;
            float k = a < 0.25f ? 1f : Mathf.Clamp01(1 - (a - 0.25f) / 0.4f);
            c.post.stageDim = Mathf.Max(c.post.stageDim, 0.5f * power * k); c.post.stageDesat = Mathf.Max(c.post.stageDesat, 0.3f * k);
            c.post.trauma += 0.35f * power * Mathf.Clamp01(1 - a / 0.3f);
        });
    }

    // 一撃の強さ（docs/FX_RESEARCH.md 1・4・5）。-1 = 連撃の 1 発 / 0 = 弱 / 1 = 強 / 2 = とどめ
    // ヒットストップ（刃とキャラだけ止める）・敵の白と揺れとのけぞり・背景の暗転と色抜き・trauma の揺れ・攻撃の向きへの押し・とどめだけ白黒の衝撃コマ
    static void Impact(Ctx c, float t, int level, float dirDeg, bool enemy = true)
    {
        int L = level + 1;
        float stop = new[] { 3f, 8f, 14f, 20f }[L] / 60f;
        float dim = new[] { 0.2f, 0.55f, 0.68f, 0.78f }[L], desat = new[] { 0.1f, 0.3f, 0.4f, 0.5f }[L];
        float trauma = new[] { 0.3f, 0.45f, 0.7f, 1f }[L], kickPx = new[] { 2f, 4f, 8f, 14f }[L];
        c.Freeze(t, stop);
        float r = dirDeg * Mathf.Deg2Rad; var dv = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        if (enemy) HitFlash(c, c.goblinT, c.goblin, t, level < 0 ? 0.8f : 1f);
        c.OnUpdateReal(rt =>
        {
            float a = rt - c.Real(t);
            if (a < 0) return;
            // 背景: 止めの間は沈めたまま、その後 12F で戻す
            float k = a < stop ? 1f : Mathf.Clamp01(1 - (a - stop) / 0.2f);
            c.post.stageDim = Mathf.Max(c.post.stageDim, dim * k); c.post.stageDesat = Mathf.Max(c.post.stageDesat, desat * k);
            // 揺れ: trauma を 0.35 秒で直線に減らす（量は 2 乗で効く）
            c.post.trauma += trauma * Mathf.Clamp01(1 - a / 0.35f);
            // 押し: 攻撃の向きへ一瞬押して 6F で戻す
            if (a < 0.1f) c.post.kick += dv * kickPx * (1 - a / 0.1f);
            // 敵: 止めの間は細かく震え、止めが明けたら攻撃の向きへのけぞって 0.25 秒で戻る
            if (enemy)
            {
                if (a < stop) c.post.goblinOff += new Vector3(Mathf.Sin(a * 190f) * 0.05f * (1 - a / stop), 0, 0);
                else c.post.goblinOff += (Vector3)(dv * (0.08f + 0.06f * L)) * Mathf.Clamp01(1 - (a - stop) / 0.25f);
            }
            if (level == 2 && a < 4 / 60f) { c.post.mono = 1; if (a < 2 / 60f) c.post.invert = 1; }
        });
    }

    static void Shake(Ctx c, float t0, float dur, float amp, float rot, float punch)
    {
        float ph = t0 * 13.7f;
        c.OnUpdate(t =>
        {
            float a = t - t0; if (a < 0 || a > dur) return;
            float k = 1 - a / dur; k *= k;
            c.post.shake += new Vector2(Mathf.Sin(t * 83f + ph), Mathf.Cos(t * 71f + ph * 1.3f)) * (amp * k);
            c.post.rot += Mathf.Sin(t * 61f + ph) * rot * k;
            c.post.zoom *= 1f + punch * k;
        });
    }

    static Vector2 WorldToUv(Vector3 p) => new Vector2((p.x + 6.4f) / 12.8f, (p.y + 3.6f) / 7.2f);

    // ================= 描画 =================
    class Sys { public ParticleSystem ps; public float t0, delay, last; public bool started; }
    class Post
    {
        public Vector2 shake; public float rot, zoom = 1f; public Vector2 zoomCenter = new Vector2(0.5f, 0.5f);
        public Vector2 blurDir; public float blur;
        public float lines, linesMode, linesSeed, linesDensity = 0.45f; public Vector2 linesCenter = new Vector2(0.5f, 0.5f);
        public float darken, invert, mono, flash;
        public float stageDim, stageDesat, trauma; public Vector2 kick; public Vector3 goblinOff;
    }
    class Tx { public Texture2D dot, glow, ring, star4, diamond, plus, flame, flame2, streak, trail, noise, column, coinFace, burst, air, sparkle, magic, fbHitLines, fbBigHit, fbCharge, fbElecRing, fbFireRing, fbFlame, fbSmoke, fibers; }
    class Ctx
    {
        public Transform root, heroT, goblinT; public Texture2D hero, goblin; public Tx tx;
        public Post post = new Post();
        public List<Sys> systems = new List<Sys>();
        public List<Action<float>> updates = new List<Action<float>>();
        public Dictionary<float, Transform> planes = new Dictionary<float, Transform>();
        public Material goblinMat;
        // ヒットストップ: 作る側の時刻（止めを含まない）＝ sim。描き出しの時刻＝ real。刃とキャラの動き（OnUpdate）は sim、
        // 粒子・揺れ・暗転（OnUpdateReal）は real で動くので、止めの間も火花と揺れは止まらない
        public List<(float t, float d)> freezes = new List<(float t, float d)>();
        public List<Action<float>> realUpdates = new List<Action<float>>();
        public void Freeze(float t, float d) => freezes.Add((t, d));
        public float Real(float sim) { float acc = 0; foreach (var f in freezes) if (sim > f.t) acc += f.d; return sim + acc; }
        public float Sim(float real)
        {
            float acc = 0;
            foreach (var f in freezes.OrderBy(x => x.t)) { float s = f.t + acc; if (real < s) break; if (real < s + f.d) return f.t; acc += f.d; }
            return real - acc;
        }
        public void Play(ParticleSystem ps, float t0, float delay = 0f) => systems.Add(new Sys { ps = ps, t0 = t0, delay = delay });
        public void OnUpdate(Action<float> a) => updates.Add(a);
        public void OnUpdateReal(Action<float> a) => realUpdates.Add(a);
    }

    static void RenderClip(Clip clip)
    {
        string outRoot = Environment.GetEnvironmentVariable("LAB_OUT");
        string dir = Path.Combine(outRoot, clip.name);
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "f_*.png")) File.Delete(f);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        UnityEngine.Random.InitState(clip.name.GetHashCode());
        var c = new Ctx { root = new GameObject("Lab").transform, tx = MakeTextures() };
        BuildStage(c);
        clip.build(c);

        var cam = new GameObject("Cam").AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 3.6f; cam.transform.position = new Vector3(0, 0, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = true; cam.allowMSAA = false;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
        var hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { antiAliasing = 4 };
        var ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = hdr;
        var bloom = new Material(Shader.Find("Hidden/LabBloom"));
        const int levels = 6;
        var mips = new RenderTexture[levels];
        for (int k = 0; k < levels; k++)
            mips[k] = new RenderTexture(W >> (k + 1), H >> (k + 1), 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var shot = new Texture2D(W, H, TextureFormat.RGB24, false);

        int frames = Mathf.RoundToInt((clip.dur + c.freezes.Sum(f => f.d)) * FPS);
        float lastTq = -1;
        // 光らせるのは HDR 1 を超える芯だけ。ブルームは狭く控えめ、背景のコントラスト・彩度はいじらない（docs/FX_RESEARCH.md 3。前回の「全体を強く」は失敗）
        const float Threshold = 1.0f, Knee = 0.15f, BloomIntensity = 0.5f, ShakePx = 16f, ShakeRot = 0.03f;
        const int BloomTop = 3;   // 広がりは 1/16 解像度まで（それより大きいぼかしは足さない）
        Shader.SetGlobalFloat("_LabEmit", 1f);
        c.goblinMat = c.goblinT.GetComponent<MeshRenderer>().sharedMaterial;
        for (int i = 0; i < frames; i++)
        {
            float tq = Mathf.Floor(i / (float)clip.hold) * clip.hold / FPS;
            if (tq != lastTq)
            {
                float st = c.Sim(tq);
                Shader.SetGlobalFloat("_LabT", tq);
                c.post = new Post();
                foreach (var u in c.updates) u(st);
                foreach (var u in c.realUpdates) u(tq);
                foreach (var s in c.systems)
                {
                    float r0 = c.Real(s.t0) + s.delay;
                    if (tq < r0) continue;
                    if (!s.started) { s.ps.Simulate(Mathf.Max(tq - r0, 0.0005f), true, true, false); s.started = true; }
                    else if (tq > s.last) s.ps.Simulate(tq - s.last, true, false, false);
                    s.last = tq;
                }
                // 揺れ: trauma の 2 乗 × 滑らかなノイズ（平行移動＋回転）＋攻撃の向きへの押し
                float tr = Mathf.Clamp01(c.post.trauma), k2 = tr * tr;
                c.post.shake += new Vector2((Mathf.PerlinNoise(tq * 30f, 1.7f) - 0.5f) * 2f * ShakePx / W, (Mathf.PerlinNoise(3.1f, tq * 30f) - 0.5f) * 2f * ShakePx / H) * k2
                              + new Vector2(c.post.kick.x / W, c.post.kick.y / H);
                c.post.rot += (Mathf.PerlinNoise(tq * 24f, 7.3f) - 0.5f) * 2f * ShakeRot * k2;
                Shader.SetGlobalFloat("_StageDim", c.post.stageDim); Shader.SetGlobalFloat("_StageDesat", c.post.stageDesat);
                if (c.goblinT.gameObject.activeSelf) c.goblinT.position = GoblinHome + c.post.goblinOff;
                lastTq = tq;
            }
            cam.Render();
            var p = c.post;
            bloom.SetFloat("_Threshold", Threshold); bloom.SetFloat("_Knee", Knee);
            Graphics.Blit(hdr, mips[0], bloom, 0);
            for (int k = 1; k < levels; k++) Graphics.Blit(mips[k - 1], mips[k], bloom, 1);
            bloom.SetFloat("_BloomIntensity", BloomIntensity); bloom.SetFloat("_Exposure", 1f);
            bloom.SetFloat("_Dim", 0f); bloom.SetFloat("_Contrast", 1f); bloom.SetFloat("_Sat", 1f);
            bloom.SetVector("_Shake", new Vector4(p.shake.x, p.shake.y, p.rot, 0));
            bloom.SetFloat("_Zoom", p.zoom); bloom.SetVector("_ZoomCenter", new Vector4(p.zoomCenter.x, p.zoomCenter.y, 1, 0));
            bloom.SetVector("_BlurDir", p.blurDir); bloom.SetFloat("_BlurAmt", p.blur);
            bloom.SetFloat("_Lines", p.lines); bloom.SetFloat("_LinesMode", p.linesMode); bloom.SetFloat("_LinesSeed", p.linesSeed);
            bloom.SetFloat("_LinesAngle", 0); bloom.SetFloat("_LinesDensity", p.linesDensity);
            bloom.SetVector("_LinesCenter", p.linesCenter); bloom.SetColor("_LinesColor", Color.white);
            bloom.SetFloat("_Darken", p.darken); bloom.SetFloat("_Invert", p.invert); bloom.SetFloat("_Mono", p.mono); bloom.SetFloat("_Flash", p.flash);
            bloom.SetColor("_Tint", new Color(1, 1, 1, 0));
            // 光の広がりは狭く（BloomTop まで）。芯の形を残してにじませる
            for (int k = BloomTop; k > 0; k--) Graphics.Blit(mips[k], mips[k - 1], bloom, 2);
            bloom.SetTexture("_BloomTex", mips[0]);
            Graphics.Blit(hdr, ldr, bloom, 3);
            RenderTexture.active = ldr;
            shot.ReadPixels(new Rect(0, 0, W, H), 0, 0); shot.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, $"f_{i:D4}.png"), shot.EncodeToPNG());
        }
        Debug.Log($"FxLab: {clip.name} {frames} frames");
    }

    static void BuildStage(Ctx c)
    {
        string art = Environment.GetEnvironmentVariable("LAB_ART");
        var bg = LoadTex(Path.Combine(art, "bg.png"));
        Quad(c.root, "BG", new Vector3(0, 0, 10), new Vector2(12.8f, 7.2f), StageMat(bg, -1f, 1f));
        c.hero = LoadTex(Path.Combine(art, "hero.png")); c.goblin = LoadTex(Path.Combine(art, "goblin.png"));
        // 暗転の効き: 背景は全部、主人公は半分、攻撃を受ける敵はほとんど沈めない（視線を敵に集める）
        c.heroT = Quad(c.root, "Hero", HeroHome, SpriteSize(c.hero, 3.3f), StageMat(c.hero, 0.5f, 0.55f));
        c.goblinT = Quad(c.root, "Goblin", GoblinHome, SpriteSize(c.goblin, 2.7f), StageMat(c.goblin, 0.5f, 0.15f));
    }

    // ================= 部品: 汎用 =================
    static Transform Plane(Ctx c, float y)
    {
        if (c.planes.TryGetValue(y, out var t)) return t;
        t = new GameObject("Plane").transform; t.SetParent(c.root, false); t.position = new Vector3(0, y, 0); c.planes[y] = t; return t;
    }

    static Vector2 SpriteSize(Texture2D tex, float height) => new Vector2(height * tex.width / tex.height, height);

    static Material StageMat(Texture2D tex, float cutoff, float dimMul)
    {
        var m = new Material(Shader.Find("Lab/Stage")) { mainTexture = tex }; m.SetFloat("_Cutoff", cutoff); m.SetFloat("_DimMul", dimMul); return m;
    }

    static Transform SpriteQuad(Transform parent, string name, Texture2D tex, Vector3 center, float height)
    {
        var m = new Material(Shader.Find("Unlit/Transparent Cutout")) { mainTexture = tex }; m.SetFloat("_Cutoff", 0.5f);
        return Quad(parent, name, center, SpriteSize(tex, height), m);
    }

    static Transform Quad(Transform parent, string name, Vector3 pos, Vector2 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.position = pos; go.transform.localScale = new Vector3(size.x, size.y, 1);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Material QMat(Ctx c, Texture tex, float noiseAmt, Vector2 tiling, float speed)
    {
        var m = new Material(Shader.Find("Lab/QuadAdd")) { mainTexture = tex };
        m.SetTexture("_NoiseTex", c.tx.noise); m.SetFloat("_NoiseAmt", noiseAmt); m.SetVector("_NoiseTiling", tiling); m.SetFloat("_Speed", speed);
        m.SetColor("_Tint", Color.black);
        return m;
    }

    static Material AddMat(Texture tex, float intensity)
    {
        var m = new Material(Shader.Find("Lab/FxAdd")) { mainTexture = tex }; m.SetFloat("_Intensity", intensity); return m;
    }

    static ParticleSystem PS(Ctx c, string name, Vector3 pos, Material mat, uint seed, Transform parent = null)
    {
        var go = new GameObject(name); go.transform.SetParent(parent != null ? parent : c.root, false); go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.useAutoRandomSeed = false; ps.randomSeed = seed;
        var m = ps.main; m.playOnAwake = false; m.loop = false; m.duration = 3f; m.maxParticles = 4000;
        m.simulationSpace = ParticleSystemSimulationSpace.World; m.startSpeed = 0;
        var em = ps.emission; em.rateOverTime = 0;
        var sh = ps.shape; sh.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    static void Trail(ParticleSystem ps, Material mat, MinMaxCurve life, bool worldSpace = true)
    {
        var tr = ps.trails; tr.enabled = true; tr.mode = ParticleSystemTrailMode.PerParticle; tr.ratio = 1f;
        tr.lifetime = life; tr.minVertexDistance = 0.015f; tr.textureMode = ParticleSystemTrailTextureMode.Stretch;
        tr.worldSpace = worldSpace; tr.dieWithParticles = true; tr.sizeAffectsWidth = true; tr.inheritParticleColor = true;
        tr.widthOverTrail = new MinMaxCurve(1f, Curve((0, 1), (1, 0)));
        tr.colorOverTrail = new MinMaxGradient(Grad(new[] { (0f, Color.white), (1f, new Color(1f, 0.7f, 0.5f)) }, new[] { (0f, 1f), (1f, 0f) }));
        ps.GetComponent<ParticleSystemRenderer>().trailMaterial = mat;
    }

    static void Drag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1000f; lv.drag = drag;
        lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false;
    }

    static void ColorLife(ParticleSystem ps, Gradient g) { var c = ps.colorOverLifetime; c.enabled = true; c.color = new MinMaxGradient(g); }
    static void SizeLife(ParticleSystem ps, AnimationCurve curve) { var s = ps.sizeOverLifetime; s.enabled = true; s.size = new MinMaxCurve(1f, curve); }
    static AnimationCurve Curve(params (float t, float v)[] k) => new AnimationCurve(k.Select(x => new Keyframe(x.t, x.v)).ToArray());

    static Gradient Grad((float t, Color c)[] cols, (float t, float a)[] alphas)
    {
        var g = new Gradient();
        g.SetKeys(cols.Select(x => new GradientColorKey(x.c, x.t)).ToArray(), alphas.Select(x => new GradientAlphaKey(x.a, x.t)).ToArray());
        return g;
    }

    static float EaseOut(float x) => 1 - Mathf.Pow(1 - x, 3);
    static float EaseIn(float x) => x * x * x;
    static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2); }

    // エフェクト素材: 白＋アルファ。小さく描かれるので mipmap と bilinear
    static Texture2D LoadFx(string path, bool gray = false)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        t.LoadImage(File.ReadAllBytes(path)); t.filterMode = FilterMode.Trilinear; t.wrapMode = TextureWrapMode.Clamp;
        if (gray)
        {
            // 色付きの連番を白黒にして、粒子の色で塗り直せるようにする
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++) { byte l = (byte)Mathf.Min(255, (px[i].r * 54 + px[i].g * 183 + px[i].b * 19) / 256 * 1.6f); px[i].r = px[i].g = px[i].b = l; }
            t.SetPixels32(px);
        }
        t.Apply(true); return t;
    }

    // 連番を 1 粒で再生する。from..to はコマ番号（0 始まり、to は含まない）。寿命の間に from → to を流す
    static ParticleSystem Flip(Ctx c, string name, Vector3 pos, Texture2D tex, int tilesX, int tilesY, int from, int to, float life, float size,
        Color col, float intensity, uint seed, float delay = 0f, bool randomRot = true, bool alpha = false, float t = 0f)
    {
        var mat = alpha ? new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = tex } : AddMat(tex, intensity);
        if (alpha) mat.SetFloat("_Intensity", intensity);
        var ps = PS(c, name, pos, mat, seed);
        var m = ps.main; m.startLifetime = life; m.startSize = size; m.startColor = col;
        if (randomRot) m.startRotation = new MinMaxCurve(0, 6.28f);
        ps.emission.SetBursts(new[] { new Burst(0, 1) });
        var ts = ps.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid;
        ts.numTilesX = tilesX; ts.numTilesY = tilesY; ts.animation = ParticleSystemAnimationType.WholeSheet;
        float n = tilesX * tilesY;
        ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, from / n, 1, (to - 0.001f) / n));
        c.Play(ps, t, delay); return ps;
    }

    static Texture2D LoadTex(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path)); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; return t;
    }

    // ================= テクスチャ生成 =================
    static Tx MakeTextures()
    {
        var tx = new Tx();
        tx.dot = Radial(64, r => Mathf.Exp(-r * r * 14f) * Smooth(1f, 0.7f, r));
        tx.glow = Radial(128, r => Mathf.Pow(1f - r, 2.2f) * (0.35f + 0.65f * Mathf.Exp(-r * r * 9f)));
        tx.ring = Radial(128, r => Mathf.Exp(-Mathf.Pow((r - 0.85f) / 0.04f, 2)) + 0.15f * Mathf.Exp(-Mathf.Pow((r - 0.8f) / 0.12f, 2)));
        tx.star4 = Tex(128, 128, (u, v) =>
        {
            float x = u * 2 - 1, y = v * 2 - 1, r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Exp(-Mathf.Abs(x) * 26f) * Mathf.Exp(-y * y * 2.2f) + Mathf.Exp(-Mathf.Abs(y) * 26f) * Mathf.Exp(-x * x * 2.2f);
            return Mathf.Clamp01(a + Mathf.Exp(-r * r * 40f) + 0.25f * Mathf.Exp(-r * r * 8f)) * Smooth(1f, 0.9f, r);
        });
        tx.diamond = Tex(32, 32, (u, v) => { float x = Mathf.Abs(u * 2 - 1), y = Mathf.Abs(v * 2 - 1); return Smooth(0.95f, 0.8f, x + y); });
        tx.plus = Tex(64, 64, (u, v) =>
        {
            float x = Mathf.Abs(u * 2 - 1), y = Mathf.Abs(v * 2 - 1), r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Max(Smooth(0.2f, 0.12f, y) * Smooth(0.75f, 0.6f, x), Smooth(0.2f, 0.12f, x) * Smooth(0.75f, 0.6f, y));
            return Mathf.Clamp01(a + 0.35f * Mathf.Exp(-r * r * 5f)) * Smooth(1f, 0.9f, r);
        });
        tx.streak = Tex(256, 32, (u, v) => { float x = u * 2 - 1, y = v * 2 - 1; return Mathf.Exp(-y * y * 30f) * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(x)), 1.6f); });
        tx.trail = Tex(8, 32, (u, v) => { float y = v * 2 - 1; return Mathf.Exp(-y * y * 6f) * Smooth(1f, 0.8f, Mathf.Abs(y)); });
        tx.column = Tex(64, 128, (u, v) => { float x = u * 2 - 1; return Mathf.Exp(-x * x * 5f) * Smooth(0f, 0.08f, v) * Mathf.Pow(1 - v, 1.4f); });
        tx.noise = TileNoise(128);
        tx.flame = Tex(96, 96, (u, v) =>
        {
            float x = u * 2 - 1, y = v * 2 - 1, r = Mathf.Sqrt(x * x + y * y);
            float f = Fbm(u * 3f + 4.1f, v * 3f + 1.7f);
            return Mathf.Clamp01(Smooth(1f, 0.2f, r) * (f * 2.0f - 0.35f));
        });
        tx.coinFace = CoinFace(256);
        // 素材（tools/fx-lab/textures。Kenney Particle Pack・CC0）。手作りの図形より形に表情がある
        string dir = Environment.GetEnvironmentVariable("LAB_TEX");
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Texture2D K(string n) => LoadFx(Path.Combine(dir, n + ".png"));
            tx.star4 = K("flare"); tx.ring = K("ring"); tx.glow = K("glow"); tx.diamond = K("shard"); tx.streak = K("cutline");
            tx.flame = K("flame_a"); tx.flame2 = K("flame_b"); tx.column = K("beam");
            tx.burst = K("burst"); tx.air = K("air"); tx.sparkle = K("sparkle"); tx.magic = K("magic_circle");
            // 連番（Brackeys VFX Bundle・CC0。名前の NxM がコマの並び）
            tx.fbHitLines = K("fb_hitlines_6x4"); tx.fbBigHit = K("fb_bighit_6x5"); tx.fbCharge = LoadFx(Path.Combine(dir, "fb_charge_7x6.png"), gray: true);
            // 斬撃の繊維（tools/fx-lab/make_slash_tex.py で作る。U は繰り返し、値はリニア）
            var fib = new Texture2D(2, 2, TextureFormat.RGBA32, true, true); fib.LoadImage(File.ReadAllBytes(Path.Combine(dir, "slash_fibers.png")));
            fib.wrapModeU = TextureWrapMode.Repeat; fib.wrapModeV = TextureWrapMode.Clamp; fib.filterMode = FilterMode.Trilinear; fib.Apply(true); tx.fibers = fib;
            tx.fbElecRing = K("fb_elecring_6x5"); tx.fbFireRing = K("fb_firering_6x5"); tx.fbFlame = K("fb_flame_16x4"); tx.fbSmoke = K("fb_smoke_8x8");
        }
        return tx;
    }

    static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }
    static float Fbm(float x, float y) { float f = 0, a = 0.5f, fr = 1; for (int o = 0; o < 5; o++) { f += a * Mathf.PerlinNoise(x * fr + o * 11.3f, y * fr + o * 7.1f); a *= 0.5f; fr *= 2; } return f; }

    static Texture2D Radial(int n, Func<float, float> alpha) => Tex(n, n, (u, v) =>
    {
        float x = u * 2f - 1f, y = v * 2f - 1f; float r = Mathf.Sqrt(x * x + y * y);
        return r >= 1f ? 0f : alpha(r);
    });

    static Texture2D Tex(int w, int h, Func<float, float, float> alpha, Func<float, float, Color> color = null, bool repeat = false)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true) { wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
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

    // 継ぎ目のないノイズ（4 隅の重み付き合成）
    static Texture2D TileNoise(int n)
    {
        const float S = 4f;
        return Tex(n, n, (u, v) => 1f, (u, v) =>
        {
            float x = u * S, y = v * S;
            float a = Fbm(x, y), b = Fbm(x - S, y), c = Fbm(x, y - S), d = Fbm(x - S, y - S);
            float f = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
            f = Mathf.Clamp01((f - 0.5f) * 1.8f + 0.5f);
            return new Color(f, f, f, 1);
        }, repeat: true);
    }

    // コインの面: 縁・内側の細い輪・中央の星を浮き彫りに。左上から光が当たる陰影を焼き込む
    static Texture2D CoinFace(int n)
    {
        var h = new float[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float px = (x + 0.5f) / n * 2 - 1, py = (y + 0.5f) / n * 2 - 1, r = Mathf.Sqrt(px * px + py * py);
                float hv = 0.35f;
                if (r > 0.84f) hv = 1f;
                else if (r > 0.72f && r < 0.77f) hv = 0.8f;
                float a = Mathf.Atan2(py, px) + Mathf.PI / 2;
                float sector = Mathf.Repeat(a, Mathf.PI * 2 / 5) / (Mathf.PI * 2 / 5);
                float tt = Mathf.Abs(sector - 0.5f) * 2;
                float rs = 0.2f + 0.33f * Mathf.Pow(1 - tt, 2.2f);
                if (r < rs) hv = 1f;
                h[x, y] = hv;
            }
        var t = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px2 = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float hv = h[x, y];
                float e = h[Mathf.Max(x - 1, 0), Mathf.Min(y + 1, n - 1)] - h[Mathf.Min(x + 1, n - 1), Mathf.Max(y - 1, 0)];
                float s = Mathf.Clamp(1f + e * 1.6f, 0.35f, 1.8f) * (0.6f + 0.4f * hv);
                px2[y * n + x] = new Color(s, s, s, 1);
            }
        t.SetPixels(px2); t.Apply(false, false); return t;
    }

    static Mesh CoinMesh(int seg = 40, float thick = 0.12f)
    {
        var v = new List<Vector3>(); var nrm = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? -thick / 2 : thick / 2; var nz = new Vector3(0, 0, side == 0 ? -1 : 1);
            int c0 = v.Count; v.Add(new Vector3(0, 0, z)); nrm.Add(nz); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; float x = Mathf.Cos(a) * 0.5f, y = Mathf.Sin(a) * 0.5f;
                v.Add(new Vector3(x, y, z)); nrm.Add(nz); uv.Add(new Vector2(0.5f + (side == 0 ? x : -x), 0.5f + y));
            }
            for (int i = 0; i < seg; i++)
            {
                if (side == 0) { tri.Add(c0); tri.Add(c0 + 1 + i); tri.Add(c0 + 2 + i); }
                else { tri.Add(c0); tri.Add(c0 + 2 + i); tri.Add(c0 + 1 + i); }
            }
        }
        int r0 = v.Count;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            v.Add(d * 0.5f + new Vector3(0, 0, -thick / 2)); nrm.Add(d); uv.Add(new Vector2(0.5f, 0.03f));
            v.Add(d * 0.5f + new Vector3(0, 0, thick / 2)); nrm.Add(d); uv.Add(new Vector2(0.5f, 0.03f));
        }
        for (int i = 0; i < seg; i++) { int b = r0 + i * 2; tri.Add(b); tri.Add(b + 2); tri.Add(b + 1); tri.Add(b + 1); tri.Add(b + 2); tri.Add(b + 3); }
        var m = new Mesh(); m.SetVertices(v); m.SetNormals(nrm); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
    }
}
