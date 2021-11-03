using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UltimateCipher;
using UnityEngine;
using Words;

using Rnd = UnityEngine.Random;

public abstract class cipherBase : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule module;
    public KMAudio Audio;
    public TextMesh[] screenTexts;
    public TextMesh submitText;
    public KMSelectable leftArrow;
    public KMSelectable rightArrow;
    public KMSelectable submit;
    public KMSelectable[] keyboard;
    public TextMesh[] arrowTexts;

    protected abstract string Name { get; }

    // The Colored Ciphers will return null here to signify that TP’s score should be used.
    // Ultimate Cipher will return a non-null value, which will cause the TP handler to award those points.
    protected virtual int? TPPoints { get { return null; } }

    protected List<List<string>> wordList;

    protected static T[] newArray<T>(params T[] p) { return p; }

    private static readonly string[] morse = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.." };
    private static readonly string[] brailleDots = { "1", "12", "14", "145", "15", "124", "1245", "125", "24", "245", "13", "123", "134", "1345", "135", "1234", "12345", "1235", "234", "2345", "136", "1236", "2456", "1346", "13456", "1356" };
    private static readonly string[] huffmanBinary = { "00000", "00001", "00010", "00011", "00100", "00101", "00110", "00111", "01000", "01001", "01010", "01011", "01100", "01101", "01110", "01111", "10000", "10001", "10010", "10011", "1010", "1011", "1100", "1101", "1110", "1111" };
    private static readonly int[][] semaphores = "45;46;47;04;14;24;34;56;57;02;05;15;25;35;67;06;16;26;36;07;17;03;12;13;27;23".Split(';').Select(str => str.Select(ch => ch - '0').ToArray()).ToArray();

    private string getKey(string kw, string alphabet, bool kwFirst)
    {
        return (kwFirst ? (kw + alphabet) : alphabet.Except(kw).Concat(kw)).Distinct().Join("");
    }

    #region Ciphers
    #region Black Cipher
    protected PageInfo[] blackcipher(string word, bool invert = false)
    {
        if (invert)
        {
            Log("INV BLACK", "Begin Digrafid Encryption");
            var digrafidResult = DigrafidEnc(word.ToUpperInvariant(), invert);
            Log("INV BLACK", "Begin Scytale Transposition");
            var scytaleResult = ScytaleTrans(digrafidResult.Encrypted, invert);
            Log("INV BLACK", "Begin Enigma Encryption");
            var enigmaResult = EnigmaEnc(scytaleResult.ToUpperInvariant(), invert);

            return newArray(
                new PageInfo(digrafidResult.ArrowLeft, digrafidResult.ArrowRight, new ScreenText(enigmaResult.Encrypted, 40), digrafidResult.Text1, digrafidResult.Text2),
                new PageInfo(digrafidResult.ArrowLeft, digrafidResult.ArrowRight, enigmaResult.Screen1, enigmaResult.Screen2, enigmaResult.Screen3));
        }
        else
        {
            Log("BLACK", "Begin Enigma Encryption");
            var enigmaResult = EnigmaEnc(word.ToUpperInvariant(), invert);
            Log("BLACK", "Begin Scytale Transposition");
            var scytaleResult = ScytaleTrans(enigmaResult.Encrypted.ToUpperInvariant(), invert);
            Log("BLACK", "Begin Digrafid Encryption");
            var digrafidResult = DigrafidEnc(scytaleResult.ToUpperInvariant(), invert);

            return newArray(
                new PageInfo(digrafidResult.ArrowLeft, digrafidResult.ArrowRight, new ScreenText(digrafidResult.Encrypted, 40), digrafidResult.Text1, digrafidResult.Text2),
                new PageInfo(digrafidResult.ArrowLeft, digrafidResult.ArrowRight, enigmaResult.Screen1, enigmaResult.Screen2, enigmaResult.Screen3));
        }
    }
    struct EnigmaResult { public string Encrypted; public ScreenText Screen1, Screen2, Screen3; }
    EnigmaResult EnigmaEnc(string word, bool invert = false)
    {
        string[][] rotors =
        {
            new string[]
            {
                "EKMFLGDQVZNTOWYHXUSPAIBRCJ",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "DQ"
            },
            new string[]
            {
                "AJDKSIRUXBLHWTMCQGZNPYFVOE",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "ER"
            },
            new string[]
            {
                "BDFHJLCPRTXVZNYEIWGAKMUSQO",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "IV"
            },
            new string[]
            {
                "ESOVPZJAYQUIRHXLNFTGKDCMWB",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "JW"
            },
            new string[]
            {
                "VZBRGITYUPSDNHLXAWMJQOFECK",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "ZM"
            },
            new string[]
            {
                "JPGVOUMFYQBENHZRDKASXLICTW",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "LY"
            },
            new string[]
            {
                "NZJHGRCXMYSWBOUFAIVLPEKQDT",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "HU"
            },
            new string[]
            {
                "FKQHTLXOCBJSPDZRAMEWNIUYGV",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "CP"
            }
        };
        string[][] reflectors =
        {
            new string[]
            {
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "LUSNPQOMJIYAHDGEFXCVBTZRKW"
            },
            new string[]
            {
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "XQUMFEPOWLTJDZHGBVYKCRIASN"
            },
            new string[]
            {
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "ESKOAQMJYHCPGTDLFUBNRXZVIW"
            }
        };
        string[] roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };
        string rotornums = "01234567";
        string reflectorlets = "ABC";
        string[][] rotorsetup = new string[5][];

        //Generate Reflector
        int num = Rnd.Range(0, 3);
        rotorsetup[0] = reflectors[num];
        string rotorconfig = reflectorlets[num] + "";
        string rotorlets = "";

        //Generate Rotors
        for (int aa = 0; aa < 3; aa++)
        {
            char pickedrotor = rotornums[Rnd.Range(0, rotornums.Length)];
            rotornums = rotornums.Replace(pickedrotor + "", "");
            rotorsetup[aa + 1] = rotors[pickedrotor - '0'];
            rotorconfig = rotorconfig + "-" + roman[pickedrotor - '0'];
            num = Rnd.Range(0, 26);
            rotorsetup[aa + 1][0] = rotorsetup[aa + 1][0].Substring(num) + "" + rotorsetup[aa + 1][0].Substring(0, num);
            rotorsetup[aa + 1][1] = rotorsetup[aa + 1][1].Substring(num) + "" + rotorsetup[aa + 1][1].Substring(0, num);
            rotorlets = rotorlets + "" + rotorsetup[aa + 1][1][0];
        }
        rotorsetup[4] = new string[]
        {
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        };

        //Generating Plugboard
        string plugboard = "";
        int numletterswaps = Rnd.Range(3, 6);
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int aa = 0; aa < numletterswaps; aa++)
        {
            char l1 = alphabet[Rnd.Range(0, alphabet.Length)];
            alphabet = alphabet.Replace(l1 + "", "");
            char l2 = alphabet[Rnd.Range(0, alphabet.Length)];
            alphabet = alphabet.Replace(l2 + "", "");
            plugboard = string.Concat(plugboard, l1, l2, "-");
            for (int bb = 0; bb < 2; bb++)
            {
                rotorsetup[4][bb] = rotorsetup[4][bb].Replace(l1, '1');
                rotorsetup[4][bb] = rotorsetup[4][bb].Replace(l2, l1);
                rotorsetup[4][bb] = rotorsetup[4][bb].Replace('1', l2);
            }
        }
        plugboard = plugboard.Substring(0, plugboard.Length - 1);
        if (invert)
        {
            Log("INV BLACK", "Rotor Config: {0}", rotorconfig);
            Log("INV BLACK", "Rotor Letters: {0}", rotorlets);
            Log("INV BLACK", "Plugboard: {0}", plugboard);
        }
        else
        {
            Log("BLACK", "Rotor Config: {0}", rotorconfig);
            Log("BLACK", "Rotor Letters: {0}", rotorlets);
            Log("BLACK", "Plugboard: {0}", plugboard);
        }
        //Start Encrypting
        string encrypt = "";
        for (int cc = 0; cc < 6; cc++)
        {
            //Letter Encryption
            char let = word[cc];
            string logoutput = let + "";
            for (int dd = rotorsetup.Length - 1; dd > 0; dd--)
            {
                let = rotorsetup[dd - 1][0][rotorsetup[dd][1].IndexOf(let)];
                logoutput += "->" + let;
            }
            let = rotorsetup[0][0][rotorsetup[0][1].IndexOf(let)];
            logoutput += "->" + let;
            for (int ee = 0; ee < rotorsetup.Length - 1; ee++)
            {
                let = rotorsetup[ee + 1][1][rotorsetup[ee][0].IndexOf(let)];
                logoutput += "->" + let;
            }
            encrypt = encrypt + "" + let;
            //Rotor Turning
            if (rotorsetup[2][1][0] == rotorsetup[2][2][0] || rotorsetup[2][1][0] == rotorsetup[2][2][1])
            {
                rotorsetup[2][0] = rotorsetup[2][0].Substring(1) + "" + rotorsetup[2][0][0];
                rotorsetup[2][1] = rotorsetup[2][1].Substring(1) + "" + rotorsetup[2][1][0];
                rotorsetup[1][0] = rotorsetup[1][0].Substring(1) + "" + rotorsetup[1][0][0];
                rotorsetup[1][1] = rotorsetup[1][1].Substring(1) + "" + rotorsetup[1][1][0];
            }
            else if (rotorsetup[3][1][0] == rotorsetup[3][2][0] || rotorsetup[3][1][0] == rotorsetup[3][2][1])
            {
                rotorsetup[2][0] = rotorsetup[2][0].Substring(1) + "" + rotorsetup[2][0][0];
                rotorsetup[2][1] = rotorsetup[2][1].Substring(1) + "" + rotorsetup[2][1][0];
            }
            rotorsetup[3][0] = rotorsetup[3][0].Substring(1) + "" + rotorsetup[3][0][0];
            rotorsetup[3][1] = rotorsetup[3][1].Substring(1) + "" + rotorsetup[3][1][0];
            if (invert)
            {
                Log("INV BLACK", "{0}", logoutput);
            }
            else
            {
                Log("BLACK", "{0}", logoutput);
            }
        }
        return new EnigmaResult
        {
            Encrypted = encrypt,
            Screen1 = new ScreenText(rotorconfig.ToUpperInvariant(), 35),
            Screen2 = new ScreenText(rotorlets.ToUpperInvariant(), 40),
            Screen3 = new ScreenText(plugboard.ToUpperInvariant(), 25)
        };
    }
    string ScytaleTrans(string word, bool invert = false)
    {
        string[] letterrows = new string[("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[1]) % 4) + 2];
        string encrypt = "";
        if (invert)
        {
            Log("INV BLACK", "Number of Rows: ({0} % 4) + 2 = {1}", Bomb.GetSerialNumber()[1], letterrows.Length);
            for (int aa = 0; aa < letterrows.Length; aa++)
                letterrows[aa] = "";
            for (int aa = 0; aa < 6; aa++)
                letterrows[aa % letterrows.Length] = letterrows[aa % letterrows.Length] + "*";
            int cur = 0;
            for (int aa = 0; aa < letterrows.Length; aa++)
            {
                letterrows[aa] = word.Substring(cur, letterrows[aa].Length);
                cur += letterrows[aa].Length;
                Log("INV BLACK", "Scytale Row #{0}: {1}", (aa + 1), letterrows[aa]);
            }
            for (int aa = 0; aa < 6; aa++)
                encrypt = encrypt + "" + letterrows[aa % letterrows.Length][aa / letterrows.Length];
            Log("INV BLACK", "{0} -> {1}", word, encrypt);
        }
        else
        {
            Log("BLACK", "Number of Rows: ({0} % 4) + 2 = {1}", Bomb.GetSerialNumber()[1], letterrows.Length);
            for (int aa = 0; aa < letterrows.Length; aa++)
                letterrows[aa] = "";
            for (int aa = 0; aa < 6; aa++)
                letterrows[aa % letterrows.Length] = letterrows[aa % letterrows.Length] + "" + word[aa];

            for (int aa = 0; aa < letterrows.Length; aa++)
            {
                Log("BLACK", "Scytale Row #{0}: {1}", (aa + 1), letterrows[aa]);
                encrypt = encrypt + "" + letterrows[aa];
            }
            Log("BLACK", "{0} -> {1}", word, encrypt);
        }
        return encrypt;
    }
    struct DigrafidResult { public ScreenText Text1, Text2; public char ArrowLeft, ArrowRight; public string Encrypted; }
    DigrafidResult DigrafidEnc(string word, bool invert = false)
    {
        int length = Rnd.Range(0, wordList.Count);
        string kw1 = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw1.ToUpperInvariant());
        length = Rnd.Range(0, wordList.Count);
        string kw2 = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw2.ToUpperInvariant());
        if (invert)
        {
            Log("INV BLACK", "Keyword A: {0}", kw1);
            Log("INV BLACK", "Keyword B: {0}", kw2);
        }
        else
        {
            Log("BLACK", "Keyword A: {0}", kw1);
            Log("BLACK", "Keyword B: {0}", kw2);
        }
        string alpha1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
        string alpha2 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
        string k1 = getKey(kw1.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumberLetters().First()) % 2 == 0);
        string k2 = getKey(kw2.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumberLetters().Last()) % 2 == 1);
        char l1 = alpha1[Rnd.Range(0, alpha1.Length)];
        char l2 = alpha2[Rnd.Range(0, alpha2.Length)];
        alpha1 = alpha1.Replace(l1 + "", "");
        alpha2 = alpha2.Replace(l2 + "", "");
        string[] grid3x3 = { "123", "456", "789" };

        tryAgain:
        string key1 = k1.Replace(l1, '#') + "" + l1;
        string key2 = k2.Replace(l2, '#') + "" + l2;
        string[] numbers = { "", "", "" };
        List<string> logoutput = new List<string>();
        for (int aa = 0; aa < 3; aa++)
        {
            int num = key1.IndexOf(word[aa * 2]);
            numbers[0] = numbers[0] + "" + (num % 9 + 1);
            int row = num / 9;
            num = key2.IndexOf(word[aa * 2 + 1]);
            numbers[2] = numbers[2] + "" + (num % 9 + 1);
            int col = num / 9;
            numbers[1] = numbers[1] + "" + grid3x3[row][col];
            logoutput.Add(word[aa * 2] + "" + word[(aa * 2) + 1] + " -> " + numbers[0][aa] + "" + numbers[1][aa] + "" + numbers[2][aa]);
        }
        string encrypt = "";
        for (int bb = 0; bb < 3; bb++)
        {
            encrypt += key1[numbers[bb][0] - '0' - '\u0001' + (numbers[bb][1] - '0' - '\u0001') / '\u0003' * '\t'];
            encrypt += key2[numbers[bb][2] - '0' - '\u0001' + (numbers[bb][1] - '0' - '\u0001') % '\u0003' * '\t'];
            logoutput.Add("Digrafid Row #" + (bb + 1) + ": " + numbers[bb] + " -> " + encrypt[bb * 2] + "" + encrypt[(bb * 2) + 1]);
        }
        if (encrypt.Contains('#'))
        {
            if (alpha1.Length == 0)
            {
                alpha1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
                l2 = alpha2[Rnd.Range(0, alpha2.Length)];
                alpha2 = alpha2.Replace(l2 + "", "");
            }
            l1 = alpha1[Rnd.Range(0, alpha1.Length)];
            alpha1 = alpha1.Replace(l1 + "", "");
            goto tryAgain;
        }

        if (invert)
        {
            Log("INV BLACK", "Key A: {0}", key1);
            Log("INV BLACK", "Key B: {0}", key2);
            for (int k = 0; k < logoutput.Count; k++)
            {
                Log("INV BLACK", "{0}", logoutput[k]);
            }
        }
        else
        {
            Log("BLACK", "Key A: {0}", key1);
            Log("BLACK", "Key B: {0}", key2);
            for (int aa = 0; aa < logoutput.Count; aa++)
                Log("BLACK", "{0}", logoutput[aa]);
        }

        return new DigrafidResult
        {
            Text1 = new ScreenText(kw1.ToUpperInvariant(), 35),
            Text2 = new ScreenText(kw2.ToUpperInvariant(), 40),
            ArrowLeft = l1,
            ArrowRight = l2,
            Encrypted = encrypt
        };
    }
    #endregion

    #region Blue Cipher
    protected PageInfo[] bluecipher(string word, bool invert = false)
    {
        string text = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(text.ToUpperInvariant());
        if (invert)
        {
            Log("INV BLUE", "Begin Atbash Encryption");
            string encrypted = Atbash(word.ToUpperInvariant(), invert);
            Log("INV BLUE", "Begin Vigenere Encryption");
            encrypted = VigenereEnc(encrypted.ToUpperInvariant(), text.ToUpperInvariant(), invert);
            Log("INV BLUE", "Begin Tridigital Encryption");
            var tridigitalResult = TridigitalEnc(text.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(encrypted, 40), tridigitalResult[0], tridigitalResult[1]),
                new PageInfo(tridigitalResult[2]));
        }
        else
        {
            Log("BLUE", "Begin Vigenere Encryption");
            string encrypted = VigenereEnc(word.ToUpperInvariant(), text.ToUpperInvariant(), invert);
            Log("BLUE", "Begin Atbash Encryption");
            encrypted = Atbash(encrypted.ToUpperInvariant(), invert);
            Log("BLUE", "Begin Tridigital Encryption");
            var tridigitalResult = TridigitalEnc(text.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(encrypted, 40), tridigitalResult[0], tridigitalResult[1]),
                new PageInfo(tridigitalResult[2]));
        }
    }
    string VigenereEnc(string word, string kw, bool invert)
    {
        string key = "ZABCDEFGHIJKLMNOPQRSTUVWXY";
        string encrypt = "";
        if (invert)
        {
            for (int aa = 0; aa < 6; aa++)
            {
                encrypt += key[((key.IndexOf(word[aa]) - key.IndexOf(kw[aa])) % 26 + 26) % 26];
                Log("INV BLUE", "{0} - {1} = {2}", word[aa], kw[aa], encrypt[aa]);
            }
        }
        else
        {
            for (int aa = 0; aa < 6; aa++)
            {
                encrypt += key[(key.IndexOf(word[aa]) + key.IndexOf(kw[aa])) % 26];
                Log("BLUE", "{0} + {1} = {2}", word[aa], kw[aa], encrypt[aa]);
            }
        }
        return encrypt;
    }
    string Atbash(string word, bool invert = false)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string encrypt = "";
        for (int i = 0; i < 6; i++)
        {
            encrypt += alpha[25 - alpha.IndexOf(word[i])];
            if (invert)
                Log("INV BLUE", "{0} -> {1}", word[i], encrypt[i]);
            else
                Log("BLUE", "{0} -> {1}", word[i], encrypt[i]);
        }
        return encrypt;
    }
    ScreenText[] TridigitalEnc(string word, bool invert)
    {
        int length = Rnd.Range(0, wordList.Count);
        string kw = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw.ToUpperInvariant());
        string key = getKey(kw.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetIndicators().Count<string>() % 2 == 0);
        if (invert)
        {
            Log("INV BLUE", "Tridigital Key: {0}", key);
        }
        else
        {
            Log("BLUE", "Tridigital Key: {0}", key);
        }
        string numerical1 = "", numerical2 = "";
        for (int aa = 0; aa < word.Length; aa++)
        {
            numerical1 += (key.IndexOf(word[aa]) / 9 + 1);
            numerical2 += (key.IndexOf(word[aa]) % 9 + 1);
            Log(invert ? "INV BLUE" : "BLUE", "{0} -> {1}{2}", word[aa], numerical1[aa], numerical2[aa]);
        }
        return new[]
        {
            new ScreenText(numerical1, 40),
            new ScreenText(numerical2, 40),
            new ScreenText(kw.ToUpperInvariant(), 35)
        };
    }
    #endregion

    #region Brown Cipher
    protected PageInfo[] browncipher(string word, bool invert = false)
    {
        string kw = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(kw.ToUpperInvariant());
        string encrypted;
        if (invert)
        {
            Log("INV BROWN", "Begin Strip Encryption");
            encrypted = StripEnc(word.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("INV BROWN", "Begin M-209 Encryption");
            encrypted = M209Enc(encrypted.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("INV BROWN", "Begin Book Encryption");
        }
        else
        {
            Log("BROWN", "Begin M-209 Encryption");
            encrypted = M209Enc(word.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("BROWN", "Begin Strip Encryption");
            encrypted = StripEnc(encrypted.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("BROWN", "Begin Book Encryption");
        }
        var bookEncResult = BookEnc(kw.ToUpperInvariant(), invert);
        return newArray(
            new PageInfo(new ScreenText(encrypted, 40), bookEncResult[0], bookEncResult[1]),
            new PageInfo(bookEncResult[2], bookEncResult[3], bookEncResult[4]));
    }
    private string M209Enc(string word, string kw, bool invert)
    {
        string[] binary =
        {
            "111000",
            "000010",
            "101101",
            "011100",
            "101110",
            "100011",
            "011000",
            "101010",
            "110101",
            "011010",

            "111001",
            "001011",
            "000111",
            "101100",
            "100110",
            "111011",
            "001101",
            "111101",

            "011001",
            "110000",
            "101110",
            "100101",
            "011111",
            "111010",
            "010110",
            "000101",
            "000011",

            "010001",
            "010000",
            "010010",
            "100001",
            "001010",
            "001100",
            "110111",
            "111100",
            "000100"
        };
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string alphaSerial = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string encrypt = "";
        string[] rotors = new string[6];
        int[] lugs = new int[6];
        for (int aa = 0; aa < 6; aa++)
        {
            rotors[aa] = binary[alphaSerial.IndexOf(Bomb.GetSerialNumber()[aa])];
            lugs[aa] = (alpha.IndexOf(kw[aa]) % 13) + 1;
            if (invert)
            {
                Log("INV BROWN", "Rotor #{0}: {1}", (aa + 1), rotors[aa]);
                Log("INV BROWN", "Number of Lugs triggering Rotor #{0}: {1}", (aa + 1), lugs[aa]);
            }
            else
            {
                Log("BROWN", "Rotor #{0}: {1}", (aa + 1), rotors[aa]);
                Log("BROWN", "Number of Lugs triggering Rotor #{0}: {1}", (aa + 1), lugs[aa]);
            }
        }
        for (int aa = 0; aa < 6; aa++)
        {
            int num = 25 - alpha.IndexOf(word[aa]);
            for (int bb = 0; bb < 6; bb++)
            {
                if (rotors[bb][aa] == '1')
                    num += lugs[bb];
            }
            encrypt = encrypt + "" + alpha[num % 26];
            Log(invert ? "INV BROWN" : "BROWN", "{0} -> {1}", word[aa], encrypt[aa]);
        }
        return encrypt;
    }
    private string StripEnc(string word, string kw, bool invert)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string[] stripTable =
        {
            "NIPTLVZWHACBFQGMSRXYUOJEDK", "SFHLNYGCUQWXDEVKOIZMTBPJAR",
            "WJCXPNVERYSQBDIFZTMGOUKHLA", "GHEJXBFPLUMKNRZCAWOSQYVDIT",
            "RUJVMCTAIHXGEPLNDQYOKFZBWS", "LDWSYHKBOVGCRMNETPIQAJXFZU",
            "ONMIRKQYDJETCZWGLFBXHASVUP", "MTVYBRXNFDJOASUQPEKLIWHGCZ",
            "ECTNHOWFBMLVYJSRXGUZDIAPKQ", "UAQWGTIKNFHLVXMZEODCJPBRSY",
            "DSYKUJATZLOEMIRBFHNWPVCQGX", "PWKGSAOQECRMZLXJYUHTFNDIVB",
            "BQXRZMSVATKPJOFIGDLUWHENYC", "VLAQDZJXMKFIGHOUWSREBCYTPN",
            "TYGUOXDLPNZWIKESBAFVMRQCJH", "FOIDVWYSJZPUXBTAHNERCMLKQG",
            "QZREKPBGSXUNWVCTMYJFLDOAHI", "HGOACUNIKRQZPWJYVLTBSEFXMD",
            "KEFBQDHZWSARTNPLCVGIYXMUOJ", "ZRLCJEMHTPVAOUQDNXWKGSIYBF",
            "ABZPIGRDVOYSLCHXUKQJNTWMFE", "CPUMFQLRGIDJHYAVKBSNXZTOEW",
            "IVSZTFUOYWNDKGBPJCAHEQRLXM", "XKNFESCJQBTYUADHIMVPZLGWRO",
            "JXDHWIEMCGBFQTYORZPAVKUSNL", "YMBOALPUXEIHSFKWQJCDRGNZTV"
        };
        string[] strips = new string[6];
        for (int aa = 0; aa < 6; aa++)
        {
            strips[aa] = stripTable[alpha.IndexOf(kw[aa])].ToUpperInvariant();
            int cursor = strips[aa].IndexOf(word[aa]);
            strips[aa] = strips[aa].Substring(cursor) + "" + strips[aa].Substring(0, cursor);
            if (invert)
                Log("INV BROWN", "Key Row #{0}: {1}", (aa + 1), strips[aa]);
            else
                Log("BROWN", "Key Row #{0}: {1}", (aa + 1), strips[aa]);
        }
        int column;
        if (invert)
        {
            column = 25 - (Bomb.GetSerialNumberNumbers().Sum() % 25);
            Log("INV BROWN", "Column Chosen: {0}", (column + 1));
        }
        else
        {
            column = (Bomb.GetSerialNumberNumbers().Sum() % 25) + 1;
            Log("BROWN", "Column Chosen: {0}", (column + 1));
        }
        string encrypt = "";
        for (int aa = 0; aa < 6; aa++)
        {
            encrypt = encrypt + "" + strips[aa][column];
            if (invert)
                Log("INV BROWN", "{0} -> {1}", word[aa], encrypt[aa]);
            else
                Log("BROWN", "{0} -> {1}", word[aa], encrypt[aa]);
        }
        return encrypt;
    }
    private ScreenText[] BookEnc(string word, bool invert)
    {
        var tempsnlets = Bomb.GetSerialNumberLetters().ToList();
        string snlets = tempsnlets[0] + "" + tempsnlets[tempsnlets.Count - 1];
        string[][][] diary =
        {
            new string[][]
            {
                new string[]{"FIRST","OF","ALL","I","WANT","TO","GET","SOMETHING","STRAIGHT","THIS","IS","A","JOURNAL","NOT","A","DIARY","I","KNOW","WHAT","IT","SAYS","ON","THE","COVER","BUT","WHEN","MOM","WENT","OUT","TO","BUY","THIS","THING","I","SPECIFICALLY","SAID","TO","MAKE","SURE","IT","DIDNT","SAY","DIARY","ON","IT","SO","DONT","EXPECT","ME","TO","BE","ALL","DEAR","DIARY","THIS","AND","DEAR","DIARY","THAT"},
                new string[]{"ALL","I","NEED","IS","FOR","SOME","JERK","TO","CATCH","ME","CARRYING","THIS","THING","AROUND","AND","GET","THE","WRONG","IDEA"},
                new string[]{"THE","OTHER","THING","I","WANT","TO","CLEAR","UP","RIGHT","AWAY","IS","HOW","THIS","WAS","NOT","MY","IDEA","IT","WAS","MOMS"},
                new string[]{"THE","DEAL","IS","THAT","IF","I","WRITE","IN","THIS","BOOK","A","LITTLE","BIT","EACH","DAY","I","GET","OUT","OF","ONE","CHORE","ON","SATURDAYS","SO","OF","COURSE","I","PICKED","THE","ONE","I","HATE","THE","MOST","BUT","IF","RODRICK","EVER","FINDS","OUT","HES","SCRUBBING","TOILETS","BECAUSE","OF","THIS","BOOK","IM","DEAD"},
                new string[]{"OH","YEAH","RODRICKS","MY","BROTHER","I","TRY","TO","AVOID","HIM","ANYWAY","BUT","NOW","THAT","I","STRUCK","THIS","DEAL","WITH","MOM","I","BETTER","BE","EXTRA","CAREFUL"},
                new string[]{"ANYWAY","I","THINK","MOM","HAS","THIS","IDEA","IM","GOING","TO","WRITE","DOWN","MY","FEELINGS","AND","ALL","THAT","BUT","SHES","NOT","ACTUALLY","ALLOWED","TO","READ","IT","SO","I","FIGURE","ILL","JUST","WRITE","WHAT","I","WANT"},
                new string[]{"THE","REAL","REASON","I","AGREED","TO","DO","THIS","AT","ALL","IS","BECAUSE","I","FIGURE","LATER","ON","WHEN","IM","RICH","AND","FAMOUS","ILL","HAVE","BETTER","THINGS","TO","DO","THAN","ANSWER","PEOPLES","STUPID","QUESTIONS","ALL","DAY","LONG"},
                new string[]{"LIKE","I","SAID","ONE","DAY","I","WILL","DEFINITELY","BE","FAMOUS","BUT","FOR","NOW","IM","STUCK","IN","THE","SEVENTH","GRADE","WITH","A","BUNCH","OF","MORONS"},
                new string[]{"TODAY","IS","THE","FIRST","DAY","OF","SCHOOL","AND","RIGHT","NOW","WERE","JUST","WAITING","AROUND","FOR","THE","TEACHER","TO","HURRY","UP","AND","FINISH","THE","SEATING","CHART"},
                new string[]{"SO","I","FIGURED","I","MIGHT","AS","WELL","WRITE","IN","THIS","BOOK","AND","JUST","GET","IT","OVER","WITH","FOR","THE","DAY"},
                new string[]{"BUT","ILL","TELL","YOU","SOMETHING","ON","THE","FIRST","DAY","OF","SCHOOL","YOU","GOT","TO","BE","REAL","CAREFUL","OF","WHERE","YOU","SIT","YOU","WALK","INTO","THE","CLASSROOM","AND","JUST","PLUNK","YOUR","STUFF","DOWN","ON","ANY","OLD","DESK","AND","THE","NEXT","THING","YOU","KNOW","THE","TEACHER","IS","SAYING","I","HOPE","YOU","ALL","LIKE","WHERE","YOURE","SITTING","BECAUSE","THESE","ARE","YOUR","PERMANENT","SEATS"},
                new string[]{"SO","IN","THIS","CLASS","I","GOT","STUCK","WITH","CHRIS","HOSEY","IN","FRONT","OF","ME","AND","LIONEL","JAMES","IN","BACK","OF","ME","OTHA","HARRIS","CAME","IN","LATE","AND","ALMOST","SAT","NEXT","TO","ME","BUT","LUCKILY","I","DID","SOME","QUICK","THINKING","AND","GOT","MYSELF","OUT","OF","THAT","ONE"}
            },
            new string[][]
            {
                new string[]{"IM","THINKING","FOR","NEXT","PERIOD","I","SHOULD","JUST","SIT","IN","THE","MIDDLE","OF","A","BUNCH","OF","CUTE","GIRLS","AS","SOON","AS","I","STEP","IN","THE","ROOM"},
                new string[]{"THEN","AGAIN","IF","I","DO","THAT","IT","JUST","PROVES","THAT","I","DIDNT","LEARN","A","THING","FROM","LAST","YEAR"},
                new string[]{"PLUS","THE","OTHER","THING","I","GOT","TO","THINK","ABOUT","IS","THAT","GIRLS","DONT","LET","YOU","COPY","OFF","OF","THEM","WHICH","COULD","BE","A","REAL","PROBLEM","IN","A","CLASS","LIKE","PRE","ALGEBRA"},
                new string[]{"SPEAKING","OF","SEATING","SOMETHING","THAT","REALLY","STUNK","TODAY","IS","HOW","IN","HOME","ROOM","I","GOT","STUCK","WITH","SOME","TEACHER","WHO","HAD","RODRICK","IN","HIS","CLASS","A","FEW","YEARS","BACK"},
                new string[]{"THE","ONLY","GOOD","THING","I","CAN","THINK","ABOUT","THE","FIRST","DAY","OF","SCHOOL","IS","THAT","SOME","OF","THE","TEACHERS","ARE","NEW","AND","SO","YOU","CAN","SLIDE","A","LITTLE"},
                new string[]{"ANYWAY","THE","TEACHER","IS","ALMOST","DONE","WITH","THE","SEATING","CHART","AND","I","THINK","I","WROTE","ENOUGH","IN","THIS","BOOK","TO","KEEP","MOM","OFF","MY","BACK","FOR","TODAY"},
                new string[]{"THIS","MORNING","MOM","MADE","ME","LEND","MY","BROTHER","RODRICK","SOME","OF","MY","MONEY","SO","HE","COULD","BUY","LUNCH","WHICH","REALLY","STUNK","IM","STILL","MAD","AT","RODRICK","FOR","THE","TRICK","HE","PULLED","ON","ME","AT","THE","BEGINNING","OF","THE","SUMMER","SO","IM","REALLY","NOT","LOOKING","TO","DO","HIM","ANY","FAVORS"},
                new string[]{"WHAT","HAPPENED","WAS","THAT","ON","THE","FIRST","DAY","OF","SUMMER","VACATION","HE","WOKE","ME","UP","IN","THE","MIDDLE","OF","THE","NIGHT","DRESSED","UP","IN","HIS","SCHOOL","CLOTHES","HE","TOLD","ME","I","SLEPT","THROUGH","THE","WHOLE","SUMMER","BUT","THAT","LUCKILY","I","HAD","WOKEN","UP","IN","TIME","FOR","THE","FIRST","DAY","OF","SCHOOL"},
                new string[]{"YOU","MIGHT","THINK","IM","PRETTY","DUMB","FOR","FALLING","FOR","THAT","ONE","BUT","I","WAS","TOO","GROGGY","TO","KNOW","ANY","BETTER","AND","PLUS","RODRICK","HAD","SET","MY","CLOCK","AHEAD","AND","PULLED","THE","BLINDS","SHUT"},
                new string[]{"SO","I","JUST","GOT","UP","AND","GOT","DRESSED","AND","WENT","DOWNSTAIRS","TO","FIX","MYSELF","SOME","BREAKFAST","I","MUSTVE","MADE","A","BIG","RACKET","BECAUSE","THE","NEXT","THING","I","KNEW","DAD","WAS","IN","MY","FACE","WONDERING","WHAT","THE","HECK","I","WAS","DOING","EATING","CHEERIOS","AT",snlets[1] + "","AM"},
                new string[]{"THE","THING","ABOUT","DAD","WHEN","HE","COMES","DOWNSTAIRS","LATE","AT","NIGHT","IS","THAT","HES","ALWAYS","JUST","WEARING","A","TEE","SHIRT","AND","SOME","BOXER","SHORTS","I","DONT","KNOW","WHICH","IS","WORSE","GETTING","YELLED","OUT","OR","HAVING","TO","SEE","YOUR","FATHER","IN","HIS","UNDERWEAR"},
                new string[]{"I","KEEP","MEANING","TO","ASK","HIM","TO","PLEASE","PUT","ON","SOME","MORE","CLOTHES","THE","NEXT","TIME","HE","COMES","DOWNSTAIRS","BUT","THE","RIGHT","OPPORTUNITY","NEVER","COMES","UP"},
                new string[]{"ANYWAY","IT","TOOK","ME","A","COUPLE","OF","MINUTES","TO","FIGURE","OUT","WHAT","ALL","WAS","GOING","ON","WHEN","I","TOLD","DAD","RODRICK","TRICKED","ME","DAD","STOMPED","ON","DOWN","TO","RODRICKS","ROOM","IN","THE","BASEMENT","AND","I","FOLLOWED","ALONG"},
                new string[]{"I","WAS","PRETTY","EXCITED","TO","FINALLY","SEE","RODRICK","GET","WHAT","WAS","COMING","TO","HIM"}
            },
            new string[][]
            {
                new string[]{"BUT","WHEN","WE","GOT","DOWN","THERE","RODRICK","HAD","COVERED","UP","HIS","TRACKS","PRETTY","GOOD","AND","YOU","WOULD","NEVER","KNOW","HE","HAD","BEEN","UP","TO","SOMETHING"},
                new string[]{"DAD","JUST","THREW","UP","HIS","HANDS","AND","WENT","BACK","UP","TO","BED","SO","NOW","DAD","THOUGHT","I","WAS","AN","IDIOT","AND","A","LIAR"},
                new string[]{"COME","TO","THINK","OF","IT","EVER","SINCE","DAD","HAS","BEEN","REAL","SUSPICIOUS","AROUND","ME","LIKE","IM","TURNING","INTO","A","BAD","KID","OR","SOMETHING"},
                new string[]{"ILL","PUT","IT","TO","YOU","THIS","WAY","IF","IM","GOING","TO","DO","SOMETHING","BAD","AND","TAKE","THE","HEAT","LIKE","I","DID","THAT","NIGHT","YOU","BETTER","BELIEVE","IM","GOING","TO","COME","UP","WITH","SOMETHING","A","LOT","MORE","SATISFYING","THAN","EATING","A","BOWL","OF","CHEERIOS","IN","THE","MIDDLE","OF","THE","NIGHT"},
                new string[]{"TODAY","IN","SOCIAL","STUDIES","I","SCORED","PRETTY","BIG","THE","TEACHER","MADE","US","SIT","IN","ALPHABETICAL","ORDER","SO","THE","WAY","THINGS","FELL","OUT","I","ENDED","UP","RIGHT","NEXT","TO","ALEX","ARUDA","WHO","IS","THE","SMARTEST","KID","IN","THE","CLASS"},
                new string[]{"HES","SUPER","EASY","TO","COPY","OFF","OF","BECAUSE","HE","ALWAYS","FINISHES","HIS","TESTS","EARLY","AND","THEN","JUST","PUTS","HIS","PAPER","ON","THE","FLOOR","NEXT","TO","HIM","WHILE","HE","READS","A","SCIENCE","FICTION","NOVEL","OR","SOMETHING"},
                new string[]{"KIDS","WHOSE","LAST","NAMES","START","WITH","THE","EARLY","LETTERS","ALWAYS","END","UP","BEING","THE","SMARTEST","BECAUSE","THEY","GET","CALLED","ON","FIRST","SOME","PEOPLE","THINK","THATS","NOT","TRUE","BUT","IF","YOU","WANT","TO","COME","OVER","TO","MY","SCHOOL","I","CAN","PROVE","IT"},
                new string[]{"I","CAN","ONLY","THINK","OF","ONE","KID","WHO","BROKE","THE","LAST","NAME","RULE","AND","THATS","PETER","UTEGER","HE","WAS","THE","SMARTEST","KID","UNTIL","THE","MIDDLE","OF","THE","FIFTH","GRADE","THATS","WHEN","A","COUPLE","OF","US","STARTED","GIVING","HIM","A","HARD","TIME","ABOUT","WHAT","HIS","INITIALS","SPELLED","EVERY","CHANCE","WE","GOT"},
                new string[]{"NOW","HE","DOESNT","RAISE","HIS","HAND","AT","ALL","WHICH","MAKES","FOR","OTHER","KIDS","TO","STEP","FORWARD","AND","TAKE","THE","SMARTEST","KID","TITLE"},
                new string[]{"I","FEEL","A","LITTLE","BAD","ABOUT","THE","WHOLE","PU","THING","BECAUSE","IM","ONE","OF","THE","GUYS","WHO","STARTED","IT","BUT","ITS","HARD","NOT","TO","TAKE","CREDIT","FOR","IT","WHENEVER","IT","COMES","UP"},
                new string[]{"I","FIGURED","OUT","ANOTHER","GOOD","THING","ABOUT","WRITING","THIS","JOURNAL","WHEN","IM","FAMOUS","I","CAN","CASH","IN","ON","IT","I","JUST","HAVE","TO","REMEMBER","TO","KEEP","IT","AWAY","FROM","MANNY","MY","LITTLE","BROTHER"},
                new string[]{"IF","YOU","HAVE","SOMETHING","VALUABLE","IN","THE","HOUSE","BELIEVE","ME","MANNY","WILL","FIND","A","WAY","TO","DESTROY","IT"},
                new string[]{"BACK","BEFORE","MANNY","CAME","ALONG","I","REMEMBER","I","WAS","ALL","EXCITED","ABOUT","GETTING","A","LITTLE","BROTHER","AFTER","ALL","THOSE","YEARS","OF","RODRICK","PICKING","ON","ME","I","FIGURED","IT","WAS","MY","TURN","TO","BE","A","LITTLE","HIGHER","ON","THE","TOTEM","POLE"}
            },
            new string[][]
            {
                new string[]{"BUT","BEING","A","BIG","BROTHER","DIDNT","TURN","OUT","LIKE","I","EXPECTED","AT","ALL","MOM","AND","DAD","PROTECT","MANNY","SO","I","CANT","PICK","ON","HIM","EVEN","IF","HE","DOES","SOMETHING","TO","TICK","ME","OFF"},
                new string[]{"PLUS","HES","NEVER","GOTTEN","PUNISHED","FOR","ANYTHING","AND","BELIEVE","ME","HES","DESERVED","IT","A","BUNCH","OF","TIMES"},
                new string[]{"JUST","THE","OTHER","DAY","HE","SOMEHOW","GOT","INTO","MY","ROOM","AND","USED","A","BUNCH","OF","MAGIC","MARKERS","TO","DECORATE","MY","DOOR","I","THOUGHT","MOM","AND","DAD","WOULD","REALLY","LET","HIM","HAVE","IT","BUT","AS","USUAL","I","WAS","WRONG"},
                new string[]{"SO","NOW","IM","STUCK","WAKING","UP","TO","THIS","HORRIBLE","DRAWING","STARING","AT","ME","EVERYDAY","MOM","WONT","LET","ME","PAINT","OVER","IT","OR","EVEN","COVER","IT","UP","WITH","A","POSTER","BECAUSE","SHE","SAYS","IT","MIGHT","HURT","MANNYS","FEELINGS"},
                new string[]{"THE","ONLY","GOOD","THING","ABOUT","GETTING","A","LITTLE","BROTHER","IS","THAT","NOW","RODRICK","DOESNT","MAKE","ME","SELL","HIS","STUPID","CHOCOLATE","BARS","FOR","SCHOOL","FUNDRAISERS","ANY","MORE"},
                new string[]{"THE","WORST","THING","ABOUT","MANNY","IS","THAT","WHEN","HE","WAS","REAL","LITTLE","HE","COULDNT","PRONOUNCE","BROTHER","SO","HE","STARTED","CALLING","ME","BUBBY","AND","MOM","AND","DAD","DIDNT","MAKE","HIM","CALL","ME","MY","REAL","NAME","WHEN","HE","COULD","SAY","IT"},
                new string[]{"LUCKILY","NONE","OF","MY","FRIENDS","HAVE","FOUND","OUT","YET","BUT","I","HAVE","HAD","SOME","REALLY","CLOSE","CALLS"},
                new string[]{"YESTERDAY","WAS","THE","FIRST","DAY","OF","PE","AND","WE","STARTED","THE","FOOTBALL","UNIT","THE","FIRST","THING","I","DID","WAS","SNEAK","OFF","TO","THE","BASKETBALL","COURTS","AND","CHECK","TO","SEE","IF","THE","CHEESE","WAS","STILL","WHERE","IT","WAS","AT","THE","END","OF","LAST","SCHOOL","YEAR","AND","SURE","ENOUGH","IT","WAS"},
                new string[]{"THAT","THING","HAS","BEEN","SITTING","ON","THE","COURT","SINCE","AT","LEAST","LAST","FALL","AND","IT","HAS","CAUSED","A","WHOLE","LOT","OF","TROUBLE","ITS","ALL","MOLDLY","AND","NASTY","AND","EVER","SINCE","IT","SHOWED","UP","PEOPLE","HAVE","BEEN","TRYING","TO","AVOID","IT"},
                new string[]{"TO","GIVE","YOU","AN","IDEA","OF","HOW","PEOPLE","WILL","GO","OUT","OF","THEIR","WAY","TO","STAY","AWAY","FROM","THE","CHEESE","ITS","SITTING","RIGHT","UNDER","THE","ONLY","HOOP","WITH","A","NET","IN","IT","BUT","NOBODYS","PLAYED","ON","THAT","COURT","FOR","A","YEAR"},
                new string[]{"DARNELL","WASHINGTON","TRIPPED","AND","FELL","AND","BRUSHED","THE","CHEESE","WITH","HIS","FINGER","LAST","YEAR","AND","STARTED","THIS","WHOLE","THING","CALLED","THE","CHEESE","TOUCH","ITS","BASICALLY","LIKE","COOTIES","WHERE","IF","YOU","GET","TOUCHED","WITH","THE","CHEESE","TOUCH","THEN","YOU","HAVE","IT","UNTIL","YOU","PASS","IT","ON","TO","SOMEBODY","ELSE"},
                new string[]{"THE","ONLY","WAY","TO","PROTECT","YOURSELF","FROM","THE","CHEESE","TOUCH","WAS","TO","CROSS","YOUR","FINGERS","BUT","IT","WAS","REALLY","HARD","TO","REMEMBER","TO","KEEP","YOUR","FINGERS","CROSSED","ALL","THE","TIME","ESPECIALLY","WHEN","WHOEVER","HAD","THE","CHEESE","TOUCH","WAS","LOOKING","FOR","HIS","NEXT","VICTIM"}
            },
            new string[][]
            {
                new string[]{"SO","I","TAPED","MY","FINGERS","TOGETHER","FOR","THE","LAST","COUPLE","WEEKS","OF","SCHOOL","I","ENDED","UP","GETTING","A","D","IN","HANDWRITING","BUT","IT","WAS","TOTALLY","WORTH","IT"},
                new string[]{"THIS","ONE","KID","NAMED","ABE","HALL","GOT","THE","CHEESE","TOUCH","IN","APRIL","AND","NOBODY","WOULD","EVEN","SIT","AT","THE","SAME","TABLE","WITH","HIM","AT","LUNCH","FOR","THE","WHOLE","REST","OF","THE","YEAR"},
                new string[]{"THIS","SUMMER","HE","MOVED","AWAY","TO","CALIFORNIA","AND","HE","TOOK","THE","CHEESE","TOUCH","WITH","HIM","NOBODY","HAS","TOUCHED","THE","CHEESE","EVER","SINCE","THEN","NOT","EVEN","WITH","A","STICK"},
                new string[]{"WELL","THE","FIRST","WEEK","OF","SCHOOL","IS","FINALLY","OVER","SO","I","CAN","SLEEP","LATE","AGAIN","MOST","KIDS","SET","THEIR","ALARMS","AND","GET","UP","EARLY","ON","SATURDAY","MORNING","TO","WATCH","CARTOONS","OR","WHATEVER","BUT","NOT","ME","THE","WAY","I","KNOW","ITS","TIME","FOR","ME","TO","CRAWL","OUT","OF","BED","IS","WHEN","I","CANT","STAND","THE","TASTE","OF","MY","BREATH","ANYMORE"},
                new string[]{"UNFORTUNATELY","DAD","WAKES","UP","AT",snlets[0] + "" + snlets[0] + "" + snlets[0], "IN","THE","MORNING","NO","MATTER","WHAT","DAY","IT","IS","AND","HE","IS","NOT","REAL","CONSIDERATE","OF","THE","FACT","THAT","I","AM","TRYING","TO","ENJOY","MY","SATURDAY"},
                new string[]{"OTHER","THAN","THE","SATURDAY","MORNING","VACUUMING","ME","AND","DAD","GET","ALONG","PRETTY","GOOD","BUT","RODRICK","AND","DAD","IS","ANOTHER","STORY","IT","DOESNT","HELP","THAT","RODRICK","IS","A","TEENAGER","WHICH","IS","DADS","LEAST","FAVORITE","TYPE","OF","PERSON"},
                new string[]{"I","THINK","IF","THERE","WAS","A","PETITION","TO","SHIP","ALL","OF","THE","TEENAGERS","IN","THE","STATE","TO","AUSTRALIA","OR","ALCATRAZ","OR","SOMETHING","DAD","WOULD","BE","THE","FIRST","PERSON","TO","SIGN","IT"},
                new string[]{"AND","THE","FIRST","TEENAGER","HED","PUT","ON","THE","BOAT","WOULD","BE","THIS","KID","NAMED","LENWOOD","HEATH","LENWOOD","IS","ALWAYS","TOILET","PAPERING","PEOPLES","HOUSES","AND","GENERALLY","STIRRING","UP","TROUBLE","IN","THE","NEIGHBORHOOD"},
                new string[]{"DAD","HAS","SEEMED","A","LOT","MORE","RELAXED","EVER","SINCE","AUGUST","WHEN","LENWOODS","DAD","SHIPPED","HIM","OFF","TO","SOME","MILITARY","ACADEMY","IN","PENNSYLVANIA"},
                new string[]{"WHILE","IM","ON","THE","SUBJECT","OF","SATURDAY","I","SHOULD","MENTION","SOME","OF","MY","OTHER","GRIPES","FIRST","OF","ALL","THERES","NOTHING","ON","TV","AFTER",snlets[1] + "" + snlets[0] + "" + snlets[0], "PM","EXCEPT","GOLF","AND","BOWLING","SECOND","OF","ALL","THE","SUN","COMES","RIGHT","THROUGH","THE","SLIDING","GLASS","WINDOW","AND","YOU","CAN","HARDLY","SEE","WHATS","ON","THE","TV","ANYWAY","AND","ON","TOP","OF","THAT","YOU","GET","ALL","SWEATY","AND","STICK","TO","THE","COUCH","ITS","PRACTICALLY","LIKE","A","CONSIPRACY","AGAINST","KIDS","TO","MAKE","THEM","GO","OUTSIDE","AND","DO","SOMETHING","BESIDES","WATCH","TV"},
                new string[]{"TODAY","AFTER","DAD","WOKE","ME","UP","I","DECIDED","TO","JUST","SKIP","THE","WHOLE","TV","THING","AND","GO","OVER","TO","ROWLEYS"},
                new string[]{"I","KNOW","I","HAVENT","MENTIONED","ROWLEY","IN","THIS","JOURNAL","YET","EVEN","THOUGH","HES","TECHNICALLY","MY","BEST","FRIEND","BUT","THERES","A","PRETTY","GOOD","REASON","FOR","THAT"},
                new string[]{"ROWLEY","KIND","OF","TICKED","ME","OFF","ON","THE","FIRST","DAY","OF","SCHOOL","WITH","SOMETHING","HE","SAID","AT","THE","END","OF","THE","DAY","WHEN","WE","WERE","GETTING","OUR","STUFF","FROM","OUR","LOCKERS"}
            },
            new string[][]
            {
                new string[]{"I","TOLD","ROWLEY","AT","LEAST","A","BILLION","TIMES","THIS","SUMMER","THAT","NOW","THAT","WERE","IN","MIDDLE","SCHOOL","YOURE","SUPPOSED","TO","SAY","HANG","OUT","NOT","PLAY","BUT","NO","MATTER","HOW","MANY","TIMES","I","KICK","HIM","IN","THE","SHINS","WHEN","HE","SAYS","PLAY","HE","ALWAYS","FORGETS","FOR","THE","NEXT","TIME"},
                new string[]{"SO","I","GUESS","YOU","COULD","SAY","IVE","BEEN","AVOIDING","ROWLEY","THIS","WEEK","IVE","BEEN","TRYING","TO","BE","MORE","CAREFUL","ABOUT","MY","IMAGE","EVER","SINCE","THIS","SUMMER","WHEN","WE","GOT","CAUGHT","PLAYING","VIKINGS","AND","INDIANS","IN","THE","WOODS","BY","A","COUPLE","OF","EIGHTH","GRADERS"},
                new string[]{"WHAT","REALLY","BURNED","ME","UP","ABOUT","THAT","WHOLE","INCIDENT","IS","HOW","THAT","GUY","CALLED","ME","A","NERD","NOW","ILL","ADMIT","IM","NOT","EXACTLY","THE","MOST","MACHO","GUY","AROUND","IN","TERMS","OF","WANTING","TO","DO","PUSHUPS","ALL","THE","TIME","OR","WHATEVER","SO","IF","YOU","WANT","TO","CALL","ME","A","WIMP","THEN","FINE","BUT","I","KNOW","ONE","THING","FOR","SURE","AND","ITS","THAT","I","AM","NOT","A","NERD"},
                new string[]{"THE","TROUBLE","WITH","NERDS","IS","THAT","THEY","GIVE","WIMPY","KIDS","LIKE","ME","A","BAD","NAME","BECAUSE","PEOPLE","END","UP","LUMPING","US","ALL","IN","THE","SAME","CATEGORY","WHEN","I","THINK","OF","NERDS","I","THINK","OF","TEACHERS","PETS","AND","TICKLE","FIGHTS","AND","HALL","MONITORS","AND","THAT","IS","NOT","ME"},
                new string[]{"NOW","ROWLEY","CAN","SPEAK","FOR","HIMSELF","ON","THE","WHOLE","NERD","THING","BUT","I","WILL","JUST","MENTION","AS","A","SIDE","NOTE","THAT","HE","IS","THE","ONLY",snlets[1] + "" + snlets[0] + "YEAROLD","I","KNOW","WHO","STILL","HAS","A","BABYSITTER"},
                new string[]{"ROWLEY","MOVED","HERE","A","COUPLE","YEARS","AGO","AND","I","KIND","OF","TOOK","HIM","UNDER","MY","WING","MY","FORMER","BEST","FRIEND","BEN","MOVED","TO","PISCATAWAY","AND","I","FIGURED","ID","BETTER","FIND","MYSELF","A","NEW","FRIEND","TO","HANG","OUT","WITH","SO","HERE","COMES","ROWLEY","STRAIGHT","OUT","OF","OHIO","HIS","MOM","BOUGHT","HIM","SOME","BOOK","CALLED","HOW","TO","MAKE","FRIENDS","IN","NEW","PLACES","AND","HE","SHOWED","UP","AT","MY","DOOR","TRYING","OUT","ALL","THESE","GIMMICKS"},
                new string[]{"ALL","THAT","KID","WOULDVE","HAD","TO","HAVE","DONE","IS","TO","HAVE","COME","RIGHT","OUT","AND","TOLD","ME","HE","HAD","A","PLAYSTATION","WITH",snlets[0] + "" + snlets[0], "GAMES","AND","IT","WOULD","HAVE","SEALED","THE","DEAL"},
                new string[]{"THE","BEST","THING","ABOUT","HAVING","ROWLEY","AROUND","IS","THAT","I","GET","A","CHANCE","TO","USE","ALL","THE","TRICKS","RODRICK","USED","ON","ME","THAT","I","COULD","NEVER","GET","AWAY","WITH","PULLING","ON","MANNY"},
                new string[]{"ANOTHER","BONUS","ABOUT","ROWLEY","IS","THAT","HE","HAS","NEVER","SQUEALED","ON","ME","NOT","EVEN","ONCE","SO","IN","SOME","WAYS","I","GUESS","YOU","COULD","SAY","HES","THE","PERFECT","FRIEND"},
                new string[]{"TODAY","WAS","A","REALLY","BAD","DAY","DAD","ENDED","UP","RUNNING","INTO","MR","SWANN","AT","CHURCH","AND","MR","SWANN","WAS","TELLING","DAD","HOW","GREAT","BISHOP","GARRIGAN","HIGH","SCHOOL","IS","WHERE","HIS","SON","DAN","GOES"},
                new string[]{"DAD","SEEMED","REAL","INTERESTED","WHICH","IS","A","VERY","BAD","SIGN","FOR","ME","NOW","IM","SURE","BISHOP","GARRIGAN","IS","A","FINE","SCHOOL","AND","ALL","THAT","EXCEPT","FOR","THE","FACT","THAT","IT","IS","ALL","BOYS","NUMBER","ONE","I","WANT","TO","GO","TO","CROSSLAND","HIGH","SCHOOL","WHERE","THERE","ARE","BOYS","AND","GIRLS","AND","NUMBER","TWO","I","WOULDNT","SURVIVE","THE","FIRST","DAY","AT","BISHOP","GARRIGAN"}
            },
            new string[][]
            {
                new string[]{"RODRICK","DOESNT","HAVE","TO","WORRY","ABOUT","GETTING","SENT","TO","BISHOP","GARRIGAN","BECAUSE","HE","IS","ALREADY","A","JUNIOR","AT","CROSSLAND","BUT","I","HAD","DEFINITELY","BETTER","FIGURE","A","WAY","OUT","OF","THIS"},
                new string[]{"MR","SWANN","WENT","ON","AND","ON","ABOUT","HOW","BISHOP","GARRIGAN","MAKES","MEN","OUT","OF","BOYS","AND","FROM","THE","WAY","DAD","KEPT","LOOKING","OVER","AT","ME","I","KNEW","I","WAS","IN","TROUBLE","IT","DOESNT","HELP","THAT","MR","SWANN","HAS","THREE","BOYS","WHO","ARE","THE","SAME","AGES","AS","US","HEFFLEY","BOYS","AND","DADS","CARPOOL","PASSES","BY","THEIR","HOUSE","EVERY","NIGHT"},
                new string[]{"AS","FAR","AS","THE","WHOLE","MAKING","MEN","OUT","OF","BOYS","IDEA","GOES","I","THINK","THE","SWANN","BOYS","HAVE","A","PRETTY","GOOD","HEAD","START"},
                new string[]{"IVE","STILL","GOT","TWO","YEARS","BEFORE","I","GO","TO","HIGH","SCHOOL","AND","HOPEFULLY","DAD","WILL","FORGET","ABOUT","BISHOP","GARRIGAN","BY","THEN"},
                new string[]{"BUT","IF","THINGS","LOOK","BAD","DOWN","THE","ROAD","I","BETTER","START","WORKING","ON","MOM","TO","CHANGE","MY","FATE"},
                new string[]{"TODAY","I","WOKE","UP","AND","AT","FIRST","I","THOUGHT","IT","WAS","STILL","SUMMER","VACATION","WHICH","IS","A","REALLY","BAD","WAY","TO","START","A","SCHOOL","DAY"},
                new string[]{"THE","NEW","THING","IS","THAT","I","HAVE","TO","FIX","MANNY","HIS","CEREAL","EVERY","MORNING","WHILE","MOM","GETS","READY","FOR","WORK","MANNY","TAKES","HIS","BOWL","AND","SITS","RIGHT","IN","FRONT","OF","THE","TV","ON","HIS","PLASTIC","POTTY"},
                new string[]{"ITS","NOT","LIKE","HES","NOT","POTTY","TRAINED","BUT","HE","GOT","IN","THE","HABIT","OF","DOING","THIS","WHEN","HE","WAS","TWO","AND","HE","JUST","NEVER","QUIT"},
                new string[]{"THE","WORST","PART","IS","THAT","AFTER","HES","DONE","HE","DUMPS","WHATEVER","HE","DIDNT","EAT","RIGHT","INTO","THE","POTTY","AND","ITS","ALWAYS","ME","WHO","HAS","TO","CLEAN","IT","UP"},
                new string[]{"MOM","ALWAYS","GETS","ON","ME","ABOUT","NOT","FINISHING","MY","BREAKFAST","BUT","IF","YOU","HAD","TO","SCRAPE","A","BUNCH","OF","CHEERIOS","OUT","OF","A","POTTY","EVERY","MORNING","I","BET","YOU","WOULDNT","HAVE","ANY","APPETITE","EITHER"},
                new string[]{"TODAY","AT","SCHOOL","WE","GOT","ASSIGNED","TO","READING","GROUPS","I","WAS","LOOKING","FORWARD","TO","FINDING","OUT","WHICH","GROUP","I","WAS","GOING","TO","GET","PUT","INTO","BECAUSE","I","WANTED","TO","SEE","IF","A","BIG","PLAN","I","HATCHED","AT","THE","END","OF","LAST","YEAR","WAS","GOING","TO","WORK"},
                new string[]{"NOW","THEY","DONT","COME","RIGHT","OUT","AND","TELL","YOU","IF","YOURE","IN","THE","HARD","GROUP","OR","THE","EASY","GROUP","BUT","YOU","CAN","FIGURE","IT","OUT","RIGHT","AWAY","BY","LOOKING","AT","THE","COVERS","OF","THE","BOOKS","THEY","GIVE","YOU"},
                new string[]{"I","WAS","PRETTY","MAD","TO","FIND","OUT","I","GOT","PUT","IN","THE","HARD","GROUP","TODAY","WHICH","MEANT","MY","PLAN","FAILED","I","WAS","HOPING","TO","GET","IN","THE","EASY","GROUP","BECAUSE","THEY","ONLY","HAVE","TO","READ","ABOUT","A","TENTH","OF","THE","STUFF","THAT","THE","KIDS","IN","THE","HARD","GROUP","HAVE","TO","READ","AND","THERES","A","WHOLE","LOT","LESS","HOMEWORK"}
            },
            new string[][]
            {
                new string[]{"AT","THE","END","OF","LAST","YEAR","I","DID","MY","BEST","TO","MUFF","UP","MY","SCREENING","TEST","TO","MAKE","SURE","I","DIDNT","GET","PUT","IN","THE","HARD","GROUP"},
                new string[]{"ANOTHER","THING","I","DID","TO","MAKE","SURE","I","DIDNT","GET","PUT","IN","THE","HARD","GROUP","WAS","TO","MAKE","SURE","I","DIDNT","TRY","TO","HARD","ON","MY","ENDOFTHEYEAR","ESSAY"},
                new string[]{"THEY","MAKE","YOU","DO","THIS","FOUR","PAGE","PAPER","AT","THE","END","OF","THE","YEAR","WHICH","IS","ANOTHER","WAY","THEY","FIGURE","OUT","HOW","TO","PLACE","YOU"},
                new string[]{"IM","GUESSING","MOM","STEPPED","IN","AND","MADE","SURE","I","GOT","PUT","IN","THE","HARD","CLASSES","BECAUSE","SHE","KNOWS","THE","PRINCIPAL","OF","THE","SCHOOL"},
                new string[]{"MOMS","ALWAYS","SAYING","HOW","IM","A","REAL","SMART","KID","BUT","I","JUST","DONT","APPLY","MYSELF"},
                new string[]{"YOU","MIGHT","WONDER","WHY","ID","WANT","TO","GET","PUT","IN","THE","EASY","CLASSES","SINCE","I","PROBABLY","DESERVE","TO","BE","IN","THE","HARD","CLASSES","BUT","I","HAVE","A","PRETTY","GOOD","ANSWER","FOR","THAT"},
                new string[]{"IF","THERES","ONE","THING","I","LEARNED","FROM","RODRICK","ITS","TO","SET","PEOPLES","EXPECTATIONS","REAL","LOW","SO","YOU","END","UP","SURPRISING","EVERYONE","BY","DOING","ALMOST","NOTHING","AT","ALL"},
                new string[]{"IN","FACT","HE","DID","SOMETHING","ON","FRIDAY","THAT","TOTALLY","PROVES","MY","POINT"},
                new string[]{"ANYWAY","I","GUESS","IM","GLAD","MY","PLAN","DIDNT","WORK","BECAUSE","I","NOTICED","AT","LEAST","TWO","OF","THE","KIDS","IN","THE","BINK","SAYS","BOO","GROUP","WERE","HOLDING","THEIR","BOOKS","UPSIDE","DOWN","AND","I","DONT","THINK","THEY","WERE","JOKING","AROUND"},
                new string[]{"TODAY","AT","LUNCH","I","GOT","TO","LISTEN","TO","ALBERT","SANDY","BRAG","ABOUT","HOW","HIS","PARENTS","BOUGHT","HIM","A",snlets[1] + "" + snlets[0], "INCH","TV","AND","A","DVD","PLAYER","AND","A","BUNCH","OF","OTHER","STUFF","FOR","HIS","BEDROOM"},
                new string[]{"IT","REALLY","MAKES","ME","MAD","BECAUSE","MY","WHOLE","GOAL","THIS","SUMMER","WAS","TO","SAVE","UP","FOR","A","TV","SO","I","DIDNT","HAVE","TO","HANG","OUT","WITH","THE","REST","OF","MY","FAMILY","AND","WATCH","WHAT","THEY","WANT","TO","WATCH","ALL","THE","TIME"},
                new string[]{"SO","I","SPENT","THE","WHOLE","SUMMER","TAKING","CARE","OF","MR","AND","MRS","ROSES","DOG","WHILE","THEY","WERE","ON","A","TRIP"},
                new string[]{"THE","DEAL","WAS","THAT","I","HAD","TO","GO","OVER","TO","THEIR","HOUSE","TWICE","A","DAY","TO","TAKE","THEIR","DOG","STEVIE","OUT","AND","I","WAS","SUPPOSED","TO","GET","THREE","BUCKS","A","DAY","FOR","DOING","IT"},
                new string[]{"THE","BIG","PROBLEM","WITH","STEVIE","IS","THAT","I","GUESS","HE","IS","TOTALLY","SHY","WHEN","IT","COMES","TO","GOING","TO","THE","BATHROOM","IN","FRONT","OF","STRANGERS","SO","I","WASTED","A","WHOLE","LOT","OF","MY","SUMMER","STANDING","THERE","WAITING","FOR","THIS","STUPID","DOG","TO","HURRY","UP","AND","GO"},
                new string[]{"SO","ID","WAIT","AND","WAIT","AND","NOTHING","WOULD","HAPPEN","SO","ID","JUST","GO","HOME","BUT","EVERY","TIME","I","CAME","BACK","TO","THE","HOUSE","LATER","ON","STEVIE","HAD","MADE","A","MESS","IN","THE","KITCHEN","I","FINALLY","FIGURED","OUT","STEVIE","WAS","JUST","HOLDING","IT","UNTIL","THE","COAST","WAS","CLEAR"}
            },
            new string[][]
            {
                new string[]{"IN","FACT","ONE","DAY","I","TRIED","AN","EXPERIMENT","WHERE","I","LEFT","AND","THEN","CAME","BACK","FIVE","MINUTES","LATER","AND","SURE","ENOUGH","STEVIE","HAD","POOPED","RIGHT","ON","THE","KITCHEN","FLOOR"},
                new string[]{"AND","ITS","NOT","LIKE","I","DIDNT","GIVE","THE","DOG","A","CHANCE","TO","GO","THE","ROSES","HAVE","SATELLITE","TV","AND","TONS","OF","JUNK","FOOD","SO","I","BASICALLY","SPENT","THREE","HOURS","A","DAY","ON","MR","ROSES","LAZBOY","WITH","THE","AIR","CONDITIONER","ON","FULL","BLAST"},
                new string[]{"SO","THIS","ONE","DAY","I","FINALLY","FIGURED","OUT","IT","WAS","A","BIG","HASSLE","TO","CLEAN","UP","THIS","DOGS","MESS","EVERY","SINGLE","DAY","SO","I","DECIDED","TO","JUST","SAVE","MYSELF","SOME","TIME","AND","CLEAN","IT","UP","ALL","AT","ONCE"},
                new string[]{"I","LET","THINGS","GO","FOR","ABOUT","A","WEEK","THEN","THE","NIGHT","BEFORE","THE","ROSES","WERE","SUPPOSED","TO","GET","BACK","I","HEADED","UP","THE","HILL","WITH","ALL","MY","CLEANING","STUFF"},
                new string[]{"AND","WOULDNT","YOU","KNOW","IT","THE","ROSES","CAME","HOME","A","DAY","EARLY"},
                new string[]{"TO","MAKE","A","LONG","STORY","SHORT","I","DIDNT","GET","PAID","A","SINGLE","CENT","NOT","EVEN","FOR","THE","DAYS","I","DID","MY","JOB","LIKE","I","WAS","SUPPOSED","TO","SO","HEARING","ALBERT","SANDY","BRAGGING","ABOUT","HIS","TV","JUST","REMINDED","ME","HOW","I","GOT","STIFFED","AND","PUT","ME","IN","A","BAD","MOOD","FOR","THE","REST","OF","THE","DAY"},
                new string[]{"YOU","KNOW","HOW","I","SAID","MOM","IS","ALWAYS","TAKING","MANNYS","SIDE","WELL","TODAY","WAS","SOME","MORE","PROOF","OF","THAT"},
                new string[]{"I","MADE","MANNY","HIS","CEREAL","LIKE","I","ALWAYS","DO","BUT","THIS","TIME","I","ACCIDENTALLY","POURED","THE","MILK","IN","BEFORE","THE","CEREAL","AND","WHEN","I","POURED","THE","CEREAL","IN","ON","TOP","OF","THAT","MANNY","JUST","ABOUT","LOST","HIS","MIND","HE","MADE","A","HUGE","RACKET","CRYING","AND","HOLLERING","AND","ALL","THAT","AND","MOM","CAME","DOWN","TO","SEE","WHAT","WAS","GOING","ON","SO","I","TOLD","HER","WHAT","HAPPENED","AND","I","FIGURED","SHE","WOULD","JUST","TELL","MANNY","TO","PIPE","DOWN","AND","EAT","HIS","STUPID","CEREAL"},
                new string[]{"BUT","INSTEAD","SHE","SAYS","I","WOULDNT","EAT","IT","EITHER","AND","THEN","SHE","GIVES","MANNY","A","BIG","HUG","AND","MAKES","ME","POUR","HIM","A","NEW","BOWL","OF","CEREAL","THIS","TIME","IN","THE","RIGHT","ORDER"},
                new string[]{"I","GUESS","I","SHOULDVE","EXPECTED","IT","A","COUPLE","WEEKS","AGO","WHEN","MANNY","WAS","AT","DAYCARE","HE","OPENED","UP","HIS","LUNCHBOX","AND","WHEN","HE","TOOK","OUT","HIS","SANDWHICH","HE","HAD","A","FIT","BECAUSE","IT","WAS","CUT","IN","TWO","HALVES","NOT","FOUR","SQUARES","LIKE","HE","USUALLY","GETS","IT","SO","THE","DAYCARE","PEOPLE","HAD","TO","CALL","MOM","UP","AT","WORK","TO","GET","HER","TO","COME","OVER","SO","MANNY","WOULD","CALM","DOWN"},
                new string[]{"TODAY","I","GOT","TOTALLY","DISSED","BY","CHRISTINE","COOLIDGE","I","ASKED","HER","IF","SHE","WOULD","BE","MY","LAB","PARTNER","FOR","SCIENCE","AND","SHE","TOLD","ME","SHE","ALREADY","HAD","A","PARTNER"},
                new string[]{"BUT","THEN","LATER","ON","IN","THE","CLASS","I","SAW","HER","WALK","UP","TO","BRYCE","ANDERSON","AND","ASK","HIM","IF","HE","WOULD","BE","HER","LAB","PARTNER","IM","NOT","SURPRISED","SHE","WENT","AFTER","BRYCE","BECAUSE","HE","IS","THE","MOST","POPULAR","KID","IN","OUR","GRADE","FOR","SOMETHING","LIKE","THREE","YEARS","RUNNING","BUT","SHE","DIDNT","HAVE","TO","GO","AND","LIE","ABOUT","IT","EITHER"}
            },
            new string[][]
            {
                new string[]{"IT","USED","TO","BE","A","LOT","MORE","SIMPLE","WITH","GIRLS","WAY","BACK","IN","THE","THIRD","GRADE","THE","DEAL","WAS","IF","YOU","WERE","THE","FASTEST","RUNNER","IN","YOUR","GRADE","YOU","WERE","THE","KING","AND","IN","MY","GRADE","THAT","WAS","RONNIE","JONES"},
                new string[]{"NOWADAYS","ITS","A","LOT","MORE","COMPLICATED","AND","IM","SURE","KIDS","LIKE","RONNIE","JONES","ARE","SITTING","AROUND","SCRATCHING","THEIR","HEADS","WONDERING","WHAT","HAPPENED"},
                new string[]{"NOT","ONLY","DOES","BRYCE","ANDERSON","GET","ALL","THE","GIRLS","BUT","HE","ALSO","HAS","A","BIG","GROUP","OF","CRONIES","THAT","FOLLOWS","HIM","AROUND","AND","BASICALLY","WORSHIPS","EVERY","WORD","THAT","COMES","OUT","OF","HIS","MOUTH"},
                new string[]{"THE","THING","THAT","REALLY","STINKS","IS","THAT","I","HAVE","ALWAYS","BEEN","INTO","GIRLS","BUT","KIDS","LIKE","BRYCE","HAVE","ONLY","COME","AROUND","TO","LIKING","GIRLS","IN","THE","PAST","COUPLE","OF","YEARS","I","REMEMBER","HOW","BRYCE","USED","TO","BE","BACK","IN","FOURTH","AND","FIFTH","GRADE"},
                new string[]{"AND","OF","COURSE","NOW","I","DONT","GET","ANY","CREDIT","FROM","THE","GIRLS","FOR","STICKING","WITH","THEM","FOR","ALL","THIS","TIME"},
                new string[]{"LIKE","I","SAID","BRYCE","IS","THE","MOST","POPULAR","KID","IN","THE","SEVENTH","GRADE","SO","THAT","LEAVES","ALL","THE","REST","OF","US","SCRAMBLING","TO","MOVE","UP","THE","LADDER"},
                new string[]{"THE","BEST","WAY","TO","FIGURE","OUT","HOW","POPULAR","YOU","ARE","IS","TO","GET","A","HOLD","OF","ONE","OF","THE","SLAM","BOOKS","THAT","GETS","PASSED","AROUND","BASICALLY","THEYRE","NOTEBOOKS","WHERE","PEOPLE","PUT","RANKINGS","DOWN","FOR","MOST","POPULAR","BOY","MOST","POPULAR","GIRL","BEST","HAIR","CUTEST","BUTT","AND","ALL","OF","THAT"},
                new string[]{"THE","PROBLEM","WITH","THESE","BOOKS","THOUGH","IS","THAT","THEYRE","IN","REGULAR","NOTEBOOKS","WHICH","ONLY","HAVE",snlets[0] + "" + snlets[0], "LINES","ON","EACH","PAGE","SO","PEOPLE","LIKE","ME","WHO","DONT","MAKE","THE","TOP",snlets[0] + "" + snlets[0], "HAVE","TO","GUESS","WHERE","THEY","RANK","THE","BEST","I","CAN","FIGURE","RIGHT","NOW","IS","THAT","IM","SOMEWHERE","AROUND",snlets[1] + "" + snlets[0] + "ND","OR",snlets[1] + "" + snlets[1] + "RD","MOST","POPULAR","BUT","I","THINK","IM","ABOUT","TO","MOVE","UP","A","RANK","BECAUSE","CHARLIE","DAVIES","(WHO","IS","A","REALLY","NICE","KID)","GETS","HIS","BRACES","ON","TUESDAY"},
                new string[]{"WHAT","SOMEONE","NEEDS","TO","DO","IS","START","UP","A","SLAM","BOOK","WITH","ONE","OF","THOSE","YELLOW","LEGAL","PADS","OF","PAPER","BECAUSE","THEY","HAVE","SOMETHING","LIKE",snlets[1] + "" + snlets[0], "LINES","IN","THEM","SO","AT","LEAST","KIDS","LIKE","ME","COULD","GET","A","BETTER","PICTURE","OF","WHERE","THEY","STAND","BUT","THE","PROBLEM","IS","THAT","IT","TAKES","A","POPULAR","KID","TO","START","A","SLAM","BOOK","AND","IM","SURE","IF","I","STARTED","ONE","IT","WOULD","GET","FILLED","OUT","BY","ALL","NERDS"},
                new string[]{"TODAY","I","GOT","A","HOLD","OF","A","SLAM","BOOK","AND","I","WAS","TRYING","TO","EXPLAIN","ALL","THIS","TO","ROWLEY","ON","THE","BUS","RIDE","HOME","BUT","HONESTLY","SOMETIMES","WITH","HIM","I","FEEL","LIKE","ITS","JUST","IN","ONE","EAR","AND","OUT","THE","OTHER"},
                new string[]{"TONIGHT","DAD","WAS","SHOWING","ME","THE","NEWEST","STUFF","HE","ADDED","TO","HIS","CIVIL","WAR","DIORAMA","IN","THE","BASEMENT","AND","I","GOT","TO","ADMIT","ITS","PRETTY","COOL"},
                new string[]{"DADS","NOT","LIKE","THE","REGULAR","KINDS","OF","DADS","YOU","SEE","ON","TV","SITTING","AROUND","WATCHING","FOOTBALL","AND","DRINKING","BEER","AND","ALL","OF","THAT"}
            },
            new string[][]
            {
                new string[]{"ANY","FREE","SECOND","HE","GETS","YOU","CAN","BE","SURE","HES","DOWN","IN","HIS","WORKROOM","PAINTING","HIS","LITTLE","SOLDIERS","OR","MOVING","STUFF","AROUND","THE","BATTLEFIELD","TRYING","TO","MAKE","IT","AS","ACCURATE","AS","POSSIBLE"},
                new string[]{"DAD","WOULD","BE","HAPPY","TO","SPEND","THE","WHOLE","WEEKEND","WORKING","ON","HIS","DIORAMA","BUT","MOM","USUALLY","HAS","OTHER","IDEAS","MOM","LIKES","TO","RENT","THESE","ROMANTIC","COMEDIES","AND","DAD","HAS","TO","WATCH","THEM","WITH","HER","WHETHER","HE","WANTS","TO","OR","NOT"},
                new string[]{"A","COUPLE","WEEKS","AGO","WHEN","MOM","RENTED","ONE","OF","THESE","MOVIES","DAD","TRIED","TO","GET","CLEVER","AND","FAKE","OUT","MOM","WHEN","SHE","GOT","UP","TO","GO","TO","THE","BATHROOM","DAD","STUFFED","A","BUNCH","OF","PILLOWS","UNDER","THE","BLANKETS","TO","MAKE","IT","SEEM","LIKE","HE","HAD","FALLEN","ASLEEP"},
                new string[]{"SO","MOM","WATCHED","THE","REST","OF","THE","MOVIE","AND","DIDNT","CATCH","ON","THAT","DAD","HAD","MADE","A","DECOY","UNTIL","THE","MOVIE","WAS","OVER"},
                new string[]{"DAD","WAS","IN","THE","DOG","HOUSE","FOR","A","LONG","TIME","AFTER","THAT","ONE"},
                new string[]{"ANOTHER","THING","I","SHOULD","MENTION","ABOUT","DADS","WORKROOM","IS","THAT","HE","IS","REAL","PROTECTIVE","OF","IT","HE","KEEPS","THE","DOOR","BOLTED","SHUT","WITH","ONE","OF","THOSE","COMBINATION","LOCKS","SO","I","HARDLY","EVER","EVEN","GET","TO","STEP","FOOT","IN","THERE"},
                new string[]{"I","DONT","EVEN","THINK","MANNY","KNOWS","THE","DIORAMA","EXISTS","IVE","SEEN","DAD","SAY","SOME","THINGS","TO","MANNY","TO","MAKE","SURE","MANNY","KEEPS","CLEAR","OF","THAT","PART","OF","THE","BASEMENT"},
                new string[]{"ROWLEY","CAME","OVER","TONIGHT","AND","DAD","GETS","REAL","EDGY","WHEN","ROWLEYS","AROUND","FOR","SOME","REASON","DAD","HAS","IT","IN","HIS","HEAD","THAT","ROWLEY","IS","A","KLUTZ","AND","THAT","HES","GOING","TO","BREAK","SOMETHING","EVERY","TIME","HE","COMES","OVER"},
                new string[]{"DAD","TOLD","ME","ABOUT","THIS","NIGHTMARE","HE","ALWAYS","HAS","ABOUT","ROWLEY","RUINING","HIS","BATTLEFIELD","IN","ONE","KLUTZY","MOVE"},
                new string[]{"SO","EVERY","TIME","ROWLEY","COMES","OVER","HE","GETS","THE","SAME","GREETING","THE","BASEMENT","IS","OFFLIMITS"},
                new string[]{"TODAY","I","GOT","OUT","OF","BED","EARLY","BECAUSE","THE","VACUUMING","THING","WAS","MORE","THAN","I","COULD","HANDLE","I","WATCHED","CARTOONS","FOR","A","WHILE","BUT","THEN","I","REMEMBERED","THE","OTHER","REASON","I","DONT","GET","UP","EARLY","ON","SATURDAYS","WITH","DAD","DOING","HIS","CHORES","ALL","AROUND","YOU","IT","MAKES","YOU","FEEL","GUILTY","AND","TAKES","ALL","THE","JOY","OUT","OF","JUST","LAYING","AROUND","DOING","NOTHING"},
                new string[]{"TONIGHT","IM","GOING","TO","SPEND","THE","NIGHT","AT","ROWLEYS","ITS","A","PRETTY","BIG","DEAL","BECAUSE","I","HAVENT","SPENT","THE","NIGHT","OVER","THERE","IN","SOMETHING","LIKE","A","YEAR","AND","A","HALF"},
                new string[]{"THE","MAIN","REASON","I","HAVENT","SLEPT","OVER","THERE","IN","SO","LONG","IS","BECAUSE","ROWLEYS","DAD","REALLY","DOESNT","LIKE","ME"}
            },
            new string[][]
            {
                new string[]{"IT","ALL","GOES","BACK","TO","SOMETHING","THAT","HAPPENED","LAST","JUNE","WE","WERE","WATCHING","SOME","CORNY","MOVIE","ROWLEY","HAD","WHERE","THERE","WERE","THESE","KIDS","THAT","TAUGHT","THEMSELVES","A","SECRET","LANGUAGE","THAT","ONLY","THEY","COULD","UNDERSTAND"},
                new string[]{"ME","AND","ROWLEY","THOUGHT","THE","WHOLE","SECRET","LANGUAGE","THING","WAS","PRETTY","COOL","BUT","WE","COULDNT","FIGURE","OUT","HOW","TO","DO","IT","LIKE","THE","KIDS","IN","THE","MOVIE"},
                new string[]{"SO","WE","DECIDED","TO","MAKE","UP","OUR","OWN","SECRET","LANGUAGE","AND","WE","TRIED","IT","OUT","OVER","DINNER"},
                new string[]{"WELL","ROWLEYS","DAD","MUST","HAVE","CRACKED","OUR","CODE","BECAUSE","THE","NEXT","THING","I","KNEW","I","WAS","GOING","HOME","EARLY"},
                new string[]{"AND","THAT","WAS","THE","LAST","TIME","I","WAS","INVITED","FOR","A","SLEEPOVER"},
                new string[]{"SO","I","DONT","KNOW","WHY","MR","JEFFERSON","IS","GIVING","ME","A","SECOND","CHANCE","EITHER","HES","GOTTEN","OVER","THE","WHOLE","SECRET","LANGUAGE","THING","OR","HES","FORGOTTEN","WHY","HE","DOESNT","LIKE","ME"},
                new string[]{"THE","SLEEPOVER","AT","ROWLEYS","LAST","NIGHT","ENDED","UP","BEING","A","NIGHTMARE","THE","FIRST","HINT","I","HAD","THAT","THINGS","WERE","GOING","TO","GO","WRONG","WAS","WHEN","ROWLEYS","MOM","TOLD","US","THATS","ENOUGH","TV","FOR","THE","NIGHT","AT",snlets[1] + "" + snlets[0] + "" + snlets[0]},
                new string[]{"I","WAS","LIKE","WELL","WHAT","ARE","WE","SUPPOSED","TO","DO","NOW","AND","SHE","SAID","YOU","COULD","READ","A","BOOK"},
                new string[]{"SO","OF","COURSE","I","THOUGHT","SHE","WAS","JOKING","BUT","RIGHT","WHEN","I","WAS","TELLING","ROWLEY","HOW","I","THOUGHT","HIS","MOM","WAS","PRETTY","FUNNY","SHE","SHOWED","UP","AGAIN","WITH","HER","ARMS","FULL","OF","BOOKS"},
                new string[]{"I","REALIZED","RIGHT","THEN","I","WAS","IN","FOR","A","PRETTY","LONG","NIGHT","SINCE","THE","TV","WAS","OFF","LIMITS","VIDEO","GAMES","WERE","OUT","TOO","SO","I","TRIED","TO","THINK","UP","WAYS","WE","COULD","KEEP","OURSELVES","ENTERTAINED","I","BROKE","OUT","SOME","BOARD","GAMES","BUT","ROWLEY","HAD","TO","TAKE","A","BATHROOM","BREAK","SOMETHING","LIKE","EVERY","FIVE","MINUTES","SO","IT","MADE","OUR","GAME","OF","RISK","GO","ON","FOREVER"},
                new string[]{"EVERY","TIME","ROWLEY","CAME","BACK","FROM","A","BATHROOM","BREAK","HE","WOULD","RUN","DOWN","STAIRS","AND","KICK","THIS","GIANT","SOMBRERO","ACROSS","THE","ROOM"},
                new string[]{"IT","WAS","FUNNY","THE","FIRST","TEN","TIMES","OR","SO","BUT","AFTER","A","WHILE","IT","REALLY","STARTED","GETTING","ON","MY","NERVES","SO","THIS","ONE","TIME","WHEN","HE","WAS","UPSTAIRS","I","PUT","ONE","OF","HIS","DADS","DUMBELLS","UNDER","THE","HAT","TO","SEE","IF","HE","WOULD","STILL","KICK","IT"},
                new string[]{"AND","SURE","ENOUGH","ROWLEY","COMES","RUNNING","DOWN","THE","STAIRS","AND","GIVES","THE","HAT","A","BIG","KICK"},
                new string[]{"ROWLEYS","DAD","WAS","DOWN","THE","STAIRS","IN","NO","TIME","FLAT","I","DONT","THINK","ROWLEY","KNEW","I","PUT","THE","DUMBELL","UNDER","THE","HAT","BUT","ROWLEYS","DAD","SEEMED","PRETTY","SUSPICIOUS"}
            },
            new string[][]
            {
                new string[]{"ANYWAY","I","GUESS","HE","DIDNT","HAVE","ENOUGH","HARD","EVIDENCE","OR","HE","WOULD","HAVE","SENT","ME","HOME","RIGHT","THEN","I","FELT","A","LITTLE","BIT","BAD","ABOUT","DOING","WHAT","I","DID","BUT","IF","YOU","THINK","ABOUT","IT","IF","ROWLEYS","PARENTS","HADNT","MAKE","US","TURN","OFF","THE","TV","THIS","NEVER","WOULD","HAVE","HAPPENED"},
                new string[]{"AT",snlets[1] + "" + snlets[0] + "" + snlets[0], "ROWLEYS","MOM","CAME","DOWN","TO","SAY","IT","WAS","LIGHTS","OUT","IF","I","WOULDVE","KNOWN","ROWLEYS","BEDTIME","ON","WEEKENDS","WAS",snlets[1] + "" + snlets[0] + "" + snlets[0], "BELIEVE","ME","I","NEVER","WOULD","HAVE","COME","OVER"},
                new string[]{"AND","THEN","I","FOUND","OUT","ANOTHER","UGLY","SURPRISE","THERE","WAS","NO","GUEST","BED","SO","I","HAD","TO","SLEEP","IN","THE","SAME","BED","WITH","ROWLEY","I","TRIED","TO","LAY","AS","FAR","AWAY","FROM","ROWLEY","AS","POSSIBLE","BUT","IT","WAS","IMPOSSIBLE","TO","GET","TO","SLEEP","WITH","HALF","OF","MY","BODY","HANGING","OFF","THE","BED"},
                new string[]{"ROWLEY","FELL","ASLEEP","RIGHT","AWAY","BUT","IT","MUSTVE","TAKEN","ME","TWO","HOURS","BUT","RIGHT","WHEN","I","FINALLY","STARTED","TO","DRIFT","OFF","ROWLEY","LETS","OUT","THIS","SCREAM","WHICH","SCARED","ME","SO","BAD","I","DROPPED","RIGHT","OUT","OF","THE","BED","AND","ONTO","THE","HARDWOOD","FLOOR"},
                new string[]{"ROWLEYS","PARENTS","CAME","RUNNING","IN","AND","ROWLEY","STARTED","BABBLING","ALL","OF","THIS","INCOHERENT","GIBBERISH"},
                new string[]{"IT","TURNS","OUT","HE","HAD","A","NIGHTMARE","THAT","A","CHICKEN","WAS","HIDING","UNDERNEATH","HIM","AND","THATS","WHAT","MADE","HIM","YELL","OUT","BUT","I","THINK","ROWLEY","WAS","SO","OUT","OF","IT","HE","DIDNT","REALLY","REALIZE","IT","WAS","JUST","A","DREAM"},
                new string[]{"SO","ROWLEYS","PARENTS","TOOK","HIM","INTO","THEIR","ROOM","AND","SPENT","THE","NEXT","TWENTY","MINUTES","CALMING","HIM","DOWN","AND","TELLING","HIM","IT","WAS","JUST","A","DREAM","AND","HOW","THERE","REALLY","WAS","NO","CHICKEN"},
                new string[]{"MAN","IF","I","WOKE","MY","DAD","UP","WITH","SOME","NONSENSE","ABOUT","A","CHICKEN","YOU","BETTER","BELIEVE","HE","WOULDNT","BE","GIVING","ME","A","BIG","HUG","AND","TELLING","ME","EVERYTHING","WAS","OK","BUT","THAT","JUST","GOES","TO","SHOW","HOW","DIFFERENT","MY","PARENTS","ARE","FROM","ROWLEYS"},
                new string[]{"AND","I","JUST","WANTED","TO","MAKE","A","NOTE","THAT","NOBODY","SEEMED","ALL","THAT","CONCERNED","THAT","I","TOOK","A","THREEFOOT","FALL","ONTO","THE","FLOOR","EVEN","THOUGH","THAT","HAPPENED","FOR","REAL","AND","NOT","JUST","IN","SOME","STUPID","DREAM"},
                new string[]{"I","THINK","ROWLEY","SPENT","THE","NIGHT","IN","HIS","PARENTS","BED","WHICH","WAS","JUST","FINE","BY","ME","BECAUSE","WITHOUT","ROWLEY","AND","HIS","NIGHTMARES","I","WAS","FINALLY","ABLE","TO","GET","SOME","SLEEP"},
                new string[]{"BUT","THE","SECOND","I","WOKE","UP","THIS","MORNING","I","CAME","HOME","AND","POURED","MYSELF","A","BIG","BOWL","OF","JUNK","CEREAL","AND","DID","MY","BEST","TO","FORGET","ABOUT","THE","WHOLE","EXPERIENCE"},
                new string[]{"I","COULDNT","WAIT","FOR","SCHOOL","TO","BE","OVER","WITH","TODAY","SO","I","COULD","GO","HOME","AND","PLAY","TWISTED","WIZARD","A","VIDEO","GAME","IVE","BEEN","PLAYING","FOR","FIVE","DAYS","STRAIGHT"}
            },
            new string[][]
            {
                new string[]{"THE","ONLY","PROBLEM","WITH","TWISTED","WIZARD","IS","THAT","YOU","CANT","SAVE","YOUR","PROGRESS","SO","YOU","HAVE","TO","JUST","LEAVE","IT","ON","ALL","THE","TIME","SO","IMAGINE","HOW","I","FELT","TODAY","WHEN","I","REALIZED","I","GOT","HOME","ABOUT","FIVE","SECONDS","TOO","LATE"},
                new string[]{"BELIEVE","ME","FROM","NOW","ON","I","AM","GOING","TO","PUT","A","PIECE","OF","BLACK","TAPE","OVER","THE","POWER","LIGHT","SO","IT","NEVER","HAPPENS","AGAIN"},
                new string[]{"I","DONT","KNOW","IF","IVE","MENTIONED","IT","BEFORE","BUT","I","AM","SUPER","GOOD","AT","VIDEO","GAMES","I","DONT","KNOW","ANYONE","WHO","HAS","BEAT","AS","MANY","GAMES","AS","ME","AND","IVE","GOT","ALL","MY","VICTORIES","ON","VIDEO","TAPE","TO","PROVE","IT"},
                new string[]{"UNFORTUNATELY","DAD","DOES","NOT","EXACTLY","APPRECIATE","MY","VIDEO","GAME","SKILLS","HE","IS","ALWAYS","GETTING","ON","ME","ABOUT","GOING","OUTSIDE","AND","DOING","SOMETHING","ACTIVE"},
                new string[]{"SO","TODAY","I","TRIED","TO","EXPLAIN","TO","HIM","THAT","WITH","VIDEO","GAMES","YOU","CAN","PLAY","SPORTS","LIKE","FOOTBALL","AND","SOCCER","AND","YOU","DONT","EVEN","HAVE","TO","GET","ALL","HOT","AND","SWEATY"},
                new string[]{"BUT","AS","USUAL","DAD","DIDNT","GET","MY","LOGIC","HES","A","PRETTY","SMART","GUY","IN","GENERAL","BUT","WHEN","IT","COMES","TO","COMMON","SENSE","SOMETIMES","I","WONDER","ABOUT","HIM"},
                new string[]{"ANYWAYS","THATS","HOW","I","FOUND","MYSELF","SHUT","OUT","OF","THE","HOUSE","THIS","AFTERNOON"},
                new string[]{"IM","SURE","DAD","WOULD","DISMANTLE","MY","VIDEO","GAME","SYSTEM","IF","HE","COULD","FIGURE","OUT","HOW","TO","BUT","LUCKILY","THE","PEOPLE","WHO","MAKE","THESE","THINGS","MAKE","THEM","PARENTPROOF"},
                new string[]{"SO","LIKE","I","SAID","I","WAS","SHUT","OUT","OF","THE","HOUSE","LOOKING","FOR","SOME","WAY","TO","ENTERTAIN","MYSELF"},
                new string[]{"WHAT","I","ALWAYS","DO","WHEN","DAD","MAKES","ME","GO","OUTSIDE","IS","I","JUST","GO","OVER","TO","COLLINS","HOUSE"},
                new string[]{"IM","NOT","A","HUGE","FAN","OF","COLLINS","BUT","HE","HAS","TWO","THINGS","GOING","FOR","HIM","ONE","HE","DOESNT","MIND","WATCHING","ME","PLAY","HIS","VIDEO","GAMES","AND","TWO","HIS","DAD","HAS","EVERY","SPIDER","MAN","COMIC","BOOK","SINCE",snlets[1] + "" + snlets[1] + "" + snlets[0] + "" + snlets[0]},
                new string[]{"I","WOULD","GO","OVER","TO","ROWLEYS","HOUSE","TO","PLAY","VIDEO","GAMES","BUT","HE","ALWAYS","WANT","TO","TAKE","TURNS","WHICH","REALLY","BREAKS","MY","CONCENTRATION"},
                new string[]{"PLUS","ROWLEY","DOESNT","UNDERSTAND","THAT","IF","HE","PLAYS","USING","MY","MEMORY","CARD","IT","WILL","REALLY","SCREW","UP","MY","STATS"},
                new string[]{"THE","OTHER","THING","ABOUT","ROWLEY","IS","THAT","HES","NOT","A","SERIOUS","GAMER","LIKE","ME","HES","GOT","THIS","ONE","RACING","GAME","CALLED","FORMULA","ONE","RACING","IF","YOU","EVER","WANT","TO","BEAT","HIM","IN","IT","JUST","NAME","YOUR","CAR","SOMETHING","STUPID","AND","WATCH","WHAT","HAPPENS"},
                new string[]{"SO","TODAY","I","PLAYED","TWISTED","WIZARD","OVER","AT","COLLINS","UNTIL","IT","WAS","TIME","TO","COME","HOME","FOR","DINNER"}
            },
            new string[][]
            {
                new string[]{"ON","MY","WAY","UP","THE","HILL","I","MADE","SURE","TO","JUMP","THROUGH","THE","THOMPSONS","SPRINKLER","TO","MAKE","IT","LOOK","LIKE","I","WAS","ALL","HOT","AND","SWEATY","AND","THEN","I","TIMED","MY","ENTRANCE","PERFECTLY"},
                new string[]{"SO","MY","TRICK","WORKED","ON","DAD","BUT","IT","KIND","OF","BACKFIRED","WITH","MOM","BECAUSE","WHEN","SHE","SAW","ME","SHE","MADE","ME","TAKE","A","SHOWER","BEFORE","DINNER"},
                new string[]{"DAD","MUST","HAVE","BEEN","PRETTY","PROUD","OF","HIMSELF","FOR","HIS","IDEA","TO","KICK","ME","OUT","OF","THE","HOUSE","YESTERDAY","BECAUSE","HE","DID","IT","AGAIN","TODAY"},
                new string[]{"I","WAS","ACTUALLY","GOING","TO","GO","OUTSIDE","ANYWAY","BECAUSE","ROWLEY","HAD","GOTTEN","THIS","NEW","MODEL","ROCKET","WE","WANTED","TO","TRY","OUT","SO","WE","WENT","DOWN","TO","THE","SCHOOL","AND","SET","IT","OFF","BUT","THE","WIND","CARRIED","IT","ALL","THE","WAY","TO","THE","WOODS","AT","THE","END","OF","THE","FOOTBALL","FIELD"},
                new string[]{"I","DONT","KNOW","IF","I","EVER","MENTIONED","IT","BEFORE","BUT","THERES","A","BULLY","WHO","HANGS","OUT","IN","THOSE","WOODS","NAMED","HERBIE","REAMER","KIDS","LIKE","ME","AND","ROWLEY","STAY","AS","FAR","AWAY","FROM","THOSE","WOODS","AS","POSSIBLE"},
                new string[]{"ITS","A","REAL","PITY","TOO","BECAUSE","LIKE","I","SAID","IT","WAS","A","BRANDNEW","ROCKET"},
                new string[]{"I","HAVE","NO","IDEA","HOW","OLD","HERBIE","REAMER","IS","OR","WHERE","HE","LIVES","I","GUESS","ITS","POSSIBLE","THAT","HE","LIVES","RIGHT","THERE","IN","THE","WOODS","LIKE","A","WILD","ANIMAL","ALL","I","KNOW","IS","THAT","HES","BEEN","AROUND","LONG","ENOUGH","THAT","HE","TERRORIZED","RODRICK","AND","HIS","FRIENDS","WHEN","THEY","WERE","IN","MY","GRADE"},
                new string[]{"THE","THING","THAT","STINKS","IS","THAT","HERBIE","REAMERS","WOODS","ARE","RIGHT","BETWEEN","MY","HOUSE","AND","THE","SCHOOL","SO","IF","WE","COULD","CUT","THROUGH","THE","WOODS","IT","WOULD","SAVE","US","SOMETHING","LIKE","TWENTY","MINUTES","OF","WALKING"},
                new string[]{"I","WAS","TELLING","DAD","ALL","ABOUT","HERBIE","REAMER","THE","OTHER","DAY","DAD","TOLD","ME","ABOUT","THE","BULLY","FROM","WHEN","HE","WAS","GROWING","UP","SAM","SHARMAN"},
                new string[]{"DAD","SAID","SAM","SHARMAN","DID","THIS","PINCH","WHERE","HE","GRABBED","YOUR","SKIN","AND","TWISTED","IT","AROUND","TWO","TIMES"},
                new string[]{"DAD","TOLD","ME","THE","WAY","ALL","THE","NEIGHBORHOOD","KIDS","DEALT","WITH","SAM","SHARMAN","WAS","THAT","THEY","BANDED","TOGETHER","AND","TOLD","THE","PRINCIPAL","ON","HIM","DAD","SAID","SAM","CRIED","AND","THAT","HE","NEVER","DID","THE","SAM","SHARMAN","PINCH","AGAIN","AND","NOW","HES","AN","AIR","CONDITIONER","REPAIRMAN","AND","APPARENTLY","NOW","HES","A","REALLY","NICE","GUY"},
                new string[]{"WELL","FROM","THE","SOUND","OF","SAM","SHARMAN","HE","WOULDNT","LAST","TWO","SECONDS","AGAINST","HERBIE","REAMER","BUT","I","DIDNT","WANT","TO","HURT","DADS","FEELINGS","SO","I","JUST","TRIED","TO","ACT","IMPRESSED","BY","HIS","STORY"},
                new string[]{"AFTER","WE","LOST","THE","ROCKET","WE","WENT","UP","TO","ROWLEYS","TO","PLAY","CARDS","BUT","I","LOST","TRACK","OF","TIME","AND","I","WAS","LATE","FOR","DINNER","AT","MY","HOUSE","SO","ON","MY","WAY","DOWN","THE","HILL","I","TRIED","TO","THINK","OF","A","GOOD","EXCUSE","TO","GET","ME","OUT","OF","TROUBLE","WITH","MOM"}
            },
            new string[][]
            {
                new string[]{"MOM","WAS","PRETTY","HOT","AT","ME","FOR","BEING","LATE","JUST","LIKE","I","EXPECTED","SO","I","TOLD","HER","THAT","THE","CLOCK","IN","ROWLEYS","KITCHEN","MUST","BE","WRONG","AND","THAT","I","THOUGHT","I","WAS","RIGHT","ON","TIME"},
                new string[]{"AND","DO","YOU","KNOW","WHAT","MOM","DID","SHE","CALLED","ROWLEYS","MOM","AND","CAUGHT","ME","RED","HANDED"},
                new string[]{"SO","MOM","WAS","REALLY","MAD","THAT","I","LIED","BUT","AS","FAR","AS","BEING","TRICKY","GOES","MOM","SHOULDNT","BLAME","ME","BECAUSE","I","LEARNED","EVERYTHING","I","KNOW","FROM","HER"},
                new string[]{"I","REMEMBER","THIS","ONE","TIME","WHEN","I","WAS","IN","THE","SECOND","GRADE","AND","MOM","COULDNT","GET","ME","TO","BRUSH","MY","TEETH","SO","SHE","MADE","THIS","PRETEND","CALL","TO","THE","DENTIST","AND","I","TOTALLY","FELL","FOR","IT"},
                new string[]{"IN","FACT","THATS","WHEN","I","STARTED","BRUSHING","MY","TEETH","FIVE","TIMES","A","DAY"},
                new string[]{"MOM","SAID","SHE","WAS","GOING","TO","THINK","ABOUT","WHAT","MY","PUNISHMENT","SHOULD","BE","FOR","TELLING","A","LIE","AND","SHED","LET","ME","KNOW","AS","SOON","AS","SHE","CAME","UP","WITH","SOMETHING","THAT","FIT","THE","CRIME"},
                new string[]{"SEE","THATS","THE","DIFFERENCE","BETWEEN","MOM","AND","DAD","DAD","IS","PRETTY","SIMPLE","IF","YOU","MESS","UP","IN","FRONT","OF","HIM","HE","JUST","THROWS","WHATEVER","IS","IN","HIS","HAND","AT","YOU"},
                new string[]{"BUT","MOMS","A","LOT","MORE","CRAFTY","WITH","HER","PUNISHMENTS","SHE","THINKS","ABOUT","IT","FOR","A","FEW","DAYS","AND","THE","WAITING","ENDS","UP","BEING","JUST","ABOUT","AS","BAD","AS","THE","PUNISHMENT"},
                new string[]{"IN","THE","MEANTIME","YOU","END","UP","DOING","ALL","THESE","NICE","THINGS","HOPING","ITLL","GET","YOU","OFF","EASIER"},
                new string[]{"BUT","THEN","AFTER","A","FEW","DAYS","JUST","WHEN","YOU","FORGET","ABOUT","THE","PUNISHMENT","THATS","COMING","THATS","WHEN","SHE","GETS","YOU"},
                new string[]{"THE","ONLY","GOOD","THING","ABOUT","MOMS","PUNISHMENTS","IS","THAT","SHES","PRETTY","SOFT","SO","IF","YOU","JUST","LAY","LOW","FOR","A","WHILE","YOU","CAN","PRETTY","MUCH","ALWAYS","GET","OUT","OF","THE","PUNISHMENT","EARLY"},
                new string[]{"ANYWAY","THATS","WHAT","IM","COUNTING","ON","WHILE","I","WAIT","FOR","THIS","PUNISHMENT","TO","GET","HANDED","DOWN"},
                new string[]{"WELL","NOW","IVE","GONE","AND","DONE","IT","REMEMBER","HOW","I","SAID","YESTERDAY","THAT","WHILE","YOURE","WAITING","FOR","MOM","TO","HAND","DOWN","HER","PUNISHMENT","YOU","END","UP","DOING","ALL","THIS","GOOD","STUFF","TO","MAKE","HER","CHANGE","HER","MIND","IT","WAS","THAT","KIND","OF","THINKING","THAT","GOT","ME","IN","THIS","EXTRA","TROUBLE","IM","IN","NOW"},
                new string[]{"TODAY","AFTER","SCHOOL","I","THOUGHT","MAYBE","IF","I","WASHED","MOMS","CAR","BEFORE","SHE","GOT","HOME","SHE","MIGHT","GO","EASY","ON","ME","WITH","THE","PUNISHMENT","THING","SO","THATS","WHAT","I","DID","BUT","I","MADE","THE","MISTAKE","OF","USING","BRILLO","TO","RUB","OFF","ALL","THE","BUGS","AND","TAR","SPOTS","SO","I","WAS","IN","FOR","THE","SURPRISE","OF","MY","LIFE","WHEN","I","RINSED","THE","CAR","OFF"}
            },
            new string[][]
            {
                new string[]{"I","TOTALLY","RUINED","THE","PAINT","JOB","I","THOUGHT","ABOUT","JUST","DENYING","EVERYTHING","WHEN","MOM","ASKED","ME","HOW","HER","CAR","GOT","SCRATCHED","UP","BUT","MANNY","WAS","THERE","TO","SEE","EVERYTHING"},
                new string[]{"MANNY","HAS","BEEN","TELLING","ON","ME","SINCE","HE","COULD","TALK","IN","FACT","HE","HAS","TOLD","ON","ME","FOR","STUFF","I","DID","BEFORE","HE","COULD","TALK","ONE","TIME","WHEN","I","WAS","EIGHT","I","BROKE","THE","SLIDING","GLASS","DOOR","BUT","MOM","AND","DAD","COULDNT","PEG","IT","ON","ME","BUT","MANNY","WAS","THERE","TO","WITNESS","IT","AND","HE","SQUEALED","ON","ME","THREE","YEARS","LATER"},
                new string[]{"SO","AFTER","MANNY","STARTED","TALKING","I","HAD","TO","START","WORRYING","ABOUT","ALL","THE","BAD","THINGS","HE","SAW","ME","DO","WHEN","HE","WAS","REAL","SMALL"},
                new string[]{"I","USED","TO","BE","A","TATTLE","TALE","MYSELF","UNTIL","I","LEARNED","MY","LESSON","THIS","ONE","TIME","I","TOLD","ON","RODRICK","FOR","SAYING","A","BAD","WORD","MOM","ASKED","ME","WHICH","WORD","HE","SAID","SO","I","SPELLED","IT","OUT","AND","IT","WAS","A","LONG","ONE","TOO","NOT","ONLY","DID","RODRICK","GET","AWAY","SCOT","FREE","BUT","I","GOT","PUNISHED","FOR","KNOWING","HOW","TO","SPELL","A","BAD","WORD"},
                new string[]{"LIKE","I","SAID","I","KNEW","I","COULDNT","TALK","MANNY","OUT","OF","TELLING","ON","ME","FOR","USING","THE","BRILLO","ON","THE","CAR","SO","I","DECIDED","TO","TRY","A","TRICK","IVE","BEEN","SAVING","UP","FOR","A","SITUATION","LIKE","THIS"},
                new string[]{"I","PACKED","UP","A","BAG","AND","MADE","IT","SEEM","LIKE","I","WAS","GOING","TO","RUN","AWAY","FROM","HOME","RATHER","THAN","FACE","MOM","AND","DAD","FOR","WHAT","I","DID"},
                new string[]{"I","GOT","TO","GIVE","RODRICK","CREDIT","FOR","THIS","ONE","HE","USED","TO","PULL","IT","ON","ME","WHEN","HE","DID","SOMETHING","BAD","AND","HE","KNEW","I","WAS","GOING","TO","TELL","ON","HIM"},
                new string[]{"HED","BASICALLY","JUST","WALK","OUTSIDE","AND","THEN","COME","HOME","FIVE","MINUTES","LATER","BUT","BY","THAT","TIME","I","WAS","IN","PIECES","AND","COULDNT","EVEN","REMEMBER","THE","BAD","THING","THAT","HE","DID"},
                new string[]{"SO","I","LEFT","THE","HOUSE","AND","WAITED","FIVE","MINUTES","AND","THEN","CAME","BACK","INSIDE","I","WAS","EXPECTING","TO","FIND","MANNY","IN","THE","FOYER","BAWLING","LIKE","A","BABY","BUT","HE","WASNT","THERE","AT","ALL"},
                new string[]{"I","WENT","AROUND","THE","HOUSE","LOOKING","FOR","HIM","AND","I","STARTED","TO","GET","REALLY","NERVOUS","BECAUSE","IM","NOT","SUPPOSED","TO","LEAVE","MANNY","ALONE"},
                new string[]{"BUT","I","FOUND","HIM","IN","THE","KITCHEN","AND","GUESS","WHAT","HE","WAS","HALFWAY","FINISHED","WITH","MY","GIANT","HERSHEYS","KISS","IVE","BEEN","SAVING","SINCE","SUMMER"},
                new string[]{"SO","THINGS","TURNED","FROM","BAD","TO","WORSE"},
                new string[]{"WHEN","MOM","GOT","HOME","I","ACTUALLY","SPILLED","THE","BEANS","TO","HER","ABOUT","THE","CAR","AS","FAST","AS","I","COULD"},
                new string[]{"I","WAS","BASICALLY","TRYING","TO","KEEP","MANNY","QUIET","ABOUT","HOW","I","LEFT","THE","HOUSE","WHICH","WOULD","HAVE","GOTTEN","ME","IN","A","LOT","MORE","TROUBLE","THAN","RUINING","HER","CAR"}
            },
            new string[][]
            {
                new string[]{"MOM","JUST","LISTENED","WITH","A","FROWN","ON","HER","FACE","AND","THEN","TOLD","ME","WED","HAVE","TO","WAIT","UNTIL","DAD","GOT","HOME","TO","SEE","WHAT","HE","THOUGHT","ABOUT","WHAT","I","DID"},
                new string[]{"I","FIGURED","SHE","MIGHT","DO","THAT","SO","I","PULLED","ANOTHER","TRICK","I","LEARNED","FROM","RODRICK","I","INVITED","GRAMMA","OVER","FOR","DINNER","I","FIGURED","NOTHING","TOO","BAD","COULD","HAPPEN","IF","SHE","WAS","AROUND","AND","I","KNEW","I","COULD","USE","ANY","PROTECTION","I","COULD","GET"},
                new string[]{"SO","AT",snlets[0] + "" + snlets[0] + "" + snlets[0], "ON","THE","DOT","DAD","CAME","HOME","AND","OF","COURSE","HES","IN","A","GREAT","MOOD","FOR","SOME","REASON","THAT","ALWAYS","HAPPENS","TO","ME","WHEN","IM","IN","WAITFORDADTOGETHOME","KIND","OF","TROUBLE","IT","STINKS","BECAUSE","YOU","KNOW","HES","JUST","GONNA","BE","THAT","MUCH","MADDER","WHEN","HE","FINDS","OUT","WHAT","YOU","DID"},
                new string[]{"MOM","KEPT","QUIET","BECAUSE","GRAMMA","WAS","AROUND","SO","AT","LEAST","THAT","PART","OF","MY","PLAN","WORKED","OUT","AFTER","DINNER","I","JUST","SNUCK","UP","TO","MY","ROOM","AS","QUIETLY","AS","I","COULD","I","THINK","MOM","IS","TELLING","DAD","ABOUT","THE","CAR","RIGHT","NOW","BECAUSE","ITS","REAL","QUIET","DOWNSTAIRS"},
                new string[]{"RODRICK","HASNT","MADE","ME","FEEL","ANY","BETTER","ABOUT","THIS","WHOLE","THING","WHENEVER","IM","IN","TROUBLE","HE","TELLS","ME","THAT","IM","PROBABLY","GOING","TO","GET","THE","BELT"},
                new string[]{"AS","FAR","AS","I","KNOW","DAD","DOESNT","BELIEVE","IN","THAT","KIND","OF","PUNISHMENT","BUT","RODRICK","ALWAYS","SEEMS","TO","THINK","DAD","IS","GOING","TO","MAKE","AN","EXCEPTION","FOR","ME"},
                new string[]{"I","HAVE","DONE","MY","BEST","TO","MAKE","SURE","DAD","READS","ARTICLES","FROM","PARENTING","MAGAZINES","FROM","TIME","TO","TIME","TO","LET","HIM","KNOW","THAT","KIND","OF","THING","DOESNT","FLY","THESE","DAYS"},
                new string[]{"I","JUST","CUT","THOSE","ARTICLES","OUT","AND","SLIP","THEM","INTO","WHATEVER","BOOK","HES","READING","AT","THE","TIME"},
                new string[]{"I","THINK","I","HEAR","DAD","HEADING","UP","THE","STAIRS","NOW"},
                new string[]{"IF","THESE","ARE","THE","LAST","WORDS","I","EVER","WRITE","THEN","I","LEAVE","ALL","MY","COMIC","BOOKS","AND","ACTION","FIGURES","TO","ROWLEY","AND","PLEASE","THROW","THE","OTHER","HALF","OF","MY","HERSHEYS","KISS","AWAY","SO","MANNY","DOESNT","GET","IT"},
                new string[]{"WELL","I","LIVED","TO","SEE","ANOTHER","DAY","LAST","NIGHT","WHEN","DAD","KNOCKED","ON","MY","DOOR","I","PEEKED","THROUGH","THE","CRACK","AND","SAW","THAT","HE","WASNT","WEARING","THE","BELT","SO","I","LET","HIM","IN"},
                new string[]{"DAD","WASNT","EVEN","ALL","THAT","MAD","MOSTLY","BECAUSE","IT","WASNT","HIS","CAR","THAT","GOT","SCRAPED","UP","SO","HE","JUST","TOLD","ME","NOT","TO","USE","BRILLO","TO","CLEAN","A","CAR","AGAIN","AND","THAT","WAS","THAT"},
                new string[]{"MOM","WAS","ANOTHER","STORY","HER","PUNISHMENT","FOR","RUINING","HER","CAR","WAS","THAT","I","HAVE","TO","CLEAN","THE","WHOLE","BASEMENT"},
                new string[]{"AND","SHE","SAID","IF","SHE","CATCHES","ME","LYING","AGAIN","SHELL","TAKE","AWAY","MY","VIDEO","GAMES","FOR","GOOD","SO","I","BETTER","BE","PRETTY","HONEST","FROM","NOW","ON"},
                new string[]{"SO","NOW","THAT","ALL","THAT","STUFF","WAS","BEHIND","ME","I","COULD","SWITCH","MY","FOCUS","BACK","TO","SCHOOL"}
            },
            new string[][]
            {
                new string[]{"WE","JUST","GOT","OUR","FIRST","BIG","ASSIGNMENT","IN","ENGLISH","WHICH","IS","TO","DO","A","BOOK","REPORT","IVE","BEEN","MILKING","THE","SAME","BOOK","FOR","THE","PAST","FIVE","YEARS","ENCYCLOPEDIA","BROWN","DOES","IT","AGAIN","THERE","ARE","ABOUT",snlets[1] + "" + snlets[0], "SHORT","STORIES","IN","THERE","BUT","I","ALWAYS","JUST","TREAT","ONE","SHORT","STORY","LIKE","ITS","THE","WHOLE","BOOK","AND","THE","TEACHER","NEVER","NOTICES"},
                new string[]{"AND","TO","GIVE","YOU","AN","IDEA","OF","HOW","SHORT","EACH","STORY","IS","I","CAN","FINISH","ONE","IN","ABOUT","THREE","AND","A","HALF","MINUTES","WITHOUT","REALLY","TRYING"},
                new string[]{"THESE","ENCYCLOPEDIA","BROWN","STORIES","ARE","ALWAYS","THE","SAME","ITS","ALWAYS","ABOUT","HOW","SOMEBODY","COMMITS","SOME","LAME","CRIME","LIKE","STEALING","A","FISH","AND","THEN","ENCYCLOPEDIA","FIGURES","OUT","WHO","DID","IT","AND","MAKES","THEM","LOOK","STUPID"},
                new string[]{"I","HAVE","TO","SAY","THAT","NO","MATTER","HOW","HARD","I","TRY","I","HAVE","NEVER","FIGURED","OUT","ONE","OF","THESE","STORIES","BEFORE","THE","END","SO","I","GUESS","IM","NOT","AS","BRAINY","AS","ENCYCLOPEDIA"},
                new string[]{"IM","KIND","OF","AN","EXPERT","AT","WRITING","THESE","BOOK","REPORTS","BY","NOW","SO","I","KNOW","HOW","TO","WRITE","EXACTLY","WHAT","A","TEACHER","WANTS","TO","HEAR"},
                new string[]{"SO","FOR","ME","BOOK","REPORTS","ARE","NO","SWEAT"},
                new string[]{"I","FORGOT","TO","MENTION","THAT","THIS","AFTERNOON","I","TOOK","CARE","OF","PART","ONE","OF","MOMS","PUNISHMENT"},
                new string[]{"NOW","IVE","JUST","GOT","TO","KEEP","MY","NOSE","CLEAN","FOR","A","WHILE","AND","ILL","BE","OFF","THE","HOOK"},
                new string[]{"SO","FAR","ITS","BEEN","ABOUT",snlets[1] + "" + snlets[1], "HOURS","AND","IVE","KEPT","MY","PROMISE","TO","MOM","ABOUT","THE","WHOLE","HONESTY","THING","IT","REALLY","HASNT","BEEN","AS","BAD","AS","I","THOUGHT","AND","IN","FACT","IT","HAS","BEEN","KIND","OF","LIBERATING"},
                new string[]{"IVE","BEEN","IN","A","COUPLE","SITUATIONS","ALREADY","WHERE","IVE","BEEN","A","LOT","MORE","TRUTHFUL","THAN","I","WOULD","HAVE","BEEN","JUST","A","DAY","OR","TWO","AGO","LIKE","TODAY","WHEN","MAX","SMEDLEY","STARTED","TELLING","ME","HIS","BIG","PLANS"},
                new string[]{"AND","AT","ROWLEYS","GRANDPAS","BIRTHDAY","PARTY"},
                new string[]{"BUT","MOST","PEOPLE","DONT","SEEM","TO","REALLY","APPRECIATE","A","PERSON","AS","HONEST","AS","ME","LETS","JUST","SAY","I","CANT","UNDERSTAND","HOW","GEORGE","WASHINGTON","EVER","GOT","ELECTED","PRESIDENT"},
                new string[]{"IM","ACTUALLY","LOOKING","FORWARD","TO","TOMORROW","BECAUSE","THERE","ARE","A","COUPLE","OF","TEACHERS","WHO","COULD","USE","A","GOOD","DOES","OF","TRUTHFULNESS"},
                new string[]{"I","THINK","THE","HONESTY","PROMISE","HAS","BEEN","CANCELLED","AND","NOT","BY","ME","BUT","BY","MOM","TODAY","I","ANSWERED","THE","PHONE","AND","IT","WAS","MRS","GRETCHEN","FROM","THE","PTA","AND","SHE","WANTED","TO","TALK","TO","MOM","BUT","MOM","SIGNALED","TO","ME","THAT","I","SHOULD","TELL","MRS","GRETCHEN","THAT","SHE","WASNT","HOME","I","DIDNT","KNOW","IF","IT","WAS","A","TRICK","OR","WHAT","BUT","I","KNEW","THAT","I","WASNT","GOING","TO","GO","AND","BREAK","MY","HONESTY","STREAK","OVER","A","THING","LIKE","THIS","SO","I","MADE","MOM","GO","OUTSIDE","ON","THE","FRONT","PORCH","BEFORE","I","WOULD","SAY","A","WORD","TO","MRS","GRETCHEN"}
            },
            new string[][]
            {
                new string[]{"MOM","DIDNT","COME","RIGHT","OUT","AND","SAY","THE","HONESTY","DEAL","IS","OFF","BUT","SHE","DIDNT","SPEAK","TO","ME","FOR","THE","REST","OF","THE","NIGHT","SO","I","FIGURE","I","CAN","JUST","GO","BACK","TO","HOW","I","WAS","BEFORE"},
                new string[]{"THE","ONLY","OTHER","THING","THAT","HAPPENED","TODAY","THATS","WORTH","MENTIONING","IS","THAT","RODRICK","BROKE","HIS","OWN","SATURDAY","SLEEPING","RECORD","AT",snlets[1] + "" + snlets[1] + "" + snlets[0], "DAD","SAID","ENOUGH","IS","ENOUGH","AND","MADE","RODRICK","GET","OUT","OF","HIS","BED","IN","THE","BASEMENT"},
                new string[]{"BUT","RODRICK","JUST","TOOK","ALL","HIS","SLEEPING","GEAR","UPSTAIRS","AND","PLOPPED","HIMSELF","ON","THE","COUCH","UNTIL","IT","WAS","TIME","FOR","DINNER"},
                new string[]{"EVER","SINCE","MOM","DANGLED","THE","IDEA","OF","TAKING","AWAY","MY","VIDEO","GAMES","IVE","BEEN","TRYING","TO","BE","ON","MY","BEST","BEHAVIOR","SO","YOU","WOULDNT","THINK","I","COULD","GET","IN","TROUBLE","IN","THE","FIFTEEN","MINUTES","IT","TAKES","TO","DRIVE","TO","CHURCH","BUT","THATS","EXACTLY","WHAT","I","DID"},
                new string[]{"I","WAS","TRYING","TO","HAVE","SOME","FUN","WITH","MANNY","BY","MAKING","FUNNY","FACES","AT","HIM","IN","THE","BACK","SEAT","OF","THE","CAR","BUT","WHEN","I","FINALLY","GOT","A","LAUGH","OUT","OF","HIM","HE","SPIT","HIS","JUICE","ALL","OVER","THE","CAR","SEAT"},
                new string[]{"THAT","JUST","MADE","MANNY","LAUGH","EXTRA","HARD","BUT","THEN","THE","NEXT","THING","YOU","KNOW","MOM","SAYS","HE","COULD","HAVE","CHOKED","TO","DEATH"},
                new string[]{"WELL","I","GUESS","THAT","THOUGHT","WAS","JUST","TOO","MUCH","FOR","MANNY","TO","TAKE","ALL","I","KNOW","IS","THAT","THE","REST","OF","THE","RIDE","WAS","PRETTY","MISERABLE","FOR","EVERYONE"},
                new string[]{"SO","YOU","CAN","SEE","HOW","I","CAN","GO","FROM","BEING","THE","HERO","TO","THE","GOAT","IN","NO","TIME","FLAT"},
                new string[]{"AT","CHURCH","MY","OLD","BEST","FRIEND","BEN","WAS","SITTING","UP","FRONT","WITH","HIS","FAMILY","MOM","DOESNT","LET","US","SIT","TOO","CLOSE","TO","BENS","FAMILY","BECAUSE","ME","AND","BEN","USED","TO","ALWAYS","GET","INTO","GIGGLE","FITS","WHEN","WED","SIT","NEAR","EACH","OTHER"},
                new string[]{"OUR","BIG","ROUTINE","WAS","THAT","AT","THE","PART","IN","CHURCH","WHERE","YOURE","SUPPOSED","TO","SAY","PEACE","BE","WITH","YOU","AND","SHAKE","HANDS","WED","SAY","PEAS","BE","WITH","YOU","(LIKE","THE","VEGETABLE)"},
                new string[]{"MOM","SAID","IF","WE","DIDNT","STOP","LAUGHING","IN","CHURCH","SHE","WAS","GOING","TO","SEPARATE","US","SO","WE","BEHAVED","OURSELVES","FOR","A","WHILE"},
                new string[]{"BUT","THIS","ONE","SUNDAY","DURING","THE","PEACE","BE","WITH","YOU","PART","BEN","ACTUALLY","HANDED","ME","A","COUPLE","OF","DRIED","UP","PEAS","HE","HAD","BEEN","CARRYING","IN","HIS","POCKET","AND","WE","BOTH","TOTALLY","LOST","IT"},
                new string[]{"MOM","KEPT","GOOD","ON","HER","PROMISE","BECAUSE","WE","HAVENT","EVEN","SAT","ON","THE","SAME","SIDE","OF","THE","CHURCH","AS","BEN","SINCE","THAT","DAY"},
                new string[]{"ON","THE","WAY","HOME","FROM","CHURCH","WE","PASSED","BY","THE","SMEDLEYS","WHO","WERE","OUT","IN","THEIR","FRONT","YARD","THERE","ARE","ABOUT","SIX","BOYS","IN","THAT","FAMILY","BUT","ITS","HARD","TO","TELL","ANY","OF","THEM","APART"}
            },
            new string[][]
            {
                new string[]{"THE","SMEDLEYS","BIG","DREAM","IN","LIFE","IS","TO","WIN","THE","GRAND","PRIZE","ON","AMERICAS","FUNNIEST","HOME","VIDEOS","SO","THEYRE","ALWAYS","TRYING","TO","STAGE","SOME","KIND","OF","ACCIDENT"},
                new string[]{"I","BET","FOR","EVERY","TEN","TIMES","YOU","SEE","A","GUY","GET","HIT","IN","THE","GROIN","WITH","A","GOLF","BALL","ON","THAT","SHOW","NINE","OF","THEM","ARE","SENT","IN","BY","FAMILIES","LIKE","THE","SMEDLEYS","JUST","TRYING","TO","MAKE","A","BUCK"},
                new string[]{"TODAY","AT","SCHOOL","I","WAS","LOOKING","AT","THE","INSIDE","COVER","OF","MY","PREALGEBRA","BOOK","AND","I","SAW","THAT","IT","USED","TO","BELONG","TO","JIMMY","JURY","WHO","IS","THE","MOST","POPULAR","KID","IN","THE",snlets[0] + "TH","GRADE","I","FIGURED","I","MIGHT","BE","ABLE","TO","TRANSLATE","THIS","INTO","SOME","PRETTY","BIG","POPULARITY","POINTS","OF","MY","OWN","BUT","THE","PROBLEM","WAS","THAT","JIMMY","JURY","DIDNT","WRITE","HIS","NAME","ANYWHERE","ON","THE","OUTSIDE","OF","THE","BOOK","SO","I","TOOK","CARE","OF","THAT","DETAIL","ON","MY","OWN"},
                new string[]{"UNFORTUNATELY","I","ALSO","GOT","BRIAN","GLEESONS","SCIENCE","BOOK","SO","THINGS","SORT","OF","EVENED","OUT"},
                new string[]{"IN","PHYS","ED","TODAY","A","COUPLE","GIRLS","CAUSED","A","STIR","WHEN","THEY","PRESENTED","A","PETITION","TO","MR","UNDERWOOD","THE","GYM","TEACHER"},
                new string[]{"THE","WAY","THEY","GOT","THE","IDEA","OF","WRITING","A","PETITION","WAS","BECAUSE","IN","HISTORY","WERE","STUDYING","MARTIN","LUTHER","WHO","IS","A","GUY","WHO","WROTE","A","LIST","OF","DEMANDS","AND","POSTED","THEM","ON","A","CHURCH","DOOR"},
                new string[]{"SO","THE","GIRLS","GOT","IT","IN","THEIR","HEADS","THAT","IT","WAS","UNFAIR","THAT","THEY","HAD","TO","DO","GIRL","PUSHUPS","WHILE","THE","GUYS","GET","TO","DO","BOY","PUSHUPS","AND","THEY","ALL","SIGNED","THEIR","NAMES","TO","A","LIST","TO","PROTEST"},
                new string[]{"IF","I","WAS","THEM","I","WOULDNT","COMPLAIN","GIRL","PUSHUPS","ARE","ABOUT","TEN","TIMES","EASIER","THAN","BOY","PUSHUPS"},
                new string[]{"WITH","GIRL","PUSHUPS","YOU","GET","TO","KEEP","YOUR","KNEES","ON","THE","GROUND","SO","YOU","ONLY","HAVE","TO","WORK","HALF","AS","HARD"},
                new string[]{"SO","I","THINK","MR","UNDERWOOD","SURPRISED","THEM","WHEN","HE","SAID","SURE","YOU","GIRLS","CAN","GO","AHEAD","AND","DO","BOY","PUSHUPS"},
                new string[]{"I","THINK","THE","GIRLS","WERE","EXPECTING","A","LOT","BIGGER","FIGHT","AND","NOW","I","KNOW","AT","LEAST","HALF","OF","THEM","WISH","THEY","COULD","TAKE","THAT","PETITION","BACK"},
                new string[]{"I","KIND","OF","GOT","INSPIRED","BY","THE","WHOLE","EPISODE","AND","I","STARTED","TO","PUT","TOGETHER","A","PETITION","SAYING","WE","BOYS","WANTED","TO","BE","ALLOWED","TO","DO","GIRL","PUSHUPS"},
                new string[]{"BUT","WHEN","I","SAW","THE","GROUP","OF","GUYS","WHO","WAS","INTERESTED","IN","SIGNING","IT","I","DECIDED","TO","JUST","BAG","IT"}
            },
            new string[][]
            {
                new string[]{"TONIGHT","WAS","A","PRETTY","BIG","DEAL","FOR","ME","BECAUSE","IT","WAS","THE","START","OF","THE","NEW","TV","SEASON","I","HAVE","HAD","TO","WATCH","FIVE","MONTHS","OF","RERUNS","SO","YOU","CAN","PROBABLY","UNDERSTAND","THAT","I","WAS","FIRED","UP","TO","FINALLY","SEE","SOMETHING","NEW"},
                new string[]{"DAD","PUT","MANNY","TO","BED","AND","MOM","MADE","POPCORN","AND","I","WAS","ALL","SET","FOR","SOME","SERIOUS","TELEVISION","WATCHING","BUT","FIVE","MINUTES","INTO","THE","FIRST","SHOW","MANNY","MAKES","AN","APPEARANCE","IN","THE","FAMILY","ROOM"},
                new string[]{"BUT","INSTEAD","OF","PUTTING","MANNY","RIGHT","BACK","TO","BED","MOM","LET","HIM","STAY","UP","AND","WATCH","TV","WITH","US"},
                new string[]{"AND","HERES","THE","KICKER","MOM","MADE","ME","CHANGE","THE","CHANNEL","BECAUSE","ON","THE","SHOW","I","WAS","WATCHING","THE","KIDS","HAD","A","DISRESPECTFUL","ATTITUDE","TOWARDS","THE","ADULTS","AND","SHE","DIDNT","WANT","MANNY","EXPOSED","TO","THAT","SORT","OF","THING"},
                new string[]{"COP","SHOWS","WERE","OUT","TOO","BECAUSE","OF","THE","VIOLENCE","SO","GUESS","WHAT","MOM","MADE","ME","TURN","TO","THE","CARTOON","CHANNEL","WHICH","IS","EXACTLY","WHAT","MANNY","WAS","WATCHING","BEFORE","HE","WENT","TO","BED"},
                new string[]{"MAN","I","WAS","STEAMED","WHEN","I","WAS","A","KID","THERE","WASNT","ANY","OF","THIS","GETTING","OUT","OF","BED","AND","COMING","BACK","DOWNSTAIRS","STUFF"},
                new string[]{"I","THINK","I","DID","IT","ONCE","OR","TWICE","BUT","DAD","PUT","A","STOP","TO","IT","REAL","QUICK"},
                new string[]{"THERE","WAS","THIS","BOOK","DAD","USED","TO","READ","TO","ME","BEFORE","I","WENT","TO","SLEEP","CALLED","A","LIGHT","IN","THE","ATTIC"},
                new string[]{"IT","WAS","AN","AWESOME","BOOK","BUT","ON","THE","BACK","OF","IT","THERE","WAS","A","PICTURE","OF","THE","GUY","WHO","WROTE","IT","SHEL","SILVERSTEIN"},
                new string[]{"HAVE","YOU","EVER","SEEN","THAT","GUY","ALL","I","CAN","SAY","IS","THAT","HE","LOOKS","MORE","LIKE","A","BURGLAR","OR","A","PIRATE","THAN","A","GUY","WHO","WRITES","POEMS","FOR","KIDS"},
                new string[]{"WELL","DAD","USED","TO","LEAVE","THAT","BOOK","RIGHT","ON","MY","END","TABLE","EVERY","NIGHT","WITH","THE","BACK","OF","THE","BOOK","FACING","UP","AND","IT","REALLY","GAVE","ME","THE","HEEBIE","JEEBIES"},
                new string[]{"I","THINK","DAD","CAUGHT","ON","THAT","SHEL","SILVERSTEIN","KIND","OF","FREAKED","ME","OUT","BECAUSE","THE","FIRST","NIGHT","AFTER","I","PULLED","THE","KIND","OF","THING","MANNY","PULLED","TONIGHT","DAD","READ","ME","SOME","POEMS","FROM","A","LIGHT","IN","THE","ATTIC","AND","THEN","SAID","IF","YOU","GET","UP","AGAIN","TONIGHT","I","WONDER","IF","YOULL","RUN","INTO","SHEL","SILVERSTEIN","IN","THE","HALLWAY"},
                new string[]{"WELL","DAD","REALLY","HAD","MY","NUMBER","WITH","THAT","TRICK","I","NEVER","GOT","UP","AGAIN","NOT","EVEN","TO","GO","TO","THE","BATHROOM","I","WOULD","RATHER","HAVE","WET","THE","BED","THAN","TO","FIND","THAT","GUY","CREEPING","AROUND","UPSTAIRS"}
            },
            new string[][]
            {
                new string[]{"LAST","NIGHT","MANNY","FINALLY","WENT","TO","BED","AND","I","FINALLY","GOT","TO","WATCH","SOME","OF","THE","NEW","SHOWS","BUT","IVE","GOT","TO","SAY","IT","WASNT","WORTH","THE","WAIT"},
                new string[]{"ALL","THE","NEW","SITCOMS","ARE","THE","SAME","THEY","BASICALLY","TAKE","ONE","LAME","JOKE","AND","THEN","DRIVE","IT","INTO","THE","GROUND","FOR","THE","NEXT",snlets[1] + "" + snlets[0], "MINUTES","I","WANTED","TO","SEE","IF","I","COULD","WRITE","A","BETTER","SHOW","THAN","THESE","CLOWNS","WHO","ARE","MAKING","THOUSANDS","OF","DOLLARS","SO","AT","LUNCH","TODAY","I","GAVE","IT","A","SHOT"},
                new string[]{"I","SHOWED","MY","DRAWINGS","TO","MOM","AND","SHE","SURPRISED","ME","BY","BEING","REALLY","INTERESTED","SHE","TOLD","ME","THAT","IF","I","REALLY","WANTED","TO","WRITE","A","TV","SHOW","I","HAD","TO","COME","UP","WITH","A","WHOLE","PLOT","WITH","A","BEGINNING","AND","END","I","THINK","SHE","WAS","JUST","HAPPY","I","WAS","SHOWING","AN","INTEREST","IN","SOMETHING","OTHER","THAN","A","VIDEO","GAME","FOR","ONCE"},
                new string[]{"IT","SEEMS","LIKE","EVERY","NEW","SHOW","THAT","COMES","OUT","NOWADAYS","IS","ABOUT","SOME","DAD","WHO","DOES","OR","SAYS","SOMETHING","REALLY","IGNORANT","AT","THE","BEGINNING","OF","THE","SHOW","BUT","THEN","BY","THE","END","OF","THE","SHOW","HE","COMES","AROUND","AND","REALIZES","HE","WAS","BEING","A","NINCOMPOOP","SO","THE","NEXT","SHOW","I","WROTE","(CALLED","WISE","UP","MR","LOCKERMAN)","IS","BASED","ON","THAT","KIND","OF","IDEA"},
                new string[]{"I","SHOWED","THIS","SCRIPT","TO","MOM","WHEN","I","WAS","DONE","TO","BE","HONEST","WITH","YOU","I","DONT","THINK","SHE","REALLY","APPROVES","OF","MY","TYPE","OF","HUMOR","BUT","SHE","IS","PRETTY","EXCITED","TO","SEE","ME","WORKING","ON","THIS","STUFF","USUALLY","WHEN","MOM","GETS","TOO","ENTHUSIASTIC","ABOUT","SOMETHING","IM","DOING","THATS","MY","SIGNAL","TO","BACK","OFF","BUT","MOM","SAID","SHES","GOING","TO","TRY","TO","GET","A","HOLD","OF","A","VIDEO","CAMERA","FROM","WORK","TO","LET","ME","PUT","SOME","OF","MY","IDEAS","ON","FILM","SO","I","GUESS","I","CAN","DEAL","WITH","IT","FOR","NOW"},
                new string[]{"LAST","NIGHT","I","WAS","SO","EXCITED","ABOUT","GETTING","MY","HANDS","ON","A","VIDEO","CAMERA","I","COULDNT","SLEEP","SO","TODAY","IN","SCHOOL","I","FAKED","ABOUT","BEING","SICK","IN","GYM","SO","I","COULD","WRITE","DOWN","MY","IDEA","FOR","A","MOVIE"},
                new string[]{"MOM","WAS","ABLE","TO","GET","A","CAMERA","FROM","WORK","SO","I","FINISHED","UP","THE","SCRIPT","AND","BROUGHT","IT","UP","TO","ROWLEYS","HOUSE","TO","SHOW","HIM","I","THINK","ITS","MY","BEST","STUFF","YET","AND","THATS","SAYING","SOMETHING"},
                new string[]{"WELL","ROWLEYS","REACTION","WASNT","QUITE","WHAT","I","WAS","HOPING","FOR"},
                new string[]{"YOUD","THINK","ROWLEY","WOULD","BE","GRATEFUL","THAT","I","WAS","GOING","TO","MAKE","HIM","A","BIG","STAR","AND","OF","COURSE","I","GOT","NO","THANKS","FOR","WRITING","JUICY","ROLES","FOR","HIS","PARENTS"},
                new string[]{"TODAY","AFTER","SCHOOL","I","WALKED","UP","TO","ROWLEYS","HOUSE","TO","SHOW","HIM","SOME","REWRITES","I","DID","FOR","THE","BOY","WHOSE","FAMILY","THOUGHT","HE","WAS","A","DOG","BUT","ROWLEY","WOULDNT","ANSWER","THE","DOOR"},
                new string[]{"I","STARTED","TO","HEAD","HOME","AND","THEN","ALL","OF","THE","SUDDEN","MOM","PULLS","UP","ALONG","SIDE","ME","WITH","MACKIE","CREAVY","IN","THE","BACK","SEAT","OF","THE","CAR"},
                new string[]{"OH","MAN","I","COMPLETELY","FORGOT","ABOUT","SOCCER","DAD","MAKES","ME","DO","IT","EVERY","YEAR","SO","IM","WELL","ROUNDED"}
            },
            new string[][]
            {
                new string[]{"SO","I","GUESS","IVE","GOT","TO","PUT","MY","FILM","CAREER","ON","HOLD","AND","GO","GET","MY","SHINS","KICKED","FOR","A","COUPLE","OF","MONTHS"},
                new string[]{"THE","FIRST","NIGHT","OF","SOCCER","PRACTICE","IS","ALWAYS","THE","SAME","THEY","START","OFF","BY","DOING","THIS","SKILLS","TEST","TO","SEE","HOW","GOOD","YOU","ARE","USUALLY","I","DONT","CARE","HOW","I","GET","RANKED","BUT","THIS","YEAR","THE","GUY","WHO","WAS","DOING","THE","TESTING","WAS","MR","MATTHEWS","WHO","IS","THE","FATHER","OF","PIPER","MATTHEWS","THE","PRETTIEST","GIRL","IN","OUR","CHURCH","SO","I","FIGURED","ID","BETTER","DO","MY","BEST","IF","I","WANTED","TO","IMPRESS","MY","FUTURE","FATHERINLAW"},
                new string[]{"EVEN","THOUGH","I","TRIED","MY","HARDEST","I","STILL","GOT","RANKED","PRE","ALPHA","MINUS","WHICH","IS","ADULT","CODE","WORDS","FOR","YOU","STINK"},
                new string[]{"THE","NEXT","THING","THEY","DO","IS","PUT","EVERYONE","ON","A","TEAM","THEY","BASICALLY","TRY","TO","SPREAD","OUT","THE","REALLY","AWFUL","KIDS","LIKE","ME","SO","NO","ONE","TEAM","HAS","TOO","MANY","TERRIBLE","PLAYERS"},
                new string[]{"AND","WOULDNT","YOU","KNOW","MY","LUCK","I","GOT","PUT","ON","KENNY","KEITHS","TEAM","SO","THAT","MEANS","MY","COACH","IS","MR","KEITH","SAME","AS","LAST","YEAR"},
                new string[]{"MR","KEITH","HATES","ME","AND","I","TRACE","IT","ALL","BACK","TO","THE","FIRST","DAY","OF","PRACTICE","LAST","YEAR","A","BUNCH","OF","US","TERRIBLE","PLAYERS","WERE","SLACKING","OFF","HANGING","OUT","BY","THE","WATER","JUG","WHEN","MR","KEITH","YELLED","FOR","US","TO","GET","BACK","ON","THE","PLAYING","FIELD"},
                new string[]{"SO","AS","A","JOKE","I","RAN","BACKWARDS","WITH","MY","REAR","END","POINTED","AT","MR","KEITH"},
                new string[]{"I","THINK","IT","WOULD","HAVE","BEEN","FUNNIER","IF","ALL","THE","OTHER","GUYS","HAD","DONE","THE","SAME","THING","I","DID","BUT","THEY","KIND","OF","HUNG","ME","OUT","TO","DRY"},
                new string[]{"ANYWAY","YOU","CAN","PROBABLY","GUESS","THAT","MR","KEITH","DID","NOT","FIND","MY","JOKE","SO","AMUSING","AND","FROM","THEN","ON","HE","MADE","THINGS","PRETTY","MISERABLE","FOR","ME"},
                new string[]{"RIGHT","BEFORE","THE","FIRST","GAME","HE","GAVE","US","ALL","OUR","POSITIONS","AND","HE","TOLD","ME","MY","POSITION","WAS","SHAG","I","DIDNT","KNOW","A","WHOLE","LOT","ABOUT","SOCCER","BUT","I","WAS","PRETTY","PROUD","THAT","I","HAD","MY","VERY","OWN","POSITION","I","REMEMBER","BRAGGING","TO","RODRICK","ABOUT","IT"},
                new string[]{"BUT","RODRICK","KNEW","A","THING","OR","TWO","ABOUT","SOCCER","AND","HE","TOLD","ME","THAT","THE","SHAG","ISNT","ACTUALLY","A","REAL","POSITION","ON","THE","FIELD","ITS","JUST","A","KID","WHO","CHASES","ALL","THE","BALLS","THAT","GO","OUT","OF","BOUNDS"},
                new string[]{"AND","SURE","ENOUGH","RODRICK","WAS","RIGHT","MR","KEITH","NEVER","PUT","ME","IN","A","GAME","AND","I","WASNT","EVEN","THE","WORST","KID","ON","OUR","TEAM","WE","HAD","COLLIN","AND","MACKIE","CREAVY","AND","A","COUPLE","OTHER","KIDS","WHO","CAN","BARELY","KICK","A","SOCCER","BALL","AND","THERE","I","WAS","CHASING","BALLS","INTO","THE","STREET","AND","LET","ME","JUST","SAY","SOMETHING","IN","DEFENSE","OF","ALL","THOSE","SHAGS","OUT","THERE","SHAG","MIGHT","NOT","BE","THE","MOST","NOBLE","POSITION","IN","SOCCER","BUT","IT","IS","DEFINITELY","THE","MOST","STRESSFUL"}
            },
            new string[][]
            {
                new string[]{"TONIGHT","I","WAS","ALL","SET","TO","GO","OVER","TO","COLLINS","HOUSE","BUT","I","FOUND","OUT","DAD","HAD","RENTED","A","MOVIE","SO","I","CHANGED","MY","PLANS","AND","STAYED","HOME","WHENEVER","DAD","GETS","A","MOVIE","HE","NEVER","CHECKS","THE","RATING","SO","ITS","ALWAYS","WORTH","HANGING","AROUND","AND","SEEING","WHAT","HE","PICKED","OUT","AND","HALF","THE","TIME","HE","GETS","SOMETHING","MOM","WOULD","NEVER","LET","ME","WATCH","ON","MY","OWN"},
                new string[]{"THE","ONLY","DOWN","SIDE","ABOUT","WATCHING","MOVIES","WITH","DAD","IS","THAT","IF","THERE","IS","EVER","A","SCENE","WITH","ANYTHING","THE","LEAST","BIT","INAPPROPRIATE","SOMEHOW","MOM","SHOWS","UP","AT","THE","WORST","MOMENT","AND","MAKES","YOU","FEEL","ASHAMED","FOR","WATCHING","IT"},
                new string[]{"LUCKILY","I","HAVE","MASTERED","THE","KIND","OF","RESPONSE","THAT","GETS","ME","OFF","THE","HOOK","EVERY","TIME","ESPECIALLY","DURING","THE","RACY","SCENES"},
                new string[]{"I","JUST","MAKE","SURE","I","HEAD","BACK","DOWNSTAIRS","LATER","ON","TO","CATCH","UP","ON","ANYTHING","I","MISSED"},
                new string[]{"TODAY","AFTER","CHURCH","MOM","AND","I","WENT","OVER","TO","GRAMMAS","TO","CHECK","UP","ON","HER","MOM","WAS","PRETTY","WORRIED","BECAUSE","GRAMMA","HASNT","BEEN","ANSWERING","HER","PHONE","FOR","A","FEW","DAYS","SO","MOM","WANTED","TO","MAKE","SURE","GRAMMA","WAS","OK","BUT","WHEN","WE","GOT","THERE","WE","FOUND","GRAMMA","SITTING","IN","HER","KITCHEN","CLIPPING","COUPONS","LIKE","USUAL","SO","WHEN","MOM","ASKED","GRAMMA","WHY","SHE","HASNT","BEEN","ANSWERING","THE","TELEPHONE","GRAMMA","SAID","CORDLESS","TELEPHONES","ERASE","THE","MEMORY","OF","THE","ELDERLY"},
                new string[]{"WELL","THAT","KIND","OF","SET","MOM","OFF","BECAUSE","SHE","KNEW","EXACTLY","WHERE","GRAMMA","WAS","GETTING","HER","INFORMATION","FROM","THE","SUPERMARKET","TABLOIDS","BUT","SOMEHOW","GRAMMA","KEEPS","GETTING","A","HOLD","OF","THESE","THINGS","EVEN","THOUGH","SHE","DOESNT","DRIVE"},
                new string[]{"SO","WHEN","MOM","CONFRONTED","GRAMMA","ON","IT","AND","SAID","WHERE","DID","YOU","READ","THAT","MOM","GRAMMA","KNEW","SHE","WAS","CORNERED"},
                new string[]{"SO","MOM","FOUND","WHERE","GRAMMA","WAS","STASHING","THE","TABLOIDS","AND","WE","TOOK","IT","HOME","WITH","US","TO","THROW","AWAY","WHAT","MOM","DOESNT","KNOW","IS","THAT","I","ALWAYS","DIG","THOSE","THINGS","OUT","OF","THE","TRASH","AND","READ","THEM","WHEN","NO","ONES","AROUND"},
                new string[]{"THERES","ACTUALLY","A","BUNCH","OF","GOOD","STUFF","IN","THERE","LIKE","HOROSCOPES","AND","PREDICTIONS","IN","FACT","THE","REASON","I","TAKE","SCHOOL","WITH","A","GRAIN","OF","SALT","IS","BECAUSE","THIS","ONE","TABLOID","SAYS","THE","WHOLE","EAST","COAST","IS","GOING","TO","BE","UNDERWATER","WITHIN","FIVE","YEARS"},
                new string[]{"I","DONT","KNOW","IF","I","EVER","MENTIONED","THIS","BEFORE","BUT","EVERY","MORNING","WHEN","DAD","WAKES","ME","UP","HE","GIVES","THE","SAME","EXACT","SPEECH","ITS","TEN","OF","SEVEN","MOMS","IN","THE","SHOWER","AND","I","WANT","YOU","IN","THERE","THE","SECOND","SHES","OUT","SO","YOURE","NOT","LATE","FOR","YOUR","BUS","LETS","MOVE","IT","HUP","HUP","HUP"},
                new string[]{"I","DONT","KNOW","WHERE","DAD","GETS","HIS","MORNING","ENERGY","BUT","I","DEFINITELY","DID","NOT","INHERIT","THAT","GENE","FROM","HIM","AFTER","HE","WAKES","ME","UP","I","PROP","MYSELF","UP","ON","MY","ELBOW","AND","TRY","MY","HARDEST","NOT","TO","FALL","BACK","ASLEEP"}
            },
            new string[][]
            {
                new string[]{"THIS","ONE","DAY","I","ACCIDENTALLY","FELL","BACK","ASLEEP","AFTER","DAD","WOKE","ME","UP","AND","BELIEVE","ME","IT","WAS","THE","LAST","TIME","I","EVER","MADE","THAT","MISTAKE"},
                new string[]{"AT","SCHOOL","THIS","MORNING","THERE","WERE","A","BUNCH","OF","KIDS","FROM","MRS","BUNNS","HOMEROOM","CLASS","STANDING","AROUND","IN","THE","HALLWAY","TRIPPING","EVERY","OTHER","KID","THAT","WALKED","BY"},
                new string[]{"ITS","REALLY","SAD","TO","SEE","WHAT","PASSES","FOR","COMEDY","THESE","DAYS","BACK","IN","THE","FIFTH","GRADE","ME","AND","BEN","WERE","AN","AWESOME","COMEDY","TEAM","AND","WE","HAD","SOME","REALLY","GOOD","ROUTINES"},
                new string[]{"BUT","EVER","SINCE","BEN","LEFT","TOWN","THE","FUNNIEST","THING","THAT","EVER","HAPPENS","IN","SCHOOL","IS","WHEN","SOME","POOR","KID","DROPS","HIS","LUNCH","TRAY","IN","THE","CAFETERIA"},
                new string[]{"I","TRIED","TO","PICK","THE","COMEDY","THING","BACK","UP","WHEN","ROWLEY","CAME","ALONG","BUT","THINGS","NEVER","REALLY","WORKED","OUT"},
                new string[]{"I","THOUGHT","OF","ANOTHER","GOOD","REASON","TO","KEEP","A","JOURNAL","WHEN","I","GET","RICH","AND","FAMOUS","I","CAN","PULL","OUT","THIS","BOOK","TO","REMIND","MYSELF","WHY","I","SHOULDNT","LET","RODRICK","SWIM","IN","MY","POOL","OR","USE","MY","BOWLING","ALLEY","OR","ANYTHING","ELSE","LIKE","THAT"},
                new string[]{"TONIGHT","RODRICK","PULLED","HIS","GETOUTOFDOINGTHEDISHES","ROUTINE","JUST","LIKE","HE","DOES","EVERY","NIGHT","DAD","HAS","A","RULE","THAT","WERE","NOT","ALLOWED","TO","WATCH","TV","UNTIL","THE","DISHES","ARE","DONE","BUT","RIGHT","AFTER","DINNER","RODRICK","ALWAYS","GOES","UPSTAIRS","TO","THE","BATHROOM","AND","DOESNT","COME","DOWN","FOR","SOMETHING","LIKE",snlets[0] + "" + snlets[1], "MINUTES","BY","THAT","TIME","IVE","DONE","ALL","THE","DISHES","MYSELF"},
                new string[]{"WELL","TONIGHT","I","SAID","ENOUGH","IS","ENOUGH","AND","I","WENT","TO","COMPLAIN","TO","MOM","AND","DAD","BUT","OF","COURSE","RODRICK","HAD","AN","EXCUSE"},
                new string[]{"ALL","I","CAN","SAY","IS","THAT","IF","RODRICK","WANTS","TO","HANG","OUT","IN","MY","MANSION","WHEN","WERE","GROWN","UP","HE","BETTER","BRING","A","TOWEL","AND","SOME","SPONGES","BECAUSE","HES","GOING","TO","BE","DOING","A","WHOLE","LOT","OF","DISHES"},
                new string[]{"TONIGHT","SOCCER","PRACTICE","ENDED","A","FEW","MINUTES","EARLY","SO","THE","COACH","COULD","HAND","OUT","UNIFORMS","AND","WE","COULD","COME","UP","WITH","A","TEAM","NAME"},
                new string[]{"I","SUGGESTED","TWISTED","WIZARDS","AND","SAID","MAYBE","WE","COULD","GET","THE","GAME","ZONE","TO","SPONSOR","US","BUT","OF","COURSE","MY","PERFECTLY","GOOD","IDEA","GOT","SHOT","DOWN"},
                new string[]{"A","BUNCH","OF","OTHER","IDEAS","GOT","TOSSED","AROUND","UNTIL","SOME","IDIOT","CAME","UP","WITH","THE","NAME","RED","SOX","I","COULDNT","BELIEVE","IT","WHEN","A","BUNCH","OF","KIDS","THOUGHT","THAT","WAS","A","REALLY","GOOD","IDEA","AND","GUESS","WHAT","THATS","WHAT","EVERYONE","VOTED","ON"},
                new string[]{"NOW","NUMBER","ONE","THE","RED","SOX","IS","A","BASEBALL","TEAM","NOT","A","SOCCER","TEAM","AND","NUMBER","TWO","OUR","UNIFORMS","ARE","BLUE","INCLUDING","THE","SOCKS","BUT","OF","COURSE","NOBODY","WOULD","LISTEN","TO","ME"}
            }
        };
        string storedLetters = "";
        List<List<string>> storedPositions = new List<List<string>>();
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var screens = new string[5];
        for (int aa = 0; aa < word.Length; aa++)
        {
            if (!(storedLetters.Contains(word[aa])))
            {
                storedLetters = storedLetters + "" + word[aa];
                List<string> tempStorage = new List<string>();
                for (int bb = 0; bb < diary.Length; bb++)
                {
                    for (int cc = 0; cc < diary[bb].Length; cc++)
                    {
                        for (int dd = 0; dd < diary[bb][cc].Length; dd++)
                        {
                            for (int ee = 0; ee < diary[bb][cc][dd].Length; ee++)
                            {
                                if (diary[bb][cc][dd][ee] == word[aa])
                                {
                                    if (dd < 9)
                                        tempStorage.Add(alpha[bb] + "" + alpha[cc] + "0" + (dd + 1) + "" + alpha[ee]);
                                    else
                                        tempStorage.Add(alpha[bb] + "" + alpha[cc] + "" + (dd + 1) + "" + alpha[ee]);
                                }
                            }
                        }
                    }
                }
                storedPositions.Add(tempStorage);
            }
            int cursor = storedLetters.IndexOf(word[aa]);
            string temp = storedPositions[cursor][Rnd.Range(0, storedPositions[cursor].Count)];
            if (invert)
                Log("INV BROWN", "{0} -> {1}", word[aa], temp);
            else
                Log("BROWN", "{0} -> {1}", word[aa], temp);
            for (var i = 0; i < 5; i++)
                screens[i] += temp[i];
        }
        return screens.Select(scr => new ScreenText(scr, 40)).ToArray();
    }
    #endregion

    #region Cornflower Cipher
    protected PageInfo[] cornflowercipher(string word, bool invert = false)
    {
        var bitsInt = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber().First());
        var bits = Enumerable.Range(0, 5).Select(bit => (bitsInt & (1 << bit)) != 0).ToArray();
        string encrypted;
        if (invert)
        {
            Log("INV CORNFLOWER", "Begin Stunted Blind Polybius Cipher");
            var polybiusResult = StuntedBlindPolybiusEnc(word, bits, invert);

            Log("INV CORNFLOWER", "Begin Chain Rotation Cipher");
            var chainRotationN = Rnd.Range(1, 10);
            encrypted = ChainRotationEnc(polybiusResult.Encrypted, chainRotationN, invert);

            Log("INV CORNFLOWER", "Begin Straddling Checkerboard Cipher");
            var kw1 = pickWord(4, 8);
            var kw2 = pickWord(4, 8);
            var encryptKW3 = StraddlingCheckerboardEnc(polybiusResult.Keyword3, kw1, kw2, bits, invert);

            return newArray(
                new PageInfo(
                    new ScreenText(encrypted, 40),
                    new ScreenText(chainRotationN.ToString(), 40),
                    new ScreenText(encryptKW3.Substring(0, encryptKW3.Length / 2), 40)),
                new PageInfo(
                    new ScreenText(encryptKW3.Substring(encryptKW3.Length / 2), 40),
                    new ScreenText(kw1, 35),
                    new ScreenText(kw2, 35)));
        }
        else
        {
            Log("CORNFLOWER", "Begin Chain Rotation Cipher");
            var chainRotationN = Rnd.Range(1, 10);
            encrypted = ChainRotationEnc(word, chainRotationN, invert);

            Log("CORNFLOWER", "Begin Stunted Blind Polybius Cipher");
            var polybiusResult = StuntedBlindPolybiusEnc(encrypted, bits, invert);

            Log("CORNFLOWER", "Begin Straddling Checkerboard Cipher");
            var kw1 = pickWord(4, 8);
            var kw2 = pickWord(4, 8);
            var encryptKW3 = StraddlingCheckerboardEnc(polybiusResult.Keyword3, kw1, kw2, bits, invert);

            return newArray(
                new PageInfo(
                    new ScreenText(polybiusResult.Encrypted.Substring(0, 6), 40),
                    new ScreenText(polybiusResult.Encrypted.Substring(6) + " " + chainRotationN, 40),
                    new ScreenText(encryptKW3.Substring(0, encryptKW3.Length / 2), 40)),
                new PageInfo(
                    new ScreenText(encryptKW3.Substring(encryptKW3.Length / 2), 40),
                    new ScreenText(kw1, 35),
                    new ScreenText(kw2, 35)));
        }
    }
    private string ChainRotationEnc(string word, int n, bool invert)
    {
        var encrypt = "";
        if (invert)
        {
            Log("INV CORNFLOWER", "Before Chain Rotation Cipher: {0}", word);
            while (word.Length > 0)
            {
                var amt = n % word.Length;
                word = word.Substring(amt) + word.Substring(0, amt);
                var obt = word[0];
                word = word.Substring(1);
                if (encrypt.Length > 0)
                    obt = (char) ((obt - 'A' + encrypt[encrypt.Length - 1] - 'A' + 1) % 26 + 'A');
                encrypt += obt;
                Log("INV CORNFLOWER", "{0} -> {1}", word, encrypt);
            }
            Log("INV CORNFLOWER", "After Chain Rotation Cipher: {0}", encrypt);
            Log("INV CORNFLOWER", "Chain Rotation Cipher amount: {0}", n);
        }
        else
        {
            while (word.Length > 0)
            {
                var obt = word[word.Length - 1];
                word = word.Remove(word.Length - 1);
                if (word.Length > 0)
                    obt = (char) ('A' + (obt - 'A' + 52 - (word[word.Length - 1] - 'A' + 1)) % 26);
                encrypt = obt + encrypt;
                var amt = n % encrypt.Length;
                encrypt = encrypt.Substring(encrypt.Length - amt) + encrypt.Substring(0, encrypt.Length - amt);
            }
            Log("CORNFLOWER", "Before Chain Rotation Cipher: {0}", encrypt);
            Log("CORNFLOWER", "Chain Rotation Cipher amount: {0}", n);
        }
        return encrypt;
    }
    private struct CornflowerResult { public string Encrypted, Keyword3; }
    private CornflowerResult StuntedBlindPolybiusEnc(string word, bool[] bits, bool invert)
    {
        string encrypted, kw3;
        if (invert)
        {
            var braille1 = brailleDots.Where(dots => Enumerable.Range(0, 3).All(i => dots.Contains((char) (i + '4')) == (word[i] > 'P'))).PickRandom();
            var braille2 = brailleDots.Where(dots => Enumerable.Range(0, 3).All(i => dots.Contains((char) (i + '4')) == (word[i + 3] > 'P'))).PickRandom();
            Log("INV CORNFLOWER", "Braille 5: {0}", braille1);
            Log("INV CORNFLOWER", "Braille 6: {0}", braille2);
            word = word.Select(ch => ch > 'P' ? (char) (ch - 13) : ch).Join("");
            Log("INV CORNFLOWER", "After ROT-13: {0}", word);
            var kw3result = FindKW3(bits[0], word, wordList[4].ToList());
            kw3 = kw3result.Keyword3;
            Log("INV CORNFLOWER", "KW3: {0}", kw3);
            Log("INV CORNFLOWER", "Braille 1-4: {0}", kw3result.Encrypted);
            wordList[4].Remove(kw3);
            encrypted = kw3result.Encrypted + (char) (Array.IndexOf(brailleDots, braille1) + 'A') + (char) (Array.IndexOf(brailleDots, braille2) + 'A');
            Log("INV CORNFLOWER", "Braille: {0}", encrypted.Select(ch => "⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵"[ch - 'A']).Join(""));
            Log("INV CORNFLOWER", "Encrypted Word: {0}", encrypted);
        }
        else
        {
            kw3 = pickWord(8);
            Log("CORNFLOWER", "Braille: {0}", toBraille(word));
            var brailleDots = "1,12,14,145,15,124,1245,125,24,245,13,123,134,1345,135,1234,12345,1235,234,2345,136,1236,2456,1346,13456,1356"
                .Split(',').Select(d => Enumerable.Range(0, 6).Select(i => d.Contains((char) ('1' + i))).ToArray()).ToArray();

            // “nibble” = 4 bits. Some nibbles are a 2×2 square within a Braille letter, some are the bottom 2 dots of one Braille letter and the top 2 dots of the next
            var brailleNibbles = new int[(word.Length * 3 + 1) / 2];
            for (int i = 0; i < word.Length; i++)
                for (var dot = 0; dot < 6; dot++)
                    if (brailleDots[word[i] - 'A'][dot])
                        brailleNibbles[(dot % 3 + 3 * i) / 2] |= 1 << ((dot % 3 + 3 * i) % 2) * 2 + dot / 3;
            Log("CORNFLOWER", "Braille nibbles: {0}", brailleNibbles.Select(i => "⠀⠁⠈⠉⠂⠃⠊⠋⠐⠑⠘⠙⠒⠓⠚⠛"[i]).Join(""));
            Log("CORNFLOWER", "KW3: {0}", kw3);
            var colSeq = sequencing(kw3.Substring(0, 4));
            var rowSeq = sequencing(kw3.Substring(4));
            Log("CORNFLOWER", "Blind Polybius columns: {0}; rows: {1}", colSeq.Select(i => i + 1).Join(""), rowSeq.Select(i => i + 1).Join(""));

            var polybius = (bits[0] ? (kw3 + "ABCDEFGHIJKLMNOP") : "ABCDEFGHIJKLMNOP".Except(kw3).Concat(kw3)).Distinct().Where(ch => ch <= 'P').Join("");
            Log("CORNFLOWER", "Stunted Polybius square: {0}", polybius);

            encrypted = brailleNibbles.Select(nibble => polybius[colSeq[nibble % 4] + 4 * rowSeq[nibble / 4]]).Join("");
            Log("CORNFLOWER", "Encrypted word: {0}", encrypted);
        }
        return new CornflowerResult { Encrypted = encrypted, Keyword3 = kw3 };
    }
    private CornflowerResult FindKW3(bool bit0, string word, List<string> eightLetterWords)
    {
        while (eightLetterWords.Count > 0)
        {
            var kw3ix = Rnd.Range(0, eightLetterWords.Count);
            var kw3 = eightLetterWords[kw3ix];
            eightLetterWords.RemoveAt(kw3ix);
            var colSeq = sequencing(kw3.Substring(0, 4));
            var rowSeq = sequencing(kw3.Substring(4));
            var polybius = (bit0 ? (kw3 + "ABCDEFGHIJKLMNOP") : "ABCDEFGHIJKLMNOP".Except(kw3).Concat(kw3)).Distinct().Where(ch => ch <= 'P').Join("");

            var stunted = word.Select(ch => polybius.IndexOf(ch)).Select(i => Array.IndexOf(colSeq, i % 4) + 4 * Array.IndexOf(rowSeq, i / 4)).ToArray();

            var braille1 = (stunted[0] & 1) | ((stunted[0] & 4) >> 1) | ((stunted[1] & 1) << 2) | ((stunted[0] & 2) << 2) | ((stunted[0] & 8) << 1) | ((stunted[1] & 2) << 4);
            var braille1ltr = Array.IndexOf(brailleDots, Enumerable.Range(1, 6).Where(bit => (braille1 & (1 << (bit - 1))) != 0).Join(""));
            if (braille1ltr == -1)
                continue;
            var braille2 = ((stunted[1] & 4) >> 2) | ((stunted[2] & 1) << 1) | ((stunted[2] & 4) << 0) | ((stunted[1] & 8) << 0) | ((stunted[2] & 2) << 3) | ((stunted[2] & 8) << 2);
            var braille2ltr = Array.IndexOf(brailleDots, Enumerable.Range(1, 6).Where(bit => (braille2 & (1 << (bit - 1))) != 0).Join(""));
            if (braille2ltr == -1)
                continue;

            var braille3 = (stunted[3] & 1) | ((stunted[3] & 4) >> 1) | ((stunted[4] & 1) << 2) | ((stunted[3] & 2) << 2) | ((stunted[3] & 8) << 1) | ((stunted[4] & 2) << 4);
            var braille3ltr = Array.IndexOf(brailleDots, Enumerable.Range(1, 6).Where(bit => (braille3 & (1 << (bit - 1))) != 0).Join(""));
            if (braille3ltr == -1)
                continue;

            var braille4 = ((stunted[4] & 4) >> 2) | ((stunted[5] & 1) << 1) | ((stunted[5] & 4) << 0) | ((stunted[4] & 8) << 0) | ((stunted[5] & 2) << 3) | ((stunted[5] & 8) << 2);
            var braille4ltr = Array.IndexOf(brailleDots, Enumerable.Range(1, 6).Where(bit => (braille4 & (1 << (bit - 1))) != 0).Join(""));
            if (braille4ltr == -1)
                continue;
            Log("INV CORNFLOWER", "Blind Polybius columns: {0}; rows: {1}", colSeq.Select(i => i + 1).Join(""), rowSeq.Select(i => i + 1).Join(""));
            Log("INV CORNFLOWER", "Stunted Polybius square: {0}", polybius);
            return new CornflowerResult { Keyword3 = kw3, Encrypted = string.Format("{0}{1}{2}{3}", (char) ('A' + braille1ltr), (char) ('A' + braille2ltr), (char) ('A' + braille3ltr), (char) ('A' + braille4ltr)) };
        }
        throw new InvalidOperationException("Inverted Cornflower Cipher: internal error: ran out of keywords (no keywords work)");
    }
    private string StraddlingCheckerboardEnc(string word, string kw1, string kw2, bool[] bits, bool invert)
    {
        var d1 = Bomb.GetIndicators().Count() % 6;
        var d2 = Bomb.GetPortCount() % 6;
        if (d2 == d1)
            d2 = (d2 + 1) % 6;

        var d3 = Bomb.GetBatteryCount() % 6;
        var d4 = Bomb.GetOnIndicators().Count() % 6;
        if (d4 == d3)
            d4 = (d4 + 1) % 6;

        // Backward Straddling Checkerboard Cipher
        var rowDigits2 = Enumerable.Range(0, 6).Where(d => d != d3 && d != d4).ToArray();
        var straddlingCheckerboard2 = MakeStraddlingCheckerboard(bits[1], bits[2], kw2, rowDigits2);

        var encryptedDigits = new List<int>();
        foreach (var ch in word)
        {
            var ix = straddlingCheckerboard2.IndexOf(ch);
            if (ix >= 6)
                encryptedDigits.Add(rowDigits2[ix / 6 - 1]);
            encryptedDigits.Add(ix % 6);
        }

        // Forward Straddling Checkerboard Cipher
        var rowDigits1 = Enumerable.Range(0, 6).Where(d => d != d1 && d != d2).ToArray();
        var straddlingCheckerboard1 = MakeStraddlingCheckerboard(bits[3], bits[4], kw1, rowDigits1);

        var encrypt = "";
        for (var i = 0; i < encryptedDigits.Count; i++)
        {
            if (encryptedDigits[i] == d1 || encryptedDigits[i] == d2)
                encrypt += straddlingCheckerboard1[encryptedDigits[i]];
            else
            {
                if (i == encryptedDigits.Count - 1)
                    encryptedDigits.Add(rowDigits2.Where(d => d != d1 && d != d2).First());
                encrypt += straddlingCheckerboard1[(Array.IndexOf(rowDigits1, encryptedDigits[i]) + 1) * 6 + encryptedDigits[i + 1]];
                i++;
            }
        }

        Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Backward Straddling Checkerboard Cipher: KW2: {0}, D3: {1}, D4: {2}", kw2, d3, d4);
        for (var i = 0; i < 5; i++)
            Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Backward Straddling Checkerboard Cipher: Row [{0}] = [{1}]", i == 0 ? " " : rowDigits2[i - 1].ToString(), straddlingCheckerboard2.Substring(6 * i, 6).Join(" "));
        Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Backward Straddling Checkerboard result: {0}", encryptedDigits.Join(""));
        Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Forward Straddling Checkerboard Cipher: KW1: {0}, D1: {1}, D2: {2}", kw1, d1, d2);
        for (var i = 0; i < 5; i++)
            Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Forward Straddling Checkerboard Cipher: Row [{0}] = [{1}]", i == 0 ? " " : rowDigits1[i - 1].ToString(), straddlingCheckerboard1.Substring(6 * i, 6).Join(" "));
        Log(invert ? "INV CORNFLOWER" : "CORNFLOWER", "Forward Straddling Checkerboard result: {0}", encrypt);
        return encrypt;
    }
    private static string MakeStraddlingCheckerboard(bool keywordFirst, bool inColumns, string kw, int[] rowDigits)
    {
        var alphabet = (keywordFirst ? (kw + "ABCDEFGHIJKLMNOPQRSTUVWXYZ") : "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Except(kw).Concat(kw)).Distinct().Join("");
        for (var i = 0; i < 6; i++)
            if (rowDigits.Contains(i))
                alphabet = alphabet.Insert(inColumns ? 5 * i : i, ".");
        if (inColumns)
            alphabet = Enumerable.Range(0, 30).Select(i => alphabet[(i / 6) + 5 * (i % 6)]).Join("");
        return alphabet;
    }
    private int[] sequencing(string str)
    {
        return str.Select((ch, ix) => str.Count(c => c < ch) + str.Take(ix).Count(c => c == ch)).ToArray();
    }
    protected string pickWord(int length)
    {
        var wl = wordList[length - 4];
        var ix = Rnd.Range(0, wl.Count);
        var word = wl[ix];
        wl.RemoveAt(ix);
        return word;
    }
    protected string pickWord(int minLength, int maxLength)
    {
        return pickWord(Rnd.Range(minLength, maxLength + 1));
    }
    private string toBraille(string word)
    {
        return word.Select(ch => "⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵"[ch - 'A']).Join("");
    }
    #endregion

    #region Forest Cipher
    protected PageInfo[] forestcipher(string word, bool invert = false)
    {
        if (invert)
        {
            Log("INV FOREST", "Begin Chain Bit-Rotation Cipher");
            var chainBitRotationResult = chainBitRotationCipher(word, invert);
            Log("INV FOREST", "Begin Semaphore Rotation Cipher");
            var semaphoreRotationResult = semaphoreRotationCipher(chainBitRotationResult.Encrypted, invert);
            Log("INV FOREST", "Begin Monoalphabetic Rubik’s Cube Cipher");
            var rubiksResult = rubiksMonoalphabeticCubeCipher(semaphoreRotationResult.Encrypted, invert);
            return newArray(
                new PageInfo(new ScreenText(rubiksResult.Encrypted, 40), chainBitRotationResult.Number, chainBitRotationResult.Keyword),
                new PageInfo(semaphoreRotationResult.Keyword, rubiksResult.AlphaKeyword, rubiksResult.RotationsKeyword));
        }
        else
        {
            Log("FOREST", "Begin Monoalphabetic Rubik’s Cube Cipher");
            var rubiksResult = rubiksMonoalphabeticCubeCipher(word, invert);
            Log("FOREST", "Begin Semaphore Rotation Cipher");
            var semaphoreRotationResult = semaphoreRotationCipher(rubiksResult.Encrypted, invert);
            Log("FOREST", "Begin Chain Bit-Rotation Cipher");
            var chainBitRotationResult = chainBitRotationCipher(semaphoreRotationResult.Encrypted, invert);
            return newArray(
                new PageInfo(new ScreenText(chainBitRotationResult.Encrypted, 40), chainBitRotationResult.Number, chainBitRotationResult.Keyword),
                new PageInfo(semaphoreRotationResult.Keyword, rubiksResult.AlphaKeyword, rubiksResult.RotationsKeyword));
        }
    }
    private struct RubiksMonoalphabeticCubeResult { public string Encrypted; public ScreenText AlphaKeyword, RotationsKeyword; }
    private RubiksMonoalphabeticCubeResult rubiksMonoalphabeticCubeCipher(string word, bool invert)
    {
        var alphaKw = pickWord(4, 8);
        var rotationsKw = pickWord(4);
        var cube = generateRubiksMonoalphabeticCube(alphaKw, rotationsKw, invert);
        var encrypted = (invert ? word.Select(ch => (char) (Array.IndexOf(cube, ch) + 'A')) : word.Select(ch => cube[ch - 'A'])).Join("");
        Log(invert ? "INV FOREST" : "FOREST", "Monoalphabetic Rubik’s Cube Cipher: Encrypted = {0}", encrypted);
        return new RubiksMonoalphabeticCubeResult { Encrypted = encrypted, AlphaKeyword = new ScreenText(alphaKw, 35), RotationsKeyword = new ScreenText(rotationsKw, 40) };
    }
    private char[] generateRubiksMonoalphabeticCube(string alphaKw, string rotationsKw, bool invert)
    {
        var alphabetKey = getKey(alphaKw, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", kwFirst: true);
        var specialCubelets = new[] { 10, 4, 13, 21, 12, 15 };
        var cubelets = specialCubelets.Concat(Enumerable.Range(0, 26)).Distinct().ToArray();

        var cube = new char[26];
        for (var i = 0; i < 26; i++)
            cube[cubelets[i]] = alphabetKey[i];
        Log(invert ? "INV FOREST" : "FOREST", "Monoalphabetic Rubik’s Cube Cipher: Alphabet keyword = {0}; cube before rotations (front | middle | back):", alphaKw);
        for (var row = 0; row < 3; row++)
            Log(invert ? "INV FOREST" : "FOREST", Enumerable.Range(0, 3).Select(layer => Enumerable.Range(0, 3).Select(col => 9 * layer + 3 * row + col == 13 ? ' ' : cube[9 * layer + 3 * row + col - (9 * layer + 3 * row + col >= 13 ? 1 : 0)]).Join(" ")).Join(" | "));

        Log(invert ? "INV FOREST" : "FOREST", "Monoalphabetic Rubik’s Cube Cipher: Rotations keyword = {0}:", rotationsKw);
        // order: U F R B L D
        var rotations = "0 9 17 18 19 11 2 1,0 1 2 5 8 7 6 3,2 11 19 22 25 16 8 5,19 18 17 20 23 24 25 22,17 9 0 3 6 14 23 20,6 7 8 16 25 24 23 14"
            .Split(',').Select(str => str.Split(' ').Select(int.Parse).ToArray()).ToArray();

        var rots = rotationsKw.Select(ch => (ch - 'A') % 24).Select((rotIx, ix) => new { Char = rotationsKw[ix], Face = rotIx / 4, NumRot = 2 * (rotIx % 4) + 1 }).ToArray();
        foreach (var rot in rots)
        {
            var r = rotations[rot.Face];
            for (var n = 0; n < rot.NumRot; n++)
            {
                var f = cube[r.Last()];
                for (var i = r.Length - 1; i > 0; i--)
                    cube[r[i]] = cube[r[i - 1]];
                cube[r[0]] = f;
            }
            Log(invert ? "INV FOREST" : "FOREST", "Monoalphabetic Rubik’s Cube after rotation {0} ({1}×{2}) (front | middle | back):", rot.Char, rot.NumRot, "UFRBLD"[rot.Face]);
            for (var row = 0; row < 3; row++)
                Log(invert ? "INV FOREST" : "FOREST", Enumerable.Range(0, 3).Select(layer => Enumerable.Range(0, 3).Select(col => 9 * layer + 3 * row + col == 13 ? ' ' : cube[9 * layer + 3 * row + col - (9 * layer + 3 * row + col >= 13 ? 1 : 0)]).Join(" ")).Join(" | "));
        }
        return cube;
    }
    private struct SemaphoreRotationResult { public string Encrypted; public ScreenText Keyword; }
    private SemaphoreRotationResult semaphoreRotationCipher(string word, bool invert)
    {
        tryAgain:
        var kw = pickWord(6, 8);
        var encrypted = semaphoreRotationCipherAttempt(word, kw, invert);
        if (encrypted == null)
            goto tryAgain;
        Log(invert ? "INV FOREST" : "FOREST", "Semaphore Rotation Cipher: keyword = {0}", kw);
        Log(invert ? "INV FOREST" : "FOREST", "Semaphore Rotation Cipher: encrypted = {0}", encrypted);
        return new SemaphoreRotationResult { Encrypted = encrypted, Keyword = new ScreenText(kw, 35) };
    }
    private string semaphoreRotationCipherAttempt(string word, string kw, bool invert)
    {
        var encrypted = "";
        for (var i = 0; i < word.Length; i++)
        {
            var rotated = semaphores[word[i] - 'A'].Select(j => (invert ? ((j - (kw[i % kw.Length] - 'A' + 1)) % 8 + 8) % 8 : j + kw[i % kw.Length] - 'A' + 1) % 8).ToArray();
            Array.Sort(rotated);
            var letter = Array.FindIndex(semaphores, s => s.SequenceEqual(rotated));
            if (letter == -1)
                return null;
            encrypted += (char) ('A' + letter);
        }
        return encrypted;
    }
    private struct ChainBitRotationResult { public string Encrypted; public ScreenText Number, Keyword; }
    private ChainBitRotationResult chainBitRotationCipher(string word, bool invert)
    {
        var logMessages = new List<string>();

        tryAgain2:
        var kw = pickWord(4, 8);
        long number;
        string encrypted;

        if (invert)
        {
            long residue = 0;
            tryAgain1:
            encrypted = "";
            logMessages.Clear();
            logMessages.Add(string.Format("Chain Bit-Rotation Cipher: encrypting {0} with keyword {1}; start with {2}", word, kw, residue));
            number = word.Aggregate(residue, (p, n) => p * 26 + (n == 'Z' ? 0L : (n - 'A' + 1)));
            logMessages.Add(string.Format("Chain Bit-Rotation Cipher: binary: {0}", Convert.ToString(number, 2)));
            for (var i = 0; i < word.Length; i++)
            {
                // Bit rotation
                var nb = (word.Length - i) * 5;
                var amtRaw = kw[i % kw.Length] - 'A' + 1;
                var amt = amtRaw % nb;
                number = (((number >> amt) & ((1L << (nb - amt)) - 1)) | (number << (nb - amt))) & ((1L << nb) - 1) | (number & ~((1L << nb) - 1));
                var numberStr = Convert.ToString(number, 2).PadLeft(nb, '0');
                logMessages.Add(string.Format("Rotate {0} bits right by {1} (= {2}) = {3}]", nb, kw[i % kw.Length], amtRaw, numberStr.Insert(numberStr.Length - nb, "[")));
                // Extract a letter
                var extracted = (int) (number & 0x1f);
                if (extracted < 1 || extracted > 26)
                {
                    residue++;
                    if (residue > 16)
                        goto tryAgain2;
                    goto tryAgain1;
                }
                encrypted += (char) (extracted + 'A' - 1);
                number >>= 5;
                logMessages.Add(string.Format("Extracted letter: {0}; remaining bits = {1}", encrypted.Last(), Convert.ToString(number, 2)));
            }
        }
        else
        {
            encrypted = "";
            logMessages.Add(string.Format("Chain Bit-Rotation Cipher: encrypting {0} with keyword {1}; start with 0", word, kw));
            number = 0L;
            for (var i = word.Length - 1; i >= 0; i--)
            {
                // Process letter
                var letter = word[i];
                var addition = letter - 'A' + 1;
                number = (number << 5) + addition;
                logMessages.Add(string.Format("Shift in {0} ({1}) = {2}", addition, letter, Convert.ToString(number, 2)));

                // Bit rotation
                var nb = (word.Length - i) * 5;
                var amt = kw[i % kw.Length] - 'A' + 1;
                number = ((((number >> (nb - (amt % nb))) & ((1L << (amt % nb)) - 1)) | (number << (amt % nb))) & ((1L << nb) - 1)) | (number & ~((1L << nb) - 1));
                var numberStr = Convert.ToString(number, 2).PadLeft(nb, '0');
                logMessages.Add(string.Format("Rotating last {0} bits left {1} = {2}]", nb, amt, numberStr.Insert(numberStr.Length - nb, "[")));
            }
            for (var i = 0; i < word.Length; i++)
            {
                var num = number % 26;
                encrypted = (char) (num == 0 ? 'Z' : num + 'A' - 1) + encrypted;
                number /= 26;
            }
        }

        logMessages.Add(string.Format("Chain Bit-Rotation Cipher: encrypted: {0}; number: {1}", encrypted, number));
        foreach (var msg in logMessages)
            Log(invert ? "INV FOREST" : "FOREST", msg);

        return new ChainBitRotationResult { Encrypted = encrypted, Number = new ScreenText(number.ToString(), 45), Keyword = new ScreenText(kw, 35) };
    }
    #endregion

    #region Gray Cipher
    protected PageInfo[] graycipher(string word, bool invert = false)
    {
        if (invert)
        {
            Log("INV GRAY", "Begin Bit Switch Encryption");
            var bitSwitchResult = BitSwitchEnc(word.ToUpperInvariant(), invert);
            Log("INV GRAY", "Begin Columnar Transposition");
            var columnResult = ColumnTrans(bitSwitchResult.Encrypted.ToUpperInvariant(), invert);
            Log("INV GRAY", "Begin Portax Encryption");
            var portaxResult = PortaxEnc(columnResult.Encrypted.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(portaxResult.Encrypted, 40), bitSwitchResult.BinaryTrue, columnResult.Num),
                new PageInfo(portaxResult.Key));
        }
        else
        {
            Log("GRAY", "Begin Portax Encryption");
            var portaxResult = PortaxEnc(word.ToUpperInvariant(), invert);
            Log("GRAY", "Begin Columnar Transposition");
            var columnResult = ColumnTrans(portaxResult.Encrypted.ToUpperInvariant(), invert);
            Log("GRAY", "Begin Bit Switch Encryption");
            var bitSwitchResult = BitSwitchEnc(columnResult.Encrypted.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(bitSwitchResult.Encrypted, 40), bitSwitchResult.BinaryTrue, columnResult.Num),
                new PageInfo(portaxResult.Key));
        }
    }
    private struct PortaxResult { public string Encrypted; public ScreenText Key; }
    private PortaxResult PortaxEnc(string word, bool invert)
    {
        string key = string.Concat(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)],
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)],
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)]);
        if (invert)
            Log("INV GRAY", "Key: {0}", key);
        else
            Log("GRAY", "Key: {0}", key);
        char[] letterenc = new char[6];
        for (int aa = 0; aa < 3; aa++)
        {
            string[] slides = new string[]
            {
                "ABCDEFGHIJKLM",
                "NOPQRSTUVWXYZ",
                "ACEGIKMOQSUWY",
                "BDFHJLNPRTVXZ"
            };
            int num = slides[2].IndexOf(key[aa]);
            if (num < 0)
                num = slides[3].IndexOf(key[aa]);
            slides[1] = slides[1].Substring(num) + slides[1].Substring(0, num);
            slides[2] = slides[2].Substring(num) + slides[2].Substring(0, num);
            slides[3] = slides[3].Substring(num) + slides[3].Substring(0, num);

            Log(invert ? "INV GRAY" : "GRAY", "Slides:");
            for (var i = 0; i < 4; i++)
                Log(invert ? "INV GRAY" : "GRAY", slides[i]);

            int num1 = slides[0].IndexOf(word[aa]);
            int col1 = 0;
            if (num1 < 0)
            {
                num1 = slides[1].IndexOf(word[aa]);
                col1 = 1;
            }
            int num2 = slides[2].IndexOf(word[aa + 3]);
            int col2 = 2;
            if (num2 < 0)
            {
                num2 = slides[3].IndexOf(word[aa + 3]);
                col2 = 3;
            }
            if (num1 == num2)
            {
                letterenc[aa] = slides[(col1 + 1) % 2][num1];
                letterenc[aa + 3] = slides[(col2 - 1) % 2 + 2][num2];
            }
            else
            {
                letterenc[aa] = slides[col1][num2];
                letterenc[aa + 3] = slides[col2][num1];
            }
            Log(invert ? "INV GRAY" : "GRAY", "{0}{1} -> {2}{3}", word[aa], word[aa + 3], letterenc[aa], letterenc[aa + 3]);
        }
        return new PortaxResult { Key = new ScreenText(key.ToUpperInvariant(), 40), Encrypted = new string(letterenc) };
    }
    private struct BitSwitchResult { public string Encrypted; public ScreenText BinaryTrue; }
    private BitSwitchResult BitSwitchEnc(string word, bool invert)
    {
        string encrypted = "";
        string order = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string p7alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string[] scramblers = {
              "21453", "21534", "31524", "31452", "41523", "41532", "51234",
              "51423", "51432", "23154", "25134", "24153", "34152", "35124",
              "45123", "45132", "54123", "54132", "24513", "25413", "34512",
              "35214", "35412", "43512", "45213", "53412", "54213", "24531",
              "25431", "34521", "35421", "43251", "43521", "45231", "53421",
              "54231"
        };
        List<string> binary = new List<string>(){
            "00001", "00010", "00011", "00100", "00101", "00110", "00111",
            "01000", "01001", "01010", "01011", "01100", "01101", "01110",
            "01111", "10000", "10001", "10010", "10011", "10100", "10101",
            "10110", "10111", "11000", "11001", "11010"
        };
        string scrambler = scramblers[order.IndexOf(Bomb.GetSerialNumber()[0])];
        string binarytrue = "";
        Log(invert ? "INV GRAY" : "GRAY", "Scrambler: {0}", scrambler);
        for (int aa = 0; aa < 6; aa++)
        {
            string binarylet = binary[p7alphabet.IndexOf(word[aa])];
            string convbin = scrambling(binarylet, scrambler, invert);
            if (!binary.Contains(convbin))
            {
                convbin = convbin.Replace("1", "*");
                convbin = convbin.Replace("0", "1");
                convbin = convbin.Replace("*", "0");
                binarytrue += "1";
            }
            else
            {
                string temp = convbin.Replace("1", "*");
                temp = temp.Replace("0", "1");
                temp = temp.Replace("*", "0");
                if (binary.Contains(temp) && Rnd.Range(0, 3) == 0)
                {
                    convbin = temp.ToUpperInvariant();
                    binarytrue += "1";
                }
                else
                    binarytrue += "0";
            }
            encrypted += p7alphabet[binary.IndexOf(convbin)];
            Log(invert ? "INV GRAY" : "GRAY", "{0} -> {1} + {2} -> {3} -> {4}", word[aa], binarylet, binarytrue[aa], convbin, encrypted[aa]);
        }
        return new BitSwitchResult { Encrypted = encrypted, BinaryTrue = new ScreenText(binarytrue, 40) };
    }
    private string scrambling(string bin, string scrambler, bool invert)
    {
        char[] c = new char[5];
        if (invert)
        {
            for (int aa = 0; aa < 5; aa++)
                c[aa] = bin[scrambler[aa] - '1'];
        }
        else
        {
            for (int aa = 0; aa < 5; aa++)
                c[scrambler[aa] - '1'] = bin[aa];
        }
        return new string(c);
    }
    private struct ColumnTransResult { public string Encrypted; public ScreenText Num; }
    private ColumnTransResult ColumnTrans(string word, bool invert)
    {
        int numcols = Rnd.Range(0, 5) + 2;
        string temp = "123456".Substring(0, numcols);
        string nums = "";
        for (int aa = 0; aa < numcols; aa++)
        {
            int randnum = Rnd.Range(0, temp.Length);
            nums += temp[randnum];
            temp = temp.Substring(0, randnum) + temp.Substring(randnum + 1);
        }
        string[] columns = new string[numcols];
        string encrypted = "";
        if (invert)
        {
            for (int bb = 0; bb < 6; bb++)
                columns[bb % numcols] = columns[bb % numcols] + "*";
            int wordIx = 0;
            for (int cc = 0; cc < numcols; cc++)
            {
                string find = (cc + 1).ToString();
                int col = nums.IndexOf(find[0]);
                int length = columns[col].Length;
                columns[col] = "";
                for (int l = 0; l < length; l++)
                {
                    columns[col] += word[wordIx];
                    wordIx++;
                }
            }
            for (int m = 0; m < numcols; m++)
                Log("INV GRAY", "Column {0}: {1}", m + 1, columns[m]);
            for (int n = 0; n < 6; n++)
            {
                encrypted = encrypted + "" + columns[n % columns.Length][0];
                columns[n % columns.Length] = columns[n % columns.Length].Substring(1);
            }
            Log("INV GRAY", "{0} -> {1}", word, encrypted);
        }
        else
        {
            for (int bb = 0; bb < 6; bb++)
                columns[bb % numcols] = columns[bb % numcols] + "" + word[bb];
            for (int cc = 0; cc < numcols; cc++)
            {
                string find = (cc + 1).ToString();
                encrypted += columns[nums.IndexOf(find[0])];
                Log("GRAY", "Column {0}: {1}", cc + 1, columns[nums.IndexOf(find[0])]);
            }
            Log("GRAY", "{0} -> {1}", word, encrypted);
        }
        return new ColumnTransResult { Encrypted = encrypted, Num = new ScreenText(nums.ToUpperInvariant(), 40) };
    }
    #endregion

    #region Green Cipher
    protected PageInfo[] greencipher(string word, bool invert = false)
    {
        if (invert)
        {
            Log("INV GREEN", "Begin Ragbaby Encryption");
            var ragbabyResult = RagbabyEnc(word.ToUpperInvariant(), invert);
            Log("INV GREEN", "Begin Mechanical Encryption");
            string text2 = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
            wordList[2].Remove(text2.ToUpperInvariant());
            var encrypted = MechanicalEnc(ragbabyResult.Encrypted.ToUpperInvariant(), text2.ToUpperInvariant(), invert);
            Log("INV GREEN", "Begin Homophonic Encryption");
            var homophonicResult = HomophonicEnc(text2, invert);
            return newArray(
                new PageInfo(new ScreenText(encrypted, 40), homophonicResult.Tens, homophonicResult.Ones),
                new PageInfo(homophonicResult.Keyword, ragbabyResult.Keyword));
        }
        else
        {
            Log("GREEN", "Begin Mechanical Encryption");
            string kw = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
            wordList[2].Remove(kw.ToUpperInvariant());
            string encrypted = MechanicalEnc(word.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("GREEN", "Begin Ragbaby Encryption");
            var ragbabyResult = RagbabyEnc(encrypted.ToUpperInvariant(), invert);
            Log("GREEN", "Begin Homophonic Encryption");
            var homophonicResult = HomophonicEnc(kw, invert);
            return newArray(
                new PageInfo(new ScreenText(ragbabyResult.Encrypted, 40), homophonicResult.Tens, homophonicResult.Ones),
                new PageInfo(homophonicResult.Keyword, ragbabyResult.Keyword));
        }
    }
    private struct HomophonicResult { public ScreenText Keyword, Tens, Ones; }
    private HomophonicResult HomophonicEnc(string word, bool invert)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string kw = string.Concat(alpha[Rnd.Range(0, 26)], alpha[Rnd.Range(0, 26)], alpha[Rnd.Range(0, 26)]);
        int[][] nums = new int[3][];
        for (int aa = 0; aa < nums.Length; aa++)
        {
            nums[aa] = new int[26];
            for (int bb = 0; bb < 26; bb++)
                nums[aa][bb] = bb + 1 + 26 * aa;
        }
        for (int cc = 0; cc < 3; cc++)
        {
            int rotations = alpha.IndexOf(kw[cc]);
            for (int dd = 0; dd < rotations; dd++)
                nums[cc] = HomophonicRot(nums[cc]);
            Log(invert ? "INV GREEN" : "GREEN", "Row {0}: {1}", cc + 1, nums[cc][0]);
        }
        string rows = "001122";
        string tens = "";
        string ones = "";
        for (int dd = 0; dd < 6; dd++)
        {
            int cur = Rnd.Range(0, rows.Length);
            string num = nums[rows[cur] - '0'][alpha.IndexOf(word[dd])].ToString();
            if (num.Length == 1)
                num = "0" + num[0];
            Log(invert ? "INV GREEN" : "GREEN", "{0} -> {1}", word[dd], num);
            tens += num[0];
            ones += num[1];
            rows = rows.Substring(0, cur) + rows.Substring(cur + 1);
        }
        return new HomophonicResult
        {
            Tens = new ScreenText(tens, 40),
            Ones = new ScreenText(ones, 40),
            Keyword = new ScreenText(kw.ToUpperInvariant(), 40)
        };
    }
    private int[] HomophonicRot(int[] n)
    {
        int[] c = new int[26];
        for (int aa = 1; aa < 26; aa++)
            c[aa] = n[aa - 1];
        c[0] = n[25];
        return c;
    }
    private struct RagbabyResult { public string Encrypted; public ScreenText Keyword; }
    private RagbabyResult RagbabyEnc(string word, bool invert)
    {
        int length = Rnd.Range(0, wordList.Count);
        string kw = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw);
        string key = getKey(kw.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetOffIndicators().Count<string>() % 2 == 0);
        Log(invert ? "INV GREEN" : "GREEN", "Generated Key: {0}", key);
        string encrypted = "";
        for (int aa = 0; aa < 6; aa++)
        {
            if (invert)
            {
                encrypted += key[((key.IndexOf(word[aa]) - (aa + 1)) % 26 + 26) % 26];
                Log("INV GREEN", "{0} -> {1}", word[aa], encrypted[aa]);
            }
            else
            {
                encrypted += key[(key.IndexOf(word[aa]) + aa + 1) % 26];
                Log("GREEN", "{0} -> {1}", word[aa], encrypted[aa]);
            }
        }
        return new RagbabyResult { Encrypted = encrypted, Keyword = new ScreenText(kw.ToUpperInvariant(), 35) };
    }
    private string MechanicalEnc(string word, string kw, bool invert)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string[] table = new string[]
        {
            "UFHKQIPLXNZESGBVMCWJRDOTYA",
            "IWCZYMLKJODGFSQRNBTXHUEVAP",
            "WBSMEJTUCPFAHZOQLIKNYVGXRD",
            "GRINQVWOTYAJXBMHCFKLDUSZEP",
            "DLTVSUIKWCXRFJZANYHMQOGEPB",
            "FSVCEIUJKPGNTYHBLRQOXMADWZ",
            "JOCYWFPADKHIUVTSMENGQLZBRX",
            "BPHORAKNUETDZYQIMSFJGVWCXL",
            "ANDSQWTGXKFPCOVBLMYEZHRJIU",
            "AQJPBUSGWNXZVDYLETCOFHRIMK",
            "BHFTDGERXJAMUNZVYKOSPILCWQ",
            "JHUKDMSNEBICZYWLXQFPORTAVG",
            "ASNTZDBGWYILEORCQFXJPKHMVU",
            "RPCQABVLGWFENIKYMDUTSJXOZH",
            "YIXNVWQSUHFOMZDGKJPCTBELAR",
            "IMPCZLEGJARNTWSYFQDOUBKHVX",
            "JGKOXMUBAVRTFYCNPWQZESILHD",
            "SVHDBZNMKWJIEUYFXRQPLGCATO",
            "TZXGOPNBWAIYRHQLVKJSCDUEFM",
            "DJQZYWTPKIXCVABFNUEOLHSGRM",
            "CJOEDYHBNIXZRTPWGALFKUSMVQ",
            "FEHLYOBGRXQKVZUIMJTNACDPSW",
            "MOGAPTHIZXRFKLYSVDBWUQNECJ",
            "RXMSBPWOEJADIYNQLGKCTUHZFV",
            "ZJVWFBEOTKRDHSCPIGQNAYLUXM",
            "VWFXUEKRLBQTMCHSGJOZYDAPIN"
        };
        string encrypted = "";
        if (invert)
        {
            for (int aa = 0; aa < 6; aa++)
            {
                int row = alpha.IndexOf(kw[aa]);
                int col = table[row].IndexOf(word[aa]);
                encrypted += alpha[col];
                Log("INV GREEN", "{0} + {1} -> {2}", kw[aa], word[aa], encrypted[aa]);
            }
        }
        else
        {
            for (int aa = 0; aa < 6; aa++)
            {
                int row = alpha.IndexOf(kw[aa]);
                int col = alpha.IndexOf(word[aa]);
                encrypted += table[row][col];
                Log("GREEN", "{0} + {1} -> {2}", kw[aa], word[aa], encrypted[aa]);
            }
        }
        return encrypted;
    }
    #endregion

    #region Indigo Cipher
    protected PageInfo[] indigocipher(string word, bool invert = false)
    {
        int rand = Rnd.Range(0, wordList.Count);
        string kw = wordList[rand][Rnd.Range(0, wordList[rand].Count)].ToUpperInvariant();
        wordList[rand].Remove(kw.ToUpperInvariant());
        string key = getKey(kw.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetPortCount() % 2 == 1);
        if (invert)
        {
            Log("INV INDIGO", "Begin Condi Encryption");
            string condiResult = CondiEnc(word.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            Log("INV INDIGO", "Begin Logic Encryption");
            var logicEncResult = LogicEnc(condiResult.ToUpperInvariant(), invert);
            Log("INV INDIGO", "Begin Fractionated Morse Encryption");
            string fractionatedMorseResult = FractionatedMorseEnc(logicEncResult.Keyword.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(logicEncResult.Encrypted, 40), new ScreenText(fractionatedMorseResult, 30), new ScreenText(kw, 35)),
                new PageInfo(logicEncResult.OverEnc, logicEncResult.OverKw, logicEncResult.Equation));
        }
        else
        {
            Log("INDIGO", "Begin Logic Encryption");
            var logicEncResult = LogicEnc(word.ToUpperInvariant(), invert);
            Log("INDIGO", "Begin Condi Encryption");
            var condiResult = CondiEnc(logicEncResult.Encrypted, key.ToUpperInvariant(), invert);
            Log("INDIGO", "Begin Fractionated Morse Encryption");
            string fractionatedMorseResult = FractionatedMorseEnc(logicEncResult.Keyword.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(condiResult, 40), new ScreenText(fractionatedMorseResult, 30), new ScreenText(kw, 35)),
                new PageInfo(logicEncResult.OverEnc, logicEncResult.OverKw, logicEncResult.Equation));
        }
    }
    private struct LogicEncResult { public string Encrypted, Keyword; public ScreenText OverEnc, OverKw, Equation; }
    private LogicEncResult LogicEnc(string word, bool invert)
    {
        int binary1s = Rnd.Range(0, 3) + 2;
        string positions = "012345";
        string binary1pos = "";
        string binary0pos = "";
        for (int bb = 0; bb < binary1s; bb++)
        {
            int randnum = Rnd.Range(0, positions.Length);
            binary1pos += positions[randnum];
            positions = positions.Substring(0, randnum) + positions.Substring(randnum + 1);
        }
        string b1 = "";
        positions = "012345";
        for (int cc = 0; cc < 6; cc++)
        {
            if (binary1pos.IndexOf(positions[cc]) >= 0)
                b1 += "1";
            else
            {
                b1 += "0";
                binary0pos += cc;
            }
        }

        binary1s = Rnd.Range(0, 3);

        char[] b2 = new char[6];
        char randPos = binary1pos[Rnd.Range(0, binary1pos.Length)];
        b2[randPos - '0'] = '1';
        binary1pos = binary1pos.Substring(0, binary1pos.IndexOf(randPos)) + "" + binary1pos.Substring(binary1pos.IndexOf(randPos) + 1);
        positions = positions.Substring(0, positions.IndexOf(randPos)) + "" + positions.Substring(positions.IndexOf(randPos) + 1);

        randPos = binary1pos[Rnd.Range(0, binary1pos.Length)];
        b2[randPos - '0'] = '0';
        positions = positions.Substring(0, positions.IndexOf(randPos)) + "" + positions.Substring(positions.IndexOf(randPos) + 1);

        randPos = binary0pos[Rnd.Range(0, binary0pos.Length)];
        b2[randPos - '0'] = '1';
        binary0pos = binary0pos.Substring(0, binary0pos.IndexOf(randPos)) + "" + binary0pos.Substring(binary0pos.IndexOf(randPos) + 1);
        positions = positions.Substring(0, positions.IndexOf(randPos)) + "" + positions.Substring(positions.IndexOf(randPos) + 1);

        randPos = binary0pos[Rnd.Range(0, binary0pos.Length)];
        b2[randPos - '0'] = '0';
        positions = positions.Substring(0, positions.IndexOf(randPos)) + "" + positions.Substring(positions.IndexOf(randPos) + 1);

        while (binary1s > 0)
        {
            binary1s--;
            randPos = positions[Rnd.Range(0, positions.Length)];
            b2[randPos - '0'] = '1';
            positions = positions.Substring(0, positions.IndexOf(randPos)) + "" + positions.Substring(positions.IndexOf(randPos) + 1);
        }
        for (int ee = 0; ee < positions.Length; ee++)
            b2[positions[ee] - '0'] = '0';
        string b3 = "";
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        List<string> binalpha = new List<string>
        {
            "00000",
            "00001",
            "00010",
            "00011",
            "00100",
            "00101",
            "00110",
            "00111",
            "01000",
            "01001",
            "01010",
            "01011",
            "01100",
            "01101",
            "01110",
            "01111",
            "10000",
            "10001",
            "10010",
            "10011",
            "10100",
            "10101",
            "10110",
            "10111",
            "11000",
            "11001"
        };
        int logicgate = Rnd.Range(0, 8);
        string[] gates = { "AND", "OR", "XOR", "NAND", "NOR", "XNOR", "->", "<-" };
        Log(invert ? "INV INDIGO" : "INDIGO", "Gate: {0}", gates[logicgate]);
        string kw = "";
        string encrypted = "";
        string overenc = "";
        string overkw = "";
        for (int dd = 0; dd < 6; dd++)
        {
            string binlet = binalpha[alpha.IndexOf(word[dd])];
            string binenc = "";
            string binkw = "";
            for (int ee = 0; ee < 5; ee++)
            {
                switch (logicgate)
                {
                    case 0:
                        if (binlet[ee] == '1')
                        {
                            binenc += "1";
                            binkw += "1";
                        }
                        else
                        {
                            string[] op1 = { "01", "10", "00" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        break;
                    case 1:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "01", "10", "11" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            binenc += "0";
                            binkw += "0";
                        }
                        break;
                    case 2:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "01", "10" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            string[] op1 = { "00", "11" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        break;
                    case 3:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "01", "10", "00" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            binenc += "1";
                            binkw += "1";
                        }
                        break;
                    case 4:
                        if (binlet[ee] == '1')
                        {
                            binenc += "0";
                            binkw += "0";
                        }
                        else
                        {
                            string[] op1 = { "01", "10", "11" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        break;
                    case 5:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "11", "00" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            string[] op1 = { "10", "01" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        break;
                    case 6:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "11", "00", "01" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            binenc += "1";
                            binkw += "0";
                        }
                        break;
                    default:
                        if (binlet[ee] == '1')
                        {
                            string[] op1 = { "11", "00", "10" };
                            string rand = op1[Rnd.Range(0, op1.Length)];
                            binenc += rand[0];
                            binkw += rand[1];
                        }
                        else
                        {
                            binenc += "0";
                            binkw += "1";
                        }
                        break;
                }
            }

            Log(invert ? "INV INDIGO" : "INDIGO", "{0} + {1} = {2}", binenc, binkw, binlet);

            if (!binalpha.Contains(binenc))
            {
                overenc += "1";
                binenc = binenc.Replace("1", "*");
                binenc = binenc.Replace("0", "1");
                binenc = binenc.Replace("*", "0");
            }
            else
            {
                string tempenc = binenc.Replace("1", "*");
                tempenc = tempenc.Replace("0", "1");
                tempenc = tempenc.Replace("*", "0");
                if (binalpha.Contains(tempenc) && Rnd.Range(0, 3) == 0)
                {
                    overenc += "1";
                    binenc = tempenc.ToUpperInvariant();
                }
                else
                {
                    overenc += "0";
                }
            }
            if (!binalpha.Contains(binkw))
            {
                overkw += "1";
                binkw = binkw.Replace("1", "*");
                binkw = binkw.Replace("0", "1");
                binkw = binkw.Replace("*", "0");
            }
            else
            {
                string tempkw = binkw.Replace("1", "*");
                tempkw = tempkw.Replace("0", "1");
                tempkw = tempkw.Replace("*", "0");
                if (binalpha.Contains(tempkw) && Rnd.Range(0, 3) == 0)
                {
                    overkw += "1";
                    binkw = tempkw.ToUpperInvariant();
                }
                else
                {
                    overkw += "0";
                }
            }
            encrypted += alpha[binalpha.IndexOf(binenc)];
            kw += alpha[binalpha.IndexOf(binkw)];
            switch (logicgate)
            {
                case 0: // AND
                    b3 += (b1[dd] == '1' && b2[dd] == '1') ? "1" : "0";
                    break;
                case 1: // OR
                    b3 += (b1[dd] == '1' || b2[dd] == '1') ? "1" : "0";
                    break;
                case 2: // XOR
                    b3 += (b1[dd] != b2[dd]) ? "1" : "0";
                    break;
                case 3: // NAND
                    b3 += (b1[dd] == '1' && b2[dd] == '1') ? "0" : "1";
                    break;
                case 4: // NOR
                    b3 += (b1[dd] == '1' || b2[dd] == '1') ? "0" : "1";
                    break;
                case 5: // XNOR
                    b3 += (b1[dd] != b2[dd]) ? "0" : "1";
                    break;
                case 6: // ->
                    b3 += (b1[dd] == '1' && b2[dd] == '0') ? "0" : "1";
                    break;
                default:    // <-
                    b3 += (b1[dd] == '0' && b2[dd] == '1') ? "0" : "1";
                    break;
            }
        }

        Log(invert ? "INV INDIGO" : "INDIGO", "Logic Encryption Keyword: {0}", kw);
        Log(invert ? "INV INDIGO" : "INDIGO", "Logic Encrypted Word: {0}", encrypted);
        Log(invert ? "INV INDIGO" : "INDIGO", "Binary 1: {0}", b1);
        Log(invert ? "INV INDIGO" : "INDIGO", "Binary 2: {0}", new string(b2));
        Log(invert ? "INV INDIGO" : "INDIGO", "Binary 3: {0}", b3);

        int num1 = 0;
        int num2 = 0;
        int num3 = 0;
        for (int ff = 0; ff < 6; ff++)
        {
            int multi = 1;
            for (int gg = 0; gg < 5 - ff; gg++)
                multi *= 2;
            if (b1[ff] == '1')
                num1 += multi;
            if (b2[ff] == '1')
                num2 += multi;
            if (b3[ff] == '1')
                num3 += multi;
        }
        return new LogicEncResult
        {
            Encrypted = encrypted,
            Keyword = kw,
            OverEnc = new ScreenText(overenc, 40),
            OverKw = new ScreenText(overkw, 40),
            Equation = new ScreenText(string.Format("{0} ? {1} = {2}", num1, num2, num3), 35)
        };
    }
    private string CondiEnc(string word, string key, bool invert)
    {
        int offset = Bomb.GetSerialNumberNumbers().Sum();
        string encrypted = "";
        if (invert)
        {
            Log("INV INDIGO", "Key: {0}", key);
            for (int aa = 0; aa < 6; aa++)
            {
                Log("INV INDIGO", "Offset: {0}", offset);
                encrypted += key[((key.IndexOf(word[aa]) - offset) % 26 + 26) % 26];
                Log("INV INDIGO", "{0} -> {1}", word[aa], encrypted[aa]);
                offset = key.IndexOf(encrypted[aa]) + 1;
            }
        }
        else
        {
            Log("INDIGO", "Key: {0}", key);
            for (int aa = 0; aa < 6; aa++)
            {
                Log("INDIGO", "Offset: {0}", offset);
                encrypted = encrypted + "" + key[(key.IndexOf(word[aa]) + offset) % 26];
                Log("INDIGO", "{0} -> {1}", word[aa], encrypted[aa]);
                offset = key.IndexOf(word[aa]) + 1;
            }
        }
        return encrypted;
    }
    private string FractionatedMorseEnc(string word, string key, bool invert)
    {
        Log(invert ? "INV INDIGO" : "INDIGO", "Key: {0}", key);
        string[] morseKey = { "...", "..-", "..x", ".-.", ".--", ".-x", ".x.", ".x-", ".xx", "-..", "-.-", "-.x", "--.", "---", "--x", "-x.", "-x-", "-xx", "x..", "x.-", "x.x", "x-.", "x--", "x-x", "xx.", "xx-" };
        int counter = 0;
        string[] rows = { "", "", "" };
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int aa = 0; aa < word.Length; aa++)
        {
            int numalpha = alpha.IndexOf(word[aa]);
            string morword = morse[numalpha];
            Log(invert ? "INV INDIGO" : "INDIGO", "{0} -> {1}", word[aa], morword);
            for (int bb = 0; bb < morword.Length; bb++)
            {
                rows[counter] += morword[bb];
                counter = (counter + 1) % 3;
            }
            if (aa != 5)
            {
                rows[counter] += "x";
                counter = (counter + 1) % 3;
            }
        }

        if (rows[1].Length != rows[0].Length)
            rows[1] = rows[1] + "x";
        if (rows[2].Length != rows[0].Length)
            rows[2] = rows[2] + "x";

        Log(invert ? "INV INDIGO" : "INDIGO", "Morse rows:");
        Log(invert ? "INV INDIGO" : "INDIGO", rows[0]);
        Log(invert ? "INV INDIGO" : "INDIGO", rows[1]);
        Log(invert ? "INV INDIGO" : "INDIGO", rows[2]);

        string encrypted = "";
        for (int bb = 0; bb < rows[0].Length; bb++)
        {
            string morenc = string.Concat(rows[0][bb], rows[1][bb], rows[2][bb]);
            for (int l = 0; l < morseKey.Length; l++)
            {
                if (morenc.Equals(morseKey[l]))
                {
                    Log(invert ? "INV INDIGO" : "INDIGO", "{0} -> {1}", morenc, key[l]);
                    encrypted += key[l];
                    break;
                }
            }
        }
        return encrypted;
    }
    #endregion

    #region Maroon Cipher
    protected PageInfo[] marooncipher(string word, bool invert = false)
    {
        int length = Rnd.Range(0, 5);
        string keyword = wordList[length][Rnd.Range(0, wordList[length].Count)];
        string key = getKey(keyword.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetPortCount() % 4 < 2);
        if (invert)
        {
            Log("INV MAROON", "Generated Keyword: {0}", keyword);
            Log("INV MAROON", "Generated Key: {0}", key);
            Log("INV MAROON", "Begin Monoalphabetic Encryption");
            var encrypted = MonoalphabeticEnc(word.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            Log("INV MAROON", "Begin Redfence Transposition");
            var redfenceResult = RedfenceTrans(encrypted.ToUpperInvariant(), invert);
            Log("INV MAROON", "Begin Huffman Encryption");
            var huffmanResult = HuffmanEnc(redfenceResult.Encrypted.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(huffmanResult.Encrypted, 40), new ScreenText(keyword, 35), redfenceResult.Key),
                huffmanResult.Page);
        }
        else
        {
            Log("MAROON", "Generated Keyword: {0}", keyword);
            Log("MAROON", "Generated Key: {0}", key);
            Log("MAROON", "Begin Huffman Encryption");
            var huffmanResult = HuffmanEnc(word.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            Log("MAROON", "Begin Redfence Transposition");
            var redfenceResult = RedfenceTrans(huffmanResult.Encrypted.ToUpperInvariant(), invert);
            Log("MAROON", "Begin Monoalphabetic Encryption");
            var encrypted = MonoalphabeticEnc(redfenceResult.Encrypted.ToUpperInvariant(), key.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(encrypted, 40), new ScreenText(keyword, 35), redfenceResult.Key),
                huffmanResult.Page);
        }
    }
    private struct HuffmanResult { public string Encrypted; public PageInfo Page; }
    private HuffmanResult HuffmanEnc(string word, string key, bool invert)
    {
        ArrayList paths = HuffmanGen();
        //Creating Path Needed to Make Diagram
        int depth = 0;
        string binary = "";
        for (int aa = 0; aa < paths.Count; aa++)
        {
            string path = (string) paths[aa];
            for (int bb = depth + 0; bb < path.Length; bb++)
                binary += "1";
            binary += "0";
            depth = path.LastIndexOf("L") + 1;
        }

        string result = "";
        string encrypted;
        //Encrypting the word
        if (invert)
        {
            for (int aa = 0; aa < paths.Count; aa++)
                Log("INV MAROON", "{0}: {1}", paths[aa], key[aa]);
            Log("INV MAROON", "Generated Binary: {0}", binary);
            while (binary.Length > 4)
            {
                string temp = binaryToLet(binary.Substring(0, 4));
                if (temp.Length != 1)
                {
                    temp = binaryToLet(binary.Substring(0, 5));
                    binary = binary.Substring(5);
                }
                else
                    binary = binary.Substring(4);
                result += temp;
            }
            if (binary.Length > 0)
            {
                if (binary.Length == 4)
                {
                    string temp = binaryToLet(binary);
                    if (temp.Length != 1)
                        temp = binaryToLet(binary.Substring(0, 2)) + binaryToLet(binary.Substring(2));
                    result += temp;
                }
                else if (binary.Length == 3)
                {
                    if (Rnd.Range(0, 2) == 0)
                        result += binaryToLet(binary.Substring(0, 2)) + binaryToLet(binary.Substring(2));
                    else
                        result += binaryToLet(binary.Substring(0, 1)) + binaryToLet(binary.Substring(1));
                }
                else
                    result += binaryToLet(binary);
            }
            encrypted = result.Substring(0, 6);
            result = result.Substring(6);
            binary = "";
            for (int aa = 0; aa < word.Length; aa++)
                binary += huffmanBinary[word[aa] - 'A'];
            binary = binary.Replace("0", "L").Replace("1", "R");
            bool flag = true;
            string result2 = "";
            while (flag)
            {
                flag = false;
                for (int aa = 0; aa < paths.Count; aa++)
                {
                    string temp = (string) paths[aa];
                    if (binary.StartsWith(temp))
                    {
                        result2 += key[aa];
                        binary = binary.Substring(temp.Length);
                        flag = true;
                        break;
                    }
                }
            }
            binary = binary.Replace("L", "0").Replace("R", "1");
            while (binary.Length > 1)
            {
                result2 += binaryToLet(binary.Substring(0, 2));
                binary = binary.Substring(2);
            }
            if (binary.Length > 0)
                result2 += binaryToLet(binary);
            Log("MAROON", "Encrypted Word: {0}", encrypted);
            Log("MAROON", "Page 2 Top Screen: {0}", result);
            Log("MAROON", "Page 2 Middle and Bottom Screen: {0}", result2);
            return new HuffmanResult
            {
                Encrypted = encrypted,
                Page = new PageInfo(new ScreenText(result, 35), new ScreenText(result2.Substring(0, result2.Length / 2), 35), new ScreenText(result2.Substring(result2.Length / 2), 35))
            };
        }
        else
        {
            for (int aa = 0; aa < paths.Count; aa++)
                Log("MAROON", "{0}: {1}", paths[aa], key[aa]);
            for (int aa = 0; aa < word.Length; aa++)
                binary += (string) paths[key.IndexOf(word[aa])];
            binary = binary.Replace("L", "0").Replace("R", "1");
            Log("MAROON", "Generated Binary: {0}", binary);
            while (binary.Length > 4)
            {
                string temp = binaryToLet(binary.Substring(0, 4));
                if (temp.Length != 1)
                {
                    temp = binaryToLet(binary.Substring(0, 5));
                    binary = binary.Substring(5);
                }
                else
                    binary = binary.Substring(4);
                result += temp;
            }
            if (binary.Length > 0)
            {
                if (binary.Length == 4)
                {
                    string temp = binaryToLet(binary);
                    if (temp.Length != 1)
                        temp = binaryToLet(binary.Substring(0, 2)) + binaryToLet(binary.Substring(2));
                    result += temp;
                }
                else if (binary.Length == 3)
                {
                    if (Rnd.Range(0, 2) == 0)
                        result += binaryToLet(binary.Substring(0, 2)) + binaryToLet(binary.Substring(2));
                    else
                        result += binaryToLet(binary.Substring(0, 1)) + binaryToLet(binary.Substring(1));
                }
                else
                    result += binaryToLet(binary);
            }
            encrypted = result.Substring(0, 6);
            result = result.Substring(6);
            Log("MAROON", "Encrypted Word: {0}", encrypted);
            Log("MAROON", "Resulting Characters: {0}", result);
            var div = Enumerable.Range(0, 3).Select(i => (int) Math.Round(result.Length * i / 3d)).ToArray();
            return new HuffmanResult
            {
                Encrypted = encrypted,
                Page = new PageInfo(
                    new ScreenText(result.Substring(0, div[1]), 35),
                    new ScreenText(result.Substring(div[1], div[2] - div[1]), 35),
                    new ScreenText(result.Substring(div[2]), 35))
            };
        }
    }
    string binaryToLet(string bin)
    {
        switch (bin)
        {
            case "00000":
                return "A";
            case "00001":
                return "B";
            case "00010":
                return "C";
            case "00011":
                return "D";
            case "00100":
                return "E";
            case "00101":
                return "F";
            case "00110":
                return "G";
            case "00111":
                return "H";
            case "01000":
                return "I";
            case "01001":
                return "J";
            case "01010":
                return "K";
            case "01011":
                return "L";
            case "01100":
                return "M";
            case "01101":
                return "N";
            case "01110":
                return "O";
            case "01111":
                return "P";
            case "10000":
                return "Q";
            case "10001":
                return "R";
            case "10010":
                return "S";
            case "10011":
                return "T";
            case "1010":
                return "U";
            case "1011":
                return "V";
            case "1100":
                return "W";
            case "1101":
                return "X";
            case "1110":
                return "Y";
            case "1111":
                return "Z";
            case "00":
                return "2";
            case "01":
                return "3";
            case "10":
                return "4";
            case "11":
                return "5";
            case "0":
                return "6";
            case "1":
                return "7";
        }
        return "";
    }
    ArrayList HuffmanGen()
    {
        ArrayList numbers = new ArrayList();
        numbers.Add(Rnd.Range(0, 7) + 10);
        //numbers.Add(10);
        numbers.Add(26 - (int) numbers[0]);
        ArrayList paths = new ArrayList() { "L", "R" };
        bool allOne = false;
        while (!allOne)
        {
            ArrayList pathList = new ArrayList();
            ArrayList numberList = new ArrayList();
            for (int aa = 0; aa < numbers.Count; aa++)
            {
                if ((int) numbers[aa] > 1)
                {
                    numberList.Add(Rnd.Range(0, (int) numbers[aa] - 1) + 1);
                    //numberList.Add((int)numbers[aa] - 1);
                    numberList.Add((int) numbers[aa] - (int) numberList[numberList.Count - 1]);
                    pathList.Add(paths[aa] + "L");
                    pathList.Add(paths[aa] + "R");
                }
                else
                {
                    pathList.Add(paths[aa]);
                    numberList.Add(numbers[aa]);
                }
            }
            numbers = numberList;
            paths = pathList;
            allOne = true;
            for (int aa = 0; aa < numbers.Count; aa++)
            {
                if ((int) numbers[aa] != 1)
                {
                    allOne = false;
                    break;
                }
            }
        }
        return paths;
    }
    private struct RedfenceResult { public string Encrypted; public ScreenText Key; }
    private RedfenceResult RedfenceTrans(string word, bool invert)
    {
        string[] rows = new string[Rnd.Range(0, 4) + 2];
        string poss = "";
        for (int aa = 0; aa < rows.Length; aa++)
        {
            rows[aa] = "";
            poss += aa + 1;
        }
        int offset = 1;
        int row = 0;
        string encrypted = "";
        if (invert)
        {
            for (int aa = 0; aa < 6; aa++)
            {
                rows[row] += "*";
                if ((row == 0 && offset == -1) || row == rows.Length - 1)
                    offset *= -1;
                row += offset;
            }
            string key = "", order = "12345";
            int cur = 0;
            for (int aa = 0; aa < rows.Length; aa++)
            {
                key += poss[Rnd.Range(0, poss.Length)];
                poss = poss.Replace(key[aa] + "", "");
                rows[order.IndexOf(key[aa])] = word.Substring(cur, rows[order.IndexOf(key[aa])].Length);
                cur += rows[order.IndexOf(key[aa])].Length;
            }
            for (int aa = 0; aa < rows.Length; aa++)
                Log("INV MAROON", "Row #{0}: {1}", aa + 1, rows[aa]);
            row = 0;
            offset = 1;
            for (int aa = 0; aa < 6; aa++)
            {
                encrypted += rows[row][0];
                rows[row] = rows[row].Substring(1);
                if ((row == 0 && offset == -1) || row == rows.Length - 1)
                    offset *= -1;
                row += offset;
            }
            Log("INV MAROON", "Redfence Key: {0}", key);
            Log("INV MAROON", "{0} -> {1}", word, encrypted);
            return new RedfenceResult { Encrypted = encrypted, Key = new ScreenText(key.ToUpperInvariant(), 40) };
        }
        else
        {
            for (int aa = 0; aa < 6; aa++)
            {
                rows[row] = rows[row] + "" + word[aa];
                if ((row == 0 && offset == -1) || row == rows.Length - 1)
                    offset *= -1;
                row += offset;
            }
            string key = "", order = "12345";
            for (int aa = 0; aa < rows.Length; aa++)
            {
                key += poss[Rnd.Range(0, poss.Length)];
                poss = poss.Replace(key[aa] + "", "");
                Log("MAROON", "Row #{0}: {1}", (aa + 1), rows[aa]);
                encrypted += rows[order.IndexOf(key[aa])].ToUpperInvariant();
            }
            Log("MAROON", "Redfence Key: {0}", key);
            Log("MAROON", "{0} -> {1}", word, encrypted);
            return new RedfenceResult { Encrypted = encrypted, Key = new ScreenText(key.ToUpperInvariant(), 40) };
        }
    }
    string MonoalphabeticEnc(string word, string key, bool invert)
    {
        string encrypt = "", alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (invert)
        {
            for (int aa = 0; aa < 6; aa++)
            {
                encrypt += alpha[key.IndexOf(word[aa])];
                Log("INV MAROON", "{0} -> {1}", word[aa], encrypt[aa]);
            }
        }
        else
        {
            for (int aa = 0; aa < 6; aa++)
            {
                encrypt += key[alpha.IndexOf(word[aa])];
                Log("MAROON", "{0} -> {1}", word[aa], encrypt[aa]);
            }
        }
        return encrypt;
    }
    #endregion

    #region Orange Cipher
    protected PageInfo[] orangecipher(string word, bool invert = false)
    {
        string encrypted = "";
        bool[] b = new bool[6];
        for (int aa = 0; aa < 6; aa++)
        {
            if (word[aa] == 'J')
            {
                encrypted += "ABCDEFGHIKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 25)];
                b[aa] = true;
            }
            else
                encrypted += word[aa];
        }

        int length = Rnd.Range(0, wordList.Count);
        string kw1 = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw1.ToUpperInvariant());

        length = Rnd.Range(0, wordList.Count);
        string kw2 = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw2.ToUpperInvariant());

        string number = "";
        for (int j = 0; j < 4; j++)
            number += Rnd.Range(0, 10);

        string matrixa = kw1.Replace('J', 'I');
        string matrixb = "AFLQVBGMRWCHNSXDIOTYEKPUZ";
        string matrixd = kw2.Replace('J', 'I');

        // Create Matrix C by converting numbers to words
        string matrixc = "";
        for (int dd = 0; dd < number.Length; dd++)
        {
            switch (number[dd])
            {
                case '0': matrixc += "ZERO"; break;
                case '1': matrixc += "ONE"; break;
                case '2': matrixc += "TWO"; break;
                case '3': matrixc += "THREE"; break;
                case '4': matrixc += "FOUR"; break;
                case '5': matrixc += "FIVE"; break;
                case '6': matrixc += "SIX"; break;
                case '7': matrixc += "SEVEN"; break;
                case '8': matrixc += "EIGHT"; break;
                case '9': matrixc += "NINE"; break;
            }
        }
        string snnums = Bomb.GetSerialNumberNumbers().Join("");
        matrixa = getKey(matrixa, "ABCDEFGHIKLMNOPQRSTUVWXYZ", Bomb.GetSerialNumberNumbers().Last() % 2 == 0);
        matrixc = getKey(matrixc, "ABCDEFGHIKLMNOPQRSTUVWXYZ", (snnums[1] - '0') % 2 == 1);
        matrixd = getKey(matrixd, "ABCDEFGHIKLMNOPQRSTUVWXYZ", (snnums[0] - '0') % 2 == 0);
        string encryptedKw;
        if (invert)
        {
            Log("INV ORANGE", "Matrix A: {0}", matrixa);
            Log("INV ORANGE", "Matrix B: {0}", matrixb);
            Log("INV ORANGE", "Matrix C: {0}", matrixc);
            Log("INV ORANGE", "Matrix D: {0}", matrixd);
            Log("INV ORANGE", "Begin Bazeries Encryption");
            encrypted = BazeriesEnc(encrypted, matrixb, matrixc, number, invert);
            Log("INV ORANGE", "Begin Foursquare Encryption");
            encrypted = FoursquareEnc(encrypted, matrixa, matrixb, matrixc, matrixd, invert);
            Log("INV ORANGE", "Begin Collon Encryption");
            encryptedKw = CollonEnc(kw2.Replace('J', 'I'), matrixa, invert);
            Log("INV ORANGE", "Encrypted Keyword: {0}", encryptedKw);
        }
        else
        {
            Log("ORANGE", "Matrix A: {0}", matrixa);
            Log("ORANGE", "Matrix B: {0}", matrixb);
            Log("ORANGE", "Matrix C: {0}", matrixc);
            Log("ORANGE", "Matrix D: {0}", matrixd);
            Log("ORANGE", "Begin Foursquare Encryption");
            encrypted = FoursquareEnc(encrypted, matrixa, matrixb, matrixc, matrixd, invert);
            Log("ORANGE", "Begin Bazeries Encryption");
            encrypted = BazeriesEnc(encrypted, matrixb, matrixc, number, invert);
            Log("ORANGE", "Begin Collon Encryption");
            encryptedKw = CollonEnc(kw2.Replace('J', 'I'), matrixa, invert);
            Log("ORANGE", "Encrypted Keyword: {0}", encryptedKw);
        }

        string alpha = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        string screenText = "";
        for (int aa = 0; aa < 6; aa++)
        {
            if (b[aa])
            {
                screenText += encrypted[aa];
                encrypted = encrypted.Substring(0, aa) + "J" + encrypted.Substring(aa + 1);
            }
            else
                screenText += alpha[Rnd.Range(0, 25)];
        }

        return newArray(
            new PageInfo(
                new ScreenText(encrypted, 40),
                new ScreenText(screenText, 40),
                new ScreenText(encryptedKw.Substring(0, encryptedKw.Length / 2), 35)),
            new PageInfo(
                new ScreenText(encryptedKw.Substring(encryptedKw.Length / 2), 35),
                new ScreenText(kw1.ToUpperInvariant(), 35),
                new ScreenText(number.ToUpperInvariant(), 40)));
    }
    private string FoursquareEnc(string word, string ma, string mb, string mc, string md, bool invert)
    {
        string encrypted = "";
        for (int gg = 0; gg < 6; gg += 2)
        {
            if (invert)
            {
                int n1 = mb.IndexOf(word[gg]);
                int n2 = mc.IndexOf(word[gg + 1]);
                encrypted += ma[(n2 % 5) + 5 * (n1 / 5)];
                encrypted += md[(n1 % 5) + 5 * (n2 / 5)];
                Log("INV ORANGE", "{0}{1} -> {2}{3}", word[gg], word[gg + 1], encrypted[gg], encrypted[gg + 1]);
            }
            else
            {
                int n1 = ma.IndexOf(word[gg]);
                int n2 = md.IndexOf(word[gg + 1]);
                encrypted += mb[(n2 % 5) + 5 * (n1 / 5)];
                encrypted += mc[(n1 % 5) + 5 * (n2 / 5)];
                Log("ORANGE", "{0}{1} -> {2}{3}", word[gg], word[gg + 1], encrypted[gg], encrypted[gg + 1]);
            }
        }
        return encrypted;
    }
    private string BazeriesEnc(string word, string mb, string mc, string num, bool invert)
    {
        string encryptedPieces = "";
        int n = 0;
        int subgroup = 0;
        for (int bb = 0; bb < num.Length; bb++)
            subgroup += num[bb] - '0';
        subgroup = subgroup % 4 + 2;
        for (int aa = 0; aa < word.Length; aa++)
        {
            char l;
            if (invert)
            {
                l = mb[mc.IndexOf(word[aa])];
                Log("INV ORANGE", "{0} -> {1}", word[aa], l);
            }
            else
            {
                l = mc[mb.IndexOf(word[aa])];
                Log("ORANGE", "{0} -> {1}", word[aa], l);
            }
            encryptedPieces += l;
            n++;
            if (n == subgroup)
            {
                encryptedPieces += "|";
                n = 0;
            }
        }
        string[] spl = encryptedPieces.Split('|');
        string encrypted = "";
        for (int cc = 0; cc < spl.Length; cc++)
        {
            char[] temp = spl[cc].ToCharArray();
            Array.Reverse(temp);
            encrypted += new string(temp);
        }
        Log(invert ? "INV ORANGE" : "ORANGE", "Subgroup Number: {0}", subgroup);
        Log(invert ? "INV ORANGE" : "ORANGE", "{0} -> {1}", encryptedPieces, encrypted);
        return encrypted;
    }
    private string CollonEnc(string word, string key, bool invert)
    {
        string encrypted = "";
        for (int aa = 0; aa < word.Length; aa++)
        {
            int cursor = key.IndexOf(word[aa]);
            List<int> possrow = new List<int> { 0, 1, 2, 3, 4 };
            List<int> posscol = new List<int> { 0, 1, 2, 3, 4 };
            possrow.Remove(cursor % 5);
            posscol.Remove(cursor / 5);
            int row = possrow[Rnd.Range(0, 4)] + 5 * (cursor / 5);
            int col = (cursor % 5) + 5 * posscol[Rnd.Range(0, 4)];
            encrypted += key[row];
            encrypted += key[col];
            Log(invert ? "INV ORANGE" : "ORANGE", "{0} -> {1}{2}", word[aa], encrypted[aa * 2], encrypted[aa * 2 + 1]);
        }
        return encrypted;
    }
    #endregion

    #region Red Cipher
    protected PageInfo[] redcipher(string word, bool invert = false)
    {
        string encrypted = "";
        bool[] b = new bool[6];
        for (int aa = 0; aa < 6; aa++)
        {
            if (word[aa] == 'J')
            {
                encrypted += "ABCDEFGHIKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 25)];
                b[aa] = true;
            }
            else
                encrypted += word[aa];
        }
        string[] words = new string[3];
        for (int bb = 0; bb < 3; bb++)
        {
            int index = Rnd.Range(0, wordList.Count);
            words[bb] = wordList[index][Rnd.Range(0, wordList[index].Count)].ToUpperInvariant();
            wordList[index].Remove(words[bb].ToUpperInvariant());
        }
        int[] snnumbers = Bomb.GetSerialNumberNumbers().ToArray();
        string key = getKey(words[0].Replace('J', 'I'), "ABCDEFGHIKLMNOPQRSTUVWXYZ", snnumbers[0] % 2 == 1);
        string key2 = getKey(words[1].Replace('J', 'I'), "ABCDEFGHIKLMNOPQRSTUVWXYZ", snnumbers[1] % 2 == 0);
        string key3 = getKey(words[2].Replace('J', 'I'), "ABCDEFGHIKLMNOPQRSTUVWXYZ", snnumbers.Last() % 2 == 1);
        if (invert)
        {
            Log("INV RED", "Playfair Key: {0}", key);
            Log("INV RED", "Begin Playfair Encryption");
            encrypted = PlayfairEnc(encrypted, key, invert);
            Log("INV RED", "CM Bifid Key 1: {0}", key);
            Log("INV RED", "CM Bifid Key 2: {0}", key2);
            Log("INV RED", "Begin CM Bifid Encryption");
            encrypted = CMBifidEnc(encrypted, key, key2, invert);
            Log("INV RED", "Trisquare Key 1: {0}", key);
            Log("INV RED", "Trisquare Key 2: {0}", key2);
            Log("INV RED", "Trisquare Key 3: {0}", key3);
            Log("INV RED", "Begin Trisquare Encryption");
            encrypted = TrisquareEnc(encrypted, key, key2, key3, invert);
        }
        else
        {
            Log("RED", "Trisquare Key 1: {0}", key);
            Log("RED", "Trisquare Key 2: {0}", key2);
            Log("RED", "Trisquare Key 3: {0}", key3);
            Log("RED", "Begin Trisquare Encryption");
            encrypted = TrisquareEnc(encrypted, key, key2, key3, invert);
            Log("RED", "CM Bifid Key 1: {0}", key);
            Log("RED", "CM Bifid Key 2: {0}", key2);
            Log("RED", "Begin CM Bifid Encryption");
            encrypted = CMBifidEnc(encrypted, key, key2, invert);
            Log("RED", "Playfair Key: {0}", key);
            Log("RED", "Begin Playfair Encryption");
            encrypted = PlayfairEnc(encrypted, key, invert);
        }
        string alpha = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        string screenText = "";
        for (int aa = 0; aa < 6; aa++)
        {
            if (b[aa])
            {
                screenText += encrypted[aa];
                encrypted = encrypted.Substring(0, aa) + "J" + encrypted.Substring(aa + 1);
            }
            else
                screenText += alpha[Rnd.Range(0, alpha.Length)];
        }
        return newArray(
            new PageInfo(new ScreenText(encrypted, 40), new ScreenText(screenText, 40)),
            new PageInfo(words.Select(w => new ScreenText(w, 35)).ToArray()));
    }
    private string CMBifidEnc(string word, string kw1, string kw2, bool invert)
    {
        string encrypted = "";
        string letters = word.ToUpperInvariant();
        if (invert)
        {
            string nums = "";
            for (int hh = 0; hh < 6; hh++)
            {
                int n = kw2.IndexOf(word[hh]);
                nums += n / 5;
                nums += n % 5;
            }
            Log("INV RED", "Rows|Columns: {0}", nums);
            for (int ii = 0; ii < 6; ii++)
            {
                encrypted += kw1[nums[ii + 6] - '0' + 5 * (nums[ii] - '0')];
                Log("INV RED", "{0} -> {1}{2} -> {3}", letters[ii], nums[ii], nums[ii + 6], encrypted[ii]);
            }
        }
        else
        {
            string rows = "";
            string cols = "";
            for (int hh = 0; hh < 6; hh++)
            {
                int n = kw1.IndexOf(word[hh]);
                rows += n / 5;
                cols += n % 5;
            }
            string nums = rows + cols;
            Log("RED", "Rows|Columns: {0}|{1}", rows, cols);
            for (int ii = 0; ii < 6; ii++)
            {
                encrypted += kw2[nums[ii * 2 + 1] - '0' + 5 * (nums[ii * 2] - '0')];
                Log("RED", "{0} -> {1}{2} -> {3}", letters[ii], nums[ii * 2], nums[ii * 2 + 1], encrypted[ii]);
            }
        }
        return encrypted;
    }
    private string TrisquareEnc(string word, string kw1, string kw2, string kw3, bool invert)
    {
        string encrypted = "";
        for (int gg = 0; gg < 6; gg++)
        {
            if (invert)
            {
                int n1 = kw3.IndexOf(word[gg]);
                gg++;
                int n2 = kw3.IndexOf(word[gg]);
                encrypted += kw1[n2 % 5 + 5 * (n1 / 5)];
                encrypted += kw2[n1 % 5 + 5 * (n2 / 5)];
                Log("INV RED", "{0}{1} -> {2}{3}", word[gg - 1], word[gg], encrypted[gg - 1], encrypted[gg]);
            }
            else
            {
                int n1 = kw1.IndexOf(word[gg]);
                gg++;
                int n2 = kw2.IndexOf(word[gg]);
                encrypted += kw3[n2 % 5 + 5 * (n1 / 5)];
                encrypted += kw3[n1 % 5 + 5 * (n2 / 5)];
                Log("RED", "{0}{1} -> {2}{3}", word[gg - 1], word[gg], encrypted[gg - 1], encrypted[gg]);
            }
        }
        return encrypted;
    }
    private string PlayfairEnc(string word, string key, bool invert)
    {
        string encrypted = "";
        int col = 0;
        int row = 0;
        char[][] matrix = new char[][]
        {
            new char[5],
            new char[5],
            new char[5],
            new char[5],
            new char[5]
        };
        for (int dd = 0; dd < key.Length; dd++)
        {
            matrix[row][col] = key[dd];
            col++;
            if (col == 5)
            {
                col = 0;
                row++;
            }
        }
        for (int ee = 0; ee < word.Length; ee++)
        {
            int col1 = 0;
            int row1 = 0;
            int col2 = 0;
            int row2 = 0;
            char char1 = word[ee];
            ee++;
            char char2 = word[ee];
            for (int ff = 0; ff < 5; ff++)
            {
                for (int gg = 0; gg < 5; gg++)
                {
                    if (char1 == matrix[ff][gg])
                    {
                        col1 = gg;
                        row1 = ff;
                    }
                    if (char2 == matrix[ff][gg])
                    {
                        col2 = gg;
                        row2 = ff;
                    }
                }
            }
            if (row1 == row2 && col1 == col2)
            {
                // Same letter: do nothing
            }
            else if (row1 == row2)
            {
                if (invert)
                {
                    col1 = (col1 + 4) % 5;
                    col2 = (col2 + 4) % 5;
                }
                else
                {
                    col1 = (col1 + 1) % 5;
                    col2 = (col2 + 1) % 5;
                }
            }
            else if (col1 == col2)
            {
                if (invert)
                {
                    row1 = (row1 + 4) % 5;
                    row2 = (row2 + 4) % 5;
                }
                else
                {
                    row1 = (row1 + 1) % 5;
                    row2 = (row2 + 1) % 5;
                }
            }
            else
            {
                int temp = col1;
                col1 = col2;
                col2 = temp;
            }
            encrypted += matrix[row1][col1];
            encrypted += matrix[row2][col2];
            Log(invert ? "INV RED" : "RED", "{0} -> {1}", word[ee - 1] + word[ee], encrypted[ee - 1] + encrypted[ee]);
        }
        return encrypted;
    }
    #endregion

    #region Violet Cipher
    protected PageInfo[] violetcipher(string word, bool invert = false)
    {
        string text = wordList[2][Rnd.Range(0, wordList[2].Count)].ToUpperInvariant();
        wordList[2].Remove(text.ToUpperInvariant());
        if (invert)
        {
            Log("INV VIOLET", "Begin Porta Encryption");
            string portaResult = PortaEnc(word.ToUpperInvariant(), text, invert);
            Log("INV VIOLET", "Begin Route Transposition");
            var routeTransResult = RouteTrans(portaResult.ToUpperInvariant(), invert);
            Log("INV VIOLET", "Begin Quagmire Encryption");
            var quagmireResult = QuagmireEnc(routeTransResult.Encrypted, text.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(quagmireResult.Encrypted, 40), quagmireResult.Keyword1, routeTransResult.RouteNumber),
                new PageInfo(quagmireResult.Keyword2));
        }
        else
        {
            Log("VIOLET", "Begin Quagmire Encryption");
            var quagmireResult = QuagmireEnc(word, text.ToUpperInvariant(), invert);
            Log("VIOLET", "Begin Route Transposition");
            var routeTransResult = RouteTrans(quagmireResult.Encrypted.ToUpperInvariant(), invert);
            Log("VIOLET", "Begin Porta Encryption");
            var portaEncResult = PortaEnc(routeTransResult.Encrypted.ToUpperInvariant(), text, invert);
            return newArray(
                new PageInfo(new ScreenText(portaEncResult, 40), quagmireResult.Keyword1, routeTransResult.RouteNumber),
                new PageInfo(quagmireResult.Keyword2));
        }
    }
    private string PortaEnc(string word, string kw1, bool invert)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string[] chart = new string[]
        {
            "NOPQRSTUVWXYZ",
            "OPQRSTUVWXYZN",
            "PQRSTUVWXYZNO",
            "QRSTUVWXYZNOP",
            "RSTUVWXYZNOPQ",
            "STUVWXYZNOPQR",
            "TUVWXYZNOPQRS",
            "UVWXYZNOPQRST",
            "VWXYZNOPQRSTU",
            "WXYZNOPQRSTUV",
            "XYZNOPQRSTUVW",
            "YZNOPQRSTUVWX",
            "ZNOPQRSTUVWXY"
        };
        string encrypted = "";
        for (int aa = 0; aa < 6; aa++)
        {
            if (alpha.IndexOf(word[aa]) < 13)
                encrypted += chart[alpha.IndexOf(kw1[aa]) / 2][alpha.IndexOf(word[aa])];
            else
                encrypted += alpha[chart[alpha.IndexOf(kw1[aa]) / 2].IndexOf(word[aa])];
            Log(invert ? "INV VIOLET" : "VIOLET", "{0} -> {1}", word[aa], encrypted[aa]);
        }
        return encrypted;
    }
    private struct RouteTransResult { public string Encrypted; public ScreenText RouteNumber; }
    private RouteTransResult RouteTrans(string word, bool invert)
    {
        string routenumber = Rnd.Range(1, 3) + "" + Rnd.Range(1, 7);
        string encrypted = "";
        string cipher;
        switch (routenumber)
        {
            case "11": cipher = "123654"; break;
            case "12": cipher = "234165"; break;
            case "13": cipher = "345216"; break;
            case "14": cipher = "456321"; break;
            case "15": cipher = "561432"; break;
            case "16": cipher = "612543"; break;
            case "01": cipher = "126354"; break;
            case "02": cipher = "231465"; break;
            case "03": cipher = "342516"; break;
            case "04": cipher = "453621"; break;
            case "05": cipher = "564132"; break;
            default /*06*/: cipher = "615243"; break;
        }
        string order = "123456";
        for (int i = 0; i < 6; i++)
            encrypted += invert ? word[order.IndexOf(cipher[i])] : word[cipher.IndexOf(order[i])];

        Log(invert ? "INV VIOLET" : "VIOLET", "{0} + {1} => {2}", word, routenumber, encrypted);
        return new RouteTransResult { Encrypted = encrypted, RouteNumber = new ScreenText(routenumber, 40) };
    }
    private struct QuagmireResult { public string Encrypted; public ScreenText Keyword1, Keyword2; }
    private QuagmireResult QuagmireEnc(string word, string kw1, bool invert)
    {
        int length = Rnd.Range(0, wordList.Count);
        string kw2 = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
        wordList[length].Remove(kw2.ToUpperInvariant());
        string key = getKey(kw2.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetOnIndicators().Count() % 2 == 0);
        string[] cipher = new string[7];
        cipher[0] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int aa = 0; aa < 6; aa++)
            cipher[aa + 1] = key.Substring(key.IndexOf(kw1[aa])) + key.Substring(0, key.IndexOf(kw1[aa]));

        Log(invert ? "INV VIOLET" : "VIOLET", "Quagmire Rows: ");
        for (var i = 1; i <= 6; i++)
            Log(invert ? "INV VIOLET" : "VIOLET", cipher[i]);

        string encrypted = "";
        for (int bb = 0; bb < 6; bb++)
        {
            if (invert)
            {
                encrypted += cipher[0][cipher[bb + 1].IndexOf(word[bb])];
                Log("INV VIOLET", "{0} -> {1}", word[bb], encrypted[bb]);
            }
            else
            {
                encrypted += cipher[bb + 1][cipher[0].IndexOf(word[bb])];
                Log("VIOLET", "{0} -> {1}", word[bb], encrypted[bb]);
            }
        }
        return new QuagmireResult { Encrypted = encrypted, Keyword1 = new ScreenText(kw1, 40), Keyword2 = new ScreenText(kw2, 35) };
    }
    #endregion

    #region White Cipher
    protected PageInfo[] whitecipher(string word, bool invert = false)
    {
        List<int> lengths;
        switch (Rnd.Range(0, 6))
        {
            case 0: lengths = new List<int> { 4, 4 }; break;
            case 1: lengths = new List<int> { 4, 0, 0 }; break;
            case 2: lengths = new List<int> { 3, 1, 0 }; break;
            case 3: lengths = new List<int> { 2, 2, 0 }; break;
            case 4: lengths = new List<int> { 2, 1, 1 }; break;
            default: lengths = new List<int> { 0, 0, 0, 0 }; break;
        }
        lengths.Shuffle();

        string kw = "";

        for (int aa = 0; aa < lengths.Count; aa++)
        {
            string tempkw = wordList[lengths[aa]][Rnd.Range(0, wordList[lengths[aa]].Count)].ToUpperInvariant();
            wordList[lengths[aa]].Remove(tempkw.ToUpperInvariant());
            kw += tempkw.ToUpperInvariant();
        }

        if (invert)
        {
            Log("INV WHITE", "Begin Base Caesar Encryption");
            var baseCaesarResult = BaseCaesarEnc(word.ToUpperInvariant(), invert);
            Log("INV WHITE", "Begin Sean Encryption");
            var seanResult = SeanEnc(baseCaesarResult.Encrypted.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("INV WHITE", "Begin Grille Transposition");
            var grilleResult = GrilleTrans(kw.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(seanResult, 40), baseCaesarResult.BaseOffset),
                new PageInfo(new ScreenText(grilleResult.Substring(0, 8), 35), new ScreenText(grilleResult.Substring(8), 35)));
        }
        else
        {
            Log("WHITE", "Begin Sean Encryption");
            var seanResult = SeanEnc(word.ToUpperInvariant(), kw.ToUpperInvariant(), invert);
            Log("WHITE", "Begin Base Caesar Encryption");
            var baseCaesarResult = BaseCaesarEnc(seanResult.ToUpperInvariant(), invert);
            Log("WHITE", "Begin Grille Transposition");
            var grilleResult = GrilleTrans(kw.ToUpperInvariant(), invert);
            return newArray(
                new PageInfo(new ScreenText(baseCaesarResult.Encrypted, 40), baseCaesarResult.BaseOffset),
                new PageInfo(new ScreenText(grilleResult.Substring(0, 8), 35), new ScreenText(grilleResult.Substring(8), 35)));
        }
    }
    private string SeanEnc(string word, string kw, bool invert)
    {
        string encrypted = "";
        string key = getKey(kw.ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetOnIndicators().Count<string>() % 2 == 0);
        Log(invert ? "INV WHITE" : "WHITE", "Key: {0}", key);

        string[] cipher = { key.Substring(0, 13), key.Substring(13) };

        bool clockwise = "ZABCDEFGHIJKLMNOPQRSTUVWXY".IndexOf(Bomb.GetSerialNumberLetters().Skip(1).First()) % 2 == 1;

        Log(invert ? "INV WHITE" : "WHITE", clockwise
            ? "Alphanumeric position of the 2nd letter of the serial number is odd. Turning the key clockwise."
            : "Alphanumeric position of the 2nd letter of the serial number is even. Turning the key counter-clockwise.");

        for (int bb = 0; bb < 6; bb++)
        {
            if (cipher[0].IndexOf(word[bb]) >= 0)
                encrypted += cipher[1][cipher[0].IndexOf(word[bb])];
            else
                encrypted += cipher[0][cipher[1].IndexOf(word[bb])];

            if (clockwise)
            {
                char top = cipher[0][12];
                char bottom = cipher[1][0];
                cipher[0] = bottom + cipher[0].Substring(0, 12);
                cipher[1] = cipher[1].Substring(1) + top;
                Log(invert ? "INV WHITE" : "WHITE", "{0} -> {1}", word[bb], encrypted[bb]);
                Log(invert ? "INV WHITE" : "WHITE", cipher[0]);
                Log(invert ? "INV WHITE" : "WHITE", cipher[1]);
            }
            else
            {
                char top = cipher[0][0];
                char bottom = cipher[1][12];
                cipher[0] = cipher[0].Substring(1) + bottom;
                cipher[1] = top + cipher[1].Substring(0, 12);
                Log(invert ? "INV WHITE" : "WHITE", "{0} -> {1}", word[bb], encrypted[bb]);
                Log(invert ? "INV WHITE" : "WHITE", cipher[0]);
                Log(invert ? "INV WHITE" : "WHITE", cipher[1]);
            }
        }
        return encrypted;
    }
    private struct BaseCaesarResult { public string Encrypted; public ScreenText BaseOffset; }
    private BaseCaesarResult BaseCaesarEnc(string word, bool invert)
    {
        int offset = Rnd.Range(1, 25) + 26 * Rnd.Range(1, 6);
        int sum = 0;
        Log(invert ? "INV WHITE" : "WHITE", "Generated Offset: {0}", offset);
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string encrypted = "";
        string logoutput = "";
        for (int i = 0; i < 6; i++)
        {
            if (invert)
            {
                encrypted += alpha[(alpha.IndexOf(word[i]) + offset) % 26];
                Log("INV WHITE", "{0} -> {1}", word[i], encrypted[i]);
            }
            else
            {
                encrypted += alpha[((alpha.IndexOf(word[i]) - offset) % 26 + 26) % 26];
                Log("WHITE", "{0} -> {1}", word[i], encrypted[i]);
            }
            sum += alpha.IndexOf(encrypted[i]) + 1;
            logoutput += (alpha.IndexOf(encrypted[i]) + 1) + " + ";
        }
        sum = sum % 8 + 2;
        logoutput = string.Format("(({0}) % 8) + 2 = {1}", logoutput.Substring(0, logoutput.Length - 3), sum);
        Log(invert ? "INV WHITE" : "WHITE", "Generated Base: {0}", logoutput);
        string baseOffset = "";
        for (int n = offset; n != 0; n /= sum)
            baseOffset = (n % sum) + baseOffset;

        Log(invert ? "INV WHITE" : "WHITE", "{0} -> {1}", offset, baseOffset);
        return new BaseCaesarResult { Encrypted = encrypted, BaseOffset = new ScreenText(baseOffset, 40) };
    }
    private string GrilleTrans(string word, bool invert)
    {
        int num = Bomb.GetPortCount() % 4;
        int[] pos = { 0, 12, 13, 4, 8, 5, 9, 1, 10, 2, 14, 3, 15, 6, 7, 11 };
        for (int aa = 0; aa < 16; aa++)
            pos[aa] = ((pos[aa] - 4 * num) % 16 + 16) % 16;
        string encrypted = "";
        for (int k = 0; k < 16; k++)
            encrypted += word[pos[k]];

        Log(invert ? "INV WHITE" : "WHITE", "Clockwise rotations: {0}", num);
        Log(invert ? "INV WHITE" : "WHITE", "{0} -> {1}", word, encrypted);
        return encrypted;
    }
    #endregion

    #region Yellow Cipher
    protected PageInfo[] yellowcipher(string word, bool invert = false)
    {
        string encrypted;
        MorbitResult morbitResult;
        HillResult hillResult;
        if (invert)
        {
            Log("INV YELLOW", "Begin Trifid Encryption");
            var trifidResult = TrifidEnc(word.ToUpperInvariant(), invert);
            Log("INV YELLOW", "Begin Morbit Encryption");
            morbitResult = MorbitEnc(trifidResult.Keyword.ToUpperInvariant(), invert);
            Log("INV YELLOW", "Begin Hill Encryption");
            hillResult = HillEnc(trifidResult.Encrypted.ToUpperInvariant(), invert);
            encrypted = hillResult.Encrypted;
        }
        else
        {
            Log("YELLOW", "Begin Hill Encryption");
            hillResult = HillEnc(word, invert);
            Log("YELLOW", "Begin Trifid Encryption");
            var trifidResult = TrifidEnc(hillResult.Encrypted, invert);
            Log("YELLOW", "Begin Morbit Encryption");
            morbitResult = MorbitEnc(trifidResult.Keyword.ToUpperInvariant(), invert);
            encrypted = trifidResult.Encrypted.ToUpperInvariant();
        }
        var div = Enumerable.Range(0, 2).Select(r => (int) Math.Round(morbitResult.Encrypted.Length * (r + 1) / 3d)).ToArray();
        return newArray(
            new PageInfo(
                new ScreenText(encrypted, 40),
                new ScreenText(morbitResult.Encrypted.Substring(0, div[0]), 40),
                new ScreenText(morbitResult.Encrypted.Substring(div[0], div[1] - div[0]), 40)),
            new PageInfo(
                new ScreenText(morbitResult.Encrypted.Substring(div[1]), 40),
                morbitResult.Keyword,
                hillResult.HillMatrix));
    }
    private struct MorbitResult { public string Encrypted; public ScreenText Keyword; }
    private MorbitResult MorbitEnc(string word, bool invert)
    {
        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string morsecode = "";
        for (int aa = 0; aa < word.Length; aa++)
            morsecode += morse[alpha.IndexOf(word[aa])] + "X";

        if (morsecode.Length % 2 == 1)
            morsecode = morsecode.Substring(0, morsecode.Length - 1);

        char[] nums = new char[8];
        int items = 0;
        string key = "12345678";
        string kw = wordList[4][Rnd.Range(0, wordList[4].Count)].ToUpperInvariant();
        wordList[4].Remove(kw.ToUpperInvariant());
        for (int cc = 0; cc < alpha.Length; cc++)
        {
            for (int dd = 0; dd < kw.Length; dd++)
            {
                if (kw[dd].ToString().IndexOf(alpha[cc]) >= 0)
                {
                    nums[dd] = key[items];
                    items++;
                }
            }
        }

        key = new string(nums);

        string encrypted = "";
        for (int ee = 0; ee < morsecode.Length; ee += 2)
        {
            switch (morsecode.Substring(ee, 2))
            {
                case "..": encrypted += key[0]; break;
                case ".-": encrypted += key[1]; break;
                case ".X": encrypted += key[2]; break;
                case "-.": encrypted += key[3]; break;
                case "--": encrypted += key[4]; break;
                case "-X": encrypted += key[5]; break;
                case "X.": encrypted += key[6]; break;
                case "X-": encrypted += key[7]; break;
            }
        }

        Log(invert ? "INV YELLOW" : "YELLOW", "Morbit Key: {0}", key);
        Log(invert ? "INV YELLOW" : "YELLOW", "Morbit Encrypted Word: {0}", encrypted);

        return new MorbitResult { Encrypted = encrypted, Keyword = new ScreenText(kw, 35) };
    }
    private struct TrifidResult { public string Encrypted, Keyword; }
    private TrifidResult TrifidEnc(string word, bool inverse)
    {
        List<string> triedWords = new List<string>();
        string[] array = new string[]
        {
            "11111111122222222233333333",
            "11122233311122233311122233",
            "12312312312312312312312312"
        };
        string[] numbers = new string[3];
        bool invalid;
        int length;
        string kw;
        string key;
        do
        {
            invalid = false;
            length = Rnd.Range(0, wordList.Count);
            kw = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
            while (triedWords.Contains(kw))
            {
                length = Rnd.Range(0, wordList.Count);
                kw = wordList[length][Rnd.Range(0, wordList[length].Count)].ToUpperInvariant();
            }
            triedWords.Add(kw.ToUpperInvariant());
            key = getKey(kw, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Bomb.GetBatteryCount() % 2 == 1);
            numbers[0] = "";
            numbers[1] = "";
            numbers[2] = "";
            if (inverse)
            {
                for (int aa = 0; aa < 6; aa++)
                {
                    int cursor = key.IndexOf(word[aa]);
                    numbers[aa / 2] += array[0][cursor];
                    numbers[aa / 2] += array[1][cursor];
                    numbers[aa / 2] += array[2][cursor];
                }
                for (int bb = 0; bb < 6; bb++)
                {
                    string n = string.Concat(numbers[0][bb], numbers[1][bb], numbers[2][bb]);
                    if (n.Equals("333"))
                    {
                        invalid = true;
                        break;
                    }
                }
            }
            else
            {
                for (int aa = 0; aa < 6; aa++)
                {
                    int cursor = key.IndexOf(word[aa]);
                    numbers[0] = numbers[0] + "" + array[0][cursor];
                    numbers[1] = numbers[1] + "" + array[1][cursor];
                    numbers[2] = numbers[2] + "" + array[2][cursor];
                }
                for (int bb = 0; bb < 3; bb++)
                {
                    string text4 = string.Concat(numbers[bb][0], numbers[bb][1], numbers[bb][2]);
                    string text5 = string.Concat(numbers[bb][3], numbers[bb][4], numbers[bb][5]);
                    if (text4.Equals("333") || text5.Equals("333"))
                    {
                        invalid = true;
                        break;
                    }
                }
            }
        }
        while (invalid);

        var encrypted = "";

        if (inverse)
        {
            wordList[length].Remove(kw.ToUpperInvariant());
            Log("INV YELLOW", "Trifid Key: {0}", key);
            Log("INV YELLOW", "Trifid Numbers:");
            Log("INV YELLOW", "{0}", numbers[0]);
            Log("INV YELLOW", "{0}", numbers[1]);
            Log("INV YELLOW", "{0}", numbers[2]);

            for (int bb = 0; bb < 6; bb++)
            {
                string text6 = string.Concat(numbers[0][bb], numbers[1][bb], numbers[2][bb]);
                string pos = "931";
                int num = 0;
                for (int d = 0; d < 3; d++)
                    num += (pos[d] - '0') * (text6[d] - '1');
                encrypted += key[num];
            }
            Log("INV YELLOW", "Trifid Encrypted Word: {0}", encrypted);
            return new TrifidResult { Encrypted = encrypted, Keyword = kw };
        }
        else
        {
            wordList[length].Remove(kw.ToUpperInvariant());
            triedWords.Clear();
            Log("YELLOW", "Trifid Key: {0}", key);
            Log("YELLOW", "Trifid Numbers:");
            Log("YELLOW", "{0}", numbers[0]);
            Log("YELLOW", "{0}", numbers[1]);
            Log("YELLOW", "{0}", numbers[2]);

            for (int bb = 0; bb < 3; bb++)
            {
                string[] nums = {
                    string.Concat(numbers[bb][0], numbers[bb][1], numbers[bb][2]),
                    string.Concat(numbers[bb][3], numbers[bb][4], numbers[bb][5])
                };

                string pos = "931";
                for (int cc = 0; cc < 2; cc++)
                {
                    int num = 0;
                    for (int d = 0; d < 3; d++)
                        num += (pos[d] - '0') * (nums[cc][d] - '1');
                    encrypted += key[num];
                }
            }
            Log("YELLOW", "Trifid Encrypted Word: {0}", encrypted);
            return new TrifidResult { Encrypted = encrypted, Keyword = kw };
        }
    }
    private struct HillResult { public string Encrypted; public ScreenText HillMatrix; }
    private HillResult HillEnc(string word, bool invert)
    {
        string encrypted = "";
        int[] matrix = new int[4];
        matrix[1] = Rnd.Range(0, 26);
        matrix[2] = Rnd.Range(0, 26);
        if ((matrix[1] * matrix[2]) % 2 == 1)
            matrix[0] = Rnd.Range(1, 13) * 2;
        else
        {
            do matrix[0] = Rnd.Range(0, 13) * 2 + 1;
            while ((matrix[0] - matrix[1] * matrix[2]) % 13 == 0);
        }

        Log(invert ? "INV YELLOW" : "YELLOW", "NUMBER A: {0}", matrix[0]);
        Log(invert ? "INV YELLOW" : "YELLOW", "NUMBER B: {0}", matrix[1]);
        Log(invert ? "INV YELLOW" : "YELLOW", "NUMBER C: {0}", matrix[2]);

        int[] nums = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        for (int aa = 0; aa < 26; aa++)
        {
            int num = aa * matrix[0] - matrix[1] * matrix[2];
            if (num % 2 == 0 || num % 13 == 0)
                nums = nums.Where(val => val != aa).ToArray();
        }
        matrix[3] = nums[Rnd.Range(0, nums.Length)];

        Log(invert ? "INV YELLOW" : "YELLOW", "NUMBER D: {0}", matrix[3]);

        string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var hillMatrix = new ScreenText(matrix.Join("-"), 35);
        if (invert)
        {
            matrix = new int[] { matrix[3], -matrix[1], -matrix[2], matrix[0] };
            int determinant = matrix[0] * matrix[3] - matrix[1] * matrix[2];
            int inverse = 1;
            while (((inverse * determinant) % 26 + 26) % 26 != 1)
                inverse++;

            Log("INV YELLOW", "N: {0}", inverse);
            for (int i = 0; i < 4; i++)
                matrix[i] = ((matrix[i] * inverse) % 26 + 26) % 26;
            Log("INV YELLOW", "Inverse matrix: {0}", matrix.Join(" "));
        }

        for (int bb = 0; bb < 6; bb++)
        {
            int n1 = (alpha.IndexOf(word[bb]) + 1) % 26;
            bb++;
            int n2 = (alpha.IndexOf(word[bb]) + 1) % 26;
            int l1 = (matrix[0] * n1 + matrix[1] * n2) % 26;
            int l2 = (matrix[2] * n1 + matrix[3] * n2) % 26;
            Log(invert ? "INV YELLOW" : "YELLOW", "{0} {1} -> {2} {3}", n1, n2, l1, l2);
            if (l1 == 0)
                l1 = 26;
            if (l2 == 0)
                l2 = 26;
            encrypted += alpha[l1 - 1];
            encrypted += alpha[l2 - 1];
        }
        Log(invert ? "INV YELLOW" : "YELLOW", "Hill Encrypted Word: {0}", encrypted);

        return new HillResult { Encrypted = encrypted, HillMatrix = hillMatrix };
    }
    #endregion
    #endregion

    #region UI, TP
    protected PageInfo[] pages;
    protected int page;
    protected bool submitScreen;
    protected string answer;
    protected bool moduleSolved;
    protected int moduleId;
    protected bool ZenModeActive;

    protected abstract void Initialize();

    void Start()
    {
        wordList = new Data().allWords;
        leftArrow.OnInteract += delegate () { left(leftArrow); return false; };
        rightArrow.OnInteract += delegate () { right(rightArrow); return false; };
        submit.OnInteract += delegate () { submitWord(submit); return false; };
        foreach (KMSelectable keybutton in keyboard)
        {
            KMSelectable pressedButton = keybutton;
            pressedButton.OnInteract += delegate () { letterPress(pressedButton); return false; };
        }
        Initialize();
        page = 0;
        getScreens();
    }

    protected virtual void left(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform("ArrowPress", transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page - 1 + pages.Length) % pages.Length;
            getScreens();
        }
    }

    protected virtual void right(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform("ArrowPress", transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page + 1) % pages.Length;
            getScreens();
        }
    }

    protected virtual void getScreens()
    {
        submitText.text = (page + 1).ToString();
        for (var screen = 0; screen < 3; screen++)
        {
            screenTexts[screen].text = screen < pages[page].Screens.Length ? pages[page].Screens[screen].Text ?? "" : "";
            screenTexts[screen].fontSize = screen < pages[page].Screens.Length ? pages[page].Screens[screen].FontSize : 40;
        }
        if (arrowTexts != null && arrowTexts.Length >= 2)
        {
            arrowTexts[0].text = (pages[page].LeftArrow ?? '<').ToString();
            arrowTexts[1].text = (pages[page].RightArrow ?? '>').ToString();
        }
    }

    protected virtual void submitWord(KMSelectable submitButton)
    {
        if (moduleSolved)
            return;

        submitButton.AddInteractionPunch();
        if (screenTexts[2].text.Equals(answer))
        {
            Audio.PlaySoundAtTransform("SolveSFX", transform);
            module.HandlePass();
            moduleSolved = true;
            screenTexts[2].text = "";
        }
        else
        {
            Audio.PlaySoundAtTransform("StrikeSFX", transform);
            module.HandleStrike();
            page = 0;
            getScreens();
            submitScreen = false;
        }
    }

    protected virtual void letterPress(KMSelectable pressed)
    {
        if (moduleSolved)
            return;

        pressed.AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform("KeyboardPress", transform);
        if (submitScreen)
        {
            if (screenTexts[2].text.Length < 6)
                screenTexts[2].text += pressed.GetComponentInChildren<TextMesh>().text;
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

    protected virtual void Log(string cipher, string message)
    {
        Debug.LogFormat("[{0} Cipher #{1}] {2}", Name, moduleId, message);
    }
    protected void Log(string cipher, string format, params object[] args)
    {
        Log(cipher, string.Format(format, args));
    }

#pragma warning disable 414
    protected string TwitchHelpMessage = "!{0} right/left/r/l [move between screens] | !{0} submit answerword";
#pragma warning restore 414

    protected virtual IEnumerator ProcessTwitchCommand(string command)
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
        if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length != 6)
            yield break;

        int[] buttons = split[1].Select(getPositionFromChar).ToArray();
        if (buttons.Any(x => x < 0))
            yield break;

        yield return null;
        yield return new WaitForSeconds(0.1f);
        foreach (char let in split[1])
        {
            keyboard[getPositionFromChar(let)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        int? pts;
        if (screenTexts[2].text.Equals(answer) && !ZenModeActive && (pts = TPPoints) != null)
            yield return "awardpointsonsolve " + pts.Value;

        yield return new WaitForSeconds(0.1f);
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    protected IEnumerator TwitchHandleForcedSolve()
    {
        if (submitScreen && !answer.StartsWith(screenTexts[2].text))
        {
            rightArrow.OnInteract();
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
    #endregion
}