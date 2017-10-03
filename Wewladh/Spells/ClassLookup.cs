using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class ClassLookup : Spell
    {
        public ClassLookup()
        {
            this.Name = "Class Lookup";
            this.Text = "Display name: ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            var sb = new StringBuilder();
            foreach (var npc in c.GameServer.NpcDatabase.Values)
            {
                if (npc.Name.Equals(args, StringComparison.CurrentCultureIgnoreCase))
                {
                    sb.AppendFormat("{0} : {1}\n", npc.Name, npc.TypeName);
                }
            }
            c.SendMessage(sb.ToString(), 8);
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