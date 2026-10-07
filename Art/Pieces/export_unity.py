"""
Exports each cyberpunk piece from cyberpunk_pieces.blend to an FBX that Unity loads from
Resources/Pieces/Cyberpunk/<Piece>.fbx. Each FBX has two meshes: "Body" (team material)
and "NeonStrip" (emissive team color). Scaled to the game's cell size (2.8 units).
Run:  Blender -b cyberpunk_pieces.blend --python export_unity.py -- <out_dir>
"""
import bpy, sys, os

OUT = sys.argv[sys.argv.index("--") + 1]
SCALE = 1.6  # pawn ≈1.7 units tall, king ≈2.3, like the old pieces

for name in ("Pawn", "Knight", "Bishop", "Rook", "Queen", "King"):
    bpy.ops.object.select_all(action='DESELECT')
    parts = []
    for part, new_name in (("Body", "Body"), ("Glow", "NeonStrip")):
        src = bpy.data.objects[f"{name}_{part}"]
        ob = src.copy(); ob.data = src.data.copy()
        ob.name = new_name
        ob.data.materials.clear()
        bpy.context.scene.collection.objects.link(ob)
        ob.location = (0, 0, 0)
        ob.scale = (SCALE, SCALE, SCALE)
        ob.select_set(True)
        parts.append(ob)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, f"{name}.fbx"), use_selection=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    for ob in parts:
        bpy.data.objects.remove(ob, do_unlink=True)
    print("EXPORTED", name)
