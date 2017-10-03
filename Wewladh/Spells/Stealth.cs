using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class Stealth : Spell
    {
        public Stealth()
        {
            this.Name = "Stealth";
            this.Icon = 10;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.NoTarget;
            this.TargetType = SpellTargetType.SelfTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            c.Stealth = !c.Stealth;

            if (c.Stealth)
                c.Hide();

            c.Display();
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