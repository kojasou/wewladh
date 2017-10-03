using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public class Dungeon : GameObject
    {
        public Dictionary<string, Map> Maps { get; private set; }
        public DateTime ExpirationDate { get; set; }
        public Dungeon(DateTime expirationDate)
        {
            this.ExpirationDate = expirationDate;
        }
        public Dungeon(int days, int hours, int minutes, int seconds)
        {
            this.ExpirationDate = DateTime.UtcNow.Add(new TimeSpan(days, hours, minutes, seconds));
        }
    }
}