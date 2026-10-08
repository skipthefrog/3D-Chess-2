"""
Renders the 1024x1024 app icon: the cyberpunk rook, glowing cyan, on a deep violet
neon backdrop with a pink rim light and a glowing floor ring. No transparency (iOS rule).
Run:  Blender -b cyberpunk_pieces.blend --python render_app_icon.py -- <out.png>
"""
import bpy, sys, math
from mathutils import Vector

OUT = sys.argv[sys.argv.index("--") + 1]
scene = bpy.context.scene
for o in list(scene.objects):
    if o.type == 'MESH' and o.name not in ("Light_Rook_Body", "Light_Rook_Glow"):
        o.hide_render = True
    if o.type == 'LIGHT':
        bpy.data.objects.remove(o, do_unlink=True)

def hexc(h):
    h = h.lstrip("#"); return tuple((int(h[i:i+2], 16) / 255) ** 2.2 for i in (0, 2, 4))

def mat_emit(name, col, strength):
    m = bpy.data.materials.new(name); m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*col, 1)
    p.inputs["Emission Color"].default_value = (*col, 1)
    p.inputs["Emission Strength"].default_value = strength
    return m

body = bpy.data.objects["Light_Rook_Body"]; glow = bpy.data.objects["Light_Rook_Glow"]
for o in (body, glow):
    o.location = (0, 0, 0); o.rotation_euler = (0, 0, math.radians(-30))
bm = body.data.materials[0]
p = bm.node_tree.nodes["Principled BSDF"]
p.inputs["Base Color"].default_value = (*hexc("2A1650"), 1)
p.inputs["Metallic"].default_value = 0.9; p.inputs["Roughness"].default_value = 0.22
glow.data.materials.clear(); glow.data.materials.append(mat_emit("IconCyan", hexc("29F0FF"), 14))

# Glowing pink ring on the floor under the rook
bpy.ops.mesh.primitive_torus_add(major_radius=0.62, minor_radius=0.025, location=(0, 0, 0.005))
ring = bpy.context.active_object; ring.data.materials.append(mat_emit("IconPink", hexc("FF2E9A"), 10))
# Reflective dark floor
bpy.ops.mesh.primitive_plane_add(size=600, location=(0, 0, 0))
floor = bpy.context.active_object
fm = bpy.data.materials.new("IconFloor"); fm.use_nodes = True
fp = fm.node_tree.nodes["Principled BSDF"]
fp.inputs["Base Color"].default_value = (*hexc("12002A"), 1); fp.inputs["Metallic"].default_value = 0.7; fp.inputs["Roughness"].default_value = 0.12
floor.data.materials.append(fm)

# Backdrop: violet vertical gradient with a magenta glow behind the rook
world = scene.world; world.use_nodes = True
nt = world.node_tree; nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputWorld"); bg = nt.nodes.new("ShaderNodeBackground")
tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); ramp = nt.nodes.new("ShaderNodeValToRGB")
nt.links.new(tc.outputs["Window"], sep.inputs[0]); nt.links.new(sep.outputs["Y"], ramp.inputs[0])
ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[0].color = (*hexc("3A0858"), 1)
ramp.color_ramp.elements[1].position = 1.0; ramp.color_ramp.elements[1].color = (*hexc("0A0018"), 1)
nt.links.new(ramp.outputs[0], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 1.0
nt.links.new(bg.outputs[0], out.inputs[0])

# Glow disc behind the rook (a camera-facing emissive plane)
bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.6, fill_type='NGON', location=(-0.15, 1.8, 1.25))
halo = bpy.context.active_object
halo.rotation_euler = (Vector((0.75, -4.4, 1.55)) - halo.location).to_track_quat('Z', 'Y').to_euler()
hm = bpy.data.materials.new("Halo"); hm.use_nodes = True; hn = hm.node_tree
hn.nodes.clear()
ho = hn.nodes.new("ShaderNodeOutputMaterial"); he = hn.nodes.new("ShaderNodeEmission"); htc = hn.nodes.new("ShaderNodeTexCoord")
hg = hn.nodes.new("ShaderNodeTexGradient"); hg.gradient_type = 'SPHERICAL'
hmap = hn.nodes.new("ShaderNodeMapping"); hmap.inputs["Scale"].default_value = (1 / 1.6, 1 / 1.6, 1 / 1.6)
hmix = hn.nodes.new("ShaderNodeMixShader"); htr = hn.nodes.new("ShaderNodeBsdfTransparent")
hn.links.new(htc.outputs["Object"], hmap.inputs[0]); hn.links.new(hmap.outputs[0], hg.inputs[0])
he.inputs["Color"].default_value = (*hexc("FF2E9A"), 1); he.inputs["Strength"].default_value = 1.8
hpow = hn.nodes.new("ShaderNodeMath"); hpow.operation = 'POWER'; hpow.inputs[1].default_value = 1.8
hn.links.new(hg.outputs["Fac"], hpow.inputs[0]); hn.links.new(hpow.outputs[0], hmix.inputs[0]); hn.links.new(htr.outputs[0], hmix.inputs[1]); hn.links.new(he.outputs[0], hmix.inputs[2])
hn.links.new(hmix.outputs[0], ho.inputs[0])
hm.blend_method = 'BLEND' if hasattr(hm, "blend_method") else None
halo.data.materials.append(hm)

def light(loc, energy, color, size):
    d = bpy.data.lights.new("L" + str(loc), 'AREA'); d.energy = energy; d.color = color; d.size = size
    o = bpy.data.objects.new(d.name, d); o.location = loc; scene.collection.objects.link(o)
    o.rotation_euler = (Vector((0, 0, 0.8)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    o.visible_camera = False; o.visible_glossy = False
light((-2.5, -2.5, 3.5), 260, (0.75, 0.9, 1.0), 3)   # cool key
light((2.6, 1.0, 1.4), 400, (1.0, 0.2, 0.6), 1.5)    # pink rim
light((-2.4, 1.6, 1.2), 300, (0.2, 0.9, 1.0), 1.5)   # cyan rim

cam = scene.camera
cam.data.type = 'PERSP'; cam.data.lens = 85
cam.location = (0.75, -4.4, 1.55)
cam.rotation_euler = (Vector((0, 0, 0.62)) - cam.location).to_track_quat('-Z', 'Y').to_euler()

scene.render.resolution_x = scene.render.resolution_y = 1024
scene.render.film_transparent = False
scene.render.image_settings.file_format = 'PNG'; scene.render.image_settings.color_mode = 'RGB'
scene.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("ICON RENDERED", OUT)
