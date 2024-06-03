using HHG.Common.Runtime;
using System;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public class Spawner : MonoBehaviour
    {
        public SpawnerWaves Waves => waves;

        [SerializeField, Unfold(UnfoldName.Child)] protected SpawnerWaves waves;

        private Coroutine coroutine; 

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {

        }

        public void Initialize(Action<Spawn> createSpawn)
        {
            waves.Initialize(createSpawn);
        }

        public void Trigger(MonoBehaviour source = null, Transform transform = null, float timeScale = 1f)
        {
            Trigger(source, transform, () => timeScale);
        }

        public void Trigger(MonoBehaviour source, Transform transform, Func<float> timeScale)
        {
            source ??= this;
            transform ??= source.transform;

            if (coroutine != null)
            {
                CoroutineUtil.StopCoroutine(coroutine);
            }

            coroutine = source.StartCoroutine(waves.SpawnAsync(transform, timeScale));
        }

        protected virtual void OnDestroy()
        {
            if (coroutine != null)
            {
                CoroutineUtil.StopCoroutine(coroutine);
            }
        }
    }
}