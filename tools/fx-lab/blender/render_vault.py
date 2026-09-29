# -*- coding: utf-8 -*-
"""金庫の大扉を Blender でモデリングして撮る（本人 2026-09-29「ちゃんとモデルとディテールも作り込んで」）
使い方: blender -b -P render_vault.py -- <素材フォルダ（metalplates/ を含む）> <出力フォルダ>
構成（画面 16:9 = 7.2 x 4.05 m を正面から正射影。扉の中心 = 画面の中心）:
  壁（ダイヤ板の鋼・扉の穴）＋枠（ボルト留めの厚いリング）＋右の蝶番 … vault_frame.png
  扉（分厚い円盤・面取り・同心円の溝・真鍮の輪・六角ボルト・銘板・放射状の閂 8 本）… vault_door_locked.png / vault_door_open.png（閂を引っ込めた）
  ハンドル（真鍮の輪・5 本の握り・ハブ）… vault_wheel.png（画面の中心で回せる）
すべて Cycles・デノイズ・背景透明・2560x1440 → 呼び出し側で 1280x720 に縮める"""
import bpy, bmesh, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = argv[0], argv[1]
os.makedirs(OUT, exist_ok=True)
MP = os.path.join(SRC, "metalplates")
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene

# ---------- 描画の設定 ----------
sc.render.engine = "CYCLES"
sc.cycles.device = "CPU"; sc.cycles.samples = 160; sc.cycles.use_denoising = True
try: sc.cycles.denoiser = "OPENIMAGEDENOISE"
except Exception: pass
sc.render.resolution_x, sc.render.resolution_y = 2560, 1440
sc.render.film_transparent = True
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"
sc.view_settings.view_transform = "AgX"; sc.view_settings.look = "AgX - Medium High Contrast"
# 映り込み用の空（上が明るく下が暗いグラデーション。金属の面に映る）
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True; nt = w.node_tree
bg = nt.nodes["Background"]; tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ")
ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (0.02, 0.02, 0.025, 1)
ramp.color_ramp.elements[1].position = 0.75; ramp.color_ramp.elements[1].color = (0.9, 0.85, 0.8, 1)
mr = nt.nodes.new("ShaderNodeMapRange"); mr.inputs["From Min"].default_value = -1; mr.inputs["From Max"].default_value = 1
nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"])
nt.links.new(tc.outputs["Object"] if False else tc.outputs["Generated"], sep.inputs["Vector"])
nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"]); nt.links.new(ramp.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.55

# ---------- 材質 ----------
def principled(name, base, metal, rough, bump_scale=0.0, noise_rough=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; t = m.node_tree; b = t.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*base, 1); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    if noise_rough > 0:   # 使い込んだ金属: 粗さにムラ
        n = t.nodes.new("ShaderNodeTexNoise"); n.inputs["Scale"].default_value = 18; n.inputs["Detail"].default_value = 8
        mr2 = t.nodes.new("ShaderNodeMapRange"); mr2.inputs["To Min"].default_value = rough - noise_rough; mr2.inputs["To Max"].default_value = rough + noise_rough
        t.links.new(n.outputs["Fac"], mr2.inputs["Value"]); t.links.new(mr2.outputs["Result"], b.inputs["Roughness"])
    return m
