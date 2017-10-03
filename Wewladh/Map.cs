using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Wewladh
{
    public abstract class Map : GameObject
    {
        private static byte[] sotp = new byte[0];

        public Dungeon Dungeon { get; set; }
        public string FileName { get; protected set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Number { get; set; }
        public string DeathMapName { get; protected set; }
        public Point DeathMapPoint { get; protected set; }
        public Tile[,] Tiles { get; private set; }
        public Warp[,] Warps { get; private set; }
        //public Door[,] Doors { get; private set; }
        public byte[] RawData { get; private set; }
        public ushort Checksum { get; private set; }
        public DateTime NextTick { get; set; }
        public long TickCount { get; set; }
        public abstract void OnTick();
        public MapFlags Flags { get; set; }
        public int Music { get; set; }
        public bool TrackVisited { get; protected set; }

        public bool[,] Walls { get; private set; }
        public bool[,] Block { get; private set; }

        public List<Spawn> Spawns { get; private set; }
        public List<VisibleObject> Objects { get; private set; }

        public bool IsPvP
        {
            get { return (Flags & MapFlags.PlayerKill) == MapFlags.PlayerKill; }
            set
            {
                if (value)
                    Flags |= MapFlags.PlayerKill;
                else
                    Flags &= ~MapFlags.PlayerKill;
            }
        }
        public bool HasDayNight
        {
            get { return (Flags & MapFlags.HasDayNight) == MapFlags.HasDayNight; }
            set
            {
                if (value)
                    Flags |= MapFlags.HasDayNight;
                else
                    Flags &= ~MapFlags.HasDayNight;
            }
        }
        public bool CanTeleport
        {
            get { return (Flags & MapFlags.CanTeleport) == MapFlags.CanTeleport; }
            set
            {
                if (value)
                    Flags |= MapFlags.CanTeleport;
                else
                    Flags &= ~MapFlags.CanTeleport;
            }
        }

        public static void LoadCollisionData(string fileName)
        {
            if (File.Exists(fileName))
            {
                sotp = File.ReadAllBytes(fileName);
            }
        }

        public Map(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Tiles = new Tile[width, height];
            this.Warps = new Warp[width, height];
            //this.Doors = new Door[width, height];
            this.Walls = new bool[width, height];
            this.Block = new bool[width, height];
            this.Spawns = new List<Spawn>();
            this.NextTick = DateTime.UtcNow;
            this.Objects = new List<VisibleObject>();
            this.Flags = MapFlags.CanTeleport | MapFlags.SendToHell | MapFlags.ShouldComa;
        }

        public List<T> GetObjects<T>() where T : VisibleObject
        {
            var list = new List<T>();
            foreach (var obj in Objects)
            {
                if (obj is T)
                    list.Add((T)obj);
            }
            return list;
        }

        public void SpawnObject(string name, int x, int y)
        {
            if (GameServer.NpcTypes.ContainsKey(name))
            {
                var type = GameServer.NpcTypes[name];
                var npc = (Monster)Activator.CreateInstance(type);
                npc.CurrentHP = npc.MaximumHP;
                npc.CurrentMP = npc.MaximumMP;
                GameServer.InsertGameObject(npc);
                InsertCharacter(npc, x, y);
            }

            if (GameServer.ItemTypes.ContainsKey(name))
            {
                var type = GameServer.ItemTypes[name];
                var item = (Item)Activator.CreateInstance(type);
                item.Amount = 1;
                item.CurrentDurability = item.MaximumDurability;
                GameServer.InsertGameObject(item);
                InsertCharacter(item, x, y);
            }
        }
        public void SpawnObject(string name, int l, int t, int r, int b)
        {
            int x = Program.Random(l, r + 1);
            int y = Program.Random(t, b + 1);

            if (GameServer.NpcTypes.ContainsKey(name))
            {
                var type = GameServer.NpcTypes[name];
                var npc = (Monster)Activator.CreateInstance(type);
                npc.CurrentHP = npc.MaximumHP;
                npc.CurrentMP = npc.MaximumMP;
                GameServer.InsertGameObject(npc);
                InsertCharacter(npc, x, y);
            }

            if (GameServer.ItemTypes.ContainsKey(name))
            {
                var type = GameServer.ItemTypes[name];
                var item = (Item)Activator.CreateInstance(type);
                item.Amount = 1;
                item.CurrentDurability = item.MaximumDurability;
                GameServer.InsertGameObject(item);
                InsertCharacter(item, x, y);
            }
        }

        public bool Passable(int x, int y)
        {
            return !Walls[x, y] && !Block[x, y] && Tiles[x, y].Weight < 1;
        }

        public override void Update()
        {
            if (DateTime.UtcNow > NextTick)
            {
                OnTick();
                TickCount++;
                NextTick = DateTime.UtcNow.AddMilliseconds(1000);
            }

            foreach (var t in Tiles)
            {
                var w = Warps[t.Point.X, t.Point.Y];
                if (w != null && w.NextAnimation <= DateTime.UtcNow)
                {
                    SpellAnimation(362, t.Point.X, t.Point.Y, 100);
                    w.NextAnimation = DateTime.UtcNow.AddMilliseconds(3750);
                }
            }

            foreach (var ms in Spawns)
            {
                if (ms.NextSpawn <= DateTime.UtcNow && (!ms.SpecificTime || ms.SpawnTime == GameServer.Time))
                {
                    int remaining = (ms.MaximumSpawns - ms.CurrentSpawns);
                    if (remaining > 0)
                    {
                        ms.IsSpawning = true;
                        VisibleObject obj = null;

                        if (GameServer.NpcTypes.ContainsKey(ms.NpcType))
                        {
                            var npc = GameServer.CreateMonster(ms.NpcType);
                            npc.SpawnControl = ms;
                            if (ms.RegionType == SpawnRegion.SingleTile)
                                npc.Direction = ms.Direction;
                            else
                                npc.Direction = (Direction)Program.Random(4);
                            npc.Loot.AddRange(ms.Loot);
                            obj = npc;
                        }

                        if (GameServer.ItemTypes.ContainsKey(ms.NpcType))
                        {
                            var item = GameServer.CreateItem(ms.NpcType);
                            item.SpawnControl = ms;
                            obj = item;
                        }

                        if (GameServer.ReactorTypes.ContainsKey(ms.NpcType))
                        {
                            var npc = GameServer.CreateReactor(ms.NpcType);
                            npc.SpawnControl = ms;
                            obj = npc;
                        }

                        if (obj != null)
                        {
                            int x = ms.X, y = ms.Y;
                            switch (ms.RegionType)
                            {
                                case SpawnRegion.FullMap:
                                    {
                                        x = Program.Random(Width);
                                        y = Program.Random(Height);
                                    } break;
                                case SpawnRegion.Rectangle:
                                    {
                                        if (ms.Right > ms.X && ms.Bottom > ms.Y)
                                        {
                                            x = Program.Random(ms.X, ms.Right + 1);
                                            y = Program.Random(ms.Y, ms.Bottom + 1);
                                        }
                                    } break;
                            }
                            GameServer.InsertGameObject(obj);
                            InsertCharacter(obj, x, y);
                            ++ms.CurrentSpawns;
                        }
                    }
                    else
                    {
                        ms.IsSpawning = false;
                        ms.NextSpawn = DateTime.UtcNow.AddMilliseconds(ms.SpawnRate);
                    }
                }
            }
        }

        public bool Initialize(GameServer gs)
        {
            this.GameServer = gs;

            if (string.IsNullOrEmpty(DeathMapName) || (DeathMapPoint == null))
            {
                DeathMapName = gs.DeathMap;
                DeathMapPoint = new Point(gs.DeathPoint.X, gs.DeathPoint.Y);
            }

            string fileName = (GameServer.DataPath + "\\maps\\" + FileName);

            if (File.Exists(fileName))
            {
                int position = 0;

                this.RawData = File.ReadAllBytes(fileName);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        this.Tiles[x, y] = new Tile(x, y);

                        var bg = (ushort)(RawData[position++] | RawData[position++] << 8);
                        var lfg = (ushort)(RawData[position++] | RawData[position++] << 8);
                        var rfg = (ushort)(RawData[position++] | RawData[position++] << 8);

                        if ((sotp.Length < (lfg - 1)) || (sotp.Length < (rfg - 1)))
                        {
                            Walls[x, y] = false;
                        }
                        else
                        {
                            if ((lfg != 0) && ((sotp[lfg - 1] & 0x0F) == 0x0F))
                                Walls[x, y] = true;
                            if ((rfg != 0) && ((sotp[rfg - 1] & 0x0F) == 0x0F))
                                Walls[x, y] = true;
                        }

                        //if (Doors[x, y] != null)
                        //{
                        //    Doors[x, y].IsOpen = true;
                        //    Walls[x, y] = false;
                        //}
                    }
                }

                this.Checksum = CRC16.Calculate(this.RawData);
                gs.InsertGameObject(this);

                return true;
            }

            return false;
        }

        public void ReloadMap()
        {
            string fileName = GameServer.DataPath + "\\maps\\" + FileName;

            if (File.Exists(fileName))
            {
                int position = 0;

                this.RawData = File.ReadAllBytes(fileName);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var bg = (ushort)(RawData[position++] | RawData[position++] << 8);
                        var lfg = (ushort)(RawData[position++] | RawData[position++] << 8);
                        var rfg = (ushort)(RawData[position++] | RawData[position++] << 8);

                        Walls[x, y] = false;
                        if ((sotp.Length < (lfg - 1)) || (sotp.Length < (rfg - 1)))
                        {
                            Walls[x, y] = false;
                        }
                        else
                        {
                            if ((lfg != 0) && ((sotp[lfg - 1] & 0x0F) == 0x0F))
                                Walls[x, y] = true;
                            if ((rfg != 0) && ((sotp[rfg - 1] & 0x0F) == 0x0F))
                                Walls[x, y] = true;
                        }

                        //if (Doors[x, y] != null)
                        //{
                        //    Doors[x, y].IsOpen = true;
                        //    Walls[x, y] = false;
                        //}
                    }
                }

                this.Checksum = CRC16.Calculate(this.RawData);

                foreach (var character in Objects)
                {
                    if (character is Player)
                        (character as Player).Client.SendMap(this);
                }
            }
        }

        public int GetBackground(int x, int y)
        {
            int index = (Width * y + x) * 6;
            if (index < RawData.Length - 6)
                return (RawData[index + 1] << 8 | RawData[index]);
            return -1;
        }
        public int GetLeftForeground(int x, int y)
        {
            int index = (Width * y + x) * 6;
            if (index < RawData.Length - 6)
                return (RawData[index + 3] << 8 | RawData[index + 2]);
            return -1;
        }
        public int GetRightForeground(int x, int y)
        {
            int index = (Width * y + x) * 6;
            if (index < RawData.Length - 6)
                return (RawData[index + 5] << 8 | RawData[index + 4]);
            return -1;
        }

        public void SetBackground(int x, int y, int value)
        {
            int index = (Width * y + x) * 6;
            string fileName = (GameServer.DataPath + "\\maps\\" + FileName);
            var fs = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.Write);
            fs.Position = index;
            fs.WriteByte((byte)(value % 256));
            fs.Position = index + 1;
            fs.WriteByte((byte)(value / 256));
            fs.Close();
            ReloadMap();
        }
        public void SetLeftForeground(int x, int y, int value)
        {
            int index = (Width * y + x) * 6;
            string fileName = (GameServer.DataPath + "\\maps\\" + FileName);
            var fs = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.Write);
            fs.Position = index + 2;
            fs.WriteByte((byte)(value % 256));
            fs.Position = index + 3;
            fs.WriteByte((byte)(value / 256));
            fs.Close();
            ReloadMap();
        }
        public void SetRightForeground(int x, int y, int value)
        {
            int index = (Width * y + x) * 6;
            string fileName = (GameServer.DataPath + "\\maps\\" + FileName);
            var fs = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.Write);
            fs.Position = index + 4;
            fs.WriteByte((byte)(value % 256));
            fs.Position = index + 5;
            fs.WriteByte((byte)(value / 256));
            fs.Close();
            ReloadMap();
        }

        public bool IsTree(int x, int y)
        {
            var result = false;
            var lfg = GetLeftForeground(x, y).ToString();
            var rfg = GetRightForeground(x, y).ToString();
            using (var sw = new StreamReader(Program.StartupPath + "\\Shared\\treeTile.tbl"))
            {
                string line = null;
                while ((line = sw.ReadLine()) != null)
                {
                    var tiles = line.Split(' ');
                    foreach (var tile in tiles)
                    {
                        if (tile == lfg || tile == rfg)
                            result = true;
                    }
                }
            }
            return result;
        }

        public void AddBlock(int x, int y)
        {
            if (!Block[x, y])
            {
                var npc = new Block();
                GameServer.InsertGameObject(npc);
                InsertCharacter(npc, x, y);
                Block[x, y] = true;
            }
        }
        public void RemoveBlock(int x, int y)
        {
            if (Block[x, y])
            {
                var characters = new Character[Objects.Count];
                Objects.CopyTo(characters);
                foreach (var character in characters)
                {
                    if (character.Point.X == x && character.Point.Y == y && character is Block)
                    {
                        RemoveCharacter(character);
                        GameServer.RemoveGameObject(character);
                    }
                }
                Block[x, y] = false;
            }
        }
        public void ToggleBlock(int x, int y)
        {
            if (Block[x, y])
            {
                RemoveBlock(x, y);
            }
            else
            {
                AddBlock(x, y);
            }
        }

        public void BroadcastMessage(string format, params object[] args)
        {
            foreach (var client in GameServer.Clients)
            {
                if (client != null && client.Player != null && client.Player.Map == this)
                {
                    client.SendMessage(format, args);
                }
            }
        }
        public void BroadcastChatLog(string format, params object[] args)
        {
            foreach (var client in GameServer.Clients)
            {
                if (client != null && client.Player != null && client.Player.Map == this)
                {
                    client.SendMessage(format, args);
                    var p = new ServerPacket(0x0D);
                    p.WriteByte(1);
                    p.WriteUInt32(uint.MaxValue);
                    p.WriteString8(format, args);
                    client.Enqueue(p);
                }
            }
        }

        public void InsertCharacter(VisibleObject obj, Point point)
        {
            InsertCharacter(obj, point.X, point.Y);
        }
        public void InsertCharacter(VisibleObject obj, int x, int y)
        {
            if (obj.Map != null)
                return;

            if (Width <= x || x < 0)
                x = 0;

            if (Height <= y || y < 0)
                y = 0;

            if (obj is Character)
            {
                var character = obj as Character;

                if (Flags.HasFlag(MapFlags.ArenaTeam) == false)
                    character.ArenaTeam = 0;

                switch (character.Direction)
                {
                    case Direction.North: character.XOffset = 0; character.YOffset = -1; break;
                    case Direction.South: character.XOffset = 0; character.YOffset = 1; break;
                    case Direction.West: character.XOffset = -1; character.YOffset = 0; break;
                    case Direction.East: character.XOffset = 1; character.YOffset = 0; break;
                }
            }

            obj.Map = this;
            obj.Point = new Point(x, y);
            Objects.Add(obj);

            Tiles[x, y].Objects.Insert(0, obj);

            if (obj is Player)
            {
                var player = obj as Player;

                if (TrackVisited)
                    player.VisitedMaps.Add(TypeName);

                player.Client.SendMapInfo();
                player.Client.SendLocation();
                player.Client.SendStatistics(StatUpdateFlags.Current);
                player.DisplayTo(player);

                //for (int x1 = 0; x1 < Width; x1++)
                //{
                //    for (int y1 = 0; y1 < Height; y1++)
                //    {
                //        if (Doors[x1, y1] != null)
                //            (character as Player).Client.ToggleDoor(Doors[x1, y1]);
                //    }
                //}
            }

            foreach (var c in Objects)
            {
                if (c.Point.DistanceFrom(obj.Point) <= 12 && c != obj)
                {
                    c.DisplayTo(obj);
                    obj.DisplayTo(c);
                }
            }

            if (obj is Character)
            {
                var character = obj as Character;

                foreach (var r in Tiles[x, y].Objects)
                {
                    var reactor = r as Reactor;
                    if (reactor != null && reactor.Alive)
                    {
                        var dialog = reactor.OnWalkover(character);
                        if (character is Player)
                        {
                            var player = (character as Player);
                            if (dialog != null)
                            {
                                dialog.GameObject = reactor;
                                player.DialogSession.GameObject = reactor;
                                player.DialogSession.Dialog = (DialogB)dialog;
                                player.DialogSession.IsOpen = true;
                                player.DialogSession.Map = player.Map;
                            }
                            player.Client.Enqueue((dialog != null) ? dialog.ToPacket() : Dialog.ExitPacket());
                        }
                    }
                }
            }
        }
        public bool RemoveCharacter(VisibleObject obj)
        {
            if (Objects.Contains(obj))
            {
                if (obj is Item)
                {
                    var item = obj as Item;
                    if (item.SpawnControl != null)
                    {
                        item.SpawnControl.CurrentSpawns--;
                        item.SpawnControl = null;
                    }
                }

                int x = obj.Point.X;
                int y = obj.Point.Y;

                foreach (var c in Objects)
                {
                    if (obj.WithinRange(c, 12) && c is Player)
                        (c as Player).Client.RemoveCharacter(obj.ID);
                }

                obj.Map = null;
                obj.Point = new Point(0, 0);
                Objects.Remove(obj);

                Tiles[x, y].Objects.Remove(obj);

                return true;
            }
            return false;
        }

        public Point[] SurroundingPoints(Point pt, Point[,] points)
        {
            var list = new List<Point>();

            if (pt.X > 0)
            {
                list.Add(points[pt.X - 1, pt.Y]);
            }
            if (pt.Y > 0)
            {
                list.Add(points[pt.X, pt.Y - 1]);
            }
            if (pt.X < this.Width - 1)
            {
                list.Add(points[pt.X + 1, pt.Y]);
            }
            if (pt.Y < this.Height - 1)
            {
                list.Add(points[pt.X, pt.Y + 1]);
            }

            return list.ToArray();
        }

        public Point[] FindPath(Point startPoint, Point endPoint)
        {
            return FindPath(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, 1, false);
        }
        public Point[] FindPath(Point startPoint, Point endPoint, bool ignoreUnits)
        {
            return FindPath(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, 1, ignoreUnits);
        }
        public Point[] FindPath(Point startPoint, Point endPoint, int distance)
        {
            return FindPath(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, distance);
        }
        public Point[] FindPath(int startX, int startY, int endX, int endY, int distance)
        {
            return FindPath(startX, startY, endX, endY, distance, false);
        }
        public Point[] FindPath(int startX, int startY, int endX, int endY, int distance, bool ignoreUnits)
        {
            List<Point> path = new List<Point>();
            Point[,] grid = new Point[Width, Height];
            List<Point> innerPoints = new List<Point>();
            List<Point> outerPoints = new List<Point>();
            bool[,] closedPoints = new bool[Width, Height];
            bool foundTarget = false;

            if ((startX + 1) * (startY + 1) > Tiles.Length)
                return new Point[0];

            if ((endX + 1) * (endY + 1) > Tiles.Length)
                return new Point[0];

            #region Generate Steps

            foreach (Tile t in Tiles)
            {
                grid[t.Point.X, t.Point.Y] = new Point(t.Point.X, t.Point.Y);
                grid[t.Point.X, t.Point.Y].StepCount = -1;
            }
            grid[startX, startY].StepCount = 0;
            outerPoints.Add(grid[startX, startY]);

            while (!foundTarget)
            {
                List<Point> newOuterBound = new List<Point>();
                foreach (Point node in outerPoints)
                {
                    Point[] nextPoint = SurroundingPoints(node, grid);
                    foreach (Point pt in nextPoint)
                    {
                        if (!closedPoints[pt.X, pt.Y])
                        {
                            if ((!Walls[pt.X, pt.Y] && (Warps[pt.X, pt.Y] == null) && (Tiles[pt.X, pt.Y].Weight < 1 || ignoreUnits)) || ((pt.X == endX) && (pt.Y == endY)))
                            {
                                newOuterBound.Add(pt);
                                closedPoints[pt.X, pt.Y] = true;
                                pt.StepCount = node.StepCount + 1;
                                if (pt.X == endX && pt.Y == endY)
                                    foundTarget = true;
                            }
                        }
                        innerPoints.Add(node);
                        closedPoints[node.X, node.Y] = true;
                    }
                }
                outerPoints = newOuterBound;
                if (outerPoints.Count < 1)
                    return new Point[0];
            }

            #endregion

            #region Draw Path

            Point lastPoint = grid[endX, endY];
            path.Add(grid[endX, endY]);

            while (lastPoint.StepCount > 1)
            {
                Point[] nextPoints = SurroundingPoints(lastPoint, grid);
                foreach (Point pt in nextPoints)
                {
                    if (pt.StepCount == (lastPoint.StepCount - 1))
                    {
                        path.Add(pt);
                        lastPoint = pt;
                        break;
                    }
                }
            }

            #endregion

            path.Reverse();
            return path.ToArray();
        }

        public int Distance(Point startPoint, Point endPoint)
        {
            return Distance(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
        }
        public int Distance(int startX, int startY, int endX, int endY)
        {
            List<Point> path = new List<Point>();
            Point[,] grid = new Point[Width, Height];
            List<Point> innerPoints = new List<Point>();
            List<Point> outerPoints = new List<Point>();
            bool[,] closedPoints = new bool[Width, Height];

            if ((startX + 1) * (startY + 1) > Tiles.Length)
                return int.MaxValue;

            if ((endX + 1) * (endY + 1) > Tiles.Length)
                return int.MaxValue;

            if ((startX == endX) && (startY == endY))
                return 0;

            foreach (Tile t in Tiles)
            {
                grid[t.Point.X, t.Point.Y] = new Point(t.Point.X, t.Point.Y);
                grid[t.Point.X, t.Point.Y].StepCount = -1;
            }
            grid[startX, startY].StepCount = 0;
            outerPoints.Add(grid[startX, startY]);

            while (outerPoints.Count != 0)
            {
                List<Point> newOuterBound = new List<Point>();
                foreach (Point node in outerPoints)
                {
                    Point[] nextPoint = SurroundingPoints(node, grid);
                    foreach (Point pt in nextPoint)
                    {
                        if (!closedPoints[pt.X, pt.Y])
                        {
                            if ((Passable(pt.X, pt.Y) && (Warps[pt.X, pt.Y] == null)) || ((pt.X == endX) && (pt.Y == endY)))
                            {
                                newOuterBound.Add(pt);
                                closedPoints[pt.X, pt.Y] = true;
                                pt.StepCount = node.StepCount + 1;
                                if ((pt.X == endX) && (pt.Y == endY))
                                    return pt.StepCount;
                            }
                        }
                        innerPoints.Add(node);
                        closedPoints[node.X, node.Y] = true;
                    }
                }
                outerPoints = newOuterBound;
            }

            return int.MaxValue;
        }

        public void SpellAnimation(int animation, int x, int y, int speed)
        {
            foreach (var c in Objects)
            {
                if ((c is Player) && (c.Point.DistanceFrom(x, y) <= 12))
                    (c as Player).Client.SpellAnimation(animation, x, y, speed);
            }
        }

        public virtual void OnLogin(Player p)
        {

        }
        public virtual void OnSessionMismatch(Player p)
        {

        }

        public Tile this[int x, int y]
        {
            get
            {
                if ((x < 0) || (y < 0) || (x >= Width) || (y >= Height))
                    return null;
                return Tiles[x, y];
            }
        }

        public class Warp
        {
            public Point Point { get; private set; }
            public string MapName { get; private set; }
            public int MinimumLevel { get; private set; }
            public int MaximumLevel { get; private set; }
            public bool NpcPassable { get; private set; }
            public DateTime NextAnimation { get; set; }

            public Warp(string mapName, Point point, int minimumLevel = 1, int maximumLevel = 99, bool npcPassable = false)
            {
                this.MapName = mapName;
                this.Point = point;
                this.MinimumLevel = minimumLevel;
                this.MaximumLevel = maximumLevel;
                this.NpcPassable = npcPassable;
            }
        }
    }

    public class Door
    {
        public Direction Direction { get; private set; }
        public List<Point> Points { get; private set; }
        public bool IsOpen { get; set; }
        public Door(Direction direction, params Point[] points)
        {
            switch (direction)
            {
                case Direction.North: Direction = Direction.East; break;
                case Direction.South: Direction = Direction.East; break;
                case Direction.West: Direction = Direction.North; break;
                case Direction.East: Direction = Direction.North; break;
            }
            this.Points = new List<Point>();
            this.Points.AddRange(points);
        }
    }

    public class Tile
    {
        public Point Point { get; private set; }
        public List<VisibleObject> Objects { get; private set; }

        public Tile(int x, int y)
        {
            this.Point = new Point(x, y);
            this.Objects = new List<VisibleObject>();
        }

        public int DistanceFrom(Tile t)
        {
            return (this.Point.DistanceFrom(t.Point));
        }

        public int Weight
        {
            get
            {
                int weight = 0;
                foreach (var obj in Objects)
                {
                    if (obj is Reactor || obj is Item)
                        continue;
                    if (obj is Player && ((obj as Player).Stealth || (obj as Player).Dead))
                        continue;
                    weight++;
                }
                return weight;
            }
        }
    }

    public class Point
    {
        public static readonly Point Null = new Point(-1, -1);

        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int StepCount { get; set; }

        public int DistanceFrom(Point pt)
        {
            return (Math.Abs(this.X - pt.X) + Math.Abs(this.Y - pt.Y));
        }
        public int DistanceFrom(int x, int y)
        {
            return (Math.Abs(this.X - x) + Math.Abs(this.Y - y));
        }
        public Direction Offset(Point pt)
        {
            if (pt.Y == Y)
            {
                if (pt.X > X)
                    return Direction.East;
                return Direction.West;
            }
            if (pt.X == X)
            {
                if (pt.Y > Y)
                    return Direction.South;
                return Direction.North;
            }
            return Direction.None;
        }
        public Point Offset(Direction d)
        {
            var point = new Point(X, Y);
            switch (d)
            {
                case Direction.North: point.Y--; break;
                case Direction.South: point.Y++; break;
                case Direction.West: point.X--; break;
                case Direction.East: point.X++; break;
            }
            return point;
        }
    }

    #region Interfaces
    public interface IPriorityQueue<T>
    {
        #region Methods
        int Push(T item);
        T Pop();
        T Peek();
        void Update(int i);
        #endregion
    }
    #endregion

    public class PriorityQueueB<T> : IPriorityQueue<T>
    {
        #region Variables Declaration
        protected List<T> InnerList = new List<T>();
        protected IComparer<T> mComparer;
        #endregion

        #region Contructors
        public PriorityQueueB()
        {
            mComparer = Comparer<T>.Default;
        }

        public PriorityQueueB(IComparer<T> comparer)
        {
            mComparer = comparer;
        }

        public PriorityQueueB(IComparer<T> comparer, int capacity)
        {
            mComparer = comparer;
            InnerList.Capacity = capacity;
        }
        #endregion

        #region Methods
        protected void SwitchElements(int i, int j)
        {
            T h = InnerList[i];
            InnerList[i] = InnerList[j];
            InnerList[j] = h;
        }

        protected virtual int OnCompare(int i, int j)
        {
            return mComparer.Compare(InnerList[i], InnerList[j]);
        }

        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        public int Push(T item)
        {
            int p = InnerList.Count, p2;
            InnerList.Add(item); // E[p] = O
            do
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            return p;
        }

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Pop()
        {
            T result = InnerList[0];
            int p = 0, p1, p2, pn;
            InnerList[0] = InnerList[InnerList.Count - 1];
            InnerList.RemoveAt(InnerList.Count - 1);
            do
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);

            return result;
        }

        /// <summary>
        /// Notify the PQ that the object at position i has changed
        /// and the PQ needs to restore order.
        /// Since you dont have access to any indexes (except by using the
        /// explicit IList.this) you should not call this function without knowing exactly
        /// what you do.
        /// </summary>
        /// <param name="i">The index of the changed object.</param>
        public void Update(int i)
        {
            int p = i, pn;
            int p1, p2;
            do	// aufsteigen
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            if (p < i)
                return;
            do	   // absteigen
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);
        }

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Peek()
        {
            if (InnerList.Count > 0)
                return InnerList[0];
            return default(T);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public int Count
        {
            get { return InnerList.Count; }
        }

        public void RemoveLocation(T item)
        {
            int index = -1;
            for (int i = 0; i < InnerList.Count; i++)
            {

                if (mComparer.Compare(InnerList[i], item) == 0)
                    index = i;
            }

            if (index != -1)
                InnerList.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return InnerList[index]; }
            set
            {
                InnerList[index] = value;
                Update(index);
            }
        }
        #endregion
    }

    public class PathFinderFast
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PathFinderNodeFast
        {
            #region Variables Declaration
            public int F; // f = gone + heuristic
            public int G;
            public ushort PX; // Parent
            public ushort PY;
            public byte Status;
            #endregion
        }
        #endregion

        #region Structs
        public struct PathFinderNode
        {
            #region Variables Declaration
            public int F;
            public int G;
            public int H;  // f = gone + heuristic
            public int X;
            public int Y;
            public int PX; // Parent
            public int PY;
            #endregion
        }
        #endregion

        #region Variables Declaration
        // Heap variables are initializated to default, but I like to do it anyway
        private byte[,] mGrid = null;
        private PriorityQueueB<int> mOpen = null;
        private List<PathFinderNode> mClose = new List<PathFinderNode>();
        private bool mStop = false;
        private bool mStopped = true;
        private int mHoriz = 0;
        private bool mDiagonals = true;
        private int mHEstimate = 2;
        private bool mPunishChangeDirection = false;
        private bool mReopenCloseNodes = true;
        private bool mTieBreaker = false;
        private bool mHeavyDiagonals = false;
        private int mSearchLimit = 2000;
        private double mCompletedTime = 0;
        private bool mDebugProgress = false;
        private bool mDebugFoundPath = false;
        private PathFinderNodeFast[] mCalcGrid = null;
        private byte mOpenNodeValue = 1;
        private byte mCloseNodeValue = 2;

        //Promoted local variables to member variables to avoid recreation between calls
        private int mH = 0;
        private int mLocation = 0;
        private int mNewLocation = 0;
        private ushort mLocationX = 0;
        private ushort mLocationY = 0;
        private ushort mNewLocationX = 0;
        private ushort mNewLocationY = 0;
        private int mCloseNodeCounter = 0;
        private ushort mGridX = 0;
        private ushort mGridY = 0;
        private ushort mGridXMinus1 = 0;
        private ushort mGridYLog2 = 0;
        private bool mFound = false;
        private sbyte[,] mDirection = new sbyte[8, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };
        private int mEndLocation = 0;
        private int mNewG = 0;
        #endregion

        #region Constructors
        public PathFinderFast(byte[,] grid)
        {
            if (grid == null)
                throw new Exception("Grid cannot be null");

            mGrid = grid;
            mGridX = (ushort)(mGrid.GetUpperBound(0) + 1);
            mGridY = (ushort)(mGrid.GetUpperBound(1) + 1);
            mGridXMinus1 = (ushort)(mGridX - 1);
            mGridYLog2 = (ushort)Math.Log(mGridY, 2);

            // This should be done at the constructor, for now we leave it here.
            if (Math.Log(mGridX, 2) != (int)Math.Log(mGridX, 2) ||
                Math.Log(mGridY, 2) != (int)Math.Log(mGridY, 2))
                throw new Exception("Invalid Grid, size in X and Y must be power of 2");

            if (mCalcGrid == null || mCalcGrid.Length != (mGridX * mGridY))
                mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

            mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
        }
        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public bool Diagonals
        {
            get { return mDiagonals; }
            set
            {
                mDiagonals = value;
                if (mDiagonals)
                    mDirection = new sbyte[8, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };
                else
                    mDirection = new sbyte[4, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
            }
        }

        public bool HeavyDiagonals
        {
            get { return mHeavyDiagonals; }
            set { mHeavyDiagonals = value; }
        }

        public int HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public bool PunishChangeDirection
        {
            get { return mPunishChangeDirection; }
            set { mPunishChangeDirection = value; }
        }

        public bool ReopenCloseNodes
        {
            get { return mReopenCloseNodes; }
            set { mReopenCloseNodes = value; }
        }

        public bool TieBreaker
        {
            get { return mTieBreaker; }
            set { mTieBreaker = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public double CompletedTime
        {
            get { return mCompletedTime; }
            set { mCompletedTime = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }
        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

        public List<PathFinderNode> FindPath(Point start, Point end)
        {
            lock (this)
            {
                // Is faster if we don't clear the matrix, just assign different values for open and close and ignore the rest
                // I could have user Array.Clear() but using unsafe code is faster, no much but it is.
                //fixed (PathFinderNodeFast* pGrid = tmpGrid) 
                //    ZeroMemory((byte*) pGrid, sizeof(PathFinderNodeFast) * 1000000);

                mFound = false;
                mStop = false;
                mStopped = false;
                mCloseNodeCounter = 0;
                mOpenNodeValue += 2;
                mCloseNodeValue += 2;
                mOpen.Clear();
                mClose.Clear();

                mLocation = (start.Y << mGridYLog2) + start.X;
                mEndLocation = (end.Y << mGridYLog2) + end.X;
                mCalcGrid[mLocation].G = 0;
                mCalcGrid[mLocation].F = mHEstimate;
                mCalcGrid[mLocation].PX = (ushort)start.X;
                mCalcGrid[mLocation].PY = (ushort)start.Y;
                mCalcGrid[mLocation].Status = mOpenNodeValue;

                mOpen.Push(mLocation);
                while (mOpen.Count > 0 && !mStop)
                {
                    mLocation = mOpen.Pop();

                    //Is it in closed list? means this node was already processed
                    if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                        continue;

                    mLocationX = (ushort)(mLocation & mGridXMinus1);
                    mLocationY = (ushort)(mLocation >> mGridYLog2);

                    if (mLocation == mEndLocation)
                    {
                        mCalcGrid[mLocation].Status = mCloseNodeValue;
                        mFound = true;
                        break;
                    }

                    if (mCloseNodeCounter > mSearchLimit)
                    {
                        mStopped = true;
                        return null;
                    }

                    if (mPunishChangeDirection)
                        mHoriz = (mLocationX - mCalcGrid[mLocation].PX);

                    //Lets calculate each successors
                    for (int i = 0; i < (mDiagonals ? 8 : 4); i++)
                    {
                        mNewLocationX = (ushort)(mLocationX + mDirection[i, 0]);
                        mNewLocationY = (ushort)(mLocationY + mDirection[i, 1]);
                        mNewLocation = (mNewLocationY << mGridYLog2) + mNewLocationX;

                        if (mNewLocationX >= mGridX || mNewLocationY >= mGridY)
                            continue;

                        if (mCalcGrid[mNewLocation].Status == mCloseNodeValue && !mReopenCloseNodes)
                            continue;

                        // Unbreakeable?
                        if (mGrid[mNewLocationX, mNewLocationY] == 0)
                            continue;

                        if (mHeavyDiagonals && i > 3)
                            mNewG = mCalcGrid[mLocation].G + (int)(mGrid[mNewLocationX, mNewLocationY] * 2.41);
                        else
                            mNewG = mCalcGrid[mLocation].G + mGrid[mNewLocationX, mNewLocationY];

                        if (mPunishChangeDirection)
                        {
                            if ((mNewLocationX - mLocationX) != 0)
                            {
                                if (mHoriz == 0)
                                    mNewG += Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y);
                            }
                            if ((mNewLocationY - mLocationY) != 0)
                            {
                                if (mHoriz != 0)
                                    mNewG += Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y);
                            }
                        }

                        //Is it open or closed?
                        if (mCalcGrid[mNewLocation].Status == mOpenNodeValue || mCalcGrid[mNewLocation].Status == mCloseNodeValue)
                        {
                            // The current node has less code than the previous? then skip this node
                            if (mCalcGrid[mNewLocation].G <= mNewG)
                                continue;
                        }

                        mCalcGrid[mNewLocation].PX = mLocationX;
                        mCalcGrid[mNewLocation].PY = mLocationY;
                        mCalcGrid[mNewLocation].G = mNewG;

                        mH = mHEstimate * (Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y));

                        if (mTieBreaker)
                        {
                            int dx1 = mLocationX - end.X;
                            int dy1 = mLocationY - end.Y;
                            int dx2 = start.X - end.X;
                            int dy2 = start.Y - end.Y;
                            int cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                            mH = (int)(mH + cross * 0.001);
                        }
                        mCalcGrid[mNewLocation].F = mNewG + mH;

                        //It is faster if we leave the open node in the priority queue
                        //When it is removed, it will be already closed, it will be ignored automatically
                        //if (tmpGrid[newLocation].Status == 1)
                        //{
                        //    //int removeX   = newLocation & gridXMinus1;
                        //    //int removeY   = newLocation >> gridYLog2;
                        //    mOpen.RemoveLocation(newLocation);
                        //}

                        //if (tmpGrid[newLocation].Status != 1)
                        //{
                        mOpen.Push(mNewLocation);
                        //}
                        mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                    }

                    mCloseNodeCounter++;
                    mCalcGrid[mLocation].Status = mCloseNodeValue;
                }

                if (mFound)
                {
                    mClose.Clear();
                    int posX = end.X;
                    int posY = end.Y;

                    PathFinderNodeFast fNodeTmp = mCalcGrid[(end.Y << mGridYLog2) + end.X];
                    PathFinderNode fNode;
                    fNode.F = fNodeTmp.F;
                    fNode.G = fNodeTmp.G;
                    fNode.H = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X = end.X;
                    fNode.Y = end.Y;

                    while (fNode.X != fNode.PX || fNode.Y != fNode.PY)
                    {
                        mClose.Add(fNode);

                        posX = fNode.PX;
                        posY = fNode.PY;
                        fNodeTmp = mCalcGrid[(posY << mGridYLog2) + posX];
                        fNode.F = fNodeTmp.F;
                        fNode.G = fNodeTmp.G;
                        fNode.H = 0;
                        fNode.PX = fNodeTmp.PX;
                        fNode.PY = fNodeTmp.PY;
                        fNode.X = posX;
                        fNode.Y = posY;
                    }

                    mClose.Add(fNode);

                    mStopped = true;
                    return mClose;
                }
                mStopped = true;
                return null;
            }
        }
        #endregion

        #region Inner Classes
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            #region Variables Declaration
            PathFinderNodeFast[] mMatrix;
            #endregion

            #region Constructors
            public ComparePFNodeMatrix(PathFinderNodeFast[] matrix)
            {
                mMatrix = matrix;
            }
            #endregion

            #region IComparer Members
            public int Compare(int a, int b)
            {
                if (mMatrix[a].F > mMatrix[b].F)
                    return 1;
                else if (mMatrix[a].F < mMatrix[b].F)
                    return -1;
                return 0;
            }
            #endregion
        }
        #endregion
    }
}