using System;
using System.Collections.Generic;

namespace Wewladh
{
    public delegate bool CharacterCondition(Character c);
    public delegate void CharacterMethod(Character c);
    public delegate void CharacterTargetMethod(Character c, Character t);

    public abstract class Character : VisibleObject
    {
        #region Properties
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public Direction Direction { get; set; }
        public int Radius { get; set; }

        public int ArenaTeam { get; set; }
        public Dictionary<string, Allegiance> AllegianceTable { get; private set; }

        public Character LastAttacker { get; set; }
        public HashSet<Character> Enemies { get; set; }
        public virtual bool InCombat
        {
            get
            {
                return (Enemies.Count > 0);
            }
        }
        public Dictionary<Character, double> Attackers { get; set; }
        public bool IsCasting { get; set; }

        public uint GroupID { get; set; }
        public Group Group
        {
            get
            {
                return GameServer.GameObject<Group>(GroupID);
            }
            set
            {
                if (value != null)
                    GroupID = value.ID;
                else
                    GroupID = 0;
            }
        }

        public Skill[] SkillBook { get; protected set; }
        public Spell[] SpellBook { get; protected set; }

        public DateTime LastHpRegen { get; set; }
        public DateTime LastMpRegen { get; set; }

        private LifeStatus lifeStatus;
        private uint deathMapID;
        public DateTime DeathTime { get; set; }
        public Map DeathMap
        {
            get
            {
                return GameServer.GameObject<Map>(deathMapID);
            }
            set
            {
                if (value != null)
                    deathMapID = value.ID;
                else
                    deathMapID = 0;
            }
        }
        public Point DeathPoint { get; set; }
        public LifeStatus LifeStatus
        {
            get { return lifeStatus; }
            set
            {
                lifeStatus = value;
                if (lifeStatus == LifeStatus.Dead)
                {
                    deathMapID = Map.ID;
                    DeathPoint.X = Point.X;
                    DeathPoint.Y = Point.Y;
                    DeathTime = DateTime.UtcNow;
                }
            }
        }
        public bool Alive
        {
            get { return lifeStatus == LifeStatus.Alive; }
        }
        public bool Dying
        {
            get { return lifeStatus == LifeStatus.Dying; }
        }
        public bool Dead
        {
            get { return lifeStatus == LifeStatus.Dead; }
        }
        public bool Coma
        {
            get { return lifeStatus == LifeStatus.Coma; }
        }

        public int Dizzies { get; set; }
        public bool Dizzy
        {
            get { return Dizzies > 0; }
        }

        public int Curses { get; set; }
        public bool Cursed
        {
            get { return Curses > 0; }
        }

        public int Hides { get; set; }
        public bool Hidden
        {
            get { return Hides > 0; }
        }

        public int Freezes { get; set; }
        public bool Frozen
        {
            get { return Freezes > 0; }
        }

        public int Sleeps { get; set; }
        public bool Sleeping
        {
            get { return Sleeps > 0; }
        }

        public int Confuses { get; set; }
        public bool Confused
        {
            get { return Confuses > 0; }
        }

        public int Silences { get; set; }
        public bool Silenced
        {
            get { return Silences > 0; }
        }

        public int Paralyzes { get; set; }
        public bool Paralyzed
        {
            get { return Paralyzes > 0; }
        }

        public bool Stealth { get; set; }

        public bool ConvertHealToDamage { get; set; }

        public bool Polymorphed { get; set; }
        public int PolymorphForm { get; set; }

        public Dictionary<string, Character> SingleTargetSpells { get; private set; }

        public bool MindControlled { get; set; }

        public Dictionary<string, Spell> Statuses { get; set; }

        public Reactor CurrentTrap { get; set; }

        public bool HasAura { get; set; }
        public Spell Aura { get; set; }
        public Character AuraOwner { get; set; }

        public bool AttackRangeOverrideActive { get; set; }
        public int AttackRangeOverride { get; set; }

        public bool AttackTargetOverrideActive { get; set; }
        public SkillTargetType AttackTargetOverride { get; set; }

        public bool ImpactActive { get; set; }
        public CharacterCondition CanUseImpact { get; set; }
        public CharacterMethod UseImpact { get; set; }
        public CharacterMethod EndImpact { get; set; }
        public CharacterTargetMethod InvokeImpact { get; set; }

