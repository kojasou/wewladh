using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class MonsterSprite : Spell
    {
        public MonsterSprite()
        {
            this.Name = "Monster Sprite";
            this.Text = "Sprite: ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            var p = (c as Player);

            int sprite;
            if (int.TryParse(args, out sprite))
            {
                p.Sprite = sprite;
                p.Display();
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