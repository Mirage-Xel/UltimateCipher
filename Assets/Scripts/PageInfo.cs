using UnityEngine;

namespace UltimateCipher
{
    public sealed class PageInfo
    {
        public ScreenText[] Screens { get; private set; }
        public char? LeftArrow { get; private set; }
        public char? RightArrow { get; private set; }
        public bool PigpenFont { get; set; }
        public Material BackgroundMaterial { get; set; }
        public bool Inverted { get; set; }

        public PageInfo(params ScreenText[] screens) : this(null, null, screens) { }

        public PageInfo(char? leftArrow, char? rightArrow, params ScreenText[] screens)
        {
            Screens = screens;
            LeftArrow = leftArrow;
            RightArrow = rightArrow;
        }
    }
}
