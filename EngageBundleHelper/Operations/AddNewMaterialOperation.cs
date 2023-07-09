using AssetsTools.NET.Extra;
using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetsTools.NET.Texture;

namespace EngageBundleHelper.Operations
{
	public record AddNewMaterialOperationParams
	{
		public required string BundleFileName { get; init; }
		/// <summary>
		/// The name of the Mesh to add this new material to
		/// </summary>
		public required string MeshName { get; init; }
		public required string NewMaterialFileName { get; init; }
		public string AlbedoTextureJsonFileName { get; init; } = string.Empty;
		public string AlbedoTextureImageFileName { get; init;} = string.Empty;
		public string NormalTextureJsonFileName { get; init; } = string.Empty;
		public string NormalTextureImageFileName { get; init; } = string.Empty;
		public string MultiTextureJsonFileName { get; init; } = string.Empty;
		public string MultiTextureImageFileName { get; init; } = string.Empty;
		public string BasePath { get; init; } = string.Empty;
		public string OutputBundleFileName { get; init; } = "output.bundle";
		/// <summary>
		///  This is the material that will be used to copy some data from when creating the new material. Specifically, we will be copying the following:
		///    1) pointer to the Shader
		///    2) pointer to the _ToonRamp texture
		///  Notes:
		///  From very limited testing, it looks like MtSkin and MtDress both use the same Shader, but MtShadow uses a different shader.
		///  We generally want to use the one from MtSkin/MtDress.
		///  From testing, it has also been observed that MtDress and MtSkin may use a different _ToonRamp texture
		/// </summary>
		public string SourceMaterialName { get; init; } = "MtDress";
	}
	public class AddNewMaterialOperation
	{
		private AddNewMaterialOperationParams parameters;

		public AddNewMaterialOperation(AddNewMaterialOperationParams parameters)
		{
			this.parameters = parameters;
		}

		public void Execute()
		{
			// Temporary bundle is needed because I'm doing this in two passes. I haven't figured out how to do this in one pass...
			string tempBundlePath = Path.Combine(parameters.BasePath, parameters.BundleFileName + "_TEMP_DELETE_ME");

			// First pass adds the new material, the texture JSON assets, and connects them. It does not import the actual texture images yet.
			Dictionary<string, long> newTexturePathIds = addNewMaterialAndTextureJson(
				Path.Combine(parameters.BasePath, parameters.BundleFileName),
				parameters.MeshName,
				Path.Combine(parameters.BasePath, parameters.NewMaterialFileName),
				Path.Combine(parameters.BasePath, parameters.AlbedoTextureJsonFileName),
				Path.Combine(parameters.BasePath, parameters.NormalTextureJsonFileName),
				Path.Combine(parameters.BasePath, parameters.MultiTextureJsonFileName),
				tempBundlePath,
				parameters.SourceMaterialName
			);
			Debug.WriteLine($"Done adding new material and textures (JSON only) to bundle (and adjusting pointers).");

			// Second pass imports the texture images for the new textures
			importTextureImagesToBundle(
				tempBundlePath,
				newTexturePathIds,
				Path.Combine(parameters.BasePath, parameters.AlbedoTextureImageFileName),
				Path.Combine(parameters.BasePath, parameters.NormalTextureImageFileName),
				Path.Combine(parameters.BasePath, parameters.MultiTextureImageFileName),
				Path.Combine(parameters.BasePath, parameters.OutputBundleFileName)
			);
			Debug.WriteLine("Done importing texture images into the texture assets");

			// Cleanup. Delete that temporary file
			File.Delete(tempBundlePath);
		}

		Dictionary<string, long> addNewMaterialAndTextureJson(
			string bundleFileName,
			string meshName,
			string newMaterialFileName,
			string albedoTextureJsonFile,
			string normalTextureJsonFile,
			string multiTextureJsonFile,
			string outputBundleFileName,
			string shaderSourceMaterialName
		)
		{
			AssetsManager assetsManager = new AssetsManager();
			BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
			AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);

