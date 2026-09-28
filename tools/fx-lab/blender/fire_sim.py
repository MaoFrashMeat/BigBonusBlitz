# -*- coding: utf-8 -*-
"""Blender（5.2、背景実行）で炎を流体シミュレーションし、正面から 1 コマずつ描き出す。
使い方: blender -b -P fire_sim.py -- <出力フォルダ> [解像度=128] [コマ数=64] [種類=campfire]
出力: <出力フォルダ>/f_0001.png …（黒背景・白黒。明るさ = 炎の濃さ。色はゲーム側の Lab/Fire が温度の段で塗る）。連番化は make_flipbook.py
種類: campfire（焚き火。下から立つ炎）/ wall（横に長い炎の帯。枠用）/ burst（一瞬で燃え上がる火球）"""
import bpy, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else os.path.join(os.path.dirname(__file__), "out")
RES = int(argv[1]) if len(argv) > 1 else 128
FRAMES = int(argv[2]) if len(argv) > 2 else 64
KIND = argv[3] if len(argv) > 3 else "campfire"
WARM = 40            # 燃え始めの捨てコマ（炎が育つまで）
FLAME_GAIN = float(argv[4]) if len(argv) > 4 else 2.5   # 炎の濃さ → 明るさ。白く飛ばない所まで
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.frame_start = 1; sc.frame_end = WARM + FRAMES

# ---- 領域（ドメイン）
dom_size = (1.8, 1.0, 3.2) if KIND != "wall" else (3.2, 1.0, 3.2)   # 壁は 3.2 幅で作り、ゲーム側で並べる（天井に当たると四角くなるので高さも 3.2）
bpy.ops.mesh.primitive_cube_add(location=(0, 0, dom_size[2] / 2))
dom = bpy.context.object; dom.name = "Domain"; dom.scale = (dom_size[0] / 2, dom_size[1] / 2, dom_size[2] / 2)
bpy.ops.object.transform_apply(scale=True)
m = dom.modifiers.new("Fluid", "FLUID"); m.fluid_type = "DOMAIN"
ds = m.domain_settings
ds.domain_type = "GAS"; ds.resolution_max = RES
ds.use_adaptive_domain = False
ds.cache_type = "ALL"; ds.cache_directory = os.path.join(OUT, "_cache")
ds.cache_frame_start = 1; ds.cache_frame_end = WARM + FRAMES
ds.vorticity = 0.9                        # 渦（炎の揺らぎ。舌の形）
ds.use_noise = True; ds.noise_scale = 3; ds.noise_strength = 1.6   # 細かい揺らぎ（高解像度ノイズ。見た目の解像度 = RES × 3）
ds.burning_rate = 0.4 if KIND != "wall" else 0.5; ds.flame_smoke = 0.6; ds.flame_vorticity = 2.4   # 燃えるのを遅く = 炎が長く伸びる
ds.flame_ignition = 1.3; ds.flame_max_temp = 2.6
ds.alpha = 1.0; ds.beta = 4.2             # 熱で上へ（beta）
ds.use_dissolve_smoke = True; ds.dissolve_speed = 12
ds.time_scale = 1.0

# ---- 燃える元（フロー）
if KIND == "wall":
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, 0.12)); src = bpy.context.object; src.scale = (1.62, 0.14, 0.05)   # 幅いっぱい（左右の縁が途切れないように）
else:
    # 小さな玉を 5 つ横に並べて 1 つにする（1 つの大きな元だと炎が柱になる。分けると舌が分かれる）
    import random; rnd = random.Random(3)
    parts = []
    for k in range(5):
        x = (k - 2) * 0.16 + rnd.uniform(-0.04, 0.04)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.07 + rnd.uniform(0, 0.04), location=(x, rnd.uniform(-0.08, 0.08), 0.2 + rnd.uniform(0, 0.08)))
        parts.append(bpy.context.object)
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]; bpy.ops.object.join(); src = bpy.context.object
bpy.ops.object.transform_apply(scale=True)
fm = src.modifiers.new("Fluid", "FLUID"); fm.fluid_type = "FLOW"
fs = fm.flow_settings
fs.flow_type = "FIRE"; fs.flow_behavior = "INFLOW" if KIND != "burst" else "INFLOW"
fs.flow_source = "MESH"; fs.surface_distance = 0.4
fs.fuel_amount = 1.3 if KIND != "wall" else 1.0; fs.temperature = 1.4   # 壁は面積が広いので燃料を減らす
fs.use_initial_velocity = True; fs.velocity_normal = 1.1
# 燃料の出方を揺らす（ノイズのテクスチャ）: 炎の根元が一様にならないように
tex = bpy.data.textures.new("FuelNoise", "CLOUDS"); tex.noise_scale = 0.12; tex.noise_depth = 3
fs.use_texture = True; fs.noise_texture = tex; fs.texture_size = 1.0; fs.texture_offset = 0.0
# 燃料のムラを時間で流す（根元の炎が同じ所に固まらない）
fs.keyframe_insert("texture_offset", frame=1); fs.texture_offset = 6.0; fs.keyframe_insert("texture_offset", frame=WARM + FRAMES)
for fc in (src.animation_data.action.fcurves if src.animation_data and src.animation_data.action and hasattr(src.animation_data.action, 'fcurves') else []):
    for k in fc.keyframe_points: k.interpolation = 'LINEAR'
