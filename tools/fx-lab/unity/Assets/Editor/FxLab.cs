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
    class Clip { public string name; public float dur; public int hold = 1; public Action<Ctx> build; public bool part, forest, hero, goblin; }

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
        // 倒れる（ディゾルブ）
        yield return new Clip { name = "death_dissolve", dur = 2.0f, hold = 1, build = DeathDissolve };
        yield return new Clip { name = "death_ash", dur = 2.8f, hold = 1, build = DeathAsh };
        yield return new Clip { name = "death_burn", dur = 2.4f, hold = 1, build = DeathBurn };
        yield return new Clip { name = "death_holy", dur = 2.6f, hold = 1, build = DeathHoly };
        yield return new Clip { name = "death_shatter", dur = 2.2f, hold = 1, build = DeathShatter };
        yield return new Clip { name = "death_slice", dur = 2.2f, hold = 1, build = DeathSlice };
        // 画面の枠（ステップアップ）
        yield return new Clip { name = "stepup", dur = 7.2f, hold = 1, build = StepUpClip };
        yield return new Clip { name = "fire_frame", dur = 4.4f, hold = 1, build = FireFrameClip };
        yield return new Clip { name = "fire_frame_red", dur = 4.4f, hold = 1, build = c => { FireFrame(c, 0.3f, 4.2f, FireRed, 1f, 501); c.OnUpdate(t => { if (t >= 0.3f) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.25f); }); } };
        foreach (var p in PartClips()) yield return p;   // 部品集
        // カットイン
        yield return new Clip { name = "cutin_streak", dur = 1.8f, hold = 1, build = CutStreak };
        yield return new Clip { name = "cutin_elec", dur = 1.8f, hold = 1, build = CutElec };
        yield return new Clip { name = "cutin_fire", dur = 1.8f, hold = 1, build = CutFire };
        yield return new Clip { name = "cutin_focus", dur = 1.8f, hold = 1, build = CutFocus };
        yield return new Clip { name = "cutin_rainbow", dur = 1.8f, hold = 1, build = CutRainbow };
        // 落ちて主人公に集まる
        yield return new Clip { name = "drop_embers", dur = 3.2f, hold = 1, build = DropEmbers };
        yield return new Clip { name = "drop_shards", dur = 3.2f, hold = 1, build = DropShards };
        yield return new Clip { name = "drop_souls", dur = 3.2f, hold = 1, build = DropSouls };
        // 前兆・昇格
        yield return new Clip { name = "cutin_upgrade", dur = 3.0f, hold = 1, build = CutUpgrade };
        yield return new Clip { name = "longfreeze", dur = 5.4f, hold = 1, build = LongFreeze };
        yield return new Clip { name = "sword_split", dur = 3.0f, hold = 1, build = SwordSplit };
        // シールド
        yield return new Clip { name = "shield_orbit", dur = 3.0f, hold = 1, build = ShieldOrbit };
        yield return new Clip { name = "shield_big", dur = 2.6f, hold = 1, build = ShieldBig };
        yield return new Clip { name = "shield_guardian", dur = 3.0f, hold = 1, build = ShieldGuardian };
        // 追加 33 本（前兆 10・扉 3・カットイン 5・バフ 5・倒れる 5・打撃 5）
        yield return new Clip { name = "omen_glass", dur = 2.4f, hold = 1, build = OmenGlass };
        yield return new Clip { name = "omen_quake", dur = 3.0f, hold = 1, build = OmenQuake };
        yield return new Clip { name = "omen_warning", dur = 2.6f, hold = 1, build = OmenWarning };
        yield return new Clip { name = "omen_timestop", dur = 3.2f, hold = 1, build = OmenTimeStop };
        yield return new Clip { name = "omen_eyelid", dur = 2.8f, hold = 1, build = OmenEyelid };
        yield return new Clip { name = "omen_heartbeat", dur = 3.0f, hold = 1, build = OmenHeartbeat };
        yield return new Clip { name = "omen_thunder", dur = 2.9f, hold = 1, build = OmenThunder };
        yield return new Clip { name = "omen_stare", dur = 2.6f, hold = 1, build = OmenStare };
        yield return new Clip { name = "omen_letterbox", dur = 2.6f, hold = 1, build = OmenLetterbox };
        yield return new Clip { name = "omen_glitch", dur = 3.1f, hold = 1, build = OmenGlitch };
        yield return new Clip { name = "door_gate", dur = 3.3f, hold = 1, build = DoorGate };
        yield return new Clip { name = "door_vault", dur = 3.3f, hold = 1, build = DoorVault };
        yield return new Clip { name = "door_shutter", dur = 3.3f, hold = 1, build = DoorShutter };
        yield return new Clip { name = "door_gold", dur = 3.3f, hold = 1, build = DoorGold };
        yield return new Clip { name = "cutin_vs", dur = 2.2f, hold = 1, build = CutVs };
        yield return new Clip { name = "cutin_vertical", dur = 1.8f, hold = 1, build = CutVertical };
        yield return new Clip { name = "cutin_panels", dur = 2.1f, hold = 1, build = CutPanels };
        yield return new Clip { name = "cutin_confetti", dur = 2.2f, hold = 1, build = CutConfetti };
        yield return new Clip { name = "cutin_enemy", dur = 1.8f, hold = 1, build = CutEnemy };
        yield return new Clip { name = "buff_barrier", dur = 2.6f, hold = 1, build = BuffBarrier };
        yield return new Clip { name = "buff_hexwall", dur = 2.4f, hold = 1, build = BuffHexWall };
        yield return new Clip { name = "buff_heal", dur = 2.4f, hold = 1, build = BuffHealBloom };
        yield return new Clip { name = "buff_power", dur = 2.2f, hold = 1, build = BuffPower };
        yield return new Clip { name = "buff_speed", dur = 2.6f, hold = 1, build = BuffSpeed };
        yield return new Clip { name = "death_freeze", dur = 2.4f, hold = 1, build = DeathFreeze };
        yield return new Clip { name = "death_blackhole", dur = 2.2f, hold = 1, build = DeathBlackhole };
        yield return new Clip { name = "death_explode", dur = 2.2f, hold = 1, build = DeathExplode };
        yield return new Clip { name = "death_pixel", dur = 2.0f, hold = 1, build = DeathPixel };
        yield return new Clip { name = "drop_gems", dur = 3.2f, hold = 1, build = DropGems };
        yield return new Clip { name = "hit_thrust", dur = 1.6f, hold = 1, build = HitThrust };
        yield return new Clip { name = "hit_blunt", dur = 1.8f, hold = 1, build = HitBlunt };
        yield return new Clip { name = "hit_fireball", dur = 2.0f, hold = 1, build = HitFireball };
        yield return new Clip { name = "hit_ice", dur = 2.2f, hold = 1, build = HitIce };
        yield return new Clip { name = "hit_combo", dur = 1.9f, hold = 1, build = HitCombo };
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

    // ================= 倒れる（ディゾルブ）=================
    // 敵の絵に「消える順」の図（R）を焼き、しきい値を上げて削る。前線は光る縁、手前に焦げの帯。
    // 消える瞬間の画素は、その色のまま粒にして飛ばす（C# が同じ図を持つので、前線と粒がぴったり揃う）
    class DeathMap
    {
        public Texture2D tex; public int w, h;
        public List<(float val, Vector2 uv, Color col)> samples = new List<(float, Vector2, Color)>();
        public Vector2[] cellCenter;   // 破片の重心（uv）
    }

    static DeathMap BakeDeath(Ctx c, Func<float, float, float> order, int cells = 0, uint seed = 1)
    {
        var spr = c.goblin; int sw = spr.width, sh = spr.height;
        int w = sw / 2, h = sh / 2;
        var src = spr.GetPixels32();
        Func<int, int, Color32> P = (x, y) => src[Mathf.Min(y * 2, sh - 1) * sw + Mathf.Min(x * 2, sw - 1)];
        var rnd = new System.Random((int)seed);
        // 破片の種: 不透明な画素から選ぶ
        var seeds = new List<Vector2>();
        if (cells > 0)
            while (seeds.Count < cells)
            {
                int x = rnd.Next(w), y = rnd.Next(h);
                if (P(x, y).a > 128) seeds.Add(new Vector2(x, y));
            }
        var val = new float[w * h]; var id = new byte[w * h];
        float lo = 1e9f, hi = -1e9f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (P(x, y).a <= 128) continue;
                float v = order(x / (float)w, y / (float)h);
                val[i] = v; lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v);
                if (cells > 0)
                {
                    int best = 0; float bd = 1e9f;
                    for (int k = 0; k < seeds.Count; k++) { float d = (seeds[k] - new Vector2(x, y)).sqrMagnitude; if (d < bd) { bd = d; best = k; } }
                    id[i] = (byte)best;
                }
            }
        var dm = new DeathMap { w = w, h = h };
        var px = new Color32[w * h];
        var sum = new Vector2[Mathf.Max(cells, 1)]; var cnt = new int[Mathf.Max(cells, 1)];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x; var sc = P(x, y);
                if (sc.a <= 128) { px[i] = new Color32(255, 0, 0, 255); continue; }
                float v = (val[i] - lo) / Mathf.Max(hi - lo, 1e-4f) * 0.98f + 0.01f;   // 0.01〜0.99。_Cut 1 で消え切る
                byte crack = 0;
                if (cells > 0)
                {
                    for (int oy = -1; oy <= 1 && crack == 0; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = Mathf.Clamp(x + ox, 0, w - 1), ny = Mathf.Clamp(y + oy, 0, h - 1);
                            if (id[ny * w + nx] != id[i] && P(nx, ny).a > 128) { crack = 255; break; }
                        }
                    sum[id[i]] += new Vector2(x / (float)w, y / (float)h); cnt[id[i]]++;
                }
                px[i] = new Color32((byte)(v * 255), id[i], crack, 255);
                if ((x & 1) == 0 && (y & 1) == 0) dm.samples.Add((v, new Vector2((x + 0.5f) / w, (y + 0.5f) / h), (Color)sc));
            }
        dm.samples.Sort((a, b) => a.val.CompareTo(b.val));
        if (cells > 0) { dm.cellCenter = new Vector2[cells]; for (int k = 0; k < cells; k++) dm.cellCenter[k] = cnt[k] > 0 ? sum[k] / cnt[k] : new Vector2(0.5f, 0.5f); }
        dm.tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        dm.tex.SetPixels32(px); dm.tex.Apply();
        return dm;
    }

    static Material DeathMat(Ctx c, DeathMap dm, Color edge, float edgeW, Color charCol, float charW)
    {
        var m = new Material(Shader.Find("Lab/Dissolve")) { mainTexture = c.goblin };
        m.SetTexture("_MapTex", dm.tex); m.SetColor("_EdgeCol", edge); m.SetFloat("_Edge", edgeW);
        m.SetColor("_CharCol", charCol); m.SetFloat("_CharW", charW); m.SetFloat("_Cut", 0);
        return m;
    }

    // 消える前線から粒を出す。pick で「この画素から出すか・色・速さ・寿命・大きさ」を決める
    delegate bool Spawn(Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float size);
    static void FrontEmitter(Ctx c, DeathMap dm, Func<Vector2, Vector3> worldOf, Func<float, float> cutAt, ParticleSystem ps, Spawn spawn, uint seed)
    {
        float last = 0; var rnd = new System.Random((int)seed); int idx = 0;
        c.OnUpdate(t =>
        {
            float cut = cutAt(t);
            if (cut <= last) return;
            while (idx < dm.samples.Count && dm.samples[idx].val < cut)
            {
                var s = dm.samples[idx++];
                if (!spawn(s.uv, s.col, rnd, out var col, out var vel, out var life, out var sz)) continue;
                var ep = new EmitParams
                {
                    position = worldOf(s.uv) + new Vector3(0, 0, -0.2f),
                    velocity = vel, startColor = col, startLifetime = life, startSize = sz, applyShapeToPosition = false
                };
                ps.Emit(ep, 1);
            }
            last = cut;
        });
    }

    static ParticleSystem DeathParticles(Ctx c, string name, Material mat, uint seed, float drag, float noise, float gravity, Gradient life)
    {
        var ps = PS(c, name, Vector3.zero, mat, seed);
        var m = ps.main; m.maxParticles = 20000; m.gravityModifier = gravity;
        if (drag > 0) Drag(ps, drag);
        if (noise > 0) { var n = ps.noise; n.enabled = true; n.strength = noise; n.frequency = 1.2f; n.scrollSpeed = 0.8f; n.quality = ParticleSystemNoiseQuality.Medium; }
        if (life != null) ColorLife(ps, life);
        c.Play(ps, 0f);
        return ps;
    }

    // 絵の uv → ワールド座標（板は 1x1 を size 倍した Quad なので、回転・移動もそのまま効く）
    static Func<Vector2, Vector3> OnSprite(Transform q) => uv => q.TransformPoint(new Vector3(uv.x - 0.5f, uv.y - 0.5f, 0));

    static float Ease01(float t, float t0, float dur) => Mathf.Clamp01((t - t0) / dur);
    static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // 倒れる間は背景を沈めて、崩れる粒を読ませる（sim の t0 から dur、戻りは 0.3 秒）
    static void DeathDim(Ctx c, float t0, float dur, float dim = 0.5f)
    {
        c.OnUpdate(t =>
        {
            float k = t < t0 ? 0 : t < t0 + dur ? 1 : Mathf.Clamp01(1 - (t - t0 - dur) / 0.3f);
            c.post.stageDim = Mathf.Max(c.post.stageDim, dim * k); c.post.stageDesat = Mathf.Max(c.post.stageDesat, 0.3f * k);
        });
    }

    // D1. 溶けて消える: 光る縁で削れていき、縁から光の粒と絵の欠片が昇る
    static void DeathDissolve(Ctx c)
    {
        SlashThrough(c, G, -45, 25, 2.3f, 1.0f, 160, Cyan, 0.3f);
        Hit(c, G, 0.36f, Cyan.mid, 1.1f, -45, 60, 28, 301);
        Impact(c, 0.36f, 1, -45);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v) => Fbm(u * 5f + 3.1f, v * 5f + 1.3f) * 0.85f + (1 - v) * 0.15f, 0, 302);
        var mat = DeathMat(c, dm, new Color(0.5f, 2.2f, 4.2f), 0.035f, new Color(0.05f, 0.15f, 0.35f, 0.9f), 0.04f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = mat;
        float t0 = 0.5f, dur = 0.85f;
        DeathDim(c, 0.4f, dur + 0.2f);
        Func<float, float> cut = t => { float k = Ease01(t, t0, dur); return k * k * (3 - 2 * k) * 1.02f; };
        c.OnUpdate(t => mat.SetFloat("_Cut", cut(t)));
        var bits = DeathParticles(c, "DeathBits", new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 303, 1.2f, 0.6f, -0.08f,
            Grad(new[] { (0f, Color.white), (1f, new Color(0.4f, 0.7f, 1f)) }, new[] { (0f, 1f), (0.6f, 0.9f), (1f, 0f) }));
        var glow = DeathParticles(c, "DeathGlow", AddMat(c.tx.dot, 2.6f), 304, 1.5f, 0.8f, -0.15f,
            Grad(new[] { (0f, Color.white), (1f, new Color(0.3f, 0.8f, 1f)) }, new[] { (0f, 1f), (1f, 0f) }));
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, bits, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = src; vel = new Vector3((float)r.NextDouble() * 0.6f - 0.3f, 0.3f + (float)r.NextDouble() * 0.9f, 0); life = 0.5f + (float)r.NextDouble() * 0.6f; sz = 0.025f;
            return r.NextDouble() < 0.35;
        }, 305);
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, glow, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = new Color(0.6f, 0.9f, 1f); vel = new Vector3((float)r.NextDouble() * 0.8f - 0.4f, 0.5f + (float)r.NextDouble() * 1.5f, 0); life = 0.4f + (float)r.NextDouble() * 0.7f; sz = 0.05f + (float)r.NextDouble() * 0.05f;
            return r.NextDouble() < 0.06;
        }, 306);
    }

    // D2. 灰になって崩れる: 攻撃の来た側から、絵の画素がそのまま粒になって風に流れる（色は灰へ）
    static void DeathAsh(Ctx c)
    {
        SlashThrough(c, G, 0, 70, 2.5f, 1.1f, 150, Steel, 0.3f);
        Hit(c, G, 0.36f, Steel.mid, 1f, 0, 50, 26, 311);
        Impact(c, 0.36f, 1, 0);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v) => u * 0.72f + Fbm(u * 6f + 7.7f, v * 6f + 2.2f) * 0.28f + (1 - v) * 0.05f, 0, 312);
        var mat = DeathMat(c, dm, new Color(1.4f, 0.8f, 0.4f), 0.012f, new Color(0.18f, 0.16f, 0.15f, 1f), 0.05f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = mat;
        float t0 = 0.55f, dur = 1.3f;
        DeathDim(c, 0.4f, dur + 0.4f, 0.45f);
        Func<float, float> cut = t => { float k = Ease01(t, t0, dur); return Mathf.Pow(k, 1.3f) * 1.02f; };
        c.OnUpdate(t => mat.SetFloat("_Cut", cut(t)));
        var ash = DeathParticles(c, "Ash", new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 313, 0.6f, 0.9f, -0.03f,
            Grad(new[] { (0f, Color.white), (0.3f, Color.white), (0.7f, new Color(0.85f, 0.8f, 0.75f)), (1f, new Color(0.7f, 0.68f, 0.66f)) }, new[] { (0f, 1f), (0.75f, 0.9f), (1f, 0f) }));
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, ash, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = Color.Lerp(src, Color.white, 0.25f); vel = new Vector3(1.2f + (float)r.NextDouble() * 2.2f, 0.3f + (float)r.NextDouble() * 1.2f, 0); life = 1.0f + (float)r.NextDouble() * 1.0f;
            sz = 0.02f + (float)r.NextDouble() * 0.025f;
            return r.NextDouble() < 0.8;
        }, 314);
    }

    // D3. 燃え尽きる: 足元から焦げて燃え上がる。前線に炎の連番、火の粉が昇る
    static void DeathBurn(Ctx c)
    {
        SlashThrough(c, G, -135, 20, 2.2f, 0.95f, 150, Flame, 0.3f);
        Hit(c, G, 0.36f, Flame.mid, 1.2f, 0, 360, 30, 321);
        Impact(c, 0.36f, 1, -135);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v) => v * 0.7f + Fbm(u * 5f + 1.9f, v * 5f + 8.3f) * 0.3f, 0, 322);
        var mat = DeathMat(c, dm, new Color(4f, 1.5f, 0.3f), 0.03f, new Color(0.08f, 0.03f, 0.02f, 1f), 0.08f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = mat;
        float t0 = 0.5f, dur = 1.25f;
        DeathDim(c, 0.4f, dur + 0.2f);
        Func<float, float> cut = t => { float k = Ease01(t, t0, dur); return k * 1.02f; };
        c.OnUpdate(t => mat.SetFloat("_Cut", cut(t)));
        var embers = DeathParticles(c, "Embers", AddMat(c.tx.dot, 3.5f), 323, 0.8f, 0.9f, -0.2f,
            Grad(new[] { (0f, new Color(1f, 0.9f, 0.6f)), (0.5f, new Color(1f, 0.45f, 0.1f)), (1f, new Color(0.6f, 0.08f, 0.02f)) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) }));
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, embers, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = Color.white; vel = new Vector3((float)r.NextDouble() * 0.8f - 0.4f, 1.0f + (float)r.NextDouble() * 2.2f, 0); life = 0.5f + (float)r.NextDouble() * 0.9f;
            sz = 0.03f + (float)r.NextDouble() * 0.04f;
            return r.NextDouble() < 0.12;
        }, 324);
        if (c.tx.fbFlame != null)
        {
            var fire = DeathParticles(c, "BurnFire", AddMat(c.tx.fbFlame, 0.8f), 325, 0f, 0f, 0f,
                Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.2f, 1f), (0.7f, 0.8f), (1f, 0f) }));
            var ft = fire.textureSheetAnimation; ft.enabled = true; ft.mode = ParticleSystemAnimationMode.Grid; ft.numTilesX = 16; ft.numTilesY = 4;
            ft.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ft.startFrame = new MinMaxCurve(0, 63);
            FrontEmitter(c, dm, OnSprite(c.goblinT), cut, fire, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
            {
                col = new Color(1f, 1f, 1f, 0.8f); vel = new Vector3(0, 0.6f + (float)r.NextDouble() * 0.6f, 0); life = 0.35f + (float)r.NextDouble() * 0.25f; sz = 0.35f + (float)r.NextDouble() * 0.3f;
                return r.NextDouble() < 0.0025;
            }, 326);
        }
    }

    // D4. 昇天: 白く光るシルエットになり、足元から光の粒になって昇る。上から光の柱
    static void DeathHoly(Ctx c)
    {
        SlashThrough(c, G, 0, 70, 2.5f, 1.1f, 150, Gold, 0.3f);
        Hit(c, G, 0.36f, Gold.mid, 1.1f, 0, 60, 26, 331);
        Impact(c, 0.36f, 1, 0);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v) => v * 0.65f + Fbm(u * 6f + 4.4f, v * 6f + 0.7f) * 0.35f, 0, 332);
        var mat = DeathMat(c, dm, new Color(3.5f, 3f, 1.6f), 0.03f, new Color(0, 0, 0, 0), 0f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = mat;
        float tw = 0.5f, t0 = 0.75f, dur = 1.2f;
        DeathDim(c, 0.4f, dur + 0.4f, 0.55f);
        Func<float, float> cut = t => { float k = Ease01(t, t0, dur); return k * 1.02f; };
        c.OnUpdate(t => { mat.SetFloat("_Cut", cut(t)); mat.SetFloat("_Flash", Mathf.Clamp01((t - tw) / 0.2f) * 0.92f); });
        var beam = Quad(c.root, "HolyBeam", new Vector3(G.x, 1.0f, 0.3f), new Vector2(2.4f, 7.4f), QMat(c, c.tx.column, 0.6f, new Vector2(2, 1), 0.8f));
        var bm = beam.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t =>
        {
            float up = Ease01(t, tw, 0.25f), down = 1 - Ease01(t, t0 + dur - 0.2f, 0.5f);
            bm.SetColor("_Tint", new Color(1f, 0.97f, 0.88f) * (1.4f * up * down));
        });
        var motes = DeathParticles(c, "HolyMotes", AddMat(c.tx.sparkle ?? c.tx.dot, 2.2f), 333, 0.4f, 0.3f, -0.25f,
            Grad(new[] { (0f, Color.white), (1f, new Color(1f, 0.85f, 0.5f)) }, new[] { (0f, 1f), (0.7f, 0.9f), (1f, 0f) }));
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, motes, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = Color.white; vel = new Vector3((float)r.NextDouble() * 0.4f - 0.2f, 1.2f + (float)r.NextDouble() * 1.6f, 0); life = 0.8f + (float)r.NextDouble() * 0.8f;
            sz = r.NextDouble() < 0.15 ? 0.22f : 0.05f + (float)r.NextDouble() * 0.04f;
            return r.NextDouble() < 0.1;
        }, 334);
        Flare(c, G, tw, new Color(1f, 0.9f, 0.55f), 3.2f, 335);
        Ring(c, new Vector3(G.x, GroundY + 0.12f, -0.6f), tw, new Color(1f, 0.9f, 0.55f), 4.5f, 336, squash: 0.25f, delay: 0.03f);
    }

    // D5. 砕け散る: とどめの止めの間にひびが光り、明けた瞬間に破片が弾け飛ぶ。破片は回って落ちながら縁から溶ける
    static void DeathShatter(Ctx c) => DeathShatterWith(c, new Color(4f, 0.8f, 2.2f), new Color(0.2f, 0f, 0.1f, 0.8f), true);
    static void DeathShatterWith(Ctx c, Color edge, Color charCol, bool slash, float tHit = 0.36f)
    {
        const int N = 26;
        if (slash)
        {
            SlashThrough(c, G, 10, 55, 2.8f, 1.15f, 160, Crimson, 0.3f, dur: 0.1f, fade: 0.35f);
            Hit(c, G, 0.36f, Crimson.mid, 1.6f, 10, 360, 40, 341);
        }
        Impact(c, tHit, 2, 10);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v) => Fbm(u * 7f + 5.5f, v * 7f + 3.3f), N, 342);
        var crackMat = DeathMat(c, dm, edge, 0.03f, new Color(0, 0, 0, 0), 0f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = crackMat;
        float tb = tHit + 0.01f;   // 止めが明けた直後（sim）に割れる
        DeathDim(c, tHit, 0.9f);
        c.OnUpdate(t => { crackMat.SetFloat("_Crack", Mathf.Clamp01((t - tHit) / 0.005f)); c.goblinT.gameObject.SetActive(t < tb); });
        c.OnUpdateReal(rt => { float a = rt - c.Real(tHit); if (a >= 0 && a < 0.4f) crackMat.SetFloat("_Crack", Mathf.Clamp01(a / 0.15f) * 1.2f); });
        var rnd = new System.Random(343);
        var hitUv = new Vector2(0.45f, 0.55f);
        for (int k = 0; k < N; k++)
        {
            var cc = dm.cellCenter[k];
            var pivot = new GameObject("Shard" + k).transform; pivot.SetParent(c.root, false);
            var home = GoblinHome + new Vector3((cc.x - 0.5f) * size.x, (cc.y - 0.5f) * size.y, -0.001f * k);
            pivot.position = home;
            var m = DeathMat(c, dm, edge, 0.05f, charCol, 0.05f);
            m.SetFloat("_CellId", k); m.SetFloat("_Crack", 1f);
            var q = Quad(pivot, "Piece", Vector3.zero, size, m);
            q.localPosition = new Vector3((0.5f - cc.x) * size.x, (0.5f - cc.y) * size.y, 0);
            var dir = (cc - hitUv); dir.x += 0.35f; dir = dir.normalized;
            float sp = 3f + (float)rnd.NextDouble() * 4.5f, spin = ((float)rnd.NextDouble() * 2 - 1) * 540f;
            var v0 = new Vector3(dir.x * sp, dir.y * sp + 2.5f, 0);
            float dStart = tb + 0.18f + (float)rnd.NextDouble() * 0.22f;
            c.OnUpdate(t =>
            {
                bool on = t >= tb; pivot.gameObject.SetActive(on);
                if (!on) return;
                float a = t - tb;
                pivot.position = home + v0 * a + new Vector3(0, -4.9f * a * a, 0);
                pivot.rotation = Quaternion.Euler(0, 0, spin * a);
                m.SetFloat("_Crack", Mathf.Clamp01(1 - a / 0.5f) * 1.2f);
                m.SetFloat("_Cut", Mathf.Clamp01((t - dStart) / 0.45f) * 1.02f);
            });
        }
        if (c.tx.fbSmoke != null)
            for (int k = 0; k < 3; k++)
                Flip(c, "ShatterDust" + k, G + new Vector3((k - 1) * 0.6f, -0.2f, -0.4f), c.tx.fbSmoke, 8, 8, k * 9, 40 + k * 9, 1.0f, 2.6f,
                     new Color(0.5f, 0.4f, 0.45f, 0.45f), 1f, 344 + (uint)k, delay: 0.02f, alpha: true, t: tb);
    }

    // D6. 真っ二つ: 斜めの一閃で切り口が光り、上半分が滑り落ちる。両方とも切り口から溶けて消える
    static void DeathSlice(Ctx c)
    {
        const float ang = -28f;
        SlashThrough(c, G + new Vector3(0, 0.1f, 0), ang, 15, 3.0f, 1.0f, 170, Crimson, 0.3f, dur: 0.08f);
        Hit(c, G, 0.36f, Crimson.mid, 1.3f, ang, 40, 30, 351);
        CutLine(c, G + new Vector3(0, 0.1f, 0), ang, 7f, 0.36f, Crimson.mid);
        Impact(c, 0.36f, 2, ang);
        var size = SpriteSize(c.goblin, 2.7f);
        float r = ang * Mathf.Deg2Rad; var tan = new Vector2(Mathf.Cos(r), Mathf.Sin(r)); var n = new Vector2(-tan.y, tan.x);
        float d = 0.1f;   // 中心から少し上を通る（world）
        // 消える順: 切り口から遠いほど後
        var dm = BakeDeath(c, (u, v) =>
        {
            var p = new Vector2((u - 0.5f) * size.x, (v - 0.5f) * size.y);
            return Mathf.Abs(Vector2.Dot(p, n) - d) * 0.6f + Fbm(u * 6f + 2.6f, v * 6f + 9.1f) * 0.4f;
        }, 0, 352);
        float tb = 0.37f, t0 = 0.75f, dur = 0.8f;
        DeathDim(c, 0.36f, dur + 0.5f);
        Func<float, float> cut = t => { float k = Ease01(t, t0, dur); return k * 1.02f; };
        var halves = new List<(Transform tr, Material m, float side)>();
        foreach (float side in new[] { 1f, -1f })
        {
            var m = DeathMat(c, dm, new Color(4f, 0.6f, 1.6f), 0.035f, new Color(0.15f, 0f, 0.05f, 0.9f), 0.04f);
            m.SetVector("_PlaneN", new Vector4(n.x * size.x, n.y * size.y, 0, 0)); m.SetFloat("_PlaneD", d); m.SetFloat("_PlaneSide", side);
            var q = Quad(c.root, side > 0 ? "UpperHalf" : "LowerHalf", GoblinHome, size, m);
            q.gameObject.SetActive(false);
            halves.Add((q, m, side));
        }
        c.OnUpdate(t => c.goblinT.gameObject.SetActive(t < tb));
        foreach (var hv in halves)
        {
            var h = hv;
            c.OnUpdate(t =>
            {
                bool on = t >= tb; h.tr.gameObject.SetActive(on);
                if (!on) return;
                float a = t - tb;
                if (h.side > 0)
                {
                    // 上半分: 切り口に沿って滑り、少し傾いて落ちる
                    float s = EaseOut(Mathf.Clamp01(a / 0.6f));
                    h.tr.position = GoblinHome + (Vector3)(tan * (0.55f * s)) + new Vector3(0, -0.35f * s * s, -0.01f);
                    h.tr.rotation = Quaternion.Euler(0, 0, -9f * s);
                }
                else h.tr.position = GoblinHome + new Vector3(0, -0.05f * Mathf.Clamp01(a / 0.3f), 0);
                h.m.SetFloat("_CutGlow", 2.2f * Mathf.Clamp01(1 - a / 0.5f) + 0.3f);
                h.m.SetFloat("_Cut", cut(t));
            });
        }
        var bits = DeathParticles(c, "SliceBits", AddMat(c.tx.dot, 2.4f), 353, 1.2f, 0.5f, -0.1f,
            Grad(new[] { (0f, Color.white), (1f, new Color(1f, 0.3f, 0.6f)) }, new[] { (0f, 1f), (1f, 0f) }));
        FrontEmitter(c, dm, uv => { var p = new Vector2((uv.x - 0.5f) * size.x, (uv.y - 0.5f) * size.y); return OnSprite(halves[Vector2.Dot(p, n) - d >= 0 ? 0 : 1].tr)(uv); }, cut, bits, (Vector2 uv, Color src, System.Random rr, out Color col, out Vector3 vel, out float life, out float sz) =>
        {
            col = Color.white; vel = new Vector3((float)rr.NextDouble() - 0.5f, 0.4f + (float)rr.NextDouble() * 1.2f, 0); life = 0.4f + (float)rr.NextDouble() * 0.6f; sz = 0.04f + (float)rr.NextDouble() * 0.04f;
            return rr.NextDouble() < 0.08;
        }, 354);
    }

    // ================= 画面の枠（ステップアップ）=================
    class FrameStep
    {
        public string label; public float mode; public Color col, core, edge; public float reach, intensity; public bool rainbow, thunderOverlay;
        public Color particle; public bool embers;
    }

    // ステップアップ: 白 → 青 → 黄 → 緑 → 赤 → 虹（期待度の色の慣習。ProbabilityEditorWindow と同じ並び）
    // 段が上がる瞬間: 枠が太く弾む・白く光る・揺れ・四隅の閃光・粒が噴く。背景は段ごとに深く沈める
    static void StepUpClip(Ctx c)
    {
        var steps = new[]
        {
            new FrameStep { label = "STEP 1", mode = 0, col = new Color(0.85f, 0.92f, 1f), core = Color.white * 2.0f, reach = 0.45f, intensity = 0.8f, particle = Color.white },
            new FrameStep { label = "STEP 2", mode = 1, col = new Color(0.15f, 0.5f, 1f), core = new Color(1.6f, 2.3f, 3.4f), reach = 0.6f, intensity = 1f, particle = new Color(0.5f, 0.8f, 1f) },
            new FrameStep { label = "STEP 3", mode = 1, col = new Color(1f, 0.78f, 0.12f), core = new Color(3.2f, 2.8f, 1.3f), reach = 0.8f, intensity = 1.1f, particle = new Color(1f, 0.9f, 0.4f) },
            new FrameStep { label = "STEP 4", mode = 3, col = new Color(0.15f, 1f, 0.3f), core = new Color(1.6f, 3.2f, 1.3f), edge = new Color(0f, 0.2f, 0.05f), reach = 0.8f, intensity = 1f, particle = new Color(0.5f, 1f, 0.5f), embers = true },
            new FrameStep { label = "STEP 5", mode = 3, col = new Color(1f, 0.28f, 0.04f), core = new Color(3.4f, 2.1f, 0.8f), edge = new Color(0.35f, 0.02f, 0f), reach = 0.95f, intensity = 1.05f, particle = new Color(1f, 0.6f, 0.2f), embers = true },
            new FrameStep { label = "STEP 6", mode = 3, col = Color.white, core = Color.white * 2.4f, reach = 1.1f, intensity = 1.1f, rainbow = true, thunderOverlay = true, particle = Color.white, embers = true },
        };
        float first = 0.4f, gap = 1.0f;
        // 全段を同じ炎にして、色だけ替える（本人 2026-09-29「炎の差し替えではなく色の差し替え」）。炎の形は段が変わっても途切れない
        var pals = new[] { FireWhite, FireBlue, FireYellow, FireGreen, FireRed, FireRainbow };
        int StepAt(float t) => Mathf.Clamp(Mathf.FloorToInt((t - first) / gap), 0, pals.Length - 1);
        FireFrame(c, first, 7.2f, FireWhite, 1f, 510, palAt: t => pals[StepAt(t)], kicks: Enumerable.Range(0, pals.Length).Select(k => first + k * gap).ToArray(),
                  powerAt: t => 0.8f + 0.07f * StepAt(t));
        const float FW = 13.0f, FH = 7.4f, T = 1.2f;
        // 枠は画面の縁に沿う角丸の四角。一周の長さに素材の繰り返しをぴったり合わせて継ぎ目を消す
        const float R = 1.0f; var half = new Vector2(6.4f, 3.6f);
        float per = 4 * (half.x - R) + 4 * (half.y - R) + 2 * Mathf.PI * R;
        float uScale = Mathf.Round(per / 4f) / per;
        Material MakeFrameMat(float seed)
        {
            var m = new Material(Shader.Find("Lab/Frame"));
            m.SetTexture("_NoiseTex", c.tx.noise); if (c.tx.fibers != null) m.SetTexture("_FiberTex", c.tx.fibers); m.SetFloat("_Seed", seed); m.SetFloat("_Intensity", 0);
            m.SetVector("_Half", half); m.SetFloat("_Radius", R); m.SetFloat("_UScale", uScale); m.SetFloat("_Thick", T);
            return m;
        }
        var mainMat = MakeFrameMat(0.3f); var thunderMat = MakeFrameMat(5.1f);
        foreach (var (mat, name) in new[] { (mainMat, "Frame"), (thunderMat, "FrameThunder") })
        {
            Quad(c.root, name, new Vector3(0, 0, -3f), new Vector2(FW, FH), mat);
        }
        // 段の札（STEP n）
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lab = new GameObject("StepLabel"); lab.transform.SetParent(c.root, false); lab.transform.position = new Vector3(0, 2.55f, -3.2f);
        var tm = lab.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 120; tm.characterSize = 0.045f; tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold; tm.text = "";
        lab.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        c.OnUpdate(t =>
        {
            int k = Mathf.FloorToInt((t - first) / gap); if (t < first) k = -1;
            k = Mathf.Min(k, steps.Length - 1);
            if (k < 0) { mainMat.SetFloat("_Intensity", 0); thunderMat.SetFloat("_Intensity", 0); tm.text = ""; return; }
            var st = steps[k]; float a = t - (first + k * gap);
            float burst = Mathf.Exp(-a * 9f);
            mainMat.SetFloat("_Mode", st.mode); mainMat.SetColor("_Col", st.mode > 2.5f ? st.col * 0.55f : st.col);   // 炎の段は床の明かりだけ（炎は連番が描く） mainMat.SetColor("_Core", st.core); mainMat.SetColor("_Edge", st.edge);
            mainMat.SetFloat("_Rainbow", st.rainbow ? 1 : 0); mainMat.SetFloat("_Burst", burst);
            bool fireStep = true;   // 全段を FireFrame の炎が描く（色だけ替える）
            mainMat.SetFloat("_Thick", (st.mode > 2.5f ? 2.0f : T * st.reach) * (1 + 0.45f * burst)); mainMat.SetFloat("_Shade", st.mode > 2.5f ? 0.82f : 0.35f + 0.07f * k); mainMat.SetFloat("_Intensity", fireStep ? 0 : st.intensity * Mathf.Clamp01(a / 0.03f + 0.3f));
            thunderMat.SetFloat("_Mode", 1); thunderMat.SetFloat("_Rainbow", 1); thunderMat.SetFloat("_Burst", burst); thunderMat.SetFloat("_Thick", T * 0.75f); thunderMat.SetFloat("_Shade", 0f);
            thunderMat.SetColor("_Core", Color.white * 2.6f); thunderMat.SetFloat("_Intensity", 0);   // 雷は重ねない（色だけで段を見せる）
            // 段が上がる瞬間: 白く光る 2F・揺れ（段ごとに強く）・背景を段ごとに沈める
            if (a < 2 / 60f) c.post.flash = Mathf.Max(c.post.flash, 0.35f + 0.05f * k);
            c.post.trauma += (0.3f + 0.08f * k) * Mathf.Clamp01(1 - a / 0.3f);
            c.post.stageDim = Mathf.Max(c.post.stageDim, 0.1f * (k + 1)); c.post.stageDesat = Mathf.Max(c.post.stageDesat, 0.06f * (k + 1));
            // 札: 段の色でポンと出る
            tm.text = st.label;
            float pop = 1 + 0.5f * Mathf.Exp(-a * 14f);
            lab.transform.localScale = Vector3.one * pop;
            tm.color = st.rainbow ? Color.HSVToRGB(Mathf.Repeat(t * 0.5f, 1f), 0.6f, 1f) * 1.6f : st.core * 0.7f;
        });
        // 段ごとの閃光（四隅）と、枠から噴く粒
        for (int k = 0; k < steps.Length; k++)
        {
            var st = steps[k]; float ts = first + k * gap;
            foreach (var corner in new[] { new Vector3(-6.2f, -3.3f, -3.3f), new Vector3(6.2f, -3.3f, -3.3f), new Vector3(6.2f, 3.3f, -3.3f), new Vector3(-6.2f, 3.3f, -3.3f) })
                Flare(c, corner, ts, st.rainbow ? Color.white : st.col, 2.4f + 0.3f * k, (uint)(400 + k * 4));
            var ps = PS(c, "FrameFx" + k, new Vector3(0, 0, -3.1f), st.embers ? AddMat(c.tx.dot, 3f) : AddMat(c.tx.diamond, 2.6f), (uint)(420 + k));
            var m = ps.main; m.duration = k == steps.Length - 1 ? 2f : gap; m.startColor = st.particle;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.BoxEdge; sh.scale = new Vector3(12.6f, 7.0f, 0f);
            var em = ps.emission; em.rateOverTime = 40 + 25 * k; em.SetBursts(new[] { new Burst(0, (short)(30 + 10 * k)) });
            if (st.embers)
            {
                m.startLifetime = new MinMaxCurve(0.5f, 1.1f); m.startSpeed = new MinMaxCurve(0.2f, 0.8f); m.startSize = new MinMaxCurve(0.04f, 0.09f); m.gravityModifier = -0.25f;
                var n = ps.noise; n.enabled = true; n.strength = 0.6f; n.frequency = 1.2f;
            }
            else
            {
                m.startLifetime = new MinMaxCurve(0.12f, 0.3f); m.startSpeed = new MinMaxCurve(2f, 6f); m.startSize = new MinMaxCurve(0.05f, 0.1f);
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
                Drag(ps, 3f);
            }
            if (st.rainbow) { var col = ps.colorOverLifetime; col.enabled = true; col.color = new MinMaxGradient(Grad(new[] { (0f, new Color(1f, 0.4f, 0.4f)), (0.33f, new Color(1f, 1f, 0.4f)), (0.66f, new Color(0.4f, 1f, 1f)), (1f, new Color(1f, 0.5f, 1f)) }, new[] { (0f, 1f), (1f, 0f) })); }
            else ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) }));
            c.Play(ps, ts);
        }
    }

    // ================= 本物寄りの炎の枠 =================
    // シミュレーションの炎の連番（Thomas Iché・CC0）を四辺に重ね、温度の色で塗る（Lab/Fire）。
    // 足すもの: 火の粉（上へ舞う・乱流・尾）/ 薄い煙（上の縁）/ 床の明かり（Lab/Frame の 3）/ 照り返し（背景を炎の色で照らす・ちらつく）/ 陽炎
    class FirePal { public Color c0, c1, c2, c3, light; public bool rainbow; public Color a0, a1, a2, a3; }   // a0〜a3 = アニメの 4 段（先の暗い色 → 根元の明るい色）
    static readonly FirePal FireRed = new FirePal { c0 = new Color(0.12f, 0.01f, 0f), c1 = new Color(0.9f, 0.16f, 0.01f), c2 = new Color(1f, 0.55f, 0.08f), c3 = new Color(2.1f, 1.35f, 0.45f), light = new Color(1f, 0.45f, 0.12f),
        a0 = new Color(0.75f, 0.1f, 0.04f), a1 = new Color(1f, 0.38f, 0.05f), a2 = new Color(1f, 0.72f, 0.12f), a3 = new Color(1.4f, 1.3f, 0.75f) };
    static readonly FirePal FireGreen = new FirePal { c0 = new Color(0f, 0.08f, 0.02f), c1 = new Color(0.04f, 0.55f, 0.1f), c2 = new Color(0.35f, 1f, 0.3f), c3 = new Color(1.6f, 2.8f, 1.5f), light = new Color(0.3f, 1f, 0.35f),
        a0 = new Color(0.02f, 0.35f, 0.08f), a1 = new Color(0.1f, 0.75f, 0.2f), a2 = new Color(0.45f, 1f, 0.4f), a3 = new Color(1.1f, 1.5f, 0.9f) };
    static readonly FirePal FireRainbow = new FirePal { c0 = new Color(0.12f, 0.01f, 0f), c1 = new Color(0.9f, 0.16f, 0.01f), c2 = new Color(1f, 0.55f, 0.08f), c3 = new Color(2.4f, 2.4f, 2.4f), light = new Color(0.9f, 0.6f, 0.9f), rainbow = true };
    static readonly FirePal FireWhite = new FirePal { c0 = new Color(0.1f, 0.1f, 0.14f), c1 = new Color(0.6f, 0.65f, 0.8f), c2 = new Color(0.9f, 0.93f, 1f), c3 = new Color(2f, 2f, 2.1f), light = new Color(0.8f, 0.85f, 1f),
        a0 = new Color(0.45f, 0.5f, 0.68f), a1 = new Color(0.72f, 0.78f, 0.95f), a2 = new Color(0.9f, 0.94f, 1f), a3 = new Color(1.35f, 1.4f, 1.45f) };
    static readonly FirePal FireYellow = new FirePal { c0 = new Color(0.3f, 0.15f, 0f), c1 = new Color(1f, 0.7f, 0.05f), c2 = new Color(1f, 0.9f, 0.3f), c3 = new Color(2.2f, 2f, 1f), light = new Color(1f, 0.8f, 0.25f),
        a0 = new Color(0.85f, 0.5f, 0.02f), a1 = new Color(1f, 0.75f, 0.06f), a2 = new Color(1f, 0.92f, 0.3f), a3 = new Color(1.45f, 1.4f, 0.9f) };
    // 理想の画像（本人 2026-09-29）: 先は濃い青、根元は薄い水色
    static readonly FirePal FireBlue = new FirePal { c0 = new Color(0f, 0.02f, 0.15f), c1 = new Color(0.05f, 0.25f, 1f), c2 = new Color(0.2f, 0.75f, 1f), c3 = new Color(1.3f, 2f, 2.4f), light = new Color(0.25f, 0.55f, 1f),
        a0 = new Color(0.05f, 0.18f, 0.85f), a1 = new Color(0.12f, 0.42f, 1f), a2 = new Color(0.2f, 0.8f, 1f), a3 = new Color(0.85f, 1.35f, 1.45f) };

    // 連番のコマの並び。Blender で作った炎（bl_*）は 8×8、それ以外の炎の連番は 16×4
    static Vector2Int GridOf(Texture t) => t != null && t.name.StartsWith("bl_") ? new Vector2Int(8, 8) : new Vector2Int(16, 4);

    static Material FireMat(Ctx c, Texture2D sheet, FirePal pal, float intensity)
    {
        var m = new Material(Shader.Find("Lab/Fire")) { mainTexture = sheet };
        m.SetColor("_C0", pal.c0); m.SetColor("_C1", pal.c1); m.SetColor("_C2", pal.c2); m.SetColor("_C3", pal.c3);
        m.SetFloat("_Intensity", intensity); m.SetFloat("_Tint", pal.rainbow ? 1 : 0);
        return m;
    }

    static Gradient RainbowGrad() => Grad(new[] { (0f, new Color(1f, 0.25f, 0.25f)), (0.2f, new Color(1f, 0.8f, 0.2f)), (0.4f, new Color(0.3f, 1f, 0.35f)),
                                                  (0.6f, new Color(0.25f, 0.8f, 1f)), (0.8f, new Color(0.55f, 0.35f, 1f)), (1f, new Color(1f, 0.35f, 0.85f)) }, new[] { (0f, 1f), (1f, 1f) });

    // 1 辺ぶんの炎。a → b の線から、画面の中心へ向かって伸びる（本人 2026-09-28「中心に向かって」）
    // 出る場所はランダムにせず、黄金比の並びで辺に沿って均等に散らす（ランダムだと偏って炎の間に隙間が空く。本人「隙間が不恰好」）
    // dir = 内向き。連番の上（炎の先）を dir に回す。surge = 大きな炎が一気に伸びる波（0.55 秒ごとの束）
    static ParticleSystem EdgeFire(Ctx c, string name, Vector3 a, Vector3 b, Vector2 dir, float rate, float w0, float w1, Material mat, uint seed, float t0, float t1,
        bool rainbow, bool surge = false, float life0 = 0.5f, float life1 = 0.8f)
    {
        var ps = PS(c, name, Vector3.zero, mat, seed);
        var m = ps.main; m.duration = 10f; m.maxParticles = 3000; m.startSize3D = true;
        var em = ps.emission; em.rateOverTime = 0;
        var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
        float sp0 = surge ? 1.8f : 0.8f, sp1 = surge ? 2.8f : 1.6f;   // 内へ伸びる速さ
        v.x = new MinMaxCurve(dir.x * sp0 - 0.1f, dir.x * sp1 + 0.1f); v.y = new MinMaxCurve(dir.y * sp0 - 0.1f, dir.y * sp1 + 0.1f); v.z = new MinMaxCurve(0f, 0f);
        SizeLife(ps, surge ? Curve((0, 0.5f), (0.25f, 1.15f), (1, 1.2f)) : Curve((0, 0.7f), (0.3f, 1f), (1, 1.1f)));
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.1f, 1f), (0.6f, 0.85f), (1f, 0f) }));
        // 連番は寿命の間に 64 コマを流す（約 3 倍速。メラメラを速く）
        var grid = GridOf(mat.mainTexture);
        var ts = ps.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = grid.x; ts.numTilesY = grid.y;
        ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ts.startFrame = new MinMaxCurve(0, grid.x * grid.y - 1);
        c.Play(ps, 0f);
        var rnd = new System.Random((int)seed); float R() => (float)rnd.NextDouble();
        float rot = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, phase = R();
        var perp = new Vector3(dir.x, dir.y, 0);
        var grad = RainbowGrad();
        int k = 0; float acc = 0, last = -1, nextWave = t0 + 0.15f; bool filled = false;
        void EmitOne()
        {
            float u = Mathf.Repeat(phase + k++ * 0.6180339f, 1f);
            float w = Mathf.Lerp(w0, w1, R());
            var ep = new EmitParams
            {
                position = Vector3.Lerp(a, b, u) + perp * ((R() - 0.5f) * 0.15f),
                startSize3D = new Vector3(w, w * (1.9f + R() * 0.2f), 1), rotation = rot + (R() - 0.5f) * 12f,
                startLifetime = surge ? Mathf.Lerp(0.4f, 0.55f, R()) : Mathf.Lerp(life0, life1, R()),
                startColor = WithAlpha(rainbow ? grad.Evaluate(R()) : Color.white, Mathf.Lerp(0.65f, 1f, R())), applyShapeToPosition = false   // 明るさを 1 本ずつばらす（強弱）
            };
            ps.Emit(ep, 1);
        }
        c.OnUpdate(t =>
        {
            if (t >= t1 + 0.12f) { if (last < t1 + 0.12f) ps.Clear(); last = t; return; }   // 次の段の閃光の瞬間に消す（色が混ざって濁らないように）
            if (t < t0) { last = t; return; }
            float dt = last < t0 ? 0 : t - last; last = t;
            if (surge)
            {
                while (t >= nextWave) { for (int i = 0; i < (int)rate; i++) EmitOne(); nextWave += 0.55f; }
                return;
            }
            if (!filled) { for (int i = 0; i < (int)(rate * 0.6f); i++) EmitOne(); filled = true; }   // 点いた瞬間に一列そろえる
            acc += rate * dt;
            while (acc >= 1) { acc -= 1; EmitOne(); }
        });
        return ps;
    }

    // flipbook = true で炎の連番を辺に並べる作り（2026-09-28 本人「並べるんじゃなくて枠で一つの炎」で既定は false。枠でつながった 1 つの炎）
    // anime = true（既定。本人 2026-09-29 の理想画像）: 同じ形を 4 段に塗り分けるアニメの炎。false で Blender の連番の本物寄り
    // palAt: 時刻ごとの色（ステップアップで炎はそのまま色だけ替える）。kicks: 弾む時刻（段が上がった瞬間）。powerAt: 時刻ごとの太さ
    static void FireFrame(Ctx c, float t0, float t1, FirePal pal, float power, uint seed, bool flipbook = false, bool anime = true,
        Func<float, FirePal> palAt = null, float[] kicks = null, Func<float, float> powerAt = null)
    {
        Func<float, FirePal> P = t => palAt != null ? palAt(t) : pal;
        Func<float, float> Kick = t => { float last = t0; if (kicks != null) foreach (var k in kicks) if (t >= k) last = k; return Mathf.Exp(-(t - last) * 9f); };
        // 枠でつながった炎（Lab/Frame の 4）。下地の暗さ・うねり・段が上がった瞬間の弾みもここ
        {
            const float R = 1.0f; var half = new Vector2(6.4f, 3.6f);
            float per = 4 * (half.x - R) + 4 * (half.y - R) + 2 * Mathf.PI * R;
            var fm = new Material(Shader.Find("Lab/Frame"));
            fm.SetTexture("_NoiseTex", c.tx.noise); fm.SetFloat("_Mode", anime ? 6 : c.tx.blWall != null ? 5 : 4); fm.SetFloat("_Seed", seed * 0.37f);
            fm.SetFloat("_Steps", anime ? 12 : 0);   // アニメはコマ打ち（12 枚/秒）
            fm.SetColor("_A0", pal.a0); fm.SetColor("_A1", pal.a1); fm.SetColor("_A2", pal.a2); fm.SetColor("_A3", pal.a3);
            if (c.tx.blWall != null) { fm.SetTexture("_FireTex", c.tx.blWall); fm.SetFloat("_FireRate", 45); fm.SetFloat("_FireTile", 1); }   // Blender の炎（一周で約 7 枚並ぶ）
            fm.SetVector("_Half", half); fm.SetFloat("_Radius", R); fm.SetFloat("_UScale", Mathf.Round(per / 2.2f) / per);
            fm.SetFloat("_Thick", (anime ? 1.5f : 1.8f) * power); fm.SetFloat("_Shade", 0.8f);
            fm.SetColor("_R0", pal.c0); fm.SetColor("_R1", pal.c1); fm.SetColor("_R2", pal.c2); fm.SetColor("_R3", pal.c3); fm.SetFloat("_Rainbow", pal.rainbow ? 1 : 0);
            Quad(c.root, "FireBand", new Vector3(0, 0, -3.02f), new Vector2(13f, 7.4f), fm);
            c.OnUpdate(t =>
            {
                bool on = t >= t0 && t < t1 + 0.12f;
                var pp = P(t);
                fm.SetColor("_A0", pp.a0); fm.SetColor("_A1", pp.a1); fm.SetColor("_A2", pp.a2); fm.SetColor("_A3", pp.a3); fm.SetFloat("_Rainbow", pp.rainbow ? 1 : 0);
                fm.SetFloat("_Intensity", on ? Mathf.Clamp01((t - t0) / 0.06f) : 0);
                fm.SetFloat("_Burst", on ? Kick(t) : 0);
                if (powerAt != null) fm.SetFloat("_Thick", (anime ? 1.5f : 1.8f) * powerAt(t) * (1 + 0.35f * Kick(t)));
                // 脈打ち: ゆっくりした呼吸（約 1.6 回/秒）＋速いちらつき
                fm.SetFloat("_Pulse", 1f + 0.1f * Mathf.Sin(t * 10f) + 0.06f * (Mathf.PerlinNoise(t * 18f, 2.2f) - 0.5f));
                if (pp.rainbow) fm.SetColor("_Col", Color.white);
            });
        }
        var sheets = new[] { c.tx.fbFlame, c.tx.fireFlame03 ?? c.tx.fbFlame };
        const float z = -3.05f;
        int n = 0;
        if (flipbook)
        // 四辺から中心へ。根元は画面の外（中心から見て縁の外側）に置く
        foreach (var (a, b, bedA, bedB, dir, rate, w0, w1) in new[]
        {
            (new Vector3(-6.9f, -3.2f, z), new Vector3(6.9f, -3.2f, z), new Vector3(-6.9f, -3.75f, z), new Vector3(6.9f, -3.75f, z), Vector2.up, 20f, 1.1f, 1.8f),     // 下
            (new Vector3(-6.9f, 3.2f, z), new Vector3(6.9f, 3.2f, z), new Vector3(-6.9f, 3.75f, z), new Vector3(6.9f, 3.75f, z), Vector2.down, 18f, 1.0f, 1.6f),     // 上
            (new Vector3(-5.9f, -4f, z), new Vector3(-5.9f, 4f, z), new Vector3(-6.5f, -4f, z), new Vector3(-6.5f, 4f, z), Vector2.right, 12f, 0.9f, 1.4f),         // 左
            (new Vector3(5.9f, -4f, z), new Vector3(5.9f, 4f, z), new Vector3(6.5f, -4f, z), new Vector3(6.5f, 4f, z), Vector2.left, 12f, 0.9f, 1.4f),             // 右
        })
        {
            // 炎の床: 根元に小さな炎を密に並べて途切れさせない
            EdgeFire(c, "EdgeBed" + n++, bedA, bedB, dir, rate * 2.6f * power, 0.55f, 0.8f, FireMat(c, sheets[1], pal, 1.3f), seed + 60 + (uint)n, t0, t1, pal.rainbow, life0: 0.35f, life1: 0.5f);
            for (int k = 0; k < 2; k++, n++)
                EdgeFire(c, "EdgeFire" + n, a, b, dir, rate * power, w0, w1, FireMat(c, sheets[k], pal, 1.4f), seed + (uint)n, t0, t1, pal.rainbow);
            // 波: 大きな炎が束になって一気に中心へ伸びる
            EdgeFire(c, "EdgeSurge" + n++, a, b, dir, rate * 0.45f * power, w0 * 1.4f, w1 * 1.6f, FireMat(c, sheets[0], pal, 1.5f), seed + 90 + (uint)n, t0, t1, pal.rainbow, surge: true);
        }

        // 火の粉: 縁から上へ舞う。乱流と短い尾
        var em = PS(c, "Embers", new Vector3(0, 0, -3.1f), AddMat(anime ? c.tx.diamond : c.tx.dot, anime ? 3f : 4f), seed + 40);
        if (anime) { var er = em.GetComponent<ParticleSystemRenderer>(); er.renderMode = ParticleSystemRenderMode.Stretch; er.velocityScale = 0.02f; er.lengthScale = 3f; }   // 細い線の火の粉
        {
            var m = em.main; m.duration = Mathf.Max(t1 - t0, 0.1f); m.startLifetime = new MinMaxCurve(0.8f, 1.8f); m.startSize = new MinMaxCurve(0.02f, 0.045f);
            m.startColor = pal.rainbow ? new MinMaxGradient(RainbowGrad()) { mode = ParticleSystemGradientMode.RandomColor } : new MinMaxGradient(Color.white);
            var e = em.emission; e.rateOverTime = 80 * power;
            var sh = em.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.BoxEdge; sh.scale = new Vector3(12.6f, 7.0f, 0f);
            m.startLifetime = new MinMaxCurve(0.5f, 1.0f);
            var v = em.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
            v.radial = new MinMaxCurve(-4.5f, -2f);   // 縁から中心へ飛ぶ
            var no = em.noise; no.enabled = true; no.strength = 1.1f; no.frequency = 1.6f; no.scrollSpeed = 1f;
            var hot = pal.rainbow ? Color.white : anime ? pal.a3 : Color.Lerp(pal.c2, Color.white, 0.5f);
            ColorLife(em, Grad(new[] { (0f, hot), (0.4f, pal.rainbow ? Color.white : pal.c2), (1f, pal.rainbow ? Color.white : pal.c1) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) }));
            SizeLife(em, Curve((0, 1), (1, 0.4f)));
            if (!anime) Trail(em, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.06f, 0.1f));
            c.Play(em, t0);
            c.OnUpdate(t => { if (t >= t1 + 0.12f) em.Clear(); });
            if (palAt != null) c.OnUpdate(t => { var pp = P(t); var mm = em.main; mm.startColor = pp.rainbow ? new MinMaxGradient(RainbowGrad()) { mode = ParticleSystemGradientMode.RandomColor } : new MinMaxGradient(anime ? pp.a3 : pp.c2); });
        }
        // 薄い煙: 上の縁に溜まって昇る（暗く、少しだけ）
        if (c.tx.fbSmoke != null)
        {
            var sm = PS(c, "FireSmoke", new Vector3(0, 3.3f, -2.9f), new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.fbSmoke }, seed + 41);
            var m = sm.main; m.duration = Mathf.Max(t1 - t0, 0.1f); m.startLifetime = new MinMaxCurve(1.2f, 1.8f); m.startSize = new MinMaxCurve(2.5f, 4f);
            m.startRotation = new MinMaxCurve(0, 6.28f); m.startColor = new Color(0.06f, 0.05f, 0.05f, 0.5f);
            var e = sm.emission; e.rateOverTime = 8 * power;
            var sh = sm.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(13f, 0.4f, 0.1f);
            var v = sm.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.1f, 0.1f); v.y = new MinMaxCurve(0.3f, 0.6f); v.z = new MinMaxCurve(0f, 0f);
            ColorLife(sm, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.3f, 1f), (1f, 0f) }));
            var ts = sm.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = 8; ts.numTilesY = 8;
            ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ts.startFrame = new MinMaxCurve(0, 63);
            c.Play(sm, t0);
            c.OnUpdate(t => { if (t >= t1 + 0.12f) sm.Clear(); });
        }
        // 照り返しと陽炎: 炎が点いている間だけ。明るさは 2 つの速さのノイズでちらつかせる
        c.OnUpdate(t =>
        {
            if (t < t0 || t > t1 + 0.12f) return;
            float k = Mathf.Clamp01((t - t0) / 0.1f);
            float fl = 0.75f + 0.25f * Mathf.PerlinNoise(t * 14f, seed * 0.1f) + 0.15f * (Mathf.PerlinNoise(t * 31f, 3.3f) - 0.5f);   // 速いちらつき
            c.post.heat = Mathf.Max(c.post.heat, power * k);
            var pp = P(t);
            var lc = pp.rainbow ? Color.HSVToRGB(Mathf.Repeat(t * 0.5f, 1f), 0.5f, 1f) : pp.light;
            float pw = powerAt != null ? powerAt(t) : power;
            // 照り返し: 炎の色で周りを照らす。段が上がった瞬間は強く
            c.post.fireLight = new Color(lc.r, lc.g, lc.b, 0) * (0.5f * pw * fl * k * (1 + 0.8f * Kick(t))); c.post.fireLight.a = 3.2f;
        });
    }

    // 本物寄りの炎の枠だけ（赤）
    static void FireFrameClip(Ctx c)
    {
        float on = 0.3f;
        FireFrame(c, on, 4.2f, FireBlue, 1f, 500);
        c.OnUpdate(t => { if (t >= on) { c.post.stageDim = Mathf.Max(c.post.stageDim, 0.25f); } if (t >= on && t < on + 2 / 60f) c.post.flash = 0.3f; });
    }

    // ================= 部品集（本人 2026-09-28「いろんなエフェクトのパーツを作っておいて」）=================
    // 1 部品 = 1 クリップ。暗い無地の背景の中央で 1 回（または流しっぱなし）。組み合わせて本番の演出を作る単位
    static readonly Vector3 PO = new Vector3(0, -0.2f, 0);
    static readonly Color PGold = new Color(1f, 0.75f, 0.25f), PCyan = new Color(0.3f, 0.85f, 1f), PRed = new Color(1f, 0.3f, 0.1f), PPurple = new Color(0.7f, 0.35f, 1f);

    static void PartsStage(Ctx c, Clip clip)
    {
        if (!clip.forest)
        {
            // 暗い無地（わずかに青寄り。下ほど暗い）
            var bg = c.root.Find("BG");
            var tex = Tex(4, 64, (u, v) => 1f, (u, v) => Color.Lerp(new Color(0.012f, 0.014f, 0.022f), new Color(0.045f, 0.05f, 0.07f), v), repeat: false);
            bg.GetComponent<MeshRenderer>().sharedMaterial = StageMat(tex, -1f, 1f);
        }
        c.heroT.gameObject.SetActive(clip.hero); c.goblinT.gameObject.SetActive(clip.goblin);
    }

    static ParticleSystem SimplePS(Ctx c, string name, Vector3 pos, Material mat, uint seed, float t0, float dur, float rate, float life0, float life1, float size0, float size1, Color col)
    {
        var ps = PS(c, name, pos, mat, seed);
        var m = ps.main; m.duration = dur; m.startLifetime = new MinMaxCurve(life0, life1); m.startSize = new MinMaxCurve(size0, size1); m.startColor = col;
        var e = ps.emission; e.rateOverTime = rate;
        c.Play(ps, t0); return ps;
    }

    // 稲妻: 折れ線を 1/30 秒ごとに作り直す（形が変わる）。芯は細い HDR、周りに太く薄い光。消えている瞬間も混ぜて明滅させる
    static void Bolt(Ctx c, Vector3 a, Vector3 b, float t0, float t1, Color col, float width, uint seed, int segs = 26, float jag = 0.35f, float blink = 0.2f)
    {
        LineRenderer Line(string n, float w, float inten, Color cc)
        {
            var lr = new GameObject(n).AddComponent<LineRenderer>(); lr.transform.SetParent(c.root, false);
            lr.positionCount = segs + 1; lr.useWorldSpace = true; lr.widthMultiplier = w; lr.sharedMaterial = AddMat(c.tx.trail, inten);
            lr.startColor = lr.endColor = cc; lr.textureMode = LineTextureMode.Stretch; lr.numCapVertices = 2; return lr;
        }
        var core = Line("BoltCore", width, 4.5f, Color.white); var glow = Line("BoltGlow", width * 7f, 0.7f, col);
        var rnd = new System.Random((int)seed); int last = -1; bool vis = false;
        var dir = (b - a); var perp = new Vector3(-dir.y, dir.x, 0).normalized; var pts = new Vector3[segs + 1];
        c.OnUpdate(t =>
        {
            int step = Mathf.FloorToInt(t * 30);
            if (step != last)
            {
                last = step; vis = rnd.NextDouble() > blink;
                float off = 0;
                for (int i = 0; i <= segs; i++)
                {
                    float s = i / (float)segs;
                    off = off * 0.55f + ((float)rnd.NextDouble() * 2 - 1) * jag;
                    pts[i] = Vector3.Lerp(a, b, s) + perp * (off * Mathf.Sin(Mathf.PI * s));
                }
                core.SetPositions(pts); glow.SetPositions(pts);
            }
            bool on = t >= t0 && t < t1 && vis;
            core.enabled = glow.enabled = on;
        });
    }

    static void P01Flare(Ctx c) { Flare(c, PO, 0.25f, PGold, 3.4f, 1001); Glow(c, PO, 0.25f, PGold, 2.2f, 1002); Bokeh(c, PO, 0.25f, PGold, 1.2f, 1003); }
    static void P02Burst(Ctx c) { Burst(c, PO, 0.25f, PRed, 3.6f, 1011); Flare(c, PO, 0.25f, PRed, 2.2f, 1012); Air(c, PO, 0.25f, PRed, 2.6f, 1013); }
    static void P03HitLines(Ctx c) { Flip(c, "HitLines", PO, c.tx.fbHitLines, 6, 4, 1, 12, 0.32f, 5f, new Color(0.8f, 0.95f, 1f), 2.4f, 1021, t: 0.25f); Flare(c, PO, 0.25f, PCyan, 2.2f, 1022); }
    static void P04BigHit(Ctx c) { Flip(c, "BigHit", PO, c.tx.fbBigHit, 6, 5, 1, 12, 0.36f, 5f, Color.white, 1.8f, 1031, t: 0.25f); Ring(c, PO, 0.25f, PGold, 4.5f, 1032, delay: 0.05f); }
    static void P05StarExp(Ctx c) { Flip(c, "StarExp", PO, c.tx.fbStarExp, 7, 6, 1, 30, 0.8f, 4.5f, Color.white, 1.6f, 1041, t: 0.25f, randomRot: false); }
    static void P06Ring(Ctx c) { Ring(c, PO, 0.25f, PCyan, 5.5f, 1051); Ring(c, PO, 0.25f, Color.white, 3.5f, 1052, delay: 0.07f); Flare(c, PO, 0.25f, PCyan, 1.8f, 1053); }
    static void P07GroundRing(Ctx c)
    {
        var g = new Vector3(0, GroundY + 0.2f, -0.6f);
        Ring(c, g, 0.25f, PGold, 6.5f, 1061, squash: 0.25f); Ring(c, g, 0.25f, Color.white, 4f, 1062, squash: 0.25f, delay: 0.06f);
        Flip(c, "GroundDust", g + new Vector3(0, 0.3f, 0.2f), c.tx.fbSmoke, 8, 8, 0, 40, 0.8f, 3.2f, new Color(0.5f, 0.45f, 0.4f, 0.5f), 1f, 1063, alpha: true, t: 0.27f);
    }
    // 空間の歪み: 輪が広がる所だけ背景がレンズのように押し出される（LabBloom の _Shock）
    static void P08Shock(Ctx c)
    {
        c.OnUpdate(t =>
        {
            float a = t - 0.25f; if (a < 0 || a > 0.7f) return;
            float r = EaseOut(Mathf.Clamp01(a / 0.6f)) * 0.75f;
            c.post.shock = new Vector4(0.5f, 0.47f, r, 1.4f * (1 - a / 0.7f));
        });
        Ring(c, PO, 0.25f, new Color(0.6f, 0.8f, 1f) * 0.5f, 9f, 1071); Flare(c, PO, 0.25f, Color.white, 2.2f, 1072);
    }
    static void P09FireRing(Ctx c) { Flip(c, "FireRing", PO, c.tx.fbFireRing, 6, 5, 0, 30, 1.0f, 5.5f, Color.white, 1.4f, 1081, t: 0.25f, randomRot: false); }
    static void P10ElecRing(Ctx c)
    {
        Flip(c, "ElecRing", PO, c.tx.fbElecRing, 6, 5, 0, 30, 0.9f, 4f, Color.white, 1.4f, 1091, t: 0.25f);
        Flip(c, "ElecRing2", PO, c.tx.fbElecRing, 6, 5, 10, 30, 0.6f, 5.5f, Color.white, 0.9f, 1092, t: 0.45f);
    }
    static void P11Sparks(Ctx c) { RealSparks(c, PO + new Vector3(0, 0.3f, 0), 70, 0.25f, 90, 12f, 1101); Flare(c, PO + new Vector3(0, 0.3f, 0), 0.25f, PGold, 1.6f, 1102); }
    static void P12Shards(Ctx c) { Shards(c, PO, 0.25f, new Color(1f, 0.25f, 0.55f), 1.5f, 0, 360, 44, 1111); Flare(c, PO, 0.25f, new Color(1f, 0.3f, 0.6f), 2f, 1112); }
    static void P13Sparkle(Ctx c) { Bokeh(c, PO, 0.25f, PGold, 2.6f, 1121); Glint(c, PO + new Vector3(-0.6f, 0.5f, -1f), 0.35f, PGold); Glint(c, PO + new Vector3(0.7f, -0.3f, -1f), 0.5f, PGold); }
    static void P14Motes(Ctx c)
    {
        var ps = SimplePS(c, "Motes", new Vector3(0, GroundY + 0.2f, 0), AddMat(c.tx.dot, 5f), 1131, 0.1f, 3f, 45, 1.2f, 1.9f, 0.05f, 0.12f, new Color(0.7f, 1f, 0.6f));
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3.2f, 0.2f, 0.2f);
        var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.1f, 0.1f); v.y = new MinMaxCurve(0.8f, 1.8f); v.z = new MinMaxCurve(0f, 0f);
        var n = ps.noise; n.enabled = true; n.strength = 0.4f; n.frequency = 1f;
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, new Color(0.4f, 1f, 0.5f)) }, new[] { (0f, 0f), (0.15f, 1f), (0.7f, 0.8f), (1f, 0f) }));
    }
    static void P15Slash(Ctx c)
    {
        SlashThrough(c, PO, -30, 25, 2.6f, 1.15f, 165, Cyan, 0.25f);
        SlashThrough(c, PO + new Vector3(0.1f, 0.1f, 0), -30, 25, 2.6f, 0.8f, 165, Cyan.Ghost(), 0.283f, fade: 0.45f, thick: 0.85f);
    }
    static void P16CutLine(Ctx c) { CutLine(c, PO, -18, 8f, 0.25f, PCyan); Flare(c, PO, 0.25f, PCyan, 1.6f, 1151); }
    // 軌跡のリボン: 光の玉が弧を描き、尾が残る
    static void P17Trail(Ctx c)
    {
        var ps = PS(c, "TrailOrbs", PO, AddMat(c.tx.dot, 4f), 1161);
        var m = ps.main; m.startLifetime = 1.3f; m.startSize = 0.22f; m.startColor = PPurple; m.simulationSpace = ParticleSystemSimulationSpace.World;
        ps.emission.SetBursts(new[] { new Burst(0, 3) });
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.6f; sh.radiusThickness = 0; sh.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
        var v = ps.velocityOverLifetime; v.enabled = true; v.orbitalZ = new MinMaxCurve(4.5f); v.radial = new MinMaxCurve(-0.6f);
        Trail(ps, AddMat(c.tx.trail, 3.5f), new MinMaxCurve(0.45f, 0.45f));
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, PPurple) }, new[] { (0f, 1f), (0.8f, 1f), (1f, 0f) }));
        c.Play(ps, 0.25f);
    }
    // 炎（焚き火）: 炎の連番を温度の色で塗る＋炎の床＋火の粉＋煙＋照り返し・陽炎
    static void P18Flame(Ctx c)
    {
        var a = new Vector3(-1.1f, GroundY + 0.9f, -0.5f); var b = new Vector3(1.1f, GroundY + 0.9f, -0.5f);
        var fire = c.tx.blCampfire ?? c.tx.fbFlame;
        EdgeFire(c, "FlameBed", a + new Vector3(0, -0.35f, 0), b + new Vector3(0, -0.35f, 0), Vector2.up, 26f, 0.55f, 0.8f, FireMat(c, c.tx.fireFlame03 ?? c.tx.fbFlame, FireRed, 1.3f), 1171, 0.1f, 9f, false, life0: 0.35f, life1: 0.5f);
        EdgeFire(c, "Flame", a, b, Vector2.up, 9f, 1.2f, 1.9f, FireMat(c, fire, FireRed, 1.4f), 1172, 0.1f, 9f, false);
        var em = SimplePS(c, "FlameEmbers", new Vector3(0, GroundY + 0.6f, -0.6f), AddMat(c.tx.dot, 4f), 1173, 0.1f, 9f, 30, 0.8f, 1.8f, 0.02f, 0.045f, Color.white);
        var sh = em.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2f, 0.2f, 0.2f);
        var v = em.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.4f, 0.4f); v.y = new MinMaxCurve(1.5f, 3.5f); v.z = new MinMaxCurve(0f, 0f);
        var n = em.noise; n.enabled = true; n.strength = 1.2f; n.frequency = 1.6f;
        ColorLife(em, Grad(new[] { (0f, new Color(1f, 0.9f, 0.6f)), (0.5f, new Color(1f, 0.45f, 0.1f)), (1f, new Color(0.6f, 0.08f, 0.02f)) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) }));
        Trail(em, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.06f, 0.1f));
        // 炎の明かり: 足元に暖かい光がちらつく
        var lg = Quad(c.root, "FlameLight", new Vector3(0, GroundY + 0.9f, 0.3f), new Vector2(7f, 4.5f), QMat(c, c.tx.glow, 0, Vector2.one, 0));
        var lm = lg.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { float fl = 0.7f + 0.3f * Mathf.PerlinNoise(t * 14f, 1.1f); lm.SetColor("_Tint", new Color(1f, 0.4f, 0.1f) * (t < 0.1f ? 0 : 0.45f * fl)); });
    }
    static void P19Embers(Ctx c)
    {
        var em = SimplePS(c, "Embers", new Vector3(0, GroundY + 0.3f, -0.6f), AddMat(c.tx.dot, 4.5f), 1181, 0.1f, 9f, 60, 1f, 2.2f, 0.02f, 0.05f, Color.white);
        var sh = em.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(6f, 0.2f, 0.2f);
        var v = em.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.3f, 0.8f); v.y = new MinMaxCurve(1.2f, 3f); v.z = new MinMaxCurve(0f, 0f);
        var n = em.noise; n.enabled = true; n.strength = 1.4f; n.frequency = 1.4f;
        ColorLife(em, Grad(new[] { (0f, new Color(1f, 0.95f, 0.7f)), (0.4f, new Color(1f, 0.55f, 0.15f)), (1f, new Color(0.8f, 0.12f, 0.02f)) }, new[] { (0f, 1f), (0.75f, 1f), (1f, 0f) }));
        SizeLife(em, Curve((0, 1), (1, 0.4f)));
        Trail(em, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.08f, 0.12f));
    }
    static void P20Smoke(Ctx c)
    {
        var sm = SimplePS(c, "Smoke", new Vector3(0, GroundY + 0.8f, -0.3f), new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.fbSmoke }, 1191, 0.1f, 9f, 6, 1.8f, 2.6f, 2.2f, 3.4f, new Color(0.85f, 0.85f, 0.9f, 0.8f));
        var m = sm.main; m.startRotation = new MinMaxCurve(0, 6.28f);
        var sh = sm.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.5f, 0.2f, 0.2f);
        var v = sm.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(0.1f, 0.4f); v.y = new MinMaxCurve(0.8f, 1.3f); v.z = new MinMaxCurve(0f, 0f);
        SizeLife(sm, Curve((0, 0.5f), (1, 1.4f)));
        ColorLife(sm, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.2f, 1f), (1f, 0f) }));
        var ts = sm.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = 8; ts.numTilesY = 8;
        ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ts.startFrame = new MinMaxCurve(0, 63);
    }
    static void P21Dust(Ctx c)
    {
        var g = new Vector3(0, GroundY + 0.5f, -0.4f);
        for (int k = 0; k < 4; k++)
            Flip(c, "Dust" + k, g + new Vector3((k - 1.5f) * 0.7f, k % 2 * 0.2f, 0), c.tx.fbSmoke, 8, 8, k * 8, 40 + k * 8, 1.1f, 2.6f, new Color(0.95f, 0.82f, 0.66f, 0.8f), 1.3f, 1201 + (uint)k, delay: k * 0.02f, alpha: true, t: 0.25f);
        var deb = PS(c, "Debris", g, new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 1206);
        var m = deb.main; m.startLifetime = new MinMaxCurve(0.6f, 1.1f); m.startSpeed = new MinMaxCurve(3f, 7f); m.startSize = new MinMaxCurve(0.04f, 0.1f); m.gravityModifier = 1.4f; m.startColor = new Color(0.75f, 0.64f, 0.5f);
        deb.emission.SetBursts(new[] { new Burst(0, 40) });
        var sh = deb.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 50; deb.transform.rotation = Quaternion.LookRotation(Vector3.up);
        c.Play(deb, 0.25f);
    }
    static void P22Bolt(Ctx c)
    {
        Lightning(c, new Vector3(0.5f, 4.2f, -1f), new Vector3(-0.2f, GroundY + 0.1f, -1f), 0.25f, new Color(0.45f, 0.6f, 1f), 1211);
    }
    static void P23Arcs(Ctx c)
    {
        // 一点から放射する放電: 短い稲妻を時間をずらして 5 本（強弱のある枝つき）
        for (int k = 0; k < 5; k++)
        {
            float ang = (k * 72f + 15f) * Mathf.Deg2Rad;
            var b = PO + new Vector3(Mathf.Cos(ang) * 2.0f, Mathf.Sin(ang) * 1.6f, -1f);
            Lightning(c, PO + new Vector3(0, 0, -1f), b, 0.2f + k * 0.18f, new Color(0.65f, 0.45f, 1f), 1221 + (uint)k * 10, width: 0.045f, branches: 3, scale: 0.8f, impact: false);
        }
        Glow(c, PO, 0.2f, PPurple, 2.5f, 1228);
        Flare(c, PO, 0.2f, PPurple, 2.2f, 1229);
    }
    static void P24Charge(Ctx c)
    {
        Flip(c, "Charge1", PO, c.tx.fbCharge, 7, 6, 0, 11, 0.5f, 4.5f, PCyan, 2.4f, 1231, t: 0.2f, randomRot: false);
        Flip(c, "Charge2", PO, c.tx.fbCharge, 7, 6, 0, 11, 0.5f, 4.5f, PCyan, 2.4f, 1232, t: 0.7f, randomRot: false);
        var ps = PS(c, "Converge", PO, AddMat(c.tx.diamond, 3f), 1233);
        var m = ps.main; m.duration = 1f; m.startLifetime = 0.5f; m.startSpeed = -4f; m.startSize = new MinMaxCurve(0.05f, 0.1f); m.startColor = PCyan;
        var e = ps.emission; e.rateOverTime = 70;
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 2.2f; sh.radiusThickness = 0;
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
        c.Play(ps, 0.2f);
        Flare(c, PO, 1.2f, PCyan, 3.2f, 1234); Ring(c, PO, 1.2f, PCyan, 5f, 1235, delay: 0.04f);
    }
    static void P25Vortex(Ctx c) { Flip(c, "Vortex", PO, c.tx.fbVortex, 6, 5, 1, 23, 1.0f, 5f, Color.white, 1.5f, 1241, t: 0.25f, randomRot: false); }
    static void P26Magic(Ctx c)
    {
        var mc = Quad(c.root, "MagicCircle", new Vector3(0, GroundY + 0.3f, 0.2f), new Vector2(4.2f, 4.2f), QMat(c, c.tx.magic, 0, Vector2.one, 0));
        var lr = Quad(c.root, "LightRing", new Vector3(0, GroundY + 0.3f, 0.19f), new Vector2(4.8f, 4.8f), QMat(c, c.tx.lightRing ?? c.tx.ring, 0, Vector2.one, 0));
        var mm = mc.GetComponent<MeshRenderer>().sharedMaterial; var lm = lr.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t =>
        {
            float e = Mathf.Clamp01((t - 0.2f) / 0.25f), burst = t < 0.2f ? 0 : Mathf.Exp(-(t - 0.2f) * 5f);
            mc.rotation = Quaternion.Euler(72, 0, 0) * Quaternion.Euler(0, 0, t * 50f); lr.rotation = Quaternion.Euler(72, 0, 0) * Quaternion.Euler(0, 0, -t * 30f);
            mm.SetColor("_Tint", PPurple * (e * (1.2f + 2f * burst))); lm.SetColor("_Tint", PPurple * (e * (0.5f + 1f * burst)));
        });
        P14Motes(c);
    }
    static void P27Pillar(Ctx c)
    {
        var beam = Quad(c.root, "Pillar", new Vector3(0, 1.2f, 0.3f), new Vector2(2.6f, 7.6f), QMat(c, c.tx.column, 0.6f, new Vector2(2, 1), 0.8f));
        var bm = beam.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { float up = Mathf.Clamp01((t - 0.2f) / 0.15f); float fl = 0.9f + 0.1f * Mathf.Sin(t * 30f); bm.SetColor("_Tint", new Color(1f, 0.95f, 0.8f) * (1.5f * up * fl)); });
        Ring(c, new Vector3(0, GroundY + 0.2f, -0.6f), 0.2f, PGold, 5f, 1261, squash: 0.25f); Flare(c, new Vector3(0, GroundY + 0.3f, -0.6f), 0.2f, PGold, 2.6f, 1262);
        P14Motes(c);
    }
    // 揺らめくオーラ（描き起こしの揺らぎの連番をキャラの後ろに）
    static void P28Aura(Ctx c)
    {
        c.heroT.position = new Vector3(0, -0.84f, 0);
        var ps = SimplePS(c, "WavyAura", new Vector3(0, -0.5f, 0.3f), AddMat(c.tx.fbWavy, 1.3f), 1271, 0.2f, 9f, 4, 0.9f, 1.1f, 3.4f, 3.8f, PCyan);
        var ts = ps.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = 6; ts.numTilesY = 5;
        ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ts.startFrame = new MinMaxCurve(0, 29);
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.25f, 1f), (0.75f, 1f), (1f, 0f) }));
        Surge(c, 0.2f, new Vector3(0, -0.5f, 0), new Vector3(0, GroundY + 0.02f, 0), PCyan, 0.8f);
    }

    // ================= 漫画の擬音（本人 2026-09-28「コミックの擬音エフェクトも欲しい」）=================
    // 出る: 2 倍から跳ね返って 1 倍（0.12 秒）・2F 白・傾きが戻る / 出ている間: 2F ごとに小刻みに震える / 消える: 少し膨らんで 0.12 秒で消える
    static void Sfx(Ctx c, Texture2D tex, Vector3 pos, float t0, float rotDeg, float height, float hold, float shake = 0.035f, uint seed = 1)
    {
        var size = new Vector2(height * tex.width / tex.height, height);
        var m = new Material(Shader.Find("Lab/Sprite")) { mainTexture = tex };
        var q = Quad(c.root, "Sfx", pos + new Vector3(0, 0, -3.5f), size, m);
        var r = new System.Random((int)seed);
        Vector3 jit = Vector3.zero; int lastStep = -1;
        c.OnUpdateReal(rt =>
        {
            float a = rt - c.Real(t0);
            if (a < 0 || a > hold + 0.14f) { q.gameObject.SetActive(false); return; }
            q.gameObject.SetActive(true);
            float sIn = a < 0.12f ? Mathf.LerpUnclamped(2.0f, 1f, EaseOutBack(a / 0.12f)) : 1f;
            float ex = Mathf.Clamp01((a - hold) / 0.12f);
            int step = Mathf.FloorToInt(a * 30);
            if (step != lastStep) { lastStep = step; jit = new Vector3(((float)r.NextDouble() - 0.5f) * 2, ((float)r.NextDouble() - 0.5f) * 2, 0) * shake; }
            q.localScale = new Vector3(size.x, size.y, 1) * (sIn * (1 + 0.25f * ex));
            q.position = pos + new Vector3(0, 0, -3.5f) + (a > 0.12f ? jit : Vector3.zero);
            q.rotation = Quaternion.Euler(0, 0, rotDeg + (a < 0.12f ? 14f * (1 - a / 0.12f) : 0));
            m.SetFloat("_Flash", a < 2 / 60f ? 1f : 0f);
            m.SetColor("_Color", new Color(1, 1, 1, 1 - ex));
        });
    }

    static void P31SfxDon(Ctx c)
    {
        Flip(c, "BigHit", PO, c.tx.fbBigHit, 6, 5, 1, 12, 0.36f, 5f, Color.white, 1.8f, 1311, t: 0.25f);
        Flip(c, "HitLines", PO, c.tx.fbHitLines, 6, 4, 1, 12, 0.32f, 6f, new Color(1f, 0.9f, 0.6f), 2f, 1312, t: 0.25f);
        c.OnUpdate(t => { float a = t - 0.25f; if (a >= 0 && a < 0.35f) c.post.trauma += 0.7f * (1 - a / 0.35f); if (a >= 0 && a < 0.03f) c.post.flash = 0.35f; });
        Sfx(c, c.tx.sfxDon, PO + new Vector3(0.6f, 1.2f, 0), 0.27f, 8f, 2.2f, 0.7f, 0.05f, 1313);
    }
    static void P32SfxZuba(Ctx c)
    {
        SlashThrough(c, PO, -30, 25, 2.6f, 1.15f, 165, Cyan, 0.25f);
        CutLine(c, PO, -30, 8f, 0.3f, PCyan);
        Sfx(c, c.tx.sfxZuba, PO + new Vector3(-0.8f, 1.4f, 0), 0.3f, 12f, 1.9f, 0.6f, 0.03f, 1321);
    }
    static void P33SfxBari(Ctx c)
    {
        Lightning(c, new Vector3(0.9f, 4.2f, -1f), new Vector3(0.2f, GroundY + 0.1f, -1f), 0.25f, new Color(0.45f, 0.6f, 1f), 1331);
        Sfx(c, c.tx.sfxBari, PO + new Vector3(-1.6f, 1.0f, 0), 0.27f, -6f, 1.4f, 0.8f, 0.07f, 1332);
    }
    static void P34SfxGogo(Ctx c)
    {
        // ゴゴゴ: 画面を暗くし、「ゴ」を時間差で置いて震わせ続ける（大きさも位置もばらす）
        c.OnUpdate(t => { if (t >= 0.2f) { c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f); c.post.darken = 0.6f; } });
        var spots = new[] { (-4.2f, 1.6f, 1.6f, -8f), (-2.2f, 2.3f, 1.2f, 6f), (2.6f, 1.9f, 1.4f, -4f), (4.4f, 0.9f, 1.0f, 10f), (-3.4f, -0.4f, 1.1f, 4f), (3.6f, -1.2f, 1.3f, -10f) };
        for (int k = 0; k < spots.Length; k++)
        {
            var (x, y, h, rot) = spots[k];
            Sfx(c, c.tx.sfxGo, new Vector3(x, y, 0), 0.25f + k * 0.12f, rot, h, 2.2f - k * 0.12f, 0.045f, 1341 + (uint)k);
        }
    }
    static void P35SfxKira(Ctx c)
    {
        Bokeh(c, PO, 0.25f, new Color(1f, 0.7f, 0.9f), 2.2f, 1351); Flare(c, PO + new Vector3(0.8f, 0.6f, 0), 0.25f, new Color(1f, 0.7f, 0.9f), 2.8f, 1352);
        Glint(c, PO + new Vector3(-0.8f, 0.2f, -1f), 0.35f, Color.white); Glint(c, PO + new Vector3(1.2f, -0.4f, -1f), 0.5f, Color.white);
        Sfx(c, c.tx.sfxKira, PO + new Vector3(0, 1.3f, 0), 0.27f, -5f, 1.3f, 0.9f, 0.015f, 1353);
    }

    // 部品のクリップ一覧（名前・秒・舞台）
    static IEnumerable<Clip> PartClips()
    {
        Clip P(string n, float d, Action<Ctx> b, bool forest = false, bool hero = false, bool goblin = false) => new Clip { name = n, dur = d, hold = 1, build = b, part = true, forest = forest, hero = hero, goblin = goblin };
        yield return P("p01_flare", 1.2f, P01Flare);
        yield return P("p02_burst", 1.2f, P02Burst);
        yield return P("p03_hitlines", 1.2f, P03HitLines);
        yield return P("p04_bighit", 1.2f, P04BigHit);
        yield return P("p05_starexp", 1.4f, P05StarExp);
        yield return P("p06_ring", 1.2f, P06Ring);
        yield return P("p07_groundring", 1.4f, P07GroundRing);
        yield return P("p08_shock", 1.4f, P08Shock, forest: true);
        yield return P("p09_firering", 1.6f, P09FireRing);
        yield return P("p10_elecring", 1.4f, P10ElecRing);
        yield return P("p11_sparks", 2.0f, P11Sparks);
        yield return P("p12_shards", 1.2f, P12Shards);
        yield return P("p13_sparkle", 1.6f, P13Sparkle);
        yield return P("p14_motes", 2.4f, P14Motes);
        yield return P("p15_slash", 1.2f, P15Slash);
        yield return P("p16_cutline", 1.0f, P16CutLine);
        yield return P("p17_trail", 1.8f, P17Trail);
        yield return P("p18_flame", 2.6f, P18Flame);
        yield return P("p19_embers", 2.4f, P19Embers);
        yield return P("p20_smoke", 3.0f, P20Smoke);
        yield return P("p21_dust", 1.6f, P21Dust);
        yield return P("p22_bolt", 1.2f, P22Bolt);
        yield return P("p23_arcs", 2.0f, P23Arcs);
        yield return P("p24_charge", 1.8f, P24Charge);
        yield return P("p25_vortex", 1.6f, P25Vortex);
        yield return P("p26_magic", 2.4f, P26Magic);
        yield return P("p27_pillar", 2.2f, P27Pillar);
        yield return P("p28_aura", 2.4f, P28Aura, hero: true);
        yield return P("p29_shield", 3.2f, ShieldClip, hero: true);
        yield return P("p30_dissolve", 2.0f, DeathDissolve, goblin: true);
        yield return P("p31_sfx_don", 1.4f, P31SfxDon);
        yield return P("p32_sfx_zuba", 1.3f, P32SfxZuba);
        yield return P("p33_sfx_bari", 1.4f, P33SfxBari);
        yield return P("p34_sfx_gogo", 2.8f, P34SfxGogo, forest: true, hero: true, goblin: true);
        yield return P("p35_sfx_kira", 1.5f, P35SfxKira);
    }

    // ================= 稲妻（強弱のある作り。本人 2026-09-28「電撃とか細い箇所・大きい箇所もない」）=================
    // 幹 → 枝 → 枝の枝で太さと明るさを段にする（幹 1 : 枝 0.45 : 枝の枝 0.2、明るさ 1 : 0.6 : 0.35）。どれも先へ細る
    // 形は大きな折れ → 細かいギザギザの順に中点をずらして作る（段ごとにずれを 0.55 倍）
    // 光り方は本物の雷の「再発光」: 光る → 消える（残光）→ また光る を 2〜3 回。2 回目以降は幹と一部の枝だけ
    static List<Vector3> Jagged(Vector3 a, Vector3 b, float disp, int levels, System.Random r)
    {
        var pts = new List<Vector3> { a, b };
        for (int l = 0; l < levels; l++)
        {
            var np = new List<Vector3>(pts.Count * 2);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p = pts[i]; var q = pts[i + 1]; var d = q - p;
                var perp = new Vector3(-d.y, d.x, 0).normalized;
                np.Add(p); np.Add((p + q) * 0.5f + perp * (((float)r.NextDouble() * 2 - 1) * disp));
            }
            np.Add(pts[pts.Count - 1]); pts = np; disp *= 0.55f;
        }
        return pts;
    }

    class Channel { public List<Vector3> pts; public int gen; public LineRenderer core, glow; public float seedT; }

    static void Lightning(Ctx c, Vector3 a, Vector3 b, float t0, Color glowCol, uint seed, float width = 0.075f, int branches = 6, float scale = 1f, bool impact = true)
    {
        var r = new System.Random((int)seed);
        float R() => (float)r.NextDouble();
        var chans = new List<Channel>();
        float len = (b - a).magnitude;
        var main = Jagged(a, b, len * 0.16f, 7, r);
        chans.Add(new Channel { pts = main, gen = 0 });
        // 枝: 幹の 15〜80% の位置から、幹の向きを 20〜50° ずらして伸ばす。長さは残りの 25〜55%
        for (int k = 0; k < branches; k++)
        {
            int i0 = Mathf.Clamp((int)(main.Count * (0.15f + R() * 0.65f)), 1, main.Count - 2);
            var from = main[i0]; var dir = (main[Mathf.Min(i0 + 4, main.Count - 1)] - main[i0 - 1]).normalized;
            float ang = (R() < 0.5f ? -1 : 1) * (20 + R() * 30) * Mathf.Deg2Rad;
            var bd = new Vector3(dir.x * Mathf.Cos(ang) - dir.y * Mathf.Sin(ang), dir.x * Mathf.Sin(ang) + dir.y * Mathf.Cos(ang), 0);
            float bl = len * (1 - i0 / (float)main.Count) * (0.25f + R() * 0.3f);
            var bp = Jagged(from, from + bd * bl, bl * 0.2f, 5, r);
            chans.Add(new Channel { pts = bp, gen = 1 });
            if (R() < 0.55f)
            {
                int j0 = Mathf.Clamp((int)(bp.Count * (0.3f + R() * 0.4f)), 1, bp.Count - 2);
                float ang2 = (R() < 0.5f ? -1 : 1) * (25 + R() * 30) * Mathf.Deg2Rad;
                var d2 = (bp[j0 + 1] - bp[j0 - 1]).normalized;
                var bd2 = new Vector3(d2.x * Mathf.Cos(ang2) - d2.y * Mathf.Sin(ang2), d2.x * Mathf.Sin(ang2) + d2.y * Mathf.Cos(ang2), 0);
                float bl2 = bl * (0.3f + R() * 0.25f);
                chans.Add(new Channel { pts = Jagged(bp[j0], bp[j0] + bd2 * bl2, bl2 * 0.22f, 4, r), gen = 2 });
            }
        }
        float[] wGen = { 1f, 0.45f, 0.2f }, bGen = { 1f, 0.6f, 0.35f };
        foreach (var ch in chans)
        {
            LineRenderer Line(string n, float inten)
            {
                var lr = new GameObject(n).AddComponent<LineRenderer>(); lr.transform.SetParent(c.root, false);
                lr.useWorldSpace = true; lr.positionCount = ch.pts.Count; lr.SetPositions(ch.pts.ToArray());
                lr.sharedMaterial = AddMat(c.tx.trail, inten); lr.textureMode = LineTextureMode.Stretch; lr.numCapVertices = 2; lr.numCornerVertices = 1;
                // 太さ: 先へ細る。途中にムラ（0.6〜1.25 倍）。幹の根元は太く、枝は先で 0
                var keys = new List<Keyframe>();
                for (int k = 0; k <= 8; k++)
                {
                    float s = k / 8f;
                    float taper = ch.gen == 0 ? Mathf.Lerp(1f, 0.35f, s) : Mathf.Pow(1 - s, 0.8f);
                    keys.Add(new Keyframe(s, taper * (0.6f + 0.65f * R())));
                }
                lr.widthCurve = new AnimationCurve(keys.ToArray());
                return lr;
            }
            ch.core = Line("BoltCore", 4.2f * bGen[ch.gen]); ch.core.widthMultiplier = width * scale * wGen[ch.gen];
            ch.glow = Line("BoltGlow", 0.75f * bGen[ch.gen]); ch.glow.widthMultiplier = width * scale * wGen[ch.gen] * 7f;
            ch.core.startColor = ch.core.endColor = Color.white; ch.glow.startColor = ch.glow.endColor = glowCol;
            ch.seedT = R();
        }
        // 再発光の時刻（60fps のコマ）: 1 回目 0〜3 / 2 回目 6〜9（幹＋枝の半分）/ 3 回目 13〜15（幹だけ）/ 残光は 25 コマで消える
        var strokes = new[] { (0, 3, 1f, 2), (6, 9, 0.8f, 1), (13, 15, 0.6f, 0) };
        var jitter = new System.Random((int)seed + 7);
        int lastStroke = -1;
        c.OnUpdate(t =>
        {
            int f = Mathf.FloorToInt((t - t0) * 60f);
            float bright = 0; int maxGen = -1, si = -1;
            for (int s = 0; s < strokes.Length; s++) if (f >= strokes[s].Item1 && f < strokes[s].Item2) { bright = strokes[s].Item3; maxGen = strokes[s].Item4; si = s; }
            float after = f < 0 ? 0 : Mathf.Clamp01(1 - (f - 3) / 25f) * 0.35f;   // 残光（幹の周りの光だけ）
            if (si != lastStroke && si >= 0)
            {
                lastStroke = si;
                // 2 回目以降は同じ道筋を少しだけずらす（形はほぼ同じ）
                foreach (var ch in chans)
                {
                    var p = ch.pts.ToArray();
                    for (int i = 1; i < p.Length - 1; i++) p[i] += new Vector3(((float)jitter.NextDouble() - 0.5f) * 0.04f, ((float)jitter.NextDouble() - 0.5f) * 0.04f, 0);
                    ch.core.SetPositions(p); ch.glow.SetPositions(p);
                }
            }
            foreach (var ch in chans)
            {
                bool lit = bright > 0 && ch.gen <= maxGen && (ch.gen == 0 || ch.seedT < (si == 0 ? 1f : 0.5f));
                ch.core.enabled = lit;
                ch.core.startColor = ch.core.endColor = Color.white * (lit ? bright : 0);
                float g = lit ? bright : (ch.gen == 0 ? after : 0);
                ch.glow.enabled = g > 0.01f;
                ch.glow.startColor = ch.glow.endColor = glowCol * g;
            }
            if (bright > 0) { c.post.flash = Mathf.Max(c.post.flash, 0.25f * bright); c.post.trauma += 0.35f * bright; }
        });
        if (impact)
        {
            Flare(c, b, t0, glowCol, 3.2f * scale, seed + 1);
            Flip(c, "BoltRing" + seed, b + new Vector3(0, 0.15f, -0.2f), c.tx.fbElecRing, 6, 5, 0, 12, 0.35f, 3f * scale, Color.white, 1.4f, seed + 2, t: t0 + 0.02f);
            var sp = PS(c, "BoltSparks" + seed, b, AddMat(c.tx.diamond, 3f), seed + 3);
            var m = sp.main; m.startLifetime = new MinMaxCurve(0.15f, 0.4f); m.startSpeed = new MinMaxCurve(3f, 9f); m.startSize = new MinMaxCurve(0.04f, 0.09f); m.startColor = Color.Lerp(glowCol, Color.white, 0.5f); m.gravityModifier = 0.6f;
            sp.emission.SetBursts(new[] { new Burst(0, 30), new Burst(0.1f, 14), new Burst(0.22f, 8) });
            var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 70; sp.transform.rotation = Quaternion.LookRotation(Vector3.up);
            var rr = sp.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = 0.04f; rr.lengthScale = 1.5f;
            ColorLife(sp, Grad(new[] { (0f, Color.white), (1f, glowCol) }, new[] { (0f, 1f), (1f, 0f) }));
            c.Play(sp, t0);
        }
    }

    // ================= カットイン（本人 2026-09-29「カットインの演出のエフェクト色々」）=================
    // 流れ: 白 2F・背景を沈める → 帯が上下から開く（6F・跳ね返る）→ キャラが残像を引いて滑り込む（7F・行き過ぎて戻る）→ 止まって揺れ・ゆっくり流れる
    //       → 抜け: キャラが反対へ飛び出し（7F）、帯が閉じる（6F）
    class CutStyle { public float mode; public Color bas, c1, c2, c3; public float angle = -9f, speed = 3f; public bool chevron, bolts, sparkle, focus; public Color chevA, chevB; }

    // 立ち絵（本人 2026-09-29「立ち絵があるよね？」）: 背景が透明な全身の絵から、胸から上を切り出す（keep = 上から残す割合）
    static Texture2D StandTex(string rel, float keep)
    {
        string dir = Environment.GetEnvironmentVariable("LAB_TEX");
        var p = Path.GetFullPath(Path.Combine(dir, "../../../", rel));
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false); src.LoadImage(File.ReadAllBytes(p));
        int w = src.width, h = Mathf.RoundToInt(src.height * keep);
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
        t.SetPixels(src.GetPixels(0, src.height - h, w, h)); t.filterMode = FilterMode.Trilinear; t.wrapMode = TextureWrapMode.Clamp; t.Apply(true);
        return t;
    }

    static Texture2D CutTex(Ctx c, int n)
    {
        string dir = Environment.GetEnvironmentVariable("LAB_TEX");
        var p = Path.GetFullPath(Path.Combine(dir, $"../../../UnityProject/BigBonusBlitz/Assets/Resources/Art/UI/CutIn/cut_salia_0{n}.png"));
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, true); t.LoadImage(File.ReadAllBytes(p)); t.filterMode = FilterMode.Trilinear; t.wrapMode = TextureWrapMode.Clamp; t.Apply(true); return t;
    }

    static Material CutBandMat(Ctx c, CutStyle st, float mode, float seed)
    {
        var m = new Material(Shader.Find("Lab/CutBand")); m.SetTexture("_NoiseTex", c.tx.noise);
        m.SetFloat("_Mode", mode); m.SetColor("_Base", st.bas); m.SetColor("_C1", st.c1); m.SetColor("_C2", st.c2); m.SetColor("_C3", st.c3);
        m.SetFloat("_Speed", st.speed); m.SetFloat("_Seed", seed); m.SetFloat("_Open", 0);
        return m;
    }

    static void CutIn(Ctx c, CutStyle st, Texture2D chr, float t0, float hold = 1.15f, float charH = 0f, Vector2 charOff = default, (float t, CutStyle st, string label)[] ups = null,
        Vector3 shift = default, float side = 1f, float bandH = 4.3f)
    {
        float tIn = t0 + 0.05f, tOut = t0 + hold;
        var bandPos = new Vector3(0, -0.35f, -3.4f) + shift;
        var rot = Quaternion.Euler(0, 0, st.angle);
        // 帯
        Material band = null, chev = null; Transform bandT = null, chevT = null;
        if (!st.focus)
        {
            band = CutBandMat(c, st, st.mode, 1.3f);
            bandT = Quad(c.root, "CutBand", bandPos, new Vector2(17f, bandH), band); bandT.rotation = rot; band.SetFloat("_Aspect", 17f / bandH);
            if (st.chevron)
            {
                var cs = new CutStyle { bas = st.bas * 0.6f, c1 = st.chevA, c2 = st.chevB, c3 = st.c3, speed = st.speed };
                chev = CutBandMat(c, cs, 3, 2.1f);
                chevT = Quad(c.root, "CutChevron", bandPos + rot * new Vector3(0, -2.7f, -0.01f), new Vector2(17f, 0.8f), chev); chevT.rotation = rot; chev.SetFloat("_Aspect", 17f / 0.8f);
            }
        }
        // キャラと残像（残像は帯の色で塗って、遅れて付いてくる）
        // 立ち絵（charH > 0）は高さ charH で絵の比率のまま。カットイン絵は画面の 7 割（帯が見えるように）
        var size = charH > 0 ? new Vector2(charH * chr.width / chr.height, charH) : new Vector2(12.8f, 7.2f) * 0.7f;
        Material CharMat(float alpha, Color tint, float outline, int queue)
        {
            var m = new Material(Shader.Find("Lab/CutChar")) { mainTexture = chr };
            m.SetFloat("_Alpha", alpha); m.SetColor("_Tint", tint); m.SetFloat("_Outline", outline); m.renderQueue = queue;
            m.SetFloat("_EdgeX", charH > 0 ? 0.0001f : 1f);
            if (outline <= 0) m.SetColor("_OutlineCol", new Color(0, 0, 0, 0));
            return m;
        }
        var chM = CharMat(1, Color.white, 4, 3018);
        var ch = Quad(c.root, "CutChar", bandPos + (charH > 0 ? new Vector3(charOff.x, charOff.y, -0.2f) : new Vector3(1.4f, 0.3f, -0.2f)), size, chM);
        var ghosts = new List<(Transform tr, Material m, float lag)>();
        for (int k = 0; k < 3; k++)
        {
            var gm = CharMat(0.5f - k * 0.13f, Color.Lerp(st.c2, Color.white, 0.2f) * 1.2f, 0, 3017 - k);
            ghosts.Add((Quad(c.root, "CutGhost" + k, ch.position, size, gm), gm, 0.018f * (k + 1)));
        }
        Vector3 home = ch.position;
        var dirIn = (Vector3)(rot * Vector3.right) * side;
        Func<float, Vector3> PosAt = t =>
        {
            if (st.focus)
                return home;
            if (t < tIn) return home + dirIn * 14f;
            if (t < tIn + 0.12f) return home + dirIn * Mathf.LerpUnclamped(14f, 0f, EaseOutBack(Mathf.Clamp01((t - tIn) / 0.12f)));
            if (t < tOut) return home - dirIn * (0.35f * (t - tIn - 0.12f));                      // ゆっくり流れる
            float e = Mathf.Clamp01((t - tOut) / 0.12f);
            return home - dirIn * (0.35f * (tOut - tIn - 0.12f) + 16f * e * e);
        };
        c.OnUpdate(t =>
        {
            bool on = t >= tIn && t < tOut + 0.14f;
            ch.gameObject.SetActive(on);
            // 帯の開き（6F で開いて少し行き過ぎ、抜けで 6F で閉じる）
            float open = t < t0 ? 0 : t < tOut + 0.02f ? Mathf.Clamp(EaseOutBack(Mathf.Clamp01((t - t0) / 0.1f)), 0, 1.15f) : 1 - Mathf.Clamp01((t - tOut - 0.02f) / 0.1f);
            if (band != null) { band.SetFloat("_Open", Mathf.Max(open, 0)); bandT.gameObject.SetActive(open > 0.001f); }
            if (chev != null) { chev.SetFloat("_Open", Mathf.Max(open, 0)); chevT.gameObject.SetActive(open > 0.001f); }
            if (!on) { foreach (var g in ghosts) g.tr.gameObject.SetActive(false); return; }
            if (st.focus)
            {
                // 集中線: 小さく出て一気に寄る
                float z = Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(Mathf.Clamp01((t - tIn) / 0.14f)));
                float ex = Mathf.Clamp01((t - tOut) / 0.12f);
                ch.localScale = new Vector3(size.x, size.y, 1) * (z * (1 + ex * 0.6f));
                chM.SetFloat("_Alpha", 1 - ex);
            }
            ch.position = PosAt(t);
            chM.SetFloat("_Flash", t < tIn + 2 / 60f ? 1f : t < tIn + 0.14f && t >= tIn + 0.12f ? 0.5f : 0f);
            foreach (var g in ghosts)
            {
                bool moving = !st.focus && (t < tIn + 0.2f || t > tOut);
                g.tr.gameObject.SetActive(moving);
                g.tr.position = PosAt(t - g.lag) + new Vector3(0, 0, 0.05f);
            }
        });
        // 画面: 白 2F・背景を沈める・止まった瞬間に揺れ
        c.OnUpdate(t =>
        {
            if (t >= t0 && t < t0 + 2 / 60f) c.post.flash = Mathf.Max(c.post.flash, 0.55f);
            if (t >= t0 && t < tOut + 0.14f) { c.post.stageDim = Mathf.Max(c.post.stageDim, 0.7f); c.post.stageDesat = Mathf.Max(c.post.stageDesat, 0.4f); }
            float a = t - (tIn + 0.12f);
            if (a >= 0 && a < 0.3f) c.post.trauma += 0.55f * (1 - a / 0.3f);
        });
        // 画面の外の流線（帯の外にも速さを出す）
        var sp = PS(c, "CutSpeed", bandPos + new Vector3(10f, 0, -0.1f), AddMat(c.tx.streak, 1.6f), 900 + (uint)(st.mode * 10));
        {
            var m = sp.main; m.duration = hold; m.startLifetime = new MinMaxCurve(0.18f, 0.32f); m.startSpeed = new MinMaxCurve(55f, 80f);
            m.startSize3D = true; m.startSizeX = new MinMaxCurve(3f, 7f); m.startSizeY = new MinMaxCurve(0.03f, 0.08f); m.startSizeZ = 1; m.startColor = st.c2;
            m.startRotation = st.angle * Mathf.Deg2Rad * -1f;
            var e = sp.emission; e.rateOverTime = st.focus ? 0 : 45;
            var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.1f, 9f, 0.1f);
            sp.transform.rotation = Quaternion.LookRotation(-(rot * Vector3.right), Vector3.forward) ;
            ColorLife(sp, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 0f), (0.2f, 0.8f), (1f, 0f) }));
            c.Play(sp, tIn);
        }
        if (st.bolts)
        {
            Lightning(c, bandPos + rot * new Vector3(-7f, 0.9f, -0.3f), bandPos + rot * new Vector3(6f, -0.6f, -0.3f), tIn + 0.14f, st.c1, 1401, width: 0.05f, branches: 4, impact: false);
            Lightning(c, bandPos + rot * new Vector3(7f, -1.1f, -0.3f), bandPos + rot * new Vector3(-5f, 1.0f, -0.3f), tIn + 0.55f, st.c1, 1402, width: 0.04f, branches: 3, impact: false);
        }
        if (st.sparkle) { Bokeh(c, home + new Vector3(0, 0.3f, -0.4f), tIn + 0.12f, st.c2, 3.5f, 1403); Flare(c, home + new Vector3(1.5f, 1.2f, -0.4f), tIn + 0.14f, st.c2, 3.2f, 1404); }
        if (st.focus)
        {
            c.OnUpdate(t =>
            {
                float a = t - t0;
                if (a >= 0 && a < 2 / 60f) { c.post.invert = 1; c.post.mono = 1; }       // 白黒の衝撃コマ
                else if (a >= 2 / 60f && a < 5 / 60f) c.post.mono = 1;
                if (t >= tIn && t < tOut + 0.1f)
                {
                    c.post.lines = 0.9f; c.post.linesMode = 1; c.post.linesSeed = Mathf.Floor(t * 20); c.post.linesDensity = 0.55f;
                    c.post.linesCenter = new Vector2(0.5f, 0.5f);
                }
            });
            Ring(c, home + new Vector3(0, 0, -0.4f), tIn + 0.14f, st.c2, 9f, 1405);
        }
        if (ups != null)
        {
            // 昇格: 停止の瞬間に帯が割れて色が変わる。白 3F・ガラスの破片が帯から弾ける・揺れ・キャラが白く光る・段の札
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var lab = new GameObject("StopLabel"); lab.transform.SetParent(c.root, false); lab.transform.position = new Vector3(-4.4f, -3.0f, -3.6f);
            var tm = lab.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 90; tm.characterSize = 0.04f; tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold; tm.text = "";
            lab.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            int k = 0;
            foreach (var u in ups)
            {
                var ns = u.st; float tu = u.t;
                c.OnUpdate(t =>
                {
                    if (t < tu) return;
                    if (band != null) { band.SetFloat("_Mode", ns.mode); band.SetColor("_Base", ns.bas); band.SetColor("_C1", ns.c1); band.SetColor("_C2", ns.c2); band.SetColor("_C3", ns.c3); band.SetFloat("_Speed", ns.speed); }
                    if (chev != null) { chev.SetColor("_C1", ns.chevA); chev.SetColor("_C2", ns.chevB); chev.SetColor("_Base", ns.bas * 0.6f); }
                    float a = t - tu;
                    if (a < 3 / 60f) c.post.flash = Mathf.Max(c.post.flash, 0.85f);
                    if (a < 0.35f) c.post.trauma += 0.7f * (1 - a / 0.35f);
                    if (a < 0.1f) chM.SetFloat("_Flash", 0.8f * (1 - a / 0.1f));
                    if (band != null && a < 0.12f) band.SetFloat("_Open", 1f + 0.25f * Mathf.Sin(a / 0.12f * Mathf.PI));   // 帯が一瞬ふくらむ
                });
                // 帯から弾けるガラスの破片（帯の色）と、帯に沿った閃光
                for (int j = 0; j < 4; j++)
                {
                    var p = bandPos + rot * new Vector3(-6f + j * 4f, (j % 2 == 0 ? 1 : -1) * 1.8f, -0.4f);
                    Shards(c, p, tu, j % 2 == 0 ? st.c2 : ns.c2, 1.4f, (j % 2 == 0 ? 90 : -90) + st.angle, 120, 16, 1450 + (uint)(k * 10 + j));
                }
                Flare(c, bandPos + new Vector3(0, 0, -0.5f), tu, ns.c2, 6f, 1460 + (uint)k);
                Ring(c, bandPos + new Vector3(0, 0, -0.5f), tu, ns.c2, 10f, 1470 + (uint)k, squash: 0.5f);
                if (ns.sparkle) { Bokeh(c, home + new Vector3(0, 0.3f, -0.4f), tu, ns.c2, 3.5f, 1480); }
                string lbl = u.label;
                c.OnUpdate(t => { if (t >= tu) { tm.text = lbl; lab.transform.localScale = Vector3.one * (1 + 0.4f * Mathf.Exp(-(t - tu) * 14f)); tm.color = Color.white; } });
                k++;
            }
            c.OnUpdate(t => { if (t >= tOut) tm.text = ""; });
        }
    }

    const string Equip = "UnityProject/BigBonusBlitz/Assets/Resources/Art/UI/AzureEquip/";
    static void CutStreak(Ctx c) => CutIn(c, new CutStyle { mode = 0, bas = new Color(0.25f, 0.06f, 0.02f), c1 = new Color(1f, 0.38f, 0.05f), c2 = new Color(1f, 0.78f, 0.2f), c3 = new Color(2f, 1.6f, 0.9f), speed = 3.2f },
        StandTex("assets/characters/salia/salia_title_reach.png", 0.82f), 0.25f, charH: 6.4f, charOff: new Vector2(0.6f, 0.9f));
    static void CutElec(Ctx c) => CutIn(c, new CutStyle { mode = 1, bas = new Color(0.02f, 0.06f, 0.2f), c1 = new Color(0.15f, 0.55f, 1f), c2 = new Color(0.55f, 0.9f, 1f), c3 = new Color(1.6f, 2.2f, 2.6f), speed = 3.5f,
        chevron = true, chevA = new Color(0.25f, 0.55f, 1f), chevB = new Color(0.8f, 0.95f, 1f), bolts = true, angle = -7f }, StandTex(Equip + "salia-2.png", 0.55f), 0.25f, charH: 6.6f, charOff: new Vector2(1.6f, 1.2f));
    static void CutFire(Ctx c) => CutIn(c, new CutStyle { mode = 2, bas = new Color(0.12f, 0.02f, 0.02f), c1 = new Color(1f, 0.3f, 0.04f), c2 = new Color(1f, 0.72f, 0.12f), c3 = new Color(1.45f, 1.35f, 0.8f), speed = 2.4f, angle = -11f }, StandTex(Equip + "salia-4.png", 0.6f), 0.25f, charH: 6.8f, charOff: new Vector2(1.4f, 1.1f));
    static void CutFocus(Ctx c) => CutIn(c, new CutStyle { mode = 0, bas = Color.black, c1 = Color.white, c2 = new Color(1f, 0.9f, 0.6f), c3 = Color.white * 2, focus = true }, StandTex(Equip + "salia-0.png", 0.55f), 0.25f, charH: 7.4f, charOff: new Vector2(0f, 1.1f));
    static void CutRainbow(Ctx c) => CutIn(c, new CutStyle { mode = 4, bas = new Color(0.05f, 0.03f, 0.1f), c1 = new Color(1f, 0.8f, 0.3f), c2 = new Color(1f, 0.9f, 0.5f), c3 = new Color(2.2f, 2f, 1.6f), speed = 3.2f,
        chevron = true, chevA = new Color(1f, 0.75f, 0.2f), chevB = new Color(1.3f, 1.2f, 0.8f), sparkle = true, angle = -9f }, StandTex(Equip + "salia-1.png", 0.55f), 0.25f, charH: 6.6f, charOff: new Vector2(1.6f, 1.2f));

    // ================= 落ちて、主人公に集まる（本人 2026-09-29「コインじゃなくエンバーやシャードが落ちる」「落ちてしばらくしたら主人公に集まって獲得」）=================
    // 流れ: 敵が倒れる → かけらが噴き上がる → 地面で跳ねて止まる（光ったまま脈打つ）→ 間 → 1 つずつ時間差で浮き上がり、弧を描いて主人公へ吸い込まれる
    //       → 触れた瞬間に主人公が光り、小さな閃光。最後の 1 つで大きめの光
    static Mesh ShardMesh()
    {
        // 細長い六角の両錐（結晶）。面ごとに法線を分けて平らに塗る
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
        var top = new Vector3(0, 1f, 0); var bot = new Vector3(0, -0.7f, 0);
        var ring = Enumerable.Range(0, 6).Select(k => new Vector3(Mathf.Cos(k * Mathf.PI / 3) * 0.32f, 0.05f * ((k % 2) * 2 - 1), Mathf.Sin(k * Mathf.PI / 3) * 0.32f)).ToArray();
        void Face(Vector3 a, Vector3 b, Vector3 cc)
        {
            var fn = Vector3.Cross(b - a, cc - a).normalized; int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); n.Add(fn); n.Add(fn); n.Add(fn); tri.AddRange(new[] { i0, i0 + 1, i0 + 2 });
        }
        for (int k = 0; k < 6; k++) { var a = ring[k]; var b = ring[(k + 1) % 6]; Face(top, b, a); Face(bot, a, b); }
        var m = new Mesh { vertices = v.ToArray(), normals = n.ToArray(), triangles = tri.ToArray() }; m.RecalculateBounds(); return m;
    }

    // kind: "ember"（燃えさし・光の粒＋尾）/ "shard"（結晶のかけら・回る）/ "soul"（魂の光・ふわふわ）
    static void DropCollect(Ctx c, string kind, Color col, int count, uint seed, bool rainbow = false)
    {
        var from = G + new Vector3(0, 0.2f, -0.6f);
        var hero = HeroHome + new Vector3(0.1f, 0.3f, -0.6f);
        float tDie = 0.3f, tBurst = 0.36f, tCollect = 1.45f;
        // 敵: 白く光ってから溶けて消える（倒れる 1 の短い版）
        Impact(c, tDie, 1, 0);
        var size = SpriteSize(c.goblin, 2.7f);
        var dm = BakeDeath(c, (u, v2) => Fbm(u * 5f + 3.1f, v2 * 5f + 1.3f) * 0.8f + (1 - v2) * 0.2f, 0, seed + 1);
        var dmat = DeathMat(c, dm, col * 3f, 0.04f, new Color(col.r * 0.2f, col.g * 0.2f, col.b * 0.2f, 0.9f), 0.04f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = dmat;
        c.OnUpdate(t => dmat.SetFloat("_Cut", Mathf.Clamp01((t - tBurst) / 0.35f) * 1.02f));
        Flare(c, from, tBurst, col, 3f, seed + 2); Ring(c, from, tBurst, col, 3.5f, seed + 3, delay: 0.03f);

        // かけら
        Material mat; bool mesh = kind == "shard";
        if (mesh) mat = new Material(Shader.Find("Lab/Crystal"));
        else mat = AddMat(c.tx.dot, kind == "ember" ? 5f : 3.5f);
        var ps = PS(c, "Drops", from, mat, seed + 4);
        var m = ps.main; m.maxParticles = 200; m.startLifetime = 9f; m.startSpeed = new MinMaxCurve(4.5f, 8.5f);
        m.startSize = mesh ? new MinMaxCurve(0.5f, 0.75f) : kind == "ember" ? new MinMaxCurve(0.16f, 0.26f) : new MinMaxCurve(0.22f, 0.32f);
        if (rainbow) m.startColor = new MinMaxGradient(RainbowGrad()) { mode = ParticleSystemGradientMode.RandomColor };
        m.gravityModifier = 0f;   // 重さと跳ね返りは下の OnUpdate で自前に計算する（ParticleSystem の当たりは止まる所でジグザグになった） m.startColor = mesh ? new MinMaxGradient(col, Color.Lerp(col, Color.white, 0.4f)) : new MinMaxGradient(Color.white);
        if (mesh) { m.startRotation3D = true; m.startRotationX = new MinMaxCurve(0, 6.28f); m.startRotationY = new MinMaxCurve(0, 6.28f); m.startRotationZ = new MinMaxCurve(0, 6.28f); }
        ps.emission.SetBursts(new[] { new Burst(0, (short)(count * 0.6f)), new Burst(0.05f, (short)(count * 0.4f)) });
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 38; sh.radius = 0.2f;
        ps.transform.rotation = Quaternion.LookRotation(new Vector3(-0.35f, 1f, 0f));

        if (mesh)
        {
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = new MinMaxCurve(3f, 8f); rot.y = new MinMaxCurve(2f, 6f); rot.z = new MinMaxCurve(-3f, 3f);
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = ShardMesh(); r.alignment = ParticleSystemRenderSpace.World;
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color });
        }
        else Trail(ps, AddMat(c.tx.trail, 2.5f), new MinMaxCurve(0.05f, 0.05f));   // 尾はごく短く（長いと噴水に見えた）
        if (kind == "soul") { var no = ps.noise; no.enabled = true; no.strength = 0.5f; no.frequency = 0.8f; }
        c.Play(ps, tBurst);

        // 止まっている間は脈打つ（頂点色の明るさ）、集める間は主人公へ吸い寄せる
        var buf = new ParticleSystem.Particle[256];
        var order = new Dictionary<uint, float>();   // 粒ごとの飛び立つ時刻（順番にずらす）
        var baseCols = new Dictionary<uint, Color>();   // 粒ごとの元の色（毎コマ掛け算すると色が飛ぶので覚えておく）
        var picked = new HashSet<uint>();
        int pickedCount = 0; var rnd = new System.Random((int)seed);
        var heroMat = c.heroT.GetComponent<MeshRenderer>().sharedMaterial;
        var pickFx = new List<float>();
        float lastT = tBurst; float g = kind == "soul" ? 1.2f : 14f, floorY = GroundY + 0.18f;
        c.OnUpdate(t =>
        {
            if (t < tBurst) return;
            float dt = Mathf.Max(t - lastT, 0); lastT = t;
            int n = ps.GetParticles(buf);
            for (int i = 0; i < n; i++)
            {
                var p = buf[i];
                if (!order.ContainsKey(p.randomSeed)) order[p.randomSeed] = tCollect + (float)rnd.NextDouble() * 0.55f;
                float go = order[p.randomSeed];
                float pulse = 0.8f + 0.2f * Mathf.Sin(t * 9f + p.randomSeed % 7);
                if (!baseCols.ContainsKey(p.randomSeed)) baseCols[p.randomSeed] = mesh ? (Color)p.startColor : Color.Lerp(col, Color.white, 0.35f);
                var baseCol = baseCols[p.randomSeed];
                if (t < go)
                {
                    // 落ちて跳ねる: 重さ・空気の抵抗・地面で 4 割の高さに跳ね返り、横は 6 割に。ほぼ止まったら止める
                    var v = p.velocity; v.y -= g * dt; v *= Mathf.Pow(kind == "soul" ? 0.2f : 0.7f, dt);
                    var pos = p.position;
                    if (pos.y <= floorY && v.y < 0) { pos.y = floorY; v.y = Mathf.Abs(v.y) > 1.2f ? -v.y * 0.4f : 0; v.x *= 0.6f; if (v.y == 0) v.x *= 0.8f; }
                    if (kind == "soul") v += new Vector3(Mathf.Sin(t * 3f + p.randomSeed) * 0.02f, 0, 0);
                    p.position = pos; p.velocity = v;
                    p.startColor = WithAlpha(baseCol * (t > tCollect - 0.6f ? pulse * 1.15f : 1f), 1f); buf[i] = p; continue;
                }
                // 飛び立つ: 最初の 0.12 秒は上へ浮き、その後は主人公へ加速（弧を描く）
                float a = t - go;
                var to = hero - p.position;
                var dir = to.normalized;
                var want = a < 0.12f ? new Vector3(0, 3.5f, 0) : dir * Mathf.Lerp(4f, 16f, Mathf.Clamp01((a - 0.12f) / 0.35f));
                p.velocity = Vector3.Lerp(p.velocity, want, 0.35f);
                p.startColor = WithAlpha(Color.Lerp(baseCol, Color.white, 0.4f) * 1.3f, 1f);
                if (a > 0.12f && to.magnitude < 0.35f)
                {
                    p.remainingLifetime = 0;
                    if (picked.Add(p.randomSeed)) { pickedCount++; pickFx.Add(t); }
                }
                buf[i] = p;
            }
            ps.SetParticles(buf, n);

        });
        // 獲得: 触れるたびに主人公が光る（重なるほど強く）、最後に大きめの光
        c.OnUpdate(t =>
        {
            float k = 0;
            foreach (var pt in pickFx) { float a = t - pt; if (a >= 0 && a < 0.12f) k += (1 - a / 0.12f) * 0.35f; }
            heroMat.SetFloat("_DimMul", 0.1f);
            c.post.stageDim = Mathf.Max(c.post.stageDim, t > tDie && t < tCollect + 1.2f ? 0.3f : 0f);
            HeroGlow(c, Mathf.Min(k, 1f), col);
        });
        // 最後の 1 つを取った後の光（主人公の足元の輪と閃光）
        float doneT = tCollect + 0.55f + 0.5f;
        Flare(c, hero, doneT, col, 2.6f, seed + 5); Ring(c, new Vector3(hero.x, GroundY + 0.15f, -0.6f), doneT, col, 4f, seed + 6, squash: 0.25f);
        Bokeh(c, hero, doneT, col, 1.2f, seed + 7);
        // 触れた瞬間の小さな閃光（粒ごと）
        var pk = PS(c, "PickFlash", hero, AddMat(c.tx.star4, 3f), seed + 8);
        { var pm = pk.main; pm.startLifetime = 0.12f; pm.startSize = new MinMaxCurve(0.6f, 1.1f); pm.startColor = Color.Lerp(col, Color.white, 0.5f); pm.startRotation = new MinMaxCurve(0, 6.28f); var pe = pk.emission; pe.rateOverTime = 0; c.Play(pk, 0f); }
        int emitted = 0;
        c.OnUpdate(t => { while (emitted < pickFx.Count) { pk.Emit(new EmitParams { position = hero + new Vector3(0, 0, -0.1f), applyShapeToPosition = false }, 1); emitted++; } });
    }

    // 主人公を一瞬その色で光らせる（重ねがけ用）
    static void HeroGlow(Ctx c, float k, Color col)
    {
        if (c.heroGlow == null)
        {
            var q = Quad(c.root, "HeroGlow", c.heroT.position + new Vector3(0, 0, -0.03f), SpriteSize(c.hero, 3.3f), QMat(c, c.hero, 0, Vector2.one, 0));
            c.heroGlow = q.GetComponent<MeshRenderer>().sharedMaterial;
        }
        c.heroGlow.SetColor("_Tint", Color.Lerp(col, Color.white, 0.5f) * k);
    }

    static void DropEmbers(Ctx c) => DropCollect(c, "ember", new Color(1f, 0.45f, 0.1f), 36, 1501);
    static void DropShards(Ctx c) => DropCollect(c, "shard", new Color(0.35f, 0.8f, 1f), 18, 1511);
    static void DropSouls(Ctx c) => DropCollect(c, "soul", new Color(0.75f, 0.45f, 1f), 12, 1521);

    // ================= カットインの昇格（本人 2026-09-29「第 1 停止で変化して赤カットインや虹カットインになる」）=================
    // 青で出る → 第 1 停止: 帯が割れて（白 3F・ガラスの破片・揺れ）赤に → 第 2 停止: もう一度割れて虹に。キャラは白く光って残る
    static void CutUpgrade(Ctx c)
    {
        var blue = new CutStyle { mode = 1, bas = new Color(0.02f, 0.06f, 0.2f), c1 = new Color(0.15f, 0.55f, 1f), c2 = new Color(0.55f, 0.9f, 1f), c3 = new Color(1.6f, 2.2f, 2.6f), speed = 3.5f,
            chevron = true, chevA = new Color(0.25f, 0.55f, 1f), chevB = new Color(0.8f, 0.95f, 1f), angle = -7f };
        var red = new CutStyle { mode = 2, bas = new Color(0.12f, 0.02f, 0.02f), c1 = new Color(1f, 0.25f, 0.04f), c2 = new Color(1f, 0.65f, 0.1f), c3 = new Color(1.45f, 1.3f, 0.75f), speed = 3.2f,
            chevA = new Color(1f, 0.3f, 0.05f), chevB = new Color(1.2f, 0.9f, 0.5f) };
        var rainbow = new CutStyle { mode = 4, bas = new Color(0.05f, 0.03f, 0.1f), c1 = new Color(1f, 0.8f, 0.3f), c2 = new Color(1f, 0.9f, 0.5f), c3 = new Color(2.2f, 2f, 1.6f), speed = 3.8f,
            chevA = new Color(1f, 0.75f, 0.2f), chevB = new Color(1.3f, 1.2f, 0.8f), sparkle = true };
        CutIn(c, blue, StandTex(Equip + "salia-2.png", 0.55f), 0.25f, hold: 2.35f, charH: 6.6f, charOff: new Vector2(1.6f, 1.2f),
              ups: new[] { (1.0f, red, "第 1 停止"), (1.75f, rainbow, "第 2 停止") });
    }

    // ================= ブラックアウト（ロングフリーズ。本人 2026-09-29）=================
    // 3F で真っ暗 → 無音の溜め（中央の小さな光が 2 回脈打つ）→ 縦に光の裂け目が走って開く → 白 → 虹で明ける（虹の炎の枠・虹の輪・きらめき）
    static void LongFreeze(Ctx c)
    {
        float tb = 0.3f, tSlit = 1.9f, tOpen = 2.35f;
        c.OnUpdate(t =>
        {
            if (t < tb) return;
            float k = Mathf.Clamp01((t - tb) / 0.05f);
            if (t < tOpen) c.post.black = k;
            else c.post.black = 1 - Mathf.Clamp01((t - tOpen) / 0.25f);
            // 心臓の鼓動のような脈（2 回）
            foreach (var hb in new[] { 0.95f, 1.35f })
            {
                float a = t - hb;
                if (a >= 0 && a < 0.35f) c.post.slit = new Vector4(0.0015f, 0.9f * Mathf.Exp(-a * 9f), 0, 0);
            }
            // 光の裂け目: 細い線が走り（上から下へ伸びる代わりに明るさで）→ 開く
            if (t >= tSlit && t < tOpen) { float a = (t - tSlit) / (tOpen - tSlit); c.post.slit = new Vector4(Mathf.Lerp(0.002f, 0.5f, a * a * a), Mathf.Lerp(0.8f, 3f, a), 0, 0); }
            if (t >= tOpen && t < tOpen + 0.05f) c.post.flash = 1f;
            float s = t - tSlit; if (s >= 0 && s < 0.5f) c.post.trauma += 0.25f * s * 2;
        });
        // 明けたら虹
        FireFrame(c, tOpen, 5.2f, FireRainbow, 1.1f, 1601);
        Ring(c, new Vector3(0, 0, -1f), tOpen, Color.white, 12f, 1602); Ring(c, new Vector3(0, 0, -1f), tOpen + 0.06f, new Color(1f, 0.7f, 0.9f), 9f, 1603);
        Bokeh(c, new Vector3(0, 0.3f, -1f), tOpen, new Color(1f, 0.9f, 0.6f), 4f, 1604);
        c.OnUpdate(t => { float a = t - tOpen; if (a >= 0 && a < 0.5f) c.post.trauma += 0.8f * (1 - a / 0.5f); });
    }

    // ================= 剣が画面を真っ二つ（凄いことが起こる前兆。本人 2026-09-29）=================
    // 暗くなる → 刃の光が斜めに走る（巨大な斬撃と斬線）→ 画面が線で割れて上下が逆へずれ、裂け目から赤い光 → 震えて溜め → 元に戻って揺れ、赤い光が残る
    static void SwordSplit(Ctx c)
    {
        const float ang = -24f;
        float tCut = 0.45f, tSplit = 0.52f, tBack = 1.55f;
        var red = new Color(1f, 0.12f, 0.05f);
        c.OnUpdate(t => { if (t > 0.2f) { c.post.stageDim = Mathf.Max(c.post.stageDim, t < tBack + 0.8f ? 0.55f : 0.3f); c.post.stageDesat = Mathf.Max(c.post.stageDesat, 0.4f); } });
        // 刃の光（画面を横断する斬撃・斬線・予兆のきらめき）
        Glint(c, new Vector3(-5.6f, 2.6f, -3f), 0.3f, Color.white);
        SlashThrough(c, new Vector3(0, 0.2f, -2f), ang, 5, 12f, 1.3f, 70, Crimson, tCut - 0.06f, dur: 0.06f, fade: 0.4f);
        CutLine(c, new Vector3(0, 0, -3f), ang, 16f, tCut, Color.white);
        c.OnUpdate(t =>
        {
            float a = t - tCut;
            if (a >= 0 && a < 2 / 60f) { c.post.flash = 0.9f; }
            if (a >= 2 / 60f && a < 5 / 60f) { c.post.mono = 1; c.post.invert = 1; }
            // 割れ: 開いて（5F）→ 震えながら少しずつ広がる → 戻る（4F）
            float s = 0;
            if (t >= tSplit && t < tBack) s = EaseOutBack(Mathf.Clamp01((t - tSplit) / 0.08f)) + (t - tSplit) * 0.25f;
            else if (t >= tBack) s = Mathf.Max(0, 1 - (t - tBack) / 0.07f) * (1 + (tBack - tSplit) * 0.25f);
            float tremble = t >= tSplit && t < tBack ? Mathf.Sin(t * 90f) * 0.004f : 0;
            if (s > 0)
            {
                c.post.split = new Vector4(ang * Mathf.Deg2Rad, 0.06f * s + tremble, 0.03f * s, 2.2f + 0.6f * Mathf.Sin(t * 40f));
                c.post.splitCol = red;
            }
            if (t >= tSplit && t < tBack) c.post.trauma += 0.12f;
            float b = t - tBack; if (b >= 0 && b < 0.45f) c.post.trauma += 0.9f * (1 - b / 0.45f);
            if (b >= 0 && b < 2 / 60f) c.post.flash = 0.7f;
        });
        // 裂け目から火花、戻った瞬間に赤い衝撃
        for (int k = 0; k < 5; k++)
        {
            float x = -5f + k * 2.5f; float y = Mathf.Tan(ang * Mathf.Deg2Rad) * x;
            RealSparks(c, new Vector3(x, y, -3f), 90 + ang, tSplit + k * 0.04f, 22, 8f, 1700 + (uint)k);
        }
        Ring(c, new Vector3(0, 0, -3f), tBack, red, 11f, 1710); Flare(c, new Vector3(0, 0, -3f), tBack, red, 5f, 1711);
        // 残る赤い光（凄いことが起こる）
        var glow = Quad(c.root, "OmenGlow", new Vector3(0, 0, -2.5f), new Vector2(14f, 8f), QMat(c, c.tx.glow, 0, Vector2.one, 0));
        var gm = glow.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { float a = t - tBack; gm.SetColor("_Tint", a < 0 ? Color.black : red * (0.35f * Mathf.Clamp01(a / 0.1f) * (0.8f + 0.2f * Mathf.Sin(t * 8f)))); });
    }

    // ================= シールド（本人 2026-09-29「シールドのエフェクトいろんなの」「盾のエフェクト」「ガーディアン」「盾がくるくる回ってる」）=================
    static Material CrestMat(Ctx c, Color col) { var m = QMat(c, c.tx.shieldCrest, 0, Vector2.one, 0); m.SetColor("_Tint", col); return m; }

    // 盾が回る: 4 枚の光の盾が主人公の周りを楕円に回る。手前は明るく大きく、奥は暗く小さく主人公の後ろへ。盾そのものも縦軸で回る
    static void ShieldOrbit(Ctx c)
    {
        var hero = HeroHome + new Vector3(1.4f, 0, 0); c.heroT.position = hero;
        var center = hero + new Vector3(0, 0.1f, 0);
        var col = new Color(0.35f, 0.8f, 1f);
        float on = 0.25f;
        Surge(c, on, center, new Vector3(center.x, GroundY + 0.02f, 0), col, 0.8f);
        const int N = 4;
        for (int k = 0; k < N; k++)
        {
            var m = CrestMat(c, Color.black);
            var q = Quad(c.root, "OrbitShield" + k, center, new Vector2(1.5f, 1.7f), m);
            float ph = k * Mathf.PI * 2 / N;
            c.OnUpdate(t =>
            {
                float a = t - on; if (a < 0) { q.gameObject.SetActive(false); return; }
                q.gameObject.SetActive(true);
                float grow = EaseOutBack(Mathf.Clamp01(a / 0.35f));                      // 中心から外へ広がって出る
                float ang = ph + a * 4.2f;                                                // 公転（約 0.67 回/秒）
                float depth = Mathf.Sin(ang);                                             // -1 奥 … 1 手前
                q.position = center + new Vector3(Mathf.Cos(ang) * 1.9f * grow, 0.15f + Mathf.Sin(ang * 2) * 0.12f - depth * 0.25f * grow, depth > 0 ? -0.6f : 0.35f);
                float spin = Mathf.Cos(a * 7f + ph);                                      // 盾そのものの回転（縦軸）
                float sc = Mathf.Lerp(0.72f, 1.1f, (depth + 1) * 0.5f) * grow;
                q.localScale = new Vector3(1.5f * sc * (0.2f + 0.8f * Mathf.Abs(spin)), 1.7f * sc, 1);
                float br = Mathf.Lerp(0.5f, 1.6f, (depth + 1) * 0.5f) * (0.8f + 0.5f * Mathf.Abs(spin));   // 正面を向くときらりと明るい
                m.SetColor("_Tint", col * br * Mathf.Clamp01(a / 0.1f));
            });
        }
        // 足元の輪と、盾の軌跡に沿うきらめき
        var ring = Quad(c.root, "OrbitRing", new Vector3(center.x, GroundY + 0.12f, 0.2f), new Vector2(4.4f, 4.4f), QMat(c, c.tx.magic ?? c.tx.ring, 0, Vector2.one, 0));
        var rm = ring.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { ring.rotation = Quaternion.Euler(72, 0, 0) * Quaternion.Euler(0, 0, -t * 60f); rm.SetColor("_Tint", col * (t < on ? 0 : 0.9f * Mathf.Clamp01((t - on) / 0.2f))); });
        c.OnUpdate(t => { if (t >= on) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f); });
        c.heroT.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DimMul", 0.1f);
        var tw = SimplePS(c, "OrbitSparkle", center, AddMat(c.tx.sparkle ?? c.tx.dot, 2f), 1801, on, 3f, 18, 0.4f, 0.8f, 0.12f, 0.26f, col);
        var sh = tw.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.9f; sh.radiusThickness = 0; sh.rotation = new Vector3(0, 0, 0); tw.transform.localScale = new Vector3(1, 0.3f, 1);
        ColorLife(tw, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.2f, 1f), (1f, 0f) }));
    }

    // 大盾: 主人公の前に大きな光の盾が出る（大きく出て締まる・白 2F）。敵の弾を 2 発受けて、盾に波紋・盾が光る・破片が跳ね返る
    static void ShieldBig(Ctx c)
    {
        var hero = HeroHome + new Vector3(1.0f, 0, 0); c.heroT.position = hero;
        var pos = hero + new Vector3(1.5f, 0.25f, -0.6f);
        var col = new Color(1f, 0.82f, 0.35f);
        float on = 0.3f;
        var m = CrestMat(c, Color.black);
        var q = Quad(c.root, "BigShield", pos, new Vector2(2.6f, 3.0f), m);
        var baseSize = new Vector3(2.6f, 3.0f, 1);
        var hits = new[] { 1.05f, 1.75f };
        c.OnUpdate(t =>
        {
            float a = t - on; if (a < 0) { q.gameObject.SetActive(false); return; }
            q.gameObject.SetActive(true);
            float s = a < 0.14f ? Mathf.LerpUnclamped(1.6f, 1f, EaseOut(a / 0.14f)) : 1f;
            float hitK = 0; foreach (var h in hits) { float b = t - h; if (b >= 0 && b < 0.25f) hitK = Mathf.Max(hitK, 1 - b / 0.25f); }
            q.localScale = baseSize * (s * (1 + 0.05f * hitK)) ;
            q.position = pos + new Vector3(0.12f * hitK, 0, 0);                            // 受けた瞬間に少し押し返される
            float br = (a < 2 / 60f ? 2.2f : 1.1f + 0.1f * Mathf.Sin(t * 6f)) + 0.6f * hitK;
            m.SetColor("_Tint", col * br * Mathf.Clamp01(a / 0.05f));
        });
        Flare(c, pos, on, col, 3.6f, 1811); Ring(c, pos, on, col, 5f, 1812, delay: 0.03f);
        for (int i = 0; i < hits.Length; i++)
        {
            float h = hits[i];
            var from = GoblinHome + new Vector3(-0.6f, 0.3f - i * 0.5f, -0.6f);
            var hitPoint = pos + new Vector3(0.1f, 0.2f - i * 0.6f, 0);
            var orb = PS(c, "BigShieldOrb" + i, from, AddMat(c.tx.dot, 7f), 1820 + (uint)i);
            float speed = 12f, time = (hitPoint - from).magnitude / speed;
            orb.transform.rotation = Quaternion.LookRotation((hitPoint - from).normalized);
            var om = orb.main; om.startLifetime = time; om.startSpeed = speed; om.startSize = 0.35f; om.startColor = new Color(1f, 0.3f, 0.8f);
            orb.emission.SetBursts(new[] { new Burst(0, 1) });
            Trail(orb, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.3f, 0.3f));
            c.Play(orb, h - time);
            Flare(c, hitPoint, h, col, 2.4f, 1830 + (uint)i);
            Ring(c, hitPoint, h, col, 2.6f, 1840 + (uint)i);                                // 盾に広がる波紋
            Shards(c, hitPoint, h, col, 1f, 20, 100, 18, 1850 + (uint)i);                   // 跳ね返る破片（敵の方へ）
            Impact(c, h, -1, 180f, enemy: false);
        }
    }

    // ガーディアン: 主人公の背後に光の守護者（主人公のシルエットを大きく）が下から立ち上がり、前に盾を構える。光の柱と昇る粒
    static void ShieldGuardian(Ctx c)
    {
        var hero = HeroHome + new Vector3(1.2f, 0, 0); c.heroT.position = hero;
        var col = new Color(1f, 0.78f, 0.3f);
        float on = 0.3f;
        var size = SpriteSize(c.hero, 3.3f) * 1.7f;
        var gpos = new Vector3(hero.x - 0.5f, GroundY + size.y * 0.5f - 0.1f, 0.4f);
        var gm = new Material(Shader.Find("Lab/Ghost")) { mainTexture = c.hero };
        gm.SetTexture("_NoiseTex", c.tx.noise); gm.SetColor("_Col", col * 0.9f); gm.SetColor("_Rim", new Color(1.8f, 1.45f, 0.8f)); gm.SetFloat("_Edge", 2.5f);
        var g = Quad(c.root, "Guardian", gpos, size, gm);
        c.OnUpdate(t =>
        {
            float a = t - on;
            gm.SetFloat("_Appear", a < 0 ? -0.05f : Mathf.Clamp01(a / 0.6f) * 1.05f);
            gm.SetFloat("_Intensity", a < 0 ? 0 : 0.9f + 0.1f * Mathf.Sin(t * 4f));
            g.position = gpos + new Vector3(0, Mathf.Sin(t * 2.2f) * 0.05f, 0);            // ゆっくり呼吸する
        });
        // 構える盾（守護者の前、主人公の前）
        var cm = CrestMat(c, Color.black);
        var cq = Quad(c.root, "GuardianShield", hero + new Vector3(1.3f, 0.6f, -0.6f), new Vector2(2.2f, 2.5f), cm);
        c.OnUpdate(t =>
        {
            float a = t - (on + 0.55f); cq.gameObject.SetActive(a >= 0);
            if (a < 0) return;
            cq.localScale = new Vector3(2.2f, 2.5f, 1) * Mathf.LerpUnclamped(0.3f, 1f, EaseOutBack(Mathf.Clamp01(a / 0.18f)));
            cm.SetColor("_Tint", col * ((a < 2 / 60f ? 3f : 1.15f) + 0.1f * Mathf.Sin(t * 6f)));
        });
        Flare(c, hero + new Vector3(1.3f, 0.6f, -0.7f), on + 0.55f, col, 3.2f, 1861);
        // 光の柱・足元の輪・昇る粒
        var beam = Quad(c.root, "GuardianBeam", new Vector3(gpos.x, 0.8f, 0.5f), new Vector2(3.4f, 8f), QMat(c, c.tx.column, 0.6f, new Vector2(2, 1), 0.8f));
        var bm = beam.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { float a = t - on; bm.SetColor("_Tint", a < 0 ? Color.black : col * (0.7f * Mathf.Clamp01(a / 0.2f) * (1 - 0.5f * Mathf.Clamp01((a - 0.8f) / 0.8f)))); });
        Ring(c, new Vector3(gpos.x, GroundY + 0.15f, -0.6f), on, col, 6f, 1862, squash: 0.25f);
        var mo = SimplePS(c, "GuardianMotes", new Vector3(gpos.x, GroundY + 0.2f, 0.3f), AddMat(c.tx.dot, 4f), 1863, on, 3f, 30, 1.2f, 2f, 0.05f, 0.11f, col);
        var sh = mo.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3f, 0.2f, 0.2f);
        var v = mo.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.1f, 0.1f); v.y = new MinMaxCurve(1f, 2.2f); v.z = new MinMaxCurve(0f, 0f);
        ColorLife(mo, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.15f, 1f), (1f, 0f) }));
        c.OnUpdate(t => { if (t >= on) { c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f); } });
        c.heroT.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DimMul", 0.1f);
    }

    // ================= 追加 30 本（本人 2026-09-29「前兆フリーズ 10 点」「他も 5 点ずつ」）=================
    static TextMesh Label(Ctx c, string text, Vector3 pos, float size, Color col)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("Label"); go.transform.SetParent(c.root, false); go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 120; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold;
        tm.text = text; tm.color = col; var fm = new Material(font.material) { renderQueue = 3030 }; go.GetComponent<MeshRenderer>().sharedMaterial = fm; go.SetActive(false);   // 帯（3015）・キャラ（3018）より手前
        return tm;
    }
    // 出て、跳ね返って、震えて、消える文字
    static void PopLabel(Ctx c, string text, Vector3 pos, float size, Color col, float t0, float hold, float rot = 0f)
    {
        var tm = Label(c, text, pos, size, col);
        var r = new System.Random(text.Length * 31 + (int)(t0 * 100));
        c.OnUpdateReal(rt =>
        {
            float a = rt - c.Real(t0);
            bool on = a >= 0 && a < hold; tm.gameObject.SetActive(on); if (!on) return;
            float sIn = a < 0.12f ? Mathf.LerpUnclamped(2.2f, 1f, EaseOutBack(a / 0.12f)) : 1f;
            float ex = Mathf.Clamp01((a - hold + 0.12f) / 0.12f);
            tm.transform.localScale = Vector3.one * sIn * (1 + 0.3f * ex);
            tm.transform.rotation = Quaternion.Euler(0, 0, rot + (a < 0.12f ? 12f * (1 - a / 0.12f) : 0));
            tm.transform.position = pos + (a > 0.12f ? new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f, 0) * 0.05f : Vector3.zero);
            var cc = a < 2 / 60f ? Color.white : col; cc.a = 1 - ex; tm.color = cc;
        });
    }
    // 前面に置く黒い板（エフェクトより奥・舞台より手前）
    static Material BlackPlate(Ctx c, float z = -2.9f)
    {
        var m = new Material(Shader.Find("Lab/Sprite")) { mainTexture = c.tx.square }; m.SetColor("_Color", new Color(0, 0, 0, 0)); m.renderQueue = 2995;   // エフェクト（3000〜）より先
        Quad(c.root, "BlackPlate", new Vector3(0, 0, z), new Vector2(14f, 8f), m);
        return m;
    }
    static void SetPlate(Material plate, float a) => plate.SetColor("_Color", new Color(0, 0, 0, a));
    static void EnemyShot(Ctx c, Vector3 from, Vector3 to, float tHit, Color col, uint seed)
    {
        var orb = PS(c, "EnemyShot", from, AddMat(c.tx.dot, 7f), seed);
        float speed = 12f, time = (to - from).magnitude / speed;
        orb.transform.rotation = Quaternion.LookRotation((to - from).normalized);
        var om = orb.main; om.startLifetime = time; om.startSpeed = speed; om.startSize = 0.35f; om.startColor = col;
        orb.emission.SetBursts(new[] { new Burst(0, 1) });
        Trail(orb, AddMat(c.tx.trail, 3f), new MinMaxCurve(0.3f, 0.3f));
        c.Play(orb, tHit - time);
    }
    static Mesh ColoredShardMesh(Color col)
    {
        var m = UnityEngine.Object.Instantiate(ShardMesh()); var cs = new Color[m.vertexCount]; for (int i = 0; i < cs.Length; i++) cs[i] = col; m.colors = cs; return m;
    }
    static ParticleSystem CrystalBurst(Ctx c, Vector3 pos, float t, Color col, int count, float speed, uint seed, float size = 0.35f)
    {
        var ps = PS(c, "CrystalBurst", pos, new Material(Shader.Find("Lab/Crystal")), seed);
        var m = ps.main; m.startLifetime = new MinMaxCurve(0.8f, 1.4f); m.startSpeed = new MinMaxCurve(speed * 0.4f, speed); m.startSize = new MinMaxCurve(size * 0.5f, size);
        m.gravityModifier = 1.6f; m.startColor = new MinMaxGradient(col, Color.Lerp(col, Color.white, 0.5f));
        m.startRotation3D = true; m.startRotationX = new MinMaxCurve(0, 6.28f); m.startRotationY = new MinMaxCurve(0, 6.28f); m.startRotationZ = new MinMaxCurve(0, 6.28f);
        ps.emission.SetBursts(new[] { new Burst(0, (short)count) });
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.15f;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = new MinMaxCurve(3f, 9f); rot.y = new MinMaxCurve(2f, 6f); rot.z = new MinMaxCurve(-4f, 4f);
        ColorLife(ps, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (0.75f, 1f), (1f, 0f) }));
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = ShardMesh(); r.alignment = ParticleSystemRenderSpace.World;
        r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color });
        c.Play(ps, t); return ps;
    }

    // ---------- 前兆・フリーズ 10 ----------
    // O1 画面にひびが入り、2 度目で広がり、ガラスのように砕けて暗転
    static void OmenGlass(Ctx c)
    {
        var center = new Vector3(0.6f, 0.4f, -3.3f);
        var rnd = new System.Random(1901);
        var plate = BlackPlate(c);
        float tb = 1.6f;
        foreach (var (tw, n, len) in new[] { (0.4f, 7, 3.8f), (1.0f, 10, 7.5f) })
        {
            for (int k = 0; k < n; k++)
            {
                float ang = (float)rnd.NextDouble() * 6.28f;
                var end = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f, 0) * len * (0.55f + 0.45f * (float)rnd.NextDouble());
                var arr = Jagged(center, end, len * 0.07f, 5, rnd).ToArray();
                var lr = new GameObject("Crack").AddComponent<LineRenderer>(); lr.transform.SetParent(c.root, false);
                lr.useWorldSpace = true; lr.sharedMaterial = AddMat(c.tx.trail, 2.6f); lr.widthMultiplier = 0.04f; lr.startColor = lr.endColor = new Color(0.85f, 0.95f, 1f);
                lr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0.25f); lr.positionCount = 0;
                float tt = tw;
                c.OnUpdate(t =>
                {
                    int cnt = Mathf.RoundToInt(Mathf.Clamp01((t - tt) / 0.1f) * arr.Length);
                    lr.positionCount = cnt; for (int i = 0; i < cnt; i++) lr.SetPosition(i, arr[i]);
                    lr.enabled = cnt > 1 && t < tb;
                });
            }
            float t0 = tw;
            Flare(c, center, tw, Color.white, 2.6f, 1902 + (uint)(tw * 10));
            c.OnUpdate(t => { float a = t - t0; if (a >= 0 && a < 2 / 60f) c.post.flash = Mathf.Max(c.post.flash, 0.5f); if (a >= 0 && a < 0.25f) c.post.trauma += 0.6f * (1 - a / 0.25f); });
        }
        c.OnUpdate(t =>
        {
            if (t > 0.3f && t < tb) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f);
            float a = t - tb;
            if (a >= 0) { if (a < 3 / 60f) c.post.flash = 1f; SetPlate(plate, Mathf.Clamp01((a - 0.03f) / 0.12f) * 0.95f); if (a < 0.45f) c.post.trauma += 0.9f * (1 - a / 0.45f); }
            else SetPlate(plate, 0);
        });
        CrystalBurst(c, center, tb, new Color(0.75f, 0.9f, 1f), 60, 9f, 1905, 0.6f);
        Shards(c, center, tb, Color.white, 2f, 0, 360, 50, 1906);
    }

    // O2 地鳴り: 揺れが強まり、石が降り、赤く脈打つ → 最後にドン
    static void OmenQuake(Ctx c)
    {
        float t0 = 0.3f, t1 = 2.4f;
        c.OnUpdate(t =>
        {
            if (t < t0) return;
            float a = Mathf.Clamp01((t - t0) / (t1 - t0));
            if (t < t1)
            {
                c.post.trauma += 0.3f + 0.35f * a;
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 5.5f)), 10);
                c.post.tint = new Color(1f, 0.3f, 0.25f, 0.12f * a + 0.4f * beat * a); c.post.darken = 0.55f * a;
                c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f * a);
            }
            else { float b = t - t1; if (b < 3 / 60f) c.post.flash = 0.8f; if (b < 0.5f) c.post.trauma += 1.1f * (1 - b / 0.5f); }
        });
        var rocks = SimplePS(c, "Rocks", new Vector3(0, 4.2f, -1f), new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 1911, t0, t1 - t0, 30, 1f, 1.6f, 0.05f, 0.16f, new Color(0.35f, 0.3f, 0.27f));
        { var m = rocks.main; m.gravityModifier = 1.1f; m.startRotation = new MinMaxCurve(0, 6.28f); var sh = rocks.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(13f, 0.1f, 0.1f); }
        for (int k = 0; k < 6; k++)
            Flip(c, "QuakeDust" + k, new Vector3(-5f + k * 2f, GroundY + 0.4f, -0.4f), c.tx.fbSmoke, 8, 8, k * 5, 40 + k * 5, 1.2f, 2.6f, new Color(0.7f, 0.6f, 0.5f, 0.55f), 1f, 1912 + (uint)k, alpha: true, t: t0 + k * 0.3f);
        Ring(c, new Vector3(0, GroundY + 0.2f, -0.6f), t1, new Color(1f, 0.4f, 0.2f), 11f, 1920, squash: 0.25f);
        for (int k = 0; k < 4; k++) Flip(c, "QuakeBig" + k, new Vector3(-4.5f + k * 3f, GroundY + 0.5f, -0.4f), c.tx.fbSmoke, 8, 8, k * 7, 40 + k * 7, 1.1f, 3.4f, new Color(0.8f, 0.7f, 0.6f, 0.7f), 1f, 1921 + (uint)k, alpha: true, t: t1);
    }

    // O3 警告: 上下に黄黒の帯が流れ、画面が赤く 3 回点滅して WARNING
    static void OmenWarning(Ctx c)
    {
        var st = new CutStyle { bas = new Color(0.06f, 0.03f, 0f), c1 = new Color(1f, 0.8f, 0.05f), c2 = new Color(0.12f, 0.04f, 0f), c3 = new Color(1.5f, 1.2f, 0.4f), speed = 2.5f };
        foreach (var y in new[] { 3.05f, -3.05f })
        {
            var m = CutBandMat(c, st, 3, y > 0 ? 1.1f : 2.2f); m.SetFloat("_Aspect", 17f / 1.1f); m.SetFloat("_Edge", 0);
            Quad(c.root, "WarnBand", new Vector3(0, y, -3.5f), new Vector2(17f, 1.1f), m);
            c.OnUpdate(t => m.SetFloat("_Open", Mathf.Clamp01((t - 0.3f) / 0.1f) * (1 - Mathf.Clamp01((t - 2.3f) / 0.1f))));
        }
        foreach (var tb in new[] { 0.45f, 0.95f, 1.45f })
        {
            float t0 = tb;
            c.OnUpdate(t => { float a = t - t0; if (a >= 0 && a < 0.32f) { float k = 1 - a / 0.32f; c.post.tint = new Color(1f, 0.15f, 0.1f, 0.6f * k); c.post.darken = Mathf.Max(c.post.darken, 0.6f * k); c.post.trauma += 0.3f * k; } });
            PopLabel(c, "WARNING", new Vector3(0, 0.4f, -3.6f), 0.07f, new Color(1f, 0.2f, 0.12f), t0, 0.3f);
        }
        PopLabel(c, "WARNING", new Vector3(0, 0.4f, -3.6f), 0.085f, new Color(1f, 0.2f, 0.12f), 1.9f, 0.5f);
        c.OnUpdate(t => { if (t > 0.3f && t < 2.4f) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.45f); if (t >= 1.9f && t < 1.93f) c.post.flash = 0.6f; });
    }

    // O4 時間停止: 舞う粒が空中で止まり、画面が白黒に。時計の輪が逆に回り、色が戻って動き出す
    static void OmenTimeStop(Ctx c)
    {
        float tf = 0.9f, dur = 1.6f;
        c.Freeze(tf, dur);
        var r = new System.Random(1931);
        for (int k = 0; k < 28; k++)
        {
            var q = Quad(c.root, "Mote", Vector3.zero, Vector2.one * (0.08f + 0.1f * (float)r.NextDouble()), QMat(c, c.tx.sparkle ?? c.tx.dot, 0, Vector2.one, 0));
            var mm = q.GetComponent<MeshRenderer>().sharedMaterial; mm.SetColor("_Tint", new Color(1f, 0.85f, 0.5f) * 1.4f);
            float x0 = -6f + 12f * (float)r.NextDouble(), ph = (float)r.NextDouble() * 10f, sp = 0.6f + 0.8f * (float)r.NextDouble();
            c.OnUpdate(t => { float y = 4f - Mathf.Repeat(t * sp + ph, 8f); q.position = new Vector3(x0 + Mathf.Sin(t * 1.7f + ph) * 0.4f, y, -1.5f); });   // 作る側の時刻で動く = 止まる
        }
        var face = Quad(c.root, "ClockFace", new Vector3(0, 0.2f, -3f), new Vector2(6f, 6f), QMat(c, c.tx.magic ?? c.tx.ring, 0, Vector2.one, 0));
        var fm = face.GetComponent<MeshRenderer>().sharedMaterial;
        var hand = new GameObject("Hand").AddComponent<LineRenderer>(); hand.transform.SetParent(c.root, false);
        hand.useWorldSpace = true; hand.positionCount = 2; hand.sharedMaterial = AddMat(c.tx.trail, 3f); hand.widthMultiplier = 0.06f; hand.startColor = hand.endColor = new Color(0.7f, 0.85f, 1f);
        c.OnUpdateReal(rt =>
        {
            float a = rt - tf;
            bool on = a >= 0 && a < dur;
            float k = on ? Mathf.Clamp01(a / 0.08f) * (1 - Mathf.Clamp01((a - dur + 0.15f) / 0.15f)) : 0;
            c.post.mono = Mathf.Max(c.post.mono, k); c.post.tint = new Color(0.75f, 0.85f, 1f, 0.35f * k); c.post.stageDim = Mathf.Max(c.post.stageDim, 0.25f * k);
            fm.SetColor("_Tint", new Color(0.6f, 0.8f, 1f) * (0.9f * k));
            face.rotation = Quaternion.Euler(0, 0, a * 25f);
            float ang = Mathf.PI / 2 + a * 9f;                                       // 逆に回る針
            hand.enabled = on; hand.SetPosition(0, new Vector3(0, 0.2f, -3.1f)); hand.SetPosition(1, new Vector3(Mathf.Cos(ang) * 2.2f, 0.2f + Mathf.Sin(ang) * 2.2f, -3.1f));
            if (a >= 0 && a < 2 / 60f) c.post.flash = 0.6f;
            if (a >= dur && a < dur + 2 / 60f) c.post.flash = 0.5f;
        });
        Ring(c, new Vector3(0, 0.2f, -1f), tf, new Color(0.6f, 0.8f, 1f), 12f, 1932);
        Ring(c, new Vector3(0, 0.2f, -1f), tf + 0.0001f, new Color(1f, 0.9f, 0.6f), 10f, 1933);
    }

    // O5 まぶたが開く: 暗転 → 金の横線 → 震えて上下に開く → 金の光
    static void OmenEyelid(Ctx c)
    {
        var gold = new Color(1f, 0.8f, 0.35f);
        c.OnUpdate(t =>
        {
            if (t < 0.3f) return;
            c.post.slitTint = gold;
            if (t < 1.9f) c.post.black = Mathf.Clamp01((t - 0.3f) / 0.08f);
            else c.post.black = 1 - Mathf.Clamp01((t - 1.9f) / 0.3f);
            if (t >= 0.8f && t < 1.5f) c.post.slit = new Vector4(0.003f + 0.002f * Mathf.Sin(t * 60f), 1.2f + 0.3f * Mathf.Sin(t * 30f), 1, 0);
            if (t >= 1.5f && t < 1.9f) { float a = (t - 1.5f) / 0.4f; c.post.slit = new Vector4(Mathf.Lerp(0.004f, 1.2f, a * a * a), Mathf.Lerp(1.5f, 3f, a), 1, 0); c.post.trauma += 0.2f; }
            if (t >= 1.9f && t < 1.95f) c.post.flash = 1f;
            if (t >= 1.9f) c.post.tint = new Color(1f, 0.85f, 0.5f, 0.4f * (1 - Mathf.Clamp01((t - 1.9f) / 0.6f)));
        });
        Ring(c, new Vector3(0, 0.2f, -1f), 1.9f, gold, 13f, 1941); Ring(c, new Vector3(0, 0.2f, -1f), 1.96f, Color.white, 9f, 1942);
        Bokeh(c, new Vector3(0, 0.2f, -1f), 1.9f, gold, 4f, 1943);
    }

    // O6 鼓動: ドクン…ドクン…と速くなり、赤く寄る → ネガ反転
    static void OmenHeartbeat(Ctx c)
    {
        var beats = new[] { 0.4f, 0.95f, 1.4f, 1.75f, 2.0f, 2.2f };
        c.OnUpdate(t =>
        {
            if (t > 0.3f) c.post.stageDim = Mathf.Max(c.post.stageDim, Mathf.Clamp01((t - 0.3f) / 2f) * 0.5f);
            for (int k = 0; k < beats.Length; k++)
            {
                float a = t - beats[k]; if (a < 0 || a > 0.3f) continue;
                float e = Mathf.Exp(-a * 14f);
                c.post.zoom *= 1 + (0.035f + 0.012f * k) * e; c.post.tint = new Color(1f, 0.1f, 0.1f, (0.3f + 0.05f * k) * e); c.post.darken = Mathf.Max(c.post.darken, 0.7f * e); c.post.trauma += 0.25f * e;
            }
            float b = t - 2.45f;
            if (b >= 0 && b < 2 / 60f) { c.post.invert = 1; c.post.mono = 1; }
            else if (b >= 2 / 60f && b < 5 / 60f) c.post.invert = 1;
            else if (b >= 5 / 60f && b < 7 / 60f) c.post.flash = 0.8f;
            if (b >= 0 && b < 0.4f) c.post.trauma += 0.9f * (1 - b / 0.4f);
        });
        Ring(c, new Vector3(0, 0.2f, -1f), 2.45f, new Color(1f, 0.2f, 0.15f), 12f, 1951);
    }

    // O7 暗雲と雷: 暗くなり雨、雷が 3 本 → 画面の真ん中に巨大な雷
    static void OmenThunder(Ctx c)
    {
        c.OnUpdate(t => { if (t > 0.2f) { float k = Mathf.Clamp01((t - 0.2f) / 0.3f); c.post.stageDim = Mathf.Max(c.post.stageDim, 0.6f * k); c.post.tint = new Color(0.55f, 0.6f, 0.9f, 0.4f * k); } });
        var rain = SimplePS(c, "Rain", new Vector3(1f, 4.2f, -1.2f), AddMat(c.tx.streak, 0.9f), 1961, 0.2f, 3f, 140, 0.4f, 0.6f, 1f, 1f, new Color(0.6f, 0.7f, 1f, 0.6f));
        { var m = rain.main; m.startSize3D = true; m.startSizeX = new MinMaxCurve(0.6f, 1.1f); m.startSizeY = 0.02f; m.startSizeZ = 1; m.startRotation = 70 * Mathf.Deg2Rad; m.startSpeed = 0;
          var sh = rain.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(15f, 0.1f, 0.1f);
          var v = rain.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-5f, -5f); v.y = new MinMaxCurve(-15f, -15f); v.z = new MinMaxCurve(0f, 0f); }
        Lightning(c, new Vector3(-3.5f, 4.2f, -1f), new Vector3(-2.8f, GroundY, -1f), 0.6f, new Color(0.5f, 0.6f, 1f), 1962, width: 0.05f, branches: 4, scale: 0.8f);
        Lightning(c, new Vector3(4f, 4.2f, -1f), new Vector3(3.2f, GroundY, -1f), 1.1f, new Color(0.5f, 0.6f, 1f), 1963, width: 0.05f, branches: 4, scale: 0.8f);
        Lightning(c, new Vector3(-0.5f, 4.2f, -1f), new Vector3(0.8f, GroundY, -1f), 1.5f, new Color(0.5f, 0.6f, 1f), 1964, width: 0.06f, branches: 5, scale: 0.9f);
        Lightning(c, new Vector3(0.3f, 4.2f, -1f), new Vector3(0f, GroundY - 0.1f, -1f), 2.0f, new Color(0.7f, 0.6f, 1f), 1965, width: 0.14f, branches: 9, scale: 1.5f);
        c.OnUpdate(t => { float a = t - 2.0f; if (a >= 0 && a < 4 / 60f) c.post.flash = 1f; if (a >= 0 && a < 0.6f) c.post.shock = new Vector4(0.5f, 0.2f, EaseOut(a / 0.6f) * 0.8f, 1.2f * (1 - a / 0.6f)); });
        Ring(c, new Vector3(0, GroundY + 0.2f, -0.6f), 2.0f, new Color(0.7f, 0.7f, 1f), 11f, 1966, squash: 0.3f);
    }

    // O8 ゴゴゴ: 敵へ集中線でゆっくり寄り、「ゴ」が周りで震える → 衝撃コマ
    static void OmenStare(Ctx c)
    {
        var gUv = WorldToUv(G);
        c.OnUpdate(t =>
        {
            if (t < 0.4f) return;
            float k = Mathf.Clamp01((t - 0.4f) / 1.6f);
            if (t < 2.0f) { c.post.lines = 0.85f; c.post.linesMode = 1; c.post.linesSeed = Mathf.Floor(t * 12); c.post.linesDensity = 0.45f; c.post.linesCenter = gUv; c.post.zoom = 1 + 0.28f * (k * k * (3 - 2 * k)); c.post.zoomCenter = gUv; c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f); c.post.trauma += 0.12f; }
            float b = t - 2.0f;
            if (b >= 0 && b < 2 / 60f) { c.post.invert = 1; c.post.mono = 1; c.post.zoom = 1.3f; c.post.zoomCenter = gUv; }
            else if (b >= 2 / 60f && b < 4 / 60f) { c.post.mono = 1; c.post.zoom = 1.3f; c.post.zoomCenter = gUv; }
            if (b >= 0 && b < 0.4f) c.post.trauma += 0.9f * (1 - b / 0.4f);
        });
        var spots = new[] { (-4.2f, 1.8f, 1.5f, -8f), (-2.0f, 2.6f, 1.1f, 6f), (2.6f, 2.2f, 1.3f, -4f), (4.6f, 0.8f, 1.0f, 10f), (-3.8f, -0.8f, 1.1f, 4f) };
        for (int k = 0; k < spots.Length; k++) { var (x, y, h, rot) = spots[k]; Sfx(c, c.tx.sfxGo, new Vector3(x, y, 0), 0.5f + k * 0.22f, rot, h, 1.8f - k * 0.22f, 0.05f, 1971 + (uint)k); }
    }

    // O9 シネマの黒帯: 上下から閉じて、細い隙間に光が震える → 一気に開いて虹の輪
    static void OmenLetterbox(Ctx c)
    {
        c.OnUpdate(t =>
        {
            if (t < 0.3f) return;
            if (t < 1.9f) { c.post.bars = 0.46f * EaseOut(Mathf.Clamp01((t - 0.3f) / 0.35f)); if (t > 0.65f) { c.post.slit = new Vector4(0.006f + 0.003f * Mathf.Sin(t * 50f), 1f, 1, 0); c.post.trauma += 0.1f; } }
            else c.post.bars = 0.46f * (1 - Mathf.Clamp01((t - 1.9f) / 0.08f));
            if (t >= 1.9f && t < 1.93f) c.post.flash = 1f;
            float b = t - 1.9f; if (b >= 0 && b < 0.4f) c.post.trauma += 0.8f * (1 - b / 0.4f);
        });
        var cols = new[] { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.3f, 1f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(0.8f, 0.4f, 1f) };
        for (int k = 0; k < cols.Length; k++) Ring(c, new Vector3(0, 0.1f, -1f), 1.9f + k * 0.05f, cols[k], 13f - k * 1.5f, 1981 + (uint)k);
        Bokeh(c, new Vector3(0, 0.1f, -1f), 1.95f, Color.white, 4f, 1987);
    }

    // O10 映像の乱れ: 乱れが強まり（色ずれ・横ずれ・走査線）→ 暗転 → 白い横線が開いて再起動
    static void OmenGlitch(Ctx c)
    {
        c.OnUpdate(t =>
        {
            if (t < 0.3f) return;
            if (t < 1.6f)
            {
                float k = Mathf.Clamp01((t - 0.3f) / 1.3f);
                float spike = Mathf.PerlinNoise(t * 9f, 0.3f) > 0.6f ? 0.6f : 0f;
                c.post.glitch = Mathf.Clamp01(0.2f + 0.8f * k + spike); c.post.mono = Mathf.PerlinNoise(t * 7f, 5.1f) > 0.7f ? 1 : 0; c.post.trauma += 0.15f * k;
            }
            else if (t < 2.5f)
            {
                c.post.black = 1;
                if (t >= 1.9f && t < 2.2f) c.post.slit = new Vector4(0.003f, Mathf.Lerp(0.5f, 2.5f, (t - 1.9f) / 0.3f), 1, 0);
                if (t >= 2.2f) { float a = (t - 2.2f) / 0.3f; c.post.slit = new Vector4(Mathf.Lerp(0.003f, 1.2f, a * a * a), 2.5f, 1, 0); }
            }
            else { c.post.black = 1 - Mathf.Clamp01((t - 2.5f) / 0.2f); c.post.glitch = 0.25f * (1 - Mathf.Clamp01((t - 2.5f) / 0.4f)); if (t < 2.53f) c.post.flash = 1f; }
        });
        var cols = new[] { new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.5f), new Color(0.3f, 0.7f, 1f) };
        for (int k = 0; k < cols.Length; k++) Ring(c, new Vector3(0, 0.1f, -1f), 2.5f + k * 0.05f, cols[k], 12f - k * 2f, 1991 + (uint)k);
    }

    // ---------- カットイン 5 ----------
    static readonly CutStyle CutBlue = new CutStyle { mode = 0, bas = new Color(0.02f, 0.06f, 0.2f), c1 = new Color(0.2f, 0.55f, 1f), c2 = new Color(0.6f, 0.9f, 1f), c3 = new Color(1.6f, 2.2f, 2.6f), speed = 3.5f, angle = -8f };
    static readonly CutStyle CutRed = new CutStyle { mode = 0, bas = new Color(0.2f, 0.02f, 0.02f), c1 = new Color(1f, 0.2f, 0.05f), c2 = new Color(1f, 0.6f, 0.2f), c3 = new Color(2.2f, 1.4f, 0.8f), speed = 3.5f, angle = -8f };
    static readonly CutStyle CutGold = new CutStyle { mode = 0, bas = new Color(0.18f, 0.1f, 0.01f), c1 = new Color(1f, 0.7f, 0.15f), c2 = new Color(1f, 0.92f, 0.5f), c3 = new Color(2.2f, 1.9f, 1.2f), speed = 3.2f, angle = -8f, sparkle = true };
    // C7 VS: 上下の帯に主人公と敵が向かい合う
    static void CutVs(Ctx c)
    {
        CutIn(c, CutBlue, StandTex(Equip + "salia-2.png", 0.5f), 0.25f, hold: 1.6f, charH: 3.9f, charOff: new Vector2(-2.6f, 0.55f), shift: new Vector3(0, 1.75f, 0), side: -1f, bandH: 3.2f);
        CutIn(c, new CutStyle { mode = 0, bas = CutRed.bas, c1 = CutRed.c1, c2 = CutRed.c2, c3 = CutRed.c3, speed = 3.5f, angle = -8f }, c.goblin, 0.4f, hold: 1.45f, charH: 3.3f, charOff: new Vector2(2.6f, 0.4f), shift: new Vector3(0, -1.75f, 0), side: 1f, bandH: 3.2f);
        PopLabel(c, "VS", new Vector3(0, 0.1f, -3.9f), 0.13f, new Color(1f, 0.85f, 0.3f), 0.62f, 1.1f, -8f);
        Flare(c, new Vector3(0, 0.1f, -3.8f), 0.62f, new Color(1f, 0.85f, 0.3f), 4f, 2001);
    }
    // C8 縦の帯: 下から立ち上がる
    static void CutVertical(Ctx c) => CutIn(c, new CutStyle { mode = 0, bas = new Color(0.05f, 0.03f, 0.12f), c1 = new Color(0.7f, 0.45f, 1f), c2 = new Color(0.95f, 0.8f, 1f), c3 = new Color(2f, 1.7f, 2.4f), speed = 3.5f, angle = 82f },
        StandTex(Equip + "salia-3.png", 0.55f), 0.25f, charH: 6.4f, charOff: new Vector2(0.2f, 0.9f), side: -1f, bandH: 5.2f);
    // C9 コマ割り: 3 本の帯が順に、違う姿で
    static void CutPanels(Ctx c)
    {
        CutIn(c, new CutStyle { mode = 0, bas = CutBlue.bas, c1 = CutBlue.c1, c2 = CutBlue.c2, c3 = CutBlue.c3, speed = 3.5f, angle = -6f }, StandTex(Equip + "salia-0.png", 0.5f), 0.25f, hold: 1.4f, charH: 2.9f, charOff: new Vector2(2f, 0.35f), shift: new Vector3(0, 2.35f, 0), side: 1f, bandH: 2.3f);
        CutIn(c, new CutStyle { mode = 0, bas = CutRed.bas, c1 = CutRed.c1, c2 = CutRed.c2, c3 = CutRed.c3, speed = 3.5f, angle = 6f }, StandTex(Equip + "salia-1.png", 0.5f), 0.42f, hold: 1.23f, charH: 2.9f, charOff: new Vector2(-2f, 0.35f), shift: new Vector3(0, 0.1f, 0), side: -1f, bandH: 2.3f);
        CutIn(c, new CutStyle { mode = 0, bas = CutGold.bas, c1 = CutGold.c1, c2 = CutGold.c2, c3 = CutGold.c3, speed = 3.2f, angle = -6f }, StandTex(Equip + "salia-4.png", 0.5f), 0.59f, hold: 1.06f, charH: 2.9f, charOff: new Vector2(2f, 0.35f), shift: new Vector3(0, -2.15f, 0), side: 1f, bandH: 2.3f);
    }
    // C10 金＋紙吹雪
    static void CutConfetti(Ctx c)
    {
        CutIn(c, CutGold, StandTex("assets/characters/salia/salia_title_reach.png", 0.82f), 0.25f, hold: 1.4f, charH: 6.4f, charOff: new Vector2(0.6f, 0.9f));
        var cf = SimplePS(c, "Confetti", new Vector3(0, 4.3f, -3.7f), new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 2011, 0.4f, 1.2f, 90, 1.6f, 2.4f, 0.08f, 0.14f, Color.white);
        { var m = cf.main; m.startColor = new MinMaxGradient(RainbowGrad()) { mode = ParticleSystemGradientMode.RandomColor }; m.startSize3D = true; m.startSizeX = new MinMaxCurve(0.08f, 0.14f); m.startSizeY = new MinMaxCurve(0.04f, 0.07f); m.startSizeZ = 1;
          m.startRotation = new MinMaxCurve(0, 6.28f); m.gravityModifier = 0.35f; m.startSpeed = new MinMaxCurve(0f, 1f);
          var sh = cf.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(14f, 0.1f, 0.1f);
          var rot = cf.rotationOverLifetime; rot.enabled = true; rot.z = new MinMaxCurve(-6f, 6f); var n = cf.noise; n.enabled = true; n.strength = 0.8f; n.frequency = 0.8f;
          cf.emission.SetBursts(new[] { new Burst(0, 60) }); }
    }
    // C11 敵のカットイン: 暗い赤と赤い稲妻、左から入る
    static void CutEnemy(Ctx c)
    {
        CutIn(c, new CutStyle { mode = 1, bas = new Color(0.1f, 0f, 0f), c1 = new Color(1f, 0.1f, 0.05f), c2 = new Color(1f, 0.45f, 0.3f), c3 = new Color(2.2f, 0.8f, 0.5f), speed = 3.8f, angle = 9f,
            chevron = true, chevA = new Color(0.8f, 0.05f, 0.02f), chevB = new Color(0.15f, 0f, 0f), bolts = true }, c.goblin, 0.25f, charH: 5.2f, charOff: new Vector2(-1.4f, 0.5f), side: -1f);
        c.OnUpdate(t => { if (t > 0.3f && t < 1.45f) c.post.tint = new Color(1f, 0.3f, 0.3f, 0.3f); });
    }

    // ---------- シールド・バフ 5 ----------
    static Vector3 BuffHero(Ctx c) { var h = HeroHome + new Vector3(1.4f, 0, 0); c.heroT.position = h; c.heroT.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DimMul", 0.1f); return h; }
    // B1 結界: 足元の方陣から光の筋が立ち上り、光の筒になる
    static void BuffBarrier(Ctx c)
    {
        var hero = BuffHero(c); var col = new Color(0.4f, 0.85f, 1f); float on = 0.3f;
        var feet = new Vector3(hero.x, GroundY + 0.12f, 0);
        Surge(c, on, hero, feet, col, 0.8f);
        var mc = Quad(c.root, "BarrierCircle", feet + new Vector3(0, 0, 0.2f), new Vector2(4.2f, 4.2f), QMat(c, c.tx.magic, 0, Vector2.one, 0)); var mm = mc.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { mc.rotation = Quaternion.Euler(72, 0, 0) * Quaternion.Euler(0, 0, t * 40f); mm.SetColor("_Tint", col * (t < on ? 0 : 1.4f * Mathf.Clamp01((t - on) / 0.2f))); });
        var st = PS(c, "BarrierStreaks", feet, AddMat(c.tx.streak, 2.2f), 2101);
        { var m = st.main; m.duration = 3f; m.startLifetime = new MinMaxCurve(0.6f, 1f); m.startSpeed = 0; m.startSize3D = true; m.startSizeX = new MinMaxCurve(0.9f, 1.8f); m.startSizeY = new MinMaxCurve(0.05f, 0.1f); m.startSizeZ = 1; m.startRotation = 90 * Mathf.Deg2Rad; m.startColor = col;
          m.scalingMode = ParticleSystemScalingMode.Shape; var e = st.emission; e.rateOverTime = 60; var sh = st.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.9f; sh.radiusThickness = 0; st.transform.localScale = new Vector3(1, 0.25f, 1);
          var v = st.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(0f, 0f); v.y = new MinMaxCurve(2f, 3.5f); v.z = new MinMaxCurve(0f, 0f);
          ColorLife(st, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.2f, 1f), (1f, 0f) })); c.Play(st, on); }
        var top = Quad(c.root, "BarrierTop", feet, new Vector2(4.2f, 1.05f), QMat(c, c.tx.lightRing ?? c.tx.ring, 0, Vector2.one, 0)); var tm2 = top.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { float a = t - on; top.position = feet + new Vector3(0, Mathf.Clamp01(a / 0.5f) * 3.4f, -0.2f); tm2.SetColor("_Tint", col * (a < 0 ? 0 : 1.2f)); });
        var dome = Quad(c.root, "BarrierGlow", hero + new Vector3(0, 0.4f, 0.3f), new Vector2(4.6f, 5.2f), QMat(c, c.tx.glow, 0, Vector2.one, 0)); var dm = dome.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => dm.SetColor("_Tint", col * (t < on ? 0 : 0.35f + 0.08f * Mathf.Sin(t * 5f))));
        c.OnUpdate(t => { if (t >= on) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.35f); });
    }
    // B2 六角の壁: 六角のタイルが下から 1 枚ずつ組み上がり、弾を受けると波紋が広がる
    static void BuffHexWall(Ctx c)
    {
        var hero = BuffHero(c); var col = new Color(0.35f, 0.85f, 1f); float on = 0.3f, hit = 1.45f;
        var hitP = hero + new Vector3(1.8f, 0.4f, -0.6f);
        int n = 0;
        for (int row = 0; row < 5; row++)
            for (int k = 0; k < 3; k++)
            {
                var p = new Vector3(hero.x + 1.0f + k * 0.78f, GroundY + 0.5f + row * 0.68f + (k % 2) * 0.34f, -0.6f);   // ハニカム
                var q = Quad(c.root, "Hex", p, new Vector2(0.9f, 0.8f), QMat(c, c.tx.hexTile, 0, Vector2.one, 0)); var m = q.GetComponent<MeshRenderer>().sharedMaterial;
                float ta = on + n * 0.035f; n++;
                var pp = p;
                c.OnUpdate(t =>
                {
                    float a = t - ta; q.gameObject.SetActive(a >= 0); if (a < 0) return;
                    float pop = EaseOutBack(Mathf.Clamp01(a / 0.12f));
                    float d = (pp - hitP).magnitude; float wave = t < hit ? 0 : Mathf.Exp(-Mathf.Pow((t - hit) * 6f - d * 2.2f, 2) * 2f);
                    q.localScale = new Vector3(0.9f, 0.8f, 1) * pop * (1 + 0.15f * wave);
                    float shimmer = 0.9f + 0.15f * Mathf.Sin(t * 5f + pp.y * 3f);
                    m.SetColor("_Tint", col * ((a < 2 / 60f ? 3f : 1.2f * shimmer) + 2.2f * wave));
                });
            }
        EnemyShot(c, GoblinHome + new Vector3(-0.6f, 0.3f, -0.6f), hitP, hit, new Color(1f, 0.3f, 0.8f), 2111);
        Flare(c, hitP, hit, col, 2.4f, 2112); Shards(c, hitP, hit, col, 1f, 20, 100, 16, 2113); Impact(c, hit, -1, 180f, enemy: false);
        c.OnUpdate(t => { if (t >= on) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.3f); });
    }
    // B3 回復の花: 緑の花びらが螺旋に舞い、十字が昇る。+30
    static void BuffHealBloom(Ctx c)
    {
        var hero = BuffHero(c); var col = new Color(0.4f, 1f, 0.5f); float on = 0.3f;
        Surge(c, on, hero, new Vector3(hero.x, GroundY + 0.02f, 0), col, 0.8f);
        var pet = PS(c, "Petals", hero + new Vector3(0, -1.2f, -0.6f), AddMat(c.tx.diamond, 2.2f), 2121);
        { var m = pet.main; m.duration = 2f; m.simulationSpace = ParticleSystemSimulationSpace.Local; m.startLifetime = new MinMaxCurve(1.2f, 1.6f); m.startSpeed = 0; m.startSize3D = true; m.startSizeX = new MinMaxCurve(0.14f, 0.22f); m.startSizeY = new MinMaxCurve(0.3f, 0.45f); m.startSizeZ = 1;
          m.startRotation = new MinMaxCurve(0, 6.28f); m.startColor = new MinMaxGradient(col, new Color(0.8f, 1f, 0.6f));
          var e = pet.emission; e.rateOverTime = 40; var sh = pet.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.3f; sh.radiusThickness = 0;
          var v = pet.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local; v.y = new MinMaxCurve(1.3f, 1.3f); v.x = new MinMaxCurve(0f, 0f); v.z = new MinMaxCurve(0f, 0f); v.orbitalZ = new MinMaxCurve(3.5f); v.radial = new MinMaxCurve(-0.35f);
          var rot = pet.rotationOverLifetime; rot.enabled = true; rot.z = new MinMaxCurve(-4f, 4f);
          ColorLife(pet, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.15f, 1f), (0.8f, 1f), (1f, 0f) })); c.Play(pet, on); }
        var cr = SimplePS(c, "HealCross", hero, AddMat(c.tx.plus, 3f), 2122, on + 0.1f, 1.8f, 8, 0.7f, 0.9f, 0.3f, 0.5f, col);
        { var sh = cr.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.8f, 2.2f, 0.1f); var v = cr.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(0f, 0f); v.y = new MinMaxCurve(0.8f, 1.4f); v.z = new MinMaxCurve(0f, 0f);
          SizeLife(cr, Curve((0, 0), (0.25f, 1), (1, 0.4f))); ColorLife(cr, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.2f, 1f), (1f, 0f) })); }
        var tm = Label(c, "+30", hero + new Vector3(0, 1.8f, -3.6f), 0.06f, new Color(0.5f, 1f, 0.55f));
        c.OnUpdate(t => { float a = t - (on + 0.25f); bool v = a >= 0 && a < 1.2f; tm.gameObject.SetActive(v); if (!v) return; tm.transform.position = hero + new Vector3(0, 1.8f + a * 0.6f, -3.6f); tm.transform.localScale = Vector3.one * (a < 0.1f ? Mathf.LerpUnclamped(1.8f, 1f, EaseOutBack(a / 0.1f)) : 1f); var cc = tm.color; cc.a = 1 - Mathf.Clamp01((a - 0.9f) / 0.3f); tm.color = cc; });
        c.OnUpdate(t => { float a = t - on; HeroGlow(c, a < 0 ? 0 : 0.45f * (0.6f + 0.4f * Mathf.Sin(a * 8f)) * (1 - Mathf.Clamp01((a - 1.6f) / 0.4f)), col); });
    }
    // B4 攻撃力上昇: 赤い矢印が昇り、足元に炎の輪。ATK UP
    static void BuffPower(Ctx c)
    {
        var hero = BuffHero(c); var col = new Color(1f, 0.3f, 0.08f); float on = 0.3f;
        Surge(c, on, hero, new Vector3(hero.x, GroundY + 0.02f, 0), col, 1f);
        Flip(c, "PowerRing", new Vector3(hero.x, GroundY + 0.35f, -0.6f), c.tx.fbFireRing, 6, 5, 0, 30, 0.7f, 4f, Color.white, 1.3f, 2131, t: on, randomRot: false);
        var ar = SimplePS(c, "PowerArrows", hero + new Vector3(0, -0.8f, -0.6f), AddMat(c.tx.arrowUp, 2.6f), 2132, on, 1.6f, 14, 0.5f, 0.8f, 0.4f, 0.7f, col);
        { var sh = ar.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.2f, 1.6f, 0.1f); var v = ar.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(0f, 0f); v.y = new MinMaxCurve(2.5f, 4f); v.z = new MinMaxCurve(0f, 0f);
          SizeLife(ar, Curve((0, 0.4f), (0.2f, 1.1f), (1, 0.8f))); ColorLife(ar, Grad(new[] { (0f, Color.white), (0.3f, new Color(1f, 0.6f, 0.2f)), (1f, col) }, new[] { (0f, 0f), (0.15f, 1f), (1f, 0f) })); }
        PopLabel(c, "ATK UP", hero + new Vector3(0, 2f, -3.6f), 0.055f, new Color(1f, 0.35f, 0.15f), on + 0.2f, 1.2f);
        c.OnUpdate(t => { float a = t - on; HeroGlow(c, a < 0 ? 0 : 0.5f * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(a * 9f))) * (1 - Mathf.Clamp01((a - 1.4f) / 0.4f)), col); });
    }
    // B5 素早さ: 風が主人公にまとわりつき、左右に素早く動いて残像。SPD UP
    static void BuffSpeed(Ctx c)
    {
        var hero = BuffHero(c); var col = new Color(0.4f, 0.9f, 1f); float on = 0.3f;
        var hist = new List<(float t, Vector3 p)>();
        Func<float, Vector3> PosAt = t => hero + new Vector3(t < on ? 0 : Mathf.Sin((t - on) * 9f) * 0.9f * Mathf.Clamp01((2.2f - t) / 0.3f), 0, 0);
        c.OnUpdate(t => { c.heroT.position = PosAt(t); });
        for (int k = 1; k <= 3; k++)
        {
            var q = Quad(c.root, "SpeedGhost" + k, hero, SpriteSize(c.hero, 3.3f), QMat(c, c.hero, 0, Vector2.one, 0)); var m = q.GetComponent<MeshRenderer>().sharedMaterial; float lag = 0.045f * k, kk = k;
            c.OnUpdate(t => { bool on2 = t > on && t < 2.2f; q.gameObject.SetActive(on2); q.position = PosAt(t - lag) + new Vector3(0, 0, 0.05f * kk); m.SetColor("_Tint", col * (0.55f - 0.14f * kk)); });
        }
        var wind = PS(c, "Wind", hero, AddMat(c.tx.dot, 3f), 2141);
        { var m = wind.main; m.duration = 2f; m.simulationSpace = ParticleSystemSimulationSpace.Local; m.startLifetime = 0.6f; m.startSpeed = 0; m.startSize = 0.06f; m.startColor = col;
          var e = wind.emission; e.rateOverTime = 26; var sh = wind.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.3f; sh.radiusThickness = 0; wind.transform.localScale = new Vector3(1, 0.55f, 1);
          var v = wind.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local; v.orbitalZ = new MinMaxCurve(7f); v.y = new MinMaxCurve(0.6f, 0.6f); v.x = new MinMaxCurve(0f, 0f); v.z = new MinMaxCurve(0f, 0f);
          Trail(wind, AddMat(c.tx.trail, 2.8f), new MinMaxCurve(0.22f, 0.22f), worldSpace: false); ColorLife(wind, Grad(new[] { (0f, Color.white), (1f, col) }, new[] { (0f, 0f), (0.15f, 1f), (1f, 0f) })); c.Play(wind, on); }
        Surge(c, on, hero, new Vector3(hero.x, GroundY + 0.02f, 0), col, 0.7f);
        PopLabel(c, "SPD UP", hero + new Vector3(0, 2f, -3.6f), 0.055f, new Color(0.45f, 0.95f, 1f), on + 0.2f, 1.2f);
    }

    // ---------- 倒れる・獲得 5 ----------
    // D7 凍って砕ける
    static void DeathFreeze(Ctx c)
    {
        var ice = new Color(0.55f, 0.85f, 1f);
        var ov = Quad(c.root, "IceOverlay", GoblinHome, SpriteSize(c.goblin, 2.7f), QMat(c, c.goblin, 0, Vector2.one, 0)); var om = ov.GetComponent<MeshRenderer>().sharedMaterial;
        c.OnUpdate(t => { ov.gameObject.SetActive(c.goblinT.gameObject.activeSelf); ov.position = c.goblinT.position + new Vector3(0, 0, -0.03f); om.SetColor("_Tint", ice * (0.8f * Mathf.Clamp01((t - 0.2f) / 0.6f))); });
        var frost = SimplePS(c, "Frost", G, AddMat(c.tx.sparkle ?? c.tx.dot, 2.2f), 2151, 0.2f, 0.8f, 30, 0.4f, 0.7f, 0.1f, 0.22f, ice);
        { var sh = frost.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 2.4f, 0.1f); }
        c.OnUpdate(t => { if (t > 0.2f && t < 1.9f) { c.post.tint = new Color(0.7f, 0.85f, 1f, 0.3f * Mathf.Clamp01((t - 0.2f) / 0.4f)); c.post.stageDim = Mathf.Max(c.post.stageDim, 0.3f); } });
        SlashThrough(c, G, -30, 25, 2.6f, 1.1f, 160, Cyan, 0.92f);
        Hit(c, G, 1.0f, Cyan.mid, 1.4f, -30, 360, 30, 2152);
        DeathShatterWith(c, new Color(1.6f, 2.6f, 4f), new Color(0.6f, 0.85f, 1f, 0.9f), false, 1.0f);
        CrystalBurst(c, G, 1.01f, ice, 30, 8f, 2153, 0.45f);
    }
    // D8 渦に吸い込まれる
    static void DeathBlackhole(Ctx c)
    {
        var col = new Color(0.65f, 0.35f, 1f);
        SlashThrough(c, G, 0, 70, 2.5f, 1.1f, 150, Crimson, 0.25f); Impact(c, 0.3f, 1, 0);
        Flip(c, "Vortex", G + new Vector3(0, 0.1f, -0.3f), c.tx.fbVortex, 6, 5, 1, 23, 1.3f, 6f, Color.white, 1.4f, 2161, t: 0.38f, randomRot: false);
        var baseScale = c.goblinT.localScale;
        c.OnUpdate(t =>
        {
            float a = Mathf.Clamp01((t - 0.45f) / 0.8f), e = a * a;
            c.goblinT.localScale = baseScale * (1 - e); c.goblinT.rotation = Quaternion.Euler(0, 0, e * 540f);
            c.post.goblinOff += (G - GoblinHome) * e; c.goblinT.gameObject.SetActive(t < 1.25f);
            if (t > 0.38f && t < 1.6f) { c.post.tint = new Color(0.7f, 0.5f, 1f, 0.35f); c.post.stageDim = Mathf.Max(c.post.stageDim, 0.5f); c.post.trauma += 0.08f; }
            if (t >= 1.25f && t < 1.28f) c.post.flash = 0.7f;
        });
        var cv = PS(c, "Converge", G, AddMat(c.tx.diamond, 3f), 2162);
        { var m = cv.main; m.duration = 0.8f; m.startLifetime = 0.45f; m.startSpeed = -6f; m.startSize = new MinMaxCurve(0.05f, 0.1f); m.startColor = col; var e = cv.emission; e.rateOverTime = 90;
          var sh = cv.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 2.8f; sh.radiusThickness = 0;
          var r = cv.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f; c.Play(cv, 0.45f); }
        Ring(c, G, 1.25f, col, 7f, 2163); Flare(c, G, 1.25f, col, 3.6f, 2164); Bokeh(c, G, 1.25f, col, 1.6f, 2165);
    }
    // D9 爆発四散
    static void DeathExplode(Ctx c)
    {
        SlashThrough(c, G, -135, 20, 2.2f, 0.95f, 150, Flame, 0.25f);
        Impact(c, 0.3f, 2, -135);
        c.OnUpdate(t => c.goblinT.gameObject.SetActive(t < 0.31f));
        Flip(c, "ExBig", G, c.tx.fbBigHit, 6, 5, 1, 12, 0.4f, 7f, Color.white, 1.8f, 2171, t: 0.31f);
        Flip(c, "ExRing", G + new Vector3(0, -0.2f, -0.2f), c.tx.fbFireRing, 6, 5, 0, 30, 0.9f, 6f, Color.white, 1.4f, 2172, t: 0.33f, randomRot: false);
        for (int k = 0; k < 4; k++) Flip(c, "ExSmoke" + k, G + new Vector3((k - 1.5f) * 0.6f, 0.2f + (k % 2) * 0.3f, -0.1f), c.tx.fbSmoke, 8, 8, k * 6, 40 + k * 6, 1.3f, 3.2f, new Color(0.25f, 0.22f, 0.2f, 0.75f), 1f, 2173 + (uint)k, delay: 0.1f, alpha: true, t: 0.31f);
        foreach (var d in new[] { 30f, 90f, 150f }) RealSparks(c, G, d, 0.31f, 40, 13f, 2180 + (uint)d);
        Shards(c, G, 0.31f, new Color(1f, 0.5f, 0.1f), 2f, 0, 360, 40, 2190);
        c.OnUpdate(t => { float a = t - 0.31f; if (a >= 0 && a < 0.5f) c.post.shock = new Vector4(WorldToUv(G).x, WorldToUv(G).y, EaseOut(a / 0.5f) * 0.6f, 1.3f * (1 - a / 0.5f)); });
        var em = SimplePS(c, "ExEmbers", G, AddMat(c.tx.dot, 4f), 2191, 0.35f, 1f, 40, 0.8f, 1.6f, 0.03f, 0.06f, Color.white);
        { var sh = em.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.8f; var v = em.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new MinMaxCurve(-0.5f, 0.5f); v.y = new MinMaxCurve(1f, 2.5f); v.z = new MinMaxCurve(0f, 0f);
          ColorLife(em, Grad(new[] { (0f, new Color(1f, 0.9f, 0.6f)), (0.5f, new Color(1f, 0.45f, 0.1f)), (1f, new Color(0.6f, 0.08f, 0.02f)) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) })); }
    }
    // D10 画素になって消える
    static void DeathPixel(Ctx c)
    {
        SlashThrough(c, G, 0, 70, 2.5f, 1.1f, 150, Steel, 0.25f); Impact(c, 0.3f, 1, 0);
        var dm = BakeDeath(c, (u, v) => { float ix = Mathf.Floor(u * 12f), iy = Mathf.Floor(v * 14f); float h = Mathf.Repeat(Mathf.Sin(ix * 12.9898f + iy * 78.233f) * 43758.5453f, 1f); return h * 0.65f + (1 - v) * 0.35f; }, 0, 2201);
        var mat = DeathMat(c, dm, new Color(0.5f, 2.4f, 3.4f), 0.06f, new Color(0, 0, 0, 0), 0f);
        c.goblinT.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Func<float, float> cut = t => Mathf.Clamp01((t - 0.45f) / 1.0f) * 1.02f;
        c.OnUpdate(t => { mat.SetFloat("_Cut", cut(t)); if (t > 0.45f && t < 1.5f) c.post.glitch = 0.18f; });
        DeathDim(c, 0.4f, 1.1f);
        var bits = DeathParticles(c, "PixelBits", new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 2202, 0.6f, 0f, -0.12f,
            Grad(new[] { (0f, Color.white), (1f, new Color(0.4f, 0.9f, 1f)) }, new[] { (0f, 1f), (0.7f, 0.9f), (1f, 0f) }));
        FrontEmitter(c, dm, OnSprite(c.goblinT), cut, bits, (Vector2 uv, Color src, System.Random r, out Color col, out Vector3 vel, out float life, out float sz) =>
        { col = src; vel = new Vector3(((float)r.NextDouble() - 0.5f) * 0.6f, 0.6f + (float)r.NextDouble() * 1.4f, 0); life = 0.6f + (float)r.NextDouble() * 0.6f; sz = 0.09f; return r.NextDouble() < 0.12; }, 2203);
    }
    // D11 宝石が落ちて集まる（色とりどりの結晶）
    static void DropGems(Ctx c) => DropCollect(c, "shard", new Color(1f, 0.9f, 0.7f), 22, 2211, rainbow: true);

    // ---------- 斬撃・打撃 5 ----------
    // H1 突き: 主人公が踏み込み、光の槍が一直線に敵を貫く
    static void HitThrust(Ctx c)
    {
        float t0 = 0.3f;
        c.OnUpdate(t => { float a = t - t0; c.heroT.position = HeroHome + new Vector3(a < 0 ? 0 : a < 0.08f ? 2f * EaseOut(a / 0.08f) : a < 0.6f ? 2f : 2f * (1 - Mathf.Clamp01((a - 0.6f) / 0.2f)), 0, 0); });
        var mid = (HeroHome + G) * 0.5f + new Vector3(0.6f, 0.1f, -0.8f);
        CutLine(c, mid, 0, 9f, t0 + 0.05f, PCyan); CutLine(c, mid + new Vector3(0, 0.12f, 0), 0, 7f, t0 + 0.07f, Color.white);
        Flare(c, G, t0 + 0.07f, PCyan, 2.8f, 2301);
        Ring(c, G + new Vector3(0.3f, 0, 0), t0 + 0.07f, PCyan, 3.2f, 2302, squash: 2.2f);
        Ring(c, G + new Vector3(1.0f, 0, 0), t0 + 0.12f, Color.white, 2.2f, 2303, squash: 2.2f);
        Shards(c, G, t0 + 0.07f, PCyan, 1.5f, 0, 28, 34, 2304);
        Impact(c, t0 + 0.07f, 1, 0);
        var sp = SimplePS(c, "ThrustLines", mid, AddMat(c.tx.streak, 1.6f), 2305, t0, 0.2f, 60, 0.12f, 0.2f, 1f, 1f, PCyan);
        { var m = sp.main; m.startSize3D = true; m.startSizeX = new MinMaxCurve(2f, 4f); m.startSizeY = 0.03f; m.startSizeZ = 1; var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(4f, 0.9f, 0.1f); }
    }
    // H2 打撃（ハンマー）: 上から叩きつけ、重い止め・地面の輪・ひび・砂煙
    static void HitBlunt(Ctx c)
    {
        float th = 0.34f; var ground = new Vector3(G.x, GroundY + 0.15f, -0.6f);
        SlashThrough(c, G + new Vector3(0, 0.4f, 0), -90, 10, 2.8f, 1.3f, 100, Gold, 0.26f, dur: 0.06f);
        Impact(c, th, 2, -90);
        Hit(c, G, th, Gold.mid, 1.3f, 90, 120, 26, 2311);
        Ring(c, ground, th, new Color(1f, 0.8f, 0.4f), 7f, 2312, squash: 0.22f); Ring(c, ground, th + 0.05f, Color.white, 5f, 2313, squash: 0.22f);
        for (int k = 0; k < 4; k++) Flip(c, "BluntDust" + k, ground + new Vector3((k - 1.5f) * 0.8f, 0.3f, 0.2f), c.tx.fbSmoke, 8, 8, k * 7, 40 + k * 7, 1.1f, 2.6f, new Color(0.75f, 0.62f, 0.48f, 0.7f), 1f, 2314 + (uint)k, delay: 0.02f, alpha: true, t: th);
        var rnd = new System.Random(2320);
        for (int k = 0; k < 7; k++)
        {
            float ang = Mathf.PI * (0.05f + 0.9f * k / 6f);
            var end = ground + new Vector3(Mathf.Cos(ang) * 2.4f, -Mathf.Sin(ang) * 0.35f, 0);
            var arr = Jagged(ground, end, 0.12f, 4, rnd).ToArray();
            var lr = new GameObject("GroundCrack").AddComponent<LineRenderer>(); lr.transform.SetParent(c.root, false);
            lr.useWorldSpace = true; lr.sharedMaterial = new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.trail }; lr.widthMultiplier = 0.05f; lr.startColor = lr.endColor = new Color(0.12f, 0.08f, 0.05f, 0.9f);
            lr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0.2f);
            c.OnUpdateReal(rt => { int cnt = Mathf.RoundToInt(Mathf.Clamp01((rt - c.Real(th)) / 0.08f) * arr.Length); lr.positionCount = cnt; for (int i = 0; i < cnt; i++) lr.SetPosition(i, arr[i]); });
        }
        c.OnUpdateReal(rt => { float a = rt - c.Real(th); if (a >= 0 && a < 0.45f) c.post.shock = new Vector4(WorldToUv(ground).x, WorldToUv(ground).y, EaseOut(a / 0.45f) * 0.5f, 1.2f * (1 - a / 0.45f)); });
    }
    // H3 火球: 手元で溜め → 火球が飛んで爆発
    static void HitFireball(Ctx c)
    {
        var hand = HeroHome + new Vector3(0.9f, 0.4f, -0.8f); float t1 = 0.6f, t2 = 0.95f;
        Flip(c, "FbCharge", hand, c.tx.fbCharge, 7, 6, 0, 11, 0.35f, 2.4f, new Color(1f, 0.55f, 0.2f), 2.2f, 2331, t: 0.25f, randomRot: false);
        var ball = new GameObject("Fireball").transform; ball.SetParent(c.root, false); ball.position = hand;
        var core = Quad(ball, "Core", Vector3.zero, new Vector2(0.9f, 0.9f), QMat(c, c.tx.glow, 0, Vector2.one, 0)); var cm = core.GetComponent<MeshRenderer>().sharedMaterial;
        var fire = PS(c, "FbFire", hand, FireMat(c, c.tx.blCampfire ?? c.tx.fbFlame, FireRed, 1.4f), 2332, ball);
        { var m = fire.main; m.duration = t2 - t1; m.startLifetime = new MinMaxCurve(0.2f, 0.35f); m.startSize = new MinMaxCurve(0.5f, 0.9f); m.startRotation = new MinMaxCurve(-1.8f, -1.3f); var e = fire.emission; e.rateOverTime = 90;
          var grid = GridOf(c.tx.blCampfire ?? c.tx.fbFlame); var ts = fire.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = grid.x; ts.numTilesY = grid.y; ts.frameOverTime = new MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f)); ts.startFrame = new MinMaxCurve(0, grid.x * grid.y - 1);
          ColorLife(fire, Grad(new[] { (0f, Color.white), (1f, Color.white) }, new[] { (0f, 1f), (1f, 0f) })); c.Play(fire, t1); }
        c.OnUpdate(t =>
        {
            float a = Mathf.Clamp01((t - t1) / (t2 - t1));
            ball.position = Vector3.Lerp(hand, G + new Vector3(0, 0, -0.8f), a) + new Vector3(0, Mathf.Sin(a * Mathf.PI) * 0.5f, 0);
            core.gameObject.SetActive(t >= 0.4f && t < t2); cm.SetColor("_Tint", new Color(1f, 0.6f, 0.2f) * (t < t1 ? 1.2f * Mathf.Clamp01((t - 0.4f) / 0.2f) : 2f));
        });
        Impact(c, t2, 1, 0);
        Flip(c, "FbBoom", G, c.tx.fbBigHit, 6, 5, 1, 12, 0.35f, 5.5f, Color.white, 1.8f, 2333, t: t2);
        Flip(c, "FbRing", G + new Vector3(0, -0.2f, -0.2f), c.tx.fbFireRing, 6, 5, 0, 30, 0.7f, 4.8f, Color.white, 1.4f, 2334, t: t2, randomRot: false);
        RealSparks(c, G, 60, t2, 50, 12f, 2335);
        for (int k = 0; k < 3; k++) Flip(c, "FbSmoke" + k, G + new Vector3((k - 1) * 0.6f, 0.3f, -0.1f), c.tx.fbSmoke, 8, 8, k * 8, 40 + k * 8, 1.2f, 2.8f, new Color(0.3f, 0.26f, 0.24f, 0.7f), 1f, 2336 + (uint)k, delay: 0.08f, alpha: true, t: t2);
    }
    // H4 氷の槍: 上から 3 本が突き刺さり、最後に砕ける
    static void HitIce(Ctx c)
    {
        var ice = new Color(0.55f, 0.85f, 1f);
        var mat = new Material(Shader.Find("Lab/Crystal")); mat.SetFloat("_Glow", 1.9f);
        var times = new[] { 0.3f, 0.45f, 0.6f }; var xs = new[] { -0.5f, 0.35f, -0.05f };
        var mesh = ColoredShardMesh(ice);
        var spears = new List<Transform>();
        for (int k = 0; k < 3; k++)
        {
            var go = new GameObject("IceSpear" + k); go.transform.SetParent(c.root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            float tk = times[k]; var target = new Vector3(G.x + xs[k], G.y - 0.3f + k * 0.15f, -0.9f); float tilt = (k - 1) * 12f;
            go.transform.rotation = Quaternion.Euler(0, 20f * k, 180 + tilt); go.transform.localScale = new Vector3(0.45f, 1.9f, 0.45f);
            c.OnUpdate(t =>
            {
                float a = (t - tk) / 0.08f; go.SetActive(a >= 0 && t < 1.3f);
                go.transform.position = Vector3.Lerp(target + new Vector3(Mathf.Sin(tilt * Mathf.Deg2Rad) * 6f, 6f, 0), target, Mathf.Clamp01(a));
            });
            Flare(c, target + new Vector3(0, -0.6f, 0), tk + 0.08f, ice, 2f, 2341 + (uint)k);
            CrystalBurst(c, target + new Vector3(0, -0.6f, 0), tk + 0.08f, ice, 10, 6f, 2344 + (uint)k, 0.25f);
            Impact(c, tk + 0.08f, k < 2 ? -1 : 0, -90);
        }
        Ring(c, new Vector3(G.x, GroundY + 0.2f, -0.6f), 0.68f, ice, 5f, 2350, squash: 0.25f);
        Flip(c, "IceMist", G + new Vector3(0, -0.5f, -0.2f), c.tx.fbSmoke, 8, 8, 0, 40, 1.2f, 3.2f, new Color(0.8f, 0.9f, 1f, 0.6f), 1f, 2351, alpha: true, t: 0.7f);
        CrystalBurst(c, G, 1.3f, ice, 40, 9f, 2352, 0.45f); Impact(c, 1.3f, 1, 90); Flare(c, G, 1.3f, ice, 3.4f, 2353);
    }
    // H5 連撃: 3 段（色が変わる）→ 最後に十字
    static void HitCombo(Ctx c)
    {
        var steps = new[] { (0.25f, 20f, Steel, -1), (0.42f, -160f, Cyan, 0), (0.6f, 60f, Gold, 0) };
        int k = 0;
        foreach (var (tt, rot, st, lvl) in steps)
        {
            SlashThrough(c, G + new Vector3(0, 0.1f * k, 0), rot, 25, 2.2f, 0.9f, 140, st, tt - 0.05f, dur: 0.05f);
            Hit(c, G, tt, st.mid, 0.9f + 0.15f * k, rot, 80, 16, 2361 + (uint)k);
            Impact(c, tt, lvl, rot); k++;
        }
        float tf = 0.85f;
        SlashThrough(c, G, -45, 20, 2.4f, 1f, 150, Flame, tf - 0.06f); SlashThrough(c, G, -135, 20, 2.4f, 1f, 150, Flame, tf - 0.03f);
        Hit(c, G, tf, Flame.mid, 1.6f, 0, 360, 40, 2365); CutLine(c, G, 45, 5f, tf, Flame.mid); CutLine(c, G, -45, 5f, tf, Flame.mid);
        Impact(c, tf, 2, 0);
        c.OnUpdate(t => c.heroT.position = HeroHome + new Vector3(Mathf.Min(1.2f, Mathf.Floor(Mathf.Clamp01((t - 0.2f) / 0.7f) * 4f) * 0.35f), 0, 0));
    }

    // ================= 扉のステップアップ（本人 2026-09-29「大門・大扉・シャッターが閉まり、第 1〜第 2 停止でぐらつき、第 3 で開く。これがステップアップで起こる」）=================
    // 閉まる（0.3 秒・叩きつけて跳ね返る・砂ぼこり）→ 第 1 停止: ぐらつき＋合わせ目から青い光 → 第 2 停止: 大きくぐらつき＋赤い光が漏れる＋火花
    // → 第 3 停止: 光が噴き出して一気に開く（白・金の光・虹の輪）。合わせ目の光の色で期待度（青 → 赤 → 虹）
    static Material DoorMat(Texture2D tex) { var m = new Material(Shader.Find("Lab/Sprite")) { mainTexture = tex }; m.SetColor("_Color", Color.white); m.renderQueue = 2990; return m; }   // 扉はエフェクト（3000）より先に描く

    static void DoorStep(Ctx c, string kind)
    {
        float tClose = 0.3f, s1 = 1.0f, s2 = 1.7f, s3 = 2.45f;
        var blue = new Color(0.35f, 0.7f, 1f); var red = new Color(1f, 0.25f, 0.12f); var gold = new Color(1f, 0.82f, 0.35f);
        bool shutter = kind == "shutter", vault = kind == "vault", goldK = kind == "gold";
        Texture2D texL = shutter ? c.tx.shutter : vault ? c.tx.doorVault : goldK ? c.tx.doorGold : c.tx.doorGate;
        var parts = new List<(Transform tr, Material m, float side)>();
        if (shutter) { var m = DoorMat(texL); parts.Add((Quad(c.root, "Shutter", new Vector3(0, 0, -3.3f), new Vector2(12.9f, 7.25f), m), m, 0)); }
        else
            foreach (var side in new[] { -1f, 1f })
            {
                var texR = vault ? c.tx.doorVaultR : goldK ? c.tx.doorGoldR : c.tx.doorGateR;
                var m = DoorMat(side > 0 && texR != null ? texR : texL); var q = Quad(c.root, side < 0 ? "DoorL" : "DoorR", new Vector3(side * 3.2f, 0, -3.3f), new Vector2(6.45f, 7.25f), m);
                if (side > 0 && texR == null) q.localScale = new Vector3(-6.45f, 7.25f, 1);   // 右の絵が無ければ左を反転
                parts.Add((q, m, side));
            }
        Transform lockT = null; Material lockM = null;
        if (vault) { lockM = DoorMat(c.tx.doorLock); lockM.renderQueue = 2991; lockT = Quad(c.root, "VaultLock", new Vector3(0, 0, -3.35f), new Vector2(3.0f, 3.0f), lockM); }
        // 合わせ目の光（ドアの隙間。シャッターは下の隙間）
        var seam = Quad(c.root, "Seam", shutter ? new Vector3(0, -3.55f, -3.25f) : new Vector3(0, 0, -3.25f), shutter ? new Vector2(13f, 0.5f) : new Vector2(0.5f, 7.4f), QMat(c, c.tx.glow, 0, Vector2.one, 0));
        var seamM = seam.GetComponent<MeshRenderer>().sharedMaterial; seamM.renderQueue = 3001;
        var rnd = new System.Random(kind.Length * 97);
        Vector3 jit = Vector3.zero; int lastJ = -1;
        c.OnUpdate(t =>
        {
            // 閉まる・ぐらつく・開く
            float close = EaseIn(Mathf.Clamp01((t - tClose) / 0.22f));
            float bounce = t > tClose + 0.22f ? Mathf.Sin((t - tClose - 0.22f) * 40f) * Mathf.Exp(-(t - tClose - 0.22f) * 12f) * 0.08f : 0;
            float open = t < s3 ? 0 : EaseOut(Mathf.Clamp01((t - s3 - 0.05f) / 0.25f));
            float shake = 0;
            foreach (var (ts, amp) in new[] { (s1, 0.06f), (s2, 0.14f) }) { float a = t - ts; if (a >= 0 && a < 0.6f) shake = Mathf.Max(shake, amp * Mathf.Exp(-a * 5f)); }
            if (t >= s2 && t < s3) shake = Mathf.Max(shake, 0.02f);                    // 第 2 停止の後は小刻みに震え続ける
            if (t >= s3 - 0.02f && t < s3 + 0.05f) shake = 0.18f;
            int st = Mathf.FloorToInt(t * 30); if (st != lastJ) { lastJ = st; jit = new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, 0) * 2f; }
            foreach (var (tr, m, side) in parts)
            {
                Vector3 p;
                if (shutter) p = new Vector3(0, Mathf.Lerp(7.4f, 0, close) + bounce * 2 + open * 7.6f + (t >= s2 && t < s3 ? 0.18f : 0), -3.3f);   // 第 2 停止でガコッと少し上がる
                else p = new Vector3(side * (3.2f + Mathf.Lerp(3.4f, 0, close) + bounce * side + open * 3.6f), 0, -3.3f);
                if (vault && t >= s3) { float sw = open; var sc = tr.localScale; tr.localScale = new Vector3((sc.x < 0 ? -1 : 1) * 6.45f * (1 - sw * 0.85f), 7.25f, 1); p.x = side * (6.45f * 0.5f * (1 - sw * 0.85f) + sw * 3.2f + 0.02f); }   // 金庫は蝶番で開く
                tr.position = p + jit * shake;
                tr.rotation = Quaternion.Euler(0, 0, jit.x * shake * 6f);
                m.SetFloat("_Flash", t >= s3 && t < s3 + 2 / 60f ? 1f : 0f);
                m.SetColor("_Color", Color.white * (t >= s2 && t < s3 ? 0.9f + 0.1f * Mathf.Sin(t * 30f) : 1f));
            }
            if (lockT != null)
            {
                float spin = (t >= s1 ? Mathf.Min(1, (t - s1) / 0.3f) * 60f : 0) + (t >= s2 ? Mathf.Min(1, (t - s2) / 0.3f) * 90f : 0) + (t >= s3 ? (t - s3) * 1500f : 0);
                lockT.rotation = Quaternion.Euler(0, 0, -spin); lockT.position = new Vector3(0, open * -6f, -3.35f) + jit * shake;
                lockT.gameObject.SetActive(close > 0.99f && open < 0.99f); lockM.SetFloat("_Flash", t >= s3 && t < s3 + 3 / 60f ? 1f : 0f);
            }
            // 合わせ目の光: 第 1 停止で青く細く、第 2 停止で赤く太く脈打つ、第 3 停止で白く噴く
            Color sc2 = t < s2 ? blue : t < s3 ? red : Color.Lerp(gold, Color.white, 0.4f);
            float w = t < s1 ? 0 : t < s2 ? 0.35f : t < s3 ? 0.8f + 0.2f * Mathf.Sin(t * 20f) : 2.5f * (1 - open);
            float br = t < s1 ? 0 : t < s2 ? 1.2f * Mathf.Clamp01((t - s1) / 0.1f) : t < s3 ? 2f + 0.6f * Mathf.Sin(t * 20f) : 4f * (1 - open);
            seam.localScale = shutter ? new Vector3(13f, 0.5f * (0.4f + w), 1) : new Vector3(0.5f * (0.4f + w), 7.4f, 1);
            seamM.SetColor("_Tint", sc2 * br);
            // 画面
            if (t > tClose) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.4f);
            if (t >= tClose + 0.22f && t < tClose + 0.25f) c.post.flash = 0.3f;
            float a1 = t - (tClose + 0.22f); if (a1 >= 0 && a1 < 0.35f) c.post.trauma += 0.6f * (1 - a1 / 0.35f);
            foreach (var ts in new[] { s1, s2 }) { float a = t - ts; if (a >= 0 && a < 0.4f) c.post.trauma += (ts == s2 ? 0.7f : 0.4f) * (1 - a / 0.4f); }
            float b = t - s3; if (b >= 0 && b < 3 / 60f) c.post.flash = 1f; if (b >= 0 && b < 0.5f) c.post.trauma += 0.9f * (1 - b / 0.5f);
            if (b >= 0) c.post.tint = new Color(1f, 0.9f, 0.6f, 0.4f * (1 - Mathf.Clamp01(b / 0.8f)));
        });
        // 閉まった瞬間の砂ぼこり、停止ごとの落ちる埃・火花、開いた瞬間の光
        for (int k = 0; k < 4; k++) Flip(c, "DoorDust" + k, new Vector3(-4.5f + k * 3f, shutter ? -3.2f : -3.1f, -3.4f), c.tx.fbSmoke, 8, 8, k * 6, 40 + k * 6, 0.9f, 2.4f, new Color(0.75f, 0.68f, 0.6f, 0.6f), 1f, 2401 + (uint)k, alpha: true, t: tClose + 0.22f);
        foreach (var ts in new[] { s1, s2 })
        {
            var dust = SimplePS(c, "DoorDebris", new Vector3(0, 3.8f, -3.45f), new Material(Shader.Find("Lab/FxAlpha")) { mainTexture = c.tx.square }, 2410 + (uint)(ts * 10), ts, 0.3f, ts == s2 ? 60 : 30, 0.8f, 1.2f, 0.03f, 0.08f, new Color(0.6f, 0.55f, 0.5f));
            var m2 = dust.main; m2.gravityModifier = 1.2f; var sh = dust.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(12f, 0.1f, 0.1f);
        }
        if (!shutter) for (int k = 0; k < 3; k++) RealSparks(c, new Vector3(0, 1.8f - k * 1.8f, -3.45f), k % 2 == 0 ? 20 : 160, s2 + k * 0.05f, 25, 7f, 2420 + (uint)k);
        else RealSparks(c, new Vector3(0, -3.5f, -3.45f), 90, s2, 40, 8f, 2420);
        PopLabel(c, "第 1 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, blue, s1, 0.6f);
        PopLabel(c, "第 2 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, red, s2, 0.7f);
        PopLabel(c, "第 3 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, gold, s3, 0.8f);
        var burstAt = shutter ? new Vector3(0, -3.2f, -3.6f) : new Vector3(0, 0, -3.6f);
        Flare(c, burstAt, s3, gold, 8f, 2430); Burst(c, burstAt, s3, gold, 12f, 2431);
        var cols = new[] { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.3f, 1f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(0.8f, 0.4f, 1f) };
        for (int k = 0; k < cols.Length; k++) Ring(c, new Vector3(0, 0, -3.6f), s3 + 0.04f + k * 0.04f, cols[k], 14f - k * 1.6f, 2432 + (uint)k);
        Bokeh(c, new Vector3(0, 0, -3.6f), s3, gold, 4.5f, 2440);
    }
    static void DoorGate(Ctx c) => DoorStep(c, "gate");
    // 金の扉（本人 2026-09-29 の参考画像「こういうかんじ」: 金の浮き彫り・放射の筋・炎の飾り・中央の錠）。絵は blender/render_golddoor.py でモデリングして撮ったもの
    static void DoorGold(Ctx c) => DoorStep(c, "gold");
    static void DoorVault(Ctx c) { if (c.tx.vaultFrame != null) VaultStep(c); else DoorStep(c, "vault"); }
    // 金庫の大扉（本人 2026-09-29「モデルとディテールも作り込んで」）。絵は blender/render_vault.py で作り込んだモデルを撮ったもの（枠・扉 2 状態・ハンドル）
    // 落ちてきて閉まる → 第 1 停止: ハンドルが回る・扉の縁から青い光 → 第 2 停止: 閂が抜ける（絵を切り替え）・蒸気・赤い光・火花
    // → 第 3 停止: ハンドルが高速で回り、蝶番を軸に扉が開く・穴から光が噴く → 壁ごと手前へ抜ける
    static void VaultStep(Ctx c)
    {
        float tClose = 0.3f, s1 = 1.0f, s2 = 1.7f, s3 = 2.45f;
        var blue = new Color(0.35f, 0.7f, 1f); var red = new Color(1f, 0.25f, 0.12f); var gold = new Color(1f, 0.82f, 0.35f);
        const float Hinge = 3.47f, DoorR = 2.54f;                                   // 蝶番の x・扉の半径（Blender 7.2 m 幅 → 12.8）
        Material M(Texture2D t, int q) { var m = DoorMat(t); m.renderQueue = q; return m; }
        var root = new GameObject("Vault").transform; root.SetParent(c.root, false);
        var frameM = M(c.tx.vaultFrame, 2988); var frame = Quad(root, "VaultFrame", new Vector3(0, 0, -3.3f), new Vector2(12.8f, 7.2f), frameM);
        var pivot = new GameObject("VaultPivot").transform; pivot.SetParent(root, false); pivot.localPosition = new Vector3(Hinge, 0, -3.31f);
        var lockedM = M(c.tx.vaultLocked, 2991); var openM = M(c.tx.vaultOpen, 2991);
        var doorL = Quad(pivot, "VaultDoorLocked", Vector3.zero, new Vector2(12.8f, 7.2f), lockedM); doorL.localPosition = new Vector3(-Hinge, 0, 0);
        var doorO = Quad(pivot, "VaultDoorOpen", Vector3.zero, new Vector2(12.8f, 7.2f), openM); doorO.localPosition = new Vector3(-Hinge, 0, 0);
        var wheelM = M(c.tx.vaultWheel, 2992); var wheelT = Quad(pivot, "VaultWheel", Vector3.zero, new Vector2(12.8f, 7.2f), wheelM); wheelT.localPosition = new Vector3(-Hinge, 0, -0.01f);
        var glow = Quad(root, "VaultGlow", new Vector3(0, 0, -3.29f), Vector2.one * DoorR * 2.75f, QMat(c, c.tx.ring, 0, Vector2.one, 0)); var glowM = glow.GetComponent<MeshRenderer>().sharedMaterial; glowM.renderQueue = 2989;   // 扉の後ろ（縁の隙間から見える）
        var rnd = new System.Random(2501); Vector3 jit = Vector3.zero; int lastJ = -1;
        c.OnUpdate(t =>
        {
            // 落ちてきて閉まる
            float drop = t < tClose ? 8f : Mathf.Max(0, 8f * (1 - EaseIn(Mathf.Clamp01((t - tClose) / 0.22f))));
            float bounce = t > tClose + 0.22f ? Mathf.Sin((t - tClose - 0.22f) * 38f) * Mathf.Exp(-(t - tClose - 0.22f) * 11f) * 0.12f : 0;
            float shake = 0;
            foreach (var (ts, amp) in new[] { (s1, 0.05f), (s2, 0.12f) }) { float a = t - ts; if (a >= 0 && a < 0.6f) shake = Mathf.Max(shake, amp * Mathf.Exp(-a * 5f)); }
            if (t >= s2 && t < s3) shake = Mathf.Max(shake, 0.018f);
            int st = Mathf.FloorToInt(t * 30); if (st != lastJ) { lastJ = st; jit = new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, 0) * 2f; }
            // 抜ける（開いた後、壁ごと手前へ大きくなって消える）
            float exit = Mathf.Clamp01((t - s3 - 0.45f) / 0.3f);
            root.localPosition = new Vector3(0, drop + bounce, 0) + jit * shake;
            root.localScale = Vector3.one * (1 + exit * exit * 1.5f);
            var fade = new Color(1, 1, 1, 1 - exit);
            // ハンドル: 第 1 で 120°、第 2 で 120°、第 3 で高速に回る
            float spin = (t >= s1 ? EaseOut(Mathf.Clamp01((t - s1) / 0.35f)) * 120f : 0) + (t >= s2 ? EaseOut(Mathf.Clamp01((t - s2) / 0.35f)) * 120f : 0) + (t >= s3 ? (t - s3) * 1400f : 0);
            wheelT.localRotation = Quaternion.Euler(0, 0, -spin);
            // 閂が抜ける（第 2 停止で絵を切り替え。一瞬扉が白く光る）
            bool open = t >= s2 + 0.06f;
            doorL.gameObject.SetActive(!open); doorO.gameObject.SetActive(open);
            // 扉が蝶番を軸に開く（横に縮めて奥へ回る見え方。暗くなる）
            float sw = t < s3 + 0.08f ? 0 : EaseInOut(Mathf.Clamp01((t - s3 - 0.08f) / 0.32f));
            pivot.localScale = new Vector3(Mathf.Lerp(1f, 0.06f, sw), 1, 1);
            float shade = Mathf.Lerp(1f, 0.35f, sw);
            foreach (var m in new[] { lockedM, openM, wheelM }) { m.SetColor("_Color", new Color(shade, shade, shade, 1 - exit)); m.SetFloat("_Flash", (t >= s2 && t < s2 + 3 / 60f) || (t >= s3 && t < s3 + 2 / 60f) ? 0.8f : 0f); }
            frameM.SetColor("_Color", fade);
            // 扉の縁の光（第 1 青・第 2 赤く脈打つ・第 3 白く噴く）
            Color gc = t < s2 ? blue : t < s3 ? red : Color.Lerp(gold, Color.white, 0.4f);
            float gb = t < s1 ? 0 : t < s2 ? 1.1f * Mathf.Clamp01((t - s1) / 0.1f) : t < s3 ? 1.8f + 0.6f * Mathf.Sin(t * 20f) : 4f * (1 - sw);
            glowM.SetColor("_Tint", gc * gb * (1 - exit));
            // 画面
            if (t > tClose) c.post.stageDim = Mathf.Max(c.post.stageDim, 0.4f * (1 - exit));
            if (t >= tClose + 0.22f && t < tClose + 0.25f) c.post.flash = 0.3f;
            float a1 = t - (tClose + 0.22f); if (a1 >= 0 && a1 < 0.4f) c.post.trauma += 0.7f * (1 - a1 / 0.4f);
            foreach (var ts in new[] { s1, s2 }) { float a = t - ts; if (a >= 0 && a < 0.4f) c.post.trauma += (ts == s2 ? 0.7f : 0.4f) * (1 - a / 0.4f); }
            float b = t - s3; if (b >= 0 && b < 3 / 60f) c.post.flash = 0.9f; if (b >= 0 && b < 0.5f) c.post.trauma += 0.8f * (1 - b / 0.5f);
            if (b >= 0) c.post.tint = new Color(1f, 0.9f, 0.6f, 0.35f * (1 - Mathf.Clamp01(b / 0.8f)));
        });
        for (int k = 0; k < 4; k++) Flip(c, "VaultDust" + k, new Vector3(-4.5f + k * 3f, -3.1f, -3.4f), c.tx.fbSmoke, 8, 8, k * 6, 40 + k * 6, 0.9f, 2.4f, new Color(0.7f, 0.68f, 0.66f, 0.6f), 1f, 2502 + (uint)k, alpha: true, t: tClose + 0.22f);
        // 第 2 停止: 閂の 8 か所から蒸気と火花
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2 / 8 + Mathf.PI / 8;
            var p = new Vector3(Mathf.Cos(a) * DoorR * 1.05f, Mathf.Sin(a) * DoorR * 1.05f, -3.45f);
            Flip(c, "VaultSteam" + k, p + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.3f, c.tx.fbSmoke, 8, 8, k * 5, 40 + k * 5, 0.9f, 1.6f, new Color(0.95f, 0.95f, 1f, 0.55f), 1f, 2510 + (uint)k, delay: 0.02f, alpha: true, t: s2);
            if (k % 2 == 0) RealSparks(c, p, a * Mathf.Rad2Deg, s2 + 0.02f, 14, 6f, 2520 + (uint)k);
        }
        PopLabel(c, "第 1 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, blue, s1, 0.6f);
        PopLabel(c, "第 2 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, red, s2, 0.7f);
        PopLabel(c, "第 3 停止", new Vector3(-4.6f, 3.1f, -3.7f), 0.035f, gold, s3, 0.8f);
        Flare(c, new Vector3(0, 0, -3.6f), s3 + 0.1f, gold, 8f, 2530); Burst(c, new Vector3(0, 0, -3.6f), s3 + 0.1f, gold, 11f, 2531);
        var cols = new[] { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.3f, 1f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(0.8f, 0.4f, 1f) };
        for (int k = 0; k < cols.Length; k++) Ring(c, new Vector3(0, 0, -3.6f), s3 + 0.14f + k * 0.04f, cols[k], 14f - k * 1.6f, 2532 + (uint)k);
        Bokeh(c, new Vector3(0, 0, -3.6f), s3 + 0.1f, gold, 4.5f, 2540);
    }
    static float EaseInOut(float x) => x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
    static void DoorShutter(Ctx c) => DoorStep(c, "shutter");

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
    // 衝撃波の輪（Lab/Ring の 0）: 太さと明るさに角度ごとのムラ・切れ目・外縁は硬く内側は柔らかい・最後は削れて消える
    // 半径は一気に広げて後はゆっくり、太さは細 → 太 → 細（docs/FX_RESEARCH.md 1）
    static void Ring(Ctx c, Vector3 pos, float t, Color col, float size, uint seed, float squash = 1f, float delay = 0f)
    {
        var m = new Material(Shader.Find("Lab/Ring")); m.SetTexture("_NoiseTex", c.tx.noise); m.SetFloat("_Mode", 0); m.SetFloat("_Seed", (seed % 97) * 0.173f);
        m.SetColor("_Col", col * 0.9f); m.SetColor("_Core", Color.Lerp(col, Color.white, 0.6f) * 3f);
        var q = Quad(c.root, "Ring", pos + new Vector3(0, 0, -0.65f), new Vector2(size, size * squash), m);
        const float life = 0.3f;
        c.OnUpdateReal(rt =>
        {
            float a = (rt - c.Real(t) - delay) / life;
            q.gameObject.SetActive(a >= 0 && a <= 1);
            if (a < 0 || a > 1) return;
            m.SetFloat("_R", Mathf.Lerp(0.06f, 0.46f, 1 - Mathf.Pow(1 - a, 3)));
            m.SetFloat("_W", 0.012f + 0.05f * Mathf.Sin(Mathf.PI * Mathf.Min(1, a * 1.6f)));
            m.SetFloat("_Erode", Mathf.Clamp01((a - 0.35f) / 0.65f) * 1.05f);
            m.SetFloat("_Intensity", Mathf.Clamp01(a / 0.08f) * 1.4f);
        });
    }

    // 放射の爆ぜ（Lab/Ring の 1）: 筋ごとに長さ・太さ・明るさが違う。3F で伸び、細って消える（全体 9F）
    static void Burst(Ctx c, Vector3 pos, float t, Color col, float size, uint seed)
    {
        var m = new Material(Shader.Find("Lab/Ring")); m.SetFloat("_Mode", 1); m.SetFloat("_Seed", (seed % 97) * 0.173f); m.SetFloat("_Count", 24);
        m.SetColor("_Col", col); m.SetColor("_Core", Color.Lerp(col, Color.white, 0.7f) * 3.2f);
        var q = Quad(c.root, "Burst", pos + new Vector3(0, 0, -0.68f), new Vector2(size, size), m);
        q.rotation = Quaternion.Euler(0, 0, (seed * 37) % 360);
        const float life = 0.15f;
        c.OnUpdateReal(rt =>
        {
            float a = (rt - c.Real(t)) / life;
            q.gameObject.SetActive(a >= 0 && a <= 1);
            if (a < 0 || a > 1) return;
            m.SetFloat("_Grow", Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(a / 0.3f)));
            m.SetFloat("_Erode", Mathf.Clamp01((a - 0.3f) / 0.7f));
            m.SetFloat("_Intensity", 1.3f);
        });
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
        public Color tint = new Color(1, 1, 1, 0), slitTint = new Color(1, 1, 1, 0); public float glitch, bars;
        public float stageDim, stageDesat, trauma; public Vector2 kick; public Vector3 goblinOff;
        public float heat; public Color fireLight;   // 炎の枠: 陽炎の強さ、照り返し（a = 届く距離）
        public Vector4 shock;   // 空間の歪み: xy 中心（uv）、z 半径（画面の高さ比）、w 強さ
        public Vector4 split; public Color splitCol; public float black; public Vector4 slit;   // 真っ二つ / 暗転 / 暗転中の光の裂け目
    }
    class Tx { public Texture2D dot, glow, ring, star4, diamond, plus, flame, flame2, streak, trail, noise, column, coinFace, burst, air, sparkle, magic, fbHitLines, fbBigHit, fbCharge, fbElecRing, fbFireRing, fbFlame, fbSmoke, fibers, square, fireFlame03, hexTile, arrowUp, fbStarExp, fbVortex, fbWavy, blCampfire, blWall, lightRing, bolt, ringDouble, twirl, starCross, smokePuff, sfxDon, sfxZuba, sfxBari, sfxGo, sfxKira, shieldCrest, doorGate, doorVault, doorLock, shutter, doorGateR, doorVaultR, doorGold, doorGoldR, vaultFrame, vaultLocked, vaultOpen, vaultWheel; }
    class Ctx
    {
        public Transform root, heroT, goblinT; public Texture2D hero, goblin; public Tx tx;
        public Post post = new Post();
        public List<Sys> systems = new List<Sys>();
        public List<Action<float>> updates = new List<Action<float>>();
        public Dictionary<float, Transform> planes = new Dictionary<float, Transform>();
        public Material goblinMat, heroGlow;
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
        if (clip.part) PartsStage(c, clip);
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
            bloom.SetFloat("_Heat", p.heat); bloom.SetColor("_FireLight", p.fireLight); bloom.SetVector("_Shock", p.shock); bloom.SetVector("_Split", p.split); bloom.SetColor("_SplitCol", p.splitCol); bloom.SetFloat("_Black", p.black); bloom.SetVector("_Slit", p.slit);
            bloom.SetVector("_Shake", new Vector4(p.shake.x, p.shake.y, p.rot, 0));
            bloom.SetFloat("_Zoom", p.zoom); bloom.SetVector("_ZoomCenter", new Vector4(p.zoomCenter.x, p.zoomCenter.y, 1, 0));
            bloom.SetVector("_BlurDir", p.blurDir); bloom.SetFloat("_BlurAmt", p.blur);
            bloom.SetFloat("_Lines", p.lines); bloom.SetFloat("_LinesMode", p.linesMode); bloom.SetFloat("_LinesSeed", p.linesSeed);
            bloom.SetFloat("_LinesAngle", 0); bloom.SetFloat("_LinesDensity", p.linesDensity);
            bloom.SetVector("_LinesCenter", p.linesCenter); bloom.SetColor("_LinesColor", Color.white);
            bloom.SetFloat("_Darken", p.darken); bloom.SetFloat("_Invert", p.invert); bloom.SetFloat("_Mono", p.mono); bloom.SetFloat("_Flash", p.flash);
            bloom.SetColor("_Tint", p.tint); bloom.SetColor("_SlitTint", p.slitTint); bloom.SetFloat("_Glitch", p.glitch); bloom.SetFloat("_Bars", p.bars);
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
        // （2026-09-29）行末コメントの後ろに素材の読み込みを足して、読み込みが丸ごとコメントになった事故が 2 回あった。1 行 1 つにする
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
        tx.noise = TileNoise(512);   // 128 だと拡大したとき輪郭が折れ線になってジャギが出た（本人 2026-09-29「ジャギが目立つ」）
        tx.flame = Tex(96, 96, (u, v) =>
        {
            float x = u * 2 - 1, y = v * 2 - 1, r = Mathf.Sqrt(x * x + y * y);
            float f = Fbm(u * 3f + 4.1f, v * 3f + 1.7f);
            return Mathf.Clamp01(Smooth(1f, 0.2f, r) * (f * 2.0f - 0.35f));
        });
        tx.coinFace = CoinFace(256);
        tx.square = Tex(8, 8, (u, v) => 1f);   // 絵の欠片（画素の粒）
        // 六角のタイル（外枠は明るく、中はうっすら）
        tx.hexTile = Tex(128, 128, (u, v) =>
        {
            float x = Mathf.Abs(u * 2 - 1), y = Mathf.Abs(v * 2 - 1);
            float d = Mathf.Max(x * 0.866f + y * 0.5f, y);                     // 六角の距離
            return d > 0.95f ? 0f : Mathf.Max(Smooth(0.78f, 0.9f, d) * (1 - Smooth(0.9f, 0.95f, d)), 0.22f + 0.2f * (1 - v));
        });
        // 上向きの矢印（＾ を 2 つ）
        tx.arrowUp = Tex(64, 96, (u, v) =>
        {
            float x = Mathf.Abs(u * 2 - 1);
            float a1 = Smooth(0.14f, 0.08f, Mathf.Abs(v - (0.72f - x * 0.45f)));
            float a2 = Smooth(0.14f, 0.08f, Mathf.Abs(v - (0.42f - x * 0.45f))) * 0.7f;
            return Mathf.Max(a1, a2) * Smooth(1f, 0.85f, x);
        });
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
            tx.fbElecRing = K("fb_elecring_6x5"); tx.fireFlame03 = K("fire_flame03_16x4");
            tx.fbStarExp = K("fb_starexp_7x6");
            // Blender の流体シミュレーションで作った炎（tools/fx-lab/blender）。無ければ既製の連番のまま
            if (File.Exists(Path.Combine(dir, "bl_campfire_8x8.png"))) { tx.blCampfire = K("bl_campfire_8x8"); tx.blCampfire.name = "bl_campfire"; }
            if (File.Exists(Path.Combine(dir, "bl_wall_8x8.png"))) { tx.blWall = K("bl_wall_8x8"); tx.blWall.name = "bl_wall"; }
            tx.fbVortex = K("fb_vortex_6x5"); tx.fbWavy = K("fb_wavy_6x5");   // 星の爆発は配布名 6x5 だが実際は 7×6（上の行）
            tx.lightRing = K("light_ring"); tx.bolt = K("bolt"); tx.ringDouble = K("ring_double"); tx.twirl = K("twirl"); tx.starCross = K("star_cross"); tx.smokePuff = K("smoke_puff");
            tx.sfxDon = K("sfx_don"); tx.sfxZuba = K("sfx_zuba"); tx.sfxBari = K("sfx_bari"); tx.sfxGo = K("sfx_go"); tx.sfxKira = K("sfx_kira"); tx.shieldCrest = K("shield_crest");   // 擬音（make_sfx_tex.py）・盾の紋章（make_shield_tex.py）
            tx.doorGate = K("door_gate_L"); tx.doorGateR = K("door_gate_R"); tx.doorVault = K("door_vault_L"); tx.doorVaultR = K("door_vault_R"); tx.doorLock = K("door_vault_lock"); tx.shutter = K("shutter");
            tx.doorGold = K("door_gold_L");
            tx.doorGoldR = K("door_gold_R");
            // 扉は落とした CC0 の 3D 素材を Blender で撮ったもの（blender/render_doors.py、LICENSE-doors.txt）
            if (File.Exists(Path.Combine(dir, "vault_frame.png"))) { tx.vaultFrame = K("vault_frame"); tx.vaultLocked = K("vault_door_locked"); tx.vaultOpen = K("vault_door_open"); tx.vaultWheel = K("vault_wheel"); }   // 作り込んだ金庫（blender/render_vault.py）
            tx.fbFireRing = K("fb_firering_6x5"); tx.fbFlame = K("fb_flame_16x4"); tx.fbSmoke = K("fb_smoke_8x8");
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
