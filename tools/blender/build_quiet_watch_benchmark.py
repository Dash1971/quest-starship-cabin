"""Build the original Quiet Watch exterior benchmark assets.

Run with Blender 4.5 LTS:
  blender --background --python tools/blender/build_quiet_watch_benchmark.py

The script is deterministic and owns every generated mesh and texture. It saves
the editable .blend source, Quest LOD FBXs, PBR texture maps, and two review
renders. No third-party geometry or texture asset is used.
"""

from pathlib import Path
import math

import bpy
import numpy as np
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "Assets/Art/QuietWatch/Models"
TEXTURE_DIR = ROOT / "Assets/Art/QuietWatch/Textures"
SOURCE_FILE = ROOT / "ArtSource/QuietWatchVisualBenchmark.blend"
REVIEW_DIR = ROOT / "Builds/ArtReview"

for directory in (MODEL_DIR, TEXTURE_DIR, SOURCE_FILE.parent, REVIEW_DIR):
    directory.mkdir(parents=True, exist_ok=True)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials,
                       bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def material(name, color, metallic=0.0, roughness=0.5, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def collection(name):
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    return coll


def move_to_collection(obj, coll):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    coll.objects.link(obj)
    return obj


def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj


def bevel(obj, amount=0.08, segments=2):
    modifier = obj.modifiers.new("Manufactured edge radius", "BEVEL")
    modifier.width = amount
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth_by_angle()
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)
    return obj


def box(coll, name, location, scale, mat, bevel_width=0.05, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, coll)
    assign(obj, mat)
    if bevel_width > 0:
        bevel(obj, bevel_width, 2)
    return obj


def cylinder(coll, name, location, radius, depth, mat, vertices=24,
             rotation=(math.pi / 2, 0, 0), bevel_width=0.04):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    move_to_collection(obj, coll)
    assign(obj, mat)
    if bevel_width > 0:
        bevel(obj, bevel_width, 2)
    return obj


def loft(coll, name, sections, mat, profile_points=8, bevel_width=0.05):
    """Create a faceted naval hull from (y, half_width, half_height) sections."""
    profile = [
        (0.0, 1.0), (0.72, 0.78), (1.0, 0.18), (0.86, -0.70),
        (0.0, -1.0), (-0.86, -0.70), (-1.0, 0.18), (-0.72, 0.78),
    ]
    profile = profile[:profile_points]
    vertices = []
    for y, width, height in sections:
        vertices.extend((px * width, y, pz * height) for px, pz in profile)
    faces = []
    count = len(profile)
    for ring in range(len(sections) - 1):
        for i in range(count):
            a = ring * count + i
            b = ring * count + (i + 1) % count
            c = (ring + 1) * count + (i + 1) % count
            d = (ring + 1) * count + i
            faces.append((a, b, c, d))
    faces.append(tuple(reversed(range(count))))
    start = (len(sections) - 1) * count
    faces.append(tuple(start + i for i in range(count)))
    mesh = bpy.data.meshes.new(name + " Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    coll.objects.link(obj)
    assign(obj, mat)
    if bevel_width > 0:
        bevel(obj, bevel_width, 2)
    return obj


def beam(coll, name, start, end, width, mat, bevel_width=0.04):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    obj = box(coll, name, (start + end) * 0.5, (width, direction.length, width),
              mat, bevel_width)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 1, 0)).rotation_difference(direction.normalized())
    return obj


def ring_segment(coll, name, radius_outer, radius_inner, depth, start_angle,
                 end_angle, segments, mat):
    vertices = []
    faces = []
    half = depth * 0.5
    for i in range(segments + 1):
        angle = start_angle + (end_angle - start_angle) * i / segments
        c, s = math.cos(angle), math.sin(angle)
        vertices += [
            (radius_inner * c, -half, radius_inner * s),
            (radius_outer * c, -half, radius_outer * s),
            (radius_outer * c, half, radius_outer * s),
            (radius_inner * c, half, radius_inner * s),
        ]
    for i in range(segments):
        a = i * 4
        b = (i + 1) * 4
        faces += [
            (a, b, b + 1, a + 1), (a + 3, a + 2, b + 2, b + 3),
            (a + 1, b + 1, b + 2, a + 2), (a, a + 3, b + 3, b),
        ]
    faces += [(0, 1, 2, 3),
              (segments * 4, segments * 4 + 3, segments * 4 + 2, segments * 4 + 1)]
    mesh = bpy.data.meshes.new(name + " Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    coll.objects.link(obj)
    assign(obj, mat)
    bevel(obj, min(depth * 0.12, 0.12), 2)
    return obj


def smart_uv(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58), island_margin=0.012)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def join_collection(coll, final_name):
    objects = [obj for obj in coll.objects if obj.type == "MESH"]
    for obj in objects:
        smart_uv(obj)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = final_name
    joined.data.name = final_name + " Mesh"
    joined.select_set(False)
    return joined


