using CUE4Parse.FileProvider;
using Newtonsoft.Json;

class FilesWindow : TuiWindow {
    public PreviewWindow? previewWin;
    private string[] Files;
    private int Selected;
    private int Offset;
    private DefaultFileProvider Provider;

    public FilesWindow(DefaultFileProvider provider, int x, int y, int width, int height) : base(x, y, width, height) {
        Provider = provider;
        Files = Provider.Files.Keys.ToArray();
        Selected = 0;

        UpdateFiles();
    }

    private void UpdateFiles() {
        for (int i = 0; i < InnerHeight && i + Offset < Files.Length; i++) {
            var CurrentIndex = i + Offset;
            if (CurrentIndex == Selected) FlipColors();
            Write(Files[CurrentIndex], i);
            if (CurrentIndex == Selected) FlipColors();
        }
    }

    public override bool Update(ConsoleKeyInfo keyInfo) {
        switch (keyInfo.Key) {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                if (Selected > 0) Selected--;
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (Selected < Files.Length - 1) Selected++;
                break;
            case ConsoleKey.E:
                if (previewWin is not null && Provider.TryLoadPackage(Files[Selected], out var package)) {
                    previewWin.Display(JsonConvert.SerializeObject(package.GetExports(), Formatting.Indented));                   
                }
                break;
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

