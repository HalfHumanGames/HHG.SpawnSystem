using HHG.Common.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public abstract class SpawnManager : MonoBehaviour
    {
        public SpawnWavesAsset SpawnWaves => spawnWaves;

        [SerializeField] protected SpawnWavesAsset spawnWaves;
    }

    public abstract class SpawnManager<TSpawn> : SpawnManager where TSpawn : Component, ISpawn
    {
        private const int poolDefaultCapacity = 500;
        private const int poolMaxSize = 10000;

        public IDataProxy<int> Wave { get; private set; }
        public IDataProxy<float> Timer { get; private set; }
        public IReadOnlyList<TSpawn> Spawns => allSpawns;

        private GameObjectPool<TSpawn> pool;
        private List<TSpawn> allSpawns = new List<TSpawn>();
        private List<TSpawn> newSpawns = new List<TSpawn>();
        private List<TSpawn> releaseQueue = new List<TSpawn>();
        private Coroutine releaseCoroutine;
        private int wave = -1; // Waves start at 0
        private float timer;

        protected enum Mode
        {
            Random,
            Cycle
        }

        protected abstract TSpawn GetPrefabTemplate();
        protected abstract float GetFirstWaveDelay();
        protected abstract float GetNextWaveDelay();
        protected abstract float GetWaveDuration(int wave);

        protected virtual void OnSpawn(IEnumerable<TSpawn> spawns) { }
        protected virtual void OnDespawn(TSpawn spawn) { }
        protected virtual void OnDoneSpawning() { }

        protected virtual void Awake()
        {
            pool = new GameObjectPool<TSpawn>(GetPrefabTemplate(), transform, false, poolDefaultCapacity, poolMaxSize);
            Wave = new DataProxy<int>(() => wave, v => wave = v);
            Timer = new DataProxy<float>(() => timer, v => timer = v);
            Timer.Value = GetWaveDuration(Wave.Value) - GetFirstWaveDelay();
        }

        protected virtual void OnEnable()
        {
            releaseCoroutine = StartCoroutine(ReleaseAsync());
        }

        protected virtual void OnDisable()
        {
            StopCoroutine(releaseCoroutine);
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
                    
                    if (spawns.Length > 0)
                    {
                        foreach (Spawn spawn in spawns)
                        {
                            Spawn(spawn);
                        }
                    }
                    else
                    {
                        CheckIfDoneSpawning();

                    }
                }
            }
        }

        private IEnumerator ReleaseAsync()
        {
            while (true)
            {
                yield return WaitFor.EndOfFrame;

                foreach (TSpawn toRelease in releaseQueue)
                {
                    pool.Release(toRelease);
                }

                releaseQueue.Clear();
            }
        }

        private void CheckIfDoneSpawning()
        {
            // Do initial check so don't create unnecessary coroutines
            // TODO: Won't work if spawner has a start delay, but we'll
            // worry about that scenario later on
            if (allSpawns.Count == 0 && Wave.Value == spawnWaves.WaveCount)
            {
                // Wait a frame in case killed spawned spawns childs spawns
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
                    TSpawn instance = pool.Get();
                    instance.transform.position = spawn.Position + offset;
                    instance.Initialize(spawn.Asset); // Initialize after set position
                    instance.gameObject.SetActive(true); // Set active after initialize
                    newSpawns.Add(instance);
                }
                SetupSpawns(newSpawns);
            }
        }

        protected void SetupAllSpawns()
        {
            TSpawn[] spawns = gameObject.GetComponentsInChildren<TSpawn>();
            SetupSpawns(spawns);
        }

        protected void SetupSpawns(IEnumerable<TSpawn> spawns)
        {
            allSpawns.AddRange(spawns);

            foreach (TSpawn spawn in spawns)
            {
                spawn.SubscribeToDespawnEvent(Despawn);

                foreach (Spawner spawner in spawn.GetComponentsInChildren<Spawner>())
                {
                    spawner.Initialize(Spawn);
                }
            }

            OnSpawn(spawns);
        }

        protected void Despawn(ISpawn spawn)
        {
            Despawn((TSpawn)spawn);
        }

        protected void Despawn(TSpawn spawn)
        {
            spawn.gameObject.SetActive(false);
            spawn.UnsubscribeFromDespawnEvent(Despawn);

            // Components often don't get destroyed until the
            // at the end of the update loop, so we release next
            // frame to prevent any weird issues from occuring
            releaseQueue.Add(spawn);
            allSpawns.Remove(spawn);
            OnDespawn(spawn);

            if (allSpawns.Count == 0)
            {
                Timer.Value = GetWaveDuration(Wave.Value) - GetNextWaveDelay();
            }

            CheckIfDoneSpawning();
        }

        protected void DespawnAll()
        {
            while (allSpawns.Count > 0)
            {
                Despawn(allSpawns[0]);
            }
        }

        protected virtual void OnDestroy()
        {

        }
    }
}