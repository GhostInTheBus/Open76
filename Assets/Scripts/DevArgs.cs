using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Command line switches for testing a build without the menus:
    //   -i76mission m01.msn    jump straight into a mission
    //   -i76screenshot out.png save a screenshot after the level settles, then quit
    //   -i76delay 20           seconds to wait before the screenshot (default 20)
    //   -i76mute               silence all audio
    public class DevArgs : MonoBehaviour
    {
        // Set by -i76drive: starts the engine and holds full throttle (for automated driving tests).
        public static bool Drive;
        private static bool _menuTest;
        private static bool _abortTest;
        private static string _shotPath;
        private static float _chaosSeconds;
        private static bool _deathCam;
        private static volatile int _lastFrame;
        private static float _lastFrameRealtime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string[] args = Environment.GetCommandLineArgs();
            string mission = null;
            string shot = null;
            float delay = 20f;
            bool mute = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i76mute") mute = true;
                else if (args[i] == "-i76drive") Drive = true;
                else if (args[i] == "-i76menutest") _menuTest = true;
                else if (args[i] == "-i76aborttest") _abortTest = true;
                else if (args[i] == "-i76deathcam") _deathCam = true;
                else if (i < args.Length - 1 && args[i] == "-i76chaos") float.TryParse(args[i + 1], out _chaosSeconds);
                else if (i < args.Length - 1 && args[i] == "-i76mission") mission = args[i + 1];
                else if (i < args.Length - 1 && args[i] == "-i76screenshot") shot = args[i + 1];
                else if (i < args.Length - 1 && args[i] == "-i76delay") float.TryParse(args[i + 1], out delay);
            }

            if (mission == null && shot == null && !mute && !Drive && !_menuTest && !_abortTest && _chaosSeconds <= 0f && !_deathCam)
            {
                return;
            }

            // Keep ticking even when the window is hidden or unfocused, so unattended test runs don't stall.
            Application.runInBackground = true;

            _shotPath = shot;
            GameObject go = new GameObject("DevArgs");
            DontDestroyOnLoad(go);
            go.AddComponent<DevArgs>().StartCoroutine(Run(mission, shot, delay, mute));
        }

        private static IEnumerator Run(string mission, string shot, float delay, bool mute)
        {
            if (mute)
            {
                AudioListener.volume = 0f;
            }

            if (mission != null)
            {
                Game.Instance.LevelName = mission;
                if (SceneManager.GetActiveScene().name != "Level")
                {
                    SceneManager.LoadScene(1);
                }
            }

            if (_menuTest)
            {
                yield return MenuTest();
            }

            if (_abortTest)
            {
                yield return AbortTest();
            }

            if (_deathCam)
            {
                yield return DeathCam();
            }

            if (_chaosSeconds > 0f)
            {
                yield return Chaos(_chaosSeconds);
                if (shot == null)
                {
                    Application.Quit();
                }
            }

            if (shot != null)
            {
                float end = Time.realtimeSinceStartup + delay;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    foreach (Assets.Scripts.Entities.Car ac in FindObjectsByType<Assets.Scripts.Entities.Car>(FindObjectsSortMode.None))
                    {
                        Rigidbody arb = ac.GetComponent<Rigidbody>();
                        Debug.Log("[Cars t=" + Time.realtimeSinceStartup.ToString("F1") + "] " + ac.name + " parent=" + (ac.transform.parent != null ? ac.transform.parent.name : "-") +
                                  " input=" + (ac.GetComponent<Assets.Scripts.CarSystems.CarInput>() != null) + " pos=" + ac.transform.position +
                                  (arb != null ? " grav=" + arb.useGravity + " sleep=" + arb.IsSleeping() + " kin=" + arb.isKinematic + " vel=" + arb.linearVelocity : " norb") + " active=" + ac.gameObject.activeInHierarchy);
                    }
                    Assets.Scripts.CarSystems.CarInput ci = FindFirstObjectByType<Assets.Scripts.CarSystems.CarInput>();
                    Assets.Scripts.Entities.Car tc = ci != null ? ci.GetComponent<Assets.Scripts.Entities.Car>() : null;
                    if (tc != null)
                    {
                        Rigidbody trb = tc.GetComponent<Rigidbody>();
                        int grounded = 0;
                        foreach (Assets.Scripts.CarSystems.RaySusp rs in tc.GetComponentsInChildren<Assets.Scripts.CarSystems.RaySusp>()) if (rs.Grounded) grounded++;
                        Debug.Log("[Trace t=" + Time.realtimeSinceStartup.ToString("F1") + "] y=" + tc.transform.position.y.ToString("F1") + " pos=" + tc.transform.position + " vel=" + (trb != null ? trb.linearVelocity.ToString() : "-") + " grounded=" + grounded + " engine=" + tc.EngineRunning + " alive=" + tc.Alive + " mainCamActive=" + Assets.Scripts.Camera.CameraManager.Instance.IsMainCameraActive + " throttle=" + tc.GetComponent<Assets.Scripts.CarSystems.CarPhysics>().Throttle + " speed=" + (trb != null ? trb.linearVelocity.magnitude.ToString("F1") : "-") + " groundRayY=" + GroundY(tc.transform.position));
                    }
                }
                Dump();
                ScreenCapture.CaptureScreenshot(shot);
                yield return new WaitForSecondsRealtime(3f);
                Application.Quit();
            }
        }

        // Mission -> Esc menu -> Abort Mission -> main menu -> start another mission.
        private static IEnumerator AbortTest()
        {
            yield return new WaitForSecondsRealtime(8f);
            Assets.Scripts.Menus.MenuController menu = FindFirstObjectByType<Assets.Scripts.Menus.MenuController>();
            if (menu == null)
            {
                Debug.Log("[AbortTest] no MenuController found");
                yield break;
            }

            menu.ShowMenu<Assets.Scripts.Menus.OptionsMenu>();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return PressMenuButton(menu, "Abort Mission", false);
            yield return new WaitForSecondsRealtime(4f);
            Debug.Log("[AbortTest] after abort: scene=" + SceneManager.GetActiveScene().name + " timeScale=" + Time.timeScale);
            if (_shotPath != null)
            {
                ScreenCapture.CaptureScreenshot(_shotPath + ".mainmenu.png");
                yield return new WaitForSecondsRealtime(1.5f);
            }

            Game.Instance.LevelName = "m02.msn";
            SceneManager.LoadScene(1);
            yield return new WaitForSecondsRealtime(12f);
            Debug.Log("[AbortTest] second mission: scene=" + SceneManager.GetActiveScene().name + " cars=" + FindObjectsByType<Assets.Scripts.Entities.Car>(FindObjectsSortMode.None).Length);
        }

        // Opens the in-game menus and presses every button (left, right and arrow adjust), logging anything that fails.
        private static IEnumerator MenuTest()
        {
            yield return new WaitForSecondsRealtime(6f);
            Assets.Scripts.Menus.MenuController menu = FindFirstObjectByType<Assets.Scripts.Menus.MenuController>();
            if (menu == null)
            {
                Debug.Log("[MenuTest] no MenuController found");
                yield break;
            }

            Debug.Log("[MenuTest] start, frame=" + Time.frameCount + " music=" + GameAudio.GetLevel(AudioCategory.Music) + " sfx=" + GameAudio.GetLevel(AudioCategory.Sfx) + " voice=" + GameAudio.GetLevel(AudioCategory.Voice));
            menu.ShowMenu<Assets.Scripts.Menus.OptionsMenu>();
            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log("[MenuTest] options menu open, timeScale=" + Time.timeScale);

            menu.ShowMenu<Assets.Scripts.Menus.AudioControlMenu>();
            yield return new WaitForSecondsRealtime(0.5f);
            for (int round = 0; round < 3; round++)
            {
                for (int idx = 0; idx < 3; idx++)
                {
                    yield return PressMenuButton(menu, new[] { "Music Level", "SFX Level", "Voice Level" }[idx], round == 2);
                }
            }

            Debug.Log("[MenuTest] audio clicked, music=" + GameAudio.GetLevel(AudioCategory.Music) + " sfx=" + GameAudio.GetLevel(AudioCategory.Sfx) + " voice=" + GameAudio.GetLevel(AudioCategory.Voice));
            if (_shotPath != null)
            {
                ScreenCapture.CaptureScreenshot(_shotPath + ".audiomenu.png");
                yield return new WaitForSecondsRealtime(1.5f);
            }

            GameAudio.SetLevel(AudioCategory.Music, 3);
            GameAudio.SetLevel(AudioCategory.Sfx, 10);
            GameAudio.SetLevel(AudioCategory.Voice, 10);
            AudioSource musicSrc = null;
            foreach (AudioSource src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (src.clip != null && src.clip.name.Contains("music")) musicSrc = src;
            }
            Debug.Log("[MenuTest] music source volume at level 3 = " + (musicSrc != null ? musicSrc.volume.ToString("F2") : "n/a"));

            menu.ShowMenu<Assets.Scripts.Menus.GraphicsMenu>();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return PressMenuButton(menu, "Quality", false);
            yield return PressMenuButton(menu, "Virtual Reality", false);

            menu.CloseMenu();
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log("[MenuTest] done, frame=" + Time.frameCount + " timeScale=" + Time.timeScale);
        }

        private static readonly KeyCode[] TapKeys =
        {
            KeyCode.Z, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.Return, KeyCode.S,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.Space
        };

        private static readonly KeyCode[] HoldKeys =
        {
            KeyCode.Space, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Keypad2, KeyCode.Keypad4, KeyCode.Keypad6, KeyCode.Keypad8
        };

        // Random but repeatable key mashing: driving, weapons, cameras, radar, menus.
        private static IEnumerator Chaos(float seconds)
        {
            yield return new WaitForSecondsRealtime(4f);
            UnityEngine.Random.InitState(1234);
            DevInput.Injecting = true;

            _lastFrame = Time.frameCount;
            _lastFrameRealtime = Time.realtimeSinceStartup;
            global::System.Threading.Timer watchdog = new global::System.Threading.Timer(state =>
            {
                if (_lastFrame == lastSeenFrame)
                {
                    Debug.Log("[Chaos] FREEZE DETECTED: no new frame for 8 s at frame " + _lastFrame);
                }

                lastSeenFrame = _lastFrame;
            }, null, 8000, 8000);

            Assets.Scripts.Menus.MenuController menu = FindFirstObjectByType<Assets.Scripts.Menus.MenuController>();
            float end = Time.realtimeSinceStartup + seconds;
            float nextLog = Time.realtimeSinceStartup + 10f;
            int actions = 0;
            int menuActions = 0;
            int startFrame = Time.frameCount;
            while (Time.realtimeSinceStartup < end)
            {
                _lastFrame = Time.frameCount;
                DevInput.ClearPresses();
                float r = UnityEngine.Random.value;
                bool menuOpen = Time.timeScale == 0f && menu != null;
                if (menuOpen)
                {
                    menuActions++;
                    if (r < 0.75f)
                    {
                        UnityEngine.UI.Button[] buttons = menu.GetComponentsInChildren<UnityEngine.UI.Button>();
                        if (buttons.Length > 0)
                        {
                            UnityEngine.UI.Button b = buttons[UnityEngine.Random.Range(0, buttons.Length)];
                            if (b != null)
                            {
                                Debug.Log("[Chaos] menu button " + b.name + " idx=" + b.transform.GetSiblingIndex());
                                b.onClick.Invoke();
                                Assets.Scripts.Menus.MenuButtonAdjust adj = b != null ? b.GetComponent<Assets.Scripts.Menus.MenuButtonAdjust>() : null;
                                if (adj != null && UnityEngine.Random.value < 0.5f) adj.Invoke(UnityEngine.Random.value < 0.5f ? -1 : 1);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[Chaos] menu key " + (r < 0.9f ? "Escape" : "Left"));
                        DevInput.PressForOneFrame(r < 0.9f ? KeyCode.Escape : KeyCode.LeftArrow);
                    }
                }
                else if (r < 0.35f)
                {
                    float[] v = { 1f, 1f, 1f, 0f, -1f };
                    DevInput.InjectedVertical = v[UnityEngine.Random.Range(0, v.Length)];
                    DevInput.InjectedHorizontal = UnityEngine.Random.Range(-1f, 1f);
                    DevInput.InjectedEBrake = UnityEngine.Random.value < 0.15f;
                }
                else if (r < 0.65f)
                {
                    KeyCode tap = TapKeys[UnityEngine.Random.Range(0, TapKeys.Length)];
                    Debug.Log("[Chaos] tap " + tap);
                    DevInput.PressForOneFrame(tap);
                }
                else if (r < 0.85f)
                {
                    KeyCode k = HoldKeys[UnityEngine.Random.Range(0, HoldKeys.Length)];
                    bool on = UnityEngine.Random.value < 0.5f;
                    Debug.Log("[Chaos] hold " + k + "=" + on);
                    DevInput.SetHeld(k, on);
                }
                else if (r < 0.95f)
                {
                    Debug.Log("[Chaos] tap Escape");
                    DevInput.PressForOneFrame(KeyCode.Escape);
                }
                else if (r < 0.96f)
                {
                    Debug.Log("[Chaos] tap K (kill)");
                    DevInput.PressForOneFrame(KeyCode.K);
                }

                actions++;
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.08f, 0.25f));
                DevInput.ClearPresses();

                if (Time.realtimeSinceStartup > nextLog)
                {
                    nextLog = Time.realtimeSinceStartup + 10f;
                    Debug.Log("[Chaos] t=" + Time.realtimeSinceStartup.ToString("F0") + " actions=" + actions + " menuActions=" + menuActions + " frames=" + (Time.frameCount - startFrame) + " timeScale=" + Time.timeScale);
                }
            }

            watchdog.Dispose();
            DevInput.Injecting = false;
            DevInput.InjectedVertical = 0f;
            DevInput.InjectedHorizontal = 0f;
            DevInput.InjectedEBrake = false;
            Debug.Log("[Chaos] done actions=" + actions + " menuActions=" + menuActions + " frames=" + (Time.frameCount - startFrame) + " timeScale=" + Time.timeScale);
        }

        // Wheel camera (F7), then kill the car. The camera used to be destroyed with the wheel.
        private static IEnumerator DeathCam()
        {
            yield return new WaitForSecondsRealtime(4f);
            DevInput.Injecting = true;
            DevInput.PressForOneFrame(KeyCode.F7);
            yield return new WaitForSecondsRealtime(1f);
            DevInput.ClearPresses();
            Debug.Log("[DeathCam] wheel camera on, parent=" + (UnityEngine.Camera.main != null && UnityEngine.Camera.main.transform.parent != null ? UnityEngine.Camera.main.transform.parent.name : "none"));
            DevInput.PressForOneFrame(KeyCode.K);
            yield return new WaitForSecondsRealtime(1f);
            DevInput.ClearPresses();
            yield return new WaitForSecondsRealtime(2f);
            Debug.Log("[DeathCam] after kill, main camera alive=" + (UnityEngine.Camera.main != null));
            DevInput.Injecting = false;
        }

        private static int lastSeenFrame = -1;

        private static IEnumerator PressMenuButton(Assets.Scripts.Menus.MenuController menu, string label, bool alsoAdjustDown)
        {
            foreach (UnityEngine.UI.Button b in menu.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                Transform container = b != null ? b.transform.Find("TextContainer") : null;
                UnityEngine.UI.Text text = container != null ? container.GetComponentInChildren<UnityEngine.UI.Text>() : null;
                if (text == null || text.text != label)
                {
                    continue;
                }

                b.onClick.Invoke();
                Debug.Log("[MenuTest] pressed " + label);
                if (alsoAdjustDown)
                {
                    Assets.Scripts.Menus.MenuButtonAdjust adj = b.GetComponent<Assets.Scripts.Menus.MenuButtonAdjust>();
                    if (adj != null)
                    {
                        adj.Invoke(-1);
                    }
                }

                break;
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }

        private static string GroundY(Vector3 p)
        {
            RaycastHit h;
            if (Physics.Raycast(new Vector3(p.x, p.y + 2000f, p.z), Vector3.down, out h, 6000f, LayerMask.GetMask("Terrain")))
            {
                return h.point.y.ToString("F1") + "(" + h.collider.name + ")";
            }

            return "none";
        }

        private static void Dump()
        {
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            Debug.Log("[DevArgs] terrains=" + terrains.Length);
            for (int i = 0; i < terrains.Length && i < 3; i++)
            {
                Terrain t = terrains[i];
                Debug.Log("[DevArgs] terrain " + t.name + " pos=" + t.transform.position + " enabled=" + t.enabled +
                          " layer=" + t.gameObject.layer + " size=" + t.terrainData.size +
                          " hmRes=" + t.terrainData.heightmapResolution + " mat=" + (t.materialTemplate != null ? t.materialTemplate.shader.name : "null") +
                          " h@mid=" + t.terrainData.GetInterpolatedHeight(0.5f, 0.5f) + " collider=" + (t.GetComponent<TerrainCollider>() != null));
            }
            foreach (Assets.Scripts.Entities.Car c in FindObjectsByType<Assets.Scripts.Entities.Car>(FindObjectsSortMode.None))
            {
                Rigidbody rb = c.GetComponent<Rigidbody>();
                Debug.Log("[DevArgs] car " + c.name + " player=" + c.IsPlayer + " pos=" + c.transform.position + " layer=" + c.gameObject.layer +
                          (rb != null ? " rb(mass=" + rb.mass + ",kin=" + rb.isKinematic + ",vel=" + rb.linearVelocity + ")" : " rb=none") +
                          " ignoreVsTerrain=" + Physics.GetIgnoreLayerCollision(c.gameObject.layer, 10));
                foreach (Collider col in c.GetComponentsInChildren<Collider>())
                {
                    Debug.Log("[DevArgs]   collider " + col.GetType().Name + " " + col.name + " layer=" + col.gameObject.layer + " enabled=" + col.enabled + " trigger=" + col.isTrigger);
                }
            }
            GameObject sp = GameObject.FindWithTag("Spawn");
            if (sp != null)
            {
                RaycastHit sh;
                bool sHit = Physics.Raycast(new Vector3(sp.transform.position.x, 2000f, sp.transform.position.z), Vector3.down, out sh, 4000f);
                Debug.Log("[DevArgs] ray over spawn hit=" + sHit + (sHit ? " " + sh.collider.name + " y=" + sh.point.y : ""));
            }
            if (terrains.Length > 0)
            {
                Terrain t0 = terrains[0];
                Vector3 c = t0.transform.position + new Vector3(320f, 0f, 320f);
                RaycastHit hit;
                bool any = Physics.Raycast(c + Vector3.up * 1000f, Vector3.down, out hit, 3000f);
                Debug.Log("[DevArgs] ray down over " + t0.name + " hit=" + any + (any ? " collider=" + hit.collider.name + " y=" + hit.point.y : ""));
                bool up = Physics.Raycast(c - Vector3.up * 500f, Vector3.up, out hit, 3000f);
                Debug.Log("[DevArgs] ray up from below hit=" + up + (up ? " collider=" + hit.collider.name + " y=" + hit.point.y : ""));
                TerrainCollider tc = t0.GetComponent<TerrainCollider>();
                if (tc != null)
                {
                    Debug.Log("[DevArgs] tc enabled=" + tc.enabled + " bounds=" + tc.bounds + " dataMatch=" + (tc.terrainData == t0.terrainData));
                }
            }
            Debug.Log("[DevArgs] gravity=" + Physics.gravity + " timeScale=" + Time.timeScale + " simMode=" + Physics.simulationMode + " autoSync=" + Physics.autoSyncTransforms);
            GameObject spawn = GameObject.FindWithTag("Spawn");
            Debug.Log("[DevArgs] spawn=" + (spawn != null ? spawn.transform.position.ToString() : "none"));
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            Debug.Log("[DevArgs] cam pos=" + (cam != null ? cam.transform.position.ToString() : "none") + " far=" + (cam != null ? cam.farClipPlane : 0));
        }
    }
#else
    // Release builds ignore every -i76 switch: no test harness, no background running, no chaos timer.
    public static class DevArgs
    {
        public static readonly bool Drive = false;
    }
#endif
}
