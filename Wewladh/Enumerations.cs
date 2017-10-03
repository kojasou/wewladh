using System;

namespace Wewladh
{
    public enum MonsterType
    {
        Normal,
        Nonsolid,
        Merchant,
        Guardian,
        Reactor
    }

    public enum ArenaGame
    {
        TeamBattle,
        CaptureTheFlag
    }

    public enum DamageType : byte
    {
        RawDamage,
        Physical,
        Magical
    }

    [Flags]
    public enum DamageFlags : byte
    {
        None = 0,
        DamageOverTime = 1,
        CanBeAbsorbed = 2,
        CanBeRedirected = 4,
        CanBeConvertedToManaDamage = 8,
        DoNotRemoveSleep = 16,
        Standard = CanBeAbsorbed | CanBeRedirected | CanBeConvertedToManaDamage
    }

    public enum BindType : byte
    {
        None,
        BindOnPickup,
        BindOnEquip,
        BindToAccount,
        BindOnUse
    }

    [Flags]
    public enum AdminRights : uint
    {
        None = 0,
        ArenaHost = 1,
        CanSetMap = 2,
        CanTeleport = 4,
        CanMoveUser = 8,
        CanSummonUser = 16,
        CanLocateUser = 32,
        CanMonsterForm = 64,
        CanWalkThroughWalls = 128,
        CanWalkThroughUnits = 256,
        CanCreateItems = 512,
        CanCreateMonsters = 1024,
        CanCreateMerchants = 2048,
        CanCreateSkills = 4096,
        CanCreateSpells = 8192,
        CanKickUser = 16384,
        CanChangeStat = 32768,
        CanStealth = 65536,
        CanGlobalShout = 131072,
        CanUseCommands = 262144,
        CanDropSoulboundItems = 524288,
        IgnoreLevelRestrictions = 1048576,
        IgnoreClassRestrictions = 2097152,
        NoManaCost = 4194304,
        NoCooldowns = 8388608,
        GameMaster = 2147483648,

        Creator = CanCreateItems | CanCreateMonsters | CanCreateMerchants | CanCreateSkills | CanCreateSpells,
        Teleporter = CanTeleport | CanLocateUser | CanMonsterForm | CanWalkThroughWalls,
        Builder = Teleporter | CanCreateItems | CanChangeStat | IgnoreLevelRestrictions | IgnoreClassRestrictions,
        Everything = uint.MaxValue
    }

    public enum Time : byte
    {
        Dusk = 0,
        Sunset = 1,
        Sunrise = 2,
        Dawn = 3
    }

    public enum NpcAlly : byte
    {
        Enemy,
        Player,
        Neutral
    }

    public enum Allegiance
    {
        Neutral,
        Friendly,
        Enemy
    }

    [Flags]
    public enum MapFlags : uint
    {
        Snow = 1,
        Rain = 2,
        NoMap = 64,
        Winter = 128,

        CanSummon = 256,
        CanLocate = 512,
        CanTeleport = 1024,
        CanUseSkill = 2048,
        CanUseSpell = 4096,

        ArenaTeam = 8192,
        PlayerKill = 16384,
        SendToHell = 32768,
        ShouldComa = 65536,

        HasDayNight = 131072,

        Default = CanSummon | CanLocate | CanTeleport | CanUseSkill | CanUseSpell | SendToHell | ShouldComa,
        Darkness = Snow | Rain
    }

    public enum Gender : byte
    {
        None,
        Male,
        Female
    }

    public enum Occupation : byte
    {
        None,
        Alchemist,
        Blacksmith,
        Tailor
    }

    public enum Profession : byte
    {
        Peasant,
        Warrior,
        Rogue,
        Wizard,
        Priest,
        Monk
    }

    public enum Specialization : byte
    {
        None,
        Fire,
        Water,
        Wind,
        Earth
    }

    public enum Direction : byte
    {
        North,
        East,
        South,
        West,
        None
    }

    public enum Element : byte
    {
        None,
        Fire,
        Sea,
        Wind,
        Earth,
        Light,
        Dark,
        Wood,
        Metal,
        Undead
    }

    public enum GuildRank : byte
    {
        None,
        Member,
        Council,
        Leader
    }

    public enum LifeStatus : byte
    {
        Alive,
        Dying,
        Dead,
        Coma
    }

    [Flags]
    public enum StatUpdateFlags : byte
    {
        UnreadMail = 0x01,
        Unknown = 0x02,
        Secondary = 0x04,
        Experience = 0x08,
        Current = 0x10,
        Primary = 0x20,
        GameMasterA = 0x40,
        GameMasterB = 0x80,
        Swimming = (GameMasterA | GameMasterB),
        Full = (Primary | Current | Experience | Secondary)
    }

    [Flags]
    public enum WeaponType : byte
    {
        None = 0,
        Basic = 1,
        TwoHanded = 2,
        Dagger = 4,
        Staff = 8,
        Claw = 16
    }

    public enum SkillPane : byte
    {
        Primary,
        Secondary,
        Miscellaneous
    }

    public enum SpellPane : byte
    {
        Primary,
        Secondary,
        Miscellaneous
    }

    public enum SpellCastType : byte
    {
        Passive = 0,
        TextInput = 1,
        Target = 2,
        DigitInputOne = 3,
        DigitInputTwo = 4,
        NoTarget = 5,
        DigitInputThree = 6,
        DigitInputFour = 7
    }

    public enum SpellTargetType : byte
    {
        NoTarget,
        SelfTarget,
        SingleTarget,
        AreaTarget,
        FacingTarget,
        GroupTarget
    }

    public enum SkillTargetType : byte
    {
        NoTarget,
        Facing,
        FirstFacing,
        Surrounding,
        Cone
    }
}