def engine_pod(coll, x, y, z, scale, mats, detail):
    hull, armour, dark, blue, _amber, _glass = mats
    cylinder(coll, "Engine housing", (x, y, z), 0.62 * scale, 3.3 * scale,
             dark, 20 if detail > 1 else 12, bevel_width=0.08 * scale)
    cylinder(coll, "Engine collar", (x, y + 1.58 * scale, z), 0.71 * scale,
             0.32 * scale, armour, 20 if detail > 1 else 12, bevel_width=0.04 * scale)
    cylinder(coll, "Engine aperture", (x, y + 1.78 * scale, z), 0.46 * scale,
             0.10 * scale, blue, 20 if detail > 1 else 12, bevel_width=0.01)
    if detail > 1:
        for side in (-1, 1):
            box(coll, "Engine service rail", (x + side * 0.58 * scale, y, z),
                (0.10 * scale, 2.5 * scale, 0.14 * scale), hull, 0.02)


def build_command_ship(lod, mats):
    hull, armour, dark, blue, amber, glass = mats
    coll = collection(f"QW_CommandShip_LOD{lod}")
    detail = 2 - lod
    sections = [
        (-12.2, 0.16, 0.18), (-10.2, 1.35, 0.52), (-7.1, 3.15, 0.86),
        (-2.3, 4.55, 1.12), (3.2, 4.15, 1.34), (7.0, 3.25, 1.17),
        (9.8, 2.35, 0.88),
    ]
    loft(coll, "Command hull", sections, hull, bevel_width=0.11 if lod == 0 else 0.07)
    loft(coll, "Port shoulder", [(-7.0, .15, .13), (-3.0, 2.8, .25),
         (4.8, 2.0, .28), (7.2, .25, .16)], armour, bevel_width=0.07)
    shoulder = coll.objects[-1]
    shoulder.location.x = -4.15
    shoulder.rotation_euler.y = math.radians(-4)
    loft(coll, "Starboard shoulder", [(-7.0, .15, .13), (-3.0, 2.8, .25),
         (4.8, 2.0, .28), (7.2, .25, .16)], armour, bevel_width=0.07)
    shoulder = coll.objects[-1]
    shoulder.location.x = 4.15
    shoulder.rotation_euler.y = math.radians(4)

    box(coll, "Dorsal mission deck", (0, -1.2, 1.42), (3.8, 7.1, 0.52), armour, .12)
    loft(coll, "Bridge crown", [(-5.8, .35, .20), (-4.3, 1.45, .38),
         (-1.2, 1.65, .48), (0.8, .70, .30)], glass, bevel_width=.08)
    box(coll, "Ventral keel", (0, 2.8, -1.35), (1.3, 8.8, .52), dark, .08)
    for x in (-2.75, 0, 2.75):
        engine_pod(coll, x, 8.3, -0.10 if x == 0 else 0.05, 1.0 if x == 0 else .86, mats, detail)

    if detail >= 1:
        for side in (-1, 1):
            box(coll, "Radiator", (side * 4.2, 2.0, 1.0), (.18, 5.2, 1.0), dark, .04,
                rotation=(0, math.radians(side * 5), 0))
            box(coll, "Docking shoulder", (side * 4.5, 4.6, -.35), (.65, 2.4, .70), armour, .08)
        for row in range(2):
            for index in range(9):
                x = -3.2 + index * .8
                box(coll, "Habitat window", (x, -1.7 + row * .52, 1.72),
                    (.28, .10, .08), amber if (index + row) % 4 == 0 else blue, .01)
        cylinder(coll, "Sensor crown", (0, -1.8, 2.12), .36, .42, dark, 16,
                 rotation=(0, 0, 0), bevel_width=.03)
        beam(coll, "Long range mast", (0, -1.8, 2.25), (0, -1.8, 3.35), .09, dark, .015)
    if detail >= 2:
        for side in (-1, 1):
            for index in range(7):
                y = -5.0 + index * 1.75
                box(coll, "Armour cassette", (side * (3.15 + .10 * index), y, 1.02),
                    (.78, 1.25, .10), armour if index % 2 else hull, .035)
            for index in range(5):
                y = -.5 + index * 1.55
                cylinder(coll, "Service manifold", (side * 3.95, y, -.65), .13, 1.1,
                         dark, 10, rotation=(0, 0, math.pi / 2), bevel_width=.02)
        for index in range(8):
            y = -6.7 + index * 1.65
            box(coll, "Keel plate", (0, y, -1.68), (1.02, 1.2, .09), armour, .025)
        for side in (-1, 1):
            beam(coll, "Antenna boom", (side * .9, -.7, 2.0),
                 (side * 1.6, -1.1, 3.0), .06, dark, .01)
            cylinder(coll, "Docking collar", (side * 4.86, 4.5, -.35), .33, .22,
                     amber, 16, rotation=(0, 0, math.pi / 2), bevel_width=.025)

    joined = join_collection(coll, f"QW_CommandShip_LOD{lod}")
    return coll, joined


