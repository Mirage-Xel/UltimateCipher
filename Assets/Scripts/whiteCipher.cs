public class whiteCipher : cipherBase
{
    protected override string Name { get { return "White"; } }

    static int moduleIdCounter = 1;

    protected override void Initialize()
    {
        moduleId = moduleIdCounter++;
        answer = wordList[2][UnityEngine.Random.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(answer.ToUpperInvariant());
        Log("WHITE", "Generated Word: {0}", answer);
        pages = whitecipher(answer);
    }
}
