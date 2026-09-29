# -*- coding: utf-8 -*-
"""金の扉（パチスロのステップアップの扉。本人 2026-09-29 の参考画像: 金の浮き彫り・中央の錠・放射の筋・炎の渦・横の閂とメダル・縁の飾り）
使い方: blender -b -P render_golddoor.py -- <出力フォルダ>
画面 16:9（7.2 x 4.05 m）を正面から撮る → gold_door_2x.png（2560x1440）。左右半分に切って扉にするのは呼び出し側"""
import bpy, bmesh, sys, os, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]; os.makedirs(OUT, exist_ok=True)
SAMPLES = int(argv[1]) if len(argv) > 1 else 128
RES = int(argv[2]) if len(argv) > 2 else 2560
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = SAMPLES; sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = RES, RES * 9 // 16
sc.render.film_transparent = False
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGB"
sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"; sc.view_settings.exposure = 0.0
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True; nt = w.node_tree
bg = nt.nodes["Background"]; tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); ramp = nt.nodes.new("ShaderNodeValToRGB")
ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (0.02, 0.008, 0.002, 1)
ramp.color_ramp.elements[1].position = 0.65; ramp.color_ramp.elements[1].color = (0.5, 0.3, 0.12, 1)
nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"]); nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"]); nt.links.new(ramp.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.6

def gold(name, base, rough, var=0.08):
    m = bpy.data.materials.new(name); m.use_nodes = True; t = m.node_tree; b = t.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*base, 1); b.inputs["Metallic"].default_value = 1.0
    n = t.nodes.new("ShaderNodeTexNoise"); n.inputs["Scale"].default_value = 25; n.inputs["Detail"].default_value = 6
    mr = t.nodes.new("ShaderNodeMapRange"); mr.inputs["To Min"].default_value = rough - var; mr.inputs["To Max"].default_value = rough + var
    t.links.new(n.outputs["Fac"], mr.inputs["Value"]); t.links.new(mr.outputs["Result"], b.inputs["Roughness"]); return m
GOLD = gold("Gold", (1.0, 0.52, 0.08), 0.22)
GOLD_B = gold("GoldBright", (1.0, 0.66, 0.2), 0.15)
GOLD_D = gold("GoldDeep", (0.45, 0.18, 0.03), 0.3)
GLASS = bpy.data.materials.new("Jewel"); GLASS.use_nodes = True; gb = GLASS.node_tree.nodes["Principled BSDF"]
gb.inputs["Base Color"].default_value = (0.8, 0.08, 0.02, 1); gb.inputs["Roughness"].default_value = 0.04; gb.inputs["Coat Weight"].default_value = 1.0; gb.inputs["Emission Color"].default_value = (1, 0.2, 0.04, 1); gb.inputs["Emission Strength"].default_value = 0.35

def finish(o, mat, bevel=0.0, seg=3):
    bpy.context.view_layer.objects.active = o
    if bevel > 0:
        md = o.modifiers.new("Bevel", "BEVEL"); md.width = bevel; md.segments = seg; md.limit_method = "ANGLE"; md.angle_limit = math.radians(30)
    try: bpy.ops.object.shade_smooth()
    except Exception: pass
    if o.type == "MESH": o.modifiers.new("WN", "WEIGHTED_NORMAL")
    o.data.materials.clear(); o.data.materials.append(mat); return o
def box(size, loc, mat, bevel=0.02, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot); o = bpy.context.object; o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True); return finish(o, mat, bevel)
def prism(pts2d, y0, depth, mat, bevel=0.015):
    """xz 平面の多角形を手前（-y）へ押し出す"""
    me = bpy.data.meshes.new("P"); bm = bmesh.new()
    vs = [bm.verts.new((x, y0, z)) for x, z in pts2d]; f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f]); ev = [e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, -depth, 0), verts=ev); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free(); o = bpy.data.objects.new("P", me); sc.collection.objects.link(o); return finish(o, mat, bevel)
def torus(R, r, loc, mat, arc=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=160, minor_segments=24, location=loc, rotation=(math.radians(90), 0, 0))
    return finish(bpy.context.object, mat)
def cyl(r, d, loc, mat, bevel=0.02, verts=128):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d, location=loc, rotation=(math.radians(90), 0, 0)); return finish(bpy.context.object, mat, bevel)
def curve(points, radius, mat, y):
    cu = bpy.data.curves.new("C", "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = radius; cu.bevel_resolution = 6; cu.use_fill_caps = True
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(points) - 1)
    for bp, (x, z, rr) in zip(sp.bezier_points, points):
        bp.co = (x, y, z); bp.handle_left_type = bp.handle_right_type = "AUTO"; bp.radius = rr
    o = bpy.data.objects.new("C", cu); sc.collection.objects.link(o); o.data.materials.append(mat); return o
