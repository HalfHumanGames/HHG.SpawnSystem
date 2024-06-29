using HHG.Common.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public abstract class SpawnManager : MonoBehaviour
    {
        public SpawnWavesAsset SpawnWaves => spawnWaves;

        [SerializeField] protected SpawnWavesAsset spawnWaves;
    }

    public abstract class SpawnManager<T> : SpawnManager where T : Component, ISpawn
    {
        public IDataProxy<int> Wave { get; private set; }
        public IDataProxy<float> Timer { get; private set; }
        public IReadOnlyList<T> Spawns => allSpawns;

        private List<T> allSpawns = new List<T>();
        private List<T> newSpawns = new List<T>();
        private int wave = -1; // Waves start at 0
        private float timer;

        protected enum Mode
        {
            Random,
            Cycle
        }

        protected abstract float GetFirstWaveDelay();
        protected abstract float GetNextWaveDelay();
        protected abstract float GetWaveDuration(int wave);

        protected virtual void OnSpawn(IEnumerable<T> spawns) { }
        protected virtual void OnDespawn(T spawn) { }
        protected virtual void OnDoneSpawning() { }

        protected virtual void Awake()
        {
            Wave = new DataProxy<int>(() => wave, v => wave = v);
            Timer = new DataProxy<float>(() => timer, v => timer = v);
            Timer.Value = GetWaveDuration(Wave.Value) - GetFirstWaveDelay();
        }

        protected virtual void Update()
        {
            if (Wave.Value < spawnWaves.WaveCount)
            {
                Timer.Value += Time.deltaTime;

                if (Timer.Value > GetWaveDuration(Wave.Value))
                {
                    Wave.Value++;

                    Timer.Value = 0;

                    Spawn[] spawns = spawnWaves.GetSpawnsForWave(Wave.Value);
                    foreach (Spawn spawn in spawns)
                    {
                        Spawn(spawn);
                    }
                }
            }
        }

        protected abstract T GetSpawn(Spawn spawn);
        protected abstract void ReleaseSpawn(T spawn);

        private void OnSpawnDie(IHealth health)
        {
            Despawn(health.Mono.GetComponent<T>());
        }

        private void CheckIfDoneSpawning()
        {
            // Do initial check so don't create unnecessary coroutines
            // TODO: Won't work if spawner has a start delay, but we'll
            // worry about that scenario later on
            if (allSpawns.Count == 0 && Wave.Value == spawnWaves.WaveCount)
            {
                // Wait a frame in case killed spawned spawns chils spawns
                this.Invoker().NextFrame(_ =>
                {
                    if (allSpawns.Count == 0 && Wave.Value == spawnWaves.WaveCount)
                    {
                        OnDoneSpawning();
                    }
                });
            }      
        }

        protected void Spawn(Spawn spawn)
        {
            if (spawn.Asset != null)
            {
                newSpawns.Clear();
                foreach (Vector3 offset in spawn.Asset.GetSpawnOffsets())
                {
                    T instance = GetSpawn(spawn);
                    instance.transform.position = spawn.Position + offset;
                    newSpawns.Add(instance);
                }
                SetupSpawns(newSpawns);
            }
        }

        protected void SetupAllSpawns()
        {
            T[] spawns = gameObject.GetComponentsInChildren<T>();
            SetupSpawns(spawns);
        }

        protected void SetupSpawns(IEnumerable<T> spawns)
        {
            allSpawns.AddRange(spawns);

            foreach (T spawn in spawns)
            {
                spawn.Health.OnDied.AddListener(OnSpawnDie);

                foreach (Spawner spawner in spawn.GetComponentsInChildren<Spawner>())
                {
                    spawner.Initialize(Spawn);
                }
            }

            OnSpawn(spawns);
        }

        protected void Despawn(T spawn)
        {
            allSpawns.Remove(spawn);

            if (allSpawns.Count == 0)
            {
                Timer.Value = GetWaveDuration(Wave.Value) - GetNextWaveDelay();
            }

            OnDespawn(spawn);
            ReleaseSpawn(spawn);
            CheckIfDoneSpawning();
        }

        protected void DespawnAll()
        {
            while(allSpawns.Count > 0)
            {
                Despawn(allSpawns[0]);
            }
        }

        protected virtual void OnDestroy()
        {

        }
    }
}