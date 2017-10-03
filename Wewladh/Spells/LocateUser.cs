using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class LocateUser : Spell
    {
        public LocateUser()
        {
            this.Name = "Locate User";
            this.Text = "Locate who? ";
            this.Icon = 4;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            if (c.Name.Equals(args, StringComparison.CurrentCultureIgnoreCase))
                return;

            var p = (c as Player);

            if (!p.AdminRights.HasFlag(AdminRights.CanLocateUser) && !p.Map.Flags.HasFlag(MapFlags.ArenaTeam))
            {
                p.SendMessage("This can only be used in a hosted arena area.");
                return;
            }

            foreach (Client client in c.GameServer.Clients)
            {
                if ((client.Player != null) && client.Player.Name.Equals(args, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!p.AdminRights.HasFlag(AdminRights.CanLocateUser) && !client.Player.Map.Flags.HasFlag(MapFlags.ArenaTeam))
                    {
                        p.SendMessage("You can only teleport to users within a hosted arena area.");
                        return;
                    }
                    var point = client.Player.Point;
                    c.Map.RemoveCharacter(c);
                    client.Player.Map.InsertCharacter(c, point);
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