using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System;
using System.Linq;
using Words;

using Rnd = UnityEngine.Random;
using System.Runtime.Remoting.Messaging;

public class vermilionCipher : MonoBehaviour
{
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
        Debug.LogFormat("[Vermilion Cipher #{0}] Answer: {1}", moduleId, answer);

        pages = Enumerable.Range(0, 2).Select(i => Enumerable.Repeat("", 3).ToArray()).ToArray();
        var encrypted = vermilioncipher(answer);
        pages[0][0] = encrypted;
        page = 0;
        getScreens();
    }

    string vermilioncipher(string word)
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
            Debug.Log(leftList.Concat(rightList).Join(""));
            leftList = leftList.Select(x => (char)(((x + keyB[i]) % 26) + 'A')).ToList();
            word = leftList.Concat(rightList).Join("");
            Debug.Log(word);
        }
        Debug.LogFormat("[Vermilion Cipher #{0}] Caesar Shuffle Cipher keys: {1}, {2}", moduleId, keyA, keyB);
        Debug.LogFormat("[Vermilion Cipher #{0}] After Caesar Shuffle Cipher: {1}", moduleId, word);
        pages[0][1] = keyA;
        pages[0][2] = keyB;
        return word;
    }


    private int[] sequencing(string str)
    {
        return str.Select((ch, ix) => str.Count(c => c < ch) + str.Take(ix).Count(c => c == ch)).ToArray();
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
        screenTexts[0].fontSize = page == 0 ? 40 : 45;
        screenTexts[1].fontSize = page == 0 ? 45 : 35;
        screenTexts[2].fontSize = page == 0 ? 40 : 35;
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