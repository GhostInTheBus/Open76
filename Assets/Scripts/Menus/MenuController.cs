using System;
using Assets.Scripts.Camera;
using Assets.Scripts.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.Menus
{
    public class MenuController : MonoBehaviour
    {
        public Button MenuButtonPrefab;
        public Transform BlankSeparatorPrefab;

        public VerticalLayoutGroup Items;
        public RawImage Background;
        private CanvasGroup _canvasGroup;

        private IMenu _currentMenu;

        // Use this for initialization
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        // Update is called once per frame
        private void Update()
        {
            if (DevInput.GetKeyDown(KeyCode.Escape))
            {
                if (_currentMenu != null)
                    _currentMenu.Back();
                else
                    ShowMenu<OptionsMenu>();
            }

            // Clicking empty space clears the UI selection, which leaves the arrow keys and Enter dead. Re-select a button.
            if (_currentMenu != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                Button first = Items.GetComponentInChildren<Button>();
                if (first != null)
                    EventSystem.current.SetSelectedGameObject(first.gameObject);
            }

            if (_currentMenu != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                MenuButtonAdjust adjust = EventSystem.current.currentSelectedGameObject.GetComponent<MenuButtonAdjust>();
                if (adjust != null)
                {
                    if (DevInput.GetKeyDown(KeyCode.LeftArrow))
                        adjust.Invoke(-1);
                    else if (DevInput.GetKeyDown(KeyCode.RightArrow))
                        adjust.Invoke(1);
                }
            }
        }

        // A menu action that throws must never take the menu (or the game) down with it.
        private static void SafeInvoke(Action action)
        {
            try
            {
                if (action != null)
                    action();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Menu action failed: " + ex.Message);
            }
        }

        public void CloseMenu()
        {
            _currentMenu = null;

            _canvasGroup.alpha = 0;
            Time.timeScale = 1;
        }

        // Leaves the mission and goes back to the main menu.
        public void AbortMission()
        {
            CloseMenu();
            Game.Instance.IntroPlayed = true;
            CameraManager.Instance.Destroy();
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        public void ShowMenu<T>() where T : IMenu, new()
        {
            _currentMenu = new T();
            EventSystem.current.SetSelectedGameObject(null);

            Redraw();

            _canvasGroup.alpha = 1;

            Time.timeScale = 0;
        }

        public void Redraw()
        {
            MenuDefinition menuDefinition = _currentMenu.BuildMenu(this);

            Texture2D texture = CacheManager.Instance.GetTexture(menuDefinition.BackgroundFilename);
            if (texture != null)
            {
                Background.texture = texture;
                Background.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
            }

            int selectedIndex = EventSystem.current.currentSelectedGameObject == null ? 0 : EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();

            // Destroy is deferred to the end of the frame, so detach the old buttons first or the
            // new ones get the wrong sibling index and a second Redraw in the same frame loses the selection.
            for (int c = Items.transform.childCount - 1; c >= 0; c--)
            {
                Transform child = Items.transform.GetChild(c);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            for (int i = 0; i < menuDefinition.MenuItems.Length; i++)
            {
                MenuItem menuItem = menuDefinition.MenuItems[i];
                if (menuItem is MenuButton)
                {
                    MenuButton menuButton = menuItem as MenuButton;
                    Button button = Instantiate(MenuButtonPrefab, Items.transform);
                    button.transform.Find("TextContainer").GetComponentInChildren<Text>().text = menuButton.Text;
                    button.transform.Find("Value").GetComponent<Text>().text = menuButton.Value;
                    Action clickAction = menuButton.OnClick;
                    button.onClick.AddListener(() => SafeInvoke(clickAction));

                    if (menuButton.OnAdjust != null)
                    {
                        Action<int> adjustAction = menuButton.OnAdjust;
                        button.gameObject.AddComponent<MenuButtonAdjust>().Setup(delta => SafeInvoke(() => adjustAction(delta)));
                    }

                    if (selectedIndex == i)
                        EventSystem.current.SetSelectedGameObject(button.gameObject);
                }
                else if (menuItem is MenuBlank)
                {
                    Instantiate(BlankSeparatorPrefab, Items.transform);
                }
            }
        }
    }
}