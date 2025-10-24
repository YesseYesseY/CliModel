public class TuiWindow {
    public int X;
    public int Y;
    public int Width;
    public int Height;

    protected string ClearWidthStr;

    private bool _focused;
    public bool Focused {
        get {
            return _focused;
        }
        set {
            var redrawBorder = value != _focused;
            _focused = value;
            if (redrawBorder) DrawBorder();
        }
    }

    protected ConsoleColor DefaultBackground;
    protected ConsoleColor DefaultForeground;

    public int InnerX => X + 1;
    public int InnerY => Y + 1;
    public int InnerWidth => Width - 1;
    public int InnerHeight => Height - 1;

    public TuiWindow(int x, int y, int width, int height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        _focused = false;
        DefaultBackground = Console.BackgroundColor;
        DefaultForeground = Console.ForegroundColor;
        ClearWidthStr = new String(' ', InnerWidth);
        DrawBorder();
    }

    protected void FlipColors() {
        var bgColor = Console.BackgroundColor;
        Console.BackgroundColor = Console.ForegroundColor;
        Console.ForegroundColor = bgColor;
    }

    private void DrawBorder() {
        Console.ForegroundColor = Focused ? ConsoleColor.Blue : ConsoleColor.DarkGray;
        Console.SetCursorPosition(X, Y);
        Console.Write('╔');
        for (int i = X; i < X + Width - 1; i++) {
            Console.Write('═');
        }
        Console.Write('╗');
        Console.SetCursorPosition(X, Y + Height);
        Console.Write('╚');
        for (int i = X; i < X + Width - 1; i++) {
            Console.Write('═');
        }
        Console.Write('╝');
    
        for (int i = Y + 1; i < Y + Height; i++) {
            Console.SetCursorPosition(X, i);
            Console.Write('║');
            Console.SetCursorPosition(X + Width, i);
            Console.Write('║');
        }
        Console.ForegroundColor = DefaultForeground;
    }

    public void Write(string text, int y) {
        Console.SetCursorPosition(InnerX, InnerY + y);
        if (text.Length < InnerWidth) {
            Console.Write(text);
            Console.Write(new String(' ', InnerWidth - text.Length));
        } else {
            Console.Write(text.Substring(0, InnerWidth));
        }
    }

    public virtual bool Update(ConsoleKeyInfo keyInfo) {
        return false;
    }
}