def build_escort(variant, lod, mats):
    hull, armour, dark, blue, amber, glass = mats
    coll = collection(f"QW_Escort{variant}_LOD{lod}")
    detail = 2 - lod
    if variant == "Spear":
        sections = [(-8.0, .12, .14), (-6.1, 1.1, .42), (-2.4, 2.15, .62),
                    (2.5, 1.75, .72), (5.5, 1.2, .55)]
        loft(coll, "Escort spear hull", sections, hull, bevel_width=.08)
        for side in (-1, 1):
            loft(coll, "Spear flight vane", [(-3.8, .10, .08), (-1.0, 1.6, .16),
                 (3.8, .7, .13)], armour, bevel_width=.04)
            coll.objects[-1].location.x = side * 2.0
        engine_pod(coll, -.72, 5.0, 0, .62, mats, detail)
        engine_pod(coll, .72, 5.0, 0, .62, mats, detail)
    else:
        sections = [(-6.2, .12, .13), (-4.7, 1.5, .46), (-1.2, 2.8, .68),
                    (3.6, 2.4, .62), (5.4, 1.45, .48)]
        loft(coll, "Escort wing hull", sections, armour, bevel_width=.08)
        for side in (-1, 1):
            loft(coll, "Escort swept wing", [(-3.6, .15, .08), (-1.0, 2.5, .15),
                 (4.0, 1.0, .12)], hull, bevel_width=.04)
            coll.objects[-1].location.x = side * 2.5
        engine_pod(coll, -1.35, 4.8, -.05, .58, mats, detail)
        engine_pod(coll, 1.35, 4.8, -.05, .58, mats, detail)
    box(coll, "Escort bridge", (0, -1.0, .82), (1.6, 2.4, .28), glass, .06)
    if detail > 0:
        for side in (-1, 1):
            for i in range(4):
                box(coll, "Escort scale light", (side * 1.5, -2.0 + i * .8, .75),
                    (.22, .07, .06), amber if i == 0 else blue, .01)
        box(coll, "Escort dorsal machinery", (0, 1.8, .87), (1.1, 1.7, .24), dark, .04)
    joined = join_collection(coll, f"QW_Escort{variant}_LOD{lod}")
    return coll, joined


