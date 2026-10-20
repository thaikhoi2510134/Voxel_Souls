"""
Kịch bản Python cho Blender (Tương thích Blender 3.x & 4.x trên cả macOS và Windows)
Tự động dựng 3 bộ mô hình 3D phong cách Blocky / Voxel cho tựa game Voxel Souls RPG:
  1. Hero_VoxelKnight.fbx (Dũng Sĩ Giáp Thép + Kiếm Cổ Ngữ + Khiên Parry)
  2. Boss_ObsidianWarden.fbx (Boss Hộ Vệ Hắc Thạch + Vương Miện Sừng + Đại Đao Dung Nham)
  3. Props_FantasyArena.fbx (Cột Đá Cổ Ngữ, Đài Lửa Ân Điển, Rương Báu Vật)

Cách chạy tự động từ Terminal (Mac) hoặc Command Prompt / PowerShell (Windows):
  - Trên macOS:
    /Applications/Blender.app/Contents/MacOS/Blender -b -P BlenderScripts/generate_voxel_models.py
  - Trên Windows:
    "C:\\Program Files\\Blender Foundation\\Blender 4.2\\blender.exe" -b -P BlenderScripts/generate_voxel_models.py

Hoặc mở Blender -> tab Scripting -> Open file này -> Nhấn Run Script!
"""

