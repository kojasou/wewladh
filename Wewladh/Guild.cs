using System;
using System.Collections.Generic;

namespace Wewladh
{
    public class Guild
    {
        public string Name { get; set; }
        public string Leader { get; set; }
        public HashSet<string> Members { get; set; }
        public HashSet<string> Council { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public Guild(string name, string leader)
        {
            this.Name = name;
            this.Leader = leader;
            this.Members = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            this.Council = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        }
        public void AddExperience(long exp)
        {
            Experience += (int)exp;
            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE guilds SET experience=@experience WHERE name=@name";
            com.Parameters.AddWithValue("@experience", Experience);
            com.Parameters.AddWithValue("@name", Name);
            com.ExecuteNonQuery();
        }
        public void AddMember(Player p)
        {
            if (Members.Contains(p.Name))
                return;

            Members.Add(p.Name);

            var members = new string[Members.Count];
            Members.CopyTo(members);

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE guilds SET members=@members WHERE name=@name";
            com.Parameters.AddWithValue("@members", string.Join(";", members));
            com.Parameters.AddWithValue("@name", Name);
            com.ExecuteNonQuery();
        }
        public void RemoveMember(Player p)
        {
            if (!Members.Contains(p.Name))
                return;

            Members.Remove(p.Name);

            var members = new string[Members.Count];
            Members.CopyTo(members);

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE guilds SET members=@members WHERE name=@name";
            com.Parameters.AddWithValue("@members", string.Join(";", members));
            com.Parameters.AddWithValue("@name", Name);
            com.ExecuteNonQuery();
        }
    }
}