			List<AssetsReplacer> assetsReplacers = new List<AssetsReplacer>();

			// We need to get a pointer to an existing Shader to reuse
			AssetFileInfo? materialInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, shaderSourceMaterialName, AssetClassID.Material);
			if (materialInfo == null)
			{
				throw new Exception($"Unable to find original {shaderSourceMaterialName} material to get the shader from");
			}
			AssetTypeValueField materialBaseField = assetsManager.GetBaseField(assetsFileInst, materialInfo);
			AssetTypeValueField shaderPointer = materialBaseField["m_Shader"];

			// Add all three textures files
			Dictionary<string, long> newTexturePathIds;
			List<AssetsReplacer> textureReplacers = addTextureAssets(assetsManager, assetsFileInst, albedoTextureJsonFile, normalTextureJsonFile, multiTextureJsonFile, out newTexturePathIds);
			assetsReplacers.AddRange(textureReplacers);

			// There's actually one more important texture -- _ToonRamp
			// This one is different from the others in that it does not live in this bundle. I think that most models share from a set of common _ToonRamp textures
			// The actual location these _ToonRamp textures lives is somewhere in Data\StreamingAssets\aa\Switch\fe_assets_unit\model\common
			// But bundles will just use a pointer to that. The PathID of the certain _ToonRamp textures are static. For example MtSkin should be -914440881901598052
			// However, the FileID may differ from bundle to bundle because the order of the dependencies may be different.
			// Therefore, similar to how we found the existing Shader to reuse, we need to also find the existing _ToonRamp pointer and reuse that
			AssetTypeValueField toonRampPointer = getToonRampPointerFromMaterial(materialBaseField);

			// Modify the material JSON file to enter in the Shader and texture paths values
			// Create a new Material asset to import into
			string fixedNewMaterialTempFileName = writeMaterialInfoToFile(newMaterialFileName, shaderPointer, newTexturePathIds, toonRampPointer);
			AssetsReplacer newMaterialReplacer = addTextureAsset(assetsManager, assetsFileInst, materialInfo, fixedNewMaterialTempFileName, out long newMaterialPathId);
			assetsReplacers.Add(newMaterialReplacer);
			Debug.WriteLine("Created new Material asset");

			// Make the Skin mesh SkinnedMeshRenderer understand the new material we just added
			AssetFileInfo? meshInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, meshName, AssetClassID.Mesh);
			if (meshInfo == null)
			{
				throw new Exception($"Unable to find mesh \"{meshName}\" to attach material to");
			}
			long meshPathId = meshInfo.PathId;
			AssetsReplacer skinnedMeshRendererReplacer = addMaterialToSkinnedMeshRenderer(assetsManager, assetsFileInst, meshPathId, newMaterialPathId);
			assetsReplacers.Add(skinnedMeshRendererReplacer);
			Debug.WriteLine($"Added new material to {meshName} mesh's SkinnedMeshRenderer's Materials list");

			// Save the changes into the bundle
			List<BundleReplacer> bundleReplacers = new List<BundleReplacer>
			{
				new BundleReplacerFromAssets(assetsFileInst.name, null, assetsFileInst.file, assetsReplacers)
			};

			using (AssetsFileWriter writer = new AssetsFileWriter(outputBundleFileName))
			{
				bundleInst.file.Write(writer, bundleReplacers);
			}

			// Cleanup
			File.Delete(fixedNewMaterialTempFileName);
			assetsManager.UnloadAll();

			return newTexturePathIds;
		}

		void importTextureImagesToBundle(
			string bundleFileName,
			Dictionary<string, long> texturePathIds,
			string albedoTextureImageFile,
			string normalTextureImageFile,
			string multiTextureImageFile,
			string outputBundleFileName
		)
		{
			AssetsManager assetsManager = new AssetsManager();
			BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
			AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);
			
			List<AssetsReplacer> assetsReplacers = new List<AssetsReplacer>();

			// Iterate through the new textures that were created and import their image files
			foreach (var entry in texturePathIds)
			{
				string textureFilePath;
				string textureName;
				switch (entry.Key)
				{
					case "_BaseMap":
						textureFilePath = albedoTextureImageFile;
						textureName = "Albedo";
						break;
					case "_BumpMap":
						textureFilePath = normalTextureImageFile;
						textureName = "Normal";
						break;
					case "_MultiMap":
						textureFilePath = multiTextureImageFile;
						textureName = "Multi";
						break;
					default: throw new Exception("Unexpected entry in texturePathIds: " + entry.Key);	// This shouldn't happen
				}
				long pathId = entry.Value;

				if (File.Exists(textureFilePath))
				{
					AssetFileInfo textureAssetInfo = assetsFileInst.file.GetAssetInfo(pathId);
					AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, textureAssetInfo);
					importTextureImage(assetsFileInst, rootNode, textureFilePath);
					assetsReplacers.Add(new AssetsReplacerFromMemory(assetsFileInst.file, textureAssetInfo, rootNode));
					Debug.WriteLine($"Successfully imported \"{textureFilePath}\" into {textureName} Texture2D asset");
				}
				else
				{
					// This means that we added a new Texture2D asset (the JSON file) to the bundle, but do not actually have the texture image
					// This will probably make that material behave unexpectedly
					Console.WriteLine($"Warning! {textureName} texture image not found! No image imported into this texture. Path searched: {textureFilePath}");
				}
			}

			// Save the changes into the bundle
			List<BundleReplacer> bundleReplacers = new List<BundleReplacer>
			{
				new BundleReplacerFromAssets(assetsFileInst.name, null, assetsFileInst.file, assetsReplacers)
			};

			using (AssetsFileWriter writer = new AssetsFileWriter(outputBundleFileName))
			{
				bundleInst.file.Write(writer, bundleReplacers);
			}

			assetsManager.UnloadAll();
		}

		List<AssetsReplacer> addTextureAssets(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, string albedoTextureJsonFile, string normalTextureJsonFile, string multiTextureJsonFile, out Dictionary<string, long> pathIds)
		{
			// Get existing texture assets
			AssetFileInfo? albedoTextureInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "_Albedo", AssetClassID.Texture2D);
			AssetFileInfo? normalTextureInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "_Normal", AssetClassID.Texture2D);
			AssetFileInfo? multiTextureInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "_Multi", AssetClassID.Texture2D);

			if (albedoTextureInfo == null || normalTextureInfo == null || multiTextureInfo == null)
			{
				throw new Exception("Can't find existing textures (Albedo, Normal, or Multi)");
			}

			List<AssetsReplacer> replacers = new List<AssetsReplacer>();
			pathIds = new Dictionary<string, long>();

			// Albedo texture correlates to the "_BaseMap" material property
			if (File.Exists(albedoTextureJsonFile))
			{
				long pathId;
				replacers.Add(addTextureAsset(assetsManager, assetsFileInst, albedoTextureInfo, albedoTextureJsonFile, out pathId));
				pathIds.Add("_BaseMap", pathId);
				Debug.WriteLine("Added new Texture2D asset for Albedo texture.");
			}
			else
			{
				// Use the pathId from the existing texture
				pathIds.Add("_BaseMap", albedoTextureInfo.PathId);
			}

			// Normal texture correlates to the "_BumpMap" material property
			if (File.Exists(normalTextureJsonFile))
			{
				long pathId;
				replacers.Add(addTextureAsset(assetsManager, assetsFileInst, normalTextureInfo, normalTextureJsonFile, out pathId));
				pathIds.Add("_BumpMap", pathId);
				Debug.WriteLine("Added new Texture2D asset for Normal texture.");
			}
			else
			{
				// Use the pathId from the existing texture
				pathIds.Add("_BumpMap", normalTextureInfo.PathId);
			}

			// Multi texture correlates to the "_MultiMap" material property
			if (File.Exists(multiTextureJsonFile))
			{
				long pathId;
				replacers.Add(addTextureAsset(assetsManager, assetsFileInst, multiTextureInfo, multiTextureJsonFile, out pathId));
				pathIds.Add("_MultiMap", pathId);
				Debug.WriteLine("Added new Texture2D asset for Multi texture.");
			}
			else
			{
				// Use the pathId from the existing texture
				pathIds.Add("_MultiMap", multiTextureInfo.PathId);
			}

			return replacers;
		}

		// TODO: Rename this as this function can add more than just texture assets
		AssetsReplacer addTextureAsset(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetFileInfo existingTextureInfo, string textureAssetJsonFile, out long pathId)
		{
			byte[] bytes = UABEHelper.ImportJsonAssetAsBytes(assetsManager, assetsFileInst, existingTextureInfo, textureAssetJsonFile);
			pathId = Helpers.getRandomNewPathId(assetsFileInst);
			return new AssetsReplacerFromMemory(pathId, existingTextureInfo.TypeId, 0xFFFF, bytes);
		}

		AssetsReplacer addNewAssetFromJson(AssetsFileInstance assetsFileInst, AssetTypeTemplateField templateField, AssetClassID typeId, string assetJsonFile, out long pathId)
		{
			byte[] bytes = UABEHelper.ImportJsonAssetAsBytes(templateField, assetJsonFile);
			pathId = Helpers.getRandomNewPathId(assetsFileInst);
			return new AssetsReplacerFromMemory(pathId, (int)typeId, 0xFFFF, bytes);
		}

		/*
		Sample m_SavedProperties:
		  "m_SavedProperties": {
			"m_TexEnvs": {
			  "Array": [
				{
				  "first": "_BaseMap",
				  "second": {
					"m_Texture": {
					  "m_FileID": 0,
					  "m_PathID": 1277293570236772672
					},
					"m_Scale": {
					  "x": 1.0,
					  "y": 1.0
					},
					"m_Offset": {
					  "x": 0.0,
					  "y": 0.0
					}
				  }
				},
		*/
		string writeMaterialInfoToFile(string fileName, AssetTypeValueField shaderPointer, Dictionary<string, long> newTexturePathIds, AssetTypeValueField toonRampPointer)
		{
			string fileContents = File.ReadAllText(fileName);
			JToken rootToken = JToken.Parse(fileContents);

			// Point the Shader at the existing one
			rootToken["m_Shader"]["m_FileID"] = shaderPointer.Children[0].AsInt;
			rootToken["m_Shader"]["m_PathID"] = shaderPointer.Children[1].AsLong;

			// Modify the texture pointers to point to the correct textures
			JToken texEnvsArray = rootToken["m_SavedProperties"]["m_TexEnvs"]["Array"];
			foreach (JToken textureInfo in texEnvsArray.Children())
			{
				string textureName = textureInfo["first"].ToString();

				// If this is one of the textures we need to write in a new path for, do it
				if (newTexturePathIds.ContainsKey(textureName))
				{
					long pathId = newTexturePathIds[textureName];
					textureInfo["second"]["m_Texture"]["m_PathID"] = pathId;
				}
				else if (textureName == "_ToonRamp")
				{
					// Point the ToonRamp texture at the existing one
					textureInfo["second"]["m_Texture"]["m_FileID"] = toonRampPointer["m_FileID"].AsInt;
					textureInfo["second"]["m_Texture"]["m_PathID"] = toonRampPointer["m_PathID"].AsLong;
				}
			}

			string outputFileName = fileName + ".mod";
			File.WriteAllText(outputFileName, rootToken.ToString());
			return outputFileName;
		}

		AssetTypeValueField getToonRampPointerFromMaterial(AssetTypeValueField materialBaseField)
		{
			AssetTypeValueField texturesArray = materialBaseField["m_SavedProperties"]["m_TexEnvs"]["Array"];
			AssetTypeValueField? toonRampEntry = texturesArray.Children.Find(textureInfo => textureInfo["first"].AsString == "_ToonRamp");
			if (toonRampEntry == null)
			{
				throw new Exception($"Unable to find _ToonRamp info for material {materialBaseField["m_Name"].AsString}");
			}
			return toonRampEntry["second"]["m_Texture"];
		}

		AssetsReplacer addMaterialToSkinnedMeshRenderer(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, long meshPathId, long newMaterialPathId)
		{
			// Find the appropriate SkinnedMeshRenderer that is used for the passed in Mesh
			List<AssetFileInfo> allSkinnedMeshRenderers = assetsFileInst.file.GetAssetsOfType(AssetClassID.SkinnedMeshRenderer);
			AssetFileInfo? skinnedMeshRendererInfo = allSkinnedMeshRenderers.Find((AssetFileInfo skinnedMeshRendererInfo) =>
			{
				// The correct SkinnedMeshRenderer is the one where the m_Mesh property points to the mesh we're interested in
				AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, skinnedMeshRendererInfo);
				long targetMeshPathId = rootNode["m_Mesh"]["m_PathID"].AsLong;
				return targetMeshPathId == meshPathId;
			});

			if (skinnedMeshRendererInfo == null)
			{
				throw new Exception($"Unable to find SkinnedMeshRenderer corresponding to Mesh at PathId {meshPathId}");
			}

			AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, skinnedMeshRendererInfo);
			AssetTypeValueField materialsArray = rootNode["m_Materials"]["Array"];
			AssetTypeValueField firstMaterial = materialsArray.Children[0];

			// Create a new entry for the new material and add it to the Materials array
			AssetTypeValueField newMaterial = ValueBuilder.DefaultValueFieldFromTemplate(firstMaterial.TemplateField);
			newMaterial["m_PathID"].AsLong = newMaterialPathId;
			materialsArray.Children.Add(newMaterial);

			return new AssetsReplacerFromMemory(assetsFileInst.file, skinnedMeshRendererInfo, rootNode);
		}

		void importTextureImage(AssetsFileInstance assetsFileInst, AssetTypeValueField rootNode, string textureFilePath)
		{
			TextureFormat format = (TextureFormat)rootNode["m_TextureFormat"].AsInt;
			byte[] platformBlob = UABEHelper.GetTexturePlatformBlob(rootNode);
			uint platform = assetsFileInst.file.Metadata.TargetPlatform;

			// I have no idea what mips or mipmaps are, but I can confirm that things look bad if this mips value is wrong
			// Originally, I just copied over the texture JSON's "m_MipCount" property. However, in game this looked really bad.
			// Importing the texture using UABE's UI fixed it, but the only difference I could tell is that UABE set mips to 1
			// Looking at UABE's Texture EditDialog code, I think it leaves mips as 1 if the texture JSON's "m_MipMap" is not true
			// I'll try to emulate that logic here (but once again, I have no idea what this actually is doing)
			int mips = 1;
			TextureFile assetToolsTextureFile = TextureFile.ReadTextureFile(rootNode);
			if (assetToolsTextureFile.m_MipMap)
			{
				mips = assetToolsTextureFile.m_MipCount;
			}


			byte[] encodedImageBytes = UABEHelper.ImportTexture(textureFilePath, format, out int width, out int height, ref mips, platform, platformBlob);

			AssetTypeValueField m_StreamData = rootNode["m_StreamData"];
			m_StreamData["offset"].AsInt = 0;
			m_StreamData["size"].AsInt = 0;
			m_StreamData["path"].AsString = "";

			if (!rootNode["m_MipCount"].IsDummy)
			{
				rootNode["m_MipCount"].AsInt = mips;
			}

			rootNode["m_TextureFormat"].AsInt = (int)format;
			rootNode["m_CompleteImageSize"].AsInt = encodedImageBytes.Length;
			rootNode["m_Width"].AsInt = width;
			rootNode["m_Height"].AsInt = height;

			AssetTypeValueField image_data = rootNode["image data"];
			image_data.Value.ValueType = AssetValueType.ByteArray;
			image_data.TemplateField.ValueType = AssetValueType.ByteArray;
			image_data.AsByteArray = encodedImageBytes;
		}
	}
}
