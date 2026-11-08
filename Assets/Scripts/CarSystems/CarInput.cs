using Assets.Scripts.Camera;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.CarSystems
{
    internal class CarInput : MonoBehaviour
    {
        private Car _car;
        private CarPhysics _carPhysics;
        private bool _devStartedEngine;

        private void Start()
        {
            _car = GetComponent<Car>();
            _carPhysics = GetComponent<CarPhysics>();
        }

        private void Update()
        {
            if (!CameraManager.Instance.IsMainCameraActive || !_car.Alive)
            {
                return;
            }
            
            // Kill player.
            if (DevInput.GetKeyDown(KeyCode.K))
            {
                _car.Kill();
                return; // Killing the car tears down its weapons and radar, so stop reading input this frame.
            }

            // Debug: Toggle all AI cars to fire.
            if (DevInput.GetKeyDown(KeyCode.Z))
            {
                Car.FireWeapons = !Car.FireWeapons;
            }

            // Weapons, specials and radar are gone after a kill; driving below must still work without them.
            if (_car.WeaponsController != null && _car.SpecialsController != null && _car.RadarPanel != null)
            {
                // Cycle radar target.
                if (DevInput.GetKeyDown(KeyCode.E))
                {
                    _car.RadarPanel.CycleTarget();
                }

                // Toggle radar range.
                if (DevInput.GetKeyDown(KeyCode.R))
                {
                    _car.RadarPanel.ToggleRange();
                }

                // Target nearest enemy.
                if (DevInput.GetKeyDown(KeyCode.T))
                {
                    _car.RadarPanel.TargetNearest();
                }

                // Clear radar target.
                if (DevInput.GetKeyDown(KeyCode.Y))
                {
                    _car.RadarPanel.ClearTarget();
                }

                // Cycle weapon.
                if (DevInput.GetKeyDown(KeyCode.Return))
                {
                    _car.WeaponsController.CycleWeapon();
                }

                // Fire active weapon(s).
                if (DevInput.GetKey(KeyCode.Space))
                {
                    _car.WeaponsController.Fire(-1);
                }
                else if (DevInput.GetKey(KeyCode.Alpha1)) // Fire weapon 1.
                {
                    _car.WeaponsController.Fire(0);
                }
                else if (DevInput.GetKey(KeyCode.Alpha2)) // Fire weapon 2.
                {
                    _car.WeaponsController.Fire(1);
                }
                else if (DevInput.GetKey(KeyCode.Alpha3)) // Fire weapon 3.
                {
                    _car.WeaponsController.Fire(2);
                }
                else if (DevInput.GetKey(KeyCode.Alpha4)) // Fire weapon 4.
                {
                    _car.WeaponsController.Fire(3);
                }
                else if (DevInput.GetKey(KeyCode.Alpha5)) // Fire weapon 5.
                {
                    _car.WeaponsController.Fire(4);
                }
                else if (DevInput.GetKey(KeyCode.Alpha6)) // Fire special 1.
                {
                    _car.SpecialsController.Fire(0);
                }
                else
                {
                    _car.WeaponsController.StopFiring();
                }

                // Fire special 2.
                if (DevInput.GetKey(KeyCode.Alpha7))
                {
                    _car.SpecialsController.Fire(1);
                }

                // Fire special 3.
                if (DevInput.GetKey(KeyCode.Alpha8))
                {
                    _car.SpecialsController.Fire(2);
                }
            }


            // Start / Stop engine.
            if (DevInput.GetKeyDown(KeyCode.S))
            {
                _car.ToggleEngine();
            }

            float throttle = DevInput.GetAxis("Vertical");
            float brake = -Mathf.Min(0, throttle);
            throttle = Mathf.Max(0, throttle);

            if (DevArgs.Drive)
            {
                // Automated test: start the engine once, then hold full throttle.
                if (!_devStartedEngine)
                {
                    _devStartedEngine = true;
                    if (!_car.EngineRunning)
                    {
                        _car.ToggleEngine();
                    }
                }

                throttle = 1f;
                brake = 0f;
            }

            if (!_car.EngineRunning)
            {
                throttle = 0f;
            }

            _carPhysics.Throttle = throttle;
            _carPhysics.Brake = brake;

            float steering = DevInput.GetAxis("Horizontal");
            _carPhysics.Steer = steering;

            _carPhysics.EBrake = DevInput.GetButton("E-brake");
            // _car.RearSlip = ebrake;

        }
    }

}