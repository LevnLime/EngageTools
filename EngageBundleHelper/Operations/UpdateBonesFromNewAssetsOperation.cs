using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Operations
{
	public record UpdateBonesFromNewAssetsOperationParams
	{
		public required string BundleFileName { get; init; }
		public required string NewAssetsFileName { get; init; }
		public required string RootBoneName { get; init; }
		public string BasePath { get; init; } = string.Empty;
		public string OutputBundleFileName { get; init; } = "output.bundle";
	}

	public class UpdateBonesFromNewAssetsOperation
	{
		private class BoneStructureTreeNode
		{
			public required string Name { get; init; }
			public required Vector3 Position { get; init; }
			public required Vector4 Rotation { get; init; }
			public required Vector3 Scale {  get; init; }
			public List<BoneStructureTreeNode> Children { get; } = new List<BoneStructureTreeNode>();
		}

		private UpdateBonesFromNewAssetsOperationParams parameters;

		public UpdateBonesFromNewAssetsOperation(UpdateBonesFromNewAssetsOperationParams parameters)
		{
			this.parameters = parameters;
		}

		public void Execute()
		{
			Execute(
				Path.Combine(parameters.BasePath, parameters.BundleFileName),
				Path.Combine(parameters.BasePath, parameters.NewAssetsFileName),
				parameters.RootBoneName,
				parameters.BasePath,
				Path.Combine(parameters.BasePath, parameters.OutputBundleFileName)
			);
		}

		public static void Execute(string bundleFileName, string newAssetsFileName, string rootBoneName, string basePath, string outputBundleFileName)
		{
			AssetsManager assetsManager = new AssetsManager();
			BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
			AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);

			List<AssetsReplacer> assetsReplacers = new List<AssetsReplacer>();

			// Traverse the bones in the new assets file that Unity built and get the bone data in a way that we can easily use
			// Note: We generate a flat map of Bone Name -> Bone Data to use for easy lookup by bone name
			// Yes, this does ignore the tree hierarchy of the bones. In my testing, I noticed that the new bone hierarchy built by Unity
			// actually does not exactly match the original hierarchy. I don't know why this happens, but because the new hierarchy
			// doesn't seem to exactly match the original, I chose to just ignore the new hierarchy and look up bones by name instead.
			// This does mean that if we ever want to later add functionality to add/remove bones, we may need to rethink the design.
			BoneStructureTreeNode newBoneStructureTreeRoot = generateBoneStructureTreeFromNewAssets(newAssetsFileName, rootBoneName);
			Dictionary<string, BoneStructureTreeNode> newBoneDataMap = flattenBoneStructureTreeToMap(newBoneStructureTreeRoot);
			Debug.WriteLine($"Successfully traversed bone hierarchy in {newAssetsFileName} and created bone data map");

			// Find the root bone of the original bundle (the bundle that we're going to be editing bone data for)
			List<AssetFileInfo> gameObjects = assetsFileInst.file.GetAssetsOfType(AssetClassID.GameObject);
			AssetFileInfo? rootBoneInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, rootBoneName, AssetClassID.GameObject, false /*exactMatch*/);
			if (rootBoneInfo == null)
			{
				throw new Exception($"Unable to find root bone {rootBoneName} game object from original bundle {bundleFileName}");
			}

			// Find the Transform component of that root bone
			AssetFileInfo rootBoneTransformInfo = getTransformFromGameObject(assetsManager, assetsFileInst, rootBoneInfo).info;

			// Traverse the bone hierarchy, copying over new bone values and populated the list of AssetsReplacers
			copyNewBoneDataRecursive(assetsManager, assetsFileInst, rootBoneTransformInfo, newBoneDataMap, assetsReplacers);

			List<BundleReplacer> bundleReplacers = new List<BundleReplacer>
			{
				new BundleReplacerFromAssets(assetsFileInst.name, null, assetsFileInst.file, assetsReplacers)
			};

			using (AssetsFileWriter writer = new AssetsFileWriter(outputBundleFileName))
			{
				bundleInst.file.Write(writer, bundleReplacers);
			}
		}

		static BoneStructureTreeNode generateBoneStructureTreeFromNewAssets(string newAssetsFileName, string rootBoneName)
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

			// Find the root bone
			List<AssetFileInfo> gameObjects = newAssetsFile.GetAssetsOfType(AssetClassID.GameObject);
			AssetFileInfo? rootBoneInfo = Helpers.findAssetInfoByName(assetsManager, newAssetsFileInst, rootBoneName, AssetClassID.GameObject, false /*exactMatch*/);
			if (rootBoneInfo == null)
			{
				throw new Exception($"Unable to find root bone {rootBoneName} game object from {newAssetsFileName} assets file");
			}

			// Find the Transform component of that root bone
			AssetTypeValueField rootBoneTransform = getTransformFromGameObject(assetsManager, newAssetsFileInst, rootBoneInfo).baseField;

			// Traverse the Transform hierarchy and generate a tree that contains the relevant information
			BoneStructureTreeNode rootTreeNode = generateBoneStructureTreeRecursive(assetsManager, newAssetsFileInst, rootBoneTransform);

			return rootTreeNode;
		}

		static BoneStructureTreeNode generateBoneStructureTreeRecursive(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetTypeValueField currentTransformNode)
		{
			// Fetch information about this current asset
			AssetTypeValueField positionData = currentTransformNode["m_LocalPosition"];
			AssetTypeValueField rotationData = currentTransformNode["m_LocalRotation"];
			AssetTypeValueField scaleData = currentTransformNode["m_LocalScale"];
			AssetTypeValueField childrenData = currentTransformNode["m_Children"]["Array"];
			AssetTypeValueField gameObject = getGameObjectFromTransform(assetsManager, assetsFileInst, currentTransformNode);
			
			string name = gameObject["m_Name"].AsString;
			Vector3 position = new Vector3(positionData["x"].AsFloat, positionData["y"].AsFloat, positionData["z"].AsFloat);
			Vector4 rotation = new Vector4(rotationData["x"].AsFloat, rotationData["y"].AsFloat, rotationData["z"].AsFloat, rotationData["w"].AsFloat);
			Vector3 scale = new Vector3(scaleData["x"].AsFloat, scaleData["y"].AsFloat, scaleData["z"].AsFloat);

			// Create a new tree node for this data
			BoneStructureTreeNode currentTreeNode = new BoneStructureTreeNode()
			{
				Name = name,
				Position = position,
				Rotation = rotation,
				Scale = scale
			};

			// Recursively build the children subtrees
			foreach (AssetTypeValueField child in childrenData)
			{
				// The children here are pointers to other Transform (PPtr<Transform>)
				AssetExternal childAssetExt = assetsManager.GetExtAsset(assetsFileInst, child);
				AssetTypeValueField childTransformNode = childAssetExt.baseField;
				BoneStructureTreeNode childTreeNode = generateBoneStructureTreeRecursive(assetsManager, assetsFileInst, childTransformNode);
				currentTreeNode.Children.Add(childTreeNode);
			}

			return currentTreeNode;
		}

		static void copyNewBoneDataRecursive(
			AssetsManager assetsManager,
			AssetsFileInstance assetsFileInst,
			AssetFileInfo currentTransformInfo,
			Dictionary<string, BoneStructureTreeNode> newBoneDataMap,
			List<AssetsReplacer> assetsReplacers)
		{
			AssetTypeValueField currentTransformNode = assetsManager.GetBaseField(assetsFileInst, currentTransformInfo);

			// Fetch the fields from this current asset
			AssetTypeValueField position = currentTransformNode["m_LocalPosition"];
			AssetTypeValueField rotation = currentTransformNode["m_LocalRotation"];
			AssetTypeValueField scale = currentTransformNode["m_LocalScale"];
			AssetTypeValueField children = currentTransformNode["m_Children"]["Array"];
			AssetTypeValueField gameObject = getGameObjectFromTransform(assetsManager, assetsFileInst, currentTransformNode);
			string boneName = gameObject["m_Name"].AsString;

			if (!newBoneDataMap.ContainsKey(boneName))
			{
				// If this bone is not in the set of new bones that Unity generated, just ignore
				Debug.WriteLine($"Bone \"{boneName}\" from original bundle not found in new bone data. Ignoring...");
				return;
			}

			BoneStructureTreeNode newBoneData = newBoneDataMap[boneName];
			Vector3 newPosition = newBoneData.Position;
			Vector4 newRotation = newBoneData.Rotation;
			Vector3 newScale = newBoneData.Scale;

			// Copy the new data over
			position["x"].AsFloat = newPosition.X;
			position["y"].AsFloat = newPosition.Y;
			position["z"].AsFloat = newPosition.Z;
			rotation["x"].AsFloat = newRotation.X;
			rotation["y"].AsFloat = newRotation.Y;
			rotation["z"].AsFloat = newRotation.Z;
			rotation["w"].AsFloat = newRotation.W;
			scale["x"].AsFloat = newScale.X;
			scale["y"].AsFloat = newScale.Y;
			scale["z"].AsFloat = newScale.Z;

			// Add a new AssetsReplacer for all these changes
			assetsReplacers.Add(new AssetsReplacerFromMemory(assetsFileInst.file, currentTransformInfo, currentTransformNode));

			// Recursively traverse through the children
			foreach (AssetTypeValueField child in children)
			{
				// The children here are pointers to other Transform (PPtr<Transform>)
				AssetExternal childAssetExt = assetsManager.GetExtAsset(assetsFileInst, child);
				AssetFileInfo childTransformInfo = childAssetExt.info;
				copyNewBoneDataRecursive(assetsManager, assetsFileInst, childTransformInfo, newBoneDataMap, assetsReplacers);
			}
		}

		// Given a GameObject, returns this GameObject's Transform component's BaseField
		static AssetExternal getTransformFromGameObject(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetFileInfo assetInfo)
		{
			AssetTypeValueField baseField = assetsManager.GetBaseField(assetsFileInst, assetInfo);
			return getTransformFromGameObject(assetsManager, assetsFileInst, baseField);
		}
		static AssetExternal getTransformFromGameObject(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetTypeValueField baseField)
		{
			/*
			Sample bone game object
			{
			  "m_Component": {
				"Array": [
				  {
					"component": {
					  "m_FileID": 0,
					  "m_PathID": 72
					}
				  }
				]
			  },
			  "m_Layer": 0,
			  "m_Name": "uHair_h051h",
			  "m_Tag": 0,
			  "m_IsActive": true
			}
			 */

			AssetTypeValueField components = baseField["m_Component"]["Array"];
			
			// The Transform should always be the first component
			AssetTypeValueField transformPPtr = components[0]["component"];
			
			return assetsManager.GetExtAsset(assetsFileInst, transformPPtr);
		}

		static AssetTypeValueField getGameObjectFromTransform(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetFileInfo assetInfo)
		{
			AssetTypeValueField baseField = assetsManager.GetBaseField(assetsFileInst, assetInfo);
			return getGameObjectFromTransform(assetsManager, assetsFileInst, baseField);
		}
		static AssetTypeValueField getGameObjectFromTransform(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetTypeValueField baseField)
		{
			AssetTypeValueField gameObjectPPtr = baseField["m_GameObject"];
			AssetExternal gameObjectAssetExternal = assetsManager.GetExtAsset(assetsFileInst, gameObjectPPtr);
			return gameObjectAssetExternal.baseField;
		}

		/// <summary>
		/// Flattens the Bones hierarchy tree into a flat map of Bone Name -> Bone data info
		/// This will allow us to look up bone data by name easily
		/// </summary>
		static Dictionary<string, BoneStructureTreeNode> flattenBoneStructureTreeToMap(BoneStructureTreeNode rootNode)
		{
			Dictionary<string, BoneStructureTreeNode> map = new Dictionary<string, BoneStructureTreeNode>();
			flattenBoneStructureTreeToMapRecursive(rootNode, map);
			return map;
		}
		static void flattenBoneStructureTreeToMapRecursive(BoneStructureTreeNode treeNode, Dictionary<string, BoneStructureTreeNode> map)
		{
			map.Add(treeNode.Name, treeNode);
			foreach(BoneStructureTreeNode child in treeNode.Children)
			{
				flattenBoneStructureTreeToMapRecursive(child, map);
			}
		}

		static void compareTrees(BoneStructureTreeNode tree1, BoneStructureTreeNode tree2)
		{
			bool namesMatch = string.Equals(tree1.Name, tree2.Name);
			if (!namesMatch)
			{
				Console.WriteLine($"Tree Mismatch: {tree1.Name} -- {tree2.Name}");
				return;
			}

			if (tree1.Children.Count != tree2.Children.Count)
			{
				Console.WriteLine($"Children Count mismatch at node {tree1.Name}");
				return;
			}

			for (int i = 0; i < tree1.Children.Count; i++)
			{
				compareTrees(tree1.Children[i], tree2.Children[i]);
			}
		}
	}
}