def build_station(lod, mats):
    hull, armour, dark, blue, amber, glass = mats
    coll = collection(f"QW_HarbourSector_LOD{lod}")
    detail = 2 - lod
    segments = 56 if lod == 0 else 28 if lod == 1 else 16
    ring_segment(coll, "Outer inhabited torus", 20.0, 17.6, 2.9,
                 math.radians(-150), math.radians(154), segments, hull)
    ring_segment(coll, "Armoured torus crown", 17.25, 15.6, 3.8,
                 math.radians(-145), math.radians(148), segments, armour)
    ring_segment(coll, "Window canyon", 15.4, 14.9, 4.1,
                 math.radians(-141), math.radians(144), segments, blue)
    cylinder(coll, "Harbour axial core", (0, 0, 0), 3.6, 18.0, dark,
             28 if lod == 0 else 16, rotation=(math.pi / 2, 0, 0), bevel_width=.16)
    cylinder(coll, "Observation drum", (0, -8.5, 0), 5.1, 1.7, armour,
             28 if lod == 0 else 16, rotation=(math.pi / 2, 0, 0), bevel_width=.14)
    cylinder(coll, "Observation lights", (0, -9.4, 0), 4.35, .10, amber,
             28 if lod == 0 else 16, rotation=(math.pi / 2, 0, 0), bevel_width=.02)
    for index in range(8 if lod < 2 else 4):
        angle = math.radians(index * 45 + 12)
        inner = (math.cos(angle) * 3.4, 0, math.sin(angle) * 3.4)
        outer = (math.cos(angle) * 15.7, 0, math.sin(angle) * 15.7)
        beam(coll, "Radial load truss", inner, outer, .52 if lod == 0 else .7, dark, .06)
    for side in (-1, 1):
        beam(coll, "Docking causeway", (side * 8, -1.2, -8.5),
             (side * 27, -3.0, -10.0), 1.2, hull, .12)
        box(coll, "Docking pier", (side * 25.7, -3.0, -9.9),
            (8.0, 5.0, 3.0), armour, .22, rotation=(0, math.radians(side * 5), 0))
        box(coll, "Hangar mouth", (side * 25.7, -5.55, -9.9),
            (5.6, .14, 1.4), dark, .03, rotation=(0, math.radians(side * 5), 0))
        box(coll, "Hangar approach light", (side * 25.7, -5.66, -10.75),
            (6.2, .08, .10), amber, .01, rotation=(0, math.radians(side * 5), 0))
    if detail > 0:
        for index in range(24):
            angle = math.radians(-134 + index * 11.4)
            radius = 18.9
            x, z = math.cos(angle) * radius, math.sin(angle) * radius
            box(coll, "Habitat bay", (x, -2.0 if index % 2 else 2.0, z),
                (1.0, 1.7, .55), armour, .06, rotation=(0, -angle, 0))
        for side in (-1, 1):
            for i in range(5):
                x = side * (21.8 + i * 1.45)
                box(coll, "Pier service module", (x, -2.7, -8.2 + (i % 2) * .7),
                    (1.0, 2.6, .75), hull if i % 2 else dark, .08)
    if detail > 1:
        for index in range(40):
            angle = math.radians(-137 + index * 6.9)
            radius = 16.3
            x, z = math.cos(angle) * radius, math.sin(angle) * radius
            box(coll, "Window block", (x, -2.12, z), (.40, .10, .16),
                amber if index % 7 == 0 else blue, .01, rotation=(0, -angle, 0))
        for index in range(10):
            angle = math.radians(index * 36)
            start = (math.cos(angle) * 4.8, -8.6, math.sin(angle) * 4.8)
            end = (math.cos(angle) * 7.0, -11.0, math.sin(angle) * 7.0)
            beam(coll, "Traffic mast", start, end, .10, dark, .015)
            box(coll, "Traffic beacon", end, (.22, .22, .22), amber, .025)
    joined = join_collection(coll, f"QW_HarbourSector_LOD{lod}")
    return coll, joined