        public double ThreatModifier { get; set; }
        public double SpellCostModifier { get; set; }

        public Character ThreatTransferTarget { get; set; }
        public double ThreatTransferPercent { get; set; }

        public bool ConvertingPhysicalDamageToManaDamage { get; set; }
        public double CurrentPhysicalDamageConvertedToManaDamage { get; set; }
        public double MaximumPhysicalDamageConvertedToManaDamage { get; set; }
        public string PhysicalConvertToManaStatusName { get; set; }
        public int PhysicalConvertToManaAnimation { get; set; }

        public bool ConvertingMagicalDamageToManaDamage { get; set; }
        public double CurrentMagicalDamageConvertedToManaDamage { get; set; }
        public double MaximumMagicalDamageConvertedToManaDamage { get; set; }
        public string MagicalConvertToManaStatusName { get; set; }
        public int MagicalConvertToManaAnimation { get; set; }

        public bool ConvertingAbsoluteDamageToManaDamage { get; set; }
        public double CurrentAbsoluteDamageConvertedToManaDamage { get; set; }
        public double MaximumAbsoluteDamageConvertedToManaDamage { get; set; }
        public string AbsoluteConvertToManaStatusName { get; set; }
        public int AbsoluteConvertToManaAnimation { get; set; }

        public bool AbsorbingPhysicalDamage { get; set; }
        public double CurrentPhysicalDamageAbsorbed { get; set; }
        public double MaximumPhysicalDamageAbsorbed { get; set; }
        public string PhysicalAbsorbStatusName { get; set; }
        public int PhysicalAbsorbAnimation { get; set; }

        public bool AbsorbingMagicalDamage { get; set; }
        public double CurrentMagicalDamageAbsorbed { get; set; }
        public double MaximumMagicalDamageAbsorbed { get; set; }
        public string MagicalAbsorbStatusName { get; set; }
        public int MagicalAbsorbAnimation { get; set; }

        public bool AbsorbingAbsoluteDamage { get; set; }
        public double CurrentAbsoluteDamageAbsorbed { get; set; }
        public double MaximumAbsoluteDamageAbsorbed { get; set; }
        public string AbsoluteAbsorbStatusName { get; set; }
        public int AbsoluteAbsorbAnimation { get; set; }

        public bool CounteringPhysicalDamage { get; set; }
        public double CurrentPhysicalDamageCountered { get; set; }
        public double MaximumPhysicalDamageCountered { get; set; }
        public string PhysicalCounterStatusName { get; set; }
        public int PhysicalCounterAnimation { get; set; }

        public bool CounteringMagicalDamage { get; set; }
        public double CurrentMagicalDamageCountered { get; set; }
        public double MaximumMagicalDamageCountered { get; set; }
        public string MagicalCounterStatusName { get; set; }
        public int MagicalCounterAnimation { get; set; }

        public bool CounteringAbsoluteDamage { get; set; }
        public double CurrentAbsoluteDamageCountered { get; set; }
        public double MaximumAbsoluteDamageCountered { get; set; }
        public string AbsoluteCounterStatusName { get; set; }
        public int AbsoluteCounterAnimation { get; set; }

        public bool RedirectingPhysicalDamage { get; set; }
        public double PhysicalRedirectPercent { get; set; }
        public int PhysicalRedirectCount { get; set; }
        public double CurrentPhysicalDamageRedirected { get; set; }
        public double MaximumPhysicalDamageRedirected { get; set; }
        public string PhysicalRedirectStatusName { get; set; }
        public Character PhysicalRedirectTarget { get; set; }
        public int PhysicalRedirectAnimation { get; set; }

        public bool RedirectingMagicalDamage { get; set; }
        public double MagicalRedirectPercent { get; set; }
        public int MagicalRedirectCount { get; set; }
        public double CurrentMagicalDamageRedirected { get; set; }
        public double MaximumMagicalDamageRedirected { get; set; }
        public string MagicalRedirectStatusName { get; set; }
        public Character MagicalRedirectTarget { get; set; }
        public int MagicalRedirectAnimation { get; set; }

        public Profession Class { get; set; }

        public Element OffenseElement { get; set; }
        public Element DefenseElement { get; set; }

        public long CurrentHP { get; set; }
        public long CurrentMP { get; set; }
        public long Level { get; set; }
        public long Ability { get; set; }
        public long Gold { get; set; }
        public long GamePoints { get; set; }
        public long Experience { get; set; }
        public long AbilityExp { get; set; }

