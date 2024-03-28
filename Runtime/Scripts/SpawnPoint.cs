using HHG.Common.Runtime;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HHG.SpawnSystem.Runtime
{
    [Serializable]
    public class SpawnPoint : ICloneable<SpawnPoint>
    {
        public string Name { get => name; set => name = value; }
        public Vector2 Position { get => position; set => position = value; }
        public int WaveCount => spawns.Count;

        public ScriptableObject this[int i]
        {
            get
            {
                return i < spawns.Count ? spawns[i] : null;
            }
            set
            {
                while (i >= spawns.Count)
                {
                    spawns.Add(null);
                }

                spawns[i] = value;
            }
        }

        [SerializeField] private string name;
        [SerializeField] private Vector2 position;
        [SerializeField] private List<ScriptableObject> spawns = new List<ScriptableObject>();

        public SpawnPoint(string name, int waves)
        {
            this.name = name;

            for (int i = 0; i < waves; i++)
            {
                spawns.Add(null);
            }
        }

        public Spawn GetSpawn(int wave)
        {
            return new Spawn(this[wave] as ISpawnAsset, position);
        }

        public void Insert(int i, ScriptableObject spawn = null)
        {
            if (i < spawns.Count)
            {
                spawns.Insert(i, spawn);
            }
            else
            {
                spawns.Add(spawn);
            }
        }

        public void Duplicate(int i)
        {
            ScriptableObject spawn = spawns[i];

            if (i < spawns.Count - 1)
            {
                spawns.Insert(i + 1, spawn);
            }
            else
            {
                spawns.Add(spawn);
            }
        }

        public void RemoveAt(int i)
        {
            spawns.RemoveAt(i);
        }

        public SpawnPoint Clone()
        {
            SpawnPoint clone = (SpawnPoint)MemberwiseClone();
            clone.name = IncrementName(clone.name);
            clone.spawns = new List<ScriptableObject>(spawns);
            return clone;
        }

        public static string IncrementName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "New Spawn Point";
            }
            else if (Regex.Match(name, @"\d+(?=\D*$)") is Match match && match.Success)
            {
                int num = int.Parse(match.Value) + 1;
                return name.Replace(match.Value, num.ToString());
            }
            else
            {
                return $"{name} (1)";
            }
        }
    }
}