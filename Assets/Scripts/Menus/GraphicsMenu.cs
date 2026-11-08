using System;
using UnityEngine;

namespace Assets.Scripts.Menus
{
    internal class GraphicsMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6grxdet1",
                MenuItems = new MenuItem[] {
                    new MenuButton("Screen Resolution", GetCurrentResolution(), NextResolution),
                    new MenuButton("Quality", GetCurrentQuality(), NextQuality),
                    new MenuBlank(),
                    new MenuButton("Virtual Reality", "Unavailable", DoNothing),
                    new MenuBlank(),
                    new MenuButton("Cancel", "", Back)
                }
            };
        }

        private string GetCurrentResolution()
        {
            Resolution current = Screen.currentResolution;
            return current.width + "x" + current.height + "@" + current.refreshRate;
        }

        private string GetCurrentQuality()
        {
            int level = QualitySettings.GetQualityLevel();
            return QualitySettings.names[level];
        }

        private void NextResolution()
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
            {
                return;
            }

            int nextIndex = (Array.IndexOf(resolutions, Screen.currentResolution) + 1) % resolutions.Length;
            Resolution newResolution = resolutions[nextIndex];
            Screen.SetResolution(newResolution.width, newResolution.height, false, newResolution.refreshRate);

            _menuController.Redraw();
        }

        private void NextQuality()
        {
            int nextLevel = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(nextLevel);

            _menuController.Redraw();
        }

        private void DoNothing()
        {
            // VR is not supported in the macOS build.
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}
