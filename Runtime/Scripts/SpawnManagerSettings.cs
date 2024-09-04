namespace HHG.SpawnSystem.Runtime
{
    public struct SpawnManagerSettings
    {
        public int PoolDefaultCapacity;
        public int PoolMaxSize;
        public bool PoolPrewarm;
        public int SpawnBatchSize;

        public SpawnManagerSettings(int poolDefaultCapacity, int poolMaxSize, bool poolPrewarm, int spawnBatchSize)
        {
            PoolDefaultCapacity = poolDefaultCapacity;
            PoolMaxSize = poolMaxSize;
            PoolPrewarm = poolPrewarm;
            SpawnBatchSize = spawnBatchSize;
        }
    }
}