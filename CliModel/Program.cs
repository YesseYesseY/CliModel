using CUE4Parse;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

string ConfigMainAes = "";
string ConfigUeVer = "GAME_UE5_LATEST";
string ConfigPaksPath = "";
List<KeyValuePair<FGuid, FAesKey>> ConfigAesKeys = new(); 
string ConfigAction = "";

void LoadConfig(string configPath) {
    var config = File.ReadAllLines(configPath);
    foreach (var line in config) {
        var data = line.Split('=');
             if (data[0] == "MainAes") ConfigMainAes = data[1];
        else if (data[0] == "UeVer") ConfigUeVer = data[1];
        else if (data[0] == "UePaksPath") ConfigPaksPath = data[1];
        else if (data[0] == "Aes") {
            var data2 = data[1].Split(',');
            ConfigAesKeys.Add(new (new FGuid(data2[0]), new FAesKey(data2[1])));
        }
    }

    if (ConfigPaksPath == "") {
        ConfigPaksPath = Path.GetDirectoryName(configPath) ?? "";
    }
}

var naturalConfigPath = Path.Join(Directory.GetCurrentDirectory(), "CliModel.config");
if (File.Exists(naturalConfigPath)) {
    Console.WriteLine("Found CliModel.config!");
    LoadConfig(naturalConfigPath);
}

for (int i = 0; i < args.Length; i++) {
         if (args[i] == "-MainAes") ConfigMainAes = args[++i];
    else if (args[i] == "-UeVer") ConfigUeVer = args[++i];
    else if (args[i] == "-PaksPath") ConfigPaksPath = args[++i];
    else if (args[i] == "-Config") LoadConfig(args[++i]);
    else if (args[i] == "--") {
        ConfigAction = args[++i];
        break;
    }
}

if (ConfigPaksPath == "") {
    Console.WriteLine("Usage: CliModel.exe [OPTIONS] -- [ACTION]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -Config     // Load options through a config file");
    Console.WriteLine("  -PaksPath // Required");
    Console.WriteLine("  -UeVer      // Default: GAME_UE5_LATEST");
    Console.WriteLine("  -MainAes");
    Console.WriteLine("Example: CliModel.exe -UeVer GAME_UE4_22 -PaksPath Z:/home/yes/WinApps/7.30/FortniteGame/Content/Paks -MainAes 0xD23E6F3CF45A2E31081CB7D5F94C85EC50CCB1A804F8C90248F72FA3896912E4");
    Console.WriteLine("Example Config:");
    Console.WriteLine("------ CliModel.config ------");
    Console.WriteLine("// Comments don't exist they're just for this example");
    Console.WriteLine("// Do not put spaces to the left/right of =");
    Console.WriteLine("// If PaksPath is not in the config it will treat the location of the config file as PaksPath");
    Console.WriteLine("PaksPath=Z:/home/yes/WinApps/7.30/FortniteGame/Content/Paks // Optional");
    Console.WriteLine("UeVer=GAME_UE4_22");
    Console.WriteLine("MainAes=0xD23E6F3CF45A2E31081CB7D5F94C85EC50CCB1A804F8C90248F72FA3896912E4");
    Console.WriteLine("Aes=A9AFB4A346420DB1399A2FB2065528F5,0x663CE8F8268B3660B2829973428DB050BE0B4F7DC31222FAA99584D91D0460C8");
    Console.WriteLine("-----------------------------");
    return;
}

var WineUsername = Environment.GetEnvironmentVariable("WINEUSERNAME");

var outPath = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "", "Output");
Directory.CreateDirectory(outPath);

var gameDir = ParsePath(ConfigPaksPath, true);
Console.WriteLine($"Loading UE Version {ConfigUeVer} on path {gameDir}");

var provider = new DefaultFileProvider(gameDir, SearchOption.TopDirectoryOnly, new VersionContainer((EGame)Enum.Parse(typeof(EGame), ConfigUeVer)));
provider.ReadScriptData = true;
provider.Initialize();

if (ConfigMainAes != "") {
    provider.SubmitKey(new FGuid(), new FAesKey(ConfigMainAes));
}
provider.SubmitKeys(ConfigAesKeys);

bool singleAction = ConfigAction != "";
while (true) {
    var input = singleAction ? ConfigAction : Console.ReadLine() ?? "";
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

    else if (inputArgs[0] == "exports") {
        if (inputArgs.Length == 1) {
            Console.WriteLine("Not enough args for exports");
            continue;
        }
        if (!provider.TryLoadPackage(inputArgs[1], out var package)) {
            Console.WriteLine($"Failed to load package {inputArgs[1]}");
            continue;
        }
        var exports = package.GetExports();

        var outFilePath = Path.Join(outPath, package.Name + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(outFilePath) ?? "");
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
        if (inputArgs.Length == 1) {
            Console.WriteLine("Not enough args for decomp");
            continue;
        }
        if (!provider.TryLoadPackageObject<UClass>(inputArgs[1], out UClass? obj) || obj is null) {
            Console.WriteLine($"Failed to load object {inputArgs[1]}");
            continue;
        }
        var code = obj.DecompileBlueprintToPseudo();
        var outFilePath = Path.Join(outPath, obj.GetPathName().Split('.')[0] + ".cpp");
        Directory.CreateDirectory(Path.GetDirectoryName(outFilePath) ?? "");
        File.WriteAllText(outFilePath, code);
        Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
    }

    if (singleAction || inputArgs[0] == "exit") {
        break;
    }
}

string ParsePath(string path, bool onlyLinuxToWindows = false) {
    if (path.Length == 0)
        return path;

    path = path.Replace("\\", "/");

    // Idk if this works for every wine config but it works for me :)
    if (WineUsername is not null) {
        if (path[0] == '~') path = path.Replace("~", $"Z:/home/{WineUsername}");
        else if (!onlyLinuxToWindows && path.StartsWith($"Z:/home/{WineUsername}")) path = path.Replace($"Z:/home/{WineUsername}", "~");
    }

    return path;
}
