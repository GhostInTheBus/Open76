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

        public static CameraManager Instance
        {
            get
            {
                // The singleton outlives scene loads (Abort Mission, then a new trip), so rebuild it if its camera was destroyed.
                if (_instance == null || _instance._mainCameraObject == null)
                {
                    _instance = new CameraManager();
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
                if (_cameraStack.Count > 0)
                {
                    var camera = _cameraStack.Peek();
                    camera.GetComponent<AudioListener>().enabled = value;
                }
                else
                {
                    MainCamera.GetComponent<AudioListener>().enabled = value;
                }
            }
        }

        public bool IsMainCameraActive
        {
            // MainCamera is a UnityEngine.Camera and ActiveCamera an FSMCamera, so comparing them was always false.
            get { return ActiveCamera != null && ActiveCamera.gameObject == _mainCameraObject; }
        }

        private CameraManager()
        {
            _cameraStack = new Stack<FSMCamera>();
            var mainCamera = Object.FindFirstObjectByType<FSMCamera>();
            if (mainCamera == null)
            {
                // Upstream scenes never got an FSMCamera on the main camera, so add one.
                mainCamera = UnityEngine.Camera.main.gameObject.AddComponent<FSMCamera>();
            }
            _mainCameraObject = mainCamera.gameObject;
            _cameraStack.Push(mainCamera);
            _audioEnabled = true;
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
        }
    }
}