src.hide_render = True
if KIND == "burst":
    # 火球: 12 コマだけ燃料を出して止める
    fs.use_plane_init = False
    src.modifiers["Fluid"].flow_settings.flow_behavior = "INFLOW"
    fs.keyframe_insert("fuel_amount", frame=WARM - 2); fs.fuel_amount = 0.0; fs.keyframe_insert("fuel_amount", frame=WARM + 10)
    fs.fuel_amount = 3.0; fs.keyframe_insert("fuel_amount", frame=WARM - 1)

# ---- 乱流の力場（炎の舌を揺らす）
bpy.ops.object.effector_add(type="TURBULENCE", location=(0, 0, dom_size[2] * 0.5))
turb = bpy.context.object; turb.field.strength = 2.6; turb.field.size = 0.3; turb.field.flow = 0.0
turb.field.noise = 0.0

# ---- 炎の材質: 白黒で「炎の濃さ」だけを出す（色はゲーム側で温度の段に塗る。Lab/Fire）
mat = bpy.data.materials.new("FireMat")
nt = mat.node_tree if mat.node_tree else None
if nt is None:
    mat.use_nodes = True; nt = mat.node_tree
nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputMaterial")
em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (1, 1, 1, 1)
attr = nt.nodes.new("ShaderNodeAttribute"); attr.attribute_name = "flame"
pw = nt.nodes.new("ShaderNodeMath"); pw.operation = "POWER"; pw.inputs[1].default_value = 1.8   # 濃さを締める（輪郭をはっきり）
mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = FLAME_GAIN
nt.links.new(attr.outputs["Fac"], pw.inputs[0]); nt.links.new(pw.outputs[0], mul.inputs[0]); nt.links.new(mul.outputs[0], em.inputs["Strength"])
nt.links.new(em.outputs["Emission"], out.inputs["Volume"])
dom.data.materials.append(mat)

# ---- カメラ（正面・正射影）と描き出し
cam_data = bpy.data.cameras.new("Cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = max(dom_size[0], dom_size[2]) * 1.0
cam = bpy.data.objects.new("Cam", cam_data); sc.collection.objects.link(cam)
cam.location = (0, -8, dom_size[2] / 2); cam.rotation_euler = (math.radians(90), 0, 0)
sc.camera = cam
sc.world = bpy.data.worlds.new("W"); sc.world.color = (0, 0, 0)
try:
    bg = sc.world.node_tree.nodes.get("Background")
    if bg: bg.inputs["Strength"].default_value = 0.0
except Exception: pass
# EEVEE: 体積がざらつかない（Cycles 24 サンプルは粒が目立った）
sc.render.engine = "BLENDER_EEVEE"
try:
    ee = sc.eevee; ee.volumetric_tile_size = "1"; ee.volumetric_samples = 128; ee.volumetric_start = 0.1; ee.volumetric_end = 20; ee.taa_render_samples = 16
except Exception as e: print("eevee settings", e)
sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"
W = 512   # 細部を残して描き、連番にするとき縮める
sc.render.resolution_x = W; sc.render.resolution_y = int(W * dom_size[2] / dom_size[0]) if KIND == "wall" else W
if KIND != "wall": cam_data.ortho_scale = dom_size[2]
sc.render.film_transparent = False
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "BW"; sc.render.image_settings.color_depth = "16"

# ---- 計算（ベイク）→ 描き出し
bpy.context.view_layer.objects.active = dom
with bpy.context.temp_override(active_object=dom, object=dom):
    bpy.ops.fluid.bake_all()
for f in range(WARM + 1, WARM + FRAMES + 1):
    sc.frame_set(f)
    sc.render.filepath = os.path.join(OUT, f"f_{f - WARM:04d}.png")
    bpy.ops.render.render(write_still=True)
print("fire_sim done", OUT)