        public long BaseMaximumHP { get; set; }
        public long BaseMaximumMP { get; set; }
        public long BaseStr { get; set; }
        public long BaseInt { get; set; }
        public long BaseWis { get; set; }
        public long BaseCon { get; set; }
        public long BaseDex { get; set; }
        public long BaseHit { get; set; }
        public long BaseDmg { get; set; }
        public long BaseArmorClass { get; set; }
        public long BaseMagicResistance { get; set; }
        public long BaseMinimumAttackPower { get; set; }
        public long BaseMaximumAttackPower { get; set; }
        public long BaseMinimumMagicPower { get; set; }
        public long BaseMaximumMagicPower { get; set; }

        public long MaximumHpMod { get; set; }
        public long MaximumMpMod { get; set; }
        public long StrMod { get; set; }
        public long IntMod { get; set; }
        public long WisMod { get; set; }
        public long ConMod { get; set; }
        public long DexMod { get; set; }
        public long HitMod { get; set; }
        public long DmgMod { get; set; }
        public long ArmorClassMod { get; set; }
        public long MagicResistanceMod { get; set; }
        public long MinimumAttackPowerMod { get; set; }
        public long MaximumAttackPowerMod { get; set; }
        public long MinimumMagicPowerMod { get; set; }
        public long MaximumMagicPowerMod { get; set; }

