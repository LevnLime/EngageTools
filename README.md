# EngageTools - EngageBundleHelper
This is a tool I created to help with doing mesh modding for Fire Emblem Engage.

# Background
A rough overview of making a mesh edit to Fire Emblem Engage involves the following:
1) Find game bundle that contains the mesh you're interested in and export it (as FBX)
2) (The fun part!) Use Blender (or other 3D model software) to edit your mesh and export the edited mesh (as FBX)
3) Use Unity to build your mesh into a new Unity assets file
4) Bring your edited mesh (from the new Unity assets file) back into the original game bundle
5) (optional) If your mesh editing involved bringing in mesh from another bundle, you may need to add new Material assets. This is pretty tedious and time consuming to do manually because you need to make sure various pointers are pointing to the right assets.

My tool is meant to help with steps 4 and 5 above. Before this tool, steps 4 and 5 (especially 5) used to take me maybe about an hour (or more) each time and it felt very slow and tedious. With the tool, I can now do steps 4 and 5 in a few seconds! :)

# Usage
```
EngageBundleHelper.exe -c <JSON config file>
```
For debugging purposes, you can add the verbse (-v) flag
```
EngageBundleHelper.exe -c <JSON config file> -v
```
The Config File is a JSON file with an Operation name and that operation's parameters. Operations are listed in the section below  

(Explanations are hard, so the easiest way to get started may be to go to the Quick Start section, which uses the SampleFiles that I've provided to run the basic (and hopefully most common) scenario.)

# Operations
These are the operations that the tool currently supports
## UpdateMeshesFromNewAssets
This operation essentially automates doing Step 4 (from the Background section) - Bring your edited mesh (from the new Unity assets file) back into the original game bundle.  
It will first export the meshes you indicate from the new Unity assets file (unity assets file named "sharedassets0.assets" by default) into temporary JSON files before importing those into the bundle with the original mesh.  
It also has a minor feature where (while the mesh is still in the temporary JSON file state), it will fix up the name and bone name hashes to match those from the original mesh.

Parameter | Description
--- | ---
BundleFileName | This is name (or path) to the bundle with the original mesh that you want to import your updated meshes into
NewAssetsFileName | This is the name (or path) of the built Unity assets file containing your updated mesh. Unity's default name for this seems to be "sharedassets0.assets".
NewAssetsnameSearchTerm | This tells the program which meshes to export. It searches by name and your search term just has to appear anywhere in the asset name. A certain guide I followed recommended naming all your updated meshes in Unity to have "_FIXED" appended to the name. Following that guidance, you would use "_FIXED" as the NewAssetsnameSearchTerm and this program will export all those meshes
MeshesToUpdate | This is an array of which meshes in the original bundle to update. Again, this matching will be done by asset name and your name just has to appear as a substring of the full mesh name
BasePath | (optional) This specifies a base path that relative path names (such as BundleFileName and NewAssetsFileName) will be based off of. By default, BasePath is just the current directory
OutputBundleFileName | The name of the bundle this operation outputs. Currently, this tool does not support updating the original bundle in place.

## AddNewMaterial
This operation essentially automates doing Step 5 (from the Background section) - bringing in a Material (that you used from a different bundle) and attaching the new material to a mesh.  
Please see the Quick Start example for a concrete example of when you might need this.  
This operation first requires you to export a lot of the data from the material you want to import into JSON files. The operation then will:
1) Create a new Material asset for your new material
2) Create new Texture2D assets for each texture type that you want to import (Albedo, Normal, and/or Multi)
3) Point your new Material's Albedo/Normal/Multi texture pointer to your new corresponding Texture2D asset (if you added a new one) or to the appropriate existing Texture2D (if you did not add a new one)
4) Find your mesh's SkinnedMeshRenderer component and add a new entry to its m_Materials array, pointing that new entry at the new Material asset it just added
5) For any Texture2D texture that you are adding, upload the actual texture image file into it.

