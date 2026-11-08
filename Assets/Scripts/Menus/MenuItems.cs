using System;

namespace Assets.Scripts.Menus
{
    public abstract class MenuItem { }

    public class MenuButton : MenuItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public Action OnClick { get; set; }

        // Optional: called with -1 or +1 for right-click and the left/right arrow keys.
        public Action<int> OnAdjust { get; set; }

        public MenuButton(string text, string value, Action onClick)
        {
            Text = text;
            Value = value;
            OnClick = onClick;
        }

        public MenuButton(string text, string value, Action onClick, Action<int> onAdjust)
            : this(text, value, onClick)
        {
            OnAdjust = onAdjust;
        }
    }

    public class MenuBlank : MenuItem
    {
    }
}
