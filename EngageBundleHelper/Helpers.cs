using AssetsTools.NET.Extra;
using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper
{
	internal static class Helpers
	{
		public static Random RNG = new Random();

		public static AssetFileInfo? findAssetInfoByName(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, string searchName, AssetClassID? typeId = null, bool exactMatch = false)
		{
			List<AssetFileInfo> assetsList;
			if (typeId != null)
				assetsList = assetsFileInst.file.GetAssetsOfType(typeId.Value);
			else
				assetsList = assetsFileInst.file.AssetInfos;

			return assetsList.Find((AssetFileInfo assetInfo) =>
			{
				AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, assetInfo);
				string currentAssetName = rootNode["m_Name"].AsString;
				if (isFindAssetInfoByNameSpecialCase(currentAssetName, searchName, (AssetClassID)assetInfo.TypeId, out bool specialCaseResult))
				{
					return specialCaseResult;
				}
				if (exactMatch)
				{
					return string.Equals(currentAssetName, searchName);
				}
				else
				{
					return !string.IsNullOrEmpty(currentAssetName) && currentAssetName.Contains(searchName);
				}
			});
		}

		private static bool isFindAssetInfoByNameSpecialCase(string currentAssetName, string searchName, AssetClassID typeId, out bool specialCaseResult)
		{
			// Special Case: Shadow Mesh
			// Fire Emblem Engage ubody bundles often contain 3 Mesh assets: the "Skin mesh", the "Dress mesh", and the "Shadow mesh"
			// Example: Alear unpromoted ubody bundle contains these 3 Mesh assets: "Drg0AF_c051_Skin", "Drg0AF_c051_Dress", and "ShadowMesh_Dress"
			// Almost always, a user searching for "Dress" or "_Dress" will want the "Dress mesh", not the "Shadow mesh". Therefore, we don't want
			// to accidentally return the "Shadow mesh" in this case if the "Shadow mesh" happens to appear first during the Find iteration.
			// This special case will ignore possible "Shadow mesh" assets unless the searchName exactly matches
			if (typeId == AssetClassID.Mesh && !string.IsNullOrEmpty(currentAssetName) && currentAssetName.Contains("ShadowMesh"))
			{
				specialCaseResult = string.Equals(currentAssetName, searchName);
				return true;
			}

			// No special case hit
			specialCaseResult = false;  // value is arbitrary because it's not going to be used anyway
			return false;
		}

		public static long getRandomNewPathId(AssetsFileInstance assetsFileInst)
		{
			long randomPathId = RNG.NextInt64();
			while (assetsFileInst.file.GetAssetInfo(randomPathId) != null)
			{
				// Retry if we happen to get a pathId that already is in use
				randomPathId = RNG.NextInt64();
			}
			return randomPathId;
		}
	}
}
