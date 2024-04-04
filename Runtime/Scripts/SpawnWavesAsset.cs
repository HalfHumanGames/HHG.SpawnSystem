using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    [CreateAssetMenu(fileName = "Spawn Waves", menuName = "HHG/Spawn System/Spawn Waves")]
    public class SpawnWavesAsset : ScriptableObject
    {
        public int WaveCount => SpawnPoints.Count == 0 ? 0 : SpawnPoints.Max(s => s.WaveCount);
        public IList SpawnPointsList => spawnWaves.SpawnPoints; // For property drawer
        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnWaves.SpawnPoints;

        [SerializeField] private SpawnWaves spawnWaves = new SpawnWaves();

        private List<SpawnPoint> spawnPoints => spawnWaves.SpawnPoints;

        public Spawn[] GetSpawnsForWave(int wave)
        {
            if (wave < 0 || wave >= WaveCount)
            {
                return new Spawn[0];
            }

            return spawnPoints.Select(s => s.GetSpawn(wave)).ToArray();
        }

        public void AddSpawnPoint()
        {
            int count = spawnPoints.Count;
            string name = SpawnPoint.IncrementName(count == 0 ? string.Empty : spawnPoints[count - 1].Name);
            spawnPoints.Add(new SpawnPoint(name, WaveCount));
        }

        public void InsertNewSpawnPointAt(int index)
        {
            string name = SpawnPoint.IncrementName(spawnPoints[index].Name);
            spawnPoints.Insert(index, new SpawnPoint(name, WaveCount));
        }

        public void InsertNewSpawnPointAfter(SpawnPoint spawnPoint)
        {
            int index = spawnPoints.IndexOf(spawnPoint);
            string name = SpawnPoint.IncrementName(spawnPoints[index].Name);
            if (index == spawnPoints.Count - 1)
            {
                spawnPoints.Add(new SpawnPoint(name, WaveCount));
            }
            else
            {
                spawnPoints.Insert(index + 1, new SpawnPoint(name, WaveCount));
            }
        }

        public void DuplicateSpawnPoint(SpawnPoint spawnPoint)
        {
            int index = spawnPoints.IndexOf(spawnPoint);
            DuplicateSpawnPointAt(index);
        }

        public void DuplicateSpawnPointAt(int index)
        {
            SpawnPoint clone = spawnPoints[index].Clone();
            spawnPoints.Insert(index + 1, clone);
        }

        public void CopyPasteSpawnPoint(int from, SpawnPoint to)
        {
            for (int i = 0; i < to.WaveCount; i++)
            {
                to[i] = spawnPoints[from][i];
            }
        }

        public void CopyPasteSpawnPoint(int from, int to)
        {
            for (int i = 0; i < spawnPoints[to].WaveCount; i++)
            {
                spawnPoints[to][i] = spawnPoints[from][i];
            }
        }

        public void ReorderSpawnPoint(int from, int to)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                ScriptableObject spawn = spawnPoints[i][from];
                spawnPoints[i].RemoveAt(from);
                spawnPoints[i].Insert(to, spawn);
            }
        }

        public void RemoveSpawnPoint(SpawnPoint spawnPoint)
        {
            spawnPoints.Remove(spawnPoint);
        }

        public void RemoveSpawnPointAt(int index)
        {
            spawnPoints.RemoveAt(index);
        }

        public void AddSpawnWave()
        {
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                spawnPoint[WaveCount] = null;
            }
        }

        public void InsertNewSpawnWaveAt(int index)
        {
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                spawnPoint.Insert(index);
            }
        }

        public void DuplicateSpawnWaveAt(int wave)
        {
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                spawnPoint.Duplicate(wave);
            }
        }

        public void CopyPasteSpawnWave(int from, int to)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                spawnPoints[i][to] = spawnPoints[i][from];
            }
        }

        public void RemoveSpawnWaveAt(int wave)
        {
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                spawnPoint.RemoveAt(wave);
            }
        }

        public string GetWaveInfoText(int wave)
        {
            var spawns = spawnPoints?.
                Select(s => s[wave]).
                Where(s => s != null);

            var counts = spawns.
                GroupBy(s => s.name).
                Select(g => new
                {
                    Spawn = g.Key,
                    Count = g.Count()
                }).
                OrderBy(g => g.Spawn);

            int sum = counts.Sum(c => c.Count);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Total: {sum}");
            foreach (var count in counts)
            {
                sb.AppendLine($"• {count.Spawn}: {count.Count}");
            }

            ISpawnAsset.AppendInfoText(sb, spawnPoints, wave);

            return sb.ToString().Trim();
        }

        public void Reset()
        {
            spawnPoints.Clear();
        }
    }
}