def scroll(cx, cz, ang, size, mat, y, flip=1, tail=1.0):
    """炎の渦: 尾（細い）→ S 字 → 内側へ巻く（太い → 細い）"""
    pts = []
    for i in range(26):
        t = i / 25
        if t < 0.35:   # 尾の S 字
            s = t / 0.35; px = -1.6 * tail + s * 1.3 * tail; pz = math.sin(s * math.pi) * 0.35 * flip
        else:          # 巻き
            s = (t - 0.35) / 0.65; th = s * 1.6 * math.pi; rad = 0.55 * (1 - s * 0.8)
            px = -0.3 + math.sin(th) * rad + 0.3; pz = (0.55 - math.cos(th) * rad) * flip - 0.0
        rr = 0.35 + 0.9 * math.sin(min(1, t * 1.4) * math.pi * 0.5) * (1 - max(0, t - 0.7) * 2.2)
        ca, sa = math.cos(ang), math.sin(ang)
        x, z = px * size, pz * size
        pts.append((cx + x * ca - z * sa, cz + x * sa + z * ca, max(rr, 0.25)))
    return curve(pts, size * 0.13, mat, y)

def ridge(d, nrm, r0, r1, hw0, hw1, y0, h, mat):
    """断面が三角の稜（中心が手前へ高い）"""
    me = bpy.data.meshes.new("R"); bm = bmesh.new()
    def V(p, y): return bm.verts.new((p.x, y, p.y))
    a0, c0, b0 = V(d * r0 + nrm * hw0, y0), V(d * r0, y0 - h * 0.5), V(d * r0 - nrm * hw0, y0)
    a1, c1, b1 = V(d * r1 + nrm * hw1, y0), V(d * r1, y0 - h), V(d * r1 - nrm * hw1, y0)
    bm.faces.new((a0, a1, c1, c0)); bm.faces.new((c0, c1, b1, b0)); bm.faces.new((a0, c0, b0)); bm.faces.new((a1, b1, c1))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("R", me); sc.collection.objects.link(o)
    md = o.modifiers.new("Bevel", "BEVEL"); md.width = 0.01; md.segments = 2
    o.data.materials.append(mat); return o

HW, HH = 3.6, 2.025
# 地の板（奥・暗い金）
box((7.4, 0.1, 4.2), (0, 0.15, 0), GOLD_D, 0.0)
# 放射の筋: 太い面（低い）と細い稜線（高い）を交互に
N = 28
for k in range(N):
    a = k * math.tau / N
    if k % 2 == 0: hw0, hw1, dep = 0.05, 0.34, 0.05
    else: hw0, hw1, dep = 0.015, 0.1, 0.1
    r0, r1 = 0.7, 4.6
    d = Vector((math.cos(a), math.sin(a))); nrm = Vector((-d.y, d.x))
    ridge(d, nrm, r0, r1, hw0, hw1, 0.1, dep * 1.6, GOLD if k % 2 == 0 else GOLD_B)
# 炎の葉（筋の間）: 涙の形の先が曲がって尖る、厚く丸い浮き彫り。内と外の 2 段
def flame(cx, cz, ang, L, W, curl, mat):
    """炎の舌: 太い根元 → S 字に揺れて細る → 先が内へ巻く。断面が丸い管を奥行き方向に潰して浮き彫りにする"""
    pts = []
    n = 22
    for i in range(n + 1):
        t = i / n
        if t < 0.72:
            s_ = t / 0.72; px = curl * s_ * s_ * 0.6 + math.sin(s_ * math.pi * 1.2) * 0.12 * (1 if curl >= 0 else -1); pz = s_ * 0.85
        else:
            s_ = (t - 0.72) / 0.28; th = s_ * math.pi * 1.1; rc = 0.16 * (1 - s_ * 0.5); sg = 1 if curl >= 0 else -1
            px = curl * 0.6 + 0.12 * math.sin(math.pi * 1.2) * sg + sg * (rc - rc * math.cos(th)); pz = 0.85 + rc * math.sin(th)
        rr = (0.35 + 0.65 * math.sin(math.pi * min(1, t * 2.2) * 0.5)) * (1 - t) ** 0.9 + 0.06
        ca, sa = math.cos(ang), math.sin(ang); x, z = px * L, pz * L
        pts.append((cx + x * ca - z * sa, cz + x * sa + z * ca, rr))
    o = curve(pts, W, mat, 0.0); o.scale.y = 0.55; o.location.y = -0.02
    return o
