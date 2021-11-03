using System;

namespace UltimateCipher
{
    public class CipherInfo
    {
        public string Name { get; private set; }
        public Func<ultimateCipher, Func<string, bool, PageInfo[]>> Cipher { get; private set; }
        public int TpPoints { get; private set; }
        public int Index { get; private set; }

        public CipherInfo(string name, Func<ultimateCipher, Func<string, bool, PageInfo[]>> cipher, int tpPoints, int index = 0)
        {
            Name = name;
            Cipher = cipher;
            TpPoints = tpPoints;
            Index = index;
        }

        public SpecificCipherInfo GetSpecific(bool inverted)
        {
            return new SpecificCipherInfo(Name, Cipher, TpPoints, Index, inverted);
        }

        public CipherInfo WithIndex(int index)
        {
            return new CipherInfo(Name, Cipher, TpPoints, index);
        }
    }

    public class SpecificCipherInfo : CipherInfo
    {
        public bool Inverted { get; private set; }
        public string FullName { get { return (Inverted ? "Inverted " : "") + Name + " Cipher"; } }

        public SpecificCipherInfo(string name, Func<ultimateCipher, Func<string, bool, PageInfo[]>> cipher, int tpPoints, int index, bool inverted) : base(name, cipher, tpPoints, index)
        {
            Inverted = inverted;
        }

        public PageInfo[] RunCipher(ultimateCipher uc, string word)
        {
            return Cipher(uc)(word, Inverted);
        }
    }
}
