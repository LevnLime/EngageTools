import bpy

# This script sets up the jiggle bone weights for the bust jiggle bones
# Please run AttachJiggleBones.py first to attach the jiggle bones to the armature of the active object
# This script will weight the newly attached jiggle bones the same amount as the corresponding bust bones


# Copies the vertex group weights from one group to another vertex group (creating if necessary)
def copy_vertex_group_weights(source_group_name, target_group_name):
    # Get the active object (assumed to be a mesh)
    obj = bpy.context.active_object

    # Ensure the object is a mesh
    if obj.type != 'MESH':
        print("Active object is not a mesh.")
        return

    # Ensure the object has vertex groups
    if not obj.vertex_groups:
        print("No vertex groups found.")
        return

    # Get the source and target vertex groups
    source_group = obj.vertex_groups.get(source_group_name)
    target_group = obj.vertex_groups.get(target_group_name)

    # If the source group doesn't exist, print an error message and return
    if not source_group:
        print(f"Source vertex group '{source_group_name}' does not exist.")
        return

    # If the target group doesn't exist, create it
    if not target_group:
        target_group = obj.vertex_groups.new(name=target_group_name)

    # Copy weights from source to target group
    for v in obj.data.vertices:
        for g in v.groups:
            if g.group == source_group.index:
                target_group.add([v.index], g.weight, 'REPLACE')
                break

# Given a list of vertex groups, lock those vertex groups and normalize all weights of the rest
def lock_vertex_groups_and_normalize(vertex_groups_to_lock):
    # Get the active object (assumed to be a mesh)
    obj = bpy.context.active_object

    # Ensure the object is a mesh
    if obj.type != 'MESH':
        print("Active object is not a mesh.")
        return

    # Unlock all vertex groups first
    bpy.ops.object.vertex_group_lock(action='UNLOCK', mask='ALL')

    # Lock the specified vertex groups
    for group_name in vertex_groups_to_lock:
        group = obj.vertex_groups.get(group_name)
        if group:
            group.lock_weight = True
        else:
            print(f"Vertex group '{group_name}' does not exist.")

    # Normalize all other vertex groups
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='BONE_DEFORM', lock_active=False)

    # Unlock all vertex groups again
    bpy.ops.object.vertex_group_lock(action='UNLOCK', mask='ALL')

    # Normalize all vertex groups again after unlocking everything
    # This addresses the case where the locked vertex groups already had a combined weight of over 1.0
    # Example: l_bust_jnt and l_bust_jig both have weights of 0.7. The first normalization above would remove
    #   any weight from any of the unlocked vertex groups (like maybe c_spine2_jnt). This second normalization
    #   below would normalize l_bust_jnt and l_bust_jig to 0.5 each.
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='BONE_DEFORM', lock_active=False)

# Set up bust jiggle bone weights by copying the weights from the bust bone to the corresponding jiggle bone
# and then normalizing the weights of the rest of the vertex groups to take weight away from them.
def addJiggleBoneWeight(source_bones, target_bones):
    for source_bone, target_bone in zip(source_bones, target_bones):
        copy_vertex_group_weights(source_bone, target_bone)
    lock_vertex_groups_and_normalize(source_bones + target_bones)    # Lock both source and target bones


# "Main"
source_bones = [
    "l_bust_jnt",
    "r_bust_jnt",
]
target_bones = [
    "l_bust_jig",
    "r_bust_jig",
]
addJiggleBoneWeight(source_bones, target_bones)