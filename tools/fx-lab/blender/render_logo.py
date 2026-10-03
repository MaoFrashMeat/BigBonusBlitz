# -*- coding: utf-8 -*-
"""ロゴの代役を Blender で作る（本人 2026-10-03 の参考動画: 「SG Special Grade RUSH」のロゴの周りで輝く演出。ロゴ本体は無いので代役を 3D で）
使い方: blender -b -P render_logo.py -- <出力フォルダ> [samples=64] [res=2048]
出力: logo_sg.png（金の縁取り＋面は白。透過）/ logo_sg_face.png（面だけ白のマスク。シェーダーで面に虹を流す）
金の縁は面取り（ベベル）で立体に。文字は Impact 斜体風（Blender で x に 0.25 せん断）"""
import bpy, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]; os.makedirs(OUT, exist_ok=True)
SAMPLES = int(argv[1]) if len(argv) > 1 else 64
RES = int(argv[2]) if len(argv) > 2 else 2048
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = SAMPLES; sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = RES, RES * 9 // 16; sc.render.film_transparent = True
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"
sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True; nt = w.node_tree
bg = nt.nodes["Background"]; tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); ramp = nt.nodes.new("ShaderNodeValToRGB")
ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (0.02, 0.01, 0.005, 1)
ramp.color_ramp.elements[1].position = 0.65; ramp.color_ramp.elements[1].color = (0.6, 0.4, 0.2, 1)
nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"]); nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"]); nt.links.new(ramp.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.6

def gold(name, base, rough):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*base, 1); b.inputs["Metallic"].default_value = 1.0; b.inputs["Roughness"].default_value = rough; return m
def flat(name, col, strength=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; t = m.node_tree
    for n in list(t.nodes): t.nodes.remove(n)
    e = t.nodes.new("ShaderNodeEmission"); e.inputs["Color"].default_value = (*col, 1); e.inputs["Strength"].default_value = strength
    o = t.nodes.new("ShaderNodeOutputMaterial"); t.links.new(e.outputs[0], o.inputs[0]); return m
GOLD = gold("Gold", (1.0, 0.6, 0.12), 0.22); WHITE = flat("Face", (1, 1, 1)); BLACK = flat("Hide", (0, 0, 0))

def text(body, size, loc, extrude, bevel, mats, shear=0.25):
    cu = bpy.data.curves.new(body, "FONT"); cu.body = body; cu.size = size; cu.align_x = "CENTER"; cu.align_y = "CENTER"
    try: cu.font = bpy.data.fonts.load("C:/Windows/Fonts/impact.ttf")
    except Exception: pass
    cu.extrude = extrude; cu.bevel_depth = bevel; cu.bevel_resolution = 4; cu.shear = shear; cu.offset = -bevel * 0.6   # ベベルで太る分を戻す
    o = bpy.data.objects.new(body, cu); sc.collection.objects.link(o); o.location = loc; o.rotation_euler = (math.radians(90), 0, 0)
    for m in mats: cu.materials.append(m)
    # FONT は面の種類で材質を分けられない → メッシュにして、法線が手前（-Y）を向く面だけ 0（面）、他は 1（縁・金）
    bpy.context.view_layer.objects.active = o; o.select_set(True); bpy.ops.object.convert(target="MESH"); o = bpy.context.object
    for poly in o.data.polygons: poly.material_index = 0 if poly.normal.z > 0.9 else 1   # 法線はローカル。手前はローカル +Z（X を 90° 回しているので）
    return o
# 文字: 面（material 0）・縁（1）・ベベル（2）。Blender の FONT は material index を面 / 前後 / ベベルに割り当てる
def logo(face_mat):
    sg = text("SG", 3.4, (0, 0, 0.45), 0.2, 0.075, [face_mat, GOLD, GOLD])
    rush = text("RUSH", 1.15, (0.1, -0.05, -1.6), 0.1, 0.04, [face_mat, GOLD, GOLD])
    small = text("Special Grade", 0.4, (0.1, -0.05, -0.92), 0.03, 0.006, [WHITE, WHITE, WHITE], shear=0.0)
    return [sg, rush, small]

def area(name, loc, power, size, col):
    d = bpy.data.lights.new(name, "AREA"); d.energy = power; d.size = size; d.color = col
    o = bpy.data.objects.new(name, d); sc.collection.objects.link(o); o.location = loc; o.rotation_mode = "QUATERNION"
    from mathutils import Vector
    o.rotation_quaternion = (Vector((0, 0, 0)) - Vector(loc)).to_track_quat("-Z", "Y")
area("Key", (-4, -6, 5), 1500, 3, (1, 0.9, 0.7)); area("Rim", (5, -3, -2), 700, 2, (1, 0.75, 0.45)); area("Top", (0, -3, 6), 400, 6, (1, 0.95, 0.85))
cd = bpy.data.cameras.new("Cam"); cd.type = "ORTHO"; cd.ortho_scale = 7.2
cam = bpy.data.objects.new("Cam", cd); sc.collection.objects.link(cam); cam.location = (0, -12, 0); cam.rotation_euler = (math.radians(90), 0, 0); sc.camera = cam

objs = logo(WHITE)
sc.render.filepath = os.path.join(OUT, "logo_sg.png"); bpy.ops.render.render(write_still=True)
# 面のマスク: 面だけ白、縁は黒。光は切る（エミッションだけ）
for o in objs:
    for i in range(len(o.data.materials)): o.data.materials[i] = (WHITE if (i == 0 and not o.name.startswith("Special")) else BLACK)
for l in [o for o in sc.objects if o.type == "LIGHT"]: l.data.energy = 0
bg.inputs["Strength"].default_value = 0
sc.cycles.samples = 16
sc.render.filepath = os.path.join(OUT, "logo_sg_face.png"); bpy.ops.render.render(write_still=True)
print("logo done")
