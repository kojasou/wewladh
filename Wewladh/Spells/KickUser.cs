using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class KickUser : Spell
    {
        public KickUser()
        {
            this.Name = "Kick User";
            this.Text = "Kick who? ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            if (c.Name.Equals(args, StringComparison.CurrentCultureIgnoreCase))
                return;

            foreach (Client client in c.GameServer.Clients)
            {
                if ((client.Player != null) && client.Player.Name.Equals(args, StringComparison.CurrentCultureIgnoreCase))
                {
                    client.Connected = false;
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