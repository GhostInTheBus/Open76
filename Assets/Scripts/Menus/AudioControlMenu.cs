namespace Assets.Scripts.Menus
{
    internal class AudioControlMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6audcon1",
                MenuItems = new MenuItem[] {
                    new MenuBlank(),
                    LevelButton("Music Level", AudioCategory.Music),
                    new MenuBlank(),
                    new MenuBlank(),
                    LevelButton("SFX Level", AudioCategory.Sfx),
                    new MenuBlank(),
                    new MenuBlank(),
                    LevelButton("Voice Level", AudioCategory.Voice),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("Back", "", Back)
                }
            };
        }

        // Click raises the level (wrapping back to 0 after 10). Right-click or the arrow keys step it up or down.
        private MenuButton LevelButton(string text, AudioCategory category)
        {
            return new MenuButton(text, GameAudio.GetLevel(category) + " / " + GameAudio.MaxLevel,
                () => Change(category, 1, true),
                delta => Change(category, delta, false));
        }

        private void Change(AudioCategory category, int delta, bool wrap)
        {
            GameAudio.ChangeLevel(category, delta, wrap);
            _menuController.Redraw();
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}
