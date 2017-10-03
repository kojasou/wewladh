using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public class Title
    {
        public int ID { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public virtual bool Available(Player p)
        {
            return false;
        }
        public Title()
        {
            Name = string.Empty;
            Description = string.Empty;
        }
    }

    public class Title_Null : Title
    {
        public Title_Null()
        {
            this.ID = 0;
            this.Name = "----------";
            this.Description = "No title";
        }
        public override bool Available(Player p)
        {
            return true;
        }
    }
}