using HHG.Common.Runtime;
using System;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    [Serializable]
    public class SpawnerWave
    {
        public int Count => count;
        public float Delay => delay;
        public float Frequency => frequency;
        public ISpawnAsset Spawn => (ISpawnAsset)spawn;

        [SerializeField] private int count;
        [SerializeField] private float delay;
        [SerializeField] private float frequency;
        [SerializeField, Dropdown(typeof(ISpawnAsset), nameof(ISpawnAsset.IsEnabled))] private ScriptableObject spawn;
    }
}