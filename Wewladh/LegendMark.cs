using System;

namespace Wewladh
{
    public abstract class LegendMark
    {
        public string Key { get; protected set; }
        public string Format { get; protected set; }
        public int Icon { get; protected set; }
        public int Color { get; protected set; }
        public string[] Arguments { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public LegendMark()
        {
            this.Key = String.Empty;
            this.Format = String.Empty;
            this.Arguments = new string[0];
            this.DateCreated = DateTime.UtcNow;
            this.DateUpdated = DateTime.UtcNow;
        }
        
        public override string ToString()
        {
            return String.Format(DateCreated.ToString("yyyy/MM/dd") + ":  " + Format, Arguments);
        }
    }
}