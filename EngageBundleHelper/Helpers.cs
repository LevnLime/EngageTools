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
