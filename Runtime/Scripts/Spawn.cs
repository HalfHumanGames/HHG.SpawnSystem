using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public class Spawn
    {
        public ISpawnAsset Asset;
        public Vector3 Position;

        public Spawn(ISpawnAsset asset, Vector3 position)
        {
            Asset = asset;
            Position = position;
        }
    }
}