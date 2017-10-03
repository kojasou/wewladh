using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public class WorldMap
    {
        public string FileName { get; private set; }
        public List<WorldMapNode> Nodes { get; private set; }
        public bool IsOpen { get; set; }
        public WorldMap(string fileName)
        {
            this.FileName = fileName;
            this.Nodes = new List<WorldMapNode>();
        }
    }
    public class WorldMapNode
    {
        public string MapName { get; private set; }
        public int MapX { get; private set; }
        public int MapY { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public string MapTypeName { get; private set; }
        public WorldMapNode(string mapName, string mapTypeName, int mapX, int mapY, int x, int y)
        {
            this.MapName = mapName;
            this.MapTypeName = mapTypeName;
            this.MapX = mapX;
            this.MapY = mapY;
            this.X = x;
            this.Y = y;
        }
    }
}