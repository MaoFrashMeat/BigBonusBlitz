# -*- coding: utf-8 -*-
"""継ぎ目のないプロシージャル・テクスチャを Blender のノードで焼く（本人 2026-09-30「プロシージャルなテクスチャをしっかり選択して」→ 案 1）
使い方: blender -b -P bake_textures.py -- <出力フォルダ> [解像度=2048]
継ぎ目なしの作り: 4D ノイズに (cos u, sin u, cos v, sin v) を入れる（u, v は 0..1 の UV）。円周を一周すると元に戻るので、どの方向にも繋がる
出力（RGB に 3 種ずつ詰める。sRGB の 8bit PNG。Unity 側は sRGB として読む）:
  proc_noise.png   R = FBM 粗 / G = FBM 中 / B = FBM 細                     … 汎用の揺らぎ
  proc_ridge.png   R = 稜線 粗 / G = 稜線 細 / B = ハイブリッド多重フラクタル … 稲妻・エネルギーの筋・岩
  proc_voro.png    R = セル F1 / G = 縁（F2−F1）/ B = 滑らかなセル             … プラズマ・ひび・水面の光
  proc_streak.png  R = 横に長い FBM 粗 / G = 細 / B = 横に長い稜線             … 光の筋・速度線・光芒（極座標で読むと放射になる）
  proc_warp.png    R = 歪ませた FBM / G = 二重に歪ませた / B = 不均質地形        … 炎・煙・オーラ
  proc_sparks.png  R = 細かい点 / G = 中の点 / B = 大きく柔らかい点             … 火花・星・塵"""
import bpy, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]; os.makedirs(OUT, exist_ok=True)
RES = int(argv[1]) if len(argv) > 1 else 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if hasattr(bpy.types, "SceneEEVEE") and "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.render.resolution_x = sc.render.resolution_y = RES; sc.render.resolution_percentage = 100
sc.render.film_transparent = False
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGB"; sc.render.image_settings.color_depth = "8"
sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"; sc.view_settings.exposure = 0; sc.view_settings.gamma = 1
sc.display_settings.display_device = "sRGB"
try: sc.eevee.taa_render_samples = 4
except Exception: pass
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs["Color"].default_value = (0, 0, 0, 1)

# 板とカメラ（正面・平行投影・板がちょうど画面）
bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0)); plane = bpy.context.object
cd = bpy.data.cameras.new("Cam"); cd.type = "ORTHO"; cd.ortho_scale = 1
cam = bpy.data.objects.new("Cam", cd); sc.collection.objects.link(cam); cam.location = (0, 0, 5); sc.camera = cam

