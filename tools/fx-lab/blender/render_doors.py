# -*- coding: utf-8 -*-
"""扉のステップアップの絵を、落とした 3D 素材から Blender で撮る（本人 2026-09-29「自分で生成しないで素材取ってきて」）
使い方: blender -b -P render_doors.py -- <素材フォルダ> <出力フォルダ>
  素材フォルダ: large_castle_door/（Poly Haven, CC0）・rollershutter_door/（Poly Haven, CC0）・metalplates/（ambientCG MetalPlates006, CC0）
出力（すべて背景が透明の PNG・1 枚 = 画面の半分 640x720 か全面 1280x720）:
  door_gate_L.png / door_gate_R.png … 城門の左右の扉（扉の真ん中あたりを画面いっぱいに寄せて撮る）
  door_vault_L.png / door_vault_R.png / door_vault_lock.png … 金属板の素材で組んだ金庫の扉と中央の錠
  shutter.png … ローラーシャッター（横に伸ばして画面の幅に合わせる）"""
import bpy, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = argv[0], argv[1]
os.makedirs(OUT, exist_ok=True)

def setup_scene(w, h):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = w * 2, h * 2          # 2 倍で撮って縮める
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"
    sc.world = bpy.data.worlds.new("W")   # 素材の .blend に入っている環境（見つからない HDRI でピンクになる）は使わない
    sc.world.color = (0.05, 0.05, 0.06)
    try:
        ee = sc.eevee; ee.taa_render_samples = 32; ee.use_shadows = True
    except Exception: pass
    return sc

def add_lights(target_z, k=1.0):
    # 左上からの主光（エリア）・右からの縁の光・弱い正面の光
    def area(name, loc, rot, power, size, col=(1, 1, 1)):
        d = bpy.data.lights.new(name, "AREA"); d.energy = power; d.size = size; d.color = col
        o = bpy.data.objects.new(name, d); bpy.context.scene.collection.objects.link(o); o.location = loc; o.rotation_euler = rot
    area("Key", (-2.5, -3.5, target_z + 2.2), (math.radians(60), 0, math.radians(-35)), 900 * k, 3, (1, 0.95, 0.88))
    area("Rim", (3.0, -1.5, target_z + 0.5), (math.radians(80), 0, math.radians(60)), 350 * k, 2, (0.8, 0.88, 1))
    area("Fill", (0, -4, target_z), (math.radians(90), 0, 0), 120 * k, 5)

def ortho_cam(x, z, height, w, h):
    cd = bpy.data.cameras.new("Cam"); cd.type = "ORTHO"; cd.ortho_scale = height * max(1, w / h) if w > h else height
    # ortho_scale は長い辺。縦長（640x720）なら高さ、横長なら幅
    cd.ortho_scale = height if h >= w else height * w / h
    cam = bpy.data.objects.new("Cam", cd); bpy.context.scene.collection.objects.link(cam)
    cam.location = (x, -6, z); cam.rotation_euler = (math.radians(90), 0, 0)
    bpy.context.scene.camera = cam
    return cam

def render(path):
    bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)
    # 2 倍 → 1 倍（Blender の画像で縮めると手間なので、そのまま保存し、呼び出し側（Python）で縮める）

def reset(): bpy.ops.wm.read_factory_settings(use_empty=True)

