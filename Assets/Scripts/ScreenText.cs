namespace UltimateCipher
{
    public struct ScreenText
    {
        public string Text { get; set; }
        public int FontSize { get; private set; }

        public ScreenText(string text, int fontSize)
        {
            Text = text;
            FontSize = fontSize;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
