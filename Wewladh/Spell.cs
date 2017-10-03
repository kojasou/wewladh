using System;
using System.Collections.Generic;

namespace Wewladh
{
    public class SpellRank
    {
        public int RequiredLevel { get; set; }
        public int RequiredAbility { get; set; }
        public bool RequiresMaster { get; set; }
        public bool AutomaticRankUp { get; set; }
        public bool LearnFromNpc { get; set; }

        public int ManaCost { get; set; }
        public double ManaPercentage { get; set; }
        public int SpellPointCost { get; set; }

        public int SuccessRate { get; set; }

        public int MinimumDistance { get; set; }
        public int MaximumDistance { get; set; }
        public bool IncludeSelf { get; set; }

        public int BaseCastLines { get; set; }
        public int MinimumCastLines { get; set; }
        public int MaximumCastLines { get; set; }

        public double BaseCooldownLength { get; set; }
        public double MinimumCooldownLength { get; set; }
        public double MaximumCooldownLength { get; set; }

        public int Duration { get; set; }
        public int Speed { get; set; }

        public long RequiredGold { get; set; }
        public Dictionary<string, int> RequiredItems { get; set; }

        public int MaxLevel { get; set; }

        public SpellRank()
        {
            this.MinimumDistance = 0;
            this.MaximumDistance = 12;
            this.MaximumCastLines = 10;
            this.MinimumCooldownLength = 1;
            this.MaximumCooldownLength = double.MaxValue;
            this.Speed = 1000;
            this.RequiredItems = new Dictionary<string, int>();
        }
    }

    public abstract class Spell : GameObject
    {
        public string Text { get; protected set; }
        public ushort Icon { get; protected set; }
        public SpellPane Pane { get; protected set; }

        public int Slot { get; set; }
        public int Rank { get; set; }
        public int Level { get; set; }
        public SpellRank[] Ranks { get; protected set; }

        public int Uses { get; set; }
        public int UsesPerLevel { get; protected set; }

        public int MaxLevel
        {
            get { return Ranks[Rank - 1].MaxLevel; }
        }

        public bool RequiresCaster { get; protected set; }

        public long SuccessRate
        {
            get { return Ranks[Rank - 1].SuccessRate; }
        }
        
        public int MinimumDistance
        {
            get
            {
                return Ranks[Rank - 1].MinimumDistance;
            }
        }
        public int MaximumDistance
        {
            get
            {
                return Ranks[Rank - 1].MaximumDistance;
            }
        }
        public bool IncludeSelf
        {
            get
            {
                return Ranks[Rank - 1].IncludeSelf;
            }
        }

        public int CastLinesMod { get; set; }
        public int CastLines
        {
            get
            {
                int value = BaseCastLines + CastLinesMod;
                if (value > MaximumCastLines)
                    return MaximumCastLines;
                if (value < MinimumCastLines)
                    return MinimumCastLines;
                return value;
            }
        }
        public int BaseCastLines
        {
            get
            {
                return Ranks[Rank - 1].BaseCastLines;
            }
        }
        public int MaximumCastLines
        {
            get
            {
                return Ranks[Rank - 1].MaximumCastLines;
            }
        }
        public int MinimumCastLines
        {
            get
            {
                return Ranks[Rank - 1].MinimumCastLines;
            }
        }

        public string CooldownFamily { get; set; }
        public double CooldownLengthMod { get; set; }
        public double CooldownLength
        {
            get
            {
                double value = BaseCooldownLength + CooldownLengthMod;
                if (value > MaximumCooldownLength)
                    return MaximumCooldownLength;
                if (value < MinimumCooldownLength)
                    return MinimumCooldownLength;
                return value;
            }
        }
        public double BaseCooldownLength
        {
            get
            {
                return Ranks[Rank - 1].BaseCooldownLength;
            }
        }
        public double MaximumCooldownLength
        {
            get
            {
                return Ranks[Rank - 1].MaximumCooldownLength;
            }
        }
        public double MinimumCooldownLength
        {
            get
            {
                return Ranks[Rank - 1].MinimumCooldownLength;
            }
        }
        public DateTime NextAvailableUse { get; set; }
        public bool IgnoreGlobalCooldown { get; set; }
        public bool CanUse
        {
            get { return DateTime.UtcNow > NextAvailableUse; }
        }

        public int Duration
        {
            get
            {
                return Ranks[Rank - 1].Duration;
            }
        }
        public int Speed
        {
            get
            {
                return Ranks[Rank - 1].Speed;
            }
        }

