using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Spells
{
    public class Create : Spell
    {
        public Create()
        {
            this.Name = "Create";
            this.Text = "Class name of item, skill, spell, or npc: ";
            this.Icon = 0;

            this.Pane = SpellPane.Miscellaneous;
            this.CastType = SpellCastType.TextInput;
            this.TargetType = SpellTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target, string args)
        {
            var map = c.Map;
            var p = c as Player;

            var _args = args.Split(',');

            var type = _args[0].Replace(' ', '_');

            if (GameServer.NpcTypes.ContainsKey(type))
            {
                if (p.AdminRights.HasFlag(AdminRights.CanCreateMerchants) || p.AdminRights.HasFlag(AdminRights.ArenaHost))
                {
                    if (map.Flags.HasFlag(MapFlags.ArenaTeam) || p.AdminRights.HasFlag(AdminRights.CanCreateMerchants))
                    {
                        int amount = 1;
                        if (_args.Length > 1)
                            int.TryParse(_args[1], out amount);

                        for (int i = 0; i < amount; i++)
                        {
                            var npc = GameServer.CreateMonster(type);
                            npc.CurrentHP = npc.BaseMaximumHP;
                            npc.CurrentMP = npc.BaseMaximumMP;
                            npc.Point.X = c.Point.X;
                            npc.Point.Y = c.Point.Y;
                            npc.Direction = c.Direction;
                            npc.Experience = 0;
                            npc.ArenaTeam = 1;
                            GameServer.InsertGameObject(npc);
                            c.Map.InsertCharacter(npc, npc.Point.X, npc.Point.Y);
                        }
                    }
                    else
                    {
                        c.SendMessage("This can only be used in a hosted arena area.");
                    }
                }
            }
            else if (GameServer.ItemTypes.ContainsKey(type))
            {
                if (p.AdminRights.HasFlag(AdminRights.CanCreateItems))
                {
                    int amount = 1;
                    if (_args.Length > 1)
                        int.TryParse(_args[1], out amount);
                    for (int i = 0; i < amount; i++)
                    {
                        var item = GameServer.CreateItem(type);
                        p.AddItem(item);
                    }
                }
            }
            else if (GameServer.SkillTypes.ContainsKey(type))
            {
                if (p.AdminRights.HasFlag(AdminRights.CanCreateSkills))
                {
                    var skill = GameServer.CreateSkill(type);
                    int index = p.FindEmptySkillIndex(skill.Pane);
                    if (index != -1)
                    {
                        if (p.SkillBook.Contains(skill.GetType().Name))
                        {
                            p.Client.SendMessage("You already have that skill.");
                        }
                        else
                        {
                            p.AddSkill(skill, index);
                            p.SpellAnimation(22, 50);
                        }
                    }
                }
            }
            else if (GameServer.SpellTypes.ContainsKey(type))
            {
                if (p.AdminRights.HasFlag(AdminRights.CanCreateSpells))
                {
                    var spell = GameServer.CreateSpell(type);
                    int index = p.FindEmptySpellIndex(spell.Pane);
                    if (index != -1)
                    {
                        if (p.SpellBook.Contains(spell.GetType().Name))
                        {
                            p.Client.SendMessage("You already have that spell.");
                        }
                        else
                        {
                            p.AddSpell(spell, index);
                            p.SpellAnimation(22, 50);
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