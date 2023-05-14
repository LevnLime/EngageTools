using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UABEAvalonia;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Xml.Linq;
using AssetsTools.NET.Texture;

namespace EngageBundleHelper
{
	/// <summary>
	/// This helper class is a wrapper for using UABEAvalonia's functions
	/// </summary>
	public class UABEHelper
	{
		public static void DumpJson(AssetTypeValueField rootNode, string filePath)
		{
			AssetImportExport importExportLib = new AssetImportExport();

			using (StreamWriter sw = new StreamWriter(filePath))
			{
				importExportLib.DumpJsonAsset(sw, rootNode);
			}
		}

		public static AssetsReplacer ImportJsonAsset(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetFileInfo assetInfo, string filePath)
		{
			byte[] bytes = ImportJsonAssetAsBytes(assetsManager, assetsFileInst, assetInfo, filePath);
			return new AssetsReplacerFromMemory(assetsFileInst.file, assetInfo, bytes);
		}

		public static byte[] ImportJsonAssetAsBytes(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetFileInfo assetInfo, string filePath)
		{
			AssetTypeTemplateField templateField = assetsManager.GetTemplateBaseField(assetsFileInst, assetInfo);
			return ImportJsonAssetAsBytes(templateField, filePath);
		}

		public static byte[] ImportJsonAssetAsBytes(AssetTypeTemplateField templateField, string filePath)
		{
			AssetImportExport importExportLib = new AssetImportExport();

			string? exceptionMessage = null;
			byte[]? bytes = null;

			using (FileStream fs = File.OpenRead(filePath))
			{
				using (StreamReader sr = new StreamReader(fs))
				{
					bytes = importExportLib.ImportJsonAsset(templateField, sr, out exceptionMessage);
				}
			}

			if (bytes == null)
			{
				throw new Exception($"ImportJson error. Something went wrong when reading from file {filePath}. Exception: {exceptionMessage}");
			}

			return bytes;
		}

		public static byte[] GetTexturePlatformBlob(AssetTypeValueField textureRootNode)
		{
			return TexturePlugin.TextureHelper.GetPlatformBlob(textureRootNode);
		}

		public static byte[] ImportTexture(string imagePath, TextureFormat format, out int width, out int height, ref int mips, uint platform = 0, byte[] platformBlob = null)
		{
			return TexturePlugin.TextureImportExport.Import(imagePath, format, out width, out height, ref mips, platform, platformBlob);
		}
	}
}
