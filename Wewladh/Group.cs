using System.Collections.Generic;

namespace Wewladh
{
    public class Group : GameObject
    {
        public Character Leader { get; set; }
        public List<Character> Members { get; set; }
        public bool HasMembers
        {
            get { return Members.Count > 1; }
        }

        public Group(GameServer gs, string name, Character leader)
        {
            this.GameServer = gs;
            this.Name = name;
            this.Leader = leader;
            this.Members = new List<Character>();
            gs.InsertGameObject(this);

            this.Members.Add(leader);
        }

        public void AddMember(Character p)
        {
            Members.Add(p);
            p.Group = this;

            var members = new Character[Members.Count];
            Members.CopyTo(members);

            foreach (Character m in members)
            {
                if (m is Player)
                {
                    var client = (m as Player).Client;
                    client.SendMessage("{0} is joining this group.", p.Name);
                    client.SendProfile();
                }
                m.Display();
            }
        }
        public void RemoveMember(Character p)
        {
            if (Members.Count <= 2)
            {
                Disband();
                return;
            }

            var members = new Character[Members.Count];
            Members.CopyTo(members);

            Members.Remove(p);
            p.Group = new Group(GameServer, string.Empty, p);

            foreach (var m in members)
            {
                if (m is Player)
                {
                    var client = (m as Player).Client;
                    client.SendMessage("{0} is leaving this group.", p.Name);
                    client.SendProfile();
                }
                m.Display();
            }
            
            if (p == Leader)
            {
                ChangeLeader(Members[0]);
            }
        }
        public void ChangeLeader(Character p)
        {
            Leader = p;

            var members = new Player[Members.Count];
            Members.CopyTo(members);

            foreach (Character m in Members)
            {
                if (m is Player)
                {
                    var client = (m as Player).Client;
                    client.SendMessage("{0} is the new leader of the group", p.Name);
                    client.SendProfile();
                }
            }
        }
        public void Disband()
        {
            Disband(true);
        }
        public void Disband(bool soloGroup)
        {
            var members = new Character[Members.Count];
            Members.CopyTo(members);

            foreach (var m in members)
            {
                if (soloGroup)
                {
                    m.Group = new Group(GameServer, string.Empty, m);
                    if (m is Player)
                    {
                        var client = (m as Player).Client;
                        client.SendMessage("Your group has disbanded.");
                        client.SendProfile();
                    }
                    m.Display();
                }
                Members.Remove(m);
            }

            GameServer.RemoveGameObject(this);
        }
        public Player[] Players()
        {
            var list = new List<Player>();
            foreach (var member in Members)
            {
                if (member is Player)
                    list.Add(member as Player);
            }
            return list.ToArray();
        }
    }
}