Parameter | Description
--- | ---
BundleFileName | The name of the bundle with the mesh you want to add the new material (and textures) to
MeshName | The name of the mesh you are adding this material for. (Note: this is a substring search)
NewMaterialFileName | File name (or path) of the JSON file for the new Material. Use UABE to export the material from the other bundle into JSON
AlbedoTextureJsonFileName | File name (or path) of the JSON file of the Albedo texture. Omit this if it's okay for your new material to reuse the existing texture already in the bundle
NormalTextureJsonFileName | File name (or path) of the JSON file of the Normal texture. Omit this if it's okay for your new material to reuse the existing texture already in the bundle
MultiTextureJsonFileName | File name (or path) of the JSON file of the Multi texture. Omit this if it's okay for your new material to reuse the existing texture already in the bundle
AlbedoTextureImageFileName | File name (or path) of the image file (likely PNG) of the Albedo texture
NormalTextureImageFileName | File name (or path) of the image file (likely PNG) of the Normal texture
MultiTextureImageFileName | File name (or path) of the image file (likely PNG) of the Multi texture
BasePath | (optional) This specifies a base path that relative path names (such as BundleFileName and all the texture file names) will be based off of. By default, BasePath is just the current directory
OutputBundleFileName | The name of the bundle this operation outputs. Currently, this tool does not support updating the original bundle in place.

# Quick Start
I've provided a folder of SampleFiles that should help in completing the basic (and I'm hoping most common) scenario.
### Example of basic scenario
Suppose that you are trying to create your own [Lapis Midriff and Thighs](https://gamebanana.com/mods/423893) mod (not my mod, but I think it makes for a good tutorial).
- You've already done Step 1 and exported Lapis's Dress and Skin meshes from Lapis's bundle (ubody_swd0af_c251.bundle)
- You've already done Step 2 and used Blender to modify these meshes
  - The Dress mesh was easy because you're just removing existing faces
  - The Skin mesh, however, is not as straightforward. Lapis's Skin mesh just gives you her neck, so you'll need to get a midriff and thighs from somewhere else.
    - A common source for female body parts is Zephia's Skin mesh (ubody_msn0df_c553.bundle) because it is the most complete. So you take a midriff and thighs from Zephia
    - Now Lapis's Skin mesh has a midriff and thighs (in addition to the neck). Notice that the neck uses Lapis's original MtSkin material, but the midriff and thighs from Zephia uses a different MtSkin.001 material
- You've already done Step 3 and had Unity build your updated mesh into a new Unity assets file (sharedassets0.assets)
  - Important! You must append "_FIXED" to the names of your generated Skin and Dress meshes. (EngageBundleHelper will search for assets by partial name)
  - In Unity, you may notice that it will say your Skin mesh has two "submeshes" (corresponding to the two Materials that that mesh uses)
### EngageBundleHelper Usage for this example basic scenario
1) Navigate to the SampleFiles folder that I've provided in command prompt.
2) Copy Lapis's bundle (ubody_swd0af_c251.bundle) into this folder
3) Copy the built Unity assets file (sharedassets0.assets) into this folder
4) Run `EngageBundleHelper.exe -c UpdateMeshesFromNewAssets.json`
    - This will output a file named "updatedMeshesOutput.bundle"
    - Note: If you get a message about "command not found", either provide a full absolute (or relative) path to the folder where EngageBundleHelper.exe resides, or add that folder to your PATH environment variable
5) Run `EngageBundleHelper.exe -c AddNewMaterial.json`
    - This will output a file named "output.bundle"
6) Rename "output.bundle" to "ubody_swd0af_c251.bundle" and use that for your mod. You're good to go!

### Description of SampleFiles files
- The MtSkin_Material.json is the MtSkin material exported from Zephia's bundle (ubody_msn0df_c553.bundle)
- The Albedo/Normal/Multi *_Texture.json files are exported (export as JSON) from Zephia's bundle (ubody_msn0df_c553.bundle)
- The Albedo/Normal/Multi *_Texture.png files were originally exported (export texture) from Zephia's bundle, but have been modified for easier reuse
- UpdateMeshesFromNewAssets.json is a config file for EngageBundlerHelper and is meant to be used first
- AddNewMaterial.json is a config file for EngageBundlerHelper and is meant to be used second
- classdata.tpk is needed by the UpdateMeshesFromNewAssets operation. (I couldn't figure out how to get the program to find the file more reliably, so I just stuck it in here)

# Libraries and References
- [AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET) - The base library for interacting with Unity bundles. I'm using the v3 version.
- [UABE Avalonia](https://github.com/nesrak1/UABEA) - The UI tool that I'm trying to automate some tasks for. I actually call into UABEAvalonia.dll directly on some occasions

- [GameBanana Model Replacement Guide](https://gamebanana.com/tuts/15746) - This is the first model replacement guide I used and tried to make sense of. (Note: this one doesn't discuss how to manually add a new material to a mesh)

