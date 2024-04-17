using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HHG.SpawnSystem.Runtime
{
    [Serializable]
    public class SpawnerWaves
    {
        private const int loopForever = -1;

        [SerializeField] private int loopCount;
        [SerializeField] private List<SpawnerWave> waves = new List<SpawnerWave>();

        private Action<Spawn> create;

        public UnityEvent OnDone;

        public SpawnerWaves(IEnumerable<SpawnerWave> spawnWaves, int loop = 0)
        {
            loopCount = loop;
            waves = new List<SpawnerWave>(spawnWaves);
        }

        public void Initialize(Action<Spawn> createSpawn)
        {
            create = createSpawn;
        }

        public IEnumerator SpawnAsync(Transform transform)
        {
            int loop = loopCount;
            do
            {
                for (int w = 0; w < waves.Count; w++)
                {
                    yield return new WaitForSeconds(waves[w].Delay);
                    for (int s = 0; s < waves[w].Count; s++)
                    {
                        Spawn spawn = new Spawn(waves[w].Spawn, transform.position);
                        create(spawn);
                        yield return new WaitForSeconds(waves[w].Frequency);
                    }
                }
            } while (loop == loopForever || --loop >= 0);

            OnDone?.Invoke();
        }
    }
}