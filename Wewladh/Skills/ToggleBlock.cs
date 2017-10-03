using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Skills
{
    public class ToggleBlock : Skill
    {
        public ToggleBlock()
        {
            this.Name = "Toggle Block";
            this.Icon = 265;

            this.Target = SkillTargetType.NoTarget;
            this.Pane = SkillPane.Miscellaneous;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target)
        {
            var map = c.Map;
            var p = c as Player;
            if (map.Flags.HasFlag(MapFlags.ArenaTeam) || p.AdminRights.HasFlag(AdminRights.GameMaster))
                map.ToggleBlock(c.Point.X, c.Point.Y);
            else
                c.SendMessage("This can only be used in a hosted arena area.");
        }
    }
}