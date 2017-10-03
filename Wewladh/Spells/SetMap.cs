using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class SetMap : Spell
    {
        public SetMap()
        {
            this.Name = "Set Map";
            this.Text = "Flags (or ? for list): ";

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            if (args != null)
            {
                if (args == "?")
                {
                    var sb = new StringBuilder();
                    sb.Append("Possible flags:\n");
                    sb.Append("Snow (1)\n");
                    sb.Append("Rain (2) [broken]\n");
                    sb.Append("NoMap (64)\n");
                    sb.Append("Winter (128)\n");
                    sb.Append("CanSummon (256)\n");
                    sb.Append("CanLocate (512)\n");
                    sb.Append("CanTeleport (1024)\n");
                    sb.Append("CanUseSkill (2048)\n");
                    sb.Append("CanUseSpell (4096)\n");
                    sb.Append("ArenaTeam (8192)\n");
                    sb.Append("PlayerKill (16384)\n");
                    sb.Append("SendToHell (32768)\n");
                    sb.Append("ShouldComa (65536)\n\n");
                    sb.Append("Combined flags:\n");
                    sb.Append("Default (CanSummon, CanLocate, CanTeleport, CanUseSkill, CanUseSpell, SendToHell, ShouldComa\n");
                    sb.Append("Darkness (Snow, Rain)\n\n");
                    sb.Append("Flags can be set either by name or numerical value. Combine flags by separating names with commas or adding up numerical values.");
                    c.SendMessage(sb.ToString(), 8);
                }
                else
                {
                    var mapFlags = c.Map.Flags;
                    if (Enum.TryParse<MapFlags>(args, true, out mapFlags))
                    {
                        c.Map.Flags = mapFlags;
                        foreach (var client in c.GameServer.Clients)
                        {
                            if (client.Player != null && client.Player.Map == c.Map)
                                client.Refresh();
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