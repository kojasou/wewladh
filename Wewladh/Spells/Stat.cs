using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class Stat : Spell
    {
        public Stat()
        {
            this.Name = "Stat";
            this.Text = "str|int|wis|con|dex|lev|hp|mp value: ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            if (args != null)
            {
                int value = 0;
                var _args = args.Split(' ');
                if (_args.Length == 2 && int.TryParse(_args[1], out value))
                {
                    switch (_args[0])
                    {
                        case "str": c.BaseStr = value; break;
                        case "int": c.BaseInt = value; break;
                        case "wis": c.BaseWis = value; break;
                        case "con": c.BaseCon = value; break;
                        case "dex": c.BaseDex = value; break;
                        case "dmg": c.BaseDmg = value; break;
                        case "hit": c.BaseHit = value; break;
                        case "hp": c.BaseMaximumHP = value; break;
                        case "mp": c.BaseMaximumMP = value; break;
                        case "lev": c.Level = value; break;
                    }
                    c.UpdateStatistics(StatUpdateFlags.Full);
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