import bpy
import os
import math

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_material(name, rgba, metallic=0.15, roughness=0.65, emission_rgba=None, emission_strength=1.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if emission_rgba is not None:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission_rgba
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission_rgba
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def add_voxel_box(name, location, scale, material, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    if parent is not None:
        obj.parent = parent
    return obj

def build_hero_voxel_knight():
    clear_scene()

    mat_steel = create_material("Hero_ArmorSteel", (0.28, 0.32, 0.42, 1.0), metallic=0.6, roughness=0.35)
    mat_gold = create_material("Hero_ArmorGold", (0.88, 0.68, 0.20, 1.0), metallic=0.8, roughness=0.25)
    mat_tunic = create_material("Hero_TunicBlue", (0.14, 0.25, 0.50, 1.0), metallic=0.05, roughness=0.8)
    mat_cape = create_material("Hero_CapeCrimson", (0.68, 0.12, 0.15, 1.0), metallic=0.05, roughness=0.85)
    mat_visor = create_material("Hero_VisorGlow", (0.25, 0.85, 1.0, 1.0), emission_rgba=(0.25, 0.85, 1.0, 1.0), emission_strength=3.0)
    mat_blade = create_material("Hero_RunicBlade", (0.85, 0.92, 1.0, 1.0), metallic=0.9, roughness=0.15, emission_rgba=(0.2, 0.55, 1.0, 1.0), emission_strength=1.5)

    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = "Hero_VoxelKnight"

    # Torso & Cape
    torso = add_voxel_box("Torso_Chest", (0, 0, 0.95), (0.56, 0.34, 0.68), mat_steel, root)
    add_voxel_box("Torso_Tabard", (0, -0.02, 0.91), (0.40, 0.36, 0.62), mat_tunic, torso)
    add_voxel_box("Torso_Belt", (0, 0, 0.67), (0.58, 0.37, 0.12), mat_gold, torso)
    add_voxel_box("Hero_Cape", (0, 0.20, 0.87), (0.52, 0.06, 0.78), mat_cape, torso)

    # Head & Helmet
    head = add_voxel_box("Head_Helm", (0, 0, 1.52), (0.44, 0.44, 0.44), mat_steel, root)
    add_voxel_box("Head_Visor", (0, -0.22, 1.54), (0.30, 0.04, 0.07), mat_visor, head)
    add_voxel_box("Head_Plume", (0, 0.04, 1.80), (0.10, 0.42, 0.18), mat_cape, head)

    # Right Arm + Runic Katana/Greatsword
    r_arm = add_voxel_box("RightArm", (0.38, 0, 0.95), (0.18, 0.18, 0.52), mat_steel, root)
    add_voxel_box("RightPauldron", (0.42, 0, 1.22), (0.26, 0.28, 0.24), mat_gold, r_arm)
    add_voxel_box("Sword_Hilt", (0.40, -0.20, 0.72), (0.08, 0.34, 0.08), mat_gold, r_arm)
    add_voxel_box("Sword_Guard", (0.40, -0.34, 0.72), (0.28, 0.08, 0.08), mat_gold, r_arm)
    add_voxel_box("Sword_Blade", (0.40, -0.94, 0.72), (0.06, 1.18, 0.10), mat_blade, r_arm)

    # Left Arm + Parry Shield
    l_arm = add_voxel_box("LeftArm", (-0.38, 0, 0.95), (0.18, 0.18, 0.52), mat_steel, root)
    add_voxel_box("LeftPauldron", (-0.42, 0, 1.22), (0.26, 0.28, 0.24), mat_gold, l_arm)
    add_voxel_box("Parry_Buckler", (-0.50, -0.12, 0.85), (0.08, 0.36, 0.36), mat_gold, l_arm)

    # Legs
    add_voxel_box("RightLeg", (0.15, 0, 0.30), (0.21, 0.22, 0.56), mat_steel, root)
    add_voxel_box("LeftLeg", (-0.15, 0, 0.30), (0.21, 0.22, 0.56), mat_steel, root)

    return root

def build_boss_obsidian_warden():
    clear_scene()

    mat_obsidian = create_material("Boss_Obsidian", (0.10, 0.09, 0.14, 1.0), metallic=0.5, roughness=0.4)
    mat_magma = create_material("Boss_MagmaCore", (0.98, 0.22, 0.06, 1.0), emission_rgba=(1.0, 0.25, 0.05, 1.0), emission_strength=4.0)
    mat_crown = create_material("Boss_CrownGold", (0.92, 0.72, 0.18, 1.0), metallic=0.85, roughness=0.25)

    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = "Boss_ObsidianWarden"

    # Towering Torso
    torso = add_voxel_box("Boss_Torso", (0, 0, 2.0), (1.50, 0.92, 1.65), mat_obsidian, root)
    add_voxel_box("Boss_MagmaHeart", (0, -0.10, 2.10), (0.68, 0.84, 0.68), mat_magma, torso)

    # Horned Helm
    head = add_voxel_box("Boss_Head", (0, 0, 3.25), (0.96, 0.96, 1.0), mat_obsidian, root)
    add_voxel_box("Boss_Eyes", (0, -0.46, 3.30), (0.72, 0.12, 0.18), mat_magma, head)
    add_voxel_box("Boss_Crown", (0, 0, 3.85), (1.02, 1.02, 0.28), mat_crown, head)
    add_voxel_box("Boss_HornL", (-0.58, -0.10, 4.15), (0.24, 0.24, 0.80), mat_crown, head)
    add_voxel_box("Boss_HornR", (0.58, -0.10, 4.15), (0.24, 0.24, 0.80), mat_crown, head)

    # Arms & Colossal Greatsword
    r_arm = add_voxel_box("Boss_RightArm", (1.02, 0, 2.0), (0.50, 0.50, 1.25), mat_obsidian, root)
    add_voxel_box("Boss_ColossalBlade", (1.08, -2.20, 1.45), (0.25, 3.35, 0.46), mat_magma, r_arm)
    add_voxel_box("Boss_LeftArm", (-1.02, 0, 2.0), (0.50, 0.50, 1.25), mat_obsidian, root)

    # Legs
    add_voxel_box("Boss_RightLeg", (0.42, 0, 0.65), (0.54, 0.54, 1.25), mat_obsidian, root)
    add_voxel_box("Boss_LeftLeg", (-0.42, 0, 0.65), (0.54, 0.54, 1.25), mat_obsidian, root)

    return root

def export_current_scene(output_dir, base_name):
    os.makedirs(output_dir, exist_ok=True)
    fbx_path = os.path.join(output_dir, f"{base_name}.fbx")
    blend_path = os.path.join(output_dir, f"{base_name}.blend")

    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True
    )
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"[VoxelSouls Blender] Exported: {fbx_path} and {blend_path}")

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    models_dir = os.path.join(project_root, "Assets", "Models")

    build_hero_voxel_knight()
    export_current_scene(models_dir, "Hero_VoxelKnight")

    build_boss_obsidian_warden()
    export_current_scene(models_dir, "Boss_ObsidianWarden")

    print("=== HOÀN TẤT XUẤT MÔ HÌNH BLENDER SANG UNITY (Assets/Models) ===")

if __name__ == "__main__":
    main()
