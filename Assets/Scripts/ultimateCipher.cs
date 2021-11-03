using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltimateCipher;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class ultimateCipher : cipherBase
{
    protected override string Name { get { return "Ultimate"; } }

    private static readonly CipherInfo[] ciphers = newArray(
        new CipherInfo("Black", c => c.blackcipher, 16),
        new CipherInfo("Blue", c => c.bluecipher, 10),
        new CipherInfo("Brown", c => c.browncipher, 17),
        new CipherInfo("Cornflower", c => c.cornflowercipher, 16),
        new CipherInfo("Forest", c => c.forestcipher, 16),
        new CipherInfo("Gray", c => c.graycipher, 13),
        new CipherInfo("Green", c => c.greencipher, 10),
        new CipherInfo("Indigo", c => c.indigocipher, 17),
        new CipherInfo("Maroon", c => c.marooncipher, 16),
        new CipherInfo("Orange", c => c.orangecipher, 17),
        new CipherInfo("Red", c => c.redcipher, 14),
        new CipherInfo("Violet", c => c.violetcipher, 14),
        new CipherInfo("White", c => c.whitecipher, 14),
        new CipherInfo("Yellow", c => c.yellowcipher, 16));

    // Objects that we need to modify at run-time
    public MeshRenderer background;
    public MeshRenderer[] screens;
    public MeshRenderer[] screentextmat;

    // Values that we need to set stuff to
    public Material[] backgroundcolors; // must match the order in ‘ciphers’
    public Material ultimateCipherBackground;
    public Material pinkCipherBackground;
    public Material cyanCipherBackground;
    public Material blackScreensAndButtons;
    public Material whiteScreensAndButtons;
    public Font standardFont;
    public Font pigpenFont;
    public Material standardFontMat;
    public Material pigpenFontMat;

    private static int moduleIdCounter = 1;

    private CipherResult ultimateCipherResult;
    private CipherResult pinkCipherResult;
    private CipherResult cyanCipherResult;
    private CipherResult trueUltimateCipherResult;

    private Mode mode = Mode.Normal;
    private int tpPoints;

    private static bool playingLongAudio;
    private bool playingAnimation;

    protected override int? TPPoints
    {
        get
        {
            switch (mode)
            {
                case Mode.PinkUC:
                case Mode.CyanUC:
                    return ciphers.Sum(c => c.TpPoints);

                case Mode.TrueUC:
                    return ciphers.Sum(c => c.TpPoints) * 2;

                case Mode.Debug:
                    return 0;

                default:
                    return tpPoints;
            }
        }
    }

    protected override void Initialize()
    {
        moduleId = moduleIdCounter++;
        wordList[2].Remove("CANCEL");
        wordList[2].Remove("PRISSY");
        setNormalMode();
    }

    private void setNormalMode()
    {
        if (ultimateCipherResult == null)
        {
            var useCiphers = ciphers.ToArray().Shuffle().Where(i => i != null).Take(3).ToArray();
            tpPoints = useCiphers.Sum(c => c.TpPoints);
            ultimateCipherResult = GeneratePages("Ultimate Cipher", ultimateCipherBackground, useCiphers.Select(c => c.GetSpecific(Rnd.Range(0, 2) != 0)).ToArray());
        }
        setMode(Mode.Normal, ultimateCipherResult);
    }

#if UNITY_EDITOR
    private void setDebugMode()
    {
        setMode(Mode.Debug, GeneratePages("Debug Cipher", ultimateCipherBackground, ciphers.Where(c => c.Name == "Forest").Select(c => c.GetSpecific(inverted: true)).ToArray()));
    }
#endif

    private void setPinkCipherMode()
    {
        // Pink Cipher: all non-inverted ciphers
        if (pinkCipherResult == null)
            pinkCipherResult = GeneratePages("Pink Cipher", pinkCipherBackground, ciphers.Select(c => c.GetSpecific(inverted: false)).ToArray().Shuffle());
        setMode(Mode.PinkUC, pinkCipherResult);
    }

    private void setCyanCipherMode()
    {
        // Cyan Cipher: all inverted ciphers
        if (cyanCipherResult == null)
            cyanCipherResult = GeneratePages("Cyan Cipher", cyanCipherBackground, ciphers.Select(c => c.GetSpecific(inverted: true)).ToArray().Shuffle());
        setMode(Mode.CyanUC, cyanCipherResult);
    }

    private void setTrueUCMode()
    {
        // True Ultimate Cipher: every cipher, inverted and non
        if (trueUltimateCipherResult == null)
        {
            trueUltimateCipherResult = GeneratePages("True Ultimate Cipher", ultimateCipherBackground,
                ciphers.Select(c => c.GetSpecific(inverted: false)).Concat(ciphers.Select(c => c.GetSpecific(inverted: true))).ToArray().Shuffle());
            StartCoroutine(trueUltimateCipherAnimation());
        }
        else
            setMode(Mode.TrueUC, trueUltimateCipherResult);
    }

    private void setMode(Mode newMode, CipherResult cr)
    {
        mode = newMode;
        pages = cr.Pages;
        answer = cr.Answer;
        page = 0;
        getScreens();
    }

    private CipherResult GeneratePages(string cipherType, Material frontPageBackground, IEnumerable<SpecificCipherInfo> ciphersToUse)
    {
        var newAnswer = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(newAnswer.ToUpperInvariant());

        Debug.LogFormat("[Ultimate Cipher #{0}] ----------------------------------------", moduleId);
        Debug.LogFormat("[Ultimate Cipher #{0}] Begin {1}", moduleId, cipherType);
        Debug.LogFormat("[Ultimate Cipher #{0}] ----------------------------------------", moduleId);
        Debug.LogFormat("[Ultimate Cipher #{0}] Generated Word: {1}", moduleId, newAnswer);

        var pages = new List<PageInfo>();

        var nextWord = newAnswer;
        foreach (var cipher in ciphersToUse)
        {
            Debug.LogFormat("[Ultimate Cipher #{0}] ----------------------------------------", moduleId);
            Debug.LogFormat("[Ultimate Cipher #{0}] Begin {1}", moduleId, cipher.FullName);
            Debug.LogFormat("[Ultimate Cipher #{0}] ----------------------------------------", moduleId);
            var newPages = cipher.RunCipher(this, nextWord);
            nextWord = newPages[0].Screens[0].Text;
            newPages[0].Screens[0].Text = "";
            foreach (var page in newPages)
            {
                page.BackgroundMaterial = backgroundcolors[cipher.Index];
                page.Inverted = cipher.Inverted;
            }
            pages.InsertRange(0, newPages);
        }

        pages.Insert(0, new PageInfo(new ScreenText(nextWord, 40)) { PigpenFont = true, BackgroundMaterial = frontPageBackground });
        return new CipherResult(newAnswer, pages.ToArray());
    }

    protected override void getScreens()
    {
        submitText.text = (page + 1).ToString();

        var curPage = pages[page];
        for (var i = 0; i < curPage.Screens.Length; i++)
        {
            screens[i].sharedMaterial = curPage.Inverted ? whiteScreensAndButtons : blackScreensAndButtons;
            screenTexts[i].text = curPage.Screens[i].Text ?? "";
            screenTexts[i].fontSize = curPage.Screens[i].FontSize;
            screenTexts[i].color = curPage.Inverted ? Color.black : Color.white;
            screenTexts[i].font = curPage.PigpenFont ? pigpenFont : standardFont;
            screentextmat[i].sharedMaterial = curPage.PigpenFont ? pigpenFontMat : standardFontMat;
            screenTexts[i].gameObject.SetActive(true);
        }
        for (var i = curPage.Screens.Length; i < 3; i++)
        {
            screenTexts[i].gameObject.SetActive(false);
            screens[i].sharedMaterial = curPage.Inverted ? whiteScreensAndButtons : blackScreensAndButtons;
        }

        arrowTexts[0].text = (curPage.LeftArrow ?? '<').ToString();
        arrowTexts[1].text = (curPage.RightArrow ?? '>').ToString();

        background.sharedMaterial = curPage.BackgroundMaterial;
    }

    protected override void submitWord(KMSelectable submitButton)
    {
        if (moduleSolved)
            return;

        submitButton.AddInteractionPunch(1f);

        if (screenTexts[2].text.Equals("PINKUC"))
            setPinkCipherMode();

        else if (screenTexts[2].text.Equals("CYANUC"))
            setCyanCipherMode();

        else if (screenTexts[2].text.Equals("TRUEUC"))
            setTrueUCMode();

        else if (screenTexts[2].text.Equals("CANCEL"))
            setNormalMode();

#if UNITY_EDITOR
        else if (screenTexts[2].text.Length == 1)
            setDebugMode();
#endif

        else if (screenTexts[2].text.Equals("MUSICA"))
        {
            page = 0;
            getScreens();
            if (!playingLongAudio)
            {
                Audio.PlaySoundAtTransform("A Choice", transform);
                StartCoroutine(songTime(818f));
            }
        }
        else if (screenTexts[2].text.Equals("IKTPQN"))
        {
            submitText.text = "<3";
            for (var i = 0; i < 3; i++)
            {
                screenTexts[i].font = standardFont;
                screentextmat[i].material = standardFontMat;
                screenTexts[i].color = Color.white;
                screens[i].sharedMaterial = blackScreensAndButtons;
                screenTexts[i].gameObject.SetActive(true);
            }
            screenTexts[0].fontSize = 35;
            screenTexts[1].fontSize = 40;
            screenTexts[2].fontSize = 25;
            screenTexts[0].text = "A-II-I-VI";
            screenTexts[1].text = "PTG";
            screenTexts[2].text = "SE-AN";
        }
        else if (screenTexts[2].text.Equals("PRISSY"))
        {
            submitText.text = "<3";
            for (var i = 0; i < 3; i++)
            {
                screenTexts[i].font = standardFont;
                screentextmat[i].material = standardFontMat;
                screenTexts[i].color = Color.white;
                screens[i].sharedMaterial = blackScreensAndButtons;
                screenTexts[i].gameObject.SetActive(true);
            }
            screenTexts[0].fontSize = 40;
            screenTexts[1].fontSize = 40;
            screenTexts[2].fontSize = 40;
            screenTexts[0].text = "THANK";
            screenTexts[1].text = "YOU FOR";
            screenTexts[2].text = "PLAYING";
            if (!playingLongAudio)
            {
                Audio.PlaySoundAtTransform("A Life's Story", transform);
                StartCoroutine(songTime(307f));
            }
        }
        else if (screenTexts[2].text.Equals(answer))
        {
            switch (mode)
            {
                case Mode.PinkUC:
                    background.sharedMaterial = pinkCipherBackground;
                    screenTexts[0].text = "TRU";
                    screenTexts[1].text = "";
                    screenTexts[2].text = "";
                    break;

                case Mode.CyanUC:
                    background.sharedMaterial = cyanCipherBackground;
                    screenTexts[0].text = "EUC";
                    screenTexts[1].text = "";
                    screenTexts[2].text = "";
                    break;

                case Mode.TrueUC:
                    background.sharedMaterial = ultimateCipherBackground;
                    screenTexts[0].text = "IKTPQN";
                    screenTexts[1].text = "";
                    screenTexts[2].text = "";
                    break;

                default:
                    background.sharedMaterial = ultimateCipherBackground;
                    screenTexts[0].text = "PINKUC";
                    screenTexts[1].text = "CYANUC";
                    screenTexts[2].text = "";
                    break;
            }
            for (var i = 0; i < 3; i++)
            {
                screenTexts[i].fontSize = 40;
                screenTexts[i].font = standardFont;
                screentextmat[i].material = standardFontMat;
                screenTexts[i].color = Color.white;
                screens[i].sharedMaterial = blackScreensAndButtons;
                screenTexts[i].gameObject.SetActive(true);
            }
            Audio.PlaySoundAtTransform("UCSolveSFX", transform);
            module.HandlePass();
            moduleSolved = true;
        }
        else
        {
            Audio.PlaySoundAtTransform("StrikeSFX", transform);
            module.HandleStrike();
            page = 0;
            getScreens();
        }
        submitScreen = false;
    }

    protected override void left(KMSelectable arrow)
    {
        if (!playingAnimation)
            base.left(arrow);
    }

    protected override void right(KMSelectable arrow)
    {
        if (!playingAnimation)
            base.right(arrow);
    }

    protected override void letterPress(KMSelectable pressed)
    {
        base.letterPress(pressed);
        for (var i = 0; i < 3; i++)
        {
            screens[i].sharedMaterial = blackScreensAndButtons;
            screenTexts[i].font = standardFont;
            screentextmat[i].sharedMaterial = standardFontMat;
            screenTexts[i].color = Color.white;
            screenTexts[i].gameObject.SetActive(true);
        }
        background.sharedMaterial = ultimateCipherBackground;
    }

    private IEnumerator trueUltimateCipherAnimation()
    {
        playingAnimation = true;
        for (int aa = 0; aa < 8; aa++)
        {
            switch (aa)
            {
                case 1:
                    screenTexts[0].text = "WELCOME";
                    break;
                case 2:
                    screenTexts[1].text = "TO";
                    break;
                case 3:
                    screenTexts[2].text = "THE";
                    break;
                case 4:
                    screenTexts[0].text = "";
                    screenTexts[1].text = "";
                    screenTexts[2].text = "";
                    break;
                case 5:
                    screenTexts[0].text = "TRUE";
                    break;
                case 6:
                    screenTexts[1].text = "ULTIMATE";
                    break;
                case 7:
                    screenTexts[2].text = "CIPHER";
                    break;
            }
            for (int cipherIx = 0; cipherIx < ciphers.Length; cipherIx++)
            {
                background.sharedMaterial = backgroundcolors[cipherIx];
                yield return new WaitForSeconds(0.12f);
            }
        }
        setMode(Mode.TrueUC, trueUltimateCipherResult);
        playingAnimation = false;
    }

    private IEnumerator songTime(float time)
    {
        playingLongAudio = true;
        yield return new WaitForSeconds(time);
        playingLongAudio = false;
    }

    protected override void Log(string cipher, string message)
    {
        Debug.LogFormat("[Ultimate Cipher #{1}] [{0}] {2}", cipher, moduleId, message);
    }

    static ultimateCipher()
    {
        for (var i = 0; i < ciphers.Length; i++)
            if (ciphers[i] != null)
                ciphers[i] = ciphers[i].WithIndex(i);
    }
}