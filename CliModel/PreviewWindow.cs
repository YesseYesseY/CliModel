class PreviewWindow : TuiWindow {
    private string Text;

    public PreviewWindow(int x, int y, int width, int height) : base(x, y, width, height) {
        Text = "";
    }

    private void RenderText() {
        using (StringReader reader = new StringReader(Text)) {
            for (int i = 0; i < InnerHeight; i++) {
                Write(reader.ReadLine() ?? ClearWidthStr, i);
            }
        }
    }

    public void Display(string text) {
        Text = text;
        RenderText();
    }

    public override bool Update(ConsoleKeyInfo keyInfo) {
        switch (keyInfo.Key) {
            default:
                break;
        }

        return false;
    }
}
