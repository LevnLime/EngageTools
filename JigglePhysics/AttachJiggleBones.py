import bpy

# This script attaches the jiggle bones from the Base Body onto armature of the active object

jiggleBonesArmatureName = "SpringBones"
jiggleBonesTargetParentBoneName = "c_spine2_jnt"  # AFTER joining to the target armature, this will be the parent bone of the jiggle bones

# The target object is the active object
# We want the armature of the target object. Allow the user to select either the armature, the root object, or a mesh inside the armature
target_object = bpy.context.active_object
if target_object.type == 'ARMATURE':
    target_armature = target_object
elif target_object.type == 'MESH':
    target_armature = target_object.find_armature()
elif target_object.type == 'EMPTY' and len(target_object.children) > 0 and target_object.children[0].type == 'ARMATURE':  # root object
    target_armature = target_object.children[0]
else:
    raise ValueError("The active object is not a valid armature, mesh, or root object with an armature child.")

# Get the source armature. This is the one that has the jiggle bones
# Note: We are searching bpy.data.objects instead of bpy.data.armatures because we want the "Object" type object so that we can select it
#   bpy.data.armatures returns "Armature" type objects, which is equivalent to the "obj.data" property of the "Object" type object
indexOfSourceArmature = bpy.data.objects.find(jiggleBonesArmatureName)
if indexOfSourceArmature == -1:
    raise ValueError(f"Armature '{jiggleBonesArmatureName}' not found in the current blend file.")
source_armature = bpy.data.objects[indexOfSourceArmature]

# Joining armatures has been shown to mess up bone roll values, so save the bone roll values of the source armature to be restored later
# Note: We must be in edit mode on the source armature to access the bone properties
#   We have to deslect everything first in case there's an object selected that can't enter edit mode (such as the root object of the target armature)
bpy.ops.object.select_all(action='DESELECT')    # Deselect all objects
bpy.context.view_layer.objects.active = source_armature    # Set the source armature as the active object so that we can enter edit mode on it
bpy.ops.object.mode_set(mode='EDIT')    # Switch to edit mode to access bone properties
source_bone_rolls = {}
for bone in source_armature.data.edit_bones:
    source_bone_rolls[bone.name] = bone.roll

# Also save the name of the root jiggle bone. This should be the first bone in the list of bones in the source armature
rootJiggleBoneName = source_armature.data.bones[0].name

# Join the jiggle bones armature into the target armature
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')    # Deselect all objects
target_armature.select_set(True)    # Select the target armature
source_armature.select_set(True)    # Select the source armature
bpy.context.view_layer.objects.active = target_armature    # Set the target armature as the active object
bpy.ops.object.join()    # Join the two armatures

# Fix the bone rolls of the jiggle bones
bpy.ops.object.mode_set(mode='EDIT')    # Switch to edit mode to access bone properties
for boneName in source_bone_rolls.keys():
    target_armature.data.edit_bones[boneName].roll = source_bone_rolls[boneName]    # Set the roll value to the saved value

# Set the parent of the jiggle bones to the target parent bone
# Note: Still needs to be done in Edit mode
target_armature.data.edit_bones[rootJiggleBoneName].parent = target_armature.data.edit_bones[jiggleBonesTargetParentBoneName]

# Return to object mode
bpy.ops.object.mode_set(mode='OBJECT')