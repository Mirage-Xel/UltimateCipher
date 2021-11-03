public class indigoCipher : cipherBase
{
    protected override string Name { get { return "Indigo"; } }

    static int moduleIdCounter = 1;

    protected override void Initialize()
    {
        moduleId = moduleIdCounter++;
        answer = wordList[2][UnityEngine.Random.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(answer.ToUpperInvariant());
        Log("INDIGO", "Generated Word: {0}", answer);
        pages = indigocipher(answer);
    }
}
