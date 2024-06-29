using HHG.Common.Runtime;
using System;

namespace HHG.SpawnSystem.Runtime
{
    public interface ISpawn
    {
        public IHealth Health { get; }
        public void Initialize(ISpawnAsset spawnAsset);
        public void SubscribeToDespawnEvent(Action<ISpawn> despawn);
        public void UnsubscribeFromDespawnEvent(Action<ISpawn> despawn);
    }
}