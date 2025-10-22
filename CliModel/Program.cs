using CUE4Parse;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

if (args.Length < 2) {
    Console.WriteLine("Usage: CliModel.exe [UEVER] [PATH_TO_FOLDER_WITH_PAKS] [OPTIONS]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -MainAes");
    Console.WriteLine("Example: CliModel.exe GAME_UE4_22 Z:/home/yes/WinApps/7.30/FortniteGame/Content/Paks -MainAes 0xD23E6F3CF45A2E31081CB7D5F94C85EC50CCB1A804F8C90248F72FA3896912E4");
    return;
}

var WineUsername = Environment.GetEnvironmentVariable("WINEUSERNAME");

var outPath = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Output");
Directory.CreateDirectory(outPath);

var gameDir = ParsePath(args[1]);
Console.WriteLine($"Loading UE Version {args[0]} on path {gameDir}");

var provider = new DefaultFileProvider(gameDir, SearchOption.TopDirectoryOnly, new VersionContainer((EGame)Enum.Parse(typeof(EGame), args[0])));
provider.ReadScriptData = true;
provider.Initialize();

for (int i = 2; i < args.Length; i++) {
    if (args[i] == "-MainAes") provider.SubmitKey(new FGuid(), new FAesKey(args[++i]));
}

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
        Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
    }

    else if (inputArgs[0] == "exportfiles") {
        var outFilePath = Path.Join(outPath, "Files.txt");
        using (StreamWriter writer = new StreamWriter(outFilePath)) {
            foreach (var path in provider.Files.Keys) {
                writer.WriteLine(path);
            }
        }
        Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
    }

    else if (inputArgs[0] == "decomp") {
        var obj = provider.LoadPackageObject<UClass>(inputArgs[1]);
        var code = obj.DecompileBlueprintToPseudo();
        var outFilePath = Path.Join(outPath, obj.GetPathName().Split('.')[0] + ".cpp");
        Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));
        File.WriteAllText(outFilePath, code);
        Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
    }
}

string ParsePath(string path) {
    if (path.Length == 0)
        return path;

    path = path.Replace("\\", "/");

    // Idk if this works for every wine config but it works for me :)
    if (WineUsername is not null) {
        if (path[0] == '~') path = path.Replace("~", $"Z:/home/{WineUsername}");
        else if (path.StartsWith($"Z:/home/{WineUsername}")) path = path.Replace($"Z:/home/{WineUsername}", "~");
    }

    return path;
}
