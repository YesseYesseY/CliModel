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

string OutputPath = "";

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

void DrawBorder((int x, int y) start, (int x, int y) end) {
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.SetCursorPosition(start.x, start.y);
    Console.Write('╔');
    for (int i = start.x; i < end.x - 1; i++) {
        Console.Write('═');
    }
    Console.Write('╗');
    Console.SetCursorPosition(start.x, end.y);
    Console.Write('╚');
    for (int i = start.x; i < end.x - 1; i++) {
        Console.Write('═');
    }
    Console.Write('╝');

    for (int i = start.y + 1; i < end.y; i++) {
        Console.SetCursorPosition(start.x, i);
        Console.Write('║');
        Console.SetCursorPosition(end.x, i);
        Console.Write('║');
    }
    Console.ForegroundColor = ConsoleColor.White;
}

void WriteWithLimit(string text, int limit) {
    if (text.Length < limit) {
        Console.Write(text);
        Console.Write(new String(' ', limit - text.Length));
    } else {
        Console.Write(text.Substring(0, limit));
    }
}

void Tui(DefaultFileProvider provider) {
    Console.CursorVisible = false;
    Console.Clear();

    var defaultBackground = Console.BackgroundColor;
    var defaultForeground = Console.ForegroundColor;

    var winHeight = Console.LargestWindowHeight - 1;
    var winWidth = Console.LargestWindowWidth - 1;

    (int x, int y) filesWinStart = (1, 1);
    (int x, int y) filesWinEnd = ((int)(winWidth * 0.50) - 1, winHeight - 2);
    int filesWinWidth = (int)Math.Abs(filesWinStart.x - filesWinEnd.x) + 1;
    int filesWinHeight = (int)Math.Abs(filesWinStart.y - filesWinEnd.y) + 1;

    (int x, int y) mainWinStart = (filesWinEnd.x + 3, 1);
    (int x, int y) mainWinEnd = (winWidth - 1, filesWinEnd.y);
    int mainWinHeight = (int)Math.Abs(mainWinStart.y - mainWinEnd.y) + 1;
    int mainWinWidth = (int)Math.Abs(mainWinStart.y - mainWinEnd.y) + 1;

    DrawBorder((filesWinStart.x - 1, filesWinStart.y - 1), (filesWinEnd.x + 1, filesWinEnd.y + 1));
    DrawBorder((mainWinStart.x - 1, mainWinStart.y - 1), (mainWinEnd.x + 1, mainWinEnd.y + 1));

    var firstRun = true;
    var files = provider.Files.Keys.Where(e => !e.EndsWith(".uexp")).ToArray();
    var selectedIndex = 0;
    var fileOffset = 0;
    bool shouldExit = false;
    string[] mainWinContent = {
        "Hello, World!",
        "This is a test :)"
    };
    
    while (!shouldExit) {
        if (!firstRun) {
            Console.SetCursorPosition(0, winHeight);
            Console.Write("e = Show Exports | Q = Quit");
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.UpArrow) {
                if (selectedIndex > 0) selectedIndex--;
                if (selectedIndex < fileOffset) {
                    fileOffset--;
                }
            }
            if (keyInfo.Key == ConsoleKey.DownArrow) {
                if (selectedIndex < files.Length) selectedIndex++;
                if (selectedIndex - fileOffset >= filesWinHeight) {
                    fileOffset++;
                }
            }
            if (keyInfo.Key == ConsoleKey.E) {
                if (provider.TryLoadPackage(files[selectedIndex], out var package)) {
                    mainWinContent = JsonConvert.SerializeObject(package.GetExports(), Formatting.Indented).Split("\r\n");
                }
            }

            if (keyInfo.Key == ConsoleKey.Q && (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0) {
                shouldExit = true;
            }
        } else {
            firstRun = false;
        }

        for (int i = 0; i < filesWinHeight; i++) {
            var selectedFileIndex = i + fileOffset;
            Console.SetCursorPosition(filesWinStart.x, filesWinStart.y + i);
            if (selectedIndex == selectedFileIndex) {
                Console.BackgroundColor = defaultForeground;
                Console.ForegroundColor = defaultBackground;
            }
            WriteWithLimit(files[selectedFileIndex], filesWinWidth);
            if (selectedIndex == selectedFileIndex) {
                Console.BackgroundColor = defaultBackground;
                Console.ForegroundColor = defaultForeground;
            }
        }

        for (int i = 0; i < mainWinHeight; i++) {
            // mainWinContent.Length
            Console.SetCursorPosition(mainWinStart.x, mainWinStart.y + i);
            if (i < mainWinContent.Length) {
                WriteWithLimit(mainWinContent[i], mainWinWidth);
            } else {
                Console.Write(new String(' ', mainWinWidth));
            }
        }

        if (shouldExit) {
            Console.SetCursorPosition(0, winHeight);
            Console.Write(new String(' ', winWidth));
            Console.SetCursorPosition(0, winHeight);
        }
    }

Console.CursorVisible = true;
}

void Cli(DefaultFileProvider provider) {
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
    
            var outFilePath = Path.Join(OutputPath, package.Name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(outFilePath) ?? "");
            File.WriteAllText(outFilePath, JsonConvert.SerializeObject(exports, Formatting.Indented));
            Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
        }
    
        else if (inputArgs[0] == "exportfiles") {
            var outFilePath = Path.Join(OutputPath, "Files.txt");
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
            var outFilePath = Path.Join(OutputPath, obj.GetPathName().Split('.')[0] + ".cpp");
            Directory.CreateDirectory(Path.GetDirectoryName(outFilePath) ?? "");
            File.WriteAllText(outFilePath, code);
            Console.WriteLine($"Exported to: {ParsePath(outFilePath)}");
        }
    
        if (singleAction || inputArgs[0] == "exit") {
            break;
        }
    }
}

void Main() {
    var naturalConfigPath = Path.Join(Directory.GetCurrentDirectory(), "CliModel.config");
    if (File.Exists(naturalConfigPath)) {
        Console.WriteLine("Found CliModel.config!");
        LoadConfig(naturalConfigPath);
    }
    
    var launchTui = false;

    for (int i = 0; i < args.Length; i++) {
             if (args[i] == "-MainAes") ConfigMainAes = args[++i];
        else if (args[i] == "-UeVer") ConfigUeVer = args[++i];
        else if (args[i] == "-PaksPath") ConfigPaksPath = args[++i];
        else if (args[i] == "-Config") LoadConfig(args[++i]);
        else if (args[i] == "-Tui") launchTui = true;
        else if (args[i] == "--") {
            ConfigAction = String.Join(' ', args.TakeLast(args.Length - ++i)); //  args[++i];
            Console.WriteLine($"ConfigAction: {ConfigAction}");
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
    
    OutputPath = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "", "Output");
    Directory.CreateDirectory(OutputPath);
    
    var gameDir = ParsePath(ConfigPaksPath, true);
    Console.WriteLine($"Loading UE Version {ConfigUeVer} on path {gameDir}");
    
    var provider = new DefaultFileProvider(gameDir, SearchOption.TopDirectoryOnly, new VersionContainer((EGame)Enum.Parse(typeof(EGame), ConfigUeVer)));
    provider.ReadScriptData = true;
    provider.Initialize();
    
    if (ConfigMainAes != "") {
        provider.SubmitKey(new FGuid(), new FAesKey(ConfigMainAes));
    }
    provider.SubmitKeys(ConfigAesKeys);
  
    if (launchTui) {
        Tui(provider);
    } else {
        Cli(provider);
    }
}


var WineUsername = Environment.GetEnvironmentVariable("WINEUSERNAME");
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

Main();
