using System;
using System.Linq;
using System.Collections.Generic;

namespace Wewladh
{
    public abstract class Monster : Character
    {
        #region Properties
        public Spawn SpawnControl { get; set; }
        public MonsterType Type { get; protected set; }
        public DateTime NextTick { get; set; }
        public long TickCount { get; set; }
        public int BaseTickSpeed { get; protected set; }
        public int MinimumTickSpeed { get; protected set; }
        public int TickSpeedMod { get; set; }
        public int TickSpeed
        {
            get
            {
                int value = BaseTickSpeed + TickSpeedMod;
                if (value < MinimumTickSpeed)
                    return MinimumTickSpeed;
                return value;
            }
        }
        public Dictionary<Character, double> ThreatMeter { get; set; }
        public bool IsHostile { get; protected set; }
        public int HostileRange { get; protected set; }
        public Character Target { get; set; }
        public int MinimumLevelForExperience { get; protected set; }
        public int MaximumLevelForExperience { get; protected set; }
        public bool ShouldComa { get; protected set; }
        public Character ForcedTarget { get; set; }
        public NpcAlly Ally { get; protected set; }

        public bool UseCastLines { get; protected set; }
        public bool DisplayCastLines { get; protected set; }

        public List<Loot> Loot { get; private set; }
        public bool CanDropLootWithoutSpawn { get; protected set; }

        public Point[] Path { get; set; }
        public int PathStep { get; set; }
        public Point PathEndPoint { get; set; }

        public List<string> Chatter { get; private set; }
        public DateTime NextChatter { get; protected set; }
        public int ChatterIndex { get; protected set; }

        public List<string> SkillImmunities { get; protected set; }
        public List<string> SpellImmunities { get; protected set; }
        #endregion

        public virtual void OnTick()
        {

        }
        public virtual void OnTickComa()
        {

        }
        public virtual void OnTickDying()
        {
            this.LifeStatus = LifeStatus.Dead;
        }
        public virtual void OnDeath() { }
        public virtual void OnDeath(Player p) { }
        public virtual void OnTickCasting() { }
        public virtual void OnChatMessage(Character c, string message)
        {

        }