def export_collection(coll, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in coll.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(iter(coll.objects))
    bpy.ops.export_scene.fbx(
        filepath=str(MODEL_DIR / f"{name}.fbx"), use_selection=True,
        object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
        bake_anim=False, mesh_smooth_type="FACE")
    bpy.ops.object.select_all(action="DESELECT")


def write_texture(name, rgba, colorspace="sRGB"):
    height, width, _ = rgba.shape
    image = bpy.data.images.new(name, width=width, height=height, alpha=True, float_buffer=False)
    image.colorspace_settings.name = colorspace
    image.pixels.foreach_set(rgba.astype(np.float32).ravel())
    image.filepath_raw = str(TEXTURE_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def make_pbr_maps():
    size = 1024
    yy, xx = np.mgrid[0:size, 0:size]
    fine_x = np.minimum(xx % 128, 127 - xx % 128)
    fine_y = np.minimum(yy % 96, 95 - yy % 96)
    coarse_x = np.minimum(xx % 384, 383 - xx % 384)
    seam = (fine_x < 3) | (fine_y < 3) | (coarse_x < 5)
    seed = ((xx // 128) * 31 + (yy // 96) * 17) % 11
    variation = (seed.astype(np.float32) - 5.0) / 95.0
    base = np.zeros((size, size, 4), dtype=np.float32)
    base[..., 0] = .31 + variation
    base[..., 1] = .35 + variation
    base[..., 2] = .40 + variation * .8
    base[..., :3][seam] *= .34
    base[..., 3] = 1
    write_texture("QW_Hull_BaseColor", base)

    packed = np.zeros_like(base)
    packed[..., 0] = np.where(seam, .20, .72)
    packed[..., 1:3] = packed[..., 0:1]
    packed[..., 3] = np.where(seam, .16, .42 + variation * 1.4)
    write_texture("QW_Hull_MetallicSmoothness", packed, "Non-Color")

    height_map = np.where(seam, .25, .62 + variation * 1.5)
    grad_y, grad_x = np.gradient(height_map)
    normal = np.zeros_like(base)
    normal[..., 0] = np.clip(.5 - grad_x * 1.8, 0, 1)
    normal[..., 1] = np.clip(.5 - grad_y * 1.8, 0, 1)
    normal[..., 2] = 1
    normal[..., 3] = 1
    write_texture("QW_Hull_Normal", normal, "Non-Color")

    ao = np.ones_like(base)
    ao[..., :3] = np.where(seam[..., None], .35, .92)
    ao[..., 3] = 1
    write_texture("QW_Hull_Occlusion", ao, "Non-Color")

    emission = np.zeros_like(base)
    windows = ((yy % 192) > 145) & ((yy % 192) < 151) & ((xx % 128) > 18) & ((xx % 128) < 96)
    emission[..., 0] = np.where(windows, .08, 0)
    emission[..., 1] = np.where(windows, .48, 0)
    emission[..., 2] = np.where(windows, .78, 0)
    emission[..., 3] = 1
    write_texture("QW_Hull_Emission", emission, "Non-Color")


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_review(coll, name, camera_location, target, resolution=(1280, 720)):
    for candidate in bpy.data.collections:
        candidate.hide_render = candidate != coll
    bpy.ops.object.camera_add(location=camera_location)
    camera = bpy.context.object
    camera.name = name + " Review Camera"
    look_at(camera, target)
    camera.data.lens = 52
    bpy.context.scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(12, 8, 18))
    key = bpy.context.object
    key.data.energy = 1800
    key.data.shape = "DISK"
    key.data.size = 12
    look_at(key, target)
    bpy.ops.object.light_add(type="AREA", location=(-14, 2, 4))
    fill = bpy.context.object
    fill.data.energy = 900
    fill.data.color = (.22, .42, .75)
    fill.data.size = 10
    look_at(fill, target)
    scene = bpy.context.scene
    # Workbench is deliberately used for the automated silhouette review. The
    # binding material/lighting verdict happens in Unity's Quest renderer.
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(REVIEW_DIR / f"{name}.png")
    scene.world.color = (.003, .005, .010)
    scene.render.film_transparent = False
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(key, do_unlink=True)
    bpy.data.objects.remove(fill, do_unlink=True)


def main():
    reset_scene()
    mats = (
        material("QW Hull PBR", (.15, .18, .21), .72, .38),
        material("QW Armour PBR", (.34, .37, .39), .58, .31),
        material("QW Machinery", (.035, .045, .055), .80, .29),
        material("QW Emissive Blue", (.01, .09, .16), .10, .24, (.10, .65, 1.0), 7.0),
        material("QW Emissive Amber", (.16, .055, .01), .08, .25, (1.0, .28, .055), 6.0),
        material("QW Bridge Glass", (.015, .055, .075), .42, .16, (.03, .22, .32), 1.0),
    )
    make_pbr_maps()

    command = []
    spear = []
    wing = []
    station = []
    for lod in range(3):
        command.append(build_command_ship(lod, mats))
        spear.append(build_escort("Spear", lod, mats))
        wing.append(build_escort("Wing", lod, mats))
        station.append(build_station(lod, mats))

    for family in (command, spear, wing, station):
        for coll, _obj in family:
            export_collection(coll, coll.name)

    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_FILE))
    render_review(command[0][0], "command-ship-benchmark", (20, 28, 15), (0, 0, 0))
    render_review(station[0][0], "harbour-sector-benchmark", (31, 32, 24), (0, 0, 0))
    for candidate in bpy.data.collections:
        candidate.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_FILE))
    print(f"Saved {SOURCE_FILE}")
    print(f"Exported {len(list(MODEL_DIR.glob('*.fbx')))} FBX assets")


if __name__ == "__main__":
    main()
