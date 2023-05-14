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
//updateMeshesOperation.Execute();

AddNewMaterialOperationParams addNewMaterialParams = new AddNewMaterialOperationParams()
{
	BundleFileName = "base.bundle",
	NewMaterialFileName = "MtSkin_Material.json",
	AlbedoTextureJsonFileName = "Albedo_Texture.json",
	NormalTextureJsonFileName = "Normal_Texture.json",
	MultiTextureJsonFileName = "Multi_Texture.json",
	BasePath = basePath,
	OutputBundleFileName = "addNewMaterialOutput.bundle"
};
AddNewMaterialOperation addNewMaterialOperation = new AddNewMaterialOperation(addNewMaterialParams);
//addNewMaterialOperation.Execute();

foo();

Console.ReadLine();

void foo()
{
	string bundleFileName = Path.Combine(basePath, "updatedAlear.bundle");
	string textureFilePath = Path.Combine(basePath, "Zephia_Multi_forReuse.png");

	AssetsManager assetsManager = new AssetsManager();
	BundleFileInstance bundleInst = assetsManager.LoadBundleFile(bundleFileName);
	AssetsFileInstance assetsFileInst = assetsManager.LoadAssetsFileFromBundle(bundleInst, 0, true /*loadDeps*/);

	AssetFileInfo? textureJsonInfo = Helpers.findAssetInfoByName(assetsManager, assetsFileInst, "_Multi", AssetClassID.Texture2D);
	if (textureJsonInfo == null)
	{
		throw new Exception("Unable to find corresponding Texture JSON file");
	}

	AssetTypeValueField rootNode = assetsManager.GetBaseField(assetsFileInst, textureJsonInfo);
	TextureFormat format = (TextureFormat)rootNode["m_TextureFormat"].AsInt;

	byte[] platformBlob = UABEHelper.GetTexturePlatformBlob(rootNode);
	uint platform = assetsFileInst.file.Metadata.TargetPlatform;

	int mips = !rootNode["m_MipCount"].IsDummy ? rootNode["m_MipCount"].AsInt : 1;
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

	var replacer = new AssetsReplacerFromMemory(assetsFileInst.file, textureJsonInfo, rootNode);
	
	using (AssetsFileWriter writer = new AssetsFileWriter(bundleFileName + ".mod"))
	{
		assetsFileInst.file.Write(writer, 0, new List<AssetsReplacer>() { replacer });
	}
}

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