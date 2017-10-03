using System;
using System.Collections.Generic;
using System.Linq;

namespace Wewladh
{
    public abstract class Merchant : Monster
    {
        public string Portrait { get; protected set; }
        public string Greeting { get; set; }
        public List<string> DialogMenuOptions { get; private set; }

        public List<Item> Inventory { get; private set; }
        public List<Type> SellItems { get; private set; }
        public List<Skill> LearnedSkills { get; private set; }
        public List<Spell> LearnedSpells { get; private set; }

        public bool HasBuy { get; protected set; }
        public bool HasSell { get; protected set; }
        public bool HasDeposit { get; protected set; }
        public bool HasWithdraw { get; protected set; }
        public bool HasLearnSkill { get; protected set; }
        public bool HasLearnSpell { get; protected set; }
        public bool HasForgetSkill { get; protected set; }
        public bool HasForgetSpell { get; protected set; }
        public bool HasUpgradeSkill { get; protected set; }
        public bool HasUpgradeSpell { get; protected set; }

        public Merchant()
        {
            this.Type = MonsterType.Merchant;
            this.Ally = NpcAlly.Neutral;
            this.Faction = "Faction_Default_Neutral";
            this.Greeting = "Hello.  What can I do for you?";
            this.Inventory = new List<Item>();
            this.SellItems = new List<Type>();
            this.LearnedSkills = new List<Skill>();
            this.LearnedSpells = new List<Spell>();
            this.DialogMenuOptions = new List<string>();
        }
        public override void DisplayTo(VisibleObject obj)
        {
            if (Hidden || Stealth)
                return;

            if (obj is Player)
            {
                var player = (obj as Player);
                var client = player.Client;

                var p = new ServerPacket(0x07);
                p.WriteUInt16(1);
                p.WriteUInt16((ushort)Point.X);
                p.WriteUInt16((ushort)Point.Y);
                p.WriteUInt32((uint)ID);
                p.WriteUInt16((ushort)((Polymorphed ? PolymorphForm : Sprite) + 0x4000));
                p.WriteByte(0); // random 1
                p.WriteByte(0); // random 2
                p.WriteByte(0); // random 3
                p.WriteByte(0); // unknown a
                p.WriteByte((byte)Direction);
                p.WriteByte(0); // unknown b
                p.WriteByte(Cursed);
                p.WriteByte(0); // unknown d
                p.WriteByte((byte)Type);
                p.WriteString8(Name);

                client.Enqueue(p);
            }
        }
        public override void OnClick(Client sender)
        {
            DialogMenu dm = new DialogMenu(this, Greeting);

            if (HasBuy) dm.Options.Add(Dialog.DIALOG_BUY_01, new Buy_1());
            if (HasSell) dm.Options.Add(Dialog.DIALOG_SELL_01, new Sell_1());
            if (HasDeposit) dm.Options.Add(Dialog.DIALOG_DEPOSIT_01, new Deposit_1());
            if (HasWithdraw) dm.Options.Add(Dialog.DIALOG_WITHDRAW_01, new Withdraw_1());
            if (HasLearnSkill) dm.Options.Add(Dialog.DIALOG_LEARNSKILL_01, new LearnSkill_1());
            if (HasLearnSpell) dm.Options.Add(Dialog.DIALOG_LEARNSPELL_01, new LearnSpell_1());
            if (HasForgetSkill) dm.Options.Add(Dialog.DIALOG_FORGETSKILL_01, new ForgetSkill_1());
            if (HasForgetSpell) dm.Options.Add(Dialog.DIALOG_FORGETSPELL_01, new ForgetSpell_1());
            if (HasUpgradeSkill) dm.Options.Add(Dialog.DIALOG_UPGRADESKILL_01, new UpgradeSkill_1());
            if (HasUpgradeSpell) dm.Options.Add(Dialog.DIALOG_UPGRADESPELL_01, new UpgradeSpell_1());

            if (DialogMenuOptions.Count == 1 && dm.Options.Count == 0)
            {
                var type = DialogMenuOptions[0];
                var dmo = GameServer.DialogMenuOptionDatabase[type];

                if (dmo.CanOpen(sender.Player) && !(dmo is QuestMenuOption))
                {
                    var dialog = dmo.Open(sender.Player, this, null);
                    GiveDialog(sender.Player, dialog);
                    return;
                }
            }

            for (int i = 0; i < DialogMenuOptions.Count; i++)
            {
                var type = DialogMenuOptions[i];
                var dmo = GameServer.DialogMenuOptionDatabase[type];

                if (dmo.CanOpen(sender.Player) && !dmo.Hidden)
                {
                    if (dmo is QuestMenuOption)
                    {
                        var qmo = dmo as QuestMenuOption;
                        var quest = sender.Player.Quests[qmo.QuestType];
                        if (quest.Progress == QuestProgress.Finished ||
                            quest.QuestStep.Progress < qmo.MinimumProgress ||
                            quest.QuestStep.Progress > qmo.MaximumProgress ||
                            quest.CurrentStep != qmo.QuestStep ||
                            sender.Player.Level < quest.MinimumLevel ||
                            sender.Player.Level > quest.MaximumLevel ||
                            !quest.Prerequisites.TrueForAll(req => sender.Player.Quests[req].Progress == QuestProgress.Finished))
                            continue;
                    }
                    var id = (ushort)(Dialog.DIALOG_GLOBAL_MAX + i);
                    dm.Options.Add(id, dmo);
                }
            }

            sender.Enqueue(dm.ToPacket());
        }
        public override void Update()
        {
            if (Alive && Chatter.Count > 0 && DateTime.UtcNow > NextChatter)
            {
                int index = ChatterIndex % Chatter.Count;
                ChatterIndex++;
                Say(Chatter[index], 0);
                NextChatter = DateTime.UtcNow.AddSeconds(Program.Random(60, 90));
            }

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

            var characters = new Character[ThreatMeter.Count];
            ThreatMeter.Keys.CopyTo(characters, 0);
            foreach (var c in characters)
            {
                if (c == null || c.Dead || !WithinRange(c, 12))
                {
                    ThreatMeter.Remove(c);
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
                else
                {
                    foreach (var m in e.Group.Members)
                    {
                        this.Enemies.Add(m);
                        m.Enemies.Add(this);
                    }
                }
            }

            if (SpawnControl != null && SpawnControl.SpecificTime && SpawnControl.SpawnTime != GameServer.Time && !Dead)
            {
                LifeStatus = LifeStatus.Dead;
                Experience = 0;
            }

            if (CurrentHP <= 0 && Alive)
            {
                if (ShouldComa)
                {
                    CurrentHP = 1;
                    AddStatus("Spell_Coma", 1, 20, this);
                }
                else
                {
                    CurrentHP = 0;
                    LifeStatus = LifeStatus.Dead;
                }
            }

            switch (LifeStatus)
            {
                case LifeStatus.Alive:
                    {
                        if (DateTime.UtcNow > NextTick)
                        {
                            if (IsHostile && Target == null)
                            {
                                var targets = from t in
                                                  from obj in Map.Objects
                                                  where obj is Character && WithinRange(obj, HostileRange)
                                                  select obj as Character
                                              where t.Alive && AllegianceTo(t) == Allegiance.Enemy && !t.Hidden && !t.Stealth
                                              orderby Point.DistanceFrom(t.Point) ascending
                                              select t;

                                if (targets.Count() > 0)
                                {
                                    var target = targets.First();
                                    Enemies.Add(target);
                                    target.Enemies.Add(this);
                                    target.Threaten(this, 1);
                                }
                            }

                            UpdateTarget();
                            if (IsCasting)
                            {
                                OnTickCasting();
                            }
                            else
                            {
                                OnTick();
                            }
                            TickCount++;
                            NextTick = DateTime.UtcNow.AddMilliseconds(TickSpeed);
                        }
                    } break;
                case LifeStatus.Coma:
                    {
                        if (DateTime.UtcNow > NextTick)
                        {
                            OnTickComa();
                            TickCount++;
                            NextTick = DateTime.UtcNow.AddMilliseconds(TickSpeed);
                        }
                    } break;
                case LifeStatus.Dying:
                    {
                        if (DateTime.UtcNow > NextTick)
                        {
                            OnTickDying();
                            TickCount++;
                            NextTick = DateTime.UtcNow.AddMilliseconds(TickSpeed);
                        }
                    } break;
                case LifeStatus.Dead:
                    {
                        #region Remove Statuses
                        statuses = new string[Statuses.Count];
                        Statuses.Keys.CopyTo(statuses, 0);
                        foreach (var s in statuses)
                        {
                            RemoveStatus(s);
                        }
                        #endregion

                        #region Remove Threat
                        characters = new Character[ThreatMeter.Count];
                        ThreatMeter.Keys.CopyTo(characters, 0);
                        foreach (var c in characters)
                        {
                            ThreatMeter.Remove(c);
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

                        #region Give EXP / Quest Kill

                        double highestGroupValue = 0;
                        double highestPlayerValue = 0;

                        Group highestGroup = null;
                        Player highestPlayer = null;

                        var groups = new Dictionary<Group, double>();
                        foreach (var attacker in Attackers)
                        {
                            if (attacker.Key is Player)
                            {
                                var player = attacker.Key as Player;

                                if (groups.ContainsKey(player.Group))
                                    groups[player.Group] += attacker.Value;
                                else
                                    groups.Add(player.Group, attacker.Value);

                                if (groups[player.Group] > highestGroupValue)
                                {
                                    highestGroup = player.Group;
                                    highestGroupValue = groups[player.Group];
                                }

                                if (attacker.Value > highestPlayerValue)
                                {
                                    highestPlayer = player;
                                    highestPlayerValue = attacker.Value;
                                }
                            }
                        }

                        foreach (var group in groups)
                        {
                            if (group.Value < MaximumHP * 0.15)
                                continue;

                            long averageLevel = 0;
                            var players = new List<Player>();

                            foreach (var p in group.Key.Members)
                            {
                                if (p.WithinRange(this, 12) && p is Player)
                                {
                                    averageLevel += p.Level;
                                    players.Add(p as Player);
                                }
                            }

                            if (players.Count > 0)
                                averageLevel /= players.Count;

                            foreach (var p in players)
                            {
                                var difference = Math.Abs(averageLevel - Level);

                                if (difference < 5)
                                {
                                    var percent = 1.0 - difference * 0.125;
                                    if (Program.Random(100) < p.ExperienceBonusChance)
                                    {
                                        percent *= p.ExperienceBonus;
                                        p.SpellAnimation(341, 100);
                                    }
                                    p.RewardExperience((long)(Experience * percent));
                                }

                                foreach (var q in p.Quests)
                                {
                                    var qs = q.Value.QuestStep;
                                    foreach (var qo in qs.Objectives.Values)
                                    {
                                        if (qo.Type == QuestObjectiveType.Kill && qo.RequiredKilledTypes.Contains(GetType().Name))
                                        {
                                            if (qo.GroupKill || p == highestPlayer)
                                                p.AddQuestCount(q.Key, qo.Name);
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Close Dialogs
                        foreach (Client c in GameServer.Clients)
                        {
                            if (c.Player != null && c.Player.DialogSession.GameObject == this && c.Player.DialogSession.IsOpen)
                            {
                                c.Player.DialogSession.IsOpen = false;
                                c.Player.DialogSession.Dialog = null;
                                c.Player.DialogSession.GameObject = null;
                                c.Player.DialogSession.Map = null;
                                c.Enqueue(Dialog.ExitPacket());
                            }
                        }
                        #endregion

                        #region Loot
                        if (SpawnControl != null || CanDropLootWithoutSpawn)
                        {
                            var chest = new Chest();

                            foreach (Loot loot in Loot)
                            {
                                if (Program.Random(loot.MaximumValue) < loot.MinimumValue)
                                {
                                    int value;
                                    int index = Program.Random(loot.Items.Count);
                                    var item = loot.Items[index];
                                    Item drop = null;

                                    if (int.TryParse(item, out value))
                                    {
                                        drop = new Gold(value);
                                        GameServer.InsertGameObject(drop);
                                    }
                                    else
                                    {
                                        drop = GameServer.CreateItem(item);
                                    }

                                    if (drop != null)
                                    {
                                        if (drop.LootRollLength > 0 || drop.BindType != BindType.None)
                                        {
                                            var chestItem = new Chest.ChestItem();
                                            chestItem.Item = drop;
                                            chest.Items.Add(chestItem);
                                        }
                                        else
                                        {
                                            if (highestGroup != null)
                                            {
                                                foreach (var member in highestGroup.Members)
                                                    drop.ProtectionOwners.Add(member.Name);
                                                drop.ProtectionExpireTime = DateTime.UtcNow.AddSeconds(60);
                                            }
                                            Map.InsertCharacter(drop, Point);
                                        }
                                    }
                                }
                            }

                            if (chest.Items.Count > 0 && highestGroup != null)
                            {
                                foreach (var member in highestGroup.Members)
                                    chest.GUIDs.Add(member.GUID);

                                chest.Direction = Direction.South;
                                GameServer.InsertGameObject(chest);
                                Map.InsertCharacter(chest, Point);
                            }
                        }
                        #endregion

                        this.OnDeath();
                        this.OnDeath(highestPlayer);

                        Map.RemoveCharacter(this);
                        GameServer.RemoveGameObject(this);
                    } break;
            }

            if (DateTime.UtcNow.Subtract(LastHpRegen).TotalSeconds > 1)
            {
                if (Alive && CurrentHP < MaximumHP && Target == null)
                {
                    CurrentHP += (long)(MaximumHP * 0.10);
                    if (CurrentHP > MaximumHP)
                        CurrentHP = MaximumHP;
                }
                LastHpRegen = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(LastMpRegen).TotalSeconds > 1)
            {
                if (Alive && CurrentMP < MaximumMP && Target == null)
                {
                    CurrentMP += (long)(MaximumMP * 0.10);
                    if (CurrentMP > MaximumMP)
                        CurrentMP = MaximumMP;
                }
                LastMpRegen = DateTime.UtcNow;
            }
        }
    }

    public class Block : Monster
    {
        public Block()
        {
            this.Name = "Block";
            this.Sprite = 641;
            this.Ally = NpcAlly.Neutral;
            this.Type = MonsterType.Guardian;
            this.BaseMaximumHP = int.MaxValue;
            this.BaseMaximumMP = int.MaxValue;
            this.BaseArmorClass = int.MinValue;
        }

        public override void Update()
        {

        }
        public override void OnTick()
        {

        }
        public override void OnChatMessage(Character c, string message)
        {

        }
        public override Allegiance AllegianceTo(Character c)
        {
            return Allegiance.Neutral;
        }
    }

    public class Chest : Merchant
    {
        private DateTime start = DateTime.UtcNow;
        public HashSet<long> GUIDs { get; private set; }
        public HashSet<ChestItem> Items { get; private set; }
        public Chest()
        {
            this.GUIDs = new HashSet<long>();
            this.Items = new HashSet<ChestItem>();
            this.Ally = NpcAlly.Neutral;

            this.Sprite = 90;
            this.Name = "Chest";
        }
        public override void Update()
        {
            var items = new ChestItem[Items.Count];
            Items.CopyTo(items);

            if (DateTime.UtcNow.Subtract(start).TotalMinutes < 1 && items.Length > 0)
            {
                foreach (var item in items)
                {
                    if (item.Choices.Count == GUIDs.Count)
                    {
                        GiveItem(item);
                        Items.Remove(item);
                    }
                }
            }
            else
            {
                foreach (var item in items)
                {
                    GiveItem(item);
                    Items.Remove(item);
                }

                GameServer.RemoveGameObject(this);
            }
        }
        public override void OnTick()
        {

        }
        public override void OnChatMessage(Character c, string message)
        {

        }
        public override void OnClick(Client sender)
        {
            if (GUIDs.Contains(sender.Player.GUID))
            {
                var dialog = new LootDialog();
                foreach (var item in Items)
                {
                    dialog.Options.Add(item.Item.Name);
                }
                dialog.Message = "Which item would you like to roll for?";
                GiveDialog(sender.Player, dialog);
            }
        }
        public override Allegiance AllegianceTo(Character c)
        {
            return Allegiance.Neutral;
        }
        public void GiveItem(ChestItem chestItem)
        {
            var need = new List<Player>();
            var greed = new List<Player>();

            foreach (var player in chestItem.Choices.Keys)
            {
                if (player != null && GameServer.GameObjects.Contains(player))
                {
                    switch (chestItem.Choices[player])
                    {
                        case 1: need.Add(player); break;
                        case 2: greed.Add(player); break;
                    }
                }
            }

            if (need.Count > 0)
            {
                var player = need[Program.Random(need.Count)];
                if (player.AddItem(chestItem.Item))
                    chestItem.Item.OnPickup(player);
                Say(string.Format("{0} won the {1}!", player.Name, chestItem.Item.Name), 1);
            }
            else if (greed.Count > 0)
            {
                var player = greed[Program.Random(greed.Count)];
                if (player.AddItem(chestItem.Item))
                    chestItem.Item.OnPickup(player);
                Say(string.Format("{0} won the {1}!", player.Name, chestItem.Item.Name), 1);
            }

            chestItem.Choices.Clear();
        }

        public class LootDialog : OptionDialog
        {
            public LootDialog()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                var chest = GameObject as Chest;

                msg.ReadByte();
                var option = msg.ReadByte() - 1;
                if (option < chest.Items.Count)
                {
                    var items = new ChestItem[chest.Items.Count];
                    chest.Items.CopyTo(items);

                    var item = items[option];

                    if (item.Choices.ContainsKey(p))
                    {
                        p.Client.SendMessage("You have already rolled on that item");
                        return null;
                    }

                    var attr = new List<string>();

                    if (item.Item is Equipment)
                    {
                        attr.Add(string.Format("Level {0} {1} {2}", Math.Max(item.Item.Level, 1),
                            item.Item.Class, item.Item.GetType().BaseType.Name));
                    }
                    else
                    {
                        if (item.Item.Level > 1)
                            attr.Add("Level: " + item.Item.Level);
                        if (item.Item.Class != Profession.Peasant)
                            attr.Add("Class: " + item.Item.Class);
                    }

                    if (item.Item.StrMod != 0)
                        attr.Add("STR: " + item.Item.StrMod);
                    if (item.Item.IntMod != 0)
                        attr.Add("INT: " + item.Item.IntMod);
                    if (item.Item.WisMod != 0)
                        attr.Add("WIS: " + item.Item.WisMod);
                    if (item.Item.ConMod != 0)
                        attr.Add("CON: " + item.Item.ConMod);
                    if (item.Item.DexMod != 0)
                        attr.Add("DEX: " + item.Item.DexMod);
                    if (item.Item.HitMod != 0)
                        attr.Add("HIT: " + item.Item.HitMod);
                    if (item.Item.DmgMod != 0)
                        attr.Add("DMG: " + item.Item.DmgMod);
                    if (item.Item.MaximumHpMod != 0)
                        attr.Add("HP: " + item.Item.MaximumHpMod);
                    if (item.Item.MaximumMpMod != 0)
                        attr.Add("MP: " + item.Item.MaximumMpMod);
                    if (item.Item.ArmorClassMod != 0)
                        attr.Add("AC: " + item.Item.ArmorClassMod);
                    if (item.Item.MagicResistanceMod != 0)
                        attr.Add("MR: " + item.Item.MagicResistanceMod);
                    if (item.Item.MaximumAttackPowerMod != 0)
                        attr.Add(string.Format("Attack Power: {0}-{1}", item.Item.MinimumAttackPowerMod, item.Item.MaximumAttackPowerMod));
                    if (item.Item.MaximumMagicPowerMod != 0)
                        attr.Add(string.Format("Magic Power: {0}-{1}", item.Item.MinimumMagicPowerMod, item.Item.MaximumMagicPowerMod));

                    var dialog = new LootItemDialog();
                    dialog.ChestItem = item;
                    dialog.Message = string.Join(", ", attr);
                    dialog.CustomName = item.Item.Name;
                    dialog.CustomImage = (ushort)(item.Item.Sprite + 0x8000);
                    return dialog;
                }

                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class LootItemDialog : OptionDialog
        {
            public ChestItem ChestItem { get; set; }

            public LootItemDialog()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.Options.Add("Need");
                this.Options.Add("Greed");
                this.Options.Add("Pass");
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                var chest = GameObject as Chest;

                if (chest.Items.Contains(ChestItem))
                {
                    msg.ReadByte();
                    var option = msg.ReadByte();

                    switch (option)
                    {
                        case 1:
                            chest.Say(string.Format("{0} rolls need for {1}.", p.Name, ChestItem.Item.Name), 1);
                            ChestItem.Choices.Add(p, 1);
                            break;
                        case 2:
                            chest.Say(string.Format("{0} rolls greed for {1}.", p.Name, ChestItem.Item.Name), 1);
                            ChestItem.Choices.Add(p, 2);
                            break;
                        default:
                            chest.Say(string.Format("{0} passed on {1}.", p.Name, ChestItem.Item.Name), 1);
                            ChestItem.Choices.Add(p, 0);
                            break;
                    }
                }

                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class ChestItem
        {
            public Item Item { get; set; }
            public Dictionary<Player, int> Choices { get; set; }
            public ChestItem()
            {
                this.Choices = new Dictionary<Player, int>();
            }
        }
    }
}