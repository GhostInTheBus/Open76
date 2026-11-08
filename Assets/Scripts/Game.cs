#if !UNITY_EDITOR
using System.IO;
using UnityEngine;
#endif

namespace Assets.Scripts
{
    public class Game
    {
        private static Game _instance;

        public static Game Instance
        {
            get { return _instance ?? (_instance = new Game()); }
        }

        public string LevelName { get; set; }
        public string GamePath { get; set; }
        public bool IntroPlayed { get; set; }

        private Game()
        {
            // Lets you point the game at the extracted data without editing scenes.
            string envPath = global::System.Environment.GetEnvironmentVariable("I76_GAME_PATH");
            if (!string.IsNullOrEmpty(envPath) && global::System.IO.Directory.Exists(envPath))
            {
                GamePath = envPath;
                return;
            }
#if !UNITY_EDITOR
            string gameExeDir = Path.Combine(Application.dataPath, "../..");
            if (File.Exists(Path.Combine(gameExeDir, "i76.exe")))
            {
                GamePath = gameExeDir;
            }
            else
            {
                Debug.LogError("Game path not found.");
            }
#endif
        }
    }
}
