using HHG.Common.Runtime;
using System;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public class Spawner : MonoBehaviour
    {
        public SpawnerWaves Waves => waves;

        [SerializeField, Unfold(UnfoldName.Child)] protected SpawnerWaves waves;

        public void Initialize(Action<Spawn> createSpawn)
        {
            waves.Initialize(createSpawn);
        }

        public void Trigger(MonoBehaviour source = null, Transform transform = null)
        {
            source ??= this;
            transform ??= source.transform;
            source.StartCoroutine(waves.SpawnAsync(transform));
        }
    }
}