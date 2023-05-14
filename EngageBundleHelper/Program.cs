using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using EngageBundleHelper;
using EngageBundleHelper.Operations;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UABEAvalonia;

// See https://aka.ms/new-console-template for more information

string basePath = "C:\\Users\\Burney\\source\\repos\\EngageTools\\TestFiles";

UpdateMeshesFromNewAssetsOperationParams updateMeshesParams = new UpdateMeshesFromNewAssetsOperationParams()
{
	BundleFileName = "base.bundle",
	NewAssetsFileName = "sharedassets0.assets",
	NewAssetsNameSearchTerm = "_FIXED",
	MeshesToUpdate = new List<string>() { "_Dress", "_Skin" },  // Be careful not to accidentally pick the "ShadowMesh_Dress"!
	BasePath = basePath,
	OutputBundleFileName = "updateMeshesOutput.bundle"
};
UpdateMeshesFromNewAssetsOperation updateMeshesOperation = new UpdateMeshesFromNewAssetsOperation(updateMeshesParams);
updateMeshesOperation.Execute();

AddNewMaterialOperationParams addNewMaterialParams = new AddNewMaterialOperationParams()
{
	BundleFileName = "updateMeshesOutput.bundle",
	NewMaterialFileName = "MtSkin_Material.json",
	AlbedoTextureJsonFileName = "Albedo_Texture.json",
	NormalTextureJsonFileName = "Normal_Texture.json",
	MultiTextureJsonFileName = "Multi_Texture.json",
	AlbedoTextureImageFileName = "Albedo_Texture.png",
	NormalTextureImageFileName = "Normal_Texture.png",
	MultiTextureImageFileName = "Multi_Texture.png",
	BasePath = basePath,
	OutputBundleFileName = "final.bundle"
};
AddNewMaterialOperation addNewMaterialOperation = new AddNewMaterialOperation(addNewMaterialParams);
addNewMaterialOperation.Execute();

Console.WriteLine("Complete!");

void HelloWorld()
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