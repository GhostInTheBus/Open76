using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    public class Building : WorldEntity
    {
        private int _health;
        private Sdf _sdf;
        private GameObject _wreckedObject;

        public override bool Alive
        {
            // TODO: implement damage etc.
            get { return _health > 0; }
        }

        public void Initialise(Sdf sdf, GameObject wreckedObject)
        {
            _sdf = sdf;
            _health = (int)sdf.Health;
            _wreckedObject = wreckedObject;
        }

        public float HealthPercent
        {
            get { return _sdf != null && _sdf.Health > 0 ? Mathf.Clamp(100f * _health / _sdf.Health, 0f, 100f) : 100f; }
        }

        // Used by the mission script action destroy.
        public void Kill()
        {
            if (Alive)
            {
                ApplyDamage(DamageType.Force, Vector3.up, _health);
            }
        }

        public override void ApplyDamage(DamageType damageType, Vector3 hitNormal, int damage)
        {
            bool alive = Alive;
            _health -= damage;

            if (alive && _health <= 0)
            {
                if (_sdf.Xdf != null)
                {
                    // TODO: Perform effect.
                }

                if (!string.IsNullOrEmpty(_sdf.DestroySoundName))
                {
                    AudioSource source = CacheManager.Instance.GetAudioSource(gameObject, _sdf.DestroySoundName);
                    if (source != null)
                    {
                        source.Play();
                    }
                }

                if (_wreckedObject != null)
                {
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(false);
                    }

                    _wreckedObject.SetActive(true);
                }
            }
        }
    }
}
