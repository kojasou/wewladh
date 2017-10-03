using System;
using System.Collections.Generic;

namespace Wewladh
{
    public class Spawn
    {
        public string NpcType { get; private set; }

        public int SpawnRate { get; private set; }
        public int MaximumSpawns { get; private set; }
        public int CurrentSpawns { get; set; }

        public DateTime NextSpawn { get; set; }
        public bool IsSpawning { get; set; }
        public bool SpecificTime { get; set; }
        public Time SpawnTime { get; set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Right { get; private set; }
        public int Bottom { get; private set; }
        public Direction Direction { get; private set; }
        public SpawnRegion RegionType { get; private set; }
        public bool CanWalkOutsideRectangle { get; set; }
        public bool CanFollowOutsideRectangle { get; set; }
        public bool CanSpawnInWall { get; set; }

        public List<Loot> Loot { get; private set; }

        public Spawn(string type, int max, int rate, bool specificTime = false, Time spawnTime = Time.Dawn)
        {
            this.Loot = new List<Loot>();
            this.IsSpawning = true;

            this.NpcType = type;
            this.MaximumSpawns = max;
            this.SpawnRate = rate;
            this.RegionType = SpawnRegion.FullMap;
            this.SpecificTime = specificTime;
            this.SpawnTime = spawnTime;
        }
        public Spawn(string type, int max, int rate, int x, int y, bool specificTime = false, Time spawnTime = Time.Dawn)
        {
            this.Loot = new List<Loot>();
            this.IsSpawning = true;

            this.NpcType = type;
            this.MaximumSpawns = max;
            this.SpawnRate = rate;
            this.X = x;
            this.Y = y;
            this.RegionType = SpawnRegion.SingleTile;
            this.SpecificTime = specificTime;
            this.SpawnTime = spawnTime;
        }
        public Spawn(string type, int max, int rate, int x, int y, Direction d, bool specificTime = false, Time spawnTime = Time.Dawn)
        {
            this.Loot = new List<Loot>();
            this.IsSpawning = true;

            this.NpcType = type;
            this.MaximumSpawns = max;
            this.SpawnRate = rate;
            this.X = x;
            this.Y = y;
            this.RegionType = SpawnRegion.SingleTile;
            this.Direction = d;
            this.SpecificTime = specificTime;
            this.SpawnTime = spawnTime;
        }
        public Spawn(string type, int max, int rate, int l, int t, int r, int b, bool specificTime = false, Time spawnTime = Time.Dawn)
        {
            this.Loot = new List<Loot>();
            this.IsSpawning = true;

            this.NpcType = type;
            this.MaximumSpawns = max;
            this.SpawnRate = rate;
            this.X = l;
            this.Y = t;
            this.Right = r;
            this.Bottom = b;
            this.RegionType = SpawnRegion.Rectangle;
            this.SpecificTime = specificTime;
            this.SpawnTime = spawnTime;
        }
    }

    public enum SpawnRegion
    {
        SingleTile,
        Rectangle,
        FullMap
    }

    public class Loot
    {
        public List<string> Items { get; private set; }
        public int MinimumValue { get; private set; }
        public int MaximumValue { get; private set; }
        public Loot(int max, params string[] items)
        {
            this.MinimumValue = 1;
            this.MaximumValue = max;
            this.Items = new List<string>(items);
        }
        public Loot(int min, int max, params string[] items)
        {
            this.MinimumValue = min;
            this.MaximumValue = max;
            this.Items = new List<string>(items);
        }
    }
}