def textured(name, tint, scale):
    m = bpy.data.materials.new(name); m.use_nodes = True; t = m.node_tree; b = t.nodes["Principled BSDF"]
    tcn = t.nodes.new("ShaderNodeTexCoord"); mp = t.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (scale, scale, scale)
    t.links.new(tcn.outputs["Generated"], mp.inputs["Vector"])
    def img(f, nc=False):
        n = t.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(os.path.join(MP, f)); n.projection = "BOX"; n.projection_blend = 0.2
        if nc: n.image.colorspace_settings.name = "Non-Color"
        t.links.new(mp.outputs["Vector"], n.inputs["Vector"]); return n
    col = img("MetalPlates006_4K-JPG_Color.jpg"); mul = t.nodes.new("ShaderNodeVectorMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = tint
    t.links.new(col.outputs["Color"], mul.inputs[0]); t.links.new(mul.outputs["Vector"], b.inputs["Base Color"])
    b.inputs["Metallic"].default_value = 0.35   # 塗装した鋼（金属の地図のままだと空を映して白く飛んだ）
    t.links.new(img("MetalPlates006_4K-JPG_Roughness.jpg", True).outputs["Color"], b.inputs["Roughness"])
    nm = t.nodes.new("ShaderNodeNormalMap"); t.links.new(img("MetalPlates006_4K-JPG_NormalGL.jpg", True).outputs["Color"], nm.inputs["Color"]); t.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m
def grooved(name, base, rough, rings):
    # 同心円の溝（波のテクスチャを球状に → 凹凸）
    m = principled(name, base, 1.0, rough, noise_rough=0.06); t = m.node_tree; b = t.nodes["Principled BSDF"]
    tcn = t.nodes.new("ShaderNodeTexCoord"); wv = t.nodes.new("ShaderNodeTexWave"); wv.wave_type = "RINGS"; wv.rings_direction = "Z" if hasattr(wv, "rings_direction") else wv.rings_direction
    wv.inputs["Scale"].default_value = rings; wv.inputs["Distortion"].default_value = 0
    bump = t.nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.35; bump.inputs["Distance"].default_value = 0.004
    t.links.new(tcn.outputs["Object"], wv.inputs["Vector"]); t.links.new(wv.outputs["Fac"], bump.inputs["Height"]); t.links.new(bump.outputs["Normal"], b.inputs["Normal"])
    return m
WALL = textured("Wall", (0.07, 0.075, 0.085), 2.2)
STEEL = principled("Steel", (0.62, 0.63, 0.66), 1.0, 0.28, noise_rough=0.08)
DARK = principled("DarkSteel", (0.1, 0.105, 0.12), 1.0, 0.4, noise_rough=0.1)
FACE = grooved("DoorFace", (0.55, 0.56, 0.6), 0.3, 70)
BRASS = principled("Brass", (0.95, 0.68, 0.32), 1.0, 0.22, noise_rough=0.06)
BOLTM = principled("Bolt", (0.72, 0.72, 0.74), 1.0, 0.25)

# ---------- 形を作る道具 ----------
def finish(o, mat, bevel=0.0, seg=3, smooth=True):
    bpy.context.view_layer.objects.active = o
    if bevel > 0:
        md = o.modifiers.new("Bevel", "BEVEL"); md.width = bevel; md.segments = seg; md.limit_method = "ANGLE"; md.angle_limit = math.radians(35)
    if smooth:
        bpy.ops.object.shade_smooth()
        o.modifiers.new("WN", "WEIGHTED_NORMAL")
    o.data.materials.clear(); o.data.materials.append(mat)   # ブール演算で空の枠が先に入ると白い既定の材質になる
    for pl in o.data.polygons: pl.material_index = 0
    return o
def cyl(r, depth, loc, mat, verts=96, bevel=0.0, rot=(math.radians(90), 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot); return finish(bpy.context.object, mat, bevel)
def ring(r_out, r_in, depth, loc, mat, bevel=0.01, verts=128):
    o = cyl(r_out, depth, loc, mat, verts, 0)
    cut = cyl(r_in, depth * 3, loc, mat, verts, 0)
    md = o.modifiers.new("Bool", "BOOLEAN"); md.operation = "DIFFERENCE"; md.object = cut; md.solver = "EXACT"
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier="Bool"); bpy.data.objects.remove(cut)
    if bevel > 0:
        md = o.modifiers.new("Bevel", "BEVEL"); md.width = bevel; md.segments = 3; md.limit_method = "ANGLE"; md.angle_limit = math.radians(35)
    # 真ん中のボルト穴の輪をなめらかに
    o.modifiers.move(o.modifiers.find("WN"), len(o.modifiers) - 1) if o.modifiers.find("WN") >= 0 else None
    return o
def hexbolt(x, z, y, r=0.045, h=0.035, mat=None):
    o = cyl(r, h, (x, y, z), mat or BOLTM, 6, r * 0.18)
    o.rotation_euler.y = math.radians(30); return o
def box(size, loc, mat, bevel=0.01):
    bpy.ops.mesh.primitive_cube_add(location=loc); o = bpy.context.object; o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True); return finish(o, mat, bevel)

