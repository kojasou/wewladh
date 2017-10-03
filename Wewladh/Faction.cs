using System.Collections.Generic;

namespace Wewladh
{
    public abstract class Faction
    {
        public string Name { get; protected set; }
        public Allegiance PlayerDefault { get; protected set; }
        public Dictionary<string, Allegiance> AllegianceTable { get; private set; }
        public Faction()
        {
            this.Name = string.Empty;
            this.PlayerDefault = Allegiance.Neutral;
            this.AllegianceTable = new Dictionary<string, Allegiance>();
        }
    }
}