class NB:
    """ノードを関数で組む小さな道具"""
    def __init__(self, nt): self.nt = nt; self.n = nt.nodes; self.l = nt.links
    def node(self, kind, **kw):
        o = self.n.new(kind)
        for k, v in kw.items(): setattr(o, k, v)
        return o
    def math(self, op, a, b=None, c=None):
        m = self.node("ShaderNodeMath", operation=op)
        for i, x in enumerate((a, b, c)):
            if x is None: continue
            if isinstance(x, (int, float)): m.inputs[i].default_value = x
            else: self.l.new(x, m.inputs[i])
        return m.outputs[0]
    def uv(self):
        tc = self.node("ShaderNodeTexCoord"); sep = self.node("ShaderNodeSeparateXYZ"); self.l.new(tc.outputs["UV"], sep.inputs[0])
        return sep.outputs[0], sep.outputs[1]
    def circle(self, u, R, phase=0.0):
        """u(0..1) → (cos, sin) * R / 2π。R = 1 タイルに入る周期の数"""
        R = R / math.tau
        a = self.math("MULTIPLY", u, math.tau)
        if phase: a = self.math("ADD", a, phase)
        return self.math("MULTIPLY", self.math("COSINE", a), R), self.math("MULTIPLY", self.math("SINE", a), R)
    def tile4d(self, u, v, Ru, Rv, phase=0.0):
        """継ぎ目なしの 4D 座標: Vector(x, y, z) と W"""
        x, y = self.circle(u, Ru, phase); z, wv = self.circle(v, Rv, phase * 1.7)
        comb = self.node("ShaderNodeCombineXYZ"); self.l.new(x, comb.inputs[0]); self.l.new(y, comb.inputs[1]); self.l.new(z, comb.inputs[2])
        return comb.outputs[0], wv
    def noise(self, vec, wv, kind="FBM", detail=8.0, rough=0.55, lac=2.0, distortion=0.0, offset=0.0, gain=1.0):
        n = self.node("ShaderNodeTexNoise"); n.noise_dimensions = "4D"
        try: n.noise_type = kind
        except Exception: pass
        n.normalize = True if hasattr(n, "normalize") else None
        n.inputs["Scale"].default_value = 1.0; n.inputs["Detail"].default_value = detail; n.inputs["Roughness"].default_value = rough
        try: n.inputs["Lacunarity"].default_value = lac
        except Exception: pass
        n.inputs["Distortion"].default_value = distortion
        for nm, val in (("Offset", offset), ("Gain", gain)):
            if nm in n.inputs: n.inputs[nm].default_value = val
        self.l.new(vec, n.inputs["Vector"]); self.l.new(wv, n.inputs["W"])
        return n.outputs["Fac"]
    def voronoi(self, vec, wv, feature="F1", smooth=0.0, rand=1.0):
        n = self.node("ShaderNodeTexVoronoi"); n.voronoi_dimensions = "4D"; n.feature = feature
        try: n.distance = "EUCLIDEAN"
        except Exception: pass
        n.inputs["Scale"].default_value = 1.0; n.inputs["Randomness"].default_value = rand
        if "Smoothness" in n.inputs: n.inputs["Smoothness"].default_value = smooth
        self.l.new(vec, n.inputs["Vector"]); self.l.new(wv, n.inputs["W"])
        return n
    def warp(self, vec, fac, amp):
        """座標をノイズで押す（domain warp）: vec + fac * amp"""
        vm = self.node("ShaderNodeVectorMath", operation="SCALE"); self.l.new(vec, vm.inputs[0])
        # fac をベクトルに: (fac, fac*0.7, fac*0.4) 方向がずれるように
        c = self.node("ShaderNodeCombineXYZ")
        self.l.new(self.math("MULTIPLY", fac, amp), c.inputs[0]); self.l.new(self.math("MULTIPLY", fac, amp * 0.7), c.inputs[1]); self.l.new(self.math("MULTIPLY", fac, amp * 0.4), c.inputs[2])
        add = self.node("ShaderNodeVectorMath", operation="ADD"); self.l.new(vec, add.inputs[0]); self.l.new(c.outputs[0], add.inputs[1])
        return add.outputs[0]
    def remap(self, x, lo, hi):
        mr = self.node("ShaderNodeMapRange"); mr.inputs["From Min"].default_value = lo; mr.inputs["From Max"].default_value = hi; mr.clamp = True
        self.l.new(x, mr.inputs["Value"]); return mr.outputs["Result"]