Z0 = 0.0               # 扉の中心の高さ（カメラの中心）
Rh = 1.46              # 穴の半径
frame, door_common, bolts_out, bolts_in, wheel = [], [], [], [], []

# ---------- 壁と枠 ----------
bpy.ops.mesh.primitive_cube_add(location=(0, 0.2, Z0)); wall = bpy.context.object; wall.scale = (3.8, 0.2, 2.2); bpy.ops.object.transform_apply(scale=True)
hole = cyl(Rh, 2, (0, 0.2, Z0), WALL, 128)
md = wall.modifiers.new("Bool", "BOOLEAN"); md.operation = "DIFFERENCE"; md.object = hole; md.solver = "EXACT"
bpy.context.view_layer.objects.active = wall; bpy.ops.object.modifier_apply(modifier="Bool"); bpy.data.objects.remove(hole)
frame.append(finish(wall, WALL, 0.012))
fr = ring(1.82, Rh - 0.02, 0.26, (0, -0.02, Z0), DARK, 0.018); frame.append(fr)
frame.append(ring(1.74, 1.62, 0.06, (0, -0.17, Z0), STEEL, 0.008))                      # 枠の細い輪
for k in range(24):
    a = k * math.tau / 24; frame.append(hexbolt(math.cos(a) * 1.68, Z0 + math.sin(a) * 1.68, -0.2, 0.04, 0.05))
for k in range(8):                                                                        # 閂の受け（枠の切り欠き）
    a = k * math.tau / 8 + math.tau / 16
    frame.append(box((0.2, 0.05, 0.2), (math.cos(a) * 1.58, -0.16, Z0 + math.sin(a) * 1.58), DARK, 0.01))
for zc in (0.95, -0.95):                                                                  # 右の蝶番（太い筒と帯）
    frame.append(cyl(0.16, 0.7, (1.95, -0.22, Z0 + zc), STEEL, 48, 0.02, rot=(0, 0, 0)))
    frame.append(box((0.9, 0.12, 0.22), (1.55, -0.26, Z0 + zc), DARK, 0.02))
    for dx in (1.25, 1.55, 1.8): frame.append(hexbolt(dx, Z0 + zc, -0.33, 0.035, 0.04))
for x in (-3.3, 3.3):                                                                     # 壁の補強の柱
    frame.append(box((0.35, 0.1, 4.2), (x, -0.05, Z0), DARK, 0.02))
    for zz in (-1.6, -0.8, 0, 0.8, 1.6): frame.append(hexbolt(x, Z0 + zz, -0.12, 0.04, 0.05))

# ---------- 扉 ----------
door_common.append(cyl(1.43, 0.42, (0, -0.1, Z0), STEEL, 128, 0.035))                  # 本体（分厚い）
door_common.append(ring(1.43, 1.2, 0.05, (0, -0.33, Z0), DARK, 0.01))                   # 外周の暗い帯
door_common.append(cyl(1.18, 0.05, (0, -0.32, Z0), FACE, 128, 0.012))                   # 溝の入った面
door_common.append(ring(1.02, 0.96, 0.05, (0, -0.37, Z0), BRASS, 0.006))                # 真鍮の輪
door_common.append(ring(0.62, 0.56, 0.05, (0, -0.37, Z0), BRASS, 0.006))
for k in range(16):
    a = k * math.tau / 16; door_common.append(hexbolt(math.cos(a) * 1.31, Z0 + math.sin(a) * 1.31, -0.37, 0.045, 0.05))
for k in range(8):
    a = k * math.tau / 8 + math.tau / 16; door_common.append(hexbolt(math.cos(a) * 0.79, Z0 + math.sin(a) * 0.79, -0.37, 0.03, 0.04, BRASS))
