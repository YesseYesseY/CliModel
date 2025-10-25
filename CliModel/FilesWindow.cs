using CUE4Parse.FileProvider;
using Newtonsoft.Json;

class FilesWindow : TuiWindow {
    public PreviewWindow? previewWin;
    private string[] Files;
    private string[] CurrentFiles;
    private int Selected;
    private int Offset;
    private bool DisplayFullPath;
    public bool Searching;
    public string SearchStr;

    private DefaultFileProvider Provider;

    public FilesWindow(DefaultFileProvider provider, int x, int y, int width, int height) : base(x, y, width, height) {
        Provider = provider;
        Files = Provider.Files.Keys.Where(e => !e.EndsWith(".ubulk")).Where(e => !e.EndsWith(".uexp")).ToArray();
        CurrentFiles = Files.ToArray();
        Selected = 0;
        Searching = false;
        SearchStr = "";
        DisplayFullPath = true;

        UpdateFiles();
    }

    private void UpdateFiles() {
        for (int i = 0; i < InnerHeight; i++) {
            var CurrentIndex = i + Offset;
            if (CurrentIndex < CurrentFiles.Length) {
                if (CurrentIndex == Selected) FlipColors();
                if (DisplayFullPath) {
                    Write(CurrentFiles[CurrentIndex], i);
                } else {
                    Write(CurrentFiles[CurrentIndex].Substring(CurrentFiles[CurrentIndex].LastIndexOf('/') + 1), i);
                }
                if (CurrentIndex == Selected) FlipColors();
            } else {
                Write(ClearWidthStr, i);
            }
        }
    }

    public override bool Update(ConsoleKeyInfo keyInfo) {
        if (Searching) {
            if (keyInfo.Key == ConsoleKey.Enter) {
                Searching = false;
                CurrentFiles = Files.Where(e => e.Contains(SearchStr)).ToArray();
                SearchStr = "";
                if (Selected > CurrentFiles.Length) Selected = CurrentFiles.Length - 1;
                if (Offset > Selected) Offset = Math.Max(Selected - 10, 0);
            } else {
                SearchStr += keyInfo.KeyChar;
                return true;
            }
        }

        switch (keyInfo.Key) {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                if (Selected > 0) Selected--;
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (Selected < CurrentFiles.Length - 1) Selected++;
                break;
            case ConsoleKey.E:
                if (previewWin is not null && Provider.TryLoadPackage(CurrentFiles[Selected], out var package)) {
                    previewWin.Display(JsonConvert.SerializeObject(package.GetExports(), Formatting.Indented));                   
                }
                break;
            case ConsoleKey.P:
                DisplayFullPath = !DisplayFullPath;
                break;
            case ConsoleKey.S:
                Searching = true;
                return true;
            default:
                break;
        }

        if (Selected - Offset >= InnerHeight) {
            Offset++;
        } else if (Selected < Offset) {
            Offset--;
        }

        UpdateFiles();
        return false;
    }
}