        public virtual uint MaximumHP
        {
            get
            {
                var value = (BaseMaximumHP + MaximumHpMod);
                if (value > uint.MaxValue)
                    return uint.MaxValue;
                if (value < uint.MinValue)
                    return uint.MinValue;
                return (uint)value;
            }
        }
        public virtual uint MaximumMP
        {
            get
            {
                var value = (BaseMaximumMP + MaximumMpMod);
                if (value > uint.MaxValue)
                    return uint.MaxValue;
                if (value < uint.MinValue)
                    return uint.MinValue;
                return (uint)value;
            }
        }
        public virtual ushort Str
        {
            get
            {
                if ((BaseStr + StrMod) > ushort.MaxValue)
                    return ushort.MaxValue;
                if ((BaseStr + StrMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseStr + StrMod);
            }
        }
        public virtual ushort Int
        {
            get
            {
                if ((BaseInt + IntMod) > ushort.MaxValue)
                    return ushort.MaxValue;
                if ((BaseInt + IntMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseInt + IntMod);
            }
        }
        public virtual ushort Wis
        {
            get
            {
                if ((BaseWis + WisMod) > ushort.MaxValue)
                    return ushort.MaxValue;
                if ((BaseWis + WisMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseWis + WisMod);
            }
        }
        public virtual ushort Con
        {
            get
            {
                if ((BaseCon + ConMod) > ushort.MaxValue)
                    return ushort.MaxValue;
                if ((BaseCon + ConMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseCon + ConMod);
            }
        }
        public virtual ushort Dex
        {
            get
            {
                if ((BaseDex + DexMod) > ushort.MaxValue)
                    return ushort.MaxValue;
                if ((BaseDex + DexMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseDex + DexMod);
            }
        }
        public virtual sbyte Hit
        {
            get
            {
                if ((BaseHit + HitMod) > sbyte.MaxValue)
                    return sbyte.MaxValue;
                if ((BaseHit + HitMod) < sbyte.MinValue)
                    return sbyte.MinValue;
                return (sbyte)(BaseHit + HitMod);
            }
        }
        public virtual sbyte Dmg
        {
            get
            {
                if ((BaseDmg + DmgMod) > sbyte.MaxValue)
                    return sbyte.MaxValue;
                if ((BaseDmg + DmgMod) < sbyte.MinValue)
                    return sbyte.MinValue;
                return (sbyte)(BaseDmg + DmgMod);
            }
        }
        public virtual sbyte ArmorClass
        {
            get
            {
                if ((BaseArmorClass + ArmorClassMod) > sbyte.MaxValue)
                    return sbyte.MaxValue;
                if ((BaseArmorClass + ArmorClassMod) < -100)
                    return -100;
                return (sbyte)(BaseArmorClass + ArmorClassMod);
            }
        }
        public virtual byte MagicResistance
        {
            get
            {
                if ((BaseMagicResistance + MagicResistanceMod) > 100)
                    return 100;
                if ((BaseMagicResistance + MagicResistanceMod) < byte.MinValue)
                    return byte.MinValue;
                return (byte)(BaseMagicResistance + MagicResistanceMod);
            }
        }
        public virtual double ArmorProtection
        {
            get
            {
                return (100 - ((200 - (ArmorClass + 100)) * 0.50)) / 100;
                //double armor = (100 - ArmorClass) * 35;
                //double level = (85 * Level);
                //return (1.0 - (armor / (armor + 400.0 + level)));
            }
        }
        public virtual int MinimumAttackPower
        {
            get
            {
                var value = (BaseMinimumAttackPower + MinimumAttackPowerMod);
                if (value < 1)
                    return 1;
                return (int)value;
            }
        }
        public virtual int MaximumAttackPower
        {
            get
            {
                var value = (BaseMaximumAttackPower + MaximumAttackPowerMod);
                if (value < 1)
                    return 1;
                return (int)value;
            }
        }
        public virtual int MinimumMagicPower
        {
            get
            {
                var value = (BaseMinimumMagicPower + MinimumMagicPowerMod);
                if (value < 1)
                    return 1;
                return (int)value;
            }
        }
        public virtual int MaximumMagicPower
        {
            get
            {
                var value = (BaseMaximumMagicPower + MaximumMagicPowerMod);
                if (value < 1)
                    return 1;
                return (int)value;
            }
        }

        public double PhysicalProtection { get; set; }
        public double MagicalProtection { get; set; }
        #endregion

        public string Faction { get; set; }

        public virtual bool CanWalkThroughWalls { get; set; }
        public virtual bool CanWalkThroughUnits { get; set; }

        public Character()
        {
            this.SingleTargetSpells = new Dictionary<string, Character>();
            this.Name = string.Empty;
            this.Statuses = new Dictionary<string, Spell>(StringComparer.CurrentCultureIgnoreCase);
            this.Point = new Point(0, 0);
            this.DeathPoint = new Point(0, 0);
            this.LastHpRegen = DateTime.UtcNow;
            this.LastMpRegen = DateTime.UtcNow;
            this.Enemies = new HashSet<Character>();
            this.Attackers = new Dictionary<Character, double>();
            this.SkillBook = new Skill[0];
            this.SpellBook = new Spell[0];
            this.SpellCostModifier = 1;
            this.ThreatModifier = 1;
            this.Faction = string.Empty;
            this.AllegianceTable = new Dictionary<string, Allegiance>();
        }

        public override void OnGameServerInsert(GameServer gs)
        {
            this.Group = new Group(gs, string.Empty, this);
        }

        public virtual void OnSkilled(Character c, Skill s)
        {

        }
        public virtual void OnSpelled(Character c, Spell s)
        {

        }

        public double GetAttackPower()
        {
            double attackPower = Program.Random(MinimumAttackPower, MaximumAttackPower + 1);
            return attackPower * 0.25 * (Level + 3);
        }
        public double GetMagicPower()
        {
            double magicPower = Program.Random(MinimumMagicPower, MaximumMagicPower + 1);
            return magicPower * 0.25 * (Level + 3);
        }

        public double HealthPercentage
        {
            get
            {
                return (double)CurrentHP / (double)MaximumHP;
            }
        }

        public virtual void AddLegendMark(string type, params string[] args) { }
        public virtual void RemoveLegendMark(string type) { }

        public virtual bool AddItem(Item item) { return false; }
        public virtual bool AddItem(Item item, int index) { return false; }

        public abstract void OnClick(Client client);
        public abstract bool Walk(Direction direction);
        public abstract bool Walk(Direction direction, bool focefully);
        public abstract void UseSkill(Skill s);
        public abstract void UseSpell(Spell s, Character target, string args);
        public abstract void UseSpell(Spell s, Character target, string args, int duration);
        public abstract void Channel(Spell s);
        public virtual Allegiance AllegianceTo(Character c)
        {
            return Allegiance.Neutral;
        }
        public void SetAllegiance(string faction, Allegiance allegiance)
        {
            if (GameServer.Factions.ContainsKey(faction))
            {
                if (AllegianceTable.ContainsKey(faction))
                    AllegianceTable[faction] = allegiance;
                else
                    AllegianceTable.Add(faction, allegiance);
            }
        }

        public bool CanTarget(Skill skill, Character character)
        {
            return CanTarget(skill, character, Direction);
        }
        public bool CanTarget(Skill skill, Character character, Direction direction)
        {
            if (!WithinRange(character, skill.MinimumDistance, skill.MaximumDistance))
                return false;

            int xoffset, yoffset;

            switch (direction)
            {
                case Direction.North: xoffset = 0; yoffset = -1; break;
                case Direction.South: xoffset = 0; yoffset = 1; break;
                case Direction.West: xoffset = -1; yoffset = 0; break;
                case Direction.East: xoffset = 1; yoffset = 0; break;
                default: return false;
            }

            switch (skill.Target)
            {
                case SkillTargetType.Cone:
                    {
                        for (int i = skill.MinimumDistance - 1; i < skill.MaximumDistance; i++)
                        {
                            int x = Point.X + (xoffset * (i + 1));
                            int y = Point.Y + (yoffset * (i + 1));
                            if (x == character.Point.X && y == character.Point.Y)
                                return true;
                            for (int j = 0; j < i; j++)
                            {
                                int x1 = x + (yoffset * (j + 1));
                                int x2 = x - (yoffset * (j + 1));
                                int y1 = y + (xoffset * (j + 1));
                                int y2 = y - (xoffset * (j + 1));
                                if (x1 == character.Point.X && y1 == character.Point.Y)
                                    return true;
                                if (x2 == character.Point.X && y2 == character.Point.Y)
                                    return true;
                            }
                        }

                        return false;
                    }
                case SkillTargetType.Facing:
                    {
                        for (int i = skill.MinimumDistance - 1; i < skill.MaximumDistance; i++)
                        {
                            int x = Point.X + (xoffset * (i + 1));
                            int y = Point.Y + (yoffset * (i + 1));
                            if (x < 0 || y < 0 || x >= Map.Width || y >= Map.Height)
                                continue;
                            if (x == character.Point.X && y == character.Point.Y)
                                return true;
                            if (Map.Walls[x, y] || Map.Block[x, y])
                                return false;
                        }

                        return false;
                    }
                case SkillTargetType.FirstFacing:
                    {
                        for (int i = 0; i < skill.MaximumDistance; i++)
                        {
                            int x = Point.X + (xoffset * (i + 1));
                            int y = Point.Y + (yoffset * (i + 1));

                            if (x == character.Point.X && y == character.Point.Y)
                                return true;

                            if (x < 0 || y < 0 || x >= Map.Width || y >= Map.Height)
                                return false;

                            if (Map.Walls[x, y] || Map.Block[x, y] || Map.Tiles[x, y].Weight > 0)
                                return false;
                        }

                        return false;
                    }
                case SkillTargetType.Surrounding:
                    {
                        return WithinRange(character, skill.MinimumDistance, skill.MaximumDistance);
                    }
            }

            return false;
        }
        public Direction TargetDirection(Skill skill, Character character)
        {
            int start = (int)Direction;
            for (int i = 0; i < 4; i++)
            {
                var direction = (Direction)((start + i) % 4);
                if (CanTarget(skill, character, direction))
                    return direction;
            }
            return Direction.None;
        }

        public bool CanUseSkill(Skill s)
        {
            if (DateTime.UtcNow < s.NextAvailableUse)
                return false;

            if (Polymorphed)
                return false;

            if (InCombat && !s.CanUseInCombat)
                return false;

            if (Alive && !s.CanUseAlive)
                return false;

            if (Dying && !s.CanUseDying)
                return false;

            if (Dead && !s.CanUseDead)
                return false;

            if (Coma && !s.CanUseInComa)
                return false;

            if (Frozen && !s.CanUseFrozen)
                return false;

            if (Sleeping && !s.CanUseAsleep)
                return false;

            if (Hidden && !s.CanUseHidden)
                return false;

            return true;
        }
        public bool CanUseSpell(Spell s)
        {
            if (DateTime.UtcNow < s.NextAvailableUse.AddSeconds(s.CastLines))
                return false;

            if (Polymorphed && !s.CanUseMorphed)
                return false;

            if (Silenced && !s.CanUseSilenced)
                return false;

            if (InCombat && !s.CanUseInCombat)
                return false;

            if (Alive && !s.CanUseAlive)
                return false;

            if (Dying && !s.CanUseDying)
                return false;

            if (Dead && !s.CanUseDead)
                return false;

            if (Coma && !s.CanUseInComa)
                return false;

            if (Frozen && !s.CanUseFrozen)
                return false;

            if (Sleeping && !s.CanUseAsleep)
                return false;

            if (Hidden && !s.CanUseHidden)
                return false;

            int manaCost = (int)(BaseMaximumMP * s.ManaPercentage + s.ManaCost);

            if (CurrentMP < manaCost)
                return false;

            return true;
        }

        public virtual bool Heal(Character healer, double amount, int sound = 255)
        {
            if (LifeStatus != LifeStatus.Alive)
                return false;

            if (amount > uint.MaxValue)
                amount = uint.MaxValue;

            if (amount < uint.MinValue)
                amount = uint.MinValue;

            if (ConvertHealToDamage)
                amount *= -1;

            CurrentHP += (long)amount;

            if (CurrentHP > MaximumHP)
                CurrentHP = MaximumHP;

            if (CurrentHP < 0)
                CurrentHP = 0;

            var percent = Math.Floor((double)CurrentHP / (double)MaximumHP * 100.0);

            if (percent > 100)
                percent = 100;

            if (percent < 0)
                percent = 0;

            foreach (Character c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                {
                    var packet = new ServerPacket(0x13);
                    packet.WriteUInt32((healer == null) ? ID : healer.ID);
                    packet.WriteUInt32(ID);
                    packet.WriteByte(0x00);
                    packet.WriteByte((byte)percent);
                    packet.WriteUInt32((uint)(0 - amount));
                    packet.WriteByte((byte)sound);
                    (c as Player).Client.Enqueue(packet);
                }
            }
            return true;
        }
        public virtual void Damage(double dmg, Character attacker = null, int sound = 0, DamageType damageType = DamageType.RawDamage, DamageFlags flags = DamageFlags.None)
        {
            if (LifeStatus != LifeStatus.Alive)
                return;

            RemoveStatus("Morph");

            var realDamage = dmg;

            if ((flags & DamageFlags.CanBeAbsorbed) == DamageFlags.CanBeAbsorbed)
            {
                if (AbsorbingAbsoluteDamage)
                {
                    CurrentAbsoluteDamageAbsorbed += dmg;
                    if (MaximumAbsoluteDamageAbsorbed <= CurrentAbsoluteDamageAbsorbed)
                        RemoveStatus("AbsoluteAbsorb");
                    realDamage = 0;
                    SpellAnimation(AbsoluteAbsorbAnimation, 100);
                }
                else if (AbsorbingPhysicalDamage && damageType == DamageType.Physical)
                {
                    CurrentPhysicalDamageAbsorbed += dmg;
                    if (MaximumPhysicalDamageAbsorbed <= CurrentPhysicalDamageAbsorbed)
                        RemoveStatus("PhysicalAbsorb");
                    realDamage = 0;
                    SpellAnimation(PhysicalAbsorbAnimation, 100);
                }
                else if (AbsorbingMagicalDamage && damageType == DamageType.Magical)
                {
                    CurrentMagicalDamageAbsorbed += dmg;
                    if (MaximumMagicalDamageAbsorbed <= CurrentMagicalDamageAbsorbed)
                        RemoveStatus("MagicalAbsorb");
                    realDamage = 0;
                    SpellAnimation(MagicalAbsorbAnimation, 100);
                }
            }

            if (damageType == DamageType.Physical)
            {
                realDamage *= ArmorProtection * (1d - PhysicalProtection);
            }
            else if (damageType == DamageType.Magical)
            {
                realDamage -= (realDamage * (MagicResistance / 100d));
                realDamage *= (1d - MagicalProtection);
            }

            if (Map.Flags.HasFlag(MapFlags.PlayerKill))
                dmg *= 0.75;

            if (attacker != null)
            {
                if (!Enemies.Contains(attacker))
                    Enemies.Add(attacker);
                if (!attacker.Enemies.Contains(this))
                    attacker.Enemies.Add(this);
                LastAttacker = attacker;
            }

            if ((flags & DamageFlags.CanBeRedirected) == DamageFlags.CanBeRedirected)
            {
                if (RedirectingPhysicalDamage && damageType == DamageType.Physical)
                {
                    if (PhysicalRedirectTarget != null && PhysicalRedirectTarget != this && WithinRange(PhysicalRedirectTarget, 12))
                    {
                        var yourDamage = realDamage * PhysicalRedirectPercent;
                        realDamage -= yourDamage;
                        PhysicalRedirectTarget.Damage(yourDamage, null, 0, DamageType.RawDamage, DamageFlags.None);
                    }
                    if (--PhysicalRedirectCount == 0)
                        RemoveStatus("PhysicalRedirect");
                }

                if (RedirectingMagicalDamage && damageType == DamageType.Magical)
                {
                    if (MagicalRedirectTarget != null && MagicalRedirectTarget != this && WithinRange(MagicalRedirectTarget, 12))
                    {
                        var yourDamage = realDamage * MagicalRedirectPercent;
                        realDamage -= yourDamage;
                        MagicalRedirectTarget.Damage(yourDamage, null, 0, DamageType.RawDamage, DamageFlags.None);
                    }
                    if (--MagicalRedirectCount == 0)
                        RemoveStatus("MagicalRedirect");
                }
            }

            if ((flags & DamageFlags.CanBeConvertedToManaDamage) == DamageFlags.CanBeConvertedToManaDamage)
            {
                if (ConvertingPhysicalDamageToManaDamage && damageType == DamageType.Physical)
                {
                    CurrentMP -= (long)realDamage;
                    if (CurrentMP < 0)
                        CurrentMP = 0;
                    CurrentPhysicalDamageConvertedToManaDamage += dmg;
                    if (MaximumPhysicalDamageConvertedToManaDamage <= CurrentPhysicalDamageConvertedToManaDamage)
                        RemoveStatus("PhysicalConvertToMana");
                    realDamage = 0;
                    SpellAnimation(PhysicalConvertToManaAnimation, 100);
                }
                if (ConvertingMagicalDamageToManaDamage && damageType == DamageType.Magical)
                {
                    CurrentMP -= (long)realDamage;
                    if (CurrentMP < 0)
                        CurrentMP = 0;
                    CurrentMagicalDamageConvertedToManaDamage += dmg;
                    if (MaximumMagicalDamageConvertedToManaDamage <= CurrentPhysicalDamageConvertedToManaDamage)
                        RemoveStatus("MagicalConvertToMana");
                    realDamage = 0;
                    SpellAnimation(MagicalConvertToManaAnimation, 100);
                }
            }

            CurrentHP -= (long)realDamage;

            if (CurrentHP < 0)
                CurrentHP = 0;

            if (attacker != null)
            {
                if (Attackers.ContainsKey(attacker))
                    Attackers[attacker] += realDamage;
                else
                    Attackers.Add(attacker, realDamage);
            }

            double percent = Math.Floor((double)CurrentHP / (double)MaximumHP * 100.0);

            if (percent < 0)
                percent = 0;

            if (percent > 100)
                percent = 100;

            var dot = (flags & DamageFlags.DamageOverTime) == DamageFlags.DamageOverTime;
            var id = (attacker != null) ? attacker.ID : ID;

            foreach (Character c in Map.Objects)
            {
                if (WithinRange(c, 12) && (c is Player))
                {
                    var packet = new ServerPacket(0x13);
                    packet.WriteUInt32(id);
                    packet.WriteUInt32(ID);
                    packet.WriteByte(dot);
                    packet.WriteByte((byte)percent);
                    packet.WriteUInt32((uint)realDamage);
                    packet.WriteByte((byte)sound);
                    (c as Player).Client.Enqueue(packet);
                }
            }
        }

        public void Threaten(Monster npc, double amount)
        {
            if (AllegianceTo(npc) != Allegiance.Enemy)
                return;

            amount *= ThreatModifier;

            if (WithinRange(ThreatTransferTarget, 12))
            {
                double transferAmount = amount * ThreatTransferPercent;
                amount -= transferAmount;

                if (npc.ThreatMeter.ContainsKey(ThreatTransferTarget))
                    npc.ThreatMeter[ThreatTransferTarget] += transferAmount;
                else
                    npc.ThreatMeter.Add(ThreatTransferTarget, transferAmount);

                npc.Enemies.Add(ThreatTransferTarget);
                ThreatTransferTarget.Enemies.Add(npc);
            }

            if (npc.ThreatMeter.ContainsKey(this))
                npc.ThreatMeter[this] += amount;
            else
                npc.ThreatMeter.Add(this, amount);

            Enemies.Add(npc);
            npc.Enemies.Add(this);
        }

        public virtual bool AddStatus(string typeName, int rank = 0, int timeLeft = 0, Character caster = null, Character target = null, Dictionary<string, string> args = null, bool instantTick = false)
        {
            var s = GameServer.CreateSpell(typeName);

            if (s == null || Statuses.ContainsKey(s.StatusName))
                return false;

            if (s.RequiresCaster && caster == null)
                return false;

            s.NextTick = DateTime.UtcNow.AddMilliseconds(s.Speed);

            if (timeLeft > 0)
                s.TimeLeft = timeLeft;

            if (rank > 0)
                s.Rank = rank;
            
            if (args != null)
                s.Arguments = args;

            s.Caster = caster;
            s.Target = target;

            Statuses.Add(s.StatusName, s);
            s.OnAdd(this);

            if (instantTick)
                s.OnTick(this);

            return true;
        }
        public virtual bool RemoveStatus(string status)
        {
            if (Statuses.ContainsKey(status))
            {
                var s = Statuses[status];
                Statuses.Remove(status);
                s.OnRemove(this);
                if (s.SingleTarget)
                {
                    s.Caster.SingleTargetSpells.Remove(status);
                }
                return true;
            }

            return false;
        }
        public virtual void RemoveStatusFamily(string family)
        {
            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.StatusFamily == family)
                {
                    Statuses.Remove(status.StatusName);
                    status.OnRemove(this);
                }
            }
        }
        public virtual bool IsFacing(Character c, int range)
        {
            if (Map != c.Map || (c.Point.X != Point.X && c.Point.Y != Point.Y))
                return false;

            for (int i = 0; i < range; i++)
            {
                int x = Point.X + (XOffset * (i + 1));
                int y = Point.Y + (YOffset * (i + 1));
                var t = Map[x, y];
                if (t != null && c.Point.X == t.Point.X && c.Point.Y == t.Point.Y)
                    return true;
            }

            return false;
        }

        public virtual void Say(string msg, int type)
        {
            foreach (var c in Map.Objects)
            {
                if (c is Player && (WithinRange(c, 12) || type == 1))
                {
                    var player = c as Player;
                    var packet = new ServerPacket(0x0D);
                    packet.WriteByte((byte)type);
                    packet.WriteUInt32(ID);
                    switch (type)
                    {
                        case 0: packet.WriteString8("{0}: {1}", Name, msg); break;
                        case 1: packet.WriteString8("{0}! {1}", Name, msg); break;
                        default: packet.WriteString8(msg); break;
                    }
                    player.Client.Enqueue(packet);
                }
            }
        }
        public virtual bool Turn(Direction direction)
        {
            return Turn(direction, false);
        }
        public virtual bool Turn(Direction direction, bool forcefully)
        {
            if (!forcefully && (Sleeping || Frozen || Coma || Dizzy || MindControlled || Polymorphed))
                return false;

            Direction = direction;

            switch (Direction)
            {
                case Direction.North: XOffset = 0; YOffset = -1; break;
                case Direction.South: XOffset = 0; YOffset = 1; break;
                case Direction.West: XOffset = -1; YOffset = 0; break;
                case Direction.East: XOffset = 1; YOffset = 0; break;
            }

            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                {
                    var p = new ServerPacket(0x11);
                    p.WriteUInt32(ID);
                    p.WriteByte((byte)direction);
                    (c as Player).Client.Enqueue(p);
                }
            }

            return true;
        }
        public virtual void BodyAnimation(int animation, int speed)
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                    (c as Player).Client.BodyAnimation(ID, animation, speed);
            }
        }
        public virtual void SpellAnimation(int animation, int speed)
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                    (c as Player).Client.SpellAnimation(ID, animation, 100);
            }
        }
        public virtual void SpellAnimation(int animation, int speed, Character from, int fromAnimation)
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                    (c as Player).Client.SpellAnimation(ID, animation, 100, from.ID, fromAnimation);
            }
        }
        public virtual void SoundEffect(int sound)
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
                    (c as Player).Client.SoundEffect(sound);
            }
        }
        public virtual void Hide()
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player && c != this)
                    (c as Player).Client.RemoveCharacter(ID);
            }
        }

        public virtual void SendMessage(string message, byte type = 3)
        {

        }
        public virtual void UpdateStatistics(StatUpdateFlags flags = StatUpdateFlags.Full)
        {

        }
    }
}