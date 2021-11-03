namespace UltimateCipher
{
    public sealed class CipherResult
    {
        public string Answer { get; private set; }
        public PageInfo[] Pages { get; private set; }

        public CipherResult(string answer, PageInfo[] pages)
        {
            Answer = answer;
            Pages = pages;
        }
    }
}
