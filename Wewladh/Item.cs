using System;
using System.Collections.Generic;

namespace Wewladh
{
    public abstract class Item : VisibleObject
    {
        public string InvokeMethod { get; set; }
        public string PickupMethod { get; set; }

        public virtual DialogB Invoke(Player sender)
        {
            return null;
        }
        public virtual void OnPickup(Player sender) { }

        public bool DropOnDeath { get; protected set; }
        public bool DropOnLogoff { get; protected set; }
        public bool EquipOnPickup { get; protected set; }

        public long GPValue { get; protected set; }
        public long Value { get; protected set; }
        public bool CanStack { get; protected set; }
        public int MaxStack { get; protected set; }
        public bool IsGold { get; protected set; }
        public BindType BindType { get; set; }
        public bool Soulbound { get; set; }
        public bool TrashItem { get; protected set; }
        public string CooldownFamily { get; protected set; }
        public int CooldownLength { get; protected set; }
        public string QuestName { get; protected set; }
        public int QuestStep { get; protected set; }
        public bool QuestItem
        {
            get { return !string.IsNullOrEmpty(QuestName) && QuestStep > 0; }
        }
        public bool CanUseInPvP { get; protected set; }

        public string RequiredSkillName { get; protected set; }
        public int RequiredSkillLevel { get; protected set; }

        public int RequiredLevel { get; protected set; }
        public int RequiredAbility { get; protected set; }

        public Spawn SpawnControl { get; set; }

        public int Weight { get; protected set; }
        public Gender Gender { get; protected set; }
        public Profession Class { get; set; }
        public int Level { get; set; }

        public int MaximumDurability { get; protected set; }
        public int CurrentDurability { get; set; }

        public string ItemInfoCategory { get; protected set; }
        public string ItemInfoDescription { get; protected set; }

        public List<string> ProtectionOwners { get; set; }
        public DateTime ProtectionExpireTime { get; set; }
        public int Amount { get; set; }
        public int Slot { get; set; }

        public bool LootRoll { get; set; }
        public double LootRollLength { get; protected set; }
        public List<Player> LootRollers { get; private set; }
        public DateTime LootRollEndTime { get; set; }
        public DateTime NextLootRollAnimation { get; set; }

        public int DisplayImage { get; protected set; }
        public double Speed { get; protected set; }
        public WeaponType WeaponType { get; protected set; }
        public int BodyStyle { get; protected set; }
        public bool CanWearWithBoots { get; protected set; }
        public bool CanWearWithArmor { get; protected set; }

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

        public long DynamicMaximumHpMod { get; set; }
        public long DynamicMaximumMpMod { get; set; }
        public long DynamicStrMod { get; set; }
        public long DynamicIntMod { get; set; }
        public long DynamicWisMod { get; set; }
        public long DynamicConMod { get; set; }
        public long DynamicDexMod { get; set; }
        public long DynamicHitMod { get; set; }
        public long DynamicDmgMod { get; set; }
        public long DynamicArmorClassMod { get; set; }
        public long DynamicMagicResistanceMod { get; set; }
        public long DynamicMinimumAttackPowerMod { get; set; }
        public long DynamicMaximumAttackPowerMod { get; set; }
        public long DynamicMinimumMagicPowerMod { get; set; }
        public long DynamicMaximumMagicPowerMod { get; set; }

        public string Description { get; protected set; }

        public string MiscData { get; set; }

        public virtual long SellValue
        {
            get { return Value / 2; }
        }

        public virtual bool CanBeBought(Player p)
        {
            return true;
        }

        public Item()
        {
            this.InvokeMethod = string.Empty;
            this.MiscData = string.Empty;
            this.Name = string.Empty;
            this.ItemInfoCategory = string.Empty;
            this.ItemInfoDescription = string.Empty;
            this.MaxStack = 1;
            this.LootRollers = new List<Player>();
            this.Amount = 1;
            this.ProtectionOwners = new List<string>();
            this.QuestName = string.Empty;
            this.RequiredSkillName = string.Empty;
            this.Level = 1;
        }