# ---------- 城門 ----------
reset()
bpy.ops.wm.open_mainfile(filepath=os.path.join(SRC, "large_castle_door", "large_castle_door.blend"))
setup_scene(640, 720); add_lights(1.3)
L = bpy.data.objects["large_castle_door_left"]; R = bpy.data.objects["large_castle_door_right"]; F = bpy.data.objects["large_castle_door_frame"]
F.hide_render = True
for obj, name in ((L, "door_gate_L"), (R, "door_gate_R")):
    L.hide_render = obj is not L; R.hide_render = obj is not R
    bb = [obj.matrix_world @ v.co for v in obj.data.vertices]
    x0, x1 = min(p.x for p in bb), max(p.x for p in bb); zc = 1.25
    width = (x1 - x0) * 0.9                                      # 蝶番の金具の分だけ内側に寄せる（扉の板で画面を埋める）
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]: bpy.data.objects.remove(o)
    ortho_cam((x0 + x1) / 2, zc, width * 720 / 640, 640, 720)   # 縦 = 幅 × 720/640（扉の幅いっぱいに寄せる）
    # 格子窓の奥は暗い部屋（背景が透けないように、扉の後ろに暗い板）
    for o in [o for o in bpy.data.objects if o.name.startswith("Backing")]: bpy.data.objects.remove(o)
    bpy.ops.mesh.primitive_plane_add(size=1, location=((x0 + x1) / 2, 0.4, zc), rotation=(math.radians(90), 0, 0)); bk = bpy.context.object; bk.name = "Backing"
    bk.scale = (width * 1.02, width * 720 / 640 * 1.02, 1)
    bm = bpy.data.materials.get("BackingMat") or bpy.data.materials.new("BackingMat"); bm.diffuse_color = (0.02, 0.018, 0.016, 1)
    if bm.node_tree is None: bm.use_nodes = True
    bm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.02, 0.018, 0.016, 1); bm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1
    bk.data.materials.append(bm)
    render(os.path.join(OUT, name + "_2x.png"))

# ---------- シャッター ----------
reset()
bpy.ops.wm.open_mainfile(filepath=os.path.join(SRC, "rollershutter_door", "rollershutter_door.blend"))
setup_scene(1280, 720); add_lights(0.8, 0.3)
bpy.data.objects["rollershutter_door_graffiti"].hide_render = True
S = bpy.data.objects["rollershutter_door"]; S.scale.x = 2.2                     # 板は横縞なので横に伸ばしても形が崩れない
bpy.context.view_layer.update()
bb = [S.matrix_world @ v.co for v in S.data.vertices]
x0, x1 = min(p.x for p in bb), max(p.x for p in bb); zmin = min(p.z for p in bb)
wid = (x1 - x0) * 0.96; hei = wid * 720 / 1280
ortho_cam((x0 + x1) / 2, zmin + hei / 2 - 0.02, hei, 1280, 720)             # 下端（取っ手と下の縁）が画面の下に来る
render(os.path.join(OUT, "shutter_2x.png"))

