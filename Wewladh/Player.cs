using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public delegate bool PlayerCondition(Player p);
    public delegate void PlayerMethod(Player p);

    public class QueuedFunction
    {
        public PlayerMethod Method { get; private set; }
        public PlayerCondition Condition { get; private set; }
        public QueuedFunction(PlayerMethod method, PlayerCondition condition)
        {
            this.Method = method;
            this.Condition = condition;
        }
    }

    public class Player : Character
    {
        public Dictionary<long, long> AbilityPointCounter { get; private set; }
        public Dictionary<long, long> AbilityPointMultiplier { get; private set; }

        #region Properties
        public bool OppositeGenderHair { get; set; }
        public int EmblemA { get; set; }
        public int EmblemB { get; set; }
        public Occupation Occupation { get; set; }
        public bool Loaded { get; set; }
        public Item Paper { get; set; }
        public int AccountID { get; set; }
        public Time Time { get; set; }
        public int CurrentMusic { get; set; }
        public bool Online { get; set; }
        public long LastSessionID { get; set; }
        public string SecureKey { get; set; }
        public AdminRights AdminRights { get; set; }
        public Client Client { get; set; }
        public bool Master { get; set; }
        public long ToNextLevel
        {
            get { return GameServer.ExperienceTable[Level]; }
        }
        public long ToThisLevel
        {
            get { return GameServer.ExperienceTable[Level - 1]; }
        }
        public long ToNextAbility
        {
            get { return GameServer.AbilityExpTable[Ability]; }
        }
        public long ToThisAbility
        {
            get { return GameServer.AbilityExpTable[Ability - 1]; }
        }
        public int AvailableStatPoints { get; set; }
        public Specialization Specialization { get; set; }
        public Gender Sex { get; set; }
        public string Nation { get; set; }
        public string GuildName { get; set; }
        public Guild Guild
        {
            get
            {
                if (GameServer.Guilds.ContainsKey(GuildName))
                    return GameServer.Guilds[GuildName];
                else
                    return null;
            }
            set
            {
                if (value != null)
                    GuildName = value.Name;
                else
                    GuildName = string.Empty;
            }
        }
        public GuildRank GuildRank { get; set; }
        public Dictionary<string, LegendMark> Legend { get; set; }
        public bool GroupToggle { get; set; }
        public int Status { get; set; }
        public DateTime LastMessage { get; set; }
        public DateTime LastWalkCounterReset { get; set; }
        public DateTime LastRefresh { get; set; }
        public DateTime LastRest { get; set; }
        public DateTime LastSnore { get; set; }
        public int WalkCounter { get; set; }
        public int NextSpellLines { get; set; }
        public DateTime StartCastTime { get; set; }
        public DialogSession DialogSession { get; set; }
        public Exchange ExchangeInfo { get; set; }
        public DateTime LastSave { get; set; }
        public Dictionary<string, string> Cookies { get; set; }
        public bool Resting { get; set; }
        public int RestPosition { get; set; }
        public uint LastInstanceID { get; set; }
        public DateTime NextInstanceTime { get; set; }
        public bool MapOpen { get; set; }
        public DateTime NextMapUpdate { get; set; }
        public WorldMap WorldMap { get; set; }
        public int Title { get; set; }
        public Dictionary<string, DateTime> ItemCooldowns { get; set; }
        public List<QueuedFunction> QueuedFunctions { get; private set; }
        public string MiniGameItem { get; set; }
        public ushort DisplayBitmask { get; set; }
        public int AvailableBagSlots { get; set; }
        public HashSet<string> VisitedMaps { get; private set; }

        public Manufacture CurrentManufacture { get; set; }
        public DateTime ManufactureStart { get; set; }
        public HashSet<string> CurrentManufactures { get; private set; }
        public HashSet<string> AvailableManufactures { get; private set; }

        public int SkillPoints { get; set; }
        public int SpellPoints { get; set; }
        public DateTime LastSkillPointRegen { get; set; }
        public DateTime LastSpellPointRegen { get; set; }

        public double ExperienceBonus { get; set; }
        public int ExperienceBonusChance { get; set; }

        private bool locked = false;
        public bool Locked
        {
            get
            {
                return locked;
            }
            set
            {
                locked = value;
                if (locked)
                    Client.SendPlayerID(uint.MaxValue);
                else
                    Client.SendPlayerID();
            }
        }

        public Item[] BagItems { get; set; }
        public Item[] Inventory { get; set; }
        public List<Item> BankItems { get; set; }
        public Equipment[] Equipment { get; set; }
        public List<Parcel> Parcels { get; set; }
        public Dictionary<string, Quest> Quests { get; set; }

        public int HairStyle { get; set; }
        public int HairColor { get; set; }
        public int FaceStyle { get; set; }
        public int FaceColor { get; set; }
        public int BodyStyle
        {
            get { return (Sex == Gender.Male) ? 16 : 32; }
        }
        public int BodyColor { get; set; }

        public int MaximumWeight
        {
            get
            {
                return (int)(BaseStr + (Level / 4) + 48);
            }
        }
        public int CurrentWeight { get; set; }

        public int AvailableWeight
        {
            get { return (MaximumWeight - CurrentWeight); }
        }

        public override uint MaximumHP
        {
            get
            {
                long value = BaseMaximumHP + MaximumHpMod + ((BaseCon - 5) * 50);
                if (value > 99999999)
                    return 99999999;
                if (value < 1)
                    return 1;
                return (uint)value;
            }
        }
        public override uint MaximumMP
        {
            get
            {
                long value = BaseMaximumMP + MaximumMpMod + ((BaseWis - 5) * 50);
                if (value > 99999999)
                    return 99999999;
                if (value < uint.MinValue)
                    return uint.MinValue;
                return (uint)value;
            }
        }
        public override ushort Str
        {
            get
            {
                if ((BaseStr + StrMod) > 999)
                    return 999;
                if ((BaseStr + StrMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseStr + StrMod);
            }
        }
        public override ushort Int
        {
            get
            {
                if ((BaseInt + IntMod) > 999)
                    return 999;
                if ((BaseInt + IntMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseInt + IntMod);
            }
        }
        public override ushort Wis
        {
            get
            {
                if ((BaseWis + WisMod) > 999)
                    return 999;
                if ((BaseWis + WisMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseWis + WisMod);
            }
        }
        public override ushort Con
        {
            get
            {
                if ((BaseCon + ConMod) > 999)
                    return 999;
                if ((BaseCon + ConMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseCon + ConMod);
            }
        }
        public override ushort Dex
        {
            get
            {
                if ((BaseDex + DexMod) > 999)
                    return 999;
                if ((BaseDex + DexMod) < ushort.MinValue)
                    return ushort.MinValue;
                return (ushort)(BaseDex + DexMod);
            }
        }
        public override int MinimumAttackPower
        {
            get
            {
                var value = (BaseMinimumAttackPower + MinimumAttackPowerMod);
                if (Class == Profession.Monk)
                    value += 25;
                return (value < 1) ? 1 : (int)value;
            }
        }
        public override int MaximumAttackPower
        {
            get
            {
                var value = (BaseMaximumAttackPower + MaximumAttackPowerMod);
                if (Class == Profession.Monk)
                    value += 25;
                return (value < 1) ? 1 : (int)value;
            }
        }
        public override int MinimumMagicPower
        {
            get
            {
                var value = (BaseMinimumMagicPower + MinimumMagicPowerMod);
                if (Class == Profession.Monk)
                    value += 25;
                return (value < 1) ? 1 : (int)value;
            }
        }
        public override int MaximumMagicPower
        {
            get
            {
                var value = (BaseMaximumMagicPower + MaximumMagicPowerMod);
                if (Class == Profession.Monk)
                    value += 25;
                return (value < 1) ? 1 : (int)value;
            }
        }
        #endregion

        #region Equipment Pointers
        public Weapon Weapon
        {
            get { return (Equipment[0] as Weapon); }
            set { Equipment[0] = value; }
        }
        public Armor Armor
        {
            get { return (Equipment[1] as Armor); }
            set { Equipment[1] = value; }
        }
        public Shield Shield
        {
            get { return (Equipment[2] as Shield); }
            set { Equipment[2] = value; }
        }
        public Helmet Helmet
        {
            get { return (Equipment[3] as Helmet); }
            set { Equipment[3] = value; }
        }
        public Earring Earring
        {
            get { return (Equipment[4] as Earring); }
            set { Equipment[4] = value; }
        }
        public Necklace Necklace
        {
            get { return (Equipment[5] as Necklace); }
            set { Equipment[5] = value; }
        }
        public Ring LeftRing
        {
            get { return (Equipment[6] as Ring); }
            set { Equipment[6] = value; }
        }
        public Ring RightRing
        {
            get { return (Equipment[7] as Ring); }
            set { Equipment[7] = value; }
        }
        public Gauntlet LeftGauntlet
        {
            get { return (Equipment[8] as Gauntlet); }
            set { Equipment[8] = value; }
        }
        public Gauntlet RightGauntlet
        {
            get { return (Equipment[9] as Gauntlet); }
            set { Equipment[9] = value; }
        }
        public Belt Belt
        {
            get { return (Equipment[10] as Belt); }
            set { Equipment[10] = value; }
        }
        public Greaves Greaves
        {
            get { return (Equipment[11] as Greaves); }
            set { Equipment[11] = value; }
        }
        public Boots Boots
        {
            get { return (Equipment[12] as Boots); }
            set { Equipment[12] = value; }
        }
        public Accessory AccessoryA
        {
            get { return (Equipment[13] as Accessory); }
            set { Equipment[13] = value; }
        }
        public Overcoat Overcoat
        {
            get { return (Equipment[14] as Overcoat); }
            set { Equipment[14] = value; }
        }
        public EventHelm EventHelm
        {
            get { return (Equipment[15] as EventHelm); }
            set { Equipment[15] = value; }
        }
        public Accessory AccessoryB
        {
            get { return (Equipment[21] as Accessory); }
            set { Equipment[21] = value; }
        }
        public DisplayHelm DisplayHelm
        {
            get { return (Equipment[22] as DisplayHelm); }
            set { Equipment[22] = value; }
        }
        public DisplayWeapon DisplayWeapon
        {
            get { return (Equipment[23] as DisplayWeapon); }
            set { Equipment[23] = value; }
        }
        public DisplayBoots DisplayBoots
        {
            get { return (Equipment[24] as DisplayBoots); }
            set { Equipment[24] = value; }
        }
        public BackAccessory BackAccessory
        {
            get { return (Equipment[25] as BackAccessory); }
            set { Equipment[25] = value; }
        }
        public Cape Cape
        {
            get { return (Equipment[26] as Cape); }
            set { Equipment[26] = value; }
        }
        public Effect Effect
        {
            get { return (Equipment[27] as Effect); }
            set { Equipment[27] = value; }
        }
        #endregion

        public override bool CanWalkThroughWalls
        {
            get
            {
                return AdminRights.HasFlag(AdminRights.CanWalkThroughWalls) || (Map.Flags.HasFlag(MapFlags.ArenaTeam) && AdminRights.HasFlag(AdminRights.ArenaHost) && ArenaTeam == 0);
            }
        }
        public override bool CanWalkThroughUnits
        {
            get
            {
                return AdminRights.HasFlag(AdminRights.CanWalkThroughUnits) || (Map.Flags.HasFlag(MapFlags.ArenaTeam) && AdminRights.HasFlag(AdminRights.ArenaHost) && ArenaTeam == 0);
            }
        }

        public Player(GameServer gs, string name)
        {
            this.Name = name;
            this.GameServer = gs;
            this.Inventory = new Item[59];
            this.BankItems = new List<Item>();
            this.SkillBook = new Skill[90];
            this.SpellBook = new Spell[90];
            this.Equipment = new Equipment[28];
            this.BagItems = new Item[60];
            this.Parcels = new List<Parcel>();
            this.Quests = new Dictionary<string, Quest>();
            this.Legend = new Dictionary<string, LegendMark>();
            this.DialogSession = new DialogSession();
            this.Nation = string.Empty;
            this.Cookies = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            this.GuildName = string.Empty;
            this.NextInstanceTime = DateTime.UtcNow;
            this.WorldMap = new WorldMap(string.Empty);
            this.GroupToggle = true;
            this.ItemCooldowns = new Dictionary<string, DateTime>();
            this.QueuedFunctions = new List<QueuedFunction>();
            this.CurrentManufactures = new HashSet<string>();
            this.AvailableManufactures = new HashSet<string>();
            this.VisitedMaps = new HashSet<string>();
            this.AbilityPointCounter = new Dictionary<long, long>();
            this.AbilityPointMultiplier = new Dictionary<long, long>();
        }

        public override void Update()
        {
            var statuses = new string[Statuses.Count];
            Statuses.Keys.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (Statuses.ContainsKey(status))
                {
                    var s = Statuses[status];

                    if (s.RequiresCaster && s.Caster == null)
                    {
                        RemoveStatus(status);
                    }
                    else if (DateTime.UtcNow > s.NextTick)
                    {
                        if (s.Channeled)
                        {
                            Channel(s);
                        }
                        else if (Alive || !s.OnlyTickAlive)
                        {
                            s.OnTick(this);
                            if (s.SpellAnimation != 0 && !s.SingleAnimation)
                                SpellAnimation(s.SpellAnimation, 100);
                        }
                        if (--s.TimeLeft < 1)
                        {
                            RemoveStatus(status);
                        }
                        s.NextTick = DateTime.UtcNow.AddMilliseconds(s.Speed);
                    }
                }
            }

            var enemies = new Character[Enemies.Count];
            Enemies.CopyTo(enemies, 0);
            foreach (var e in enemies)
            {
                if (e == null || e.Dead || !WithinRange(e, 12))
                {
                    Enemies.Remove(e);
                }
            }

            if (CurrentManufacture != null && DateTime.UtcNow.Subtract(ManufactureStart).TotalSeconds > 5)
            {
                if (CurrentManufacture.CanManufacture(this))
                {
                    CurrentManufacture.ManufactureItem(this);
                    var index = SkillBook.IndexOf(CurrentManufacture.SkillName);
                    var skill = SkillBook[index];
                    if (skill.Level < skill.MaxLevel && skill.Level <= CurrentManufacture.MaximumLevel && (skill.Level < CurrentManufacture.MediumLevel || Program.Random(2) == 0))
                    {
                        skill.Level++;
                        DisplaySkill(skill);
                        Client.SendMessage("{0} is now level {1}!", skill.Name, skill.Level);
                    }
                }
                CurrentManufacture = null;
                var packet = new ServerPacket(0x51);
                packet.WriteByte(0x01);
                packet.WriteByte(0x00);
                packet.WriteByte(0x00);
                Client.Enqueue(packet);
            }

            var queuedFunctions = new QueuedFunction[QueuedFunctions.Count];
            QueuedFunctions.CopyTo(queuedFunctions);
            foreach (var qf in queuedFunctions)
            {
                if (qf.Condition(this))
                {
                    qf.Method(this);
                    QueuedFunctions.Remove(qf);
                }
            }

            if (Time != GameServer.Time)
            {
                Time = GameServer.Time;
                var packet = new ServerPacket(0x20);
                packet.WriteByte((byte)Time);
                packet.WriteByte(0x00);
                Client.Enqueue(packet);
            }

            if (Group.HasMembers && Aura == null)
            {
                foreach (var member in Group.Members)
                {
                    if (member.Aura != null && member.AuraOwner == member && WithinRange(member, member.Aura.MaximumDistance))
                    {
                        AuraOwner = member;
                        var aura = member.Aura;
                        if (AddStatus(aura.TypeName, aura.Rank, aura.TimeLeft, this, null, null, true))
                        {
                            SpellAnimation(aura.SpellAnimation, 100);
                            member.SpellAnimation(aura.SpellAnimation, 100);
                        }
                    }
                }
            }

            if (Aura != null && (AuraOwner == null || !WithinRange(AuraOwner, Aura.MaximumDistance) || AuraOwner.Aura == null))
                RemoveStatus(Aura.StatusName);

            if (Aura != null && !Statuses.ContainsKey(Aura.StatusName))
                Aura = null;

            if (DateTime.UtcNow > NextMapUpdate && Group.HasMembers && MapOpen)
            {
                var packet = new ServerPacket(0x63);
                packet.WriteByte(0x06); // ??
                packet.WriteByte((byte)Group.Members.Count);
                foreach (var member in Group.Members)
                {
                    packet.WriteString8(member.Name);
                    packet.WriteString8(member.Map.Name);
                    packet.WriteUInt16((ushort)member.Point.X);
                    packet.WriteUInt16((ushort)member.Point.Y);
                }
                packet.WriteByte(0x00);
                Client.Enqueue(packet);
                NextMapUpdate = DateTime.UtcNow.AddSeconds(1);
            }

            if (DateTime.UtcNow.Subtract(LastWalkCounterReset).TotalSeconds > 0.15)
            {
                if (WalkCounter > 0)
                    WalkCounter--;
                LastWalkCounterReset = DateTime.UtcNow;
            }

            if (Alive && CurrentHP <= 0)
            {
                if (Map.Flags.HasFlag(MapFlags.ShouldComa))
                {
                    CurrentHP = 1;
                    AddStatus("Spell_Coma", 1, 20, this);
                    Client.SendStatistics(StatUpdateFlags.Current);
                }
                else
                {
                    CurrentHP = 0;
                    LifeStatus = LifeStatus.Dying;
                    Client.SendStatistics(StatUpdateFlags.Current);
                }
            }

            switch (LifeStatus)
            {
                case LifeStatus.Alive:
                    {

                    } break;
                case LifeStatus.Coma:
                    {

                    } break;
                case LifeStatus.Dying:
                    {
                        #region Remove Statuses
                        statuses = new string[Statuses.Count];
                        Statuses.Keys.CopyTo(statuses, 0);
                        foreach (var s in statuses)
                        {
                            RemoveStatus(s);
                        }
                        #endregion

                        #region Remove Enemies
                        enemies = new Character[Enemies.Count];
                        Enemies.CopyTo(enemies, 0);
                        foreach (var e in enemies)
                        {
                            Enemies.Remove(e);
                            e.Enemies.Remove(this);
                        }
                        #endregion

                        #region Remove Equipment
                        var equipment = new Equipment[Equipment.Length];
                        Equipment.CopyTo(equipment, 0);
                        foreach (var item in equipment)
                        {
                            if (item != null && item.DropOnDeath)
                            {
                                RemoveEquipment(item);
                                Map.InsertCharacter(item, Point.X, Point.Y);
                            }
                        }
                        #endregion

                        Resting = false;
                        LifeStatus = LifeStatus.Dead;
                        Display();

                        if (ExchangeInfo != null)
                            CancelExchange();

                        #region Punishments and AP Rewards
                        if (Map.Flags.HasFlag(MapFlags.PlayerKill))
                        {
                            if (LastAttacker != null && LastAttacker.Map == Map && LastAttacker is Player)
                            {
                                var player = LastAttacker as Player;
                                if (!AbilityPointCounter.ContainsKey(player.GUID))
                                    AbilityPointCounter.Add(player.GUID, 15);
                                if (!AbilityPointMultiplier.ContainsKey(player.GUID))
                                    AbilityPointMultiplier.Add(player.GUID, player.Ability);
                                var counter = AbilityPointCounter[player.GUID];
                                var multiplier = AbilityPointMultiplier[player.GUID];
                                if (counter > 0)
                                {
                                    player.RewardAbilityExp(5 * multiplier);
                                    if (multiplier > 1)
                                        AbilityPointMultiplier[player.GUID] = multiplier - 1;
                                    AbilityPointCounter[player.GUID] = counter - 1;
                                }
                            }

                            if (LastAttacker == null)
                            {
                                Map.BroadcastMessage("{0} has fallen in battle.", Name);
                            }
                            else
                            {
                                Map.BroadcastMessage("{0} has fallen in battle to {1}.", Name, LastAttacker.Name);
                            }
                        }

                        if (Map.Flags.HasFlag(MapFlags.SendToHell))
                        {
                            var deathMapName = Map.DeathMapName;
                            var deathMapPoint = Map.DeathMapPoint;

                            Map.RemoveCharacter(this);
                            GameServer.MapDatabase[deathMapName].InsertCharacter(this, deathMapPoint);
                        }

                        if (GameServer.GlobalScript != null)
                        {
                            var dialog = GameServer.GlobalScript.OnDeath(this);
                            GiveDialog(this, dialog);
                        }
                        #endregion
                    } break;
                case LifeStatus.Dead:
                    {

                    } break;
            }

            if (DateTime.UtcNow.Subtract(LastSkillPointRegen).TotalSeconds > 1)
            {
                SkillPoints = 10;
                LastSkillPointRegen = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(LastSpellPointRegen).TotalSeconds > 1)
            {
                SpellPoints = 10;
                LastSpellPointRegen = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(LastHpRegen).TotalSeconds > (Resting ? 1 : 5))
            {
                if (Alive && CurrentHP < MaximumHP)
                {
                    CurrentHP += (long)(MaximumHP * (InCombat ? 0.01 : 0.05));
                    if (CurrentHP > MaximumHP)
                        CurrentHP = MaximumHP;
                    Client.SendStatistics(StatUpdateFlags.Current);
                }
                LastHpRegen = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(LastMpRegen).TotalSeconds > (Resting ? 1 : 5))
            {
                if (Alive && CurrentMP < MaximumMP)
                {
                    CurrentMP += (long)(MaximumMP * (InCombat ? 0.01 : 0.05));
                    if (CurrentMP > MaximumMP)
                        CurrentMP = MaximumMP;
                    Client.SendStatistics(StatUpdateFlags.Current);
                }
                LastMpRegen = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(Client.LastPacket).TotalSeconds > 90)
            {
                if (DateTime.UtcNow.Subtract(LastSnore).TotalSeconds > 7.5)
                {
                    BodyAnimation(16, 120);
                    LastSnore = DateTime.UtcNow;
                }
            }
        }
        public override void OnClick(Client sender)
        {
            var p = new ServerPacket(0x34);
            p.WriteUInt32(ID);
            if (Weapon != null)
            {
                p.WriteUInt16((ushort)(Weapon.Sprite + 0x8000));
                p.WriteUInt16((byte)Weapon.Color);
                p.WriteString8(Weapon.GetType().Name);
                p.WriteString8(Weapon.Name);
                p.WriteUInt32((uint)Weapon.CurrentDurability);
                p.WriteUInt32((uint)Weapon.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Armor != null)
            {
                p.WriteUInt16((ushort)(Armor.Sprite + 0x8000));
                p.WriteUInt16((byte)Armor.Color);
                p.WriteString8(Armor.GetType().Name);
                p.WriteString8(Armor.Name);
                p.WriteUInt32((uint)Armor.CurrentDurability);
                p.WriteUInt32((uint)Armor.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Shield != null)
            {
                p.WriteUInt16((ushort)(Shield.Sprite + 0x8000));
                p.WriteUInt16((byte)Shield.Color);
                p.WriteString8(Shield.GetType().Name);
                p.WriteString8(Shield.Name);
                p.WriteUInt32((uint)Shield.CurrentDurability);
                p.WriteUInt32((uint)Shield.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Helmet != null)
            {
                p.WriteUInt16((ushort)(Helmet.Sprite + 0x8000));
                p.WriteUInt16((byte)Helmet.Color);
                p.WriteString8(Helmet.GetType().Name);
                p.WriteString8(Helmet.Name);
                p.WriteUInt32((uint)Helmet.CurrentDurability);
                p.WriteUInt32((uint)Helmet.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Earring != null)
            {
                p.WriteUInt16((ushort)(Earring.Sprite + 0x8000));
                p.WriteUInt16((byte)Earring.Color);
                p.WriteString8(Earring.GetType().Name);
                p.WriteString8(Earring.Name);
                p.WriteUInt32((uint)Earring.CurrentDurability);
                p.WriteUInt32((uint)Earring.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Necklace != null)
            {
                p.WriteUInt16((ushort)(Necklace.Sprite + 0x8000));
                p.WriteUInt16((byte)Necklace.Color);
                p.WriteString8(Necklace.GetType().Name);
                p.WriteString8(Necklace.Name);
                p.WriteUInt32((uint)Necklace.CurrentDurability);
                p.WriteUInt32((uint)Necklace.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (LeftRing != null)
            {
                p.WriteUInt16((ushort)(LeftRing.Sprite + 0x8000));
                p.WriteUInt16((byte)LeftRing.Color);
                p.WriteString8(LeftRing.GetType().Name);
                p.WriteString8(LeftRing.Name);
                p.WriteUInt32((uint)LeftRing.CurrentDurability);
                p.WriteUInt32((uint)LeftRing.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (RightRing != null)
            {
                p.WriteUInt16((ushort)(RightRing.Sprite + 0x8000));
                p.WriteUInt16((byte)RightRing.Color);
                p.WriteString8(RightRing.GetType().Name);
                p.WriteString8(RightRing.Name);
                p.WriteUInt32((uint)RightRing.CurrentDurability);
                p.WriteUInt32((uint)RightRing.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (LeftGauntlet != null)
            {
                p.WriteUInt16((ushort)(LeftGauntlet.Sprite + 0x8000));
                p.WriteUInt16((byte)LeftGauntlet.Color);
                p.WriteString8(LeftGauntlet.GetType().Name);
                p.WriteString8(LeftGauntlet.Name);
                p.WriteUInt32((uint)LeftGauntlet.CurrentDurability);
                p.WriteUInt32((uint)LeftGauntlet.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (RightGauntlet != null)
            {
                p.WriteUInt16((ushort)(RightGauntlet.Sprite + 0x8000));
                p.WriteUInt16((byte)RightGauntlet.Color);
                p.WriteString8(RightGauntlet.GetType().Name);
                p.WriteString8(RightGauntlet.Name);
                p.WriteUInt32((uint)RightGauntlet.CurrentDurability);
                p.WriteUInt32((uint)RightGauntlet.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Belt != null)
            {
                p.WriteUInt16((ushort)(Belt.Sprite + 0x8000));
                p.WriteUInt16((byte)Belt.Color);
                p.WriteString8(Belt.GetType().Name);
                p.WriteString8(Belt.Name);
                p.WriteUInt32((uint)Belt.CurrentDurability);
                p.WriteUInt32((uint)Belt.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Greaves != null)
            {
                p.WriteUInt16((ushort)(Greaves.Sprite + 0x8000));
                p.WriteUInt16((byte)Greaves.Color);
                p.WriteString8(Greaves.GetType().Name);
                p.WriteString8(Greaves.Name);
                p.WriteUInt32((uint)Greaves.CurrentDurability);
                p.WriteUInt32((uint)Greaves.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (AccessoryA != null)
            {
                p.WriteUInt16((ushort)(AccessoryA.Sprite + 0x8000));
                p.WriteUInt16((byte)AccessoryA.Color);
                p.WriteString8(AccessoryA.GetType().Name);
                p.WriteString8(AccessoryA.Name);
                p.WriteUInt32((uint)AccessoryA.CurrentDurability);
                p.WriteUInt32((uint)AccessoryA.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Boots != null)
            {
                p.WriteUInt16((ushort)(Boots.Sprite + 0x8000));
                p.WriteUInt16((byte)Boots.Color);
                p.WriteString8(Boots.GetType().Name);
                p.WriteString8(Boots.Name);
                p.WriteUInt32((uint)Boots.CurrentDurability);
                p.WriteUInt32((uint)Boots.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Overcoat != null)
            {
                p.WriteUInt16((ushort)(Overcoat.Sprite + 0x8000));
                p.WriteUInt16((byte)Overcoat.Color);
                p.WriteString8(Overcoat.GetType().Name);
                p.WriteString8(Overcoat.Name);
                p.WriteUInt32((uint)Overcoat.CurrentDurability);
                p.WriteUInt32((uint)Overcoat.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (AccessoryB != null)
            {
                p.WriteUInt16((ushort)(AccessoryB.Sprite + 0x8000));
                p.WriteUInt16((byte)AccessoryB.Color);
                p.WriteString8(AccessoryB.GetType().Name);
                p.WriteString8(AccessoryB.Name);
                p.WriteUInt32((uint)AccessoryB.CurrentDurability);
                p.WriteUInt32((uint)AccessoryB.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (DisplayHelm != null)
            {
                p.WriteUInt16((ushort)(DisplayHelm.Sprite + 0x8000));
                p.WriteUInt16((byte)DisplayHelm.Color);
                p.WriteString8(DisplayHelm.GetType().Name);
                p.WriteString8(DisplayHelm.Name);
                p.WriteUInt32((uint)DisplayHelm.CurrentDurability);
                p.WriteUInt32((uint)DisplayHelm.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (DisplayWeapon != null)
            {
                p.WriteUInt16((ushort)(DisplayWeapon.Sprite + 0x8000));
                p.WriteUInt16((byte)DisplayWeapon.Color);
                p.WriteString8(DisplayWeapon.GetType().Name);
                p.WriteString8(DisplayWeapon.Name);
                p.WriteUInt32((uint)DisplayWeapon.CurrentDurability);
                p.WriteUInt32((uint)DisplayWeapon.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (DisplayBoots != null)
            {
                p.WriteUInt16((ushort)(DisplayBoots.Sprite + 0x8000));
                p.WriteUInt16((byte)DisplayBoots.Color);
                p.WriteString8(DisplayBoots.GetType().Name);
                p.WriteString8(DisplayBoots.Name);
                p.WriteUInt32((uint)DisplayBoots.CurrentDurability);
                p.WriteUInt32((uint)DisplayBoots.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (BackAccessory != null)
            {
                p.WriteUInt16((ushort)(BackAccessory.Sprite + 0x8000));
                p.WriteUInt16((byte)BackAccessory.Color);
                p.WriteString8(BackAccessory.GetType().Name);
                p.WriteString8(BackAccessory.Name);
                p.WriteUInt32((uint)BackAccessory.CurrentDurability);
                p.WriteUInt32((uint)BackAccessory.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Cape != null)
            {
                p.WriteUInt16((ushort)(Cape.Sprite + 0x8000));
                p.WriteUInt16((byte)Cape.Color);
                p.WriteString8(Cape.GetType().Name);
                p.WriteString8(Cape.Name);
                p.WriteUInt32((uint)Cape.CurrentDurability);
                p.WriteUInt32((uint)Cape.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            if (Effect != null)
            {
                p.WriteUInt16((ushort)(Effect.Sprite + 0x8000));
                p.WriteUInt16((byte)Effect.Color);
                p.WriteString8(Effect.GetType().Name);
                p.WriteString8(Effect.Name);
                p.WriteUInt32((uint)Effect.CurrentDurability);
                p.WriteUInt32((uint)Effect.MaximumDurability);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            else
            {
                p.WriteUInt16(0x00);
                p.WriteUInt16(0x00);
                p.WriteString8(string.Empty);
                p.WriteString8(string.Empty);
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt32(uint.MinValue);
                p.Write(new byte[] { 0, 0, 0, 0, 0 });
            }
            p.WriteByte((byte)Status);
            p.WriteString8(Name);
            p.WriteByte(GameServer.NationDatabase[Nation].Flag);
            p.WriteByte((byte)Title); // title
            p.WriteByte(GroupToggle);
            p.WriteString8((Guild != null) ? GuildRank.ToString() : string.Empty);
            p.WriteString8(string.Format("{0}{1}{2}",
                Master ? "Master " : string.Empty,
                (Specialization != Specialization.None) ? Specialization + " " : string.Empty,
                Class));
            p.WriteString8((Guild != null) ? Guild.Name : string.Empty);
            p.WriteByte((byte)Legend.Count);
            foreach (var kvp in Legend.OrderBy(l => l.Value.DateUpdated))
            {
                p.WriteByte((byte)kvp.Value.Icon);
                p.WriteByte((byte)kvp.Value.Color);
                p.WriteString8(kvp.Value.Key);
                p.WriteString8(kvp.Value.ToString());
            }
            p.WriteUInt16(0x00); // ??
            p.WriteUInt32(0x00); // ??
            p.WriteUInt32(0x00); // ??
            p.WriteUInt32(0x00); // ??
            p.WriteByte(0x00); // profile
            p.WriteByte(0x00); // ??
            sender.Enqueue(p);
        }
        public override void Display()
        {
            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && (c is Player))
                    DisplayTo(c);
            }
        }
        public override void DisplayTo(VisibleObject obj)
        {
            /*
             * helm2:   0001
             * armor2:  0002
             * weapon2: 0004
             * boots2:  0008
             * armor1:  0010
             * cape:    0020
             * boots1:  0040
             * ground:  0080
             * back:    0100
             * acc2:    0200
             * acc1:    0400
             * helm:    0800
             */

            if (obj is Player)
            {
                var player = (obj as Player);
                var client = (obj as Player).Client;

                if (Stealth && !player.AdminRights.HasFlag(AdminRights.CanStealth))
                    return;

                var p = new ServerPacket(0x33);
                p.WriteUInt16((ushort)Point.X);
                p.WriteUInt16((ushort)Point.Y);
                p.WriteByte((byte)Direction);
                p.WriteUInt32((uint)ID);
                p.WriteByte(Cursed);
                p.WriteByte(0x00); // ??
                p.WriteUInt16(DisplayBitmask);

                #region Dead
                if (Dead)
                {
                    p.WriteUInt16((ushort)HairStyle);
                    p.WriteByte((byte)(BodyStyle + 32));
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteByte(0);
                    p.WriteByte(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt32(0);
                    p.WriteUInt32(0);
                    p.WriteByte(0);
                }
                #endregion

                #region Hidden
                else if (Hidden || Stealth)
                {
                    p.WriteUInt16(0x00);
                    if (player == this || player.AdminRights.HasFlag(AdminRights.CanStealth))
                        p.WriteByte(0x50);
                    else
                        p.WriteByte(0x00);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteByte(0);
                    p.WriteByte(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                    p.WriteUInt32(0);
                    p.WriteUInt32(0);
                    p.WriteByte(0);
                }
                #endregion

                #region Form
                else if (Polymorphed)
                {
                    p.WriteUInt16(ushort.MaxValue);
                    p.WriteUInt16((ushort)(PolymorphForm + 0x4000));
                    p.WriteUInt32(uint.MinValue);
                    p.WriteUInt32(uint.MinValue);
                }
                #endregion

                #region Form
                else if (Sprite > 0)
                {
                    p.WriteUInt16(ushort.MaxValue);
                    p.WriteUInt16((ushort)(Sprite + 0x4000));
                    p.WriteUInt32(uint.MinValue);
                    p.WriteUInt32(uint.MinValue);
                }
                #endregion

                #region Normal
                else
                {
                    if (EventHelm != null)
                    {
                        p.WriteUInt16((ushort)EventHelm.DisplayImage);
                    }
                    else if (Map.Flags.HasFlag(MapFlags.PlayerKill))
                    {
                        p.WriteUInt16((ushort)HairStyle);
                    }
                    else if (DisplayHelm != null && (DisplayBitmask & 0x0001) == 0x0001)
                    {
                        p.WriteUInt16((ushort)DisplayHelm.DisplayImage);
                    }
                    else if (Helmet != null && (DisplayBitmask & 0x0800) == 0x0800)
                    {
                        p.WriteUInt16((ushort)Helmet.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16((ushort)HairStyle);
                    }
                    
                    if (Armor != null && (DisplayBitmask & 0x0010) == 0x0010)
                    {
                        p.WriteByte((byte)(BodyStyle + Armor.BodyStyle));
                    }
                    else
                    {
                        p.WriteByte((byte)BodyStyle);
                    }

                    if (AdminRights.HasFlag(AdminRights.ArenaHost) && Map.Flags.HasFlag(MapFlags.ArenaTeam) && ArenaTeam == 0)
                    {
                        p.WriteUInt16(262);
                    }
                    else if (Armor != null && (DisplayBitmask & 0x0010) == 0x0010)
                    {
                        p.WriteUInt16((ushort)Armor.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AdminRights.HasFlag(AdminRights.ArenaHost) && Map.Flags.HasFlag(MapFlags.ArenaTeam) && ArenaTeam == 0)
                    {
                        p.WriteUInt16(12);
                    }
                    else if (DisplayBoots != null && (DisplayBitmask & 0x0008) == 0x0008)
                    {
                        p.WriteUInt16((ushort)DisplayBoots.DisplayImage);
                    }
                    else if (Boots != null && (DisplayBitmask & 0x0040) == 0x0040)
                    {
                        p.WriteUInt16((ushort)Boots.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AdminRights.HasFlag(AdminRights.ArenaHost) && Map.Flags.HasFlag(MapFlags.ArenaTeam) && ArenaTeam == 0)
                    {
                        p.WriteUInt16(262);
                    }
                    else if (Armor != null && (DisplayBitmask & 0x0010) == 0x0010)
                    {
                        p.WriteUInt16((ushort)Armor.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Shield != null)
                    {
                        p.WriteUInt16((ushort)Shield.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(ushort.MaxValue);
                    }

                    if (DisplayWeapon != null && (DisplayBitmask & 0x0004) == 0x0004)
                    {
                        p.WriteUInt16((ushort)DisplayWeapon.DisplayImage);
                    }
                    else if (Weapon != null)
                    {
                        p.WriteUInt16((ushort)Weapon.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (DisplayWeapon != null && (DisplayBitmask & 0x0004) == 0x0004)
                    {
                        p.WriteUInt16((ushort)DisplayWeapon.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Map.Flags.HasFlag(MapFlags.ArenaTeam))
                    {
                        if (ArenaTeam == ushort.MinValue)
                        {
                            if (AdminRights.HasFlag(AdminRights.ArenaHost))
                                p.WriteUInt16(69);
                            else
                                p.WriteUInt16(15);
                        }
                        else
                        {
                            p.WriteUInt16((ushort)ArenaTeam);
                        }
                    }
                    else if (EventHelm != null)
                    {
                        p.WriteUInt16((ushort)EventHelm.Color);
                    }
                    else if (DisplayHelm != null && (DisplayBitmask & 0x0001) == 0x0001)
                    {
                        p.WriteUInt16((ushort)DisplayHelm.Color);
                    }
                    else
                    {
                        p.WriteUInt16((ushort)HairColor);
                    }

                    if (DisplayBoots != null && (DisplayBitmask & 0x0008) == 0x0008)
                    {
                        p.WriteUInt16((ushort)DisplayBoots.Color);
                    }
                    else if (Boots != null && (DisplayBitmask & 0x0040) == 0x0040)
                    {
                        p.WriteUInt16((ushort)Boots.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AccessoryA != null && (DisplayBitmask & 0x0400) == 0x0400)
                    {
                        p.WriteUInt16((ushort)AccessoryA.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AccessoryA != null && (DisplayBitmask & 0x0400) == 0x0400)
                    {
                        p.WriteUInt16((ushort)AccessoryA.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    p.WriteByte(0); // lantern

                    if (Resting)
                    {
                        p.WriteByte((byte)RestPosition);
                    }
                    else
                    {
                        p.WriteByte(0);
                    }

                    if (Overcoat != null && (DisplayBitmask & 0x0002) == 0x0002)
                    {
                        p.WriteUInt16((ushort)Overcoat.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Overcoat != null && (DisplayBitmask & 0x0002) == 0x0002)
                    {
                        p.WriteUInt16((ushort)Overcoat.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AccessoryB != null && (DisplayBitmask & 0x0200) == 0x0200)
                    {
                        p.WriteUInt16((ushort)AccessoryB.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (AccessoryB != null && (DisplayBitmask & 0x0200) == 0x0200)
                    {
                        p.WriteUInt16((ushort)AccessoryB.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (BackAccessory != null && (DisplayBitmask & 0x0100) == 0x0100)
                    {
                        p.WriteUInt16((ushort)BackAccessory.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (BackAccessory != null && (DisplayBitmask & 0x0100) == 0x0100)
                    {
                        p.WriteUInt16((ushort)BackAccessory.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Cape != null && (DisplayBitmask & 0x0020) == 0x0020)
                    {
                        p.WriteUInt16((ushort)Cape.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Map.Flags.HasFlag(MapFlags.ArenaTeam))
                    {
                        p.WriteUInt16((ushort)ArenaTeam);
                    }
                    else if (Cape != null && (DisplayBitmask & 0x0020) == 0x0020)
                    {
                        p.WriteUInt16((ushort)Cape.Color);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    if (Effect != null && (DisplayBitmask & 0x0080) == 0x0080)
                    {
                        p.WriteUInt16((ushort)Effect.DisplayImage);
                    }
                    else
                    {
                        p.WriteUInt16(0);
                    }

                    p.WriteByte(0x00); // ??
                    p.WriteByte((byte)FaceStyle); // face style
                    p.WriteByte(0x00); // ??
                    p.WriteByte(0x00); // 0x01 when body colored
                    p.WriteUInt16((byte)FaceColor); // face color
                    p.WriteUInt16((byte)BodyColor); // body color
                    if (OppositeGenderHair)
                    {
                        p.WriteByte((byte)(Sex == Gender.Male ? 0x03 : 0x04));
                    }
                    else
                    {
                        p.WriteByte(0x02); // opposite gender hair
                    }
                }
                #endregion

                if (Group.HasMembers && Group.Members.Contains(player))
                {
                    p.WriteByte(0x05);
                }
                else if (Guild != null && Guild.Members.Contains(player.Name))
                {
                    p.WriteByte(0x03);
                }
                else
                {
                    p.WriteByte(0x00);
                }

                p.WriteString8(Name);
                p.WriteString8(string.Empty); // grp name
                p.WriteByte(0x07); // ??
                p.WriteUInt16((ushort)EmblemA);
                p.WriteUInt16((ushort)EmblemB);
                client.Enqueue(p);
            }
        }
        public override bool Walk(Direction direction)
        {
            return Walk(direction, false);
        }
        public override bool Walk(Direction direction, bool forcefully)
        {
            if (Map.Walls[Point.X, Point.Y] && !CanWalkThroughWalls)
                return false;

            bool dizzy = false;

            int newXOffset = 0, newYOffset = 0;
            Point oldPoint = new Point(Point.X, Point.Y);
            Point newPoint = new Point(Point.X, Point.Y);

            if (Dizzy && (direction != Direction))
            {
                dizzy = true;
                direction = Direction;
            }

            switch (direction)
            {
                case Direction.North: newPoint.Y--; newXOffset = 0; newYOffset = -1; break;
                case Direction.South: newPoint.Y++; newXOffset = 0; newYOffset = 1; break;
                case Direction.West: newPoint.X--; newXOffset = -1; newYOffset = 0; break;
                case Direction.East: newPoint.X++; newXOffset = 1; newYOffset = 0; break;
                default: return false;
            }

            if (!forcefully && (Sleeping || Paralyzed || Frozen || Coma || MindControlled || Polymorphed || DialogSession.IsOpen))
                return false;

            if ((newPoint.X < 0) || (newPoint.Y < 0) || (newPoint.X >= Map.Width) || (newPoint.Y >= Map.Height))
                return false;

            if (Map.Walls[newPoint.X, newPoint.Y] && !CanWalkThroughWalls)
                return false;

            if (Map[newPoint.X, newPoint.Y].Weight > 0 && !CanWalkThroughUnits)
                return false;

            if (Map.Warps[newPoint.X, newPoint.Y] != null)
            {
                var mapWarp = Map.Warps[newPoint.X, newPoint.Y];
                if (Level < mapWarp.MinimumLevel && !AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                {
                    Client.SendMessage("You are too low level to enter.");
                    return false;
                }
                if (Level > mapWarp.MaximumLevel && !AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                {
                    Client.SendMessage("You are too high level to enter.");
                    return false;
                }
            }

            Map[oldPoint.X, oldPoint.Y].Objects.Remove(this);
            Point = newPoint;
            Direction = direction;
            XOffset = newXOffset;
            YOffset = newYOffset;
            Map[newPoint.X, newPoint.Y].Objects.Insert(0, this);

            var p1 = new ServerPacket(0x0B);
            p1.WriteByte((byte)direction);
            p1.WriteUInt16((ushort)oldPoint.X);
            p1.WriteUInt16((ushort)oldPoint.Y);
            p1.WriteUInt16(0x0B);
            p1.WriteUInt16(0x0B);
            p1.WriteByte(0x01);
            Client.Enqueue(p1);

            var p2 = new ServerPacket(0x32);
            p2.WriteByte(0x00);
            Client.Enqueue(p2);

            if (dizzy || locked || forcefully)
            {
                var p3 = new ServerPacket(0x0C);
                p3.WriteUInt32((uint)ID);
                p3.WriteUInt16((ushort)oldPoint.X);
                p3.WriteUInt16((ushort)oldPoint.Y);
                p3.WriteByte((byte)direction);
                p3.WriteByte(0x00);
                Client.Enqueue(p3);
            }

            if (Resting)
            {
                Resting = false;
                Display();
            }

            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    RemoveStatus(status.StatusName);
            }

            foreach (var c in Map.Objects)
            {
                if (c != this)
                {
                    if (c.Point.DistanceFrom(oldPoint) > 12 && c.Point.DistanceFrom(newPoint) <= 12)
                    {
                        c.DisplayTo(this);
                        this.DisplayTo(c);
                    }

                    if (c.Point.DistanceFrom(oldPoint) <= 12 || c.Point.DistanceFrom(newPoint) <= 12)
                    {
                        if (c is Player)
                        {
                            var p = new ServerPacket(0x0C);
                            p.WriteUInt32((uint)ID);
                            p.WriteUInt16((ushort)oldPoint.X);
                            p.WriteUInt16((ushort)oldPoint.Y);
                            p.WriteByte((byte)direction);
                            p.WriteByte(0x00);
                            (c as Player).Client.Enqueue(p);
                        }
                    }

                    if (c.Point.DistanceFrom(oldPoint) <= 12 && c.Point.DistanceFrom(newPoint) > 12)
                    {
                        if (c is Player)
                            (c as Player).Client.RemoveCharacter(ID);
                        Client.RemoveCharacter(c.ID);
                    }
                }
            }

            //for (int x = 0; x < Map.Width; x++)
            //{
            //    for (int y = 0; y < Map.Height; y++)
            //    {
            //        if (Map.Doors[x, y] != null)
            //            Client.ToggleDoor(Map.Doors[x, y]);
            //    }
            //}

            var mapID = Map.ID;

            foreach (var r in Map.Tiles[newPoint.X, newPoint.Y].Objects)
            {
                var reactor = r as Reactor;
                if (reactor != null && reactor.Alive)
                {
                    var dialog = reactor.OnWalkover(this);
                    reactor.GiveDialog(this, dialog);
                    break;
                }
            }

            if (mapID == Map.ID && Map.Warps[Point.X, Point.Y] != null)
            {
                var mapWarp = Map.Warps[Point.X, Point.Y];
                if (GameServer.MapDatabase.ContainsKey(mapWarp.MapName))
                {
                    Map map = GameServer.MapDatabase[mapWarp.MapName];
                    if (Map.Dungeon != null && Map.Dungeon.Maps.ContainsKey(mapWarp.MapName))
                        map = Map.Dungeon.Maps[mapWarp.MapName];
                    Map.RemoveCharacter(this);
                    map.InsertCharacter(this, mapWarp.Point.X, mapWarp.Point.Y);
                }
            }

            return true;
        }

        public override bool Heal(Character healer, double amount, int sound)
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

            Client.SendStatistics(StatUpdateFlags.Current);

            foreach (var obj in Map.Objects)
            {
                if (WithinRange(obj, 12) && obj is Character)
                {
                    var c = obj as Character;

                    if (c is Player)
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

                    if (c is Monster && healer != null && c.Enemies.Contains(this))
                    {
                        healer.Threaten(c as Monster, amount / 2);
                    }
                }
            }

            return true;
        }
        public override void Damage(double dmg, Character attacker = null, int sound = 0, DamageType damageType = DamageType.RawDamage, DamageFlags flags = DamageFlags.None)
        {
            if (LifeStatus != LifeStatus.Alive)
                return;

            RemoveStatus("Morph");

            var realDamage = dmg;

            if (Resting)
            {
                Resting = false;
                Display();
            }

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
                    CurrentPhysicalDamageConvertedToManaDamage += realDamage;
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

            Client.SendStatistics(StatUpdateFlags.Current);

            foreach (var c in Map.Objects)
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

        public override void UseSkill(Skill s)
        {
            if (DateTime.UtcNow < s.NextAvailableUse)
                return;

            if (Polymorphed)
            {
                Client.SendMessage("You are morphed.");
                return;
            }

            if (InCombat && !s.CanUseInCombat)
            {
                Client.SendMessage("That cannot be used while in combat.");
                return;
            }

            if (Alive && !s.CanUseAlive)
            {
                Client.SendMessage("That cannot be used while alive.");
                return;
            }

            if (Dying && !s.CanUseDying)
            {
                Client.SendMessage("That cannot be used while dying.");
                return;
            }

            if (Dead && !s.CanUseDead)
            {
                Client.SendMessage("That cannot be used while dead.");
                return;
            }

            if (Coma && !s.CanUseInComa)
            {
                Client.SendMessage("That cannot be used while in a coma.");
                return;
            }

            if (Frozen && !s.CanUseFrozen)
            {
                Client.SendMessage("That cannot be used while frozen.");
                return;
            }

            if (Sleeping && !s.CanUseAsleep)
            {
                Client.SendMessage("That cannot be used while asleep.");
                return;
            }

            if (Hidden && !s.CanUseHidden)
            {
                Client.SendMessage("That cannot be used while hidden.");
                return;
            }

            if (!Hidden && s.RequiresHidden)
            {
                Client.SendMessage("You must be hidden to use that skill.");
                return;
            }

            if ((s.RequiredWeapon != WeaponType.None) && ((Weapon == null) || !Weapon.WeaponType.HasFlag(s.RequiredWeapon)))
            {
                if (!s.IsAssail)
                    Client.SendMessage("You do not have the correct weapon to use that skill.");
                return;
            }

            if (Resting)
            {
                Resting = false;
                Display();
            }

            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    RemoveStatus(status.StatusName);
            }

            if (Confused && (Program.Random(2) == 0))
            {
                Client.SendMessage("You are too confused.");
                Cooldown(s);
                return;
            }

            long successRate = s.SuccessRate + Hit;
            bool success = false;
            bool hidden = Hidden;
            bool usedImpact = false;
            var targetType = s.Target;
            int maximumDistance = s.MaximumDistance;

            if (s.IsAssail && ImpactActive && CanUseImpact.Invoke(this))
            {
                UseImpact(this);
                usedImpact = true;
            }

            if (s.IsAssail && AttackTargetOverrideActive)
                targetType = AttackTargetOverride;

            if (s.IsAssail && AttackRangeOverrideActive)
                maximumDistance = AttackRangeOverride;

            switch (targetType)
            {
                #region No Target
                case SkillTargetType.NoTarget:
                    {
                        s.Invoke(this, null);
                        success = true;
                    } break;
                #endregion
                #region Cone
                case SkillTargetType.Cone:
                    {
                        var tiles = new List<Tile>();
                        for (int i = 0; i < maximumDistance; i++)
                        {
                            int x = Point.X + (XOffset * (i + 1));
                            int y = Point.Y + (YOffset * (i + 1));
                            var t = Map[x, y];
                            if (t != null)
                                tiles.Add(t);
                            for (int j = 0; j < i; j++)
                            {
                                int x1 = x + (YOffset * (j + 1));
                                int x2 = x - (YOffset * (j + 1));
                                int y1 = y + (XOffset * (j + 1));
                                int y2 = y - (XOffset * (j + 1));
                                var t1 = Map[x1, y1];
                                var t2 = Map[x2, y2];
                                if (t1 != null)
                                    tiles.Add(t1);
                                if (t2 != null)
                                    tiles.Add(t2);
                            }
                        }
                        foreach (var t in tiles)
                        {
                            var targets = new VisibleObject[t.Objects.Count];
                            t.Objects.CopyTo(targets);

                            foreach (var realTarget in targets)
                            {
                                var target = realTarget as Character;

                                if (target == null || target is Block || target is Chest)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Neutral)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                    continue;

                                if (target.Alive && !s.CanUseOnAliveTarget)
                                    continue;

                                if (target.Dying && !s.CanUseOnDyingTarget)
                                    continue;

                                if (target.Dead && !s.CanUseOnDeadTarget)
                                    continue;

                                if (target.Coma && !s.CanUseOnComaTarget)
                                    continue;

                                long chance = successRate - (target.Dex / 4);
                                int roll = Program.Random(100);

                                if (roll <= chance)
                                {
                                    s.Invoke(this, target);
                                    if (usedImpact)
                                        InvokeImpact(this, target);
                                    target.OnSkilled(this, s);

                                    if (s.IsAssail && (Weapon != null))
                                        ReduceDurability(Weapon, 1);

                                    if (s.IsAssail && (target is Player))
                                    {
                                        var player = (target as Player);
                                        var items = new Equipment[player.Equipment.Length];
                                        player.Equipment.CopyTo(items, 0);
                                        foreach (var item in items)
                                        {
                                            if ((item != null) && (--item.CurrentDurability < 1))
                                                player.ReduceDurability(item, 1);
                                        }
                                    }
                                }
                                else
                                {
                                    target.SpellAnimation(target is Player ? 115 : 33, 10);
                                }

                                success = true;
                            }
                        }
                    } break;
                #endregion
                #region Facing
                case SkillTargetType.Facing:
                    {
                        for (int i = 0; i < maximumDistance; i++)
                        {
                            int x = Point.X + (XOffset * (i + 1));
                            int y = Point.Y + (YOffset * (i + 1));
                            var t = Map[x, y];
                            if (t != null)
                            {
                                var targets = new VisibleObject[t.Objects.Count];
                                t.Objects.CopyTo(targets);

                                foreach (var realTarget in targets)
                                {
                                    var target = realTarget as Character;

                                    if (target == null || target is Block || target is Chest)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Neutral)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                        continue;

                                    if (target.Alive && !s.CanUseOnAliveTarget)
                                        continue;

                                    if (target.Dying && !s.CanUseOnDyingTarget)
                                        continue;

                                    if (target.Dead && !s.CanUseOnDeadTarget)
                                        continue;

                                    if (target.Coma && !s.CanUseOnComaTarget)
                                        continue;

                                    long chance = successRate - (target.Dex / 4);
                                    int roll = Program.Random(100);

                                    if (roll <= chance)
                                    {
                                        s.Invoke(this, target);
                                        if (usedImpact)
                                            InvokeImpact(this, target);
                                        target.OnSkilled(this, s);

                                        if (s.IsAssail && (Weapon != null))
                                            ReduceDurability(Weapon, 1);

                                        if (s.IsAssail && (target is Player))
                                        {
                                            var player = (target as Player);
                                            var items = new Equipment[player.Equipment.Length];
                                            player.Equipment.CopyTo(items, 0);
                                            foreach (var item in items)
                                            {
                                                if ((item != null) && (--item.CurrentDurability < 1))
                                                    player.ReduceDurability(item, 1);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        target.SpellAnimation(target is Player ? 115 : 33, 10);
                                    }

                                    success = true;
                                }

                                if (Map.Walls[t.Point.X, t.Point.Y] || Map.Block[t.Point.X, t.Point.Y])
                                    break;
                            }
                        }
                    } break;
                #endregion
                #region First Facing
                case SkillTargetType.FirstFacing:
                    {
                        bool foundtarget = false;

                        for (int i = 0; i < maximumDistance && !foundtarget; i++)
                        {
                            int x = Point.X + (XOffset * (i + 1));
                            int y = Point.Y + (YOffset * (i + 1));
                            var t = Map[x, y];
                            if (t != null)
                            {
                                var targets = new VisibleObject[t.Objects.Count];
                                t.Objects.CopyTo(targets);

                                foreach (var realTarget in targets)
                                {
                                    var target = realTarget as Character;

                                    if (target == null)
                                        continue;

                                    foundtarget = true;

                                    if (target is Block || target is Chest)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Neutral)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                        continue;

                                    if (target.Alive && !s.CanUseOnAliveTarget)
                                        continue;

                                    if (target.Dying && !s.CanUseOnDyingTarget)
                                        continue;

                                    if (target.Dead && !s.CanUseOnDeadTarget)
                                        continue;

                                    if (target.Coma && !s.CanUseOnComaTarget)
                                        continue;

                                    if (target.Point.DistanceFrom(Point) < s.Ranks[s.Rank - 1].MinimumDistance)
                                        continue;

                                    long chance = successRate - (target.Dex / 4);
                                    int roll = Program.Random(100);

                                    if (roll <= chance)
                                    {
                                        s.Invoke(this, target);
                                        if (usedImpact)
                                            InvokeImpact(this, target);
                                        target.OnSkilled(this, s);

                                        if (s.IsAssail && (Weapon != null))
                                            ReduceDurability(Weapon, 1);

                                        if (s.IsAssail && (target is Player))
                                        {
                                            var player = (target as Player);
                                            var items = new Equipment[player.Equipment.Length];
                                            player.Equipment.CopyTo(items, 0);
                                            foreach (var item in items)
                                            {
                                                if (item != null)
                                                    player.ReduceDurability(item, 1);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        target.SpellAnimation(target is Player ? 115 : 33, 10);
                                    }

                                    success = true;
                                }

                                if (Map.Walls[t.Point.X, t.Point.Y] || Map.Block[t.Point.X, t.Point.Y])
                                    break;
                            }
                        }
                    } break;
                #endregion
                #region Surrounding
                case SkillTargetType.Surrounding:
                    {
                        var characters = new VisibleObject[Map.Objects.Count];
                        Map.Objects.CopyTo(characters);

                        foreach (var realTarget in characters)
                        {
                            var target = realTarget as Character;

                            if (target != null && WithinRange(target, maximumDistance))
                            {
                                if (target is Block || target is Chest)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Neutral)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                    continue;

                                if (target.Dead && !s.CanUseOnDeadTarget)
                                    continue;

                                if (target.Alive && !s.CanUseOnAliveTarget)
                                    continue;

                                if (target.Dying && !s.CanUseOnDyingTarget)
                                    continue;

                                if (target.Coma && !s.CanUseOnComaTarget)
                                    continue;

                                long chance = successRate - (target.Dex / 4);
                                int roll = Program.Random(100);

                                if (roll <= chance)
                                {
                                    s.Invoke(this, target);
                                    if (usedImpact)
                                        InvokeImpact(this, target);
                                    target.OnSkilled(this, s);

                                    if (s.IsAssail && (Weapon != null))
                                        ReduceDurability(Weapon, 1);

                                    if (s.IsAssail && (target is Player))
                                    {
                                        var player = (target as Player);
                                        var items = new Equipment[player.Equipment.Length];
                                        player.Equipment.CopyTo(items, 0);
                                        foreach (var item in items)
                                        {
                                            if (item != null)
                                                player.ReduceDurability(item, 1);
                                        }
                                    }
                                }
                                else
                                {
                                    target.SpellAnimation(target is Player ? 115 : 33, 10);
                                }

                                success = true;
                            }
                        }
                    } break;
                #endregion
            }

            if (usedImpact)
                EndImpact(this);

            if (success)
            {
                if (s.Level < s.MaxLevel && s.ImprovesOnUse && ++s.Uses >= s.UsesPerLevel)
                {
                    s.Uses = 0;
                    Client.SendMessage(string.Format("{0} is now level {1}!", s.Name, ++s.Level));
                }

                if (hidden && Hidden)
                    RemoveStatus("Hidden");

                if (s.BodyAnimation != 0)
                    BodyAnimation(s.BodyAnimation, 20);

                Cooldown(s);
            }
        }
        public override void UseSpell(Spell s, Character target, string args)
        {
            UseSpell(s, target, args, s.Ranks[s.Rank - 1].Duration);
        }
        public override void UseSpell(Spell s, Character target, string args, int duration)
        {
            if (DateTime.UtcNow < s.NextAvailableUse.AddSeconds(s.CastLines))
            {
                Client.SendMessage("You cannot cast that yet.");
                IsCasting = false;
                return;
            }

            if (Polymorphed && !s.CanUseMorphed)
            {
                Client.SendMessage("You cannot cast while morphed.");
                IsCasting = false;
                return;
            }

            if (Silenced && !s.CanUseSilenced)
            {
                Client.SendMessage("You cannot cast while silenced.");
                IsCasting = false;
                return;
            }

            if (InCombat && !s.CanUseInCombat)
            {
                Client.SendMessage("That cannot be used while in combat.");
                IsCasting = false;
                return;
            }

            if (Alive && !s.CanUseAlive)
            {
                Client.SendMessage("That cannot be used while alive.");
                IsCasting = false;
                return;
            }

            if (Dying && !s.CanUseDying)
            {
                Client.SendMessage("That cannot be used while dying.");
                IsCasting = false;
                return;
            }

            if (Dead && !s.CanUseDead)
            {
                Client.SendMessage("That cannot be used while dead.");
                IsCasting = false;
                return;
            }

            if (Coma && !s.CanUseInComa)
            {
                Client.SendMessage("That cannot be used while in a coma.");
                IsCasting = false;
                return;
            }

            if (Frozen && !s.CanUseFrozen)
            {
                Client.SendMessage("That cannot be used while frozen.");
                IsCasting = false;
                return;
            }

            if (Sleeping && !s.CanUseAsleep)
            {
                Client.SendMessage("That cannot be used while asleep.");
                IsCasting = false;
                return;
            }

            if (Hidden && !s.CanUseHidden)
            {
                Client.SendMessage("That cannot be used while hidden.");
                IsCasting = false;
                return;
            }

            if (Resting)
            {
                Resting = false;
                Display();
            }

            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    RemoveStatus(status.StatusName);
            }

            int manaCost = (int)(BaseMaximumMP * s.ManaPercentage + s.ManaCost);

            if (CurrentMP < manaCost)
            {
                Client.SendMessage("Your mana is too low.");
                IsCasting = false;
                return;
            }

            if (Confused && (Program.Random(2) == 0))
            {
                Client.SendMessage("You are too confused.");
                Cooldown(s);
                IsCasting = false;
                return;
            }

            long successRate = s.SuccessRate + Hit;
            int count = 0;
            bool confused = false;
            bool hidden = Hidden;
            bool cooldown = true;

            switch (s.CastType)
            {
                case SpellCastType.Passive:
                    {

                    } break;
                case SpellCastType.NoTarget:
                case SpellCastType.TextInput:
                case SpellCastType.DigitInputOne:
                case SpellCastType.DigitInputTwo:
                case SpellCastType.DigitInputThree:
                case SpellCastType.DigitInputFour:
                    {
                        switch (s.TargetType)
                        {
                            #region No Target
                            case SpellTargetType.NoTarget:
                                {
                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, instantTick: s.InstantTick);
                                        ++count;
                                    }
                                    else
                                    {
                                        s.Invoke(this, target, args);
                                        ++count;
                                    }
                                } break;
                            #endregion
                            #region Self Target
                            case SpellTargetType.SelfTarget:
                                {
                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, instantTick: s.InstantTick);
                                        ++count;
                                    }
                                    else
                                    {
                                        if (s.HasStatus && Statuses.ContainsKey(s.StatusName))
                                        {
                                            if (s.ReplaceStatus && (Statuses[s.StatusName].GetType() != s.GetType()))
                                            {
                                                RemoveStatus(s.StatusName);
                                            }
                                            else if (!s.ToggleStatus)
                                            {
                                                Client.SendMessage("You already have that status effect.");
                                                IsCasting = false;
                                                return;
                                            }
                                        }

                                        s.Invoke(this, this, args);
                                        var s_args = s.GetStatusArguments(this, this);

                                        if (s.HasStatus)
                                        {
                                            if (Statuses.ContainsKey(s.StatusName))
                                            {
                                                RemoveStatus(s.StatusName);
                                                cooldown = false;
                                            }
                                            else
                                            {
                                                bool add = AddStatus(s.GetType().Name, s.Rank, duration, this,
                                                    args: s_args, instantTick: s.InstantTick);
                                                if (add && s.Aura)
                                                {
                                                    AuraOwner = this;
                                                }
                                            }
                                        }

                                        if (s.SpellAnimation != 0)
                                        {
                                            SpellAnimation(s.SpellAnimation, 100);
                                        }
                                        ++count;
                                    }
                                } break;
                            #endregion
                            #region Area Target
                            case SpellTargetType.AreaTarget:
                                {
                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, instantTick: s.InstantTick);
                                        ++count;
                                    }
                                    else
                                    {
                                        if (s.TileAnimation)
                                        {
                                            foreach (var tile in Map.Tiles)
                                            {
                                                if (tile.Point.DistanceFrom(Point) <= s.MaximumDistance)
                                                {
                                                    Map.SpellAnimation(s.SpellAnimation, tile.Point.X, tile.Point.Y, 100);
                                                }
                                            }
                                        }
                                        var characters = new VisibleObject[Map.Objects.Count];
                                        Map.Objects.CopyTo(characters);

                                        foreach (var obj in characters)
                                        {
                                            var c = obj as Character;

                                            if (c != null && WithinRange(c, s.MaximumDistance) && c != this)
                                            {
                                                if (c is Monster && (c as Monster).SpellImmunities.Contains(s.GetType().Name))
                                                    continue;

                                                if (AllegianceTo(c) == Allegiance.Neutral)
                                                    continue;

                                                if (AllegianceTo(c) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                                    continue;

                                                if (AllegianceTo(c) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                                    continue;

                                                if (c.Dead && !s.CanUseOnDeadTarget)
                                                    continue;

                                                if (c.Alive && !s.CanUseOnAliveTarget)
                                                    continue;

                                                if (c.Dying && !s.CanUseOnDyingTarget)
                                                    continue;

                                                if (c.Coma && !s.CanUseOnComaTarget)
                                                    continue;

                                                if (s.HasStatus && c.Statuses.ContainsKey(s.StatusName))
                                                    continue;

                                                long chance = successRate - (c.Dex / 4);
                                                int roll = Program.Random(100);

                                                if (roll <= chance)
                                                {
                                                    s.Invoke(this, c, args);
                                                    var s_args = s.GetStatusArguments(this, c);

                                                    if (s.HasStatus)
                                                    {
                                                        c.AddStatus(s.GetType().Name, s.Rank, duration, this,
                                                            args: s_args, instantTick: s.InstantTick);
                                                    }

                                                    if (s.SpellAnimation != 0)
                                                    {
                                                        c.SpellAnimation(s.SpellAnimation, 100);
                                                    }

                                                    if ((c is Player) && (c != this))
                                                    {
                                                        var client = (c as Player).Client;

                                                        if (s.Unfriendly)
                                                        {
                                                            client.SendMessage(string.Format("{0} attacks you with {1}.",
                                                                Name, s.Name));
                                                        }
                                                        else
                                                        {
                                                            client.SendMessage(string.Format("{0} casts {1} on you.",
                                                                Name, s.Name));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    c.SpellAnimation(c is Player ? 115 : 33, 10);
                                                }

                                                if (++count == 50)
                                                    break;
                                            }
                                        }
                                    }
                                } break;
                            #endregion
                            #region Facing Target
                            case SpellTargetType.FacingTarget:
                                {
                                    for (int i = 0; i < s.Ranks[s.Rank - 1].MaximumDistance; i++)
                                    {
                                        int x = Point.X + (XOffset * (i + 1));
                                        int y = Point.Y + (YOffset * (i + 1));
                                        var tile = Map[x, y];
                                        if (tile != null)
                                        {
                                            if (Map.Walls[tile.Point.X, tile.Point.Y])
                                            {
                                                ++count;
                                                break;
                                            }

                                            var targets = new VisibleObject[tile.Objects.Count];
                                            tile.Objects.CopyTo(targets);

                                            foreach (var obj in targets)
                                            {
                                                var t = obj as Character;

                                                if (t == null)
                                                    continue;

                                                if (t is Monster && (t as Monster).SpellImmunities.Contains(s.GetType().Name))
                                                    continue;

                                                if (AllegianceTo(t) == Allegiance.Neutral)
                                                    continue;

                                                if (AllegianceTo(t) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                                    continue;

                                                if (AllegianceTo(t) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                                    continue;

                                                if (t.Alive && !s.CanUseOnAliveTarget)
                                                    continue;

                                                if (t.Dying && !s.CanUseOnDyingTarget)
                                                    continue;

                                                if (t.Dead && !s.CanUseOnDeadTarget)
                                                    continue;

                                                if (t.Coma && !s.CanUseOnComaTarget)
                                                    continue;

                                                long chance = successRate - (t.Dex / 4);
                                                int roll = Program.Random(100);

                                                if (roll <= chance)
                                                {
                                                    s.Invoke(this, t, args);
                                                }
                                                else
                                                {
                                                    t.SpellAnimation(t is Player ? 115 : 33, 10);
                                                }

                                                ++count;
                                            }

                                            if (s.SpellAnimation != 0 && s.TileAnimation)
                                                Map.SpellAnimation(s.SpellAnimation, x, y, 100);
                                        }
                                    }
                                } break;
                            #endregion
                            #region Group Target
                            case SpellTargetType.GroupTarget:
                                {
                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, instantTick: s.InstantTick);
                                        ++count;
                                    }
                                    else
                                    {
                                        var characters = new Character[Group.Members.Count];
                                        Group.Members.CopyTo(characters);

                                        foreach (var c in characters)
                                        {
                                            if (WithinRange(c, s.MaximumDistance) && c != this)
                                            {
                                                if (c.Dead && !s.CanUseOnDeadTarget)
                                                    continue;

                                                if (c.Alive && !s.CanUseOnAliveTarget)
                                                    continue;

                                                if (c.Dying && !s.CanUseOnDyingTarget)
                                                    continue;

                                                if (c.Coma && !s.CanUseOnComaTarget)
                                                    continue;

                                                if (s.HasStatus && c.Statuses.ContainsKey(s.StatusName))
                                                    continue;

                                                s.Invoke(this, c, args);
                                                var s_args = s.GetStatusArguments(this, c);

                                                if (s.HasStatus)
                                                {
                                                    c.AddStatus(s.GetType().Name, s.Rank, duration, this,
                                                        args: s_args, instantTick: s.InstantTick);
                                                }

                                                if (s.SpellAnimation != 0)
                                                {
                                                    c.SpellAnimation(s.SpellAnimation, 100);
                                                }

                                                c.SendMessage(string.Format("{0} casts {1} on you.", Name, s.Name));

                                                ++count;
                                            }
                                        }
                                    }
                                } break;
                            #endregion
                        }
                    } break;
                case SpellCastType.Target:
                    {
                        switch (s.TargetType)
                        {
                            #region Self Target
                            case SpellTargetType.SelfTarget:
                                {
                                    if (Confused && Program.Random(2) == 0)
                                    {
                                        Client.SendMessage("You are too confused.");
                                        Cooldown(s);
                                        IsCasting = false;
                                        return;
                                    }

                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, instantTick: s.InstantTick);
                                        ++count;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(s.StatusName))
                                        {
                                            if (Statuses.ContainsKey(s.StatusName))
                                            {
                                                if (s.ReplaceStatus && Statuses[s.StatusName].Name != s.Name)
                                                    RemoveStatus(s.StatusName);
                                                else if (!s.ToggleStatus)
                                                {
                                                    Client.SendMessage("You already have that status effect.");
                                                    IsCasting = false;
                                                    return;
                                                }
                                            }
                                        }

                                        s.Invoke(this, target, args);
                                        var s_args = s.GetStatusArguments(this, target);

                                        if (!string.IsNullOrEmpty(s.StatusName))
                                        {
                                            if (Statuses.ContainsKey(s.StatusName))
                                            {
                                                RemoveStatus(s.StatusName);
                                                cooldown = false;
                                            }
                                            else
                                            {
                                                AddStatus(s.GetType().Name, s.Rank, duration, this, args: s_args, instantTick: s.InstantTick);
                                            }
                                        }

                                        if (s.SpellAnimation != 0)
                                        {
                                            SpellAnimation(s.SpellAnimation, 100);
                                        }
                                        ++count;
                                    }
                                } break;
                            #endregion
                            #region Single Target
                            case SpellTargetType.SingleTarget:
                                {
                                    if (!WithinRange(target, s.MinimumDistance, s.MaximumDistance))
                                        return;

                                    if (target.Alive && !s.CanUseOnAliveTarget)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (target.Dying && !s.CanUseOnDyingTarget)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (target.Dead && !s.CanUseOnDeadTarget)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (target.Coma && !s.CanUseOnComaTarget)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (target is Monster && (target as Monster).SpellImmunities.Contains(s.GetType().Name))
                                    {
                                        Client.SendMessage(string.Format("{0} is immune to {1}!", target.Name, s.Name));
                                        IsCasting = false;
                                        return;
                                    }

                                    if (AllegianceTo(target) == Allegiance.Neutral)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.PlayerCanUseOnAlly)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.PlayerCanUseOnEnemy)
                                    {
                                        Client.SendMessage("It doesn't work.");
                                        IsCasting = false;
                                        return;
                                    }

                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, target);
                                        ++count;
                                    }
                                    else
                                    {
                                        if (Confused && s.Unfriendly && Program.Random(2) == 0)
                                        {
                                            Client.SendMessage("You attack yourself in your confusion.");
                                            target = this;
                                            confused = true;
                                        }

                                        if (s.HasStatus && target.Statuses.ContainsKey(s.StatusName))
                                        {
                                            Client.SendMessage(string.Format("{0} already has that status effect.", target.Name));
                                            IsCasting = false;
                                            return;
                                        }


                                        long chance = successRate - (target.Dex / 4);
                                        int roll = Program.Random(100);

                                        if (roll <= chance)
                                        {
                                            s.Invoke(this, target, args);
                                            var s_args = s.GetStatusArguments(this, target);

                                            if (s.HasStatus)
                                            {
                                                if (SingleTargetSpells.ContainsKey(s.StatusName))
                                                {
                                                    var oldTarget = SingleTargetSpells[s.StatusName];
                                                    oldTarget.RemoveStatus(s.StatusName);
                                                }
                                                target.AddStatus(s.GetType().Name, s.Rank, duration, this, args: s_args);
                                                if (s.SingleTarget)
                                                {
                                                    SingleTargetSpells.Add(s.StatusName, target);
                                                }
                                            }

                                            if (s.SpellAnimation != 0)
                                            {
                                                target.SpellAnimation(s.SpellAnimation, 100);
                                            }

                                            if (target is Player && target != this)
                                            {
                                                var client = (target as Player).Client;

                                                if (s.Unfriendly)
                                                {
                                                    client.SendMessage(string.Format("{0} attacks you with {1}.",
                                                        Name, s.Name));
                                                }
                                                else
                                                {
                                                    client.SendMessage(string.Format("{0} casts {1} on you.",
                                                        Name, s.Name));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            target.SpellAnimation(target is Player ? 115 : 33, 10);
                                        }

                                        ++count;
                                    }
                                } break;
                            #endregion
                        }
                    } break;
            }

            if (count > 0)
            {
                if (s.Level < s.MaxLevel && ++s.Uses >= s.UsesPerLevel)
                {
                    s.Uses = 0;
                    Client.SendMessage(string.Format("{0} is now level {1}!", s.Name, ++s.Level));
                }
                if (hidden && Hidden)
                    RemoveStatus("Hidden");
                if (s.BodyAnimation != 0)
                    BodyAnimation(s.BodyAnimation, 40);
                if (!s.KeepMana && !AdminRights.HasFlag(AdminRights.NoManaCost))
                {
                    CurrentMP -= manaCost;
                    Client.SendStatistics(StatUpdateFlags.Current);
                }
                s.KeepMana = false;
            }

            if (!confused)
            {
                Client.SendMessage(string.Format("You cast {0}.", s.Name));
            }

            if (cooldown)
            {
                Cooldown(s);
            }

            IsCasting = false;
        }
        public override void Channel(Spell s)
        {
            if (Polymorphed)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Silenced && !s.CanUseSilenced)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (InCombat && !s.CanUseInCombat)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Alive && !s.CanUseAlive)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Dying && !s.CanUseDying)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Dead && !s.CanUseDead)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Coma && !s.CanUseInComa)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Frozen && !s.CanUseFrozen)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Sleeping && !s.CanUseAsleep)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Hidden && !s.CanUseHidden)
            {
                RemoveStatus(s.StatusName);
                return;
            }

            if (Resting)
            {
                Resting = false;
                Display();
            }

            int manaCost = (int)(BaseMaximumMP * s.Ranks[s.Rank - 1].ManaPercentage) + s.Ranks[s.Rank - 1].ManaCost;

            if (CurrentMP < manaCost)
            {
                Client.SendMessage("Your mana is too low.");
                RemoveStatus(s.StatusName);
                return;
            }

            int count = 0;
            bool hidden = Hidden;

            switch (s.CastType)
            {
                case SpellCastType.Passive:
                    {

                    } break;
                case SpellCastType.NoTarget:
                case SpellCastType.TextInput:
                case SpellCastType.DigitInputOne:
                case SpellCastType.DigitInputTwo:
                case SpellCastType.DigitInputThree:
                case SpellCastType.DigitInputFour:
                    {
                        switch (s.TargetType)
                        {
                            case SpellTargetType.NoTarget:
                                {
                                    s.Invoke(this, null, null);
                                    ++count;
                                } break;
                            case SpellTargetType.SelfTarget:
                                {
                                    s.Invoke(this, this, null);

                                    if (s.SpellAnimation != 0)
                                    {
                                        SpellAnimation(s.SpellAnimation, 100);
                                    }

                                    ++count;
                                } break;
                            case SpellTargetType.AreaTarget:
                                {
                                    foreach (var obj in Map.Objects)
                                    {
                                        if (WithinRange(obj, s.MinimumCastLines, s.MaximumDistance) && (!(obj is Reactor)) && (!(obj is Item)) && (s.IncludeSelf || obj != this))
                                        {
                                            var c = obj as Character;

                                            if ((c is Player) && (Map.Flags & MapFlags.PlayerKill) != MapFlags.PlayerKill && !s.PlayerCanUseOnAlly)
                                                continue;

                                            if ((c is Player) && (Map.Flags & MapFlags.PlayerKill) == MapFlags.PlayerKill && !s.PlayerCanUseOnEnemy)
                                                continue;

                                            if (c is Monster && (c as Monster).Ally == NpcAlly.Player && !s.PlayerCanUseOnAlly)
                                                continue;

                                            if (c is Monster && (c as Monster).Ally == NpcAlly.Enemy && !s.PlayerCanUseOnEnemy)
                                                continue;

                                            if (c.Alive && !s.CanUseOnAliveTarget)
                                                continue;

                                            if (c.Dying && !s.CanUseOnDyingTarget)
                                                continue;

                                            if (c.Dead && !s.CanUseOnDeadTarget)
                                                continue;

                                            if (c.Coma && !s.CanUseOnComaTarget)
                                                continue;

                                            s.Invoke(this, c, null);

                                            if (s.SpellAnimation != 0)
                                            {
                                                c.SpellAnimation(s.SpellAnimation, 100);
                                            }

                                            if ((c is Player) && (c != this))
                                            {
                                                if (s.Unfriendly)
                                                {
                                                    (c as Player).Client.SendMessage(String.Format("{0} attacks you with {1}.",
                                                        Name, s.Name));
                                                }
                                                else
                                                {
                                                    (c as Player).Client.SendMessage(String.Format("{0} casts {1} on you.",
                                                        Name, s.Name));
                                                }
                                            }

                                            if (++count == 50)
                                                break;
                                        }
                                    }
                                } break;
                        }
                    } break;
                case SpellCastType.Target:
                    {
                        switch (s.TargetType)
                        {
                            case SpellTargetType.SingleTarget:
                                {
                                    if ((s.Target == null) || !WithinRange(s.Target, 12))
                                    {
                                        RemoveStatus(s.StatusName);
                                        return;
                                    }

                                    if (s.Target.Alive && !s.CanUseOnAliveTarget)
                                    {
                                        RemoveStatus(s.StatusName);
                                        return;
                                    }

                                    if (s.Target.Dying && !s.CanUseOnDyingTarget)
                                    {
                                        RemoveStatus(s.StatusName);
                                        return;
                                    }

                                    if (s.Target.Dead && !s.CanUseOnDeadTarget)
                                    {
                                        RemoveStatus(s.StatusName);
                                        return;
                                    }

                                    if (s.Target.Coma && !s.CanUseOnComaTarget)
                                    {
                                        RemoveStatus(s.StatusName);
                                        return;
                                    }

                                    s.Invoke(this, s.Target, null);

                                    if (s.SpellAnimation != 0)
                                    {
                                        s.Target.SpellAnimation(s.SpellAnimation, 100);
                                    }

                                    if ((s.Target is Player) && (s.Target != this))
                                    {
                                        if (s.Unfriendly)
                                        {
                                            (s.Target as Player).Client.SendMessage(String.Format("{0} attacks you with {1}.",
                                                Name, s.Name));
                                        }
                                        else
                                        {
                                            (s.Target as Player).Client.SendMessage(String.Format("{0} casts {1} on you.",
                                                Name, s.Name));
                                        }
                                    }

                                    ++count;
                                } break;
                        }
                    } break;
            }

            if (count > 0)
            {
                if (hidden && Hidden)
                    RemoveStatus("Hidden");
                if (s.BodyAnimation != 0)
                    BodyAnimation(s.BodyAnimation, 40);
                CurrentMP -= manaCost;
                Client.SendStatistics(StatUpdateFlags.Current);
            }
        }

        public override Allegiance AllegianceTo(Character c)
        {
            if (c == null || c is Block || c is Chest)
                return Allegiance.Neutral;

            if (Map.Flags.HasFlag(MapFlags.ArenaTeam))
            {
                if (ArenaTeam == 0 || c.ArenaTeam == 0)
                    return Allegiance.Neutral;
                else if (ArenaTeam == c.ArenaTeam)
                    return Allegiance.Friendly;
                else
                    return Allegiance.Enemy;
            }
            else if (Map.Flags.HasFlag(MapFlags.PlayerKill))
            {
                var player = c as Player;

                if (player != null && Group.Members.Contains(player))
                    return Allegiance.Friendly;
                else
                    return Allegiance.Enemy;
            }
            else
            {
                if (c is Player)
                    return Allegiance.Friendly;

                if (Enemies.Contains(c) || c.Enemies.Contains(this))
                    return Allegiance.Enemy;

                if (GameServer.Factions.ContainsKey(c.Faction))
                {
                    if (AllegianceTable.ContainsKey(c.Faction))
                        return AllegianceTable[c.Faction];
                    else
                        return GameServer.Factions[c.Faction].PlayerDefault;
                }
            }

            return Allegiance.Neutral;
        }

        public void Cooldown(Skill s)
        {
            if (s.IsAssail && (Weapon != null))
            {
                Cooldown(s, Weapon.Speed + s.CooldownLengthMod);
            }
            else
            {
                Cooldown(s, s.CooldownLength);
            }
        }
        public void Cooldown(Skill s, double length)
        {
            if (AdminRights.HasFlag(AdminRights.NoCooldowns))
                return;

            s.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);
            if (!s.IsAssail)
            {
                Client.SendCooldown(s.Slot, (long)Math.Floor(length), 1);
            }
        }

        public void Cooldown(Spell s)
        {
            Cooldown(s, s.CooldownLength);
        }
        public void Cooldown(Spell s, double length)
        {
            if (AdminRights.HasFlag(AdminRights.NoCooldowns))
                return;

            s.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);
            Client.SendCooldown(s.Slot, (long)Math.Floor(length), 0);

            if (!string.IsNullOrEmpty(s.CooldownFamily))
            {
                foreach (var t in SpellBook)
                {
                    if ((t != null) && (t.CooldownFamily == s.CooldownFamily))
                    {
                        t.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);
                        Client.SendCooldown(t.Slot, (long)Math.Floor(length), 0);
                    }
                }
            }
        }

        public override bool AddStatus(string typeName, int rank = 0, int timeLeft = 0, Character caster = null, Character target = null, Dictionary<string, string> args = null, bool instantTick = false)
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

            Client.DisplaySpellBar(s);

            if (s.Aura)
                Aura = s;

            return true;
        }
        public override bool RemoveStatus(string status)
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
                s.TimeLeft = 0;
                Client.DisplaySpellBar(s);
                if (Aura == s)
                    Aura = null;
                return true;
            }

            return false;
        }

        public override void SendMessage(string message, byte type = 3)
        {
            Client.SendMessage(message, type);
        }
        public override void UpdateStatistics(StatUpdateFlags flags = StatUpdateFlags.Full)
        {
            Client.SendStatistics(flags);
        }

        public override void AddLegendMark(string type, params string[] args)
        {
            var legend = GameServer.CreateLegendMark(type);
            if (legend != null)
            {
                if (Legend.ContainsKey(legend.Key))
                {
                    Legend[legend.Key].Arguments = args;
                    Legend[legend.Key].DateUpdated = DateTime.UtcNow;
                }
                else
                {
                    legend.Arguments = args;
                    Legend.Add(legend.Key, legend);
                }
            }
        }
        public override void RemoveLegendMark(string type)
        {
            var legend = GameServer.CreateLegendMark(type);
            if (legend != null && Legend.ContainsKey(legend.Key))
                Legend.Remove(legend.Key);
        }

        public bool QuestFamilyFinished(string familyType)
        {
            var questFamily = GameServer.QuestFamilyDatabase[familyType];
            foreach (var questType in questFamily.QuestTypes)
            {
                if (Quests[questType].Progress != QuestProgress.Finished)
                    return false;
            }
            return true;
        }
        public int QuestsFinishedInFamily(string familyType)
        {
            int count = 0;
            var questFamily = GameServer.QuestFamilyDatabase[familyType];
            foreach (var questType in questFamily.QuestTypes)
            {
                if (Quests[questType].Progress == QuestProgress.Finished)
                    count++;
            }
            return count;
        }

        public bool StartQuest(string type, int step)
        {
            var quest = Quests[type];
            var questStep = quest[step];

            if (Level < quest.MinimumLevel || Level > quest.MaximumLevel)
                return false;

            foreach (var req in quest.Prerequisites)
            {
                if (Quests[req].Progress != QuestProgress.Finished)
                    return false;
            }

            if (step != quest.CurrentStep)
                return false;

            if (questStep.Progress != QuestProgress.Unstarted)
                return false;

            quest.Progress = QuestProgress.InProgress;
            questStep.Progress = QuestProgress.InProgress;

            foreach (var qo in questStep.Objectives.Values)
            {
                qo.Count = 0;
                qo.MiscData = string.Empty;
            }

            SendQuestInfo(quest);

            return true;
        }
        public bool FinishQuest(string type, int step)
        {
            var quest = Quests[type];
            var questStep = quest[step];

            if (step != quest.CurrentStep)
                return false;

            if (questStep.Progress != QuestProgress.InProgress)
                return false;

            foreach (var qo in questStep.Objectives.Values)
            {
                switch (qo.Type)
                {
                    case QuestObjectiveType.Item:
                        {
                            var count = Inventory.Count(qo.RequiredItemType);
                            if (count < qo.Requirement)
                                return false;
                        } break;
                    case QuestObjectiveType.Misc:
                    case QuestObjectiveType.Kill:
                        {
                            if (qo.Count < qo.Requirement)
                                return false;
                        } break;
                    case QuestObjectiveType.Skill:
                        {

                        } break;
                    case QuestObjectiveType.Spell:
                        {

                        } break;
                    default:
                        {
                            if (!qo.IsComplete(this))
                                return false;
                        } break;
                }
            }

            if (step == quest.Steps.Count)
                quest.Progress = QuestProgress.Finished;
            else
                quest.CurrentStep++;

            questStep.Progress = QuestProgress.Finished;

            foreach (var qo in questStep.Objectives.Values)
            {
                if (qo.Type == QuestObjectiveType.Item)
                {
                    for (int i = 0; i < qo.Requirement; i++)
                    {
                        int index = Inventory.IndexOf(qo.RequiredItemType);
                        RemoveItem(index, 1);
                    }
                }
            }

            Gold += questStep.GoldReward;
            RewardExperience(questStep.ExpReward);

            if (Guild != null)
            {
                int guildExp = (int)(questStep.ExpReward * 0.05);
                Guild.AddExperience(guildExp);
                Client.SendMessage(string.Format("{0} has received {1} guild experience!", Guild.Name, guildExp));
            }

            foreach (var itemName in questStep.ItemReward)
            {
                var item = GameServer.CreateItem(itemName);
                if (item != null)
                    AddItem(item);
            }

            questStep.OnComplete(this);

            foreach (var questFamily in GameServer.QuestFamilyDatabase.Values)
            {
                if (questFamily.QuestTypes.Contains(type))
                    questFamily.QuestCompleted(this, quest);
            }

            var packet = new ServerPacket(0x8A);
            packet.WriteByte(0x02);
            packet.WriteUInt16((ushort)quest.ID);
            packet.WriteByte(0x00);
            Client.Enqueue(packet);

            return true;
        }
        public bool AbandonQuest(string type)
        {
            var quest = Quests[type];
            var questStep = quest.QuestStep;

            if (questStep.Progress != QuestProgress.InProgress)
                return false;

            foreach (var qo in questStep.Objectives.Values)
            {
                qo.Count = 0;
                qo.MiscData = string.Empty;
            }

            if (quest.CurrentStep == 1)
                quest.Progress = QuestProgress.Unstarted;

            questStep.Progress = QuestProgress.Unstarted;

            questStep.OnAbandon(this);

            var packet = new ServerPacket(0x8A);
            packet.WriteByte(0x02);
            packet.WriteUInt16((ushort)quest.ID);
            packet.WriteByte(0x00);
            Client.Enqueue(packet);

            return true;
        }

        public bool CanStartQuest(string type, int step)
        {
            var quest = Quests[type];
            var questStep = quest[step];
            return quest.CurrentStep == step && questStep.Progress == QuestProgress.Unstarted;
        }
        public bool CanFinishQuest(string type, int step)
        {
            var quest = Quests[type];
            var questStep = quest[step];

            if (step != quest.CurrentStep)
                return false;

            if (questStep.Progress != QuestProgress.InProgress)
                return false;

            foreach (var qo in questStep.Objectives.Values)
            {
                switch (qo.Type)
                {
                    case QuestObjectiveType.Item:
                        {
                            var count = Inventory.Count(qo.RequiredItemType);
                            if (count < qo.Requirement)
                                return false;
                        } break;
                    case QuestObjectiveType.Misc:
                    case QuestObjectiveType.Kill:
                        {
                            if (qo.Count < qo.Requirement)
                                return false;
                        } break;
                    case QuestObjectiveType.Skill:
                        {

                        } break;
                    case QuestObjectiveType.Spell:
                        {

                        } break;
                    default:
                        {
                            if (!qo.IsComplete(this))
                                return false;
                        } break;
                }
            }

            return true;
        }

        public int GetQuestCount(string type, string objective)
        {
            return Quests[type].QuestStep[objective].Count;
        }
        public string GetQuestString(string type, string objective)
        {
            return Quests[type].QuestStep[objective].MiscData;
        }

        public void AddQuestCount(string type, string objName)
        {
            var quest = Quests[type];
            var questStep = quest.QuestStep;
            var objective = questStep[objName];
            if (objective.Type == QuestObjectiveType.Misc || objective.Type == QuestObjectiveType.Kill)
            {
                ++objective.Count;
                var packet = new ServerPacket(0x8A);
                packet.WriteByte(0x03);
                packet.WriteByte(0x01);
                packet.WriteUInt16((ushort)quest.ID);
                packet.WriteString8(objective.Name);
                packet.WriteUInt32((uint)objective.Count);
                Client.Enqueue(packet);
            }
        }
        public void SetQuestCount(string type, string objName, int count)
        {
            var quest = Quests[type];
            var questStep = quest.QuestStep;
            var objective = questStep[objName];
            if (objective.Type == QuestObjectiveType.Misc || objective.Type == QuestObjectiveType.Kill)
            {
                objective.Count = Math.Min(count, objective.Requirement);
                var packet = new ServerPacket(0x8A);
                packet.WriteByte(0x03);
                packet.WriteByte(0x01);
                packet.WriteUInt16((ushort)quest.ID);
                packet.WriteString8(objective.Name);
                packet.WriteUInt32((uint)objective.Count);
                Client.Enqueue(packet);
            }
        }
        public void SetQuestString(string type, string objName, object value)
        {
            var quest = Quests[type];
            var questStep = quest.QuestStep;
            var objective = questStep[objName];
            objective.MiscData = value.ToString();
        }

        public void SendQuestInfo(Quest quest)
        {
            var packet = new ServerPacket(0x8A);
            packet.WriteByte(0x01);
            packet.WriteByte(0x01);
            packet.WriteUInt16((ushort)quest.ID);
            packet.WriteByte((byte)quest.CurrentStep);
            packet.WriteUInt32(0);
            var objectives = (from o in quest.QuestStep.Objectives.Values
                              where o.Type == QuestObjectiveType.Misc || o.Type == QuestObjectiveType.Kill
                              select o).ToArray();
            for (int i = 0; i < 5; i++)
            {
                if (i < objectives.Length)
                {
                    packet.WriteUInt32((uint)objectives[i].Count);
                }
                else
                {
                    packet.WriteUInt32(0);
                }
            }
            packet.WriteUInt16(0);
            packet.WriteByte(0x00);
            Client.Enqueue(packet);
        }
        public void SendQuestInfo(IEnumerable<Quest> quests)
        {
            var packet = new ServerPacket(0x8A);
            packet.WriteByte(0x01);
            packet.WriteByte((byte)quests.Count());
            foreach (var quest in quests)
            {
                packet.WriteUInt16((ushort)quest.ID);
                packet.WriteByte((byte)quest.CurrentStep);
                packet.WriteUInt32(0);
                var objectives = (from o in quest.QuestStep.Objectives.Values
                                  where o.Type == QuestObjectiveType.Misc || o.Type == QuestObjectiveType.Kill
                                  select o).ToArray();
                for (int i = 0; i < 5; i++)
                {
                    if (i < objectives.Length)
                    {
                        packet.WriteUInt32((uint)objectives[i].Count);
                    }
                    else
                    {
                        packet.WriteUInt32(0);
                    }
                }
                packet.WriteUInt16(0);
            }
            packet.WriteByte(0x00);
            Client.Enqueue(packet);
        }

        public override void Save()
        {
            if (!Loaded)
                return;

            var apcounter = new List<string>();
            foreach (var kvp in AbilityPointCounter)
            {
                apcounter.Add(string.Format("{0}:{1}", kvp.Key, kvp.Value));
            }

            var apmultiply = new List<string>();
            foreach (var kvp in AbilityPointMultiplier)
            {
                apmultiply.Add(string.Format("{0}:{1}", kvp.Key, kvp.Value));
            }

            #region Data
            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE characters SET "
                + "name=@name, "
                + "last_session_id=@last_session_id, "
                + "gm=@gm, "
                + "sex=@sex, "
                + "class=@class, "
                + "specialization=@specialization, "
                + "mapname=@mapname, "
                + "mapid=@mapid, "
                + "pointx=@pointx, "
                + "pointy=@pointy, "
                + "direction=@direction, "
                + "gamepoints=@gamepoints, "
                + "hairstyle=@hairstyle, "
                + "haircolor=@haircolor, "
                + "ac=@ac, "
                + "mr=@mr, "
                + "s_str=@s_str, "
                + "s_int=@s_int, "
                + "s_wis=@s_wis, "
                + "s_con=@s_con, "
                + "s_dex=@s_dex, "
                + "dmg=@dmg, "
                + "hit=@hit, "
                + "curhp=@curhp, "
                + "curmp=@curmp, "
                + "maxhp=@maxhp, "
                + "maxmp=@maxmp, "
                + "level=@level, "
                + "exp=@exp, "
                + "tnl=@tnl, "
                + "ability=@ability, "
                + "abexp=@abexp, "
                + "tna=@tna, "
                + "statpoints=@statpoints, "
                + "dead=@dead, "
                + "deathmapname=@deathmapname, "
                + "deathmapid=@deathmapid, "
                + "deathpointx=@deathpointx, "
                + "deathpointy=@deathpointy, "
                + "displaymask=@displaymask, "
                + "nation=@nation, "
                + "title=@title, "
                + "bagslots=@bagslots, "
                + "occupation=@occupation, "
                + "apcounter=@apcounter, "
                + "apmultiply=@apmultiply "
                + "WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.Parameters.AddWithValue("@name", Name);
            com.Parameters.AddWithValue("@last_session_id", Program.SessionID);
            com.Parameters.AddWithValue("@gm", AdminRights.ToString());
            com.Parameters.AddWithValue("@sex", Sex.ToString());
            com.Parameters.AddWithValue("@class", Class.ToString());
            com.Parameters.AddWithValue("@specialization", Specialization.ToString());
            com.Parameters.AddWithValue("@mapname", Map.GetType().Name);
            com.Parameters.AddWithValue("@mapid", Map.ID);
            com.Parameters.AddWithValue("@pointx", Point.X);
            com.Parameters.AddWithValue("@pointy", Point.Y);
            com.Parameters.AddWithValue("@direction", Direction.ToString());
            com.Parameters.AddWithValue("@gamepoints", GamePoints);
            com.Parameters.AddWithValue("@hairstyle", HairStyle);
            com.Parameters.AddWithValue("@haircolor", HairColor);
            com.Parameters.AddWithValue("@ac", BaseArmorClass);
            com.Parameters.AddWithValue("@mr", BaseMagicResistance);
            com.Parameters.AddWithValue("@s_str", BaseStr);
            com.Parameters.AddWithValue("@s_int", BaseInt);
            com.Parameters.AddWithValue("@s_wis", BaseWis);
            com.Parameters.AddWithValue("@s_con", BaseCon);
            com.Parameters.AddWithValue("@s_dex", BaseDex);
            com.Parameters.AddWithValue("@dmg", BaseDmg);
            com.Parameters.AddWithValue("@hit", BaseHit);
            com.Parameters.AddWithValue("@curhp", CurrentHP);
            com.Parameters.AddWithValue("@curmp", CurrentMP);
            com.Parameters.AddWithValue("@maxhp", BaseMaximumHP);
            com.Parameters.AddWithValue("@maxmp", BaseMaximumMP);
            com.Parameters.AddWithValue("@level", Level);
            com.Parameters.AddWithValue("@exp", Experience);
            com.Parameters.AddWithValue("@tnl", ToNextLevel);
            com.Parameters.AddWithValue("@ability", Ability);
            com.Parameters.AddWithValue("@abexp", AbilityExp);
            com.Parameters.AddWithValue("@tna", ToNextAbility);
            com.Parameters.AddWithValue("@statpoints", AvailableStatPoints);
            com.Parameters.AddWithValue("@dead", Dead);
            com.Parameters.AddWithValue("@deathmapname", DeathMap.GetType().Name);
            com.Parameters.AddWithValue("@deathmapid", DeathMap.ID);
            com.Parameters.AddWithValue("@deathpointx", DeathPoint.X);
            com.Parameters.AddWithValue("@deathpointy", DeathPoint.Y);
            com.Parameters.AddWithValue("@displaymask", DisplayBitmask);
            com.Parameters.AddWithValue("@nation", Nation);
            com.Parameters.AddWithValue("@title", Title);
            com.Parameters.AddWithValue("@bagslots", AvailableBagSlots);
            com.Parameters.AddWithValue("@occupation", Occupation.ToString());
            com.Parameters.AddWithValue("@apcounter", string.Join(",", apcounter));
            com.Parameters.AddWithValue("@apmultiply", string.Join(",", apmultiply));
            com.ExecuteNonQuery();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE accounts SET gold = @gold WHERE acct_id = @acct_id";
            com.Parameters.AddWithValue("@gold", Gold);
            com.Parameters.AddWithValue("@acct_id", AccountID);
            com.ExecuteNonQuery();
            #endregion
            #region Bank
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM bankitems WHERE (acct_id = @acct_id)";
            com.Parameters.AddWithValue("@acct_id", AccountID);
            com.ExecuteNonQuery();
            foreach (var item in BankItems)
            {
                if (item != null)
                {
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO bankitems VALUES (@acct_id, @itemname, @color, @amount, @durability, @data)";
                    com.Parameters.AddWithValue("@acct_id", AccountID);
                    com.Parameters.AddWithValue("@itemname", item.GetType().Name);
                    com.Parameters.AddWithValue("@color", item.Color);
                    com.Parameters.AddWithValue("@amount", item.Amount);
                    com.Parameters.AddWithValue("@durability", item.CurrentDurability);
                    com.Parameters.AddWithValue("@data", item.MiscData);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Skills
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM skillbooks WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < SkillBook.Length; i++)
            {
                if (SkillBook[i] != null && !SkillBook[i].RequiresAdmin)
                {
                    long cooldown = 0;
                    if (!SkillBook[i].CanUse)
                        cooldown = (long)SkillBook[i].NextAvailableUse.Subtract(DateTime.UtcNow).TotalSeconds;
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO skillbooks VALUES (@character_id, @skillname, @rank, @level, @cooldown, @i)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@skillname", SkillBook[i].GetType().Name);
                    com.Parameters.AddWithValue("@rank", SkillBook[i].Rank);
                    com.Parameters.AddWithValue("@level", SkillBook[i].Level);
                    com.Parameters.AddWithValue("@cooldown", cooldown);
                    com.Parameters.AddWithValue("@i", i);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Spells
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM spellbooks WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < SpellBook.Length; i++)
            {
                if (SpellBook[i] != null && !SpellBook[i].RequiresAdmin)
                {
                    long cooldown = 0;
                    if (!SpellBook[i].CanUse)
                        cooldown = (long)SpellBook[i].NextAvailableUse.Subtract(DateTime.UtcNow).TotalSeconds;
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO spellbooks VALUES (@character_id, @spellname, @rank, @level, @cooldown, @i)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@spellname", SpellBook[i].GetType().Name);
                    com.Parameters.AddWithValue("@rank", SpellBook[i].Rank);
                    com.Parameters.AddWithValue("@level", SpellBook[i].Level);
                    com.Parameters.AddWithValue("@cooldown", cooldown);
                    com.Parameters.AddWithValue("@i", i);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Spell Bar
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM spellbars WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            foreach (Spell s in Statuses.Values)
            {
                if (s != null && !s.Channeled && !s.Aura)
                {
                    var sb = new StringBuilder();
                    foreach (var arg in s.Arguments)
                    {
                        sb.AppendFormat("{0};{1}\n", arg.Key, arg.Value);
                    }
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO spellbars VALUES (@character_id, @spellname, @rank, @timeleft, @args)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@spellname", s.GetType().Name);
                    com.Parameters.AddWithValue("@rank", s.Rank);
                    com.Parameters.AddWithValue("@timeleft", s.TimeLeft);
                    com.Parameters.AddWithValue("@args", sb.ToString());
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Inventory
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM inventories WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < Inventory.Length; i++)
            {
                if (Inventory[i] != null)
                {
                    var item = Inventory[i];
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO inventories VALUES (@character_id, @itemname, @color, @amount, @durability, @soulbound, @i, @data, @maxhpmod, @maxmpmod, @strmod, @intmod, @wismod, @conmod, @dexmod, @hitmod, @dmgmod, @acmod, @mrmod, @minattackmod, @maxattackmod, @minmagicmod, @maxmagicmod)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@itemname", item.GetType().Name);
                    com.Parameters.AddWithValue("@color", item.Color);
                    com.Parameters.AddWithValue("@amount", item.Amount);
                    com.Parameters.AddWithValue("@durability", item.CurrentDurability);
                    com.Parameters.AddWithValue("@soulbound", item.Soulbound);
                    com.Parameters.AddWithValue("@i", i);
                    com.Parameters.AddWithValue("@data", item.MiscData);
                    com.Parameters.AddWithValue("@maxhpmod", item.DynamicMaximumHpMod);
                    com.Parameters.AddWithValue("@maxmpmod", item.DynamicMaximumMpMod);
                    com.Parameters.AddWithValue("@strmod", item.DynamicStrMod);
                    com.Parameters.AddWithValue("@intmod", item.DynamicIntMod);
                    com.Parameters.AddWithValue("@wismod", item.DynamicWisMod);
                    com.Parameters.AddWithValue("@conmod", item.DynamicConMod);
                    com.Parameters.AddWithValue("@dexmod", item.DynamicDexMod);
                    com.Parameters.AddWithValue("@hitmod", item.DynamicHitMod);
                    com.Parameters.AddWithValue("@dmgmod", item.DynamicDmgMod);
                    com.Parameters.AddWithValue("@acmod", item.DynamicArmorClassMod);
                    com.Parameters.AddWithValue("@mrmod", item.DynamicMagicResistanceMod);
                    com.Parameters.AddWithValue("@minattackmod", item.DynamicMinimumAttackPowerMod);
                    com.Parameters.AddWithValue("@maxattackmod", item.DynamicMaximumAttackPowerMod);
                    com.Parameters.AddWithValue("@minmagicmod", item.DynamicMinimumMagicPowerMod);
                    com.Parameters.AddWithValue("@maxmagicmod", item.DynamicMaximumMagicPowerMod);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Bags
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM bags WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < BagItems.Length; i++)
            {
                if (BagItems[i] != null)
                {
                    var item = BagItems[i];
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO bags VALUES (@character_id, @itemname, @color, @amount, @durability, @soulbound, @i, @data, @maxhpmod, @maxmpmod, @strmod, @intmod, @wismod, @conmod, @dexmod, @hitmod, @dmgmod, @acmod, @mrmod, @minattackmod, @maxattackmod, @minmagicmod, @maxmagicmod)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@itemname", item.GetType().Name);
                    com.Parameters.AddWithValue("@color", item.Color);
                    com.Parameters.AddWithValue("@amount", item.Amount);
                    com.Parameters.AddWithValue("@durability", item.CurrentDurability);
                    com.Parameters.AddWithValue("@soulbound", item.Soulbound);
                    com.Parameters.AddWithValue("@i", i);
                    com.Parameters.AddWithValue("@data", item.MiscData);
                    com.Parameters.AddWithValue("@maxhpmod", item.DynamicMaximumHpMod);
                    com.Parameters.AddWithValue("@maxmpmod", item.DynamicMaximumMpMod);
                    com.Parameters.AddWithValue("@strmod", item.DynamicStrMod);
                    com.Parameters.AddWithValue("@intmod", item.DynamicIntMod);
                    com.Parameters.AddWithValue("@wismod", item.DynamicWisMod);
                    com.Parameters.AddWithValue("@conmod", item.DynamicConMod);
                    com.Parameters.AddWithValue("@dexmod", item.DynamicDexMod);
                    com.Parameters.AddWithValue("@hitmod", item.DynamicHitMod);
                    com.Parameters.AddWithValue("@dmgmod", item.DynamicDmgMod);
                    com.Parameters.AddWithValue("@acmod", item.DynamicArmorClassMod);
                    com.Parameters.AddWithValue("@mrmod", item.DynamicMagicResistanceMod);
                    com.Parameters.AddWithValue("@minattackmod", item.DynamicMinimumAttackPowerMod);
                    com.Parameters.AddWithValue("@maxattackmod", item.DynamicMaximumAttackPowerMod);
                    com.Parameters.AddWithValue("@minmagicmod", item.DynamicMinimumMagicPowerMod);
                    com.Parameters.AddWithValue("@maxmagicmod", item.DynamicMaximumMagicPowerMod);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Parcels
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM parcels WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < Parcels.Count; i++)
            {
                if (Parcels[i] != null)
                {
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO parcels VALUES (@character_id, @sender, @itemname, @color, @amount, @durability, @gold)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@sender", Parcels[i].Sender);
                    if (Parcels[i].Item != null)
                    {
                        com.Parameters.AddWithValue("@itemname", Parcels[i].Item.GetType().Name);
                        com.Parameters.AddWithValue("@color", Parcels[i].Item.Color);
                        com.Parameters.AddWithValue("@amount", Parcels[i].Item.Amount);
                        com.Parameters.AddWithValue("@durability", Parcels[i].Item.CurrentDurability);
                    }
                    else
                    {
                        com.Parameters.AddWithValue("@itemname", string.Empty);
                        com.Parameters.AddWithValue("@color", 0);
                        com.Parameters.AddWithValue("@amount", 0);
                        com.Parameters.AddWithValue("@durability", 0);
                    }
                    com.Parameters.AddWithValue("@gold", Parcels[i].Gold);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Equipment
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM equipments WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            for (int i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i] != null)
                {
                    var item = Equipment[i];
                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO equipments VALUES (@character_id, @itemname, @color, @durability, @soulbound, @i, @data, @maxhpmod, @maxmpmod, @strmod, @intmod, @wismod, @conmod, @dexmod, @hitmod, @dmgmod, @acmod, @mrmod, @minattackmod, @maxattackmod, @minmagicmod, @maxmagicmod)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@itemname", item.GetType().Name);
                    com.Parameters.AddWithValue("@color", item.Color);
                    com.Parameters.AddWithValue("@durability", item.CurrentDurability);
                    com.Parameters.AddWithValue("@soulbound", item.Soulbound);
                    com.Parameters.AddWithValue("@i", i);
                    com.Parameters.AddWithValue("@data", item.MiscData);
                    com.Parameters.AddWithValue("@maxhpmod", item.DynamicMaximumHpMod);
                    com.Parameters.AddWithValue("@maxmpmod", item.DynamicMaximumMpMod);
                    com.Parameters.AddWithValue("@strmod", item.DynamicStrMod);
                    com.Parameters.AddWithValue("@intmod", item.DynamicIntMod);
                    com.Parameters.AddWithValue("@wismod", item.DynamicWisMod);
                    com.Parameters.AddWithValue("@conmod", item.DynamicConMod);
                    com.Parameters.AddWithValue("@dexmod", item.DynamicDexMod);
                    com.Parameters.AddWithValue("@hitmod", item.DynamicHitMod);
                    com.Parameters.AddWithValue("@dmgmod", item.DynamicDmgMod);
                    com.Parameters.AddWithValue("@acmod", item.DynamicArmorClassMod);
                    com.Parameters.AddWithValue("@mrmod", item.DynamicMagicResistanceMod);
                    com.Parameters.AddWithValue("@minattackmod", item.DynamicMinimumAttackPowerMod);
                    com.Parameters.AddWithValue("@maxattackmod", item.DynamicMaximumAttackPowerMod);
                    com.Parameters.AddWithValue("@minmagicmod", item.DynamicMinimumMagicPowerMod);
                    com.Parameters.AddWithValue("@maxmagicmod", item.DynamicMaximumMagicPowerMod);
                    com.ExecuteNonQuery();
                }
            }
            #endregion
            #region Legend
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM legends WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            foreach (var value in Legend.Values)
            {
                com = Program.MySqlConnection.CreateCommand();
                com.CommandText = "INSERT INTO legends VALUES (@character_id, @legendname, @date, @dateupdated, @arguments)";
                com.Parameters.AddWithValue("@character_id", GUID);
                com.Parameters.AddWithValue("@legendname", value.GetType().Name);
                com.Parameters.AddWithValue("@date", value.DateCreated);
                com.Parameters.AddWithValue("@dateupdated", value.DateUpdated);
                com.Parameters.AddWithValue("@arguments", string.Join("\n", value.Arguments));
                com.ExecuteNonQuery();
            }
            #endregion
            #region Cookies
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM cookies WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            foreach (var kvp in Cookies)
            {
                com = Program.MySqlConnection.CreateCommand();
                com.CommandText = "INSERT INTO cookies VALUES (@character_id, @c_key, @c_value)";
                com.Parameters.AddWithValue("@character_id", GUID);
                com.Parameters.AddWithValue("@c_key", kvp.Key);
                com.Parameters.AddWithValue("@c_value", kvp.Value);
                com.ExecuteNonQuery();
            }
            #endregion
            #region Quests
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM quests WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM quest_steps WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM quest_objectives WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();

            foreach (var kvp in Quests)
            {
                if (kvp.Value.Progress == QuestProgress.Unstarted)
                    continue;

                com = Program.MySqlConnection.CreateCommand();
                com.CommandText = "INSERT INTO quests VALUES (@character_id, @quest_name, @current_step, @progress)";
                com.Parameters.AddWithValue("@character_id", GUID);
                com.Parameters.AddWithValue("@quest_name", kvp.Key);
                com.Parameters.AddWithValue("@current_step", kvp.Value.CurrentStep);
                com.Parameters.AddWithValue("@progress", kvp.Value.Progress.ToString());
                com.ExecuteNonQuery();

                for (int i = 0; i < kvp.Value.Steps.Count; i++)
                {
                    var qs = kvp.Value.Steps[i];

                    com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "INSERT INTO quest_steps VALUES (@character_id, @quest_name, @step, @progress)";
                    com.Parameters.AddWithValue("@character_id", GUID);
                    com.Parameters.AddWithValue("@quest_name", kvp.Key);
                    com.Parameters.AddWithValue("@step", i + 1);
                    com.Parameters.AddWithValue("@progress", qs.Progress.ToString());
                    com.ExecuteNonQuery();

                    foreach (var qo in qs.Objectives)
                    {
                        if (qo.Value.Count == 0 && string.IsNullOrEmpty(qo.Value.MiscData))
                            continue;

                        com = Program.MySqlConnection.CreateCommand();
                        com.CommandText = "INSERT INTO quest_objectives VALUES (@character_id, @questname, @queststep, @objectivename, @count, @miscdata)";
                        com.Parameters.AddWithValue("@character_id", GUID);
                        com.Parameters.AddWithValue("@questname", kvp.Key);
                        com.Parameters.AddWithValue("@queststep", i + 1);
                        com.Parameters.AddWithValue("@objectivename", qo.Value.Name);
                        com.Parameters.AddWithValue("@count", qo.Value.Count);
                        com.Parameters.AddWithValue("@miscdata", qo.Value.MiscData ?? string.Empty);
                        com.ExecuteNonQuery();
                    }
                }
            }
            #endregion
            #region Professions
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM professions WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "INSERT INTO professions VALUES (@character_id, @list)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.Parameters.AddWithValue("@list", string.Join(",", AvailableManufactures));
            com.ExecuteNonQuery();
            #endregion
            #region Professions
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "DELETE FROM visited_maps WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.ExecuteNonQuery();
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "INSERT INTO visited_maps VALUES (@character_id, @list)";
            com.Parameters.AddWithValue("@character_id", GUID);
            com.Parameters.AddWithValue("@list", string.Join(",", VisitedMaps));
            com.ExecuteNonQuery();
            #endregion
            LastSave = DateTime.UtcNow;
        }
        public static Player Create(GameServer gs, string name)
        {
            Player p = new Player(gs, name);
            p.Point = new Point(gs.StartPoint.X, gs.StartPoint.Y);
            p.Map = gs.MapDatabase[gs.StartMap];
            p.DeathPoint = new Point(gs.StartPoint.X, gs.StartPoint.Y);
            p.DeathMap = gs.MapDatabase[gs.StartMap];
            p.BaseArmorClass = 100;
            p.BaseMagicResistance = 0;
            p.BaseStr = 5;
            p.BaseInt = 5;
            p.BaseWis = 5;
            p.BaseCon = 5;
            p.BaseDex = 5;
            p.BaseDmg = 0;
            p.BaseHit = 0;
            p.CurrentHP = 150;
            p.CurrentMP = 150;
            p.BaseMaximumHP = 150;
            p.BaseMaximumMP = 150;
            p.Level = 1;
            p.Ability = 1;
            p.DisplayBitmask = 0x0010;
            return p;
        }

        public int FindEmptyInventoryIndex()
        {
            for (int i = 0; i < Inventory.Length; i++)
            {
                if (Inventory[i] == null)
                    return i;
            }
            return -1;
        }
        public int FindEmptySkillIndex(SkillPane skillPane)
        {
            int minimum = (skillPane == SkillPane.Primary) ? 0 : (skillPane == SkillPane.Secondary) ? 36 : 72;
            int maximum = (skillPane == SkillPane.Primary) ? 34 : (skillPane == SkillPane.Secondary) ? 70 : 88;

            for (int i = minimum; i <= maximum; i++)
            {
                if (SkillBook[i] == null)
                    return i;
            }

            return -1;
        }
        public int FindEmptySpellIndex(SpellPane spellPane)
        {
            int minimum = (spellPane == SpellPane.Primary) ? 0 : (spellPane == SpellPane.Secondary) ? 36 : 72;
            int maximum = (spellPane == SpellPane.Primary) ? 34 : (spellPane == SpellPane.Secondary) ? 70 : 88;

            for (int i = minimum; i <= maximum; i++)
            {
                if (SpellBook[i] == null)
                    return i;
            }

            return -1;
        }

        public void WriteCookie(string key, object value)
        {
            if (Cookies.ContainsKey(key))
                Cookies[key] = value.ToString();
            else
                Cookies.Add(key, value.ToString());
        }
        public string ReadCookie(string key)
        {
            if (Cookies.ContainsKey(key))
                return Cookies[key];
            else
                return string.Empty;
        }
        public int ReadCookieInt32(string key)
        {
            if (Cookies.ContainsKey(key))
            {
                var cookie = Cookies[key];
                int value;
                if (int.TryParse(cookie, out value))
                    return value;
            }

            return default(int);
        }
        public DateTime ReadCookieDateTime(string key)
        {
            if (Cookies.ContainsKey(key))
            {
                var cookie = Cookies[key];
                DateTime value;
                if (DateTime.TryParse(cookie, out value))
                    return value;
            }

            return default(DateTime);
        }

        public void RewardExperience(long amount)
        {
            var cap = GameServer.ExperienceTable[9];

            if (amount + Experience > cap)
                amount = cap - Experience;

            if (amount > 0)
            {
                if (Dead || Class == Profession.Peasant)
                    return;

                Experience += amount;
                Client.SendStatistics(StatUpdateFlags.Experience);

                Client.SendMessage(string.Format("You received {0} experience!", amount));
            }

            while ((Level < 10) && (ToNextLevel <= Experience))
            {
                AdvanceLevel();
            }
        }
        public void AdvanceLevel()
        {
            Level++;

            AvailableStatPoints += ((Level % 10) == 0) ? 3 : 2;

            switch (Class)
            {
                case Profession.Warrior:
                    {
                        BaseMaximumHP += 200;
                        BaseMaximumMP += 100;
                    } break;
                case Profession.Rogue:
                    {
                        BaseMaximumHP += 200;
                        BaseMaximumMP += 100;
                    } break;
                case Profession.Wizard:
                    {
                        BaseMaximumHP += 100;
                        BaseMaximumMP += 200;
                    } break;
                case Profession.Priest:
                    {
                        BaseMaximumHP += 100;
                        BaseMaximumMP += 200;
                    } break;
                case Profession.Monk:
                    {
                        BaseMaximumHP += 150;
                        BaseMaximumMP += 150;
                    } break;
            }

            CurrentHP = MaximumHP;
            CurrentMP = MaximumMP;

            Client.SendStatistics(StatUpdateFlags.Primary | StatUpdateFlags.Current |StatUpdateFlags.Experience);
            Client.SendMessage(string.Format("You have reached level {0}!", Level));
            SpellAnimation(79, 100);

            foreach (var s in SpellBook)
            {
                if (s != null)
                {
                    for (int i = (s.Ranks.Length - 1); i > 0; i--)
                    {
                        if (s.Ranks[i].RequiredLevel <= Level && s.Ranks[i].RequiredAbility <= Ability && (!s.Ranks[i].RequiresMaster || Master) && s.Ranks[i].AutomaticRankUp)
                        {
                            if (s.Rank < (i + 1))
                            {
                                s.Rank = (i + 1);
                                Client.SendMessage(string.Format("{0} is now Rank {1}!", s.Name, s.Rank));
                                DisplaySpell(s);
                            }
                            break;
                        }
                    }
                }
            }

            foreach (var s in SkillBook)
            {
                if (s != null)
                {
                    for (int i = (s.Ranks.Length - 1); i > 0; i--)
                    {
                        if (s.Ranks[i].RequiredLevel <= Level && s.Ranks[i].RequiredAbility <= Ability && (!s.Ranks[i].RequiresMaster || Master) && s.Ranks[i].AutomaticRankUp)
                        {
                            if (s.Rank < (i + 1))
                            {
                                s.Rank = (i + 1);
                                Client.SendMessage(string.Format("{0} is now Rank {1}!", s.Name, s.Rank));
                                DisplaySkill(s);
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void RewardAbilityExp(long amount)
        {
            var cap = GameServer.AbilityExpTable[9];

            if (amount + AbilityExp > cap)
                amount = cap - AbilityExp;

            if (amount > 0)
            {
                if (Dead || Class == Profession.Peasant)
                    return;

                AbilityExp += amount;
                Client.SendStatistics(StatUpdateFlags.Experience);

                Client.SendMessage(string.Format("You received {0} ability exp!", amount));
            }

            while ((Ability < 10) && (ToNextAbility <= AbilityExp))
            {
                AdvanceAbility();
            }
        }
        public void AdvanceAbility()
        {
            Ability++;

            Client.SendStatistics(StatUpdateFlags.Primary | StatUpdateFlags.Experience);
            Client.SendMessage(string.Format("You have reached ability {0}!", Ability));
            SpellAnimation(191, 100);
        }

        public override bool Turn(Direction direction)
        {
            return Turn(direction, false);
        }
        public override bool Turn(Direction direction, bool forcefully)
        {
            if (!forcefully && (Sleeping || Frozen || Coma || Dizzy || MindControlled || Polymorphed))
            {
                var p = new ServerPacket(0x11);
                p.WriteUInt32(ID);
                p.WriteByte((byte)Direction);
                Client.Enqueue(p);
                return false;
            }

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

        public bool RecieveParcel()
        {
            if (Parcels.Count == 0)
            {
                Client.SendMessage("You do not have any parcels.");
                return false;
            }

            if (ExchangeInfo != null)
                return false;

            var parcel = Parcels[0];
            var item = parcel.Item;
            var gold = parcel.Gold;

            if (item != null)
            {
                if (item.Weight > AvailableWeight)
                {
                    Client.SendMessage("That item is too heavy.");
                    return false;
                }
                var index = FindEmptyInventoryIndex();
                if (index < 0)
                {
                    Client.SendMessage("You cannot hold anymore items.");
                    return false;
                }
                AddItem(item);
                Client.SendMessage(string.Format("You received {0} from {1}.", parcel.Item.Name, parcel.Sender));
            }

            if (gold > 0)
            {
                Gold += gold;
                Client.SendStatistics(StatUpdateFlags.Experience);
                Client.SendMessage(string.Format("You received {0} gold from {1}.", parcel.Gold, parcel.Sender));
            }

            Parcels.RemoveAt(0);

            return true;
        }

        public void ReduceDurability(Equipment e, int amount)
        {
            if (--e.CurrentDurability < 1)
            {
                if (e.CurrentDurability < 0)
                    e.CurrentDurability = 0;
                var index = FindEmptyInventoryIndex();
                if (index != -1)
                {
                    var item = e;
                    RemoveEquipment(item);
                    AddItem(item, index);
                    Display();
                    Client.SendMessage(string.Format("Your {0} is worn out and cannot be used anymore.",
                        item.Name));
                }
            }
        }

        public void UpdateBag()
        {
            var packet = new ServerPacket(0x61);
            packet.WriteByte(0x05);
            packet.WriteByte(0x00);
            packet.WriteByte((byte)(AvailableBagSlots / 6));
            packet.WriteUInt32(ushort.MaxValue);
            byte count = 0;
            foreach (var item in BagItems)
            {
                if (item != null)
                    count++;
            }
            packet.WriteByte(count);
            foreach (var item in BagItems)
            {
                if (item != null)
                {
                    packet.WriteByte((byte)item.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + item.Sprite));
                    packet.WriteUInt16((ushort)item.Color);
                    packet.WriteString8(item.Name);
                    packet.WriteString8(item.GetType().Name);
                    packet.WriteUInt32((uint)item.Amount);
                    packet.WriteByte(item.CanStack);
                    packet.WriteUInt32((uint)item.MaximumDurability);
                    packet.WriteUInt32((uint)item.CurrentDurability);
                    packet.Write(new byte[] { 0, 0, 0 });
                }
            }
            Client.Enqueue(packet);
        }

        public void Cursor(int mode)
        {
            var packet = new ServerPacket(0x51);
            packet.WriteByte((byte)mode);
            packet.WriteByte(0x00);
            packet.WriteByte(0x00);
            Client.Enqueue(packet);
        }

        public bool AddBagItem(Item item, int index)
        {
            if (item.Amount < 1)
                return false;

            int amount = 0;

            if (ExchangeInfo == null)
            {
                if (item.CanStack && BagItems.Contains(item.GetType().Name))
                {
                    int oldIndex = BagItems.IndexOf(item.GetType().Name);
                    var newItem = BagItems[oldIndex];

                    while (item.Amount > 0 && newItem.Amount < newItem.MaxStack)
                    {
                        item.Amount--;
                        newItem.Amount++;
                        amount++;
                    }

                    var packet = new ServerPacket(0x61);
                    packet.WriteByte(0x05);
                    packet.WriteByte(0x02);
                    packet.WriteByte((byte)newItem.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + newItem.Sprite));
                    packet.WriteUInt16((ushort)newItem.Color);
                    packet.WriteString8(newItem.Name);
                    packet.WriteString8(newItem.GetType().Name);
                    packet.WriteUInt32((uint)newItem.Amount);
                    packet.WriteByte(newItem.CanStack);
                    packet.WriteUInt32((uint)newItem.MaximumDurability);
                    packet.WriteUInt32((uint)newItem.CurrentDurability);
                    packet.Write(new byte[] { 0, 0, 0 });
                    Client.Enqueue(packet);
                }
                else if ((index >= 0) && (index < BagItems.Length) && (BagItems[index] == null))
                {
                    var newItem = GameServer.CreateItem(item.GetType().Name);
                    newItem.Amount = 0;

                    while (item.Amount > 0 && newItem.Amount < newItem.MaxStack)
                    {
                        item.Amount--;
                        newItem.Amount++;
                        amount++;
                    }

                    newItem.Color = item.Color;
                    newItem.CurrentDurability = item.CurrentDurability;
                    newItem.Soulbound = item.Soulbound;
                    newItem.MiscData = item.MiscData;

                    if (newItem.BindType == BindType.BindOnPickup)
                        newItem.Soulbound = true;

                    newItem.Slot = (index + 1);
                    BagItems[index] = newItem;

                    var packet = new ServerPacket(0x61);
                    packet.WriteByte(0x05);
                    packet.WriteByte(0x02);
                    packet.WriteByte((byte)newItem.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + newItem.Sprite));
                    packet.WriteUInt16((ushort)newItem.Color);
                    packet.WriteString8(newItem.Name);
                    packet.WriteString8(newItem.GetType().Name);
                    packet.WriteUInt32((uint)newItem.Amount);
                    packet.WriteByte(newItem.CanStack);
                    packet.WriteUInt32((uint)newItem.MaximumDurability);
                    packet.WriteUInt32((uint)newItem.CurrentDurability);
                    packet.Write(new byte[] { 0, 0, 0 });
                    Client.Enqueue(packet);
                }
            }

            if (item.Amount > 0)
            {
                Map.InsertCharacter(item, Point.X, Point.Y);
            }

            return (amount > 0);
        }
        public Item RemoveBagItem(int index, int amount)
        {
            if (index < 0 || index >= BagItems.Length || BagItems[index] == null)
                return null;

            var item = BagItems[index];

            if (amount > item.Amount)
                amount = item.Amount;

            var newItem = GameServer.CreateItem(item.GetType().Name);

            item.Amount -= amount;
            newItem.Amount = amount;

            newItem.Color = item.Color;
            newItem.CurrentDurability = item.CurrentDurability;
            newItem.Soulbound = item.Soulbound;
            newItem.MiscData = item.MiscData;

            if (item.Amount > 0)
            {
                var packet = new ServerPacket(0x61);
                packet.WriteByte(0x05);
                packet.WriteByte(0x02);
                packet.WriteByte((byte)item.Slot);
                packet.WriteUInt16((ushort)(0x8000 + newItem.Sprite));
                packet.WriteUInt16((ushort)item.Color);
                packet.WriteString8(item.Name);
                packet.WriteString8(item.GetType().Name);
                packet.WriteUInt32((uint)item.Amount);
                packet.WriteByte(item.CanStack);
                packet.WriteUInt32((uint)item.MaximumDurability);
                packet.WriteUInt32((uint)item.CurrentDurability);
                packet.Write(new byte[] { 0, 0, 0 });
                Client.Enqueue(packet);
            }
            else
            {
                BagItems[item.Slot - 1] = null;

                var packet = new ServerPacket(0x61);
                packet.WriteByte(0x05);
                packet.WriteByte(0x03);
                packet.WriteByte((byte)(index + 1));
                Client.Enqueue(packet);
            }

            return newItem;
        }

        public override bool AddItem(Item item)
        {
            return AddItem(item, FindEmptyInventoryIndex());
        }
        public override bool AddItem(Item item, int index)
        {
            if (item.Amount < 1)
                return false;

            int amount = 0;

            if (ExchangeInfo == null)
            {
                if (item.CanStack && Inventory.Contains(item.GetType().Name))
                {
                    int oldIndex = Inventory.IndexOf(item.GetType().Name);
                    var newItem = Inventory[oldIndex];

                    while (item.Amount > 0 && newItem.Amount < newItem.MaxStack)
                    {
                        item.Amount--;
                        newItem.Amount++;
                        amount++;
                    }

                    var packet = new ServerPacket(0x0F);
                    packet.WriteByte((byte)newItem.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + newItem.Sprite));
                    packet.WriteUInt16((ushort)newItem.Color);
                    packet.WriteString8(newItem.Name);
                    packet.WriteString8(newItem.GetType().Name);
                    packet.WriteUInt32((uint)newItem.Amount);
                    packet.WriteByte(newItem.CanStack);
                    packet.WriteUInt32((uint)newItem.MaximumDurability);
                    packet.WriteUInt32((uint)newItem.CurrentDurability);
                    packet.WriteUInt32(0x00);
                    Client.Enqueue(packet);
                }
                else if (item.Weight > AvailableWeight)
                {
                    Client.SendMessage("That item is too heavy.");
                }
                else if ((index >= 0) && (index < Inventory.Length) && (Inventory[index] == null))
                {
                    var newItem = GameServer.CreateItem(item.GetType().Name);
                    newItem.Amount = 0;

                    while (item.Amount > 0 && newItem.Amount < newItem.MaxStack)
                    {
                        item.Amount--;
                        newItem.Amount++;
                        amount++;
                    }

                    newItem.Color = item.Color;
                    newItem.CurrentDurability = item.CurrentDurability;
                    newItem.Soulbound = item.Soulbound;
                    newItem.MiscData = item.MiscData;
                    newItem.DynamicMaximumHpMod = item.DynamicMaximumHpMod;
                    newItem.DynamicMaximumMpMod = item.DynamicMaximumMpMod;
                    newItem.DynamicStrMod = item.DynamicStrMod;
                    newItem.DynamicIntMod = item.DynamicIntMod;
                    newItem.DynamicWisMod = item.DynamicWisMod;
                    newItem.DynamicConMod = item.DynamicConMod;
                    newItem.DynamicDexMod = item.DynamicDexMod;
                    newItem.DynamicHitMod = item.DynamicHitMod;
                    newItem.DynamicDmgMod = item.DynamicDmgMod;
                    newItem.DynamicArmorClassMod = item.DynamicArmorClassMod;
                    newItem.DynamicMagicResistanceMod = item.DynamicMagicResistanceMod;
                    newItem.DynamicMinimumAttackPowerMod = item.DynamicMinimumAttackPowerMod;
                    newItem.DynamicMaximumAttackPowerMod = item.DynamicMaximumAttackPowerMod;
                    newItem.DynamicMinimumMagicPowerMod = item.DynamicMinimumMagicPowerMod;
                    newItem.DynamicMaximumMagicPowerMod = item.DynamicMaximumMagicPowerMod;

                    if (newItem.BindType == BindType.BindOnPickup)
                        newItem.Soulbound = true;

                    newItem.Slot = (index + 1);
                    Inventory[index] = newItem;
                    CurrentWeight += newItem.Weight;
                    Client.SendStatistics(StatUpdateFlags.Primary);

                    var packet = new ServerPacket(0x0F);
                    packet.WriteByte((byte)newItem.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + newItem.Sprite));
                    packet.WriteUInt16((ushort)newItem.Color);
                    packet.WriteString8(newItem.Name);
                    packet.WriteString8(newItem.GetType().Name);
                    packet.WriteUInt32((uint)newItem.Amount);
                    packet.WriteByte(newItem.CanStack);
                    packet.WriteUInt32((uint)newItem.MaximumDurability);
                    packet.WriteUInt32((uint)newItem.CurrentDurability);
                    packet.WriteUInt32(0x00);
                    Client.Enqueue(packet);
                }
            }

            if (item.Amount > 0)
            {
                Map.InsertCharacter(item, Point.X, Point.Y);
            }

            return (amount > 0);
        }

        public Item RemoveItem(Item item)
        {
            return RemoveItem(item, 1);
        }
        public Item RemoveItem(Item item, int amount)
        {
            if ((item != null) && (item.Slot > 0) && (item.Slot <= Inventory.Length) && (Inventory[item.Slot - 1] == item))
            {
                if (amount > item.Amount)
                    amount = item.Amount;

                var newItem = GameServer.CreateItem(item.GetType().Name);

                item.Amount -= amount;
                newItem.Amount = amount;

                newItem.Color = item.Color;
                newItem.CurrentDurability = item.CurrentDurability;
                newItem.Soulbound = item.Soulbound;
                newItem.MiscData = item.MiscData;
                newItem.DynamicMaximumHpMod = item.DynamicMaximumHpMod;
                newItem.DynamicMaximumMpMod = item.DynamicMaximumMpMod;
                newItem.DynamicStrMod = item.DynamicStrMod;
                newItem.DynamicIntMod = item.DynamicIntMod;
                newItem.DynamicWisMod = item.DynamicWisMod;
                newItem.DynamicConMod = item.DynamicConMod;
                newItem.DynamicDexMod = item.DynamicDexMod;
                newItem.DynamicHitMod = item.DynamicHitMod;
                newItem.DynamicDmgMod = item.DynamicDmgMod;
                newItem.DynamicArmorClassMod = item.DynamicArmorClassMod;
                newItem.DynamicMagicResistanceMod = item.DynamicMagicResistanceMod;
                newItem.DynamicMinimumAttackPowerMod = item.DynamicMinimumAttackPowerMod;
                newItem.DynamicMaximumAttackPowerMod = item.DynamicMaximumAttackPowerMod;
                newItem.DynamicMinimumMagicPowerMod = item.DynamicMinimumMagicPowerMod;
                newItem.DynamicMaximumMagicPowerMod = item.DynamicMaximumMagicPowerMod;

                if (item.Amount > 0)
                {
                    var packet = new ServerPacket(0x0F);
                    packet.WriteByte((byte)item.Slot);
                    packet.WriteUInt16((ushort)(0x8000 + item.Sprite));
                    packet.WriteUInt16((ushort)item.Color);
                    packet.WriteString8(item.Name);
                    packet.WriteString8(item.GetType().Name);
                    packet.WriteUInt32((uint)item.Amount);
                    packet.WriteByte(item.CanStack);
                    packet.WriteUInt32((uint)item.MaximumDurability);
                    packet.WriteUInt32((uint)item.CurrentDurability);
                    packet.WriteUInt32(0x00);
                    Client.Enqueue(packet);
                }
                else
                {
                    Inventory[item.Slot - 1] = null;
                    CurrentWeight -= item.Weight;
                    Client.SendStatistics(StatUpdateFlags.Primary);

                    var packet = new ServerPacket(0x10);
                    packet.WriteByte((byte)item.Slot);
                    packet.WriteUInt16(0x0000);
                    packet.WriteByte(0x00);
                    Client.Enqueue(packet);
                }

                return newItem;
            }

            return null;
        }

        public Item RemoveItem(int index)
        {
            return RemoveItem(index, 1);
        }
        public Item RemoveItem(int index, int amount)
        {
            if ((index >= 0) && (index < Inventory.Length) && (Inventory[index] != null))
                return RemoveItem(Inventory[index], amount);
            return null;
        }

        public void RemoveItem(string name)
        {
            RemoveItem(name, 1);
        }
        public void RemoveItem(string name, int amount)
        {
            for (int i = 0; i < Inventory.Length && amount > 0; i++)
            {
                var item = Inventory[i];

                if (item != null && item.GetType().Name == name)
                {
                    while (amount > 0 && item.Amount > 0)
                    {
                        amount--;
                        item.Amount--;
                    }
                    if (item.Amount > 0)
                    {
                        var packet = new ServerPacket(0x0F);
                        packet.WriteByte((byte)item.Slot);
                        packet.WriteUInt16((ushort)(0x8000 + item.Sprite));
                        packet.WriteUInt16((ushort)item.Color);
                        packet.WriteString8(item.Name);
                        packet.WriteString8(item.GetType().Name);
                        packet.WriteUInt32((uint)item.Amount);
                        packet.WriteByte(item.CanStack);
                        packet.WriteUInt32((uint)item.MaximumDurability);
                        packet.WriteUInt32((uint)item.CurrentDurability);
                        packet.WriteUInt32(0x00);
                        Client.Enqueue(packet);
                    }
                    else
                    {
                        Inventory[i] = null;
                        CurrentWeight -= item.Weight;
                        Client.SendStatistics(StatUpdateFlags.Primary);

                        var packet = new ServerPacket(0x10);
                        packet.WriteByte((byte)item.Slot);
                        packet.WriteUInt16(0x0000);
                        packet.WriteByte(0x00);
                        Client.Enqueue(packet);
                    }
                }
            }
        }

        public bool AddEquipment(Equipment item)
        {
            return AddEquipment(item, (item.EquipmentSlot - 1));
        }
        public bool AddEquipment(Equipment item, int index)
        {
            if (item.Amount < 1)
                return false;

            if (ExchangeInfo != null)
                return false;

            if (index < 0)
                return false;

            if (Equipment.Length <= index)
                return false;

            if (item.BindType == BindType.BindOnPickup || item.BindType == BindType.BindOnEquip)
                item.Soulbound = true;

            Equipment[index] = item;

            var packet = new ServerPacket(0x37);
            packet.WriteByte((byte)(index + 1));
            packet.WriteUInt16((ushort)(0x8000 + item.Sprite));
            packet.WriteUInt16((ushort)item.Color);
            packet.WriteString8(item.Name);
            packet.WriteString8(item.GetType().Name);
            packet.WriteUInt32((uint)item.MaximumDurability);
            packet.WriteUInt32((uint)item.CurrentDurability);
            Client.Enqueue(packet);
            item.OnEquip(this);

            Client.SendStatistics(StatUpdateFlags.Full);

            return true;
        }
        public Equipment RemoveEquipment(int index)
        {
            var item = Equipment[index];
            if (item != null)
            {
                Equipment[index] = null;
                var packet = new ServerPacket(0x38);
                packet.WriteByte((byte)(index + 1));
                Client.Enqueue(packet);
                item.OnUnequip(this);
                Client.SendStatistics(StatUpdateFlags.Full);
            }
            return item;
        }
        public void RemoveEquipment(Equipment item)
        {
            if (item == null)
                return;

            for (int i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i] == item)
                {
                    Equipment[i] = null;
                    var packet = new ServerPacket(0x38);
                    packet.WriteByte((byte)(i + 1));
                    Client.Enqueue(packet);
                    item.OnUnequip(this);

                    Client.SendStatistics(StatUpdateFlags.Full);
                    break;
                }
            }
        }

        public bool AddSkill(Skill skill)
        {
            return AddSkill(skill, -1);
        }
        public bool AddSkill(Skill skill, int index)
        {
            int emptyIndex = FindEmptySkillIndex(skill.Pane);

            int minimum = (skill.Pane == SkillPane.Primary) ? 0 : (skill.Pane == SkillPane.Secondary) ? 36 : 72;
            int maximum = (skill.Pane == SkillPane.Primary) ? 34 : (skill.Pane == SkillPane.Secondary) ? 70 : 88;

            if (index < minimum && (index = emptyIndex) < 0)
                return false;

            if (index > maximum && (index = emptyIndex) < 0)
                return false;

            for (int i = (skill.Ranks.Length - 1); i > 0; i--)
            {
                if (skill.Ranks[i].RequiredLevel <= Level && skill.Ranks[i].RequiredAbility <= Ability && (!skill.Ranks[i].RequiresMaster || Master) && skill.Ranks[i].AutomaticRankUp)
                {
                    skill.Rank = (i + 1);
                    break;
                }
            }

            skill.Slot = (index + 1);
            SkillBook[index] = skill;
            DisplaySkill(skill);

            return true;
        }
        public Skill RemoveSkill(int index)
        {
            var skill = SkillBook[index];
            if (skill != null)
            {
                SkillBook[index] = null;
                var packet = new ServerPacket(0x2D);
                packet.WriteByte((byte)(index + 1));
                Client.Enqueue(packet);
            }
            return skill;
        }
        public void RemoveSkill(Skill skill)
        {
            if (skill == null)
                return;

            for (int i = 0; i < SkillBook.Length; i++)
            {
                if (SkillBook[i] == skill)
                {
                    SkillBook[i] = null;
                    var packet = new ServerPacket(0x2D);
                    packet.WriteByte((byte)(i + 1));
                    Client.Enqueue(packet);
                    break;
                }
            }
        }
        public void DisplaySkill(Skill skill)
        {
            var packet = new ServerPacket(0x2C);
            packet.WriteByte((byte)skill.Slot);
            packet.WriteUInt16((ushort)skill.Icon);
            packet.WriteString8(skill.ToString());
            packet.WriteByte(0);
            Client.Enqueue(packet);
        }

        public bool AddSpell(Spell spell)
        {
            return AddSpell(spell, -1);
        }
        public bool AddSpell(Spell spell, int index)
        {
            int emptyIndex = FindEmptySpellIndex(spell.Pane);

            int minimum = (spell.Pane == SpellPane.Primary) ? 0 : (spell.Pane == SpellPane.Secondary) ? 36 : 72;
            int maximum = (spell.Pane == SpellPane.Primary) ? 34 : (spell.Pane == SpellPane.Secondary) ? 70 : 88;

            if (index < minimum && (index = emptyIndex) < 0)
                return false;

            if (index > maximum && (index = emptyIndex) < 0)
                return false;

            for (int i = (spell.Ranks.Length - 1); i > 0; i--)
            {
                if (spell.Ranks[i].RequiredLevel <= Level && spell.Ranks[i].RequiredAbility <= Ability && (!spell.Ranks[i].RequiresMaster || Master) && spell.Ranks[i].AutomaticRankUp)
                {
                    spell.Rank = (i + 1);
                    break;
                }
            }

            spell.Slot = (index + 1);
            SpellBook[index] = spell;
            DisplaySpell(spell);

            return true;
        }
        public Spell RemoveSpell(int index)
        {
            var spell = SpellBook[index];
            if (spell != null)
            {
                SpellBook[index] = null;
                var packet = new ServerPacket(0x18);
                packet.WriteByte((byte)(index + 1));
                Client.Enqueue(packet);
            }
            return spell;
        }
        public void RemoveSpell(Spell spell)
        {
            if (spell == null)
                return;

            for (int i = 0; i < SpellBook.Length; i++)
            {
                if (SpellBook[i] == spell)
                {
                    SpellBook[i] = null;
                    var packet = new ServerPacket(0x18);
                    packet.WriteByte((byte)(i + 1));
                    Client.Enqueue(packet);
                    break;
                }
            }
        }
        public void DisplaySpell(Spell spell)
        {
            var packet = new ServerPacket(0x17);
            packet.WriteByte((byte)spell.Slot);
            packet.WriteUInt16((ushort)spell.Icon);
            packet.WriteByte((byte)spell.CastType);
            packet.WriteString8(spell.ToString());
            packet.WriteString8(spell.Text);
            packet.WriteByte((byte)spell.CastLines);
            Client.Enqueue(packet);
        }

        public void FinishExchange()
        {
            var trader = ExchangeInfo.Trader;

            var exchangeA = this.ExchangeInfo;
            var exchangeB = trader.ExchangeInfo;

            var itemsA = exchangeA.Items.ToArray();
            var itemsB = exchangeB.Items.ToArray();

            var goldA = exchangeA.Gold;
            var goldB = exchangeB.Gold;

            this.ExchangeInfo = null;
            trader.ExchangeInfo = null;

            foreach (var item in itemsB)
                this.AddItem(item);
            exchangeA.Items.Clear();

            foreach (var item in itemsA)
                trader.AddItem(item);
            exchangeB.Items.Clear();

            this.Gold += goldB;
            this.Client.SendStatistics(StatUpdateFlags.Experience);

            trader.Gold += goldA;
            trader.Client.SendStatistics(StatUpdateFlags.Experience);
        }
        public void CancelExchange()
        {
            var trader = ExchangeInfo.Trader;

            var exchangeA = this.ExchangeInfo;
            var exchangeB = trader.ExchangeInfo;

            var itemsA = exchangeA.Items.ToArray();
            var itemsB = exchangeB.Items.ToArray();

            var goldA = exchangeA.Gold;
            var goldB = exchangeB.Gold;

            this.ExchangeInfo = null;
            trader.ExchangeInfo = null;

            foreach (var item in itemsA)
                this.AddItem(item);
            exchangeA.Items.Clear();

            foreach (var item in itemsB)
                trader.AddItem(item);
            exchangeB.Items.Clear();

            this.Gold += goldA;
            this.Client.SendStatistics(StatUpdateFlags.Experience);

            trader.Gold += goldB;
            trader.Client.SendStatistics(StatUpdateFlags.Experience);

            var packet = new ServerPacket(0x42);
            packet.WriteByte(0x04);
            packet.WriteByte(0x00);
            packet.WriteString8("Exchange cancelled");
            Client.Enqueue(packet);

            packet = new ServerPacket(0x42);
            packet.WriteByte(0x04);
            packet.WriteByte(0x01);
            packet.WriteString8("Exchange cancelled");
            trader.Client.Enqueue(packet);
        }

        public void CancelSpellCast()
        {
            if (IsCasting)
            {
                IsCasting = false;
                var packet = new ServerPacket(0x48);
                packet.WriteByte(0x00);
                Client.Enqueue(packet);
            }
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }

    public class Exchange
    {
        public Player Trader { get; set; }
        public List<Item> Items { get; set; }
        public long Gold { get; set; }
        public bool Confirmed { get; set; }
        public int Weight { get; set; }
        public Exchange(Player t)
        {
            this.Trader = t;
            this.Items = new List<Item>();
        }
    }
}