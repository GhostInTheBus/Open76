using System.Collections;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    public class SceneRoot : MonoBehaviour
    {
        public string GamePath;
        public string MissionFile;
        public string VcfToLoad;

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            gameObject.AddComponent<SceneViewAudioHelper>();
#endif

            if (!string.IsNullOrEmpty(Game.Instance.LevelName))
            {
                MissionFile = Game.Instance.LevelName;
            }

            if (string.IsNullOrEmpty(Game.Instance.GamePath))
            {
                Game.Instance.GamePath = GamePath;
            }

            Debug.Log("[I76] SceneRoot start t=" + Time.realtimeSinceStartup.ToString("F1") + " mission=" + MissionFile + " gamePath=" + Game.Instance.GamePath);
            yield return LevelLoader.Instance.LoadLevel(MissionFile);
            Debug.Log("[I76] LoadLevel done t=" + Time.realtimeSinceStartup.ToString("F1"));
            
            if (MissionFile.ToLower().StartsWith("m"))
            {
                CacheManager cacheManager = CacheManager.Instance;
                GameObject importedVcf = cacheManager.ImportVcf(VcfToLoad, true, out Vdf unused);
                importedVcf.AddComponent<CarInput>();

                GameObject spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
                Vector3 spawnPos = spawnPoint.transform.position;
                Quaternion spawnRot = spawnPoint.transform.rotation;

                // The mission file's spawn height isn't always on the ground, so drop the car onto the terrain below it.
                Physics.SyncTransforms();
                if (Physics.Raycast(new Vector3(spawnPos.x, 1000f, spawnPos.z), Vector3.down, out RaycastHit groundHit, 2000f, LayerMask.GetMask("Terrain")))
                {
                    spawnPos.y = groundHit.point.y + 1.5f;
                }

                // Move the Rigidbody itself, otherwise its interpolated physics pose pulls the car back to the origin.
                importedVcf.transform.SetPositionAndRotation(spawnPos, spawnRot);
                Rigidbody carBody = importedVcf.GetComponent<Rigidbody>();
                if (carBody != null)
                {
                    carBody.position = spawnPos;
                    carBody.rotation = spawnRot;
                    carBody.linearVelocity = Vector3.zero;
                    carBody.angularVelocity = Vector3.zero;
                }

                Physics.SyncTransforms();
                Debug.Log("[I76] spawn=" + spawnPoint.transform.position + " car now=" + importedVcf.transform.position + " spawns=" + GameObject.FindGameObjectsWithTag("Spawn").Length + " t=" + Time.realtimeSinceStartup.ToString("F1"));

                CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>().Target = importedVcf.transform;
            }

            var musicAudioSource = GetComponent<AudioSource>();
            GameAudio.Register(musicAudioSource, AudioCategory.Music);
            AudioClip musicClip = null;
            yield return VirtualFilesystem.Instance.LoadMusicClip(Random.Range(2, 18), clip => musicClip = clip);
            if (musicClip != null)
            {
                DestroyMusicClip(musicAudioSource);
                musicAudioSource.clip = musicClip;
                musicAudioSource.Play();
            }
        }

        // The decoded track is tens of MB and nothing else owns it, so free it before the next one and when the level ends.
        private static void DestroyMusicClip(AudioSource source)
        {
            if (source != null && source.clip != null)
            {
                source.Stop();
                Destroy(source.clip);
                source.clip = null;
            }
        }

        private void OnDestroy()
        {
            DestroyMusicClip(GetComponent<AudioSource>());
        }
    }
}