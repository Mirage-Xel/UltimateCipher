using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System;
using System.Linq;
using Words;

using Rnd = UnityEngine.Random;
using System.Runtime.Remoting.Messaging;

static class ListHelpers
{

    public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    public static List<List<T>> Transpose<T>(this List<List<T>> lists)
    {
        var longest = lists.Any() ? lists.Max(l => l.Count) : 0;
        List<List<T>> outer = new List<List<T>>(longest);
        for (int i = 0; i < longest; i++)
            outer.Add(new List<T>(lists.Count));
        for (int j = 0; j < lists.Count; j++)
            for (int i = 0; i < longest; i++)
                outer[i].Add(lists[j].Count > i ? lists[j][i] : default(T));
        return outer;
    }

    public static List<int> IndexOf<T>(this List<List<T>> nestedList, T value)
    {
        for (int i = 0; i < nestedList.Count; i++)
        {
           List<T> list = nestedList[i];
            for (int j = 0; j < list.Count; j++)
                if (list[j].Equals(value))
                {
                    return new List<int>() { i, j };
                }
        }
        return new List<int>() { -1, -1 };
    }
    public static List<T> RotateIgnoringSome<T> (this List<T> source, int amount, List<T> ignoredValues)
    {
        List<T> removedValues = new List<T>();
        List<int> positions = new List<int>();
        for (int i = source.Count - 1; i > -1; i--)
            if(ignoredValues.Contains(source[i]))
            {            
                removedValues.Add(source[i]);
                positions.Add(i);
                source.RemoveAt(i);
            }
        amount = source.Count - amount;
        source = source.Skip(amount).Concat(source.Take(amount)).ToList();
        for (int i = removedValues.Count - 1 ; i > -1; i--)
            source.Insert(positions[i], removedValues[i]);
        return source;
    }

}
public class crimsonCipher : MonoBehaviour
{
    private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public TextMesh[] screenTexts;
    public KMBombInfo Bomb;
    public KMBombModule module;
    public AudioClip[] sounds;
    public KMAudio Audio;
    public TextMesh submitText;

    public KMSelectable leftArrow;
    public KMSelectable rightArrow;
    public KMSelectable submit;
    public KMSelectable[] keyboard;

