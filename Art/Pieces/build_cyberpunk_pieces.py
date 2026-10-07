"""
Builds the cyberpunk chess piece set in Blender and renders a preview sheet.
Run:  Blender -b --python build_cyberpunk_pieces.py -- <out_dir>
Each piece is two meshes: <Piece>_Body (tinted per team) and <Piece>_Glow (neon light strips).
Pieces stand on z=0, face +X, and are sized in meters (pawn ~0.95 tall, king ~1.45).
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.dirname(__file__)
SIDES = 8  # octagonal: faceted, techy silhouettes

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ───────── mesh helpers ─────────

def new_obj(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    return ob

def lathe(name, profile, sides=SIDES, rot=math.pi / SIDES):
    """Revolve a (radius, z) profile around Z. Radius 0 points become poles."""
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        if r <= 1e-6:
            rings.append([bm.verts.new((0, 0, z))])
        else:
            rings.append([bm.verts.new((r * math.cos(rot + i * 2 * math.pi / sides),
                                        r * math.sin(rot + i * 2 * math.pi / sides), z)) for i in range(sides)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1: continue
        for i in range(sides):
            j = (i + 1) % sides
            if len(a) == 1:   bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1: bm.faces.new((a[i], a[j], b[0]))
            else:             bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return new_obj(name, bm)

def ring(name, r, z, h, sides=SIDES):
    return lathe(name, [(0, z), (r, z), (r, z + h), (0, z + h)], sides)

def box(name, size, loc, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    ob = new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = [math.radians(a) for a in rot]
    return ob

def cone(name, r1, r2, depth, loc, rot=(0, 0, 0), verts=4):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=verts, radius1=r1, radius2=r2, depth=depth)
    ob = new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = [math.radians(a) for a in rot]
    return ob

def gem(name, r, loc, subdiv=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    ob = new_obj(name, bm)
    ob.location = loc
    return ob

def torus(name, major, minor, loc, rot=(0, 0, 0), seg=16, rseg=6):
    bm = bmesh.new()
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        c = Vector((math.cos(a), math.sin(a), 0))
        rings.append([bm.verts.new(c * (major + minor * math.cos(2 * math.pi * k / rseg)) +
                                   Vector((0, 0, minor * math.sin(2 * math.pi * k / rseg)))) for k in range(rseg)])
    for i in range(seg):
        a, b = rings[i], rings[(i + 1) % seg]
        for k in range(rseg):
            l = (k + 1) % rseg
            bm.faces.new((a[k], b[k], b[l], a[l]))
    ob = new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = [math.radians(x) for x in rot]
    return ob

def join(name, parts):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = ob.data.name = name
    for poly in ob.data.polygons: poly.use_smooth = False
    return ob

def base(r):
    """Chunky octagonal plinth with a recessed slot for a neon ring"""
    body = lathe("base", [(0, 0), (r, 0), (r, 0.07), (r * 0.9, 0.07), (r * 0.9, 0.11),
                          (r * 0.94, 0.11), (r * 0.94, 0.15), (r * 0.78, 0.18), (0, 0.18)])
    glow = ring("base_glow", r * 0.955, 0.075, 0.03)
    return [body], [glow]

# ───────── the pieces ─────────

def pawn():
    b, g = base(0.36)
    b.append(lathe("torso", [(0, 0.18), (0.25, 0.18), (0.15, 0.48), (0.21, 0.52), (0.21, 0.56), (0, 0.56)]))
    b.append(gem("helmet", 0.21, (0, 0, 0.72)))
    g.append(ring("visor", 0.2, 0.7, 0.055))                         # wraparound visor
    g.append(ring("collar", 0.215, 0.525, 0.02))
    b.append(cone("antenna", 0.018, 0.012, 0.2, (-0.06, 0.07, 0.95), (12, -15, 0), 6))
    g.append(gem("antenna_tip", 0.04, (-0.085, 0.095, 1.05), 1))
    return b, g

def rook():
    b, g = base(0.4)
    b.append(lathe("tower", [(0, 0.18), (0.3, 0.18), (0.25, 0.78), (0.34, 0.83), (0.34, 0.98), (0.27, 0.98), (0.27, 0.94), (0, 0.94)]))
    g.append(ring("band", 0.272, 0.42, 0.05))
    g.append(ring("band2", 0.262, 0.56, 0.025))
    g.append(ring("core", 0.26, 0.94, 0.02))                          # glowing reactor top
    for i in range(4):
        a = math.radians(45 + 90 * i)
        b.append(box(f"merlon{i}", (0.17, 0.11, 0.16), (0.27 * math.cos(a), 0.27 * math.sin(a), 1.05), (0, 0, math.degrees(a) + 90)))
    g.append(box("beacon", (0.05, 0.05, 0.12), (0, 0, 1.04)))
    return b, g

def knight():
    b, g = base(0.4)
    b.append(box("neck", (0.3, 0.28, 0.62), (-0.03, 0, 0.47), (0, -14, 0)))
    b.append(box("head", (0.52, 0.27, 0.25), (0.12, 0, 0.82), (0, 28, 0)))
    b.append(box("snout", (0.2, 0.24, 0.16), (0.32, 0, 0.66), (0, 28, 0)))
    b.append(box("chest", (0.24, 0.3, 0.28), (0.1, 0, 0.32), (0, 10, 0)))
    for s in (-1, 1):
        b.append(cone(f"ear{s}", 0.07, 0.0, 0.2, (-0.06, 0.08 * s, 1.03), (-12 * s, -18, 45)))
    g.append(box("eyes", (0.07, 0.31, 0.05), (0.16, 0, 0.9), (0, 28, 0)))  # visor slit through both sides
    g.append(box("mouth", (0.03, 0.25, 0.025), (0.37, 0, 0.6), (0, 28, 0)))
    for i, (x, z, h) in enumerate([(-0.14, 0.98, 0.18), (-0.21, 0.82, 0.16), (-0.24, 0.64, 0.14), (-0.24, 0.47, 0.12)]):
        g.append(box(f"mohawk{i}", (0.12, 0.04, h), (x, 0, z), (0, -35 + 10 * i, 0)))   # punk crest
    return b, g

def bishop():
    b, g = base(0.38)
    b.append(lathe("body", [(0, 0.18), (0.24, 0.18), (0.13, 0.62), (0.2, 0.66), (0.2, 0.7), (0, 0.7)]))
    b.append(lathe("mitre", [(0, 0.7), (0.16, 0.71), (0.2, 0.86), (0.13, 1.02), (0, 1.14)]))
    g.append(ring("collar", 0.205, 0.665, 0.025))
    g.append(box("slash", (0.46, 0.035, 0.26), (0, 0.02, 0.9), (40, 0, 0)))
    g.append(gem("orb", 0.055, (0, 0, 1.18)))
    g.append(ring("waist", 0.175, 0.36, 0.035))
    return b, g

def queen():
    b, g = base(0.4)
    b.append(lathe("body", [(0, 0.18), (0.27, 0.18), (0.14, 0.78), (0.24, 0.85), (0.24, 0.92), (0.17, 0.95), (0, 0.95)]))
    g.append(ring("crownband", 0.245, 0.86, 0.035))
    g.append(ring("waist", 0.2, 0.46, 0.03))
    g.append(ring("waist2", 0.185, 0.53, 0.015))
    for i in range(8):
        a = 2 * math.pi * i / 8
        spike = cone(f"spike{i}", 0.05, 0.0, 0.24, (0.2 * math.cos(a), 0.2 * math.sin(a), 1.05),
                     (0, 22, math.degrees(a)), 4)
        (g if i % 2 == 0 else b).append(spike)
    g.append(gem("orb", 0.1, (0, 0, 1.07)))
    return b, g

def king():
    b, g = base(0.42)
    b.append(lathe("body", [(0, 0.18), (0.28, 0.18), (0.15, 0.88), (0.25, 0.95), (0.25, 1.03), (0.15, 1.07), (0, 1.07)]))
    g.append(ring("crownband", 0.255, 0.96, 0.04))
    g.append(ring("waist", 0.21, 0.5, 0.03))
    b.append(box("cross_v", (0.11, 0.11, 0.34), (0, 0, 1.23)))
    b.append(box("cross_h", (0.3, 0.11, 0.1), (0, 0, 1.27)))
    g.append(box("cross_core", (0.32, 0.125, 0.04), (0, 0, 1.27)))
    g.append(box("cross_core_v", (0.04, 0.125, 0.36), (0, 0, 1.23)))
    g.append(torus("halo", 0.23, 0.018, (0, 0, 1.16), (0, 0, 0)))
    return b, g

PIECES = [("Pawn", pawn), ("Knight", knight), ("Bishop", bishop), ("Rook", rook), ("Queen", queen), ("King", king)]

# ───────── materials ─────────

def principled(name, color, metal, rough):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Metallic"].default_value = metal
    p.inputs["Roughness"].default_value = rough
    return m

def emissive(name, color, strength):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Emission Color"].default_value = (*color, 1)
    p.inputs["Emission Strength"].default_value = strength
    return m

def hexc(h):
    h = h.lstrip("#")
    return tuple((int(h[i:i + 2], 16) / 255) ** 2.2 for i in (0, 2, 4))

teams = [
    ("Dark", principled("DarkBody", hexc("3A2366"), 0.8, 0.28), emissive("PinkGlow", hexc("FF2E9A"), 9)),
    ("Light", principled("LightBody", hexc("D9DCEC"), 0.6, 0.25), emissive("CyanGlow", hexc("29F0FF"), 9)),
]

# ───────── build: master pieces for export + two preview rows ─────────

masters = bpy.data.collections.new("Export"); scene.collection.children.link(masters)
for name, make in PIECES:
    bparts, gparts = make()
    body = join(f"{name}_Body", bparts)
    glow = join(f"{name}_Glow", gparts)
    bev = body.modifiers.new("Bevel", 'BEVEL'); bev.width = 0.01; bev.segments = 1; bev.harden_normals = True
    for ob in (body, glow):
        scene.collection.objects.unlink(ob); masters.objects.link(ob)

for row, (team, bodymat, glowmat) in enumerate(teams):
    for i, (name, _) in enumerate(PIECES):
        for part, mat in (("Body", bodymat), ("Glow", glowmat)):
            src = bpy.data.objects[f"{name}_{part}"]
            ob = src.copy(); ob.data = src.data.copy(); ob.name = f"{team}_{name}_{part}"
            ob.data.materials.clear(); ob.data.materials.append(mat)
            ob.location = (i * 1.25 + (0.62 if row == 0 else 0), row * 1.7, 0)
            ob.rotation_euler = (0, 0, math.radians(-60 if row == 0 else -120))
            scene.collection.objects.link(ob)
masters.hide_render = True

# Floor and lights
bpy.ops.mesh.primitive_plane_add(size=40, location=(2.9, 1, 0))
floor = bpy.context.active_object
floor.data.materials.append(principled("Floor", hexc("120028"), 0.6, 0.18))

def light(kind, loc, energy, color, size=2, target=(2.9, 0.7, 0.6)):
    d = bpy.data.lights.new(kind + str(loc), kind); d.energy = energy; d.color = color
    if kind == 'AREA': d.size = size
    o = bpy.data.objects.new(d.name, d); o.location = loc; scene.collection.objects.link(o)
    o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
light('AREA', (1.5, -4, 5), 900, (1, 0.95, 1), 4)
light('AREA', (-3, 4, 2.5), 700, (1, 0.25, 0.65), 3)
light('AREA', (9, 3.5, 2.5), 700, (0.2, 0.9, 1), 3)

cam_data = bpy.data.cameras.new("Cam"); cam_data.lens = 40
cam = bpy.data.objects.new("Cam", cam_data); scene.collection.objects.link(cam)
cam.location = (3.4, -7.6, 4.3)
cam.rotation_euler = (Vector((3.4, 0.85, 0.45)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam

world = bpy.data.worlds.new("World"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (*hexc("14002E"), 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6

for engine in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
    try: scene.render.engine = engine; break
    except TypeError: pass
scene.render.resolution_x, scene.render.resolution_y = 1920, 1000
scene.view_settings.view_transform = 'AgX'
try:
    scene.eevee.use_raytracing = True
except Exception: pass

# Bloom-style glare so the neon reads
try:
    tree = bpy.data.node_groups.new("Comp", 'CompositorNodeTree')
    scene.compositing_node_group = tree
    rl = tree.nodes.new('CompositorNodeRLayers')
    glare = tree.nodes.new('CompositorNodeGlare')
    out = tree.nodes.new('NodeGroupOutput')
    tree.interface.new_socket("Image", in_out='OUTPUT', socket_type='NodeSocketColor')
    for key, val in (("Type", 'Bloom'), ("Quality", 'High')):
        if key in glare.inputs: glare.inputs[key].default_value = val
    if "Strength" in glare.inputs: glare.inputs["Strength"].default_value = 0.6
    if "Size" in glare.inputs: glare.inputs["Size"].default_value = 0.5
    tree.links.new(rl.outputs["Image"], glare.inputs["Image"])
    tree.links.new(glare.outputs["Image"], out.inputs[0])
except Exception as e:
    print("compositor skipped:", e)

scene.render.filepath = os.path.join(OUT, "cyberpunk_pieces_preview.png")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "cyberpunk_pieces.blend"))
bpy.ops.render.render(write_still=True)
print("RENDERED", scene.render.filepath)
