using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public abstract class Dialog
    {
        #region Constants
        public const ushort DIALOG_BUY_01 = 0x0001;
        public const ushort DIALOG_BUY_02 = 0x0002;
        public const ushort DIALOG_SELL_01 = 0x0003;
        public const ushort DIALOG_SELL_02 = 0x0004;
        public const ushort DIALOG_SELL_03 = 0x0005;
        public const ushort DIALOG_SELL_04 = 0x0006;
        public const ushort DIALOG_DEPOSIT_01 = 0x0007;
        public const ushort DIALOG_DEPOSIT_02 = 0x0008;
        public const ushort DIALOG_WITHDRAW_01 = 0x0009;
        public const ushort DIALOG_WITHDRAW_02 = 0x000A;
        public const ushort DIALOG_LEARNSKILL_01 = 0x000B;
        public const ushort DIALOG_LEARNSKILL_02 = 0x000C;
        public const ushort DIALOG_LEARNSKILL_03 = 0x000D;
        public const ushort DIALOG_LEARNSPELL_01 = 0x000E;
        public const ushort DIALOG_LEARNSPELL_02 = 0x000F;
        public const ushort DIALOG_LEARNSPELL_03 = 0x0010;
        public const ushort DIALOG_FORGETSKILL_01 = 0x0011;
        public const ushort DIALOG_FORGETSKILL_02 = 0x0012;
        public const ushort DIALOG_FORGETSPELL_01 = 0x0013;
        public const ushort DIALOG_FORGETSPELL_02 = 0x0014;
        public const ushort DIALOG_UPGRADESKILL_01 = 0x0015;
        public const ushort DIALOG_UPGRADESKILL_02 = 0x0016;
        public const ushort DIALOG_UPGRADESKILL_03 = 0x0017;
        public const ushort DIALOG_UPGRADESPELL_01 = 0x0018;
        public const ushort DIALOG_UPGRADESPELL_02 = 0x0019;
        public const ushort DIALOG_UPGRADESPELL_03 = 0x001A;
        public const ushort DIALOG_GLOBAL_MAX = 0x0100;
        #endregion

        public GameObject GameObject { get; set; }
        public string Message { get; set; }
        public string CustomName { get; set; }
        public ushort CustomImage { get; set; }

        public string Name
        {
            get { return CustomName ?? GameObject.Name; }
            set { CustomName = value; }
        }
        public ushort Image
        {
            get
            {
                if (CustomImage != ushort.MinValue)
                    return CustomImage;
                else if (GameObject is Item)
                    return (ushort)((GameObject as VisibleObject).Sprite + 0x8000);
                else if (GameObject is VisibleObject)
                    return (ushort)((GameObject as VisibleObject).Sprite + 0x4000);
                else
                    return 0;
            }
            set { CustomImage = value; }
        }

        public abstract ServerPacket ToPacket();
        public static ServerPacket ExitPacket()
        {
            var p = new ServerPacket(0x30);
            p.WriteByte(0x0A);
            p.WriteByte(0x00);
            return p;
        }
    }

    public abstract class DialogB : Dialog
    {
        public bool CanClose { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoNext { get; set; }
        public abstract DialogB Back(Player p, ClientPacket msg);
        public abstract DialogB Next(Player p, ClientPacket msg);
        public abstract DialogB Exit(Player p, ClientPacket msg);
        public DialogB()
        {
            CanClose = true;
        }
    }

    public class EndDialog : NormalDialog
    {
        public EndDialog(string message)
        {
            this.CanGoBack = false;
            this.CanGoNext = false;
            this.Message = message;
        }
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
    }

    public class DialogMenu : Dialog
    {
        public SortedDictionary<ushort, DialogMenuOption> Options { get; private set; }

        public DialogMenu(string message)
        {
            this.Message = message;
            this.Options = new SortedDictionary<ushort, DialogMenuOption>();
        }
        public DialogMenu(GameObject go, string message)
        {
            this.GameObject = go;
            this.Message = message;
            this.Options = new SortedDictionary<ushort, DialogMenuOption>();
        }

        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x00); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(Message);
            p.WriteByte((byte)Options.Count);
            foreach (var kvp in Options)
            {
                p.WriteString8(kvp.Value.Title);
                p.WriteUInt16(kvp.Key);
            }
            return p;
        }
    }

    #region Buy
    public class Buy_1 : DialogMenuOption
    {
        public Buy_1()
        {
            this.Title = "Buy";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new BuyMenu(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class Buy_2 : DialogMenuOption
    {
        public Buy_2()
        {
            this.Title = "Buy";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I do not have that item.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var i in npc.Inventory)
            {
                if (i.Name == name && i.CanBeBought(p))
                {
                    int index = p.FindEmptyInventoryIndex();

                    if (i.Value > p.Gold)
                    {
                        dialog.Message = "You do not have enough gold.";
                        return dialog;
                    }

                    if (i.GPValue > p.GamePoints)
                    {
                        dialog.Message = "You do not have enough GP.";
                        return dialog;
                    }

                    if (i.Weight > p.AvailableWeight)
                    {
                        dialog.Message = "You cannot hold this item.";
                        return dialog;
                    }

                    if (index < 0)
                    {
                        dialog.Message = "You cannot hold anymore items.";
                        return dialog;
                    }

                    var item = npc.GameServer.CreateItem(i.GetType().Name);

                    p.AddItem(item, index);
                    p.Gold -= item.Value;
                    p.GamePoints -= item.GPValue;
                    p.Client.SendStatistics(StatUpdateFlags.Experience);
                    return new BuyMenu(p);
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class BuyMenu : Dialog
    {
        private Player player;
        public BuyMenu(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var npc = (GameObject as Merchant);
            var items = npc.Inventory.Where(i => i.CanBeBought(player));

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x04); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to buy?");
            p.WriteUInt16(DIALOG_BUY_02); // dialog id
            p.WriteUInt16((ushort)items.Count());
            foreach (Item i in items)
            {
                p.WriteUInt16((ushort)(0x8000 + i.Sprite));
                p.WriteUInt16((ushort)i.Color);
                p.WriteUInt32((uint)i.Value);
                p.WriteString8(i.Name);
                p.WriteString8(i.GetType().Name);
                p.WriteString8(i.GetType().Name);
                p.WriteString8(string.Empty); // old description
            }
            return p;
        }
    }
    #endregion

    #region Sell
    public class Sell_1 : DialogMenuOption
    {
        public Sell_1()
        {
            this.Title = "Sell";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var list = new List<int>();
            foreach (Item i in p.Inventory)
            {
                if ((i != null) && (npc.SellItems.Contains(i.GetType()) || i.TrashItem))
                    list.Add(i.Slot);
            }
            return new SellMenu(list);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class Sell_2 : DialogMenuOption
    {
        public Sell_2()
        {
            this.Title = "Sell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            int slot = msg.ReadByte();
            if ((slot > 0) && (slot <= p.Inventory.Length))
            {
                var item = p.Inventory[slot - 1];
                if (item != null && (npc.SellItems.Contains(item.GetType()) || item.TrashItem))
                {
                    if (item.Amount == 1)
                        return new SellConfirmMenu(item, 1);
                    else
                        return new SellHowManyMenu(item);
                }
            }
            return new DialogMenu("I don't want that.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class Sell_3 : DialogMenuOption
    {
        public Sell_3()
        {
            this.Title = "Sell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            msg.ReadByte(); // ??

            int slot = msg.ReadByte();
            var amount = int.Parse(msg.ReadString(msg.ReadByte()));

            if ((slot > 0) && (slot <= p.Inventory.Length))
            {
                var item = p.Inventory[slot - 1];
                if (item != null && (npc.SellItems.Contains(item.GetType()) || item.TrashItem))
                {
                    if (item.Amount < amount)
                    {
                        return new DialogMenu("You don't have that many.");
                    }
                    else
                    {
                        return new SellConfirmMenu(item, amount);
                    }
                }
            }

            return new DialogMenu("I don't want that.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class Sell_4 : DialogMenuOption
    {
        public Sell_4()
        {
            this.Title = "Sell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            msg.ReadByte(); // ??
            int slot = msg.ReadByte();
            int amount = msg.ReadByte();

            if ((slot > 0) && (slot <= p.Inventory.Length))
            {
                var item = p.Inventory[slot - 1];
                if (item != null && (npc.SellItems.Contains(item.GetType()) || item.TrashItem))
                {
                    if (item.Amount < amount)
                    {
                        return new DialogMenu("You don't have that many.");
                    }
                    else
                    {
                        p.Gold += (item.SellValue * amount);
                        p.RemoveItem(item, amount);
                        p.Client.SendStatistics(StatUpdateFlags.Experience);

                        var list = new List<int>();
                        foreach (Item i in p.Inventory)
                        {
                            if ((i != null) && (npc.SellItems.Contains(i.GetType()) || i.TrashItem))
                                list.Add(i.Slot);
                        }
                        return new SellMenu(list);
                    }
                }
            }

            return new DialogMenu("I don't want that.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class SellMenu : Dialog
    {
        private List<int> list = new List<int>();
        public SellMenu(IEnumerable<int> items)
        {
            this.list.AddRange(items);
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x05); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What are you selling today?");
            p.WriteUInt16(DIALOG_SELL_02); // dialog id
            p.WriteByte((byte)list.Count);
            list.ForEach(i => p.WriteByte((byte)i));
            return p;
        }
    }
    public class SellHowManyMenu : Dialog
    {
        private Item item = null;
        public SellHowManyMenu(Item item)
        {
            this.item = item;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x03); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("How many would you like to sell?");
            p.WriteByte(0x01);
            p.WriteByte((byte)item.Slot);
            p.WriteUInt16(DIALOG_SELL_03);
            return p;
        }
    }
    public class SellConfirmMenu : Dialog
    {
        private Item item = null;
        private int amount = 0;
        public SellConfirmMenu(Item item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x01); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(string.Format("I'll give you {0} gold for it.  Is that fair enough?", item.SellValue * amount));
            p.WriteByte(0x02);
            p.WriteByte((byte)item.Slot);
            p.WriteByte((byte)amount);
            p.WriteByte(0x02);
            p.WriteString8("Yes");
            p.WriteUInt16(DIALOG_SELL_04);
            p.WriteString8("No");
            p.WriteUInt16(DIALOG_SELL_01);
            return p;
        }
    }
    #endregion

    #region Deposit
    public class Deposit_1 : DialogMenuOption
    {
        public Deposit_1()
        {
            this.Title = "Deposit";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var list = new List<int>();
            foreach (Item i in p.Inventory)
            {
                if (i != null && !i.Soulbound && i.CurrentDurability == i.MaximumDurability)
                    list.Add(i.Slot);
            }
            return new DepositMenu(list);
        }
        public override bool CanOpen(Player p)
        {
            return (p.AccountID != 0);
        }
    }
    public class Deposit_2 : DialogMenuOption
    {
        public Deposit_2()
        {
            this.Title = "Deposit";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            int slot = msg.ReadByte();
            if ((slot > 0) && (slot <= p.Inventory.Length))
            {
                var item = p.Inventory[slot - 1];
                if (item != null && !item.Soulbound && item.CurrentDurability == item.MaximumDurability)
                {
                    var newItem = p.RemoveItem(item);
                    int index = p.BankItems.FindIndex(i => i != null && i.GetType() == newItem.GetType());
                    if (index < 0)
                        p.BankItems.Add(newItem);
                    else
                        p.BankItems[index].Amount += newItem.Amount;

                    var list = new List<int>();
                    foreach (Item i in p.Inventory)
                    {
                        if (i != null && !i.Soulbound)
                            list.Add(i.Slot);
                    }
                    return new DepositMenu(list);
                }
            }
            return new DialogMenu("I can't take that.");
        }
        public override bool CanOpen(Player p)
        {
            return (p.AccountID != 0);
        }
    }
    public class DepositMenu : Dialog
    {
        private List<int> list = new List<int>();
        public DepositMenu(IEnumerable<int> items)
        {
            this.list.AddRange(items);
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x05); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What are you depositing today?");
            p.WriteUInt16(DIALOG_DEPOSIT_02); // dialog id
            p.WriteByte((byte)list.Count);
            list.ForEach(i => p.WriteByte((byte)i));
            return p;
        }
    }
    #endregion

    #region Withdraw
    public class Withdraw_1 : DialogMenuOption
    {
        public Withdraw_1()
        {
            this.Title = "Withdraw";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new WithdrawMenu(p);
        }
        public override bool CanOpen(Player p)
        {
            return (p.AccountID != 0);
        }
    }
    public class Withdraw_2 : DialogMenuOption
    {
        public Withdraw_2()
        {
            this.Title = "Withdraw";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I do not have that item.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var i in p.BankItems)
            {
                if (i.Name == name)
                {
                    int index = p.FindEmptyInventoryIndex();

                    if (i.Weight > p.AvailableWeight)
                    {
                        dialog.Message = "You cannot hold this item.";
                        return dialog;
                    }

                    if (index < 0)
                    {
                        dialog.Message = "You cannot hold anymore items.";
                        return dialog;
                    }

                    var item = npc.GameServer.CreateItem(i.GetType().Name);

                    if (--i.Amount < 1)
                        p.BankItems.Remove(i);

                    p.AddItem(item, index);
                    return new WithdrawMenu(p);
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return (p.AccountID != 0);
        }
    }
    public class WithdrawMenu : Dialog
    {
        private Player player;
        public WithdrawMenu(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x04); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to withdraw?");
            p.WriteUInt16(DIALOG_WITHDRAW_02); // dialog id
            p.WriteUInt16((ushort)player.BankItems.Count());
            foreach (Item i in player.BankItems)
            {
                p.WriteUInt16((ushort)(0x8000 + i.Sprite));
                p.WriteUInt16((ushort)i.Color);
                p.WriteUInt32((uint)i.Amount);
                p.WriteString8(i.Name);
                p.WriteString8(i.Name.GetType().Name);
                p.WriteString8(i.Name.GetType().Name);
                p.WriteString8(string.Empty); // old description
            }
            return p;
        }
    }
    #endregion

    #region Learn Skill
    public class LearnSkill_1 : DialogMenuOption
    {
        public LearnSkill_1()
        {
            this.Title = "Learn Skill";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new LearnSkillDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSkill_2 : DialogMenuOption
    {
        public LearnSkill_2()
        {
            this.Title = "Learn Skill";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var name = msg.ReadString(msg.ReadByte());
            foreach (var skill in npc.LearnedSkills)
            {
                if (name == skill.Name)
                {
                    return new LearnSkillDialog_2(skill);
                }
            }
            return new DialogMenu("I cannot teach you that skill.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSkill_3 : DialogMenuOption
    {
        public LearnSkill_3()
        {
            this.Title = "Learn Skill";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I cannot teach you that skill.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var s in npc.LearnedSkills)
            {
                if (s.Name == name)
                {
                    int index = p.FindEmptySkillIndex(s.Pane);
                    var skill = npc.GameServer.CreateSkill(s.GetType().Name);

                    if (p.SkillBook.Contains(s.GetType().Name))
                    {
                        dialog.Message = "You already have that skill.";
                        return dialog;
                    }

                    if (p.Class != skill.RequiredClass && skill.RequiredClass != Profession.Peasant && !p.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
                    {
                        dialog.Message = "You cannot learn this skill.";
                        return dialog;
                    }

                    if ((p.Specialization != skill.RequiredSpecialization) && (skill.RequiredSpecialization != Specialization.None))
                    {
                        dialog.Message = "You cannot learn this skill.";
                        return dialog;
                    }

                    if (p.Level < skill.Ranks[0].RequiredLevel && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (!p.Master && skill.Ranks[0].RequiresMaster && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (p.Ability < skill.Ranks[0].RequiredAbility && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your ability is too low.";
                        return dialog;
                    }

                    if (p.BaseStr < skill.RequiredStr)
                    {
                        dialog.Message = "Your strength is too low.";
                        return dialog;
                    }

                    if (p.BaseInt < skill.RequiredInt)
                    {
                        dialog.Message = "Your intelligence is too low.";
                        return dialog;
                    }

                    if (p.BaseWis < skill.RequiredWis)
                    {
                        dialog.Message = "Your wisdom is too low.";
                        return dialog;
                    }

                    if (p.BaseCon < skill.RequiredCon)
                    {
                        dialog.Message = "Your constitution is too low.";
                        return dialog;
                    }

                    if (p.BaseDex < skill.RequiredDex)
                    {
                        dialog.Message = "Your dexterity is too low.";
                        return dialog;
                    }

                    foreach (var item in s.RequiredItems)
                    {
                        if (p.Inventory.Count(item.Key) < item.Value)
                        {
                            dialog.Message = "You have not brought me the items I requested.";
                            return dialog;
                        }
                    }

                    if (p.Gold < s.RequiredGold)
                    {
                        dialog.Message = "You do not have enough gold.";
                        return dialog;
                    }

                    if (index < 0)
                    {
                        dialog.Message = "You cannot learn anymore skills.";
                        return dialog;
                    }

                    p.Gold -= s.RequiredGold;
                    p.Client.SendStatistics(StatUpdateFlags.Experience);

                    foreach (var item in s.RequiredItems)
                    {
                        int itemIndex = p.Inventory.IndexOf(item.Key);
                        p.RemoveItem(itemIndex, item.Value);
                    }

                    p.AddSkill(skill, index);
                    p.SpellAnimation(22, 50);
                    return new DialogMenu("Use this skill well.");
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSkillDialog_1 : Dialog
    {
        private Player player = null;
        public LearnSkillDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var npc = (GameObject as Merchant);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to learn?");
            p.WriteUInt16(DIALOG_LEARNSKILL_02); // dialog id

            var skills = new List<Skill>();
            foreach (var s in npc.LearnedSkills)
            {
                if (player.SkillBook.Contains(s.GetType().Name))
                    continue;
                if (s.RequiredClass != Profession.Peasant && s.RequiredClass != player.Class && !player.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
                    continue;
                if (s.RequiredSpecialization != Specialization.None && s.RequiredSpecialization != player.Specialization)
                    continue;
                skills.Add(s);
            }
            p.WriteUInt16((ushort)skills.Count);
            foreach (var s in skills)
            {
                p.WriteByte(0x03);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    public class LearnSkillDialog_2 : Dialog
    {
        private Skill skill = null;
        public LearnSkillDialog_2(Skill skill)
        {
            this.skill = skill;
        }
        public override ServerPacket ToPacket()
        {
            var stringBuilder = new StringBuilder();
            foreach (var req in skill.RequiredItems)
            {
                stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
            }
            stringBuilder.AppendFormat("{0} gold", skill.RequiredGold);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x01); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(skill.DialogDescription + "  I require: " + stringBuilder);
            p.WriteString8(skill.Name);
            p.WriteByte(0x02);
            p.WriteString8("Learn skill");
            p.WriteUInt16(DIALOG_LEARNSKILL_03);
            p.WriteString8("Go back");
            p.WriteUInt16(DIALOG_LEARNSKILL_01);
            return p;
        }
    }
    #endregion

    #region Learn Spell
    public class LearnSpell_1 : DialogMenuOption
    {
        public LearnSpell_1()
        {
            this.Title = "Learn Spell";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new LearnSpellDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSpell_2 : DialogMenuOption
    {
        public LearnSpell_2()
        {
            this.Title = "Learn Spell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var name = msg.ReadString(msg.ReadByte());
            foreach (var spell in npc.LearnedSpells)
            {
                if (name == spell.Name)
                {
                    return new LearnSpellDialog_2(spell);
                }
            }
            return new DialogMenu("I cannot teach you that spell.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSpell_3 : DialogMenuOption
    {
        public LearnSpell_3()
        {
            this.Title = "Learn Spell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I cannot teach you that spell.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var s in npc.LearnedSpells)
            {
                if (s.Name == name)
                {
                    int index = p.FindEmptySpellIndex(s.Pane);
                    var spell = npc.GameServer.CreateSpell(s.GetType().Name);

                    if (p.SpellBook.Contains(s.GetType().Name))
                    {
                        dialog.Message = "You already have that spell.";
                        return dialog;
                    }

                    if (p.Class != spell.RequiredClass && spell.RequiredClass != Profession.Peasant && !p.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
                    {
                        dialog.Message = "You cannot learn this spell.";
                        return dialog;
                    }

                    if (p.Specialization != spell.RequiredSpecialization && spell.RequiredSpecialization != Specialization.None)
                    {
                        dialog.Message = "You cannot learn this spell.";
                        return dialog;
                    }

                    if (p.Level < spell.Ranks[0].RequiredLevel && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (!p.Master && spell.Ranks[0].RequiresMaster && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (p.Ability < spell.Ranks[0].RequiredAbility && !p.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        dialog.Message = "Your ability is too low.";
                        return dialog;
                    }

                    if (p.BaseStr < spell.RequiredStr)
                    {
                        dialog.Message = "Your strength is too low.";
                        return dialog;
                    }

                    if (p.BaseInt < spell.RequiredInt)
                    {
                        dialog.Message = "Your intelligence is too low.";
                        return dialog;
                    }

                    if (p.BaseWis < spell.RequiredWis)
                    {
                        dialog.Message = "Your wisdom is too low.";
                        return dialog;
                    }

                    if (p.BaseCon < spell.RequiredCon)
                    {
                        dialog.Message = "Your constitution is too low.";
                        return dialog;
                    }

                    if (p.BaseDex < spell.RequiredDex)
                    {
                        dialog.Message = "Your dexterity is too low.";
                        return dialog;
                    }

                    foreach (var item in s.RequiredItems)
                    {
                        if (p.Inventory.Count(item.Key) < item.Value)
                        {
                            dialog.Message = "You have not brought me the items I requested.";
                            return dialog;
                        }
                    }

                    if (p.Gold < s.RequiredGold)
                    {
                        dialog.Message = "You do not have enough gold.";
                        return dialog;
                    }

                    if (index < 0)
                    {
                        dialog.Message = "You cannot learn anymore spells.";
                        return dialog;
                    }

                    p.Gold -= s.RequiredGold;
                    p.Client.SendStatistics(StatUpdateFlags.Experience);

                    foreach (var item in s.RequiredItems)
                    {
                        int itemIndex = p.Inventory.IndexOf(item.Key);
                        p.RemoveItem(itemIndex, item.Value);
                    }

                    p.AddSpell(spell, index);
                    p.SpellAnimation(22, 50);
                    return new DialogMenu("Use this spell well.");
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class LearnSpellDialog_1 : Dialog
    {
        private Player player = null;
        public LearnSpellDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var npc = (GameObject as Merchant);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to learn?");
            p.WriteUInt16(DIALOG_LEARNSPELL_02); // dialog id

            var spells = new List<Spell>();
            foreach (var s in npc.LearnedSpells)
            {
                if (player.SpellBook.Contains(s.GetType().Name))
                    continue;
                if (s.RequiredClass != Profession.Peasant && s.RequiredClass != player.Class && !player.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
                    continue;
                if (s.RequiredSpecialization != Specialization.None && s.RequiredSpecialization != player.Specialization)
                    continue;
                spells.Add(s);
            }
            p.WriteUInt16((ushort)spells.Count);
            foreach (var s in spells)
            {
                p.WriteByte(0x02);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    public class LearnSpellDialog_2 : Dialog
    {
        private Spell spell = null;
        public LearnSpellDialog_2(Spell spell)
        {
            this.spell = spell;
        }
        public override ServerPacket ToPacket()
        {
            var stringBuilder = new StringBuilder();
            foreach (var req in spell.RequiredItems)
            {
                stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
            }
            stringBuilder.AppendFormat("{0} gold", spell.RequiredGold);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x01); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(GameObject.Name);
            p.WriteString16(spell.DialogDescription + "  I require: " + stringBuilder);
            p.WriteString8(spell.Name);
            p.WriteByte(0x02);
            p.WriteString8("Learn spell");
            p.WriteUInt16(DIALOG_LEARNSPELL_03);
            p.WriteString8("Go back");
            p.WriteUInt16(DIALOG_LEARNSPELL_01);
            return p;
        }
    }
    #endregion

    #region Forget Skill
    public class ForgetSkill_1 : DialogMenuOption
    {
        public ForgetSkill_1()
        {
            this.Title = "Forget Skill";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new ForgetSkillDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class ForgetSkill_2 : DialogMenuOption
    {
        public ForgetSkill_2()
        {
            this.Title = "Learn Skill";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var name = msg.ReadString(msg.ReadByte());

            var skills = new Skill[p.SkillBook.Length];
            p.SkillBook.CopyTo(skills, 0);

            foreach (var skill in skills)
            {
                if (skill != null && name == skill.Name)
                {
                    p.RemoveSkill(skill);
                    return new DialogMenu("n just liek that it forgotten hoorya!!.");
                }
            }

            return new DialogMenu("UMMM YOU DONT HAVE THAT SKILL>?.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class ForgetSkillDialog_1 : Dialog
    {
        private Player player = null;
        public ForgetSkillDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(GameObject.Name);
            p.WriteString16("My penis extends when elephant?");
            p.WriteUInt16(DIALOG_FORGETSKILL_02); // dialog id

            var skills = new List<Skill>();
            foreach (var s in player.SkillBook)
            {
                if (s != null)
                    skills.Add(s);
            }

            p.WriteUInt16((ushort)skills.Count);
            foreach (var s in skills)
            {
                p.WriteByte(0x03);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    #endregion

    #region Forget Spell
    public class ForgetSpell_1 : DialogMenuOption
    {
        public ForgetSpell_1()
        {
            this.Title = "Forget Spell";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new ForgetSpellDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class ForgetSpell_2 : DialogMenuOption
    {
        public ForgetSpell_2()
        {
            this.Title = "Forget Spell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var name = msg.ReadString(msg.ReadByte());

            var spells = new Spell[p.SpellBook.Length];
            p.SpellBook.CopyTo(spells, 0);

            foreach (var spell in spells)
            {
                if (spell != null && name == spell.Name)
                {
                    p.RemoveSpell(spell);
                    return new DialogMenu("n just liek that it forgotten hoorya!!.");
                }
            }

            return new DialogMenu("UMMM YOU DONT HAVE THAT SpeLL>?.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class ForgetSpellDialog_1 : Dialog
    {
        private Player player = null;
        public ForgetSpellDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(GameObject.Name);
            p.WriteString16("what did the camel?");
            p.WriteUInt16(DIALOG_FORGETSPELL_02); // dialog id

            var spells = new List<Spell>();
            foreach (var s in player.SpellBook)
            {
                if (s != null)
                    spells.Add(s);
            }
            p.WriteUInt16((ushort)spells.Count);
            foreach (var s in spells)
            {
                p.WriteByte(0x02);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    #endregion

    #region Upgrade Skill
    public class UpgradeSkill_1 : DialogMenuOption
    {
        public UpgradeSkill_1()
        {
            this.Title = "Upgrade Skill";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new UpgradeSkillDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSkill_2 : DialogMenuOption
    {
        public UpgradeSkill_2()
        {
            this.Title = "Upgrade Skill";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var name = msg.ReadString(msg.ReadByte());
            foreach (var skill in npc.LearnedSkills)
            {
                if (name == skill.Name)
                {
                    int index = p.SkillBook.IndexOf(skill.GetType().Name);
                    int rank = p.SkillBook[index].Rank;
                    return new UpgradeSkillDialog_2(skill, skill.Ranks[rank]);
                }
            }
            return new DialogMenu("I cannot teach you that skill.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSkill_3 : DialogMenuOption
    {
        public UpgradeSkill_3()
        {
            this.Title = "Upgrade Skill";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I cannot upgrade that skill.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var s in npc.LearnedSkills)
            {
                if (s.Name == name)
                {
                    int index = p.SkillBook.IndexOf(s.GetType().Name);

                    if (index < 0)
                    {
                        dialog.Message = "You do not have that skill.";
                        return dialog;
                    }

                    var skill = p.SkillBook[index];

                    if (skill.Rank == skill.Ranks.Length)
                    {
                        dialog.Message = "You cannot upgrade that skill any further.";
                        return dialog;
                    }

                    var rank = skill.Ranks[skill.Rank];

                    if (!rank.LearnFromNpc)
                    {
                        dialog.Message = "I cannot upgrade that skill.";
                        return dialog;
                    }

                    if (p.Level < rank.RequiredLevel)
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (rank.RequiresMaster && !p.Master)
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (p.Ability < rank.RequiredAbility)
                    {
                        dialog.Message = "Your ability is too low.";
                        return dialog;
                    }

                    foreach (var item in rank.RequiredItems)
                    {
                        if (p.Inventory.Count(item.Key) < item.Value)
                        {
                            dialog.Message = "You have not brought me the items I requested.";
                            return dialog;
                        }
                    }

                    if (p.Gold < rank.RequiredGold)
                    {
                        dialog.Message = "You do not have enough gold.";
                        return dialog;
                    }

                    p.Gold -= rank.RequiredGold;
                    p.Client.SendStatistics(StatUpdateFlags.Experience);

                    foreach (var item in rank.RequiredItems)
                    {
                        int itemIndex = p.Inventory.IndexOf(item.Key);
                        p.RemoveItem(itemIndex, item.Value);
                    }

                    skill.Rank++;
                    p.DisplaySkill(skill);
                    p.SpellAnimation(22, 50);
                    return new DialogMenu("Use this skill well.");
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSkillDialog_1 : Dialog
    {
        private Player player = null;
        public UpgradeSkillDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var npc = (GameObject as Merchant);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to upgrade?");
            p.WriteUInt16(DIALOG_UPGRADESKILL_02); // dialog id

            var skills = new List<Skill>();
            foreach (var s in npc.LearnedSkills)
            {
                int index = player.SkillBook.IndexOf(s.GetType().Name);

                if (index < 0)
                    continue;

                var skill = player.SkillBook[index];

                if (skill.Rank == skill.Ranks.Length)
                    continue;

                var rank = skill.Ranks[skill.Rank];

                if (rank.LearnFromNpc)
                    skills.Add(s);
            }
            p.WriteUInt16((ushort)skills.Count);
            foreach (var s in skills)
            {
                p.WriteByte(0x02);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    public class UpgradeSkillDialog_2 : Dialog
    {
        private Skill skill = null;
        private SkillRank rank = null;
        public UpgradeSkillDialog_2(Skill skill, SkillRank rank)
        {
            this.skill = skill;
            this.rank = rank;
        }
        public override ServerPacket ToPacket()
        {
            var stringBuilder = new StringBuilder();
            foreach (var req in rank.RequiredItems)
            {
                stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
            }
            stringBuilder.AppendFormat("{0} gold", rank.RequiredGold);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x01); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(GameObject.Name);
            p.WriteString16("I require: " + stringBuilder);
            p.WriteString8(skill.Name);
            p.WriteByte(0x03);
            p.WriteString8("Upgrade skill");
            p.WriteUInt16(DIALOG_UPGRADESKILL_03);
            p.WriteString8("Go back");
            p.WriteUInt16(DIALOG_UPGRADESKILL_01);
            return p;
        }
    }
    #endregion

    #region Upgrade Spell
    public class UpgradeSpell_1 : DialogMenuOption
    {
        public UpgradeSpell_1()
        {
            this.Title = "Upgrade Spell";
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            return new UpgradeSpellDialog_1(p);
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSpell_2 : DialogMenuOption
    {
        public UpgradeSpell_2()
        {
            this.Title = "Upgrade Spell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var name = msg.ReadString(msg.ReadByte());
            foreach (var spell in npc.LearnedSpells)
            {
                if (name == spell.Name)
                {
                    int index = p.SpellBook.IndexOf(spell.GetType().Name);
                    int rank = p.SpellBook[index].Rank;
                    return new UpgradeSpellDialog_2(spell, spell.Ranks[rank]);
                }
            }
            return new DialogMenu("I cannot teach you that spell.");
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSpell_3 : DialogMenuOption
    {
        public UpgradeSpell_3()
        {
            this.Title = "Upgrade Spell";
            this.Hidden = true;
        }

        public override Dialog Open(Player p, GameObject go, ClientPacket msg)
        {
            var npc = (go as Merchant);

            var dialog = new DialogMenu("I cannot upgrade that spell.");

            var name = msg.ReadString(msg.ReadByte());

            foreach (var s in npc.LearnedSpells)
            {
                if (s.Name == name)
                {
                    int index = p.SpellBook.IndexOf(s.GetType().Name);

                    if (index < 0)
                    {
                        dialog.Message = "You do not have that spell.";
                        return dialog;
                    }

                    var spell = p.SpellBook[index];

                    if (spell.Rank == spell.Ranks.Length)
                    {
                        dialog.Message = "You cannot upgrade that spell any further.";
                        return dialog;
                    }

                    var rank = spell.Ranks[spell.Rank];

                    if (!rank.LearnFromNpc)
                    {
                        dialog.Message = "I cannot upgrade that spell.";
                        return dialog;
                    }

                    if (p.Level < rank.RequiredLevel)
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (rank.RequiresMaster && !p.Master)
                    {
                        dialog.Message = "Your level is too low.";
                        return dialog;
                    }

                    if (p.Ability < rank.RequiredAbility)
                    {
                        dialog.Message = "Your ability is too low.";
                        return dialog;
                    }

                    foreach (var item in rank.RequiredItems)
                    {
                        if (p.Inventory.Count(item.Key) < item.Value)
                        {
                            dialog.Message = "You have not brought me the items I requested.";
                            return dialog;
                        }
                    }

                    if (p.Gold < rank.RequiredGold)
                    {
                        dialog.Message = "You do not have enough gold.";
                        return dialog;
                    }

                    p.Gold -= rank.RequiredGold;
                    p.Client.SendStatistics(StatUpdateFlags.Experience);

                    foreach (var item in rank.RequiredItems)
                    {
                        int itemIndex = p.Inventory.IndexOf(item.Key);
                        p.RemoveItem(itemIndex, item.Value);
                    }

                    spell.Rank++;
                    p.DisplaySpell(spell);
                    p.SpellAnimation(22, 50);
                    return new DialogMenu("Use this spell well.");
                }
            }

            return dialog;
        }
        public override bool CanOpen(Player p)
        {
            return true;
        }
    }
    public class UpgradeSpellDialog_1 : Dialog
    {
        private Player player = null;
        public UpgradeSpellDialog_1(Player player)
        {
            this.player = player;
        }
        public override ServerPacket ToPacket()
        {
            var npc = (GameObject as Merchant);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x07); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("What would you like to upgrade?");
            p.WriteUInt16(DIALOG_UPGRADESPELL_02); // dialog id

            var spells = new List<Spell>();
            foreach (var s in npc.LearnedSpells)
            {
                int index = player.SpellBook.IndexOf(s.GetType().Name);

                if (index < 0)
                    continue;

                var spell = player.SpellBook[index];

                if (spell.Rank == spell.Ranks.Length)
                    continue;

                var rank = spell.Ranks[spell.Rank];

                if (rank.LearnFromNpc)
                    spells.Add(s);
            }
            p.WriteUInt16((ushort)spells.Count);
            foreach (var s in spells)
            {
                p.WriteByte(0x02);
                p.WriteUInt16((ushort)s.Icon);
                p.WriteByte(0x00);
                p.WriteString8(s.Name);
            }
            return p;
        }
    }
    public class UpgradeSpellDialog_2 : Dialog
    {
        private Spell spell = null;
        private SpellRank rank = null;
        public UpgradeSpellDialog_2(Spell spell, SpellRank rank)
        {
            this.spell = spell;
            this.rank = rank;
        }
        public override ServerPacket ToPacket()
        {
            var stringBuilder = new StringBuilder();
            foreach (var req in rank.RequiredItems)
            {
                stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
            }
            stringBuilder.AppendFormat("{0} gold", rank.RequiredGold);

            var p = new ServerPacket(0x2F);
            p.WriteByte(0x01); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16("I require: " + stringBuilder);
            p.WriteString8(spell.Name);
            p.WriteByte(0x02);
            p.WriteString8("Upgrade spell");
            p.WriteUInt16(DIALOG_UPGRADESPELL_03);
            p.WriteString8("Go back");
            p.WriteUInt16(DIALOG_UPGRADESPELL_01);
            return p;
        }
    }
    #endregion

    public abstract class DialogMenuOption
    {
        public string Title { get; protected set; }
        public bool Hidden { get; protected set; }
        public string ChatTrigger { get; protected set; }
        public abstract Dialog Open(Player p, GameObject go, ClientPacket msg);
        public abstract bool CanOpen(Player p);
        public DialogMenuOption()
        {
            this.Title = string.Empty;
            this.ChatTrigger = string.Empty;
        }
    }

    public abstract class QuestMenuOption : DialogMenuOption
    {
        public string QuestType { get; protected set; }
        public int QuestStep { get; protected set; }
        public QuestProgress MinimumProgress { get; protected set; }
        public QuestProgress MaximumProgress { get; protected set; }
        public QuestMenuOption()
        {
            this.QuestType = string.Empty;
            this.MinimumProgress = QuestProgress.Unstarted;
            this.MaximumProgress = QuestProgress.Finished;
        }
        public override bool CanOpen(Player p)
        {
            return p.Quests[QuestType].Progress != QuestProgress.Finished && p.Quests[QuestType].CurrentStep == QuestStep;
        }
    }

    public abstract class NormalDialog : DialogB
    {
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x30);
            p.WriteByte(0x00); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x00); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00);
            p.WriteUInt16(0x0000); // dialog id (unused)
            p.WriteUInt16(0x0001); // dialog page (always 1)
            p.WriteByte(CanGoBack);
            p.WriteByte(CanGoNext);
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(Message);
            return p;
        }
    }

    public abstract class InputDialog : DialogB
    {
        public int InputLength { get; set; }
        public string CaptionA { get; set; }
        public string CaptionB { get; set; }
        public InputDialog()
        {
            this.InputLength = 20;
            this.CaptionA = string.Empty;
            this.CaptionB = string.Empty;
        }
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x30);
            p.WriteByte(0x04); // type!
            p.WriteByte(0x01);
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x00); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteUInt16(0x0000); // dialog id (unused)
            p.WriteUInt16(0x0001); // dialog page (always 1)
            p.WriteByte(CanGoBack);
            p.WriteByte(CanGoNext);
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(Message);
            p.WriteString8(CaptionA);
            p.WriteByte((byte)InputLength);
            p.WriteString8(CaptionB);
            return p;
        }
    }

    public abstract class OptionDialog : DialogB
    {
        public List<string> Options { get; private set; }
        public OptionDialog()
        {
            this.Options = new List<string>();
        }
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
        public override ServerPacket ToPacket()
        {
            var p = new ServerPacket(0x30);
            p.WriteByte(0x02); // type!
            p.WriteByte(0x01); // ??
            p.WriteUInt32(GameObject.ID);
            p.WriteByte(0x00); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteByte(0x01); // ??
            p.WriteUInt16(Image);
            p.WriteByte(0x00); // ??
            p.WriteUInt16(0x0000); // dialog id (unused)
            p.WriteUInt16(0x0001); // dialog page (always 1)
            p.WriteByte(CanGoBack);
            p.WriteByte(CanGoNext);
            p.WriteByte(0x00); // ??
            p.WriteString8(Name);
            p.WriteString16(Message);
            p.WriteByte((byte)Options.Count);
            foreach (string option in Options)
                p.WriteString8(option);
            return p;
        }
    }

    public class BindOnEquipDialog : OptionDialog
    {
        public BindOnEquipDialog()
        {
            this.CanGoBack = false;
            this.CanGoNext = true;
            this.Options.Add("No");
            this.Options.Add("Yes");
        }
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            msg.ReadByte();
            var option = msg.ReadByte();

            if (option != 2)
                return null;

            var item = (GameObject as Equipment);
            if (item != null && item.BindType == BindType.BindOnEquip && !item.Soulbound)
            {
                item.Soulbound = true;

                p.RemoveItem(item);
                item.Amount = 1;

                int index = (item.EquipmentSlot - 1);
                if ((item is Ring) && (p.LeftRing != null) && (p.RightRing == null))
                    index = 7;
                else if ((item is Gauntlet) && (p.LeftGauntlet != null) && (p.RightGauntlet == null))
                    index = 9;
                else if ((item is Accessory) && (p.AccessoryA != null) && (p.AccessoryB == null))
                    index = 21;

                if (p.Equipment[index] != null)
                    p.AddItem(p.RemoveEquipment(index));

                p.AddEquipment(item, index);

                p.Display();
            }
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
    }

    public class ItemPatchDialog : OptionDialog
    {
        public ItemPatchDialog()
        {
            this.CanGoBack = false;
            this.CanGoNext = true;
            this.Options.Add("No");
            this.Options.Add("Yes");
        }
        public override DialogB Back(Player p, ClientPacket msg)
        {
            return null;
        }
        public override DialogB Next(Player p, ClientPacket msg)
        {
            msg.ReadByte();
            var option = msg.ReadByte();

            if (option != 2)
                return null;

            var armor = p.Armor;
            var item = (GameObject as Patch);
            if (item != null && armor != null && p.Inventory.Contains(item))
            {
                armor.Soulbound = true;
                p.RemoveEquipment(armor);
                armor.DynamicMaximumHpMod = item.MaximumHpMod;
                armor.DynamicMaximumMpMod = item.MaximumMpMod;
                armor.DynamicStrMod = item.StrMod;
                armor.DynamicIntMod = item.IntMod;
                armor.DynamicWisMod = item.WisMod;
                armor.DynamicConMod = item.ConMod;
                armor.DynamicDexMod = item.DexMod;
                armor.DynamicArmorClassMod = item.ArmorClassMod;
                armor.DynamicMagicResistanceMod = item.MagicResistanceMod;
                armor.DynamicMinimumAttackPowerMod = item.MinimumAttackPowerMod;
                armor.DynamicMaximumAttackPowerMod = item.MaximumAttackPowerMod;
                armor.DynamicMinimumMagicPowerMod = item.MinimumMagicPowerMod;
                armor.DynamicMaximumMagicPowerMod = item.MaximumMagicPowerMod;
                p.AddEquipment(armor);
                p.RemoveItem(item);
            }
            return null;
        }
        public override DialogB Exit(Player p, ClientPacket msg)
        {
            return null;
        }
    }

    public class DialogSession
    {
        public GameObject GameObject { get; set; }
        public DialogB Dialog { get; set; }
        public bool IsOpen { get; set; }
        public Map Map { get; set; }
        public DialogSession()
        {

        }
    }
}