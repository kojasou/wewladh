using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public class HostedArena
    {
        public ArenaGame GameType { get; set; }
        public long HostGUID { get; set; }
        public List<long> AssistantGUIDs { get; private set; }
        public bool InProgress
        {
            get { return HostGUID != 0; }
        }
        public HostedArena()
        {
            this.AssistantGUIDs = new List<long>();
        }
    }
}