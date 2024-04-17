using HHG.Common.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public abstract class SpawnManager : MonoBehaviour
    {
        public SpawnWavesAsset SpawnWaves => spawnWaves;

        [SerializeField] protected SpawnWavesAsset spawnWaves;
    }

    public abstract class SpawnManager<T> : SpawnManager where T : ISpawn
    {
        public IDataProxy<int> Wave { get; private set; }
        public IReadOnlyList<T> Spawns => spawns;

        private List<T> spawns = new List<T>();
        private int wave;
        private float timer;

        private enum Mode
        {
            Random,
            Cycle
        }

        protected abstract float GetFirstWaveDelay();
        protected abstract float GetNextWaveDelay();
        protected abstract float GetWaveDuration(int wave);

        protected virtual void OnSpawned(T[] spawns) { }
        protected virtual void OnDespawned(T spawn) { }
        protected virtual void OnDoneSpawning() { }

        protected virtual void Awake()
        {
            wave = 0;
            Wave = new DataProxy<int>(() => wave, v => wave = v);
            timer = GetWaveDuration(Wave.Value) - GetFirstWaveDelay();
        }

        protected virtual void Update()
        {
            if (Wave.Value < spawnWaves.WaveCount)
            {
                timer += Time.deltaTime;

                if (timer > GetWaveDuration(Wave.Value))
                {
                    timer = 0;

                    Spawn[] spawns = spawnWaves.GetSpawnsForWave(Wave.Value++);
                    foreach (Spawn spawn in spawns)
                    {
                        Spawn(spawn);
                    }
                }
            }
        }

        private void OnSpawnDie(IHealth health)
        {
            Despawn(health.Mono.GetComponent<T>());
            CheckIfDoneSpawning();
        }

        private void CheckIfDoneSpawning()
        {
            // Do initial check so don't create unnecessary coroutines
            // TODO: Won't work if spawner has a start delay, but we'll
            // worry about that scenario later on
            if (spawns.Count == 0 && Wave.Value == spawnWaves.WaveCount)
            {
                // Wait a frame in case killed spawned spawns chils spawns
                this.Invoker().NextFrame(_ =>
                {
                    if (spawns.Count == 0 && Wave.Value == spawnWaves.WaveCount)
                    {
                        OnDoneSpawning();
                    }
                });
            }      
        }

        private void Spawn(Spawn spawn)
        {
            if (spawn.Asset == null) return;

            // Spawns can get stuck if their target position is lined up with them
            // No idea why this happens, but offsetting it prevents this from happening
            Vector3 offset = new Vector3(.1f, .1f);
            GameObject instance = Instantiate(spawn.Asset.Prefab, spawn.Position + offset, Quaternion.identity, transform);
            // Use GetComponentsInChildren since spawns may contain child spawns
            T[] spawned = instance.GetComponentsInChildren<T>();
            foreach (T enemy in spawned)
            {
                enemy.Health.OnDied.AddListener(OnSpawnDie);
                spawns.Add(enemy);
            }

            Spawner[] spawners = instance.GetComponentsInChildren<Spawner>();
            foreach (Spawner spawner in spawners)
            {
                spawner.Initialize(Spawn);
            }

            OnSpawned(spawned);
        }

        private void Despawn(T enemy)
        {
            spawns.Remove(enemy);

            if (spawns.Count == 0)
            {
                timer = GetWaveDuration(Wave.Value) - GetNextWaveDelay();
            }

            OnDespawned(enemy);
        }

        protected virtual void OnDestroy()
        {

        }
    }
}