def bake(name, build):
    mat = bpy.data.materials.new(name); mat.use_nodes = True; nt = mat.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    b = NB(nt); r, g, bl = build(b)
    comb = b.node("ShaderNodeCombineColor"); b.l.new(r, comb.inputs[0]); b.l.new(g, comb.inputs[1]); b.l.new(bl, comb.inputs[2])
    em = b.node("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.0; b.l.new(comb.outputs[0], em.inputs["Color"])
    out = b.node("ShaderNodeOutputMaterial"); b.l.new(em.outputs[0], out.inputs[0])
    plane.data.materials.clear(); plane.data.materials.append(mat)
    sc.render.filepath = os.path.join(OUT, name + ".png"); bpy.ops.render.render(write_still=True); print("baked", name)

def t_noise(b):
    u, v = b.uv()
    out = []
    for R, ph in ((3, 0), (8, 1.3), (20, 2.9)):
        vec, wv = b.tile4d(u, v, R, R, ph); out.append(b.noise(vec, wv, "FBM", 6, 0.55))
    return out
def t_ridge(b):
    u, v = b.uv()
    v1, w1 = b.tile4d(u, v, 4, 4, 0.4); v2, w2 = b.tile4d(u, v, 12, 12, 1.1); v3, w3 = b.tile4d(u, v, 7, 7, 2.2)
    return (b.remap(b.noise(v1, w1, "RIDGED_MULTIFRACTAL", 6, 0.6, 2.0, 0, 0.9, 2.0), 0.0, 2.2),
            b.remap(b.noise(v2, w2, "RIDGED_MULTIFRACTAL", 6, 0.6, 2.0, 0, 0.9, 2.0), 0.0, 2.2),
            b.remap(b.noise(v3, w3, "HYBRID_MULTIFRACTAL", 6, 0.55, 2.0, 0, 0.6, 1.5), 0.0, 2.0))
def t_voro(b):
    u, v = b.uv()
    vec, wv = b.tile4d(u, v, 8, 8, 0.7)
    f1 = b.voronoi(vec, wv, "F1"); f2 = b.voronoi(vec, wv, "F2"); sm = b.voronoi(vec, wv, "SMOOTH_F1", 0.6)
    edge = b.math("SUBTRACT", f2.outputs["Distance"], f1.outputs["Distance"])
    return (b.remap(f1.outputs["Distance"], 0.0, 0.8), b.remap(edge, 0.0, 0.5), b.remap(sm.outputs["Distance"], 0.0, 0.8))
def t_streak(b):
    u, v = b.uv()
    v1, w1 = b.tile4d(u, v, 1.5, 24, 0.3); v2, w2 = b.tile4d(u, v, 3, 60, 1.7); v3, w3 = b.tile4d(u, v, 2, 40, 2.6)
    return (b.noise(v1, w1, "FBM", 6, 0.5), b.noise(v2, w2, "FBM", 6, 0.5), b.remap(b.noise(v3, w3, "RIDGED_MULTIFRACTAL", 6, 0.6, 2.0, 0, 0.9, 2.0), 0.0, 2.2))
def t_warp(b):
    u, v = b.uv()
    v1, w1 = b.tile4d(u, v, 4, 4, 0.2)
    r = b.noise(v1, w1, "FBM", 6, 0.55, 2.0, 2.0)
    v2, w2 = b.tile4d(u, v, 3, 3, 1.4); f = b.noise(v2, w2, "FBM", 4, 0.5); vw = b.warp(v2, f, 1.2); f2 = b.noise(vw, w2, "FBM", 4, 0.5); vw2 = b.warp(vw, f2, 0.8)
    g = b.noise(vw2, w2, "FBM", 6, 0.55)
    v3, w3 = b.tile4d(u, v, 6, 6, 2.8)
    bl = b.remap(b.noise(v3, w3, "HETERO_TERRAIN", 6, 0.55, 2.0, 0, 0.4, 1.0), 0.0, 2.0)
    return r, g, bl
def t_sparks(b):
    u, v = b.uv()
    out = []
    for R, k, pw in ((40, 5.0, 5.0), (16, 3.5, 3.0), (6, 2.2, 1.6)):
        vec, wv = b.tile4d(u, v, R, R, R * 0.1)
        vo = b.voronoi(vec, wv, "F1", 0.0, 1.0)
        # 点: 1 − 距離×k を pow で尖らせる。明るさはセルの色でばらす
        d = b.math("SUBTRACT", 1.0, b.math("MULTIPLY", vo.outputs["Distance"], k))
        d = b.math("MAXIMUM", d, 0.0); d = b.math("POWER", d, pw)
        sepc = b.node("ShaderNodeSeparateColor"); b.l.new(vo.outputs["Color"], sepc.inputs[0])
        br = b.math("ADD", 0.25, b.math("MULTIPLY", b.math("POWER", sepc.outputs[0], 2.0), 0.75))
        out.append(b.math("MULTIPLY", d, br))
    return out

for name, fn in (("proc_noise", t_noise), ("proc_ridge", t_ridge), ("proc_voro", t_voro), ("proc_streak", t_streak), ("proc_warp", t_warp), ("proc_sparks", t_sparks)):
    bake(name, fn)
print("bake done")