# ---------- 金庫の扉（金属板の素材で組む）----------
reset()
setup_scene(640, 720); add_lights(1.0)
mp = os.path.join(SRC, "metalplates")
def mat_metal(name, scale, dark=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    tc = nt.nodes.new("ShaderNodeTexCoord"); mp_ = nt.nodes.new("ShaderNodeMapping"); mp_.inputs["Scale"].default_value = (scale, scale, scale)
    nt.links.new(tc.outputs["UV"], mp_.inputs["Vector"])
    def img(file, noncolor=False):
        n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(os.path.join(mp, file))
        if noncolor: n.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mp_.outputs["Vector"], n.inputs["Vector"]); return n
    col = img("MetalPlates006_4K-JPG_Color.jpg"); met = img("MetalPlates006_4K-JPG_Metalness.jpg", True); rou = img("MetalPlates006_4K-JPG_Roughness.jpg", True); nor = img("MetalPlates006_4K-JPG_NormalGL.jpg", True)
    if dark != 1.0:
        mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs["Fac"].default_value = 1; mul.inputs["Color2"].default_value = (dark, dark, dark * 1.05, 1)
        nt.links.new(col.outputs["Color"], mul.inputs["Color1"]); nt.links.new(mul.outputs["Color"], bsdf.inputs["Base Color"])
    else: nt.links.new(col.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(met.outputs["Color"], bsdf.inputs["Metallic"]); nt.links.new(rou.outputs["Color"], bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(nor.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return m
plate = mat_metal("Plate", 1.0); dark = mat_metal("Dark", 2.0, 0.45); lockm = mat_metal("Lock", 1.5, 0.8)
gold = bpy.data.materials.new("Gold"); gold.use_nodes = True; gb = gold.node_tree.nodes["Principled BSDF"]; gb.inputs["Base Color"].default_value = (1, 0.7, 0.25, 1); gb.inputs["Metallic"].default_value = 1; gb.inputs["Roughness"].default_value = 0.3
def box(loc, size, mat, bevel=0.02):
    bpy.ops.mesh.primitive_cube_add(location=loc); o = bpy.context.object; o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True); bpy.ops.object.shade_smooth()
    bpy.ops.object.modifier_add(type="BEVEL"); o.modifiers["Bevel"].width = bevel; o.modifiers["Bevel"].segments = 3
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.uv.cube_project(cube_size=2.0); bpy.ops.object.mode_set(mode="OBJECT")
    o.data.materials.append(mat); return o
def bolt(x, z, r=0.035):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=(x, -0.2, z)); o = bpy.context.object; o.scale.y = 0.5; bpy.ops.object.shade_smooth(); o.data.materials.append(dark); return o
# 左扉（幅 1.6、高さ 1.8）: 板・へこんだ 2 枚のパネル・縁の鋲・縦の補強
W2, H2 = 1.6, 1.8
box((-W2 / 2, 0, 0.9), (W2, 0.3, H2), plate)
for zc in (1.32, 0.48): box((-W2 / 2 - 0.05, -0.17, zc), (1.3, 0.06, 0.66), dark, 0.03)
for i in range(9):
    x = -W2 + 0.1 + i * 0.175
    for z in (0.06, 0.9, 1.74): bolt(x, z)
box((-0.06, -0.17, 0.9), (0.12, 0.08, H2), dark, 0.02)            # 合わせ目の縁
box((-W2 / 2, -0.18, 0.12), (W2, 0.05, 0.08), gold, 0.01)          # 金の帯（下）
box((-W2 / 2, -0.18, 1.68), (W2, 0.05, 0.08), gold, 0.01)          # 金の帯（上）
left_objs = [o for o in bpy.data.objects if o.type == "MESH"]
ortho_cam(-W2 / 2, 0.9, H2, 640, 720)
render(os.path.join(OUT, "door_vault_L_2x.png"))
# 右扉 = 左を鏡に映す
for o in left_objs: o.scale.x *= -1; o.location.x *= -1
bpy.context.view_layer.update()
for o in [o for o in bpy.data.objects if o.type == "CAMERA"]: bpy.data.objects.remove(o)
ortho_cam(W2 / 2, 0.9, H2, 640, 720)
render(os.path.join(OUT, "door_vault_R_2x.png"))
for o in left_objs: o.hide_render = True
# 中央の錠: 太い輪・内側の円盤・6 本の握り・中心の金
setup_scene(360, 360)
bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=0.2, location=(0, -0.3, 0.9), rotation=(math.radians(90), 0, 0)); o = bpy.context.object; o.data.materials.append(lockm); bpy.ops.object.shade_smooth()
bpy.ops.object.modifier_add(type="BEVEL"); o.modifiers["Bevel"].width = 0.03; o.modifiers["Bevel"].segments = 4
bpy.ops.mesh.primitive_cylinder_add(radius=0.38, depth=0.1, location=(0, -0.42, 0.9), rotation=(math.radians(90), 0, 0)); o = bpy.context.object; o.data.materials.append(dark)
for k in range(6):
    a = k * math.pi / 3
    if k < 3:   # 中心を通る 3 本で 6 方向
        bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=0.72, location=(0, -0.55, 0.9), rotation=(0, math.pi / 2 - a, 0)); bpy.context.object.data.materials.append(gold)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, location=(math.cos(a) * 0.36, -0.55, 0.9 + math.sin(a) * 0.36)); bpy.context.object.data.materials.append(gold)
bpy.ops.mesh.primitive_cylinder_add(radius=0.1, depth=0.12, location=(0, -0.6, 0.9), rotation=(math.radians(90), 0, 0)); bpy.context.object.data.materials.append(gold)
for o in [o for o in bpy.data.objects if o.type == "CAMERA"]: bpy.data.objects.remove(o)
ortho_cam(0, 0.9, 1.1, 360, 360)
render(os.path.join(OUT, "door_vault_lock_2x.png"))
print("render_doors done")