    private string[][] pages;
    private List<List<string>> wordList;
    private string answer;
    private int page;
    private bool submitScreen;
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        leftArrow.OnInteract += delegate () { left(leftArrow); return false; };
        rightArrow.OnInteract += delegate () { right(rightArrow); return false; };
        submit.OnInteract += delegate () { submitWord(submit); return false; };
        foreach (KMSelectable keybutton in keyboard)
        {
            KMSelectable pressedButton = keybutton;
            pressedButton.OnInteract += delegate () { letterPress(pressedButton); return false; };
        }
    }

    void Start()
    {
        wordList = new Data().allWords;
        // Answer is always 6 letters long
        answer = pickWord(6);
        Debug.LogFormat("[Crimson Cipher #{0}] Answer: {1}", moduleId, answer);
        pages = Enumerable.Range(0, 3).Select(i => Enumerable.Repeat("", 3).ToArray()).ToArray();
        var encrypted = crimsoncipher(answer);
        pages[0][0] = encrypted;
        page = 0;
        getScreens();
    }

    string crimsoncipher(string word)
    {

        //Casear Shuffle Cipher
        string keyA = pickWord(5);
        string keyB = pickWord(5);
        int pivPos;
        for (int i = 4; i > -1; i--)
        {
            pivPos = (keyA[i] - '@') % 5 + 1;
            List<char> rightList = word.Take(pivPos).ToList();
            List<char> leftList = word.Skip(pivPos).ToList();
            Debug.Log(leftList.Join("") + "|" + rightList.Join(""));
            leftList = leftList.Select(x => (char)(((x + keyB[i] + 1) % 26) + 'A')).ToList();
            Debug.Log(leftList.Join("") + "|" + rightList.Join(""));
            word = leftList.Concat(rightList).Join("");
        }
        Debug.LogFormat("[Crimson Cipher #{0}] Caesar Shuffle Cipher keys: {1}, {2}", moduleId, keyA, keyB);
        Debug.LogFormat("[Crimson Cipher #{0}] After Caesar Shuffle Cipher: {1}", moduleId, word);
        pages[2][0] = keyA;
        pages[2][1] = keyB;
        //Dual Triplex Reflector Cipher
        string keyC = pickWord(5, 8);
        string keyD = pickWord(5, 8);
        string keyE = pickWord(5);
        pages[1][0] = keyC;
        pages[1][1] = keyD;
        pages[1][2] = keyE;
        List<List<char>> topReflector = constructTriplexReflector(keyD);
        List<List<char>> bottomReflector = constructTriplexReflector(keyC);
        string dtrCipherResult = "";
        for (int i = 0; i < 6; i++)
        {
            Debug.Log(topReflector.Select(x => x.Join("")).Join("\n") + "\n---------\n" + bottomReflector.Select(x => x.Join("")).Join("\n"));
            char intermediateLetterA = bottomReflector[topReflector.IndexOf(word[i])[0]][topReflector.IndexOf(word[i])[1]];
            char intermediateLetterB = bottomReflector[topReflector.IndexOf(intermediateLetterA)[0]][topReflector.IndexOf(intermediateLetterA)[1]];
            char encryptedLetter = bottomReflector[topReflector.IndexOf(intermediateLetterB)[0]][topReflector.IndexOf(intermediateLetterB)[1]];
            Debug.Log(word[i] + " -> " + intermediateLetterA + " -> " + intermediateLetterB + " -> " + encryptedLetter);
            dtrCipherResult += encryptedLetter;
            if (i == 5)
                break;
            Debug.Log(keyE[i] + " = " + ((keyE[i] - '@') / 9).ToString() + ((keyE[i] - '@') / 3 % 3).ToString() + ((keyE[i] - '@') % 3).ToString() + " = " + ((keyE[i] - '@') / 9).ToString() + " " + ((keyE[i] - '@') / 3 % 3).ToString() + ((keyE[i] - '@') % 3).ToString() + " = " + ((keyE[i] - '@') / 9).ToString() + ((keyE[i] - '@') / 3 % 3).ToString() + " " + ((keyE[1] - '@') % 3).ToString());
            List<int> indexA = bottomReflector.IndexOf(intermediateLetterA);
            List<int> indexB = topReflector.IndexOf(intermediateLetterB);
            bottomReflector = bottomReflector.Transpose();
            bottomReflector[indexA[1]] = bottomReflector[indexA[1]].RotateIgnoringSome((keyE[i] - '@') / 9, new List<char>() { ' ' });
            bottomReflector = bottomReflector.Transpose();
            bottomReflector[indexA[0]] = bottomReflector[indexA[0]].RotateIgnoringSome((keyE[i] - '@') % 9, new List<char>() { ' ' });
            topReflector[indexA[0]] = topReflector[indexA[0]].RotateIgnoringSome((keyE[i] - '@') / 3, new List<char>() { ' ' });
            topReflector = topReflector.Transpose();
            topReflector[indexA[1]] = topReflector[indexA[1]].RotateIgnoringSome((keyE[i] - '@') % 3, new List<char>() { ' ' });
            topReflector = topReflector.Transpose();
        }
        word = dtrCipherResult;
        //Transposed Halved Polybius Cipher
        string keyF = pickWord(5, 8);
        string keyG = pickWord(7);
        pages[0][1] = keyF;
        pages[0][2] = keyG;
        return word;
       
    }

    private List<List<char>> constructTriplexReflector (string key)
    {
        List<char> flatReflector = (key + alphabet).ToList().Distinct().ToList();
        flatReflector.Insert(13, ' ');
        return flatReflector.ChunkBy(9);
     }

    private string pickWord(int length)
    {
        var wl = wordList[length - 4];
        var ix = Rnd.Range(0, wl.Count);
        var word = wl[ix];
        wl.RemoveAt(ix);
        return word;
    }

    private string pickWord(int minLength, int maxLength)
    {
        return pickWord(Rnd.Range(minLength, maxLength + 1));
    }

    void left(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform(sounds[0].name, transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page + pages.Length - 1) % pages.Length;
            getScreens();
        }
    }

    void right(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform(sounds[0].name, transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page + 1) % pages.Length;
            getScreens();
        }
    }

    private void getScreens()
    {
        submitText.text = (page + 1) + "";
        screenTexts[0].text = pages[page][0];
        screenTexts[1].text = pages[page][1];
        screenTexts[2].text = pages[page][2];
        foreach (TextMesh i in screenTexts)
             i.fontSize = page == 2 ? 40 : 35;       
    }

    void submitWord(KMSelectable submitButton)
    {
        if (!moduleSolved)
        {
            submitButton.AddInteractionPunch();
            if (screenTexts[2].text.Equals(answer))
            {
                Audio.PlaySoundAtTransform(sounds[2].name, transform);
                module.HandlePass();
                moduleSolved = true;
                screenTexts[2].text = "";
            }
            else
            {
                Audio.PlaySoundAtTransform(sounds[3].name, transform);
                module.HandleStrike();
                page = 0;
                getScreens();
                submitScreen = false;
            }
        }
    }

    void letterPress(KMSelectable pressed)
    {
        if (!moduleSolved)
        {
            pressed.AddInteractionPunch();
            Audio.PlaySoundAtTransform(sounds[1].name, transform);
            if (submitScreen)
            {
                if (screenTexts[2].text.Length < 6)
                    screenTexts[2].text = screenTexts[2].text + pressed.GetComponentInChildren<TextMesh>().text;
            }
            else
            {
                submitText.text = "SUB";
                screenTexts[0].text = "";
                screenTexts[1].text = "";
                screenTexts[2].text = pressed.GetComponentInChildren<TextMesh>().text;
                screenTexts[2].fontSize = 40;
                submitScreen = true;
            }
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} right/left/r/l [move between screens] | !{0} submit answerword";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {

        if (command.EqualsIgnoreCase("right") || command.EqualsIgnoreCase("r"))
        {
            yield return null;
            rightArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);

        }
        if (command.EqualsIgnoreCase("left") || command.EqualsIgnoreCase("l"))
        {
            yield return null;
            leftArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length != 6) yield break;
        int[] buttons = split[1].Select(getPositionFromChar).ToArray();
        if (buttons.Any(x => x < 0)) yield break;

        yield return null;

        yield return new WaitForSeconds(0.1f);
        foreach (char let in split[1])
        {
            keyboard[getPositionFromChar(let)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.1f);
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (submitScreen && !answer.StartsWith(screenTexts[2].text))
        {
            KMSelectable[] arrows = new KMSelectable[] { leftArrow, rightArrow };
            arrows[UnityEngine.Random.Range(0, 2)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        int start = submitScreen ? screenTexts[2].text.Length : 0;
        for (int i = start; i < 6; i++)
        {
            keyboard[getPositionFromChar(answer[i])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    private int getPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }
}