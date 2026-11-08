using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class CameraManager
    {
        private readonly Stack<FSMCamera> _cameraStack;
        private readonly GameObject _mainCameraObject;
        private bool _audioEnabled;

        public UnityEngine.Camera MainCamera
        {
            get { return _mainCameraObject != null ? _mainCameraObject.GetComponent<UnityEngine.Camera>() : null; }
        }

        public FSMCamera ActiveCamera
        {
            get
            {
                if (_cameraStack.Count > 0)
                {
                    return _cameraStack.Peek();
                }

                return null;
            }
        }

        private static CameraManager _instance;
        private static int _destroyedFrame = -1;

        public static CameraManager Instance
        {
            get
            {
                // The singleton outlives scene loads (Abort Mission, then a new trip), so rebuild it if its camera was destroyed.
                // Not in the frame Destroy() ran: the old camera is still findable then, and the next scene is about to replace it.
                bool tearingDown = Time.frameCount == _destroyedFrame;
                if (_instance == null || (_instance._mainCameraObject == null && !tearingDown))
                {
                    _instance = new CameraManager(tearingDown);
                }

                return _instance;
            }
        }

        public bool AudioEnabled
        {
            get { return _audioEnabled; }
            set
            {
                if (_audioEnabled == value)
                {
                    return;
                }

                _audioEnabled = value;
                AudioListener listener = null;
                if (_cameraStack.Count > 0)
                {
                    listener = _cameraStack.Peek().GetComponent<AudioListener>();
                }
                else if (MainCamera != null)
                {
                    listener = MainCamera.GetComponent<AudioListener>();
                }

                if (listener != null)
                {
                    listener.enabled = value;
                }
            }
        }

        public bool IsMainCameraActive
        {
            // MainCamera is a UnityEngine.Camera and ActiveCamera an FSMCamera, so comparing them was always false.
            get { return ActiveCamera != null && ActiveCamera.gameObject == _mainCameraObject; }
        }

        private CameraManager(bool tearingDown)
        {
            _cameraStack = new Stack<FSMCamera>();
            _audioEnabled = true;

            // With no camera this stays an empty manager: MainCamera and ActiveCamera are null and IsMainCameraActive is false.
            FSMCamera mainCamera = tearingDown ? null : FindMainFsmCamera();
            if (mainCamera == null)
            {
                return;
            }

            _mainCameraObject = mainCamera.gameObject;
            _cameraStack.Push(mainCamera);
        }

        private static FSMCamera FindMainFsmCamera()
        {
            // Only a camera tagged MainCamera counts. A cutscene "Stack Camera" also carries an FSMCamera, but is untagged.
            foreach (FSMCamera candidate in Object.FindObjectsByType<FSMCamera>(FindObjectsSortMode.None))
            {
                if (candidate.CompareTag("MainCamera"))
                {
                    return candidate;
                }
            }

            UnityEngine.Camera sceneCamera = UnityEngine.Camera.main;
            if (sceneCamera == null)
            {
                return null;
            }

            // Upstream scenes never got an FSMCamera on the main camera, so add one.
            FSMCamera fsmCamera = sceneCamera.GetComponent<FSMCamera>();
            return fsmCamera != null ? fsmCamera : sceneCamera.gameObject.AddComponent<FSMCamera>();
        }
        
        public void PushCamera()
        {
            if (_cameraStack.Count > 0)
            {
                var camera = _cameraStack.Peek();
                camera.gameObject.SetActive(false);
            }

            GameObject newCameraObject = new GameObject("Stack Camera " + _cameraStack.Count);
            newCameraObject.AddComponent<UnityEngine.Camera>();
            var newCamera = newCameraObject.AddComponent<FSMCamera>();
            newCameraObject.AddComponent<AudioListener>();
            _cameraStack.Push(newCamera);
        }

        public void PopCamera()
        {
            // The bottom of the stack is the main camera. A script popping more than it pushed must not destroy it.
            if (_cameraStack.Count <= 1)
            {
                return;
            }

            var stackCamera = _cameraStack.Pop();
            Object.Destroy(stackCamera.gameObject);

            // Bring back the camera underneath. This used to deactivate it, which left a black screen after every cutscene.
            var camera = _cameraStack.Peek();
            camera.gameObject.SetActive(true);
        }

        public void Destroy()
        {
            while (_cameraStack.Count > 0)
            {
                var stackCamera = _cameraStack.Pop();
                Object.Destroy(stackCamera.gameObject);
            }

            _instance = null;
            _destroyedFrame = Time.frameCount;
        }
    }
}
