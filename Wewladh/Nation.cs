using System.Collections.Generic;

namespace Wewladh
{
    public abstract class Nation
    {
        public string Name { get; protected set; }
        public byte Flag { get; protected set; }
        public List<string> Maps { get; private set; }
        public List<int> XPoints { get; private set; }
        public List<int> YPoints { get; private set; }
        public Nation()
        {
            this.Maps = new List<string>();
            this.XPoints = new List<int>();
            this.YPoints = new List<int>();
        }
        protected void AddSpawnLocation(string map, int x, int y)
        {
            this.Maps.Add(map);
            this.XPoints.Add(x);
            this.YPoints.Add(y);
        }
    }
}