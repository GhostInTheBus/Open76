using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Thin wrapper over UnityEngine.Input. Real input always works; tests can inject extra key presses.
    public static class DevInput
    {
        private static readonly HashSet<KeyCode> Down = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> Held = new HashSet<KeyCode>();

        public static bool Injecting;
        public static float InjectedVertical;
        public static float InjectedHorizontal;
        public static bool InjectedEBrake;

        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key) || (Injecting && Down.Contains(key));
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key) || (Injecting && Held.Contains(key));
        }

        public static float GetAxis(string axis)
        {
            float value = Input.GetAxis(axis);
            if (Injecting)
            {
                if (axis == "Vertical" && InjectedVertical != 0f) return InjectedVertical;
                if (axis == "Horizontal" && InjectedHorizontal != 0f) return InjectedHorizontal;
            }

            return value;
        }

        public static bool GetButton(string button)
        {
            return Input.GetButton(button) || (Injecting && button == "E-brake" && InjectedEBrake);
        }

        public static void PressForOneFrame(KeyCode key)
        {
            Down.Add(key);
        }

        public static void ClearPresses()
        {
            Down.Clear();
        }

        public static void SetHeld(KeyCode key, bool held)
        {
            if (held) Held.Add(key); else Held.Remove(key);
        }
    }
#else
    // Release builds: plain pass-through, no key injection.
    public static class DevInput
    {
        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public static float GetAxis(string axis)
        {
            return Input.GetAxis(axis);
        }

        public static bool GetButton(string button)
        {
            return Input.GetButton(button);
        }
    }
#endif
}
