using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class MoveUser : Spell
    {
        public MoveUser()
        {
            this.Name = "Move User";
            this.Text = "user,map,x,y: ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            var _args = args.Split(',');
            if (_args.Length == 4)
            {
                int x, y;
                string name1 = _args[1].TrimEnd(' ').Replace(' ', '_');
                string name2 = "map_" + name1;
                int.TryParse(_args[2].Replace(" ", string.Empty), out x);
                int.TryParse(_args[3].Replace(" ", string.Empty), out y);
                if (c.GameServer.MapDatabase.ContainsKey(name1))
                {
                    var map = c.GameServer.MapDatabase[name1];
                    if (map.Width > x && map.Height > y && x >= 0 && y >= 0)
                    {
                        foreach (Client client in c.GameServer.Clients)
                        {
                            if (client.Player != null && client.Player.Name.Equals(_args[0], StringComparison.CurrentCultureIgnoreCase))
                            {
                                client.Player.Map.RemoveCharacter(client.Player);
                                map.InsertCharacter(client.Player, x, y);
                            }
                        }
                    }
                }
                if (c.GameServer.MapDatabase.ContainsKey(name2))
                {
                    var map = c.GameServer.MapDatabase[name2];
                    if (map.Width > x && map.Height > y && x >= 0 && y >= 0)
                    {
                        foreach (Client client in c.GameServer.Clients)
                        {
                            if (client.Player != null && client.Player.Name.Equals(_args[0], StringComparison.CurrentCultureIgnoreCase))
                            {
                                client.Player.Map.RemoveCharacter(client.Player);
                                map.InsertCharacter(client.Player, x, y);
                            }
                        }
                    }
                }
            }
        }
        public override void OnAdd(Character target)
        {

        }
        public override void OnRemove(Character target)
        {

        }
        public override void OnTick(Character c)
        {

        }
    }
}