        public override void Display()
        {

        }
        public override void DisplayTo(VisibleObject obj)
        {
            if (obj is Player)
            {
                var player = (obj as Player);
                var client = player.Client;

                if (player.Quests.ContainsKey(QuestName))
                {
                    bool returnQuest = true;

                    var quest = player.Quests[QuestName];
                    if (QuestStep == quest.CurrentStep)
                    {
                        var subQuest = quest.QuestStep;
                        if (subQuest.Progress == QuestProgress.InProgress)
                            returnQuest = false;
                    }

                    if (returnQuest)
                        return;
                }

                var p = new ServerPacket(0x07);
                p.WriteUInt16(1);
                p.WriteUInt16((ushort)Point.X);
                p.WriteUInt16((ushort)Point.Y);
                p.WriteUInt32((uint)ID);
                p.WriteUInt16((ushort)(Sprite + 0x8000));
                p.WriteByte(0); // random 1
                p.WriteByte(0); // random 2
                p.WriteByte(0); // random 3

                client.Enqueue(p);
            }
        }
        public void OnTick()
        {

        }
        public override void Update()
        {

        }
        public override bool WithinRange(VisibleObject vo, int range)
        {
            return base.WithinRange(vo, range);
        }
    }

    public sealed class Gold : Item
    {
        public Gold(long value)
        {
            this.Name = "Gold";
            this.Value = value;
            this.Sprite = 136;
        }
        public override void DisplayTo(VisibleObject obj)
        {
            if (obj is Player)
            {
                var player = (obj as Player);
                var client = player.Client;

                var p = new ServerPacket(0x07);
                p.WriteUInt16(1);
                p.WriteUInt16((ushort)Point.X);
                p.WriteUInt16((ushort)Point.Y);
                p.WriteUInt32((uint)ID);
                if (Value < 10)
                {
                    p.WriteUInt16((ushort)(139 + 0x8000));
                }
                else if (Value < 100)
                {
                    p.WriteUInt16((ushort)(142 + 0x8000));
                }
                else if (Value < 1000)
                {
                    p.WriteUInt16((ushort)(138 + 0x8000));
                }
                else if (Value < 10000)
                {
                    p.WriteUInt16((ushort)(141 + 0x8000));
                }
                else if (Value < 100000)
                {
                    p.WriteUInt16((ushort)(137 + 0x8000));
                }
                else if (Value < 1000000)
                {
                    p.WriteUInt16((ushort)(140 + 0x8000));
                }
                else
                {
                    p.WriteUInt16((ushort)(136 + 0x8000));
                }
                p.WriteByte(0); // random 1
                p.WriteByte(0); // random 2
                p.WriteByte(0); // random 3

                client.Enqueue(p);
            }
        }
    }

    public abstract class Patch : Item
    {
        public override DialogB Invoke(Player sender)
        {
            if (sender.Armor == null)
            {
                sender.Client.SendMessage("You have no armor equipped.");
                return null;
            }
            else
            {
                var dialog = new ItemPatchDialog();
                dialog.Message = string.Format("You are about to patch your {0} with {1}. This will replace any previous patches on {0}.",
                    sender.Armor.Name, Name);
                return dialog;
            }
        }
    }

    public abstract class Equipment : Item
    {
        public int EquipmentSlot { get; set; }

        public override long SellValue
        {
            get
            {
                return (long)(((double)Value / 2d) * ((double)CurrentDurability / (double)MaximumDurability));
            }
        }

        public Equipment()
        {
            this.CanStack = false;
        }

        public override DialogB Invoke(Player p)
        {
            if (this.CurrentDurability < 1)
            {
                p.Client.SendMessage("This item is too worn out");
                return null;
            }

            if ((this.Gender != Gender.None) && (this.Gender != p.Sex))
            {
                p.Client.SendMessage("This does not fit you.");
                return null;
            }

            if (this.Class != Profession.Peasant && this.Class != p.Class && !p.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
            {
                p.Client.SendMessage("Your class cannot equip this item.");
                return null;
            }

            if (this.RequiredLevel > p.Level && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
            {
                p.Client.SendMessage(string.Format("You must be level {0} to equip this item.", RequiredLevel));
                return null;
            }

            if (this.RequiredAbility > p.Ability && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
            {
                p.Client.SendMessage(string.Format("You must be ability {0} to equip this item.", RequiredAbility));
                return null;
            }

            if (BindType == BindType.BindOnEquip && !Soulbound)
            {
                var dialog = new BindOnEquipDialog();
                dialog.Message = string.Format("Equipping {0} will bind it to you. Do you wish to continue?", Name);
                return dialog;
            }

            p.RemoveItem(this);
            this.Amount = 1;

            int index = (EquipmentSlot - 1);
            if ((this is Ring) && (p.LeftRing != null) && (p.RightRing == null))
                index = 7;
            else if ((this is Gauntlet) && (p.LeftGauntlet != null) && (p.RightGauntlet == null))
                index = 9;
            else if ((this is Accessory) && (p.AccessoryA != null) && (p.AccessoryB == null))
                index = 21;

            if (p.Equipment[index] != null)
                p.AddItem(p.RemoveEquipment(index));

            p.AddEquipment(this, index);

            p.Display();

            return null;
        }
        public virtual void OnEquip(Player p)
        {
            p.MaximumHpMod += MaximumHpMod + DynamicMaximumHpMod;
            p.MaximumMpMod += MaximumMpMod + DynamicMaximumMpMod;
            p.StrMod += StrMod+DynamicStrMod;
            p.IntMod += IntMod + DynamicIntMod;
            p.WisMod += WisMod + DynamicWisMod;
            p.ConMod += ConMod + DynamicConMod;
            p.DexMod += DexMod + DynamicDexMod;
            p.HitMod += HitMod + DynamicHitMod;
            p.DmgMod += DmgMod + DynamicDmgMod;
            p.ArmorClassMod += ArmorClassMod + DynamicArmorClassMod;
            p.MagicResistanceMod += MagicResistanceMod + DynamicMagicResistanceMod;
            p.CurrentWeight += Weight;
        }
        public virtual void OnUnequip(Player p)
        {
            p.MaximumHpMod -= MaximumHpMod + DynamicMaximumHpMod;
            p.MaximumMpMod -= MaximumMpMod + DynamicMaximumMpMod;
            p.StrMod -= StrMod + DynamicStrMod;
            p.IntMod -= IntMod + DynamicIntMod;
            p.WisMod -= WisMod + DynamicWisMod;
            p.ConMod -= ConMod + DynamicConMod;
            p.DexMod -= DexMod + DynamicDexMod;
            p.HitMod -= HitMod + DynamicHitMod;
            p.DmgMod -= DmgMod + DynamicDmgMod;
            p.ArmorClassMod -= ArmorClassMod + DynamicArmorClassMod;
            p.MagicResistanceMod -= MagicResistanceMod + DynamicMagicResistanceMod;
            p.CurrentWeight -= Weight;
        }
    }

    public abstract class Weapon : Equipment
    {
        public Weapon()
        {
            this.EquipmentSlot = 1;
            this.WeaponType = WeaponType.Basic;
        }

        public override DialogB Invoke(Player p)
        {
            if (((WeaponType & WeaponType.TwoHanded) == WeaponType.TwoHanded) && (p.Shield != null))
            {
                p.Client.SendMessage("You cannot equip a two-handed weapon with a shield.");
                return null;
            }
            else if (((WeaponType & WeaponType.Staff) == WeaponType.Staff) && (p.Shield != null))
            {
                p.Client.SendMessage("You cannot equip a two-handed weapon with a shield.");
                return null;
            }
            else
            {
                return base.Invoke(p);
            }
        }
        public override void OnEquip(Player p)
        {
            base.OnEquip(p);
            p.MinimumAttackPowerMod += MinimumAttackPowerMod + DynamicMinimumAttackPowerMod;
            p.MaximumAttackPowerMod += MaximumAttackPowerMod + DynamicMaximumAttackPowerMod;
            p.MinimumMagicPowerMod += MinimumMagicPowerMod + DynamicMinimumMagicPowerMod;
            p.MaximumMagicPowerMod += MaximumMagicPowerMod + DynamicMaximumMagicPowerMod;
        }
        public override void OnUnequip(Player p)
        {
            base.OnUnequip(p);
            p.MinimumAttackPowerMod -= MinimumAttackPowerMod + DynamicMinimumAttackPowerMod;
            p.MaximumAttackPowerMod -= MaximumAttackPowerMod + DynamicMaximumAttackPowerMod;
            p.MinimumMagicPowerMod -= MinimumMagicPowerMod + DynamicMinimumMagicPowerMod;
            p.MaximumMagicPowerMod -= MaximumMagicPowerMod + DynamicMaximumMagicPowerMod;
        }
    }
    public abstract class DisplayWeapon : Equipment
    {
        public DisplayWeapon()
        {
            this.EquipmentSlot = 24;
        }
    }
    public abstract class Armor : Equipment
    {
        public Armor()
        {
            this.EquipmentSlot = 2;
            this.CanWearWithBoots = true;
        }

        public override DialogB Invoke(Player p)
        {
            if ((p.Boots != null) && !CanWearWithBoots)
            {
                p.Client.SendMessage("You cannot wear this armor with boots.");
                return null;
            }
            else if ((p.Boots != null) && !p.Boots.CanWearWithArmor)
            {
                p.Client.SendMessage("You cannot wear armor with the boots you are wearing.");
                return null;
            }
            else
            {
                return base.Invoke(p);
            }
        }
    }
    public abstract class Shield : Equipment
    {
        public Shield()
        {
            this.EquipmentSlot = 3;
        }

        public override DialogB Invoke(Player p)
        {
            if ((p.Weapon != null) && ((p.Weapon.WeaponType & WeaponType.TwoHanded) == WeaponType.TwoHanded))
            {
                p.Client.SendMessage("You cannot equip a shield with a two-handed weapon.");
                return null;
            }
            else if ((p.Weapon != null) && ((p.Weapon.WeaponType & WeaponType.Staff) == WeaponType.Staff))
            {
                p.Client.SendMessage("You cannot equip a shield with a two-handed weapon.");
                return null;
            }
            else
            {
                return base.Invoke(p);
            }
        }
    }
    public abstract class Helmet : Equipment
    {
        public Helmet()
        {
            this.EquipmentSlot = 4;
        }
    }
    public abstract class EventHelm : Equipment
    {
        public EventHelm()
        {
            this.EquipmentSlot = 16;
        }
    }
    public abstract class Earring : Equipment
    {
        public Earring()
        {
            this.EquipmentSlot = 5;
        }
    }
    public abstract class Necklace : Equipment
    {
        public Element Element { get; protected set; }
        
        public Necklace()
        {
            this.EquipmentSlot = 6;
        }

        public override void OnEquip(Player p)
        {
            base.OnEquip(p);
            p.OffenseElement = Element;
        }
        public override void OnUnequip(Player p)
        {
            base.OnUnequip(p);
            p.OffenseElement = Element.None;
        }
    }
    public abstract class Ring : Equipment
    {
        public Ring()
        {
            this.EquipmentSlot = 7;
        }
    }
    public abstract class Gauntlet : Equipment
    {
        public Gauntlet()
        {
            this.EquipmentSlot = 9;
        }
    }
    public abstract class Belt : Equipment
    {
        public Element Element { get; protected set; }

        public Belt()
        {
            this.EquipmentSlot = 11;
        }

        public override void OnEquip(Player p)
        {
            base.OnEquip(p);
            p.DefenseElement = Element;
        }
        public override void OnUnequip(Player p)
        {
            base.OnUnequip(p);
            p.DefenseElement = Element.None;
        }
    }
    public abstract class Greaves : Equipment
    {
        public Greaves()
        {
            this.EquipmentSlot = 12;
        }
    }
    public abstract class Boots : Equipment
    {
        public Boots()
        {
            this.EquipmentSlot = 13;
            this.CanWearWithArmor = true;
        }

        public override DialogB Invoke(Player p)
        {
            if ((p.Armor != null) && !CanWearWithArmor)
            {
                p.Client.SendMessage("You cannot wear these boots with armor.");
                return null;
            }
            else if ((p.Armor != null) && !p.Armor.CanWearWithBoots)
            {
                p.Client.SendMessage("You cannot wear boots with the armor you are wearing.");
                return null;
            }
            else
            {
                return base.Invoke(p);
            }
        }
    }
    public abstract class Accessory : Equipment
    {
        public Accessory()
        {
            this.EquipmentSlot = 14;
        }
    }
    public abstract class DisplayBoots : Equipment
    {
        public DisplayBoots()
        {
            this.EquipmentSlot = 25;
        }
    }
    public abstract class BackAccessory : Equipment
    {
        public BackAccessory()
        {
            this.EquipmentSlot = 26;
        }
    }
    public abstract class Cape : Equipment
    {
        public Cape()
        {
            this.EquipmentSlot = 27;
        }
    }
    public abstract class Effect : Equipment
    {
        public Effect()
        {
            this.EquipmentSlot = 28;
        }
    }
    public abstract class Overcoat : Equipment
    {
        public Overcoat()
        {
            this.EquipmentSlot = 15;
        }
    }
    public abstract class DisplayHelm : Equipment
    {
        public DisplayHelm()
        {
            this.EquipmentSlot = 23;
        }
    }

    public class ItemTemplate
    {

        public ItemTemplate()
        {

        }
    }
}