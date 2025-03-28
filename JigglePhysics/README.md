# How to use (WIP)
(A better/more detailed guide will be coming in the future)

For now though, here's a "quick and dirty" version of the instructions:

### Requirements:
- You have the blender file for the model you want to add bust physics to
- Your armature is not drastically different from Zephia's. Specifically, you need a "c_spine2_jnt" bone in the spine and it should be the immediate parent of "l_bust_jnt" and "r_bust_jnt" (the two breast bones)
- You have some weights on l_bust_jnt and r_bust_jnt (Zephia's default weights work great)

### Instructions:
1. Grab the files in this folder (https://github.com/LevnLime/EngageTools/tree/master/JigglePhysics)
2. In BLENDER, in your project with your model, import "BaseBodyF.fbx"
3. SELECT the armature of your model, and run AttachJiggleBones.py. This should attach the bones from my "SpringBones" armature onto your model's armature. You can delete the rest of the stuff that came from BaseBodyF.fbx now
4. SELECT the mesh you want to add jiggle bone weights for, and then run AddJiggleBoneWeight.py. You will likely want to do this for both your skin mesh and your dress mesh. This script will add weight to the new jiggle bones that the previous step added. It will add weight equal to whatever weight you already had on l_bust_jnt / r_bust_jnt
5. Export your model as usual. Personally, I uncheck Armature -> Add Leaf Bones in the export options, but I'm not sure if that's required
6. In UNITY, do your normal steps, but then also use "Mass CSV Spring Bone Import" to import spring bone information from one of the three "presets" I provided in the SpringBonesDynamicsPresets folder. Choose based on how much jiggle you want.
7. Build like normal and try it!