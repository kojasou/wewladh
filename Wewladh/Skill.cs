using System;
using System.Collections.Generic;

namespace Wewladh
{
    public class SkillRank
    {
        public int RequiredLevel { get; set; }
        public int RequiredAbility { get; set; }
        public bool RequiresMaster { get; set; }
        public bool AutomaticRankUp { get; set; }
        public bool LearnFromNpc { get; set; }

        public int SkillPointCost { get; set; }

        public int MinimumDistance { get; set; }
        public int MaximumDistance { get; set; }

        public double BaseCooldownLength { get; set; }
        public double MinimumCooldownLength { get; set; }
        public double MaximumCooldownLength { get; set; }

        public int SuccessRate { get; set; }

        public long RequiredGold { get; set; }
        public Dictionary<string, int> RequiredItems { get; set; }

        public int MaxLevel { get; set; }

        public SkillRank()
        {
            this.MinimumDistance = 1;
            this.MaximumDistance = 1;
            this.MinimumCooldownLength = 1;
            this.MaximumCooldownLength = double.MaxValue;
            this.RequiredItems = new Dictionary<string, int>();
        }
    }

    public abstract class Skill : GameObject
    {
        public ushort Icon { get; protected set; }
        public SkillPane Pane { get; protected set; }
        public bool IsAssail { get; protected set; }

        public int Slot { get; set; }
        public int Rank { get; set; }
        public int Level { get; set; }
        public SkillRank[] Ranks { get; protected set; }

        public int SkillPointCostMod { get; set; }
        public int SkillPointCost
        {
            get
            {
                int value = Ranks[Rank - 1].SkillPointCost + SkillPointCostMod;
                if (value < 0)
                    return 0;
                if (value > 10)
                    return 10;
                return value;
            }
        }

        public int Uses { get; set; }
        public int UsesPerLevel { get; protected set; }

        public int MaxLevel
        {
            get { return Ranks[Rank - 1].MaxLevel; }
        }
        public bool ImprovesOnUse { get; protected set; }

        public double CooldownLengthMod { get; set; }
        public double CooldownLength
        {
            get
            {
                double value = Ranks[Rank - 1].BaseCooldownLength + CooldownLengthMod;
                if (value > Ranks[Rank - 1].MaximumCooldownLength)
                    return Ranks[Rank - 1].MaximumCooldownLength;
                if (value < Ranks[Rank - 1].MinimumCooldownLength)
                    return Ranks[Rank - 1].MinimumCooldownLength;
                return value;
            }
        }
        public DateTime NextAvailableUse { get; set; }
        public bool CanUse
        {
            get { return DateTime.UtcNow > NextAvailableUse; }
        }

        public int MinimumDistance
        {
            get { return Ranks[Rank - 1].MinimumDistance; }
        }
        public int MaximumDistance
        {
            get { return Ranks[Rank - 1].MaximumDistance; }
        }

        public long SuccessRate
        {
            get { return Ranks[Rank - 1].SuccessRate; }
        }

        public int BodyAnimation { get; protected set; }

        public SkillTargetType Target { get; protected set; }
        public WeaponType RequiredWeapon { get; protected set; }
        public bool Attack { get; protected set; }

        public bool NpcCanUseOnAlly { get; protected set; }
        public bool NpcCanUseOnEnemy { get; protected set; }
        public bool PlayerCanUseOnAlly { get; protected set; }
        public bool PlayerCanUseOnEnemy { get; protected set; }

        public bool CanUseOnAliveTarget { get; protected set; }
        public bool CanUseOnDyingTarget { get; protected set; }
        public bool CanUseOnDeadTarget { get; protected set; }
        public bool CanUseOnComaTarget { get; protected set; }

        public bool CanUseFrozen { get; protected set; }
        public bool CanUseAsleep { get; protected set; }
        public bool CanUseHidden { get; protected set; }
        public bool CanUseAlive { get; protected set; }
        public bool CanUseDying { get; protected set; }
        public bool CanUseDead { get; protected set; }
        public bool CanUseInComa { get; protected set; }
        public bool CanUseInCombat { get; protected set; }
        public bool RequiresHidden { get; protected set; }

        public abstract void Invoke(Character c, Character target);

        public bool RequiresAdmin { get; protected set; }

        public int RequiredStr { get; protected set; }
        public int RequiredInt { get; protected set; }
        public int RequiredWis { get; protected set; }
        public int RequiredCon { get; protected set; }
        public int RequiredDex { get; protected set; }
        public Profession RequiredClass { get; protected set; }
        public Specialization RequiredSpecialization { get; protected set; }

        public string RequiredSkillA { get; protected set; }
        public int RequiredSkillARank { get; protected set; }

        public string RequiredSkillB { get; protected set; }
        public int RequiredSkillBRank { get; protected set; }

        public long RequiredGold { get; protected set; }
        public Dictionary<string, int> RequiredItems { get; protected set; }

        public string Description { get; protected set; }
        public string GuiDescription { get; protected set; }
        public string DialogDescription { get; protected set; }

        public Skill()
        {
            this.Name = string.Empty;
            this.Pane = SkillPane.Primary;
            this.Rank = 1;
            this.RequiredSkillA = "0";
            this.RequiredSkillB = "0";
            this.Description = string.Empty;
            this.CanUseOnAliveTarget = true;
            this.CanUseHidden = true;
            this.CanUseAlive = true;
            this.CanUseInCombat = true;
            this.Ranks = new SkillRank[1];
            this.Ranks[0] = new SkillRank();
            this.GuiDescription = string.Empty;
            this.DialogDescription = "poopfart";
            this.RequiredItems = new Dictionary<string, int>();
        }

        public override string ToString()
        {
            return string.Format("{0} (Lev:{1}/{2})", Name, Level, MaxLevel);
        }
    }
}