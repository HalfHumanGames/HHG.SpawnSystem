using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public interface ISpawnAsset
    {
        public GameObject Prefab { get; }
        public Sprite Icon { get; }
    }
}