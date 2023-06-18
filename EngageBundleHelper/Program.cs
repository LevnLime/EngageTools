using EngageBundleHelper;
using EngageBundleHelper.Operations;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommandLine;
using Newtonsoft.Json;

// See https://aka.ms/new-console-template for more information

CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(RunWithOptions);

static void RunWithOptions(CommandLineOptions options)
{
	if (options.Verbose)
	{
		Debug.IsDebugEnabled = true;
	}

	// Parse the config file
	if (!File.Exists(options.ConfigFile))
		throw new FileNotFoundException(options.ConfigFile);
	string fileContents = File.ReadAllText(options.ConfigFile);

	RootConfigFile config = JsonConvert.DeserializeObject<RootConfigFile>(fileContents);
	
	switch(config.Operation)
	{
		case "UpdateMeshesFromNewAssets":
			UpdateMeshesFromNewAssetsOperationParams updateMeshesParams = JsonConvert.DeserializeObject<UpdateMeshesFromNewAssetsOperationParams>(fileContents);
			UpdateMeshesFromNewAssetsOperation updateMeshesOperation = new UpdateMeshesFromNewAssetsOperation(updateMeshesParams);
			updateMeshesOperation.Execute();
			break;
		case "AddNewMaterial":
			AddNewMaterialOperationParams addNewMaterialParams = JsonConvert.DeserializeObject<AddNewMaterialOperationParams>(fileContents);
			AddNewMaterialOperation addNewMaterialOperation = new AddNewMaterialOperation(addNewMaterialParams);
			addNewMaterialOperation.Execute();
			break;
		case "UpdateBonesFromNewAssets":
			UpdateBonesFromNewAssetsOperationParams updateBonesParams = JsonConvert.DeserializeObject<UpdateBonesFromNewAssetsOperationParams>(fileContents);
			UpdateBonesFromNewAssetsOperation updateBonesOperation = new UpdateBonesFromNewAssetsOperation(updateBonesParams);
			updateBonesOperation.Execute();
			break;
		default:
			throw new ArgumentException($"Invalid Operation: {config.Operation}");
	}
}

public record RootConfigFile
{
	public required string Operation { get; init; }
}

public class CommandLineOptions
{
	[Option('c', "ConfigFile", Required = true, HelpText = "Filename of the config file to use")]
	public required string ConfigFile { get; set; }

	[Option('v', "verbose", Required = false, HelpText = "Print verbose messages to the console to assist in debugging")]
	public bool Verbose { get; set; }
}