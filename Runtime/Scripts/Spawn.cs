using System.Collections.Generic;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    public class Spawn
    {
        public ISpawnAsset Asset => asset;
        public Vector3 Position => position;

        private ISpawnAsset asset;
        private Vector3 position;
        private Dictionary<object, object> metadata;

        public Spawn(ISpawnAsset asset, Vector3 position, params KeyValuePair<object, object>[] metaDataEntries)
        {
            this.asset = asset;
            this.position = position;

            if (metaDataEntries.Length > 0)
            {
                metadata = new Dictionary<object, object>();

                for (int i = 0; i < metaDataEntries.Length; i++)
                {
                    object key = metaDataEntries[i].Key;
                    object value = metaDataEntries[i].Value;
                    metadata[key] = value;
                }
            }
        }

        public bool TryGetMetadata<T>(object key, out T val)
        {
            if (metadata != null && metadata.TryGetValue(key, out object weak) && weak is T typed)
            {
                val = typed;
                return true;
            }

            val = default;
            return false;
        }

        public void SetMetadata(object key, object value)
        {
            if (metadata == null)
            {
                metadata = new Dictionary<object, object>();
            }

            metadata[key] = value;
        }
    }
}