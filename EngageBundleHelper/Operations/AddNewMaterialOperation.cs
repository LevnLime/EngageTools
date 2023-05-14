using AssetsTools.NET.Extra;
using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Operations
{
	public record AddNewMaterialOperationParams
	{
		public required string BundleFileName { get; init; }
		public required string NewMaterialFileName { get; init; }
		public string AlbedoTextureJsonFileName { get; init; } = string.Empty;
		public string NormalTextureJsonFileName { get; init; } = string.Empty;
		public string MultiTextureJsonFileName { get; init; } = string.Empty;
		public string BasePath { get; init; } = string.Empty;
		public string OutputBundleFileName { get; init; } = "output.bundle";
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
			addNewMaterialAndTextures(
				Path.Combine(parameters.BasePath, parameters.BundleFileName),
				Path.Combine(parameters.BasePath, parameters.NewMaterialFileName),
				Path.Combine(parameters.BasePath, parameters.AlbedoTextureJsonFileName),
				Path.Combine(parameters.BasePath, parameters.NormalTextureJsonFileName),
				Path.Combine(parameters.BasePath, parameters.MultiTextureJsonFileName),
				Path.Combine(parameters.BasePath, parameters.OutputBundleFileName)
			);
		}

		void addNewMaterialAndTextures(
			string bundleFileName,
			string newMaterialFileName,
			string albedoTextureJsonFile,
			string normalTextureJsonFile,
			string multiTextureJsonFile,
			string outputBundleFileName
		)
		{
			AssetsManager assetsManager = new AssetsManager();
			BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
			AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);

			List<AssetsReplacer> assetsReplacers = new List<AssetsReplacer>();

			// We need to get a pointer to the Shader to reuse
			// From very limited testing, it looks like MtSkin and MtDress both use the same Shader, but MtShadow uses a different shader. We want to use the one from MtSkin/MtDress
			AssetFileInfo? materialInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "MtDress", AssetClassID.Material);
			if (materialInfo == null)
			{
				throw new Exception("Unable to find original MtDress material");
			}
			AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, materialInfo);
			AssetTypeValueField shaderElem = rootNode["m_Shader"];
			int shaderFileId = shaderElem.Children[0].AsInt;
			long shaderPathId = shaderElem.Children[0].AsLong;

			// Add all three textures files
			Dictionary<string, long> newTexturePathIds;
			List<AssetsReplacer> textureReplacers = addTextureAssets(assetsManager, assetsFileInst, albedoTextureJsonFile, normalTextureJsonFile, multiTextureJsonFile, out newTexturePathIds);
			assetsReplacers.AddRange(textureReplacers);

			// Modify the material JSON file to enter in the Shader and texture paths values
			// Create a new Material asset to import into
			string fixedNewMaterialTempFileName = writeMaterialInfoToFile(newMaterialFileName, shaderElem, newTexturePathIds);
			AssetsReplacer newMaterialReplacer = addTextureAsset(assetsManager, assetsFileInst, materialInfo, fixedNewMaterialTempFileName, out long newMaterialPathId);
			assetsReplacers.Add(newMaterialReplacer);

			// Make the Skin mesh SkinnedMeshRenderer understand the new material we just added
			AssetFileInfo? skinMeshInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "_Skin", AssetClassID.Mesh);
			long skinMeshPathId = skinMeshInfo.PathId;
			AssetsReplacer skinnedMeshRendererReplacer = addMaterialToSkinnedMeshRenderer(assetsManager, assetsFileInst, skinMeshPathId, newMaterialPathId);
			assetsReplacers.Add(skinnedMeshRendererReplacer);

			// Save the changes into the bundle
			List<BundleReplacer> bundleReplacers = new List<BundleReplacer>
			{
				new BundleReplacerFromAssets(assetsFileInst.name, null, assetsFileInst.file, assetsReplacers)
			};

			using (AssetsFileWriter writer = new AssetsFileWriter(outputBundleFileName))
			{
				bundleInst.file.Write(writer, bundleReplacers);
			}

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
		string writeMaterialInfoToFile(string fileName, AssetTypeValueField shaderElem, Dictionary<string, long> newTexturePathIds)
		{
			string fileContents = File.ReadAllText(fileName);
			JToken rootToken = JToken.Parse(fileContents);

			// Point the Shader at the existing one
			rootToken["m_Shader"]["m_FileID"] = shaderElem.Children[0].AsInt;
			rootToken["m_Shader"]["m_PathID"] = shaderElem.Children[1].AsLong;

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
			}

			string outputFileName = fileName + ".mod";
			File.WriteAllText(outputFileName, rootToken.ToString());
			return outputFileName;
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
	}
}