        public Monster()
        {
            this.BaseTickSpeed = 1000;
            this.MinimumTickSpeed = 100;
            this.NextTick = DateTime.UtcNow.AddMilliseconds(TickSpeed);
            this.Path = new Point[0];
            this.ThreatMeter = new Dictionary<Character, double>();
            this.MinimumLevelForExperience = 1;
            this.MaximumLevelForExperience = 100;
            this.Loot = new List<Loot>();
            this.Chatter = new List<string>();
            this.SkillImmunities = new List<string>();
            this.SpellImmunities = new List<string>();

            this.Type = MonsterType.Normal;
            this.Ally = NpcAlly.Enemy;
            this.Faction = "Faction_Default_Enemy";
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
                client.Enqueue(p);
            }
        }
        public override void OnClick(Client client)
        {
            client.SendMessage(string.Format("{0} [Level {1}]", Name, Level));
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
                            if (!IsCasting)
                                OnTick();
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

        public override void Display()
        {
            foreach (var c in Map.Objects)
            {
                DisplayTo(c);
            }
        }
        public virtual void OnDamaged(Character c, double dmg)
        {

        }
        public void UpdateTarget()
        {
            var target = Target;
            Target = null;

            if (ThreatMeter.Count > 0)
            {
                if (ForcedTarget != null && ThreatMeter.ContainsKey(ForcedTarget))
                {
                    var highest = (from t in ThreatMeter
                                   orderby t.Value descending
                                   select t.Value).First();
                    ThreatMeter[ForcedTarget] = highest + 1;
                }

                var targets = from t in ThreatMeter
                              where t.Key.Alive && AllegianceTo(t.Key) == Allegiance.Enemy && !t.Key.Hidden && !t.Key.Stealth
                              orderby t.Value descending, Point.DistanceFrom(t.Key.Point) ascending
                              select t.Key;

                if (SpawnControl != null &&
                    SpawnControl.RegionType == SpawnRegion.Rectangle &&
                    !SpawnControl.CanWalkOutsideRectangle &&
                    !SpawnControl.CanFollowOutsideRectangle)
                {
                    int x1 = SpawnControl.X;
                    int y1 = SpawnControl.Y;
                    int x2 = SpawnControl.Right + x1;
                    int y2 = SpawnControl.Bottom + y1;
                    targets = from t in targets
                              where t.Point.X >= x1 && t.Point.X <= x2 && t.Point.Y >= y1 && t.Point.Y <= y2
                              select t;
                }

                Target = (targets.Count() > 0) ? targets.First() : null;
                if ((Target != null) && (Target != target) && (Target is Player))
                    (Target as Player).Client.SpellAnimation(ID, 160, 100);
            }
        }

        public override void UseSkill(Skill s)
        {
            if (DateTime.UtcNow < s.NextAvailableUse)
                return;

            if (Polymorphed)
                return;

            if (Alive && !s.CanUseAlive)
                return;

            if (Dying && !s.CanUseDying)
                return;

            if (Dead && !s.CanUseDead)
                return;

            if (Coma && !s.CanUseInComa)
                return;

            if (Frozen && !s.CanUseFrozen)
                return;

            if (Sleeping && !s.CanUseAsleep)
                return;

            if (Hidden && !s.CanUseHidden)
                return;

            if (!Hidden && s.RequiresHidden)
                return;

            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    RemoveStatus(status.StatusName);
            }

            if (Confused && (Program.Random(2) == 0))
            {
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

                                if (target == null || target is Chest || target is Block)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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
                                    {
                                        InvokeImpact(this, target);
                                    }

                                    if (s.IsAssail && target is Player)
                                    {
                                        var player = (target as Player);
                                        var items = new Equipment[player.Equipment.Length];
                                        player.Equipment.CopyTo(items, 0);
                                        foreach (var item in items)
                                        {
                                            if (item != null && --item.CurrentDurability < 1)
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

                                    if (target == null || target is Chest || target is Block)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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
                                        {
                                            InvokeImpact(this, target);
                                        }

                                        if (s.IsAssail && (target is Player))
                                        {
                                            var player = (target as Player);
                                            var items = new Equipment[player.Equipment.Length];
                                            player.Equipment.CopyTo(items, 0);
                                            foreach (var item in items)
                                            {
                                                if (item != null && --item.CurrentDurability < 1)
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

                                if (Map.Walls[t.Point.X, t.Point.Y])
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

                                    if (target == null || target is Chest || target is Block)
                                        continue;

                                    foundtarget = true;

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                        continue;

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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
                                        {
                                            InvokeImpact(this, target);
                                        }

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

                                if (Map.Walls[t.Point.X, t.Point.Y])
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

                            if (target == null || target is Chest || target is Block)
                                continue;

                            if (WithinRange(target, maximumDistance) && target != this)
                            {
                                if (AllegianceTo(target) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                    continue;

                                if (AllegianceTo(target) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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
                                    {
                                        InvokeImpact(this, target);
                                    }

                                    if (s.IsAssail && target is Player)
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
                if (hidden && Hidden)
                    RemoveStatus("Hidden");

                if (s.BodyAnimation != 0)
                    BodyAnimation(s.BodyAnimation, 20);

                Cooldown(s);
            }
        }
        public override void UseSpell(Spell s, Character target, string args)
        {
            UseSpell(s, target, args, s.Duration);
        }
        public override void UseSpell(Spell s, Character target, string args, int duration)
        {
            if (DateTime.UtcNow < s.NextAvailableUse.AddSeconds(s.CastLines))
                return;

            if (Polymorphed && !s.CanUseMorphed)
                return;

            if (Silenced && !s.CanUseSilenced)
                return;

            if (Alive && !s.CanUseAlive)
                return;

            if (Dying && !s.CanUseDying)
                return;

            if (Dead && !s.CanUseDead)
                return;

            if (Coma && !s.CanUseInComa)
                return;

            if (Frozen && !s.CanUseFrozen)
                return;

            if (Sleeping && !s.CanUseAsleep)
                return;

            if (Hidden && !s.CanUseHidden)
                return;

            var statuses = new Spell[Statuses.Count];
            Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    RemoveStatus(status.StatusName);
            }

            int manaCost = (int)(BaseMaximumMP * s.ManaPercentage + s.ManaCost);

            if (CurrentMP < manaCost)
                return;

            if (Confused && Program.Random(2) == 0)
            {
                Cooldown(s);
                return;
            }

            long successRate = s.SuccessRate + Hit;
            int count = 0;
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
                                            if (s.ReplaceStatus && Statuses[s.StatusName].GetType() != s.GetType())
                                            {
                                                RemoveStatus(s.StatusName);
                                            }
                                            else if (!s.ToggleStatus)
                                            {
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

                                                if (AllegianceTo(c) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                                    continue;

                                                if (AllegianceTo(c) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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

                                                    if (c is Player)
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

                                                if (AllegianceTo(t) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                                    continue;

                                                if (AllegianceTo(t) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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
                                        Cooldown(s);
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
                                                    return;
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

                                    if (target is Monster && (target as Monster).SpellImmunities.Contains(s.GetType().Name))
                                        return;

                                    if (target.Alive && !s.CanUseOnAliveTarget)
                                        return;

                                    if (target.Dying && !s.CanUseOnDyingTarget)
                                        return;

                                    if (target.Dead && !s.CanUseOnDeadTarget)
                                        return;

                                    if (target.Coma && !s.CanUseOnComaTarget)
                                        return;

                                    if (AllegianceTo(target) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                        return;

                                    if (AllegianceTo(target) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
                                        return;

                                    if (s.Channeled)
                                    {
                                        AddStatus(s.GetType().Name, s.Rank, duration, this, target);
                                        ++count;
                                    }
                                    else
                                    {
                                        if (Confused && s.Unfriendly && (Program.Random(2) == 0))
                                            target = this;

                                        if (s.HasStatus && target.Statuses.ContainsKey(s.StatusName))
                                            return;

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

                                            if (target is Player)
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
                if (hidden && Hidden)
                    RemoveStatus("Hidden");
                if (s.BodyAnimation != 0)
                    BodyAnimation(s.BodyAnimation, 40);
                s.KeepMana = false;
            }

            if (cooldown)
            {
                Cooldown(s);
            }
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

            int manaCost = (int)(BaseMaximumMP * s.Ranks[s.Rank - 1].ManaPercentage) + s.Ranks[s.Rank - 1].ManaCost;

            if (CurrentMP < manaCost)
            {
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

                                            if (AllegianceTo(c) == Allegiance.Friendly && !s.NpcCanUseOnAlly)
                                                continue;

                                            if (AllegianceTo(c) == Allegiance.Enemy && !s.NpcCanUseOnEnemy)
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

                                            if (c is Player)
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

                                    if (s.Target is Player)
                                    {
                                        if (s.Unfriendly)
                                        {
                                            (s.Target as Player).Client.SendMessage(string.Format("{0} attacks you with {1}.",
                                                Name, s.Name));
                                        }
                                        else
                                        {
                                            (s.Target as Player).Client.SendMessage(string.Format("{0} casts {1} on you.",
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
            }
        }

        public void Cooldown(Skill s)
        {
            Cooldown(s, s.CooldownLength);
        }
        public void Cooldown(Skill s, double length)
        {
            s.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);
        }

        public void Cooldown(Spell s)
        {
            Cooldown(s, s.CooldownLength);
        }
        public void Cooldown(Spell s, double length)
        {
            s.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);

            if (!string.IsNullOrEmpty(s.CooldownFamily))
            {
                foreach (var t in SpellBook)
                {
                    if ((t != null) && (t.CooldownFamily == s.CooldownFamily))
                    {
                        t.NextAvailableUse = DateTime.UtcNow.AddSeconds(length);
                    }
                }
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
                return Allegiance.Enemy;
            }
            else
            {
                if (Enemies.Contains(c) || c.Enemies.Contains(this))
                    return Allegiance.Enemy;

                var player = c as Player;

                if (player != null)
                {
                    if (GameServer.Factions.ContainsKey(Faction))
                    {
                        if (player.AllegianceTable.ContainsKey(Faction))
                            return player.AllegianceTable[Faction];
                        else
                            return GameServer.Factions[Faction].PlayerDefault;
                    }
                }
                else
                {
                    if (Faction == c.Faction)
                        return Allegiance.Friendly;

                    if (GameServer.Factions.ContainsKey(Faction))
                    {
                        var faction = GameServer.Factions[Faction];
                        if (faction.AllegianceTable.ContainsKey(c.Faction))
                            return faction.AllegianceTable[c.Faction];
                    }
                }
            }

            return Allegiance.Neutral;
        }

        public void NexonPathfind(Point pt)
        {
            int offsetX = (Point.X - pt.X);
            int offsetY = (Point.Y - pt.Y);

            if (Math.Abs(offsetX) > Math.Abs(offsetY))
            {
                if (offsetX > 0)
                {
                    if (Walk(Direction.West)) return;
                    if (Walk(Direction.North)) return;
                    if (Walk(Direction.East)) return;
                    if (Walk(Direction.South)) return;
                }

                if (offsetX < 0)
                {
                    if (Walk(Direction.East)) return;
                    if (Walk(Direction.South)) return;
                    if (Walk(Direction.West)) return;
                    if (Walk(Direction.North)) return;
                }
            }

            if (Math.Abs(offsetX) < Math.Abs(offsetY))
            {
                if (offsetY > 0)
                {
                    if (Walk(Direction.North)) return;
                    if (Walk(Direction.East)) return;
                    if (Walk(Direction.South)) return;
                    if (Walk(Direction.West)) return;
                }

                if (offsetY < 0)
                {
                    if (Walk(Direction.South)) return;
                    if (Walk(Direction.West)) return;
                    if (Walk(Direction.North)) return;
                    if (Walk(Direction.East)) return;
                }
            }

            if (offsetY > 0)
            {
                if (Walk(Direction.North)) return;
                if (Walk(Direction.East)) return;
                if (Walk(Direction.South)) return;
                if (Walk(Direction.West)) return;
            }

            if (offsetX < 0)
            {
                if (Walk(Direction.East)) return;
                if (Walk(Direction.South)) return;
                if (Walk(Direction.West)) return;
                if (Walk(Direction.North)) return;
            }

            if (offsetY < 0)
            {
                if (Walk(Direction.South)) return;
                if (Walk(Direction.West)) return;
                if (Walk(Direction.North)) return;
                if (Walk(Direction.East)) return;
            }

            if (offsetX > 0)
            {
                if (Walk(Direction.West)) return;
                if (Walk(Direction.North)) return;
                if (Walk(Direction.East)) return;
                if (Walk(Direction.South)) return;
            }
        }
        public bool Pathfind(Point pt)
        {
            return Pathfind(pt, 1, false);
        }
        public bool Pathfind(Point pt, int distance)
        {
            return Pathfind(pt, distance, false);
        }
        public bool Pathfind(Point pt, int distance, bool ignoreUnits)
        {
            int errors = -1;

            if (pt.DistanceFrom(Point) <= distance)
                return false;

            if (PathEndPoint != pt)
            {
                PathEndPoint = pt;
                PathStep = Path.Length;
            }

            while (++errors < 6)
            {
                if (Path.Length <= PathStep)
                {
                    Path = Map.FindPath(Point, pt, ignoreUnits);
                    PathStep = 0;
                    continue;
                }

                var nextPoint = Path[PathStep++];
                var lastPoint = Path[Path.Length - 1];

                if (nextPoint.DistanceFrom(Point) != 1)
                {
                    Path = Map.FindPath(Point, pt, ignoreUnits);
                    PathStep = 0;
                    continue;
                }

                if (nextPoint.X > Map.Width || nextPoint.Y > Map.Height)
                {
                    Path = Map.FindPath(Point, pt, ignoreUnits);
                    PathStep = 0;
                    continue;
                }

                if (Map.Walls[nextPoint.X, nextPoint.Y] && !CanWalkThroughWalls)
                {
                    Path = Map.FindPath(Point, pt, ignoreUnits);
                    PathStep = 0;
                    continue;
                }

                if (Map.Tiles[nextPoint.X, nextPoint.Y].Weight > 0 && !CanWalkThroughUnits)
                {
                    Path = Map.FindPath(Point, pt, ignoreUnits);
                    PathStep = 0;
                    continue;
                }

                return Walk(Point.Offset(nextPoint));
            }

            return false;
        }

        public override bool Walk(Direction direction)
        {
            return Walk(direction, false);
        }
        public override bool Walk(Direction direction, bool forcefully)
        {
            int newXOffset = 0, newYOffset = 0;
            Point oldPoint = new Point(Point.X, Point.Y);
            Point newPoint = new Point(Point.X, Point.Y);

            if (Dizzy && direction != Direction)
                direction = Direction;

            switch (direction)
            {
                case Direction.North: newPoint.Y--; newXOffset = 0; newYOffset = -1; break;
                case Direction.South: newPoint.Y++; newXOffset = 0; newYOffset = 1; break;
                case Direction.West: newPoint.X--; newXOffset = -1; newYOffset = 0; break;
                case Direction.East: newPoint.X++; newXOffset = 1; newYOffset = 0; break;
                default: return false;
            }

            if (SpawnControl != null && SpawnControl.RegionType == SpawnRegion.Rectangle)
            {
                if (!SpawnControl.CanWalkOutsideRectangle && (Target == null || !SpawnControl.CanFollowOutsideRectangle))
                {
                    if (newPoint.X < SpawnControl.X || newPoint.X > SpawnControl.X + SpawnControl.Right)
                        return false;
                    if (newPoint.Y < SpawnControl.Y || newPoint.Y > SpawnControl.Y + SpawnControl.Bottom)
                        return false;
                }
            }

            if (!forcefully && (Sleeping || Paralyzed || Frozen || Coma || MindControlled || Polymorphed))
                return false;

            if ((newPoint.X < 0) || (newPoint.Y < 0) || (newPoint.X >= Map.Width) || (newPoint.Y >= Map.Height))
                return false;

            if (Map.Walls[newPoint.X, newPoint.Y] && !CanWalkThroughWalls)
                return false;

            if (Map[newPoint.X, newPoint.Y].Weight > 0 && !CanWalkThroughUnits)
                return false;

            if ((Map.Warps[newPoint.X, newPoint.Y] != null) && (!Map.Warps[newPoint.X, newPoint.Y].NpcPassable))
                return false;

            Map[oldPoint.X, oldPoint.Y].Objects.Remove(this);
            Point = newPoint;
            Direction = direction;
            XOffset = newXOffset;
            YOffset = newYOffset;
            Map[newPoint.X, newPoint.Y].Objects.Insert(0, this);

            foreach (var c in Map.Objects)
            {
                if (c != this)
                {
                    if (c.Point.DistanceFrom(oldPoint) > 12 && c.Point.DistanceFrom(newPoint) <= 12)
                    {
                        DisplayTo(c);
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
                    }
                }
            }

            foreach (var r in Map.Tiles[newPoint.X, newPoint.Y].Objects)
            {
                var reactor = r as Reactor;
                if (reactor != null && reactor.Alive)
                {
                    reactor.OnWalkover(this);
                    break;
                }
            }

            if (Map.Warps[newPoint.X, newPoint.Y] != null)
            {
                var mapWarp = Map.Warps[newPoint.X, newPoint.Y];
                if (GameServer.MapDatabase.ContainsKey(mapWarp.MapName))
                {
                    Map.RemoveCharacter(this);
                    GameServer.MapDatabase[mapWarp.MapName].InsertCharacter(this, mapWarp.Point.X, mapWarp.Point.Y);
                }
            }

            return true;
        }
        public override void Damage(double dmg, Character attacker = null, int sound = 255, DamageType damageType = DamageType.RawDamage, DamageFlags flags = DamageFlags.None)
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
                attacker.Threaten(this, dmg);
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

            OnDamaged(attacker, realDamage);

            double percent = Math.Floor((double)CurrentHP / (double)MaximumHP * 100.0);

            if (percent < 0)
                percent = 0;

            if (percent > 100)
                percent = 100;

            var dot = (flags & DamageFlags.DamageOverTime) == DamageFlags.DamageOverTime;
            var id = (attacker != null) ? attacker.ID : ID;

            foreach (var c in Map.Objects)
            {
                if (WithinRange(c, 12) && c is Player)
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
    }
}