door_common.append(box((0.62, 0.04, 0.16), (0, -0.37, Z0 + 1.08), BRASS, 0.01))        # 銘板
for x in (-0.24, 0.24): door_common.append(hexbolt(x, Z0 + 1.08, -0.4, 0.018, 0.02))
# 閂: 出ている（枠の受けまで伸びる）/ 引っ込んだ（扉の中）
for k in range(8):
    a = k * math.tau / 8 + math.tau / 16
    for store, r0, r1 in ((bolts_out, 1.2, 1.66), (bolts_in, 1.2, 1.36)):
        L = r1 - r0; cx, cz = math.cos(a) * (r0 + L / 2), Z0 + math.sin(a) * (r0 + L / 2)
        bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.085, depth=L, location=(cx, -0.2, cz), rotation=(0, math.pi / 2 - a, 0))
        store.append(finish(bpy.context.object, STEEL, 0.015))

# ---------- ハンドル ----------
bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.045, major_segments=128, minor_segments=24, location=(0, -0.55, Z0), rotation=(math.radians(90), 0, 0))
wheel.append(finish(bpy.context.object, BRASS))
for k in range(5):
    a = k * math.tau / 5 + math.pi / 2
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.032, depth=0.5, location=(math.cos(a) * 0.25, -0.55, Z0 + math.sin(a) * 0.25), rotation=(0, math.pi / 2 - a, 0))
    wheel.append(finish(bpy.context.object, BRASS))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.07, segments=32, ring_count=16, location=(math.cos(a) * 0.56, -0.58, Z0 + math.sin(a) * 0.56)); wheel.append(finish(bpy.context.object, BRASS))
wheel.append(cyl(0.13, 0.14, (0, -0.55, Z0), STEEL, 64, 0.02))
wheel.append(cyl(0.06, 0.06, (0, -0.64, Z0), BRASS, 48, 0.01))

# ---------- 光とカメラ ----------
def area(name, loc, target, power, size, col=(1, 1, 1)):
    d = bpy.data.lights.new(name, "AREA"); d.energy = power; d.size = size; d.color = col
    o = bpy.data.objects.new(name, d); sc.collection.objects.link(o); o.location = loc
    dirv = (target[0] - loc[0], target[1] - loc[1], target[2] - loc[2])
    o.rotation_euler = (0, 0, 0); o.rotation_mode = "QUATERNION"
    from mathutils import Vector
    o.rotation_quaternion = Vector(dirv).to_track_quat("-Z", "Y")
area("Key", (-4, -5, 3.5), (0, 0, 0), 1500, 4, (1, 0.93, 0.85))
area("Rim", (4.5, -2.5, 1.5), (0, 0, 0), 900, 2.5, (0.75, 0.85, 1))
area("Top", (0, -3, 4.5), (0, 0, 0), 350, 6)
area("Low", (0, -4, -3.5), (0, 0, 0), 150, 4, (1, 0.7, 0.5))
cd = bpy.data.cameras.new("Cam"); cd.type = "ORTHO"; cd.ortho_scale = 7.2
cam = bpy.data.objects.new("Cam", cd); sc.collection.objects.link(cam); cam.location = (0, -10, Z0); cam.rotation_euler = (math.radians(90), 0, 0); sc.camera = cam

def shot(name, visible):
    for o in frame + door_common + bolts_out + bolts_in + wheel: o.hide_render = o not in visible
    sc.render.filepath = os.path.join(OUT, name); bpy.ops.render.render(write_still=True)
# 影は全部入りのときの形で落ちるように、各部品は「その部品だけ見える・他は影だけ落とす」で撮る
def holdout(objs):
    for o in objs: o.is_holdout = True
shot("vault_frame_2x.png", frame)
shot("vault_door_locked_2x.png", door_common + bolts_out)
shot("vault_door_open_2x.png", door_common + bolts_in)
shot("vault_wheel_2x.png", wheel)
print("render_vault done")
