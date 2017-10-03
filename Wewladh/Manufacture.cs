using System.Collections.Generic;

namespace Wewladh
{
    public abstract class Manufacture
    {
        public string Item { get; protected set; }
        public string SkillName { get; protected set; }
        public string Description { get; protected set; }
        public Dictionary<string, int> Ingredients { get; private set; }
        public int MediumLevel { get; protected set; }
        public int MaximumLevel { get; protected set; }
        public Manufacture()
        {
            this.Item = string.Empty;
            this.SkillName = string.Empty;
            this.Description = string.Empty;
            this.Ingredients = new Dictionary<string, int>();
        }
        public virtual bool CanManufacture(Player p)
        {
            foreach (var i in Ingredients)
            {
                if (p.Inventory.Count(i.Key) < i.Value)
                    return false;
            }
            var index = p.SkillBook.IndexOf(SkillName);
            return p.CurrentManufactures.Contains(GetType().Name) && index > -1;
        }
        public virtual void ManufactureItem(Player p)
        {
            foreach (var i in Ingredients)
            {
                p.RemoveItem(i.Key, i.Value);
            }
            var item = p.GameServer.CreateItem(Item);
            p.AddItem(item);
            p.Client.SendMessage(string.Format("You created {0}!", item.Name));
        }
    }
}