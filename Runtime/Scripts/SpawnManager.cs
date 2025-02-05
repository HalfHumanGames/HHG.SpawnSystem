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
        public IReadOnlyList<TSpawn> Spawns => allSpawns;
        public IDataProxy<int> Wave { get; private set; }
        public IDataProxy<float> Timer { get; private set; }
        public bool IsDone => isDone;

        private GameObjectPool<TSpawn> pool;
        private List<TSpawn> allSpawns = new List<TSpawn>();
        private List<TSpawn> newSpawns = new List<TSpawn>();
        private Queue<Spawn> spawnQueue = new Queue<Spawn>();
        private int wave = -1; // Waves start at 0
        private int spawnBatchCount;
        private float timer;
        private bool isDone;
        private bool canContinueSpawning => spawnBatchCount < settings.SpawnBatchSize;
        private SpawnManagerSettings settings;

        protected enum Mode
        {
            Random,
            Cycle
        }

        protected abstract int GetEnemyThresholdToAutoStartNextWave();
        protected abstract TSpawn GetPrefabTemplate();
        protected abstract float GetFirstWaveDelay();
        protected abstract float GetNextWaveDelay();
        protected abstract float GetWaveDuration(int wave);
        protected abstract SpawnManagerSettings GetSettings();

        protected virtual void OnSpawn(IEnumerable<TSpawn> spawns) { }
        protected virtual void OnDespawn(TSpawn spawn) { }
        protected virtual void OnDoneSpawning() { }

        protected virtual void Awake()
        {
            settings = GetSettings();
            pool = new GameObjectPool<TSpawn>(GetPrefabTemplate(), transform, Debug.isDebugBuild, settings.PoolDefaultCapacity, settings.PoolMaxSize, settings.PoolPrewarm);
            Wave = new DataProxy<int>(() => wave, v => wave = v);
            Timer = new DataProxy<float>(() => timer, v => timer = v);
            Timer.Value = GetWaveDuration(Wave.Value) - GetFirstWaveDelay();
            StartCoroutine(UpdateRoutine());
        }

        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void Update()
        {

        }

        protected virtual IEnumerator UpdateRoutine()
        {
            while (true)
            {
                spawnBatchCount = 0;

                if (isDone || !enabled)
                {
                    yield return new WaitForEndOfFrame();
                }

                // Spawns queued while the navmesh
                // was being rebuilt
                while (spawnQueue.Count > 0 && canContinueSpawning)
                {
                    Spawn(spawnQueue.Dequeue());
                }

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
                                if (canContinueSpawning)
                                {
                                    Spawn(spawn);
                                }
                                else
                                {
                                    spawnQueue.Enqueue(spawn);
                                }
                            }
                        }
                        else
                        {
                            CheckIfDoneSpawningSingleCheck();
                        }
                    }
                }
                else
                {
                    CheckIfDoneSpawningSingleCheck();
                }

                yield return new WaitForEndOfFrame();
            }
        }

        protected virtual bool IsDoneSpawning()
        {
            return allSpawns.Count == 0 && spawnQueue.Count == 0 && Wave.Value >= spawnWaves.WaveCount;
        }

        private void CheckIfDoneSpawningDoubleCheck()
        {
            // Do initial check so don't create unnecessary coroutines
            // This won't work if spawner has a start delay, but we'll
            // worry about that scenario later since it's an edge case
            if (IsDoneSpawning())
            {
                // Wait a frame in case killed spawned spawns childs spawns
                CoroutineUtil.Coroutiner.NextFrame(_ => CheckIfDoneSpawningSingleCheck());
            }
        }

        private void CheckIfDoneSpawningSingleCheck()
        {
            if (IsDoneSpawning())
            {
                isDone = true;
                OnDoneSpawning();
            }
        }

        protected void Spawn(Spawn spawn)
        {
            if (isDone)
            {
                return;
            }

            if (spawn.Asset != null)
            {
                if (enabled && canContinueSpawning)
                {
                    newSpawns.Clear();
                    foreach (Vector3 offset in spawn.Asset.GetSpawnOffsets())
                    {
                        TSpawn instance = pool.Get();
                        instance.transform.position = spawn.Position + offset;
                        instance.Initialize(spawn); // Initialize after set position
                        instance.gameObject.SetActive(true); // Set active after initialize
                        newSpawns.Add(instance);
                        spawnBatchCount++;
                    }
                    SetupSpawns(newSpawns);
                }
                else // Navmesh is being rebuilt
                {
                    spawnQueue.Enqueue(spawn);
                }
            }
        }

        protected void SetupAllSpawns()
        {
            // We only want to call SetupSpawns on active spawns
            TSpawn[] activeSpawns = gameObject.GetComponentsInChildren<TSpawn>();
            SetupSpawns(activeSpawns);
        }

        protected void SetupSpawns(IEnumerable<TSpawn> spawns)
        {
            allSpawns.AddRange(spawns);

            foreach (TSpawn spawn in spawns)
            {
                spawn.UnsubscribeFromDespawnEvent(Despawn); // Just in case
                spawn.SubscribeToDespawnEvent(Despawn);

                foreach (Spawner spawner in spawn.GetComponentsInChildren<Spawner>(true))
                {
                    spawner.Initialize(Spawn);
                }

                foreach (ISpawnListener listener in spawn.GetComponentsInChildren<ISpawnListener>(true))
                {
                    listener.OnSpawn();
                }
            }

            OnSpawn(spawns);
        }

        protected void Despawn(IEnumerable<TSpawn> spawns)
        {
            foreach (TSpawn spawn in spawns)
            {
                Despawn(spawn);
            }
        }

        protected void Despawn(ISpawn spawn)
        {
            Despawn((TSpawn)spawn);
        }

        protected void Despawn(TSpawn spawn)
        {
            spawn.gameObject.SetActive(false);
            spawn.UnsubscribeFromDespawnEvent(Despawn);

            // Components get destroyed at the end of the update loop, so we
            // add a 2 frame delay to prevent any weird issues from occuring
            pool.Release(spawn, 2);

            allSpawns.Remove(spawn);
            OnDespawn(spawn);

            foreach (IDespawnListener listener in spawn.GetComponentsInChildren<IDespawnListener>(true))
            {
                listener.OnDespawn();
            }

            if (allSpawns.Count <= GetEnemyThresholdToAutoStartNextWave())
            {
                Timer.Value = GetWaveDuration(Wave.Value) - GetNextWaveDelay();
            }

            CheckIfDoneSpawningDoubleCheck();
        }

        protected void DespawnAll()
        {
            while (allSpawns.Count > 0)
            {
                Despawn(allSpawns[0]);
            }
        }

        protected void StopSpawning()
        {
            isDone = true;
        }

        protected virtual void OnDestroy()
        {

        }
    }
}