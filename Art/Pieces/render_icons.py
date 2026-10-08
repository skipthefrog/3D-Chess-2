"""
Renders a transparent-background picture of every cyberpunk piece for each team, used by
menus such as pawn promotion. Output: Resources/PieceIcons/<Light|Dark>_<Piece>.png
Run:  Blender -b cyberpunk_pieces.blend --python render_icons.py -- <out_dir>
"""
import bpy, sys, os, math
from mathutils import Vector

OUT = sys.argv[sys.argv.index("--") + 1]
scene = bpy.context.scene
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = 512
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'

cam = scene.camera
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.75

pieces = ["Pawn", "Knight", "Bishop", "Rook", "Queen", "King"]
teams = {"Dark": "Dark", "Light": "Light"}
all_objs = [o for o in scene.objects if o.type == 'MESH']

for team in teams:
    for name in pieces:
        keep = {f"{team}_{name}_Body", f"{team}_{name}_Glow"}
        for o in all_objs:
            o.hide_render = o.name not in keep
        body = bpy.data.objects[f"{team}_{name}_Body"]
        target = body.location + Vector((0, 0, 0.68))
        # Three-quarter view from the front, slightly above
        direction = Vector((math.cos(math.radians(-70)), math.sin(math.radians(-70)), 0.45)).normalized()
        cam.location = target + direction * 6
        cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = os.path.join(OUT, f"{team}_{name}.png")
        bpy.ops.render.render(write_still=True)
        print("ICON", team, name)
