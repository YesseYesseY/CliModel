using CUE4Parse;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

if (args.Length < 2) {
    Console.WriteLine("Usage: CliModel.exe [UEVER] [PATH_TO_FOLDER_WITH_PAKS]");
    Console.WriteLine("Example: CliModel.exe GAME_UE4_22 Z:/home/yes/WinApps/7.30/FortniteGame/Content/Paks");
    return;
}

var WineUsername = Environment.GetEnvironmentVariable("WINEUSERNAME");

var outPath = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Output");
Directory.CreateDirectory(outPath);

var gameDir = ParsePath(args[1]);
Console.WriteLine($"Loading UE Version {args[0]} on path {gameDir}");

var provider = new DefaultFileProvider(gameDir, SearchOption.TopDirectoryOnly, new VersionContainer((EGame)Enum.Parse(typeof(EGame), args[0])));
provider.Initialize();
provider.SubmitKey(new FGuid(), new FAesKey("0xD23E6F3CF45A2E31081CB7D5F94C85EC50CCB1A804F8C90248F72FA3896912E4"));

while (true) {
    var input = Console.ReadLine();
    var inputArgs = input.Split(' ');

    if (inputArgs is null || inputArgs.Length == 0)
        continue;

    if (inputArgs[0] == "files") {
        var searchWord = inputArgs.Length > 1 ? inputArgs[1] : "";
        foreach (var yes in provider.Files.Keys) {
            if (yes.Contains(searchWord)) Console.WriteLine(yes);
        }
        Console.WriteLine("Search done!");
    }

    else if (inputArgs[0] == "jsonexports") {
        var package = provider.LoadPackage(inputArgs[1]);
        var exports = package.GetExports();

        var outFilePath = Path.Join(outPath, package.Name + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));
        File.WriteAllText(outFilePath, JsonConvert.SerializeObject(exports, Formatting.Indented));
        Console.WriteLine($"Exported to: {outFilePath}");
    }

    else if (inputArgs[0] == "path") {
        Console.WriteLine(ParsePath(inputArgs[1]));
    }
}

string ParsePath(string path) {
    if (path.Length == 0)
        return path;

    // Idk if this works for every wine config but it works for me :)
    if (WineUsername is not null && path[0] == '~') {
        return path.Replace("~", $"Z:/home/{WineUsername}");
    }

    return path;
}