for k in range(N):
    if k % 2 == 0: continue
    a = k * math.tau / N
    for rr, L, W in ((1.45, 0.62, 0.16), (2.45, 0.95, 0.22), (3.5, 1.1, 0.26)):
        c = Vector((math.cos(a), math.sin(a))) * rr
        side = 1 if (k // 2) % 2 == 0 else -1
        flame(c.x, c.y, a - math.pi / 2, L, W, 0.35 * side, GOLD_B)
        flame(c.x, c.y, a - math.pi / 2 + 0.55 * side, L * 0.6, W * 0.7, -0.5 * side, GOLD)
        flame(c.x, c.y, a - math.pi / 2 - 0.5 * side, L * 0.45, W * 0.6, 0.6 * side, GOLD)
# 上下の弧の帯（中央の錠を囲う）
torus(1.02, 0.07, (0, -0.08, 0), GOLD_B); torus(1.2, 0.035, (0, -0.06, 0), GOLD)
# 横の閂（左右）とメダル
for s in (-1, 1):
    box((2.75, 0.2, 0.42), (s * 2.1, -0.08, 0), GOLD, 0.05)
    box((2.75, 0.05, 0.08), (s * 2.1, -0.2, 0.13), GOLD_B, 0.02); box((2.75, 0.05, 0.08), (s * 2.1, -0.2, -0.13), GOLD_B, 0.02)
    box((0.66, 0.22, 0.66), (s * 2.05, -0.2, 0), GOLD, 0.07)                     # メダルの台
    cyl(0.25, 0.1, (s * 2.05, -0.33, 0), GOLD_B, 0.03)
    for j in range(9):                                                             # メダルの扇（貝の筋）
        aa = math.pi * (0.1 + 0.8 * j / 8); d = Vector((math.cos(aa), math.sin(aa)))
        prism([tuple(d * 0.05 + Vector((s * 2.05, -0.08))), tuple(d * 0.22 + Vector((s * 2.05, -0.08)) + Vector((-d.y, d.x)) * 0.03), tuple(d * 0.22 + Vector((s * 2.05, -0.08)) - Vector((-d.y, d.x)) * 0.03)], -0.38, 0.04, GOLD, 0.006)
    scroll(s * 3.25, 0.0, math.pi / 2 if s > 0 else -math.pi / 2, 0.3, GOLD_B, -0.2)     # 閂の端の渦
# 中央の錠
cyl(0.72, 0.28, (0, -0.2, 0), GOLD, 0.05); torus(0.62, 0.05, (0, -0.36, 0), GOLD_B); torus(0.38, 0.06, (0, -0.4, 0), GOLD_B)
cyl(0.3, 0.1, (0, -0.4, 0), GOLD_D, 0.02)
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.16, segments=48, ring_count=24, location=(0, -0.45, 0)); o = bpy.context.object; o.scale.y = 0.6; finish(o, GLASS)
for k in range(12):
    a = k * math.tau / 12; bpy.ops.mesh.primitive_uv_sphere_add(radius=0.035, segments=24, ring_count=12, location=(math.cos(a) * 0.5, -0.38, math.sin(a) * 0.5)); finish(bpy.context.object, GOLD_B)
# 合わせ目の縦の帯（上下）
box((0.14, 0.18, 4.1), (0, -0.02, 0), GOLD, 0.03)
# 外枠と四隅の飾り
T = 0.26
for (sx, sz, x, z) in ((7.4, T, 0, HH - T / 2), (7.4, T, 0, -HH + T / 2), (T, 4.1, -HW + T / 2, 0), (T, 4.1, HW - T / 2, 0)):
    box((sx, 0.24, sz), (x, -0.12, z), GOLD, 0.05)
    box((sx * (0.98 if sx > 1 else 0.4), 0.06, sz * (0.4 if sx > 1 else 0.98)), (x, -0.26, z), GOLD_B, 0.015)
for sx in (-1, 1):
    for sz in (-1, 1):
        cx, cz = sx * (HW - 0.55), sz * (HH - 0.55)
        box((0.7, 0.2, 0.7), (cx, -0.12, cz), GOLD, 0.08, rot=(0, math.radians(45), 0))
        cyl(0.2, 0.1, (cx, -0.26, cz), GOLD_B, 0.03)
        scroll(cx - sx * 0.5, cz - sz * 0.05, 0 if sx < 0 else math.pi, 0.32, GOLD_B, -0.2, flip=sz * sx)

# 光とカメラ
def area(name, loc, power, size, col):
    d = bpy.data.lights.new(name, "AREA"); d.energy = power; d.size = size; d.color = col
    o = bpy.data.objects.new(name, d); sc.collection.objects.link(o); o.location = loc; o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = (Vector((0, 0, 0)) - Vector(loc)).to_track_quat("-Z", "Y")
area("Key", (-5, -4, 5), 1800, 2.5, (1, 0.86, 0.62))
area("Rim", (6, -2.5, -1.5), 900, 2, (1, 0.72, 0.4))
area("Top", (0, -2, 6), 500, 7, (1, 0.9, 0.75))
area("Fill", (0, -9, 0), 60, 12, (1, 0.7, 0.35))
cd = bpy.data.cameras.new("Cam"); cd.type = "ORTHO"; cd.ortho_scale = 7.2
cam = bpy.data.objects.new("Cam", cd); sc.collection.objects.link(cam); cam.location = (0, -12, 0); cam.rotation_euler = (math.radians(90), 0, 0); sc.camera = cam
sc.render.filepath = os.path.join(OUT, "gold_door_2x.png"); bpy.ops.render.render(write_still=True)
print("golddoor done")
