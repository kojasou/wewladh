using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Skills
{
    public class DeleteObject : Skill
    {
        public DeleteObject()
        {
            this.Name = "Delete Object";
            this.Icon = 265;

            this.Target = SkillTargetType.NoTarget;
            this.Pane = SkillPane.Miscellaneous;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target)
        {
            int x = c.Point.X + c.XOffset;
            int y = c.Point.Y + c.YOffset;
            var tile = c.Map[x, y];
            if (tile != null)
            {
                var characters = new VisibleObject[tile.Objects.Count];
                tile.Objects.CopyTo(characters);
                foreach (var t in characters)
                {
                    if (t is Block || t is Chest) continue;

                    if (t is Monster || t is Item || t is Reactor)
                    {
                        GameServer.RemoveGameObject(t);
                    }
                }
            }
        }
    }
}