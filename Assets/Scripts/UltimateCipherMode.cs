namespace UltimateCipher
{
    public enum Mode
    {
        Normal, // Three ciphers chosen at random
        PinkUC, // All non-inverted ciphers
        CyanUC, // All inverted ciphers
        TrueUC,  // All ciphers
        Debug   // Can only be triggered in the Unity Editor
    }
}
