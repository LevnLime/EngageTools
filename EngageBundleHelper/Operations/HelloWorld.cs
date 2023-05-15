using AssetsTools.NET.Extra;
using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Operations
{
	internal class HelloWorld
	{
		string basePath = "";
		void HelloWorldTest()
		{
			string testFile = Path.Combine(basePath, "base.bundle");
			AssetsManager assetsManager = new AssetsManager();

			BundleFileInstance baseBundle = assetsManager.LoadBundleFile(testFile);
			AssetsFileInstance baseAssetsFileInst = assetsManager.LoadAssetsFileFromBundle(baseBundle, 0);

			var file = baseAssetsFileInst;
			List<AssetsReplacer> replacers = new List<AssetsReplacer>();
			foreach (var assetInfo in file.file.GetAssetsOfType(AssetClassID.Material))
			{
				var rootNode = assetsManager.GetBaseField(file, assetInfo);
				string name = rootNode["m_Name"].AsString;
				string newName = name + "_MODIFIED";
				Console.WriteLine($"{name} -> {newName}");
				rootNode["m_Name"].AsString = newName;
				replacers.Add(new AssetsReplacerFromMemory(file.file, assetInfo, rootNode));
			}

			var writer = new AssetsFileWriter(Path.Combine(basePath, "testtestModified.bundle"));
			file.file.Write(writer, 0, replacers);
			writer.Close();
		}
	}
}