        public int ManaCost
        {
            get
            {
                return Ranks[Rank - 1].ManaCost;
            }
        }
        public double ManaPercentage
        {
            get
            {
                return Ranks[Rank - 1].ManaPercentage;
            }
        }

        public int SpellPointCostMod { get; set; }
        public int SpellPointCost
        {
            get
            {
                int value = Ranks[Rank - 1].SpellPointCost + SpellPointCostMod;
                if (value < 0)
                    return 0;
                if (value > 10)
                    return 10;
                return value;
            }
        }

        public bool KeepMana { get; set; }

        public bool IsActive { get; set; }
        public Character Caster { get; set; }
        public Character Target { get; set; }
        public bool Aura { get; protected set; }
        public bool Channeled { get; protected set; }
        public string StatusName { get; protected set; }
        public string StatusFamily { get; protected set; }
        public bool ToggleStatus { get; protected set; }
        public bool ReplaceStatus { get; protected set; }
        public int TimeLeft { get; set; }
        public DateTime NextTick { get; set; }
        public Dictionary<string, string> Arguments { get; set; }
        public bool InstantTick { get; set; }
        public bool HasStatus
        {
            get { return !string.IsNullOrEmpty(StatusName); }
        }
        public bool CanTurnWithChannel { get; set; }
        public bool SingleTarget { get; protected set; }
        public bool PersistDeath { get; protected set; }
        public bool OnlyTickAlive { get; protected set; }

        public int BodyAnimation { get; protected set; }
        public int SpellAnimation { get; protected set; }
        public bool SingleAnimation { get; protected set; }
        public bool TileAnimation { get; protected set; }

        public SpellCastType CastType { get; protected set; }
        public SpellTargetType TargetType { get; protected set; }
        public bool Unfriendly { get; protected set; }
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
        public bool CanUseSilenced { get; protected set; }
        public bool CanUseAlive { get; protected set; }
        public bool CanUseDying { get; protected set; }
        public bool CanUseDead { get; protected set; }
        public bool CanUseInComa { get; protected set; }
        public bool CanUseInCombat { get; protected set; }
        public bool CanUseMorphed { get; protected set; }

        public virtual void Invoke(Character c, Character target, string args) { }
        public virtual Dictionary<string, string> GetStatusArguments(Character c, Character t)
        {
            return new Dictionary<string, string>();
        }
        public virtual void OnAdd(Character target) { }
        public virtual void OnRemove(Character target) { }
        public virtual void OnTick(Character c) { }

        public bool RequiresAdmin { get; protected set; }

        public int RequiredStr { get; protected set; }
        public int RequiredInt { get; protected set; }
        public int RequiredWis { get; protected set; }
        public int RequiredCon { get; protected set; }
        public int RequiredDex { get; protected set; }
        public Profession RequiredClass { get; protected set; }
        public Specialization RequiredSpecialization { get; protected set; }

        public string RequiredSpellA { get; protected set; }
        public int RequiredSpellARank { get; protected set; }

        public string RequiredSpellB { get; protected set; }
        public int RequiredSpellBRank { get; protected set; }

        public long RequiredGold { get; protected set; }
        public Dictionary<string, int> RequiredItems { get; protected set; }

        public string Description { get; protected set; }
        public string GuiDescription { get; protected set; }
        public string DialogDescription { get; protected set; }

        public Spell()
        {
            this.Name = String.Empty;
            this.Text = String.Empty;
            this.Rank = 1;
            this.Pane = SpellPane.Primary;
            this.RequiredSpellA = "0";
            this.RequiredSpellB = "0";
            this.Description = string.Empty;
            this.CanUseFrozen = false;
            this.CanUseAsleep = false;
            this.CanUseHidden = true;
            this.CanUseInCombat = true;
            this.CanUseAlive = true;
            this.CanUseDying = false;
            this.CanUseDead = false;
            this.CanUseInComa = false;
            this.CanUseOnAliveTarget = true;
            this.CanUseOnDyingTarget = false;
            this.CanUseOnDeadTarget = false;
            this.CanUseOnComaTarget = false;
            this.Ranks = new SpellRank[1];
            this.Ranks[0] = new SpellRank();
            this.Arguments = new Dictionary<string, string>();
            this.GuiDescription = string.Empty;
            this.DialogDescription = string.Empty;
            this.RequiredItems = new Dictionary<string, int>();
            this.OnlyTickAlive = true;
        }

        public override string ToString()
        {
            return string.Format("{0} (Lev:{1}/{2})", Name, Level, MaxLevel);
        }
    }
}