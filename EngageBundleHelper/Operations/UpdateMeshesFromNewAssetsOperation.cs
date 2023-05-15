using AssetsTools.NET.Extra;
using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Linq;

namespace EngageBundleHelper.Operations
{
	public record UpdateMeshesFromNewAssetsOperationParams
	{
		public required string BundleFileName { get; init; }
		public required string NewAssetsFileName { get; init; }
		public required string NewAssetsNameSearchTerm { get; init; }
		public required IEnumerable<string> MeshesToUpdate { get; init;}
		public string BasePath { get; init; } = string.Empty;
		public string OutputBundleFileName { get; init; } = "output.bundle";
	}
	public class UpdateMeshesFromNewAssetsOperation
	{
		
		private UpdateMeshesFromNewAssetsOperationParams parameters;

		public UpdateMeshesFromNewAssetsOperation(UpdateMeshesFromNewAssetsOperationParams parameters)
		{
			this.parameters = parameters;
		}

		public void Execute()
		{
			Execute(
				Path.Combine(parameters.BasePath, parameters.BundleFileName),
				Path.Combine(parameters.BasePath, parameters.NewAssetsFileName),
				parameters.NewAssetsNameSearchTerm,
				parameters.MeshesToUpdate,
				parameters.BasePath,
				Path.Combine(parameters.BasePath, parameters.OutputBundleFileName)
			);
		}

		public static void Execute(string bundleFileName, string newAssetsFileName, string newAssetsNameSearchTerm, IEnumerable<string> meshesToUpdate, string basePath, string outputBundleFileName)
		{
			AssetsManager assetsManager = new AssetsManager();
			BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
			AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);

			List<AssetsReplacer> assetsReplacers = new List<AssetsReplacer>();

			// Export assets from the new assets file into JSON
			List<string> exportedJsonFileNames = exportAssetsToJson(newAssetsFileName, newAssetsNameSearchTerm, AssetClassID.Mesh, basePath);

			// Import those new assets back into the original bundle
			foreach (string meshToUpdate in meshesToUpdate)
			{
				string? exportedJsonFileName = exportedJsonFileNames.Find(jsonFileName => jsonFileName.Contains(meshToUpdate));
				if (!string.IsNullOrEmpty(exportedJsonFileName))
				{
					AssetsReplacer? replacer = updateExistingMesh(assetsManager, assetsFileInst, meshToUpdate, exportedJsonFileName, false /*exactMatch*/);
					if (replacer != null)
					{
						assetsReplacers.Add(replacer);
					}
				}
			}

			List<BundleReplacer> bundleReplacers = new List<BundleReplacer>
			{
				new BundleReplacerFromAssets(assetsFileInst.name, null, assetsFileInst.file, assetsReplacers)
			};

			using (AssetsFileWriter writer = new AssetsFileWriter(outputBundleFileName))
			{
				bundleInst.file.Write(writer, bundleReplacers);
			}

			// Cleanup by deleting the JSON files that were created
			foreach (string exportedJsonFileName in exportedJsonFileNames)
			{
				Debug.WriteLine($"Cleanup - Deleting temp JSON file {exportedJsonFileName}");
				File.Delete(exportedJsonFileName);
			}
		}
		static List<string> exportAssetsToJson(string newAssetsFileName, string assetNameSearchTerm, AssetClassID? assetType = null, string folderPath = "")
		{
			// Load assets from a Unity assets file
			AssetsManager assetsManager = new AssetsManager();
			try
			{
				assetsManager.LoadClassPackage("classdata.tpk");  // I took this from UABE Avalonia v6
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Error: classdata.tpk not found. Please copy this file from SampleFiles into your current directory.");
				throw;
			}
			AssetsFileInstance newAssetsFileInst = assetsManager.LoadAssetsFile(newAssetsFileName, true /*loadDeps*/);
			AssetsFile newAssetsFile = newAssetsFileInst.file;
			assetsManager.LoadClassDatabaseFromPackage(newAssetsFile.Metadata.UnityVersion);

			List<string> exportedJsonFileNames = new List<string>();
			List<AssetFileInfo> assetsToSearch = assetType != null ? newAssetsFile.GetAssetsOfType(assetType.Value) : newAssetsFile.AssetInfos;
			foreach (AssetFileInfo? assetInfo in assetsToSearch)
			{
				AssetTypeValueField rootNode = assetsManager.GetBaseField(newAssetsFileInst, assetInfo);
				string currentAssetName = rootNode["m_Name"].AsString;
				bool shouldExportAsset = currentAssetName.Contains(assetNameSearchTerm);
				if (shouldExportAsset)
				{
					string exportedFileName = Path.Combine(folderPath, currentAssetName + ".json");
					UABEHelper.DumpJson(rootNode, exportedFileName);
					exportedJsonFileNames.Add(exportedFileName);
				}
			}

			assetsManager.UnloadAll();
			return exportedJsonFileNames;
		}

		static AssetsReplacer? updateExistingMesh(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, string meshName, string newMeshJsonFilePath, bool exactMatch = false)
		{
			AssetFileInfo? assetInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, meshName, AssetClassID.Mesh, exactMatch);
			if (assetInfo != null)
			{
				AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, assetInfo);
				string assetName = rootNode["m_Name"].AsString;
				uint rootBoneNameHash = rootNode["m_RootBoneNameHash"].AsUInt;
				AssetTypeValueField boneNameHashesArray = rootNode["m_BoneNameHashes"]["Array"];
				IEnumerable<uint> boneNameHashes = boneNameHashesArray.Children.Select(x => x.AsUInt);

				writeBoneInfoToMeshFile(newMeshJsonFilePath, assetName, rootBoneNameHash, boneNameHashes);

				AssetsReplacer replacer = UABEHelper.ImportJsonAsset(assetsManager, assetsFileInst, assetInfo, newMeshJsonFilePath);
				Debug.WriteLine($"Imported Mesh asset {assetName} from JSON file \"{newMeshJsonFilePath}\"");
				return replacer;
			}
			else
			{
				Debug.WriteLine($"Mesh with name {meshName} not found");
				return null;
			}
		}

		static void writeBoneInfoToMeshFile(string fileName, string assetName, uint rootBoneNameHash, IEnumerable<uint> boneNameHashes)
		{
			string fileContents = File.ReadAllText(fileName);
			JToken rootToken = JToken.Parse(fileContents);
			rootToken["m_Name"] = assetName;
			rootToken["m_RootBoneNameHash"] = rootBoneNameHash;
			rootToken["m_BoneNameHashes"]["Array"] = new JArray(boneNameHashes);
			File.WriteAllText(fileName, rootToken.ToString());
			Debug.WriteLine($"Updated name and bone name hashes in JSON file \"{fileName}\"");
		}
	}
}
