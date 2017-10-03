using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Wewladh
{
    public class GameServer : Server
    {
        public int Index { get; set; }
        public LoginServer LoginServer { get; private set; }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public string DataPath { get; private set; }

        public string StartMap { get; private set; }
        public string DeathMap { get; private set; }
        public Point StartPoint { get; private set; }
        public Point DeathPoint { get; private set; }
        public bool AllowLogin { get; private set; }
        public bool AllowCreate { get; private set; }

        public HashSet<string> RealmFirsts { get; private set; }

        public GlobalScript GlobalScript { get; private set; }
        public Dictionary<string, MethodInfo> CompiledMethods { get; private set; }

        public HashSet<GameObject> GameObjects { get; private set; }
        public Dictionary<uint, GameObject> GameObjectsID { get; private set; }
        public Dictionary<long, GameObject> GameObjectsGUID { get; private set; }

        public T GameObject<T>(uint id) where T : GameObject
        {
            if (GameObjectsID.ContainsKey(id) && GameObjectsID[id] is T)
                return (T)GameObjectsID[id];
            return null;
        }
        public T GameObject<T>(long guid) where T : GameObject
        {
            if (GameObjectsGUID.ContainsKey(guid) && GameObjectsGUID[guid] is T)
                return (T)GameObjectsGUID[guid];
            return null;
        }

        public HostedArena HostedArena { get; private set; }
        public Dictionary<string, Guild> Guilds { get; private set; }
        public int[] ExperienceTable { get; private set; }
        public int[] AbilityExpTable { get; private set; }

        private DateTime lastSave;

        private uint nextGameObjectID = 1;
        private long nextGameObjectGUID = 1;

        private HashSet<string> secureKeys = new HashSet<string>();

        #region Time
        private static Time[] time = new Time[]
        {
            Time.Dawn, Time.Dawn, Time.Dawn,
            Time.Dusk, Time.Dusk, Time.Dusk,
            Time.Dawn, Time.Dawn, Time.Dawn,
            Time.Dusk, Time.Dusk, Time.Dusk,
            Time.Dawn, Time.Dawn, Time.Dawn,
            Time.Dusk, Time.Dusk, Time.Dusk,
            Time.Dawn, Time.Dawn, Time.Dawn,
            Time.Dusk, Time.Dusk, Time.Dusk
        };
        public Time Time
        {
            get { return time[DateTime.UtcNow.Hour]; }
        }
        #endregion

        #region Object Dictionaries
        public Dictionary<string, Map> MapDatabase { get; private set; }
        public Dictionary<string, Metafile> MetafileDatabase { get; private set; }
        public List<Title> TitleDatabase { get; private set; }
        public Dictionary<string, Nation> NationDatabase { get; private set; }
        public Dictionary<int, MiniGame> MiniGameDatabase { get; private set; }
        public Dictionary<string, Faction> Factions { get; private set; }

        public Dictionary<string, Type> NpcTypes { get; private set; }
        public Dictionary<string, Type> ItemTypes { get; private set; }
        public Dictionary<string, Type> ReactorTypes { get; private set; }
        public SortedDictionary<string, Type> SkillTypes { get; private set; }
        public SortedDictionary<string, Type> SpellTypes { get; private set; }
        public Dictionary<string, Type> LegendMarkTypes { get; private set; }
        public Dictionary<string, Type> QuestTypes { get; private set; }
        public Dictionary<string, Type> QuestFamilyTypes { get; private set; }
        public Dictionary<string, Type> DialogMenuOptionTypes { get; private set; }
        public Dictionary<string, Type> ManufactureTypes { get; private set; }

        public Dictionary<string, Monster> NpcDatabase { get; private set; }
        public Dictionary<string, Item> ItemDatabase { get; private set; }
        public Dictionary<string, Reactor> ReactorDatabase { get; private set; }
        public SortedDictionary<string, Skill> SkillDatabase { get; private set; }
        public SortedDictionary<string, Spell> SpellDatabase { get; private set; }
        public Dictionary<string, LegendMark> LegendMarkDatabase { get; private set; }
        public Dictionary<string, Quest> QuestDatabase { get; private set; }
        public Dictionary<string, QuestFamily> QuestFamilyDatabase { get; private set; }
        public Dictionary<string, DialogMenuOption> DialogMenuOptionDatabase { get; private set; }
        public Dictionary<string, Manufacture> ManufactureDatabase { get; private set; }
        #endregion

        public GameServer(string dataPath)
        {
            DataPath = dataPath;

            StartPoint = new Point(0, 0);
            DeathPoint = new Point(0, 0);

            XDocument doc = XDocument.Load(dataPath + "\\config.xml");
            Name = doc.Element("config").Element("name").Value;
            Description = doc.Element("config").Element("description").Value;
            int gamePort = (int)doc.Element("config").Element("gameport");
            int loginPort = (int)doc.Element("config").Element("loginport");

            StartMap = doc.Element("config").Element("startpoint").Attribute("map").Value;
            StartPoint.X = (int)doc.Element("config").Element("startpoint").Attribute("x");
            StartPoint.Y = (int)doc.Element("config").Element("startpoint").Attribute("y");

            DeathMap = doc.Element("config").Element("deathpoint").Attribute("map").Value;
            DeathPoint.X = (int)doc.Element("config").Element("deathpoint").Attribute("x");
            DeathPoint.Y = (int)doc.Element("config").Element("deathpoint").Attribute("y");

            AllowLogin = (bool)doc.Element("config").Element("allowlogin");
            AllowCreate = (bool)doc.Element("config").Element("allowcreate");

            ExperienceTable = new int[256];
            AbilityExpTable = new int[256];

            var tnlFile = new StreamReader(dataPath + "\\tnl.txt");
            while (!tnlFile.EndOfStream)
            {
                var line = tnlFile.ReadLine().Split('\t');
                if (line.Length == 2)
                {
                    int exp, level;
                    if (int.TryParse(line[0], out level) && int.TryParse(line[1], out exp) && level < ExperienceTable.Length)
                        ExperienceTable[level] = exp;
                }
            }

            var tnaFile = new StreamReader(dataPath + "\\tna.txt");
            while (!tnaFile.EndOfStream)
            {
                var line = tnaFile.ReadLine().Split('\t');
                if (line.Length == 2)
                {
                    int exp, level;
                    if (int.TryParse(line[0], out level) && int.TryParse(line[1], out exp) && level < AbilityExpTable.Length)
                        AbilityExpTable[level] = exp;
                }
            }

            Program.WriteLine("[LOADING GAME SERVER <{0}>]", Name);

            IPHostEntry entry = Dns.GetHostEntry(Program.HostName);
            if (entry.AddressList.Length > 0)
            {
                EndPoint = new IPEndPoint(entry.AddressList[0], gamePort);
            }

            if (!Directory.Exists(dataPath + "\\maps"))
                Directory.CreateDirectory(dataPath + "\\maps");
            if (!Directory.Exists(dataPath + "\\metafiles"))
                Directory.CreateDirectory(dataPath + "\\metafiles");
            if (!Directory.Exists(dataPath + "\\Scripts"))
                Directory.CreateDirectory(dataPath + "\\Scripts");

            LoginServer = new LoginServer(this, loginPort);

            GameObjects = new HashSet<GameObject>();
            GameObjectsID = new Dictionary<uint, GameObject>();

            HostedArena = new HostedArena();
            Guilds = new Dictionary<string, Guild>(StringComparer.CurrentCultureIgnoreCase);

            RealmFirsts = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            CompiledMethods = new Dictionary<string, MethodInfo>(StringComparer.CurrentCultureIgnoreCase);

            MapDatabase = new Dictionary<string, Map>(StringComparer.CurrentCultureIgnoreCase);
            MetafileDatabase = new Dictionary<string, Metafile>(StringComparer.CurrentCultureIgnoreCase);
            TitleDatabase = new List<Title>();
            NationDatabase = new Dictionary<string, Nation>(StringComparer.CurrentCultureIgnoreCase);
            MiniGameDatabase = new Dictionary<int, MiniGame>();
            Factions = new Dictionary<string, Faction>();

            NpcTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            ItemTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            ReactorTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            SkillTypes = new SortedDictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            SpellTypes = new SortedDictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            LegendMarkTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            QuestTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            QuestFamilyTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            DialogMenuOptionTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
            ManufactureTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

            NpcDatabase = new Dictionary<string, Monster>(StringComparer.CurrentCultureIgnoreCase);
            ItemDatabase = new Dictionary<string, Item>(StringComparer.CurrentCultureIgnoreCase);
            ReactorDatabase = new Dictionary<string, Reactor>(StringComparer.CurrentCultureIgnoreCase);
            SkillDatabase = new SortedDictionary<string, Skill>(StringComparer.CurrentCultureIgnoreCase);
            SpellDatabase = new SortedDictionary<string, Spell>(StringComparer.CurrentCultureIgnoreCase);
            LegendMarkDatabase = new Dictionary<string, LegendMark>(StringComparer.CurrentCultureIgnoreCase);
            QuestDatabase = new Dictionary<string, Quest>(StringComparer.CurrentCultureIgnoreCase);
            QuestFamilyDatabase = new Dictionary<string, QuestFamily>(StringComparer.CurrentCultureIgnoreCase);
            DialogMenuOptionDatabase = new Dictionary<string, DialogMenuOption>(StringComparer.CurrentCultureIgnoreCase);
            ManufactureDatabase = new Dictionary<string, Manufacture>(StringComparer.CurrentCultureIgnoreCase);

            var ids = new List<int>();

            var acctGold = new Dictionary<int, long>();

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT character_id, creation_date FROM characters WHERE (acct_id = 0)";
            var reader = com.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var dt = DateTime.Parse(reader.GetString(1));
                if (DateTime.UtcNow.Subtract(dt).TotalDays > 3)
                    ids.Add(id);
            }
            reader.Close();

            foreach (var id in ids)
            {
                com = Program.MySqlConnection.CreateCommand();
                com.CommandText = "DELETE FROM characters WHERE (character_id = @character_id)";
                com.Parameters.AddWithValue("@character_id", id);
                com.ExecuteNonQuery();
            }

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT id FROM realm_firsts";
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetString(0);
                RealmFirsts.Add(id);
            }
            reader.Close();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT name, leader, members, council FROM guilds";
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var leader = reader.GetString(1);
                var members = reader.GetString(2);
                var council = reader.GetString(3);
                var guild = new Guild(name, leader);
                foreach (var m in members.Split(';'))
                    guild.Members.Add(m);
                foreach (var c in council.Split(';'))
                    guild.Council.Add(c);
                Guilds.Add(name, guild);
            }
            reader.Close();

            LoadScripts();
            LoadMetafileDatabase();
        }

        public override void Shutdown()
        {
            listener.Stop();

            var clients = new Client[Clients.Count];
            Clients.CopyTo(clients);
            foreach (Client client in clients)
            {
                if ((client != null) && (client.Player != null))
                {
                    if (client.Player.ExchangeInfo != null)
                        client.Player.CancelExchange();
                    client.Connected = false;
                }
            }
        }

        protected override void LoadPacketHandlers()
        {
            MessageHandlers[0x05] = MsgHandler_RequestMap;
            MessageHandlers[0x06] = MsgHandler_Walk;
            MessageHandlers[0x07] = MsgHandler_PickupItem;
            MessageHandlers[0x08] = MsgHandler_DropItem;
            MessageHandlers[0x0B] = MsgHandler_Logoff;
            MessageHandlers[0x0C] = MsgHandler_RequestDisplay;
            MessageHandlers[0x0E] = MsgHandler_Chat;
            MessageHandlers[0x0F] = MsgHandler_UseSpell;
            MessageHandlers[0x10] = MsgHandler_ClientJoin;
            MessageHandlers[0x11] = MsgHandler_Turn;
            MessageHandlers[0x13] = MsgHandler_Spacebar;
            MessageHandlers[0x16] = MsgHandler_RequestBag;
            MessageHandlers[0x18] = MsgHandler_Userlist;
            MessageHandlers[0x19] = MsgHandler_PrivateMessage;
            MessageHandlers[0x1C] = MsgHandler_UseItem;
            MessageHandlers[0x1D] = MsgHandler_Emotion;
            MessageHandlers[0x23] = MsgHandler_Paper;
            MessageHandlers[0x24] = MsgHandler_DropGold;
            MessageHandlers[0x29] = MsgHandler_ExchangeItem;
            MessageHandlers[0x2A] = MsgHandler_ExchangeGold;
            MessageHandlers[0x2D] = MsgHandler_RequestProfile;
            MessageHandlers[0x2E] = MsgHandler_Group;
            MessageHandlers[0x2F] = MsgHandler_ToggleGroup;
            MessageHandlers[0x30] = MsgHandler_MoveItemSkillSpell;
            MessageHandlers[0x38] = MsgHandler_Refresh;
            MessageHandlers[0x39] = MsgHandler_RequestDialog;
            MessageHandlers[0x3A] = MsgHandler_DialogResponse;
            MessageHandlers[0x3B] = MsgHandler_MessageBoards;
            MessageHandlers[0x3E] = MsgHandler_UseSkill;
            MessageHandlers[0x3F] = MsgHandler_WorldMap;
            MessageHandlers[0x43] = MsgHandler_CharacterClicked;
            MessageHandlers[0x44] = MsgHandler_RemoveEquipment;
            MessageHandlers[0x47] = MsgHandler_UseStatPoint;
            MessageHandlers[0x4A] = MsgHandler_Exchange;
            MessageHandlers[0x4D] = MsgHandler_StartSpellCast;
            MessageHandlers[0x4E] = MsgHandler_Chant;
            MessageHandlers[0x55] = MsgHandler_Manufacture;
            MessageHandlers[0x5A] = MsgHandler_Title;
            MessageHandlers[0x6A] = MsgHandler_MiniGame;
            MessageHandlers[0x72] = MsgHandler_Bag;
            MessageHandlers[0x73] = MsgHandler_RequestWebsite;
            MessageHandlers[0x79] = MsgHandler_PlayerStatus;
            MessageHandlers[0x88] = MsgHandler_RequestProfile;
            MessageHandlers[0x89] = MsgHandler_DisplayBitmask;
            MessageHandlers[0x8D] = MsgHandler_Quest;
        }

        private void LoadScripts()
        {
            string[] scriptPaths = Directory.GetFiles(DataPath + "\\Scripts", "*.cs", SearchOption.AllDirectories);

            Program.Write("Compiling scripts... ");
            Assembly asm = Program.Compile(scriptPaths);
            Program.WriteLine("{0} script file{1} compiled!", scriptPaths.Length, (scriptPaths.Length == 1 ? "" : "s"));

            Program.Write("Loading game objects... ");

            TitleDatabase.Add(new Title_Null());

            foreach (var type in asm.GetTypes())
            {
                if (type.IsSubclassOf(typeof(GlobalScript)))
                {
                    GlobalScript = (GlobalScript)Activator.CreateInstance(type);
                }
                else if (type.IsSubclassOf(typeof(Script)))
                {
                    foreach (var method in type.GetMethods())
                    {
                        CompiledMethods.Add(method.Name, method);
                    }
                }
                else if (type.IsSubclassOf(typeof(Map)))
                {
                    var map = (Map)Activator.CreateInstance(type);
                    if (map.Initialize(this))
                    {
                        if (map.Number == 0)
                            map.Number = MapDatabase.Count + 30000;
                        MapDatabase.Add(type.Name, map);
                    }
                }
                else if (type.IsSubclassOf(typeof(Title)))
                {
                    TitleDatabase.Add((Title)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Faction)))
                {
                    var faction = (Faction)Activator.CreateInstance(type);
                    Factions.Add(type.Name, faction);
                }
                else if (type.IsSubclassOf(typeof(MiniGame)))
                {
                    var minigame = (MiniGame)Activator.CreateInstance(type);
                    MiniGameDatabase.Add(minigame.ID, minigame);
                }
                else if (type.IsSubclassOf(typeof(DialogMenuOption)))
                {
                    DialogMenuOptionTypes.Add(type.Name, type);
                    DialogMenuOptionDatabase.Add(type.Name, (DialogMenuOption)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Manufacture)))
                {
                    ManufactureTypes.Add(type.Name, type);
                    var m = (Manufacture)Activator.CreateInstance(type);
                    ManufactureDatabase.Add(type.Name, m);
                }
                else if (type.IsSubclassOf(typeof(Quest)))
                {
                    QuestTypes.Add(type.Name, type);
                    QuestDatabase.Add(type.Name, (Quest)Activator.CreateInstance(type));
                    QuestDatabase[type.Name].ID = QuestDatabase.Count;
                }
                else if (type.IsSubclassOf(typeof(Nation)))
                {
                    NationDatabase.Add(type.Name, (Nation)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(QuestFamily)))
                {
                    QuestFamilyTypes.Add(type.Name, type);
                    QuestFamilyDatabase.Add(type.Name, (QuestFamily)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Item)))
                {
                    ItemTypes.Add(type.Name, type);
                    ItemDatabase.Add(type.Name, (Item)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Reactor)))
                {
                    ReactorTypes.Add(type.Name, type);
                    ReactorDatabase.Add(type.Name, (Reactor)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Monster)))
                {
                    NpcTypes.Add(type.Name, type);
                    NpcDatabase.Add(type.Name, (Monster)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Skill)))
                {
                    SkillTypes.Add(type.Name, type);
                    SkillDatabase.Add(type.Name, (Skill)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(Spell)))
                {
                    SpellTypes.Add(type.Name, type);
                    SpellDatabase.Add(type.Name, (Spell)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(LegendMark)))
                {
                    LegendMarkTypes.Add(type.Name, type);
                    LegendMarkDatabase.Add(type.Name, (LegendMark)Activator.CreateInstance(type));
                }
            }

            TitleDatabase.Sort((x, y) => x.ID.CompareTo(y.ID));

            Program.WriteLine("all objects loaded!");
        }
        private void LoadMetafileDatabase()
        {
            var CharicInfo0 = new Metafile("CharicInfo0");
            foreach (var item in ItemDatabase.Values)
            {
                var me = new Metafile.Element(item.GetType().Name);
                me.Properties.Add(item.Name);
                me.Properties.Add(item.Weight.ToString());
                me.Properties.Add(item.Value.ToString());
                me.Properties.Add("1");
                me.Properties.Add("0");
                me.Properties.Add("0");
                me.Properties.Add("0");
                me.Properties.Add(item.MaximumDurability.ToString());
                me.Properties.Add(item.ArmorClassMod.ToString());
                me.Properties.Add(item.HitMod.ToString());
                me.Properties.Add(item.DmgMod.ToString());
                me.Properties.Add(item.MaximumHpMod.ToString());
                me.Properties.Add(item.MaximumMpMod.ToString());
                me.Properties.Add(item.StrMod.ToString());
                me.Properties.Add(item.DexMod.ToString());
                me.Properties.Add(item.IntMod.ToString());
                me.Properties.Add(item.WisMod.ToString());
                me.Properties.Add(item.ConMod.ToString());
                me.Properties.Add(((int)item.Class).ToString());
                me.Properties.Add("0");
                me.Properties.Add(item.RequiredLevel.ToString());
                me.Properties.Add("0");
                me.Properties.Add("1");
                if (item is Weapon)
                {
                    me.Properties.Add(item.MinimumAttackPowerMod + "m" + item.MaximumAttackPowerMod);
                    me.Properties.Add(item.MinimumMagicPowerMod + "m" + item.MaximumMagicPowerMod);
                }
                else
                {
                    me.Properties.Add("0");
                    me.Properties.Add("0");
                }
                me.Properties.Add("2");
                CharicInfo0.Elements.Add(me);
            }
            MetafileDatabase.Add(CharicInfo0.Name, CharicInfo0);
            CharicInfo0.Write(DataPath);

            var Titles = new Metafile("Titles");
            foreach (var title in TitleDatabase)
            {
                Titles.Elements.Add(new Metafile.Element(title.ID.ToString(), title.Name, title.Description));
            }
            MetafileDatabase.Add(Titles.Name, Titles);
            Titles.Write(DataPath);

            var UsualURLs = new Metafile("UsualURLs");
            UsualURLs.Elements.Add(new Metafile.Element("CharacterAdminURL", "http://www.nexon.com/nx/Page/Gnx.aspx?URL=Help/GameIDManage"));
            UsualURLs.Elements.Add(new Metafile.Element("NexonClubNewURL", "https://www.nexon.com/join/page/nx.aspx?url=join/join&codeRegSite=161"));
            UsualURLs.Elements.Add(new Metafile.Element("TMapDownLoadURL", "http://www.wewladh.com/TownMap/"));
            MetafileDatabase.Add(UsualURLs.Name, UsualURLs);
            UsualURLs.Write(DataPath);

            var Options = new Metafile("Options");
            Options.Elements.Add(new Metafile.Element("itemShopUrl", "http://lod.nexon.com/", "http://shop.lod.nexon.com"));
            Options.Elements.Add(new Metafile.Element("nexonIdRegisterUrl", "https://www.nexon.com/join/page/nx.aspx?url=join/join&codeRegSite=161"));
            Options.Elements.Add(new Metafile.Element("PasswordChangeNotifyUrl", "http://login.lod.nexon.com/Soap/SendMessage.aspx"));
            Options.Elements.Add(new Metafile.Element("AgeAuthUrl", "http://classicrpgcharacter.nexon.com/service/classicrpgcheckage.aspx?gameid=%s&servercode=%d&gametype=%d"));
            Options.Elements.Add(new Metafile.Element("AgeCheckEnable", "0"));
            Options.Elements.Add(new Metafile.Element("NexonAuthEnable", "1"));
            Options.Elements.Add(new Metafile.Element("UserNumDisplayMode", "1"));
            Options.Elements.Add(new Metafile.Element("UserNumFactor", "10"));
            MetafileDatabase.Add(Options.Name, Options);
            Options.Write(DataPath);

            var Light = new Metafile("Light");
            Light.Elements.Add(new Metafile.Element("Default_0", "0", "1", "18", "6", "11", "60"));
            Light.Elements.Add(new Metafile.Element("Default_1", "2", "3", "20", "100", "10", "100"));
            Light.Elements.Add(new Metafile.Element("Default_2", "4", "5", "23", "170", "36", "50"));
            Light.Elements.Add(new Metafile.Element("Default_3", "6", "7", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_4", "8", "9", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_5", "10", "11", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_6", "12", "13", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_7", "14", "15", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_8", "16", "18", "32", "0", "0", "255"));
            Light.Elements.Add(new Metafile.Element("Default_9", "19", "20", "26", "170", "36", "50"));
            Light.Elements.Add(new Metafile.Element("Default_a", "21", "22", "23", "100", "10", "100"));
            Light.Elements.Add(new Metafile.Element("Default_b", "23", "24", "20", "27", "1", "59"));
            foreach (var map in MapDatabase.Values)
            {
                if ((map.Flags & MapFlags.HasDayNight) == MapFlags.HasDayNight)
                {
                    var me = new Metafile.Element(map.Number.ToString());
                    me.Properties.Add("Default");
                    Light.Elements.Add(me);
                }
            }
            MetafileDatabase.Add(Light.Name, Light);
            Light.Write(DataPath);

            var NationDesc = new Metafile("NationDesc");
            foreach (var nation in NationDatabase.Values)
            {
                var title = "nation_" + nation.Flag;
                if (NationDesc.Elements.TrueForAll(el => el.Text != title))
                {
                    var me = new Metafile.Element(title, nation.Name);
                    NationDesc.Elements.Add(me);
                }
            }
            MetafileDatabase.Add(NationDesc.Name, NationDesc);
            NationDesc.Write(DataPath);

            var NPCIllust = new Metafile("NPCIllust");
            foreach (var npc in NpcDatabase.Values)
            {
                if (npc is Merchant)
                {
                    var merchant = npc as Merchant;
                    if (!string.IsNullOrEmpty(merchant.Portrait))
                    {
                        var me = new Metafile.Element(merchant.Name);
                        me.Properties.Add(merchant.Portrait);
                        NPCIllust.Elements.Add(me);
                    }
                }
            }
            MetafileDatabase.Add(NPCIllust.Name, NPCIllust);
            NPCIllust.Write(DataPath);

            var ItemHelp = new Metafile("ItemHelp");
            foreach (var item in ItemDatabase.Values)
            {
                var me = new Metafile.Element(item.Name);

                if (string.IsNullOrEmpty(item.ItemInfoCategory))
                    me.Properties.Add(item.GetType().BaseType.Name);
                else
                    me.Properties.Add(item.ItemInfoCategory);

                if (string.IsNullOrEmpty(item.ItemInfoDescription))
                    me.Properties.Add("No information available.");
                else
                    me.Properties.Add(item.ItemInfoDescription);

                ItemHelp.Elements.Add(me);
            }
            MetafileDatabase.Add(ItemHelp.Name, ItemHelp);
            ItemHelp.Write(DataPath);

            var TMap1 = new Metafile("TMap1");
            foreach (var map in MapDatabase.Values)
            {
                var me = new Metafile.Element(map.Number.ToString());
                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        if (map.Warps[x, y] != null)
                        {
                            var warp = map.Warps[x, y];
                            if (MapDatabase.ContainsKey(warp.MapName))
                            {
                                var name = MapDatabase[warp.MapName].Name;
                                me.Properties.Add(string.Format("{0} {1} {2} {3} {4} \"0\"",
                                    name.Replace(" ", string.Empty), x, y, x, y));
                            }
                        }
                    }
                }
                me.Properties.Add("\n");
                TMap1.Elements.Add(me);
            }
            MetafileDatabase.Add(TMap1.Name, TMap1);
            TMap1.Write(DataPath);

            var ItemInfo0 = new Metafile("ItemInfo0");
            foreach (var item in ItemDatabase.Values)
            {
                var me = new Metafile.Element(item.GetType().Name);
                me.Properties.Add(item.Name);
                me.Properties.Add("0");
                me.Properties.Add(item.Value.ToString());
                me.Properties.Add("0");
                me.Properties.Add("0");
                me.Properties.Add("0");
                me.Properties.Add("0");
                ItemInfo0.Elements.Add(me);
            }
            MetafileDatabase.Add(ItemInfo0.Name, ItemInfo0);
            ItemInfo0.Write(DataPath);

            var ItemDesc0 = new Metafile("ItemDesc0");
            foreach (var item in ItemDatabase.Values)
            {
                var me = new Metafile.Element(item.GetType().Name);
                me.Properties.Add(item.Sprite.ToString());
                me.Properties.Add(item.Description ?? string.Empty);
                ItemDesc0.Elements.Add(me);
            }
            MetafileDatabase.Add(ItemDesc0.Name, ItemDesc0);
            ItemDesc0.Write(DataPath);

            var SkillSpellDesc = new Metafile("SkillSpellDesc");
            foreach (var skill in SkillDatabase.Values)
            {
                if (!string.IsNullOrEmpty(skill.GuiDescription))
                {
                    var me = new Metafile.Element(skill.Name);
                    me.Properties.Add(skill.GuiDescription);
                    SkillSpellDesc.Elements.Add(me);
                }
            }
            MetafileDatabase.Add(SkillSpellDesc.Name, SkillSpellDesc);
            SkillSpellDesc.Write(DataPath);

            var Quest1_32 = new Metafile("Quest1_32"); 
            foreach (var quest in QuestDatabase.Values)
            {
                var el = new Metafile.Element(quest.ID.ToString());
                el.Properties.Add(quest.Name);
                el.Properties.Add(quest.Steps.Count.ToString());
                el.Properties.Add("0");
                el.Properties.Add(quest.Description);
                Quest1_32.Elements.Add(el);

                for (int i = 0; i < quest.Steps.Count; i++)
                {
                    var step = quest.Steps[i];
                    el = new Metafile.Element(quest.ID + "_" + (i + 1));
                    el.Properties.Add("DESC_STR1");

                    for (int j = 0; j < step.NpcLocations.Count; j++)
                    {
                        el.Properties.Add("TMAP_NPC" + j);
                    }

                    int objIndex = 0;
                    foreach (var qo in step.Objectives.Values)
                    {
                        switch (qo.Type)
                        {
                            case QuestObjectiveType.Item: el.Properties.Add("ITEM" + objIndex); break;
                            case QuestObjectiveType.Skill: el.Properties.Add("SKILL" + objIndex); break;
                            case QuestObjectiveType.Spell: el.Properties.Add("SPELL" + objIndex); break;
                            case QuestObjectiveType.Kill: el.Properties.Add("KILLED" + objIndex); break;
                            case QuestObjectiveType.Misc: el.Properties.Add("KILLED" + objIndex); break;
                            default: continue;
                        }
                        objIndex++;
                    }

                    if (step.Reward != string.Empty)
                        el.Properties.Add("REQUITAL0");

                    Quest1_32.Elements.Add(el);

                    el = new Metafile.Element(quest.ID + "_" + (i + 1) + "_DESC_STR1");
                    el.Properties.Add(step.Description);
                    Quest1_32.Elements.Add(el);

                    for (int j = 0; j < step.NpcLocations.Count; j++)
                    {
                        string baseString = quest.ID + "_" + (i + 1) + "_";
                        var npcData = step.NpcLocations[i].Split(',');
                        el = new Metafile.Element(baseString + "TMAP_NPC" + j);
                        el.Properties.Add(MapDatabase[npcData[0]].Number.ToString());
                        el.Properties.Add(npcData[1]);
                        if (npcData.Length > 2)
                        {
                            el.Properties.Add(npcData[2]);
                            el.Properties.Add(npcData[3]);
                        }
                        Quest1_32.Elements.Add(el);
                    }

                    objIndex = 0;
                    foreach (var qo in step.Objectives.Values)
                    {
                        string baseString = quest.ID + "_" + (i + 1) + "_";
                        switch (qo.Type)
                        {
                            case QuestObjectiveType.Item: el = new Metafile.Element(baseString + "ITEM" + objIndex); break;
                            case QuestObjectiveType.Skill: el = new Metafile.Element(baseString + "SKILL" + objIndex); break;
                            case QuestObjectiveType.Spell: el = new Metafile.Element(baseString + "SPELL" + objIndex); break;
                            case QuestObjectiveType.Kill: el = new Metafile.Element(baseString + "KILLED" + objIndex); break;
                            case QuestObjectiveType.Misc: el = new Metafile.Element(baseString + "KILLED" + objIndex); break;
                            default: continue;
                        }
                        objIndex++;

                        el.Properties.Add(qo.Name);
                        el.Properties.Add(qo.Requirement.ToString());
                        switch (qo.Type)
                        {
                            case QuestObjectiveType.Item:
                            case QuestObjectiveType.Kill:
                            case QuestObjectiveType.Misc:
                                el.Properties.Add(qo.DisplayName);
                                break;
                        }

                        Quest1_32.Elements.Add(el);
                    }

                    if (step.Reward != string.Empty)
                    {
                        el = new Metafile.Element(quest.ID + "_" + (i + 1) + "_REQUITAL0");
                        el.Properties.Add(string.Empty);
                        el.Properties.Add("{=c[Reward]\n" + step.Reward);
                        Quest1_32.Elements.Add(el);
                    }
                }
            }
            MetafileDatabase.Add(Quest1_32.Name, Quest1_32);
            Quest1_32.Write(DataPath);

            var NPCLocation = new Metafile("NPCLocation");
            foreach (var map in MapDatabase.Values)
            {
                var me = new Metafile.Element(map.Number.ToString());
                foreach (var ms in map.Spawns)
                {
                    if (ms.RegionType == SpawnRegion.SingleTile && NpcDatabase.ContainsKey(ms.NpcType))
                    {
                        me.Properties.Add(NpcDatabase[ms.NpcType].Name);
                        me.Properties.Add(ms.X.ToString());
                        me.Properties.Add(ms.Y.ToString());
                    }
                }
                me.Properties.Add("\r");
                NPCLocation.Elements.Add(me);
            }
            MetafileDatabase.Add(NPCLocation.Name, NPCLocation);
            NPCLocation.Write(DataPath);
            
            var sclass1 = new Metafile("SClass1");
            var sclass2 = new Metafile("SClass2");
            var sclass3 = new Metafile("SClass3");
            var sclass4 = new Metafile("SClass4");
            var sclass5 = new Metafile("SClass5");

            sclass1.Elements.Add(new Metafile.Element("Skill"));
            sclass2.Elements.Add(new Metafile.Element("Skill"));
            sclass3.Elements.Add(new Metafile.Element("Skill"));
            sclass4.Elements.Add(new Metafile.Element("Skill"));
            sclass5.Elements.Add(new Metafile.Element("Skill"));

            var skills = from s in SkillDatabase.Values
                         orderby s.Ranks[0].RequiredLevel ascending, s.Name
                         select s;
            foreach (var skill in skills)
            {
                var stringBuilder = new StringBuilder();
                foreach (var req in skill.RequiredItems)
                {
                    stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
                }
                stringBuilder.AppendFormat("{0} gold", skill.RequiredGold);

                var element = new Metafile.Element(skill.Name);
                element.Properties.Add(String.Format("{0}/{1}/{2}",
                    skill.Ranks[0].RequiredLevel,
                    skill.Ranks[0].RequiresMaster,
                    skill.Ranks[0].RequiredAbility));
                element.Properties.Add(String.Format("{0}/0/0", skill.Icon));
                element.Properties.Add(String.Format("{0}/{1}/{2}/{3}/{4}",
                    skill.RequiredStr, skill.RequiredInt, skill.RequiredWis, skill.RequiredCon, skill.RequiredDex));
                element.Properties.Add(String.Format("{0}/{1}", skill.RequiredSkillA, skill.RequiredSkillARank));
                element.Properties.Add(String.Format("{0}/{1}", skill.RequiredSkillB, skill.RequiredSkillBRank));
                element.Properties.Add(skill.DialogDescription + "\n\n{=e" + stringBuilder);
                switch (skill.RequiredClass)
                {
                    case Profession.Warrior: sclass1.Elements.Add(element); break;
                    case Profession.Rogue: sclass2.Elements.Add(element); break;
                    case Profession.Wizard: sclass3.Elements.Add(element); break;
                    case Profession.Priest: sclass4.Elements.Add(element); break;
                    case Profession.Monk: sclass5.Elements.Add(element); break;
                }
            }

            sclass1.Elements.Add(new Metafile.Element("Skill_End"));
            sclass2.Elements.Add(new Metafile.Element("Skill_End"));
            sclass3.Elements.Add(new Metafile.Element("Skill_End"));
            sclass4.Elements.Add(new Metafile.Element("Skill_End"));
            sclass5.Elements.Add(new Metafile.Element("Skill_End"));

            sclass1.Elements.Add(new Metafile.Element(""));
            sclass2.Elements.Add(new Metafile.Element(""));
            sclass3.Elements.Add(new Metafile.Element(""));
            sclass4.Elements.Add(new Metafile.Element(""));
            sclass5.Elements.Add(new Metafile.Element(""));

            sclass1.Elements.Add(new Metafile.Element("Spell"));
            sclass2.Elements.Add(new Metafile.Element("Spell"));
            sclass3.Elements.Add(new Metafile.Element("Spell"));
            sclass4.Elements.Add(new Metafile.Element("Spell"));
            sclass5.Elements.Add(new Metafile.Element("Spell"));

            var spells = from s in SpellDatabase.Values
                         orderby s.Ranks[0].RequiredLevel ascending, s.Name
                         select s;
            foreach (var spell in spells)
            {
                var stringBuilder = new StringBuilder();
                foreach (var req in spell.RequiredItems)
                {
                    stringBuilder.AppendFormat("{0} ({1}), ", req.Key, req.Value);
                }
                stringBuilder.AppendFormat("{0} gold", spell.RequiredGold);

                var element = new Metafile.Element(spell.Name);
                element.Properties.Add(String.Format("{0}/{1}/{2}",
                    spell.Ranks[0].RequiredLevel,
                    spell.Ranks[0].RequiresMaster,
                    spell.Ranks[0].RequiredAbility));
                element.Properties.Add(String.Format("{0}/0/0", spell.Icon));
                element.Properties.Add(String.Format("{0}/{1}/{2}/{3}/{4}",
                    spell.RequiredStr, spell.RequiredInt, spell.RequiredWis, spell.RequiredCon, spell.RequiredDex));
                element.Properties.Add(String.Format("{0}/{1}", spell.RequiredSpellA, spell.RequiredSpellARank));
                element.Properties.Add(String.Format("{0}/{1}", spell.RequiredSpellB, spell.RequiredSpellBRank));
                element.Properties.Add(spell.DialogDescription + "\n\n{=e" + stringBuilder);
                switch (spell.RequiredClass)
                {
                    case Profession.Warrior: sclass1.Elements.Add(element); break;
                    case Profession.Rogue: sclass2.Elements.Add(element); break;
                    case Profession.Wizard: sclass3.Elements.Add(element); break;
                    case Profession.Priest: sclass4.Elements.Add(element); break;
                    case Profession.Monk: sclass5.Elements.Add(element); break;
                }
            }

            sclass1.Elements.Add(new Metafile.Element("Spell_End"));
            sclass2.Elements.Add(new Metafile.Element("Spell_End"));
            sclass3.Elements.Add(new Metafile.Element("Spell_End"));
            sclass4.Elements.Add(new Metafile.Element("Spell_End"));
            sclass5.Elements.Add(new Metafile.Element("Spell_End"));

            MetafileDatabase.Add("SClass1", sclass1);
            MetafileDatabase.Add("SClass2", sclass2);
            MetafileDatabase.Add("SClass3", sclass3);
            MetafileDatabase.Add("SClass4", sclass4);
            MetafileDatabase.Add("SClass5", sclass5);

            sclass1.Write(DataPath);
            sclass2.Write(DataPath);
            sclass3.Write(DataPath);
            sclass4.Write(DataPath);
            sclass5.Write(DataPath);

            string[] files = Directory.GetFiles(DataPath + "\\metafiles");
            foreach (string file in files)
            {
                var mf = Metafile.Read(file);
                if (mf != null && !MetafileDatabase.ContainsKey(mf.Name))
                {
                    MetafileDatabase.Add(mf.Name, mf);
                    mf.Write(DataPath);
                }
            }
        }

        public void UpdateGame()
        {
            var gameObjects = new GameObject[GameObjects.Count];
            GameObjects.CopyTo(gameObjects);

            foreach (var go in gameObjects)
            {
                if (go != null && go.ID != uint.MinValue)
                    go.Update();
            }

            if (DateTime.UtcNow.Subtract(lastSave).TotalMinutes > 1)
            {
                foreach (var client in Clients)
                {
                    if (client.Player != null)
                        client.Player.Save();
                }
                lastSave = DateTime.UtcNow;
            }
        }

        public void InsertGameObject(GameObject go)
        {
            if (go.ID != uint.MinValue)
                return;

            go.GameServer = this;
            go.ID = nextGameObjectID++;

            GameObjects.Add(go);
            GameObjectsID.Add(go.ID, go);

            go.OnGameServerInsert(this);
        }
        public void RemoveGameObject(GameObject go)
        {
            if (go.ID == uint.MinValue)
                return;

            if (go is VisibleObject)
            {
                var c = go as VisibleObject;
                if (c.Map != null)
                    c.Map.RemoveCharacter(c);
                if (c is Monster)
                {
                    var npc = c as Monster;
                    if (npc.SpawnControl != null)
                        npc.SpawnControl.CurrentSpawns--;
                }
                if (c is Reactor)
                {
                    var reactor = c as Reactor;
                    if (reactor.SpawnControl != null)
                        reactor.SpawnControl.CurrentSpawns--;
                }
                if (c is Item)
                {
                    var item = c as Item;
                    if (item.SpawnControl != null)
                        item.SpawnControl.CurrentSpawns--;
                }
            }

            GameObjects.Remove(go);
            GameObjectsID.Remove(go.ID);

            go.ID = uint.MinValue;

            go.OnGameServerRemove(this);
        }
        public void DeleteGameObject(GameObject go)
        {
            go.OnGameServerDelete(this);
        }

        public bool SendParcel(Parcel parcel, string player)
        {
            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT character_id FROM characters WHERE (name = @name) AND (server = @server)";
            com.Parameters.AddWithValue("@name", player);
            com.Parameters.AddWithValue("@server", Name);
            var result = com.ExecuteScalar();

            if (result == null)
                return false;

            var guid = (int)result;

            if (guid == 0)
                return false;

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "INSERT INTO parcels VALUES (@character_id, @sender, @itemname, @color, @amount, @durability, @gold)";
            com.Parameters.AddWithValue("@character_id", guid);
            com.Parameters.AddWithValue("@sender", parcel.Sender);
            if (parcel.Item != null)
            {
                com.Parameters.AddWithValue("@itemname", parcel.Item.GetType().Name);
                com.Parameters.AddWithValue("@color", parcel.Item.Color);
                com.Parameters.AddWithValue("@amount", parcel.Item.Amount);
                com.Parameters.AddWithValue("@durability", parcel.Item.CurrentDurability);
            }
            else
            {
                com.Parameters.AddWithValue("@itemname", string.Empty);
                com.Parameters.AddWithValue("@color", 0);
                com.Parameters.AddWithValue("@amount", 0);
                com.Parameters.AddWithValue("@durability", 0);
            }
            com.Parameters.AddWithValue("@gold", parcel.Gold);
            com.ExecuteNonQuery();

            foreach (var c in Clients)
            {
                if ((c != null) && (c.Player != null) && c.Player.Name.Equals(player, StringComparison.CurrentCultureIgnoreCase))
                {
                    c.Player.Parcels.Add(parcel);
                    c.Player.Client.SendMessage("You have been sent a parcel.");
                    break;
                }
            }

            return true;
        }

        public bool HasRealmFirst(string id)
        {
            return RealmFirsts.Contains(id);
        }
        public void SetRealmFirst(string id)
        {
            if (RealmFirsts.Add(id))
            {
                var com = Program.MySqlConnection.CreateCommand();
                com.CommandText = "INSERT INTO realm_firsts VALUES (@id)";
                com.Parameters.AddWithValue("@id", id);
                com.ExecuteNonQuery();
            }
        }

        public void BroadcastMessage(string format, params object[] args)
        {
            foreach (var client in Clients)
            {
                if (client != null)
                {
                    client.SendMessage(format, args);
                }
            }
        }

        public Player FindPlayer(string name)
        {
            foreach (var client in Clients)
            {
                if (client != null && client.Player != null && client.Player.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return client.Player;
            }
            return null;
        }

        #region Message Handlers
        private void MsgHandler_ClientJoin(Client client, ClientPacket msg)
        {
            if (client.Player != null)
                return;

            var seed = msg.ReadByte();
            var key = msg.ReadString8();
            var name = msg.ReadString8();
            var id = msg.ReadUInt32();

            var r = ExpectedRedirects[id];
            var encryptionParameters = new Encryption.Parameters(key, seed);

            if (name != r.Name || !r.EncryptionParameters.Matches(encryptionParameters))
                return;

            var accountId = 0;
            var gold = 0L;

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT acct_id FROM characters WHERE (name = @name)";
            com.Parameters.AddWithValue("@name", r.Name);
            var reader = com.ExecuteReader();
            if (reader.Read())
            {
                accountId = reader.GetInt32(0);
            }
            reader.Close();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT gold FROM accounts WHERE acct_id = @acct_id";
            com.Parameters.AddWithValue("@acct_id", accountId);
            reader = com.ExecuteReader();
            if (reader.Read())
            {
                gold = reader.GetInt64("gold");
            }
            reader.Close();

            foreach (Client c in Clients)
            {
                if (c.Player != null && c.Player.Name.Equals(r.Name, StringComparison.CurrentCultureIgnoreCase))
                    return;

                if (c.Player != null && c.Player.AccountID == accountId)
                    return;
            }

            Player p = new Player(this, r.Name);
            InsertGameObject(p);
            p.Client = client;
            client.Player = p;

            #region Characters
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT "
                + "character_id, "      // 0
                + "name, "              // 1
                + "password, "          // 2
                + "acct_id, "           // 3
                + "server, "            // 4
                + "last_session_id, "   // 5
                + "gm, "                // 6
                + "sex, "               // 7
                + "class, "             // 8
                + "specialization, "    // 9
                + "mapname, "           // 10
                + "mapid, "             // 11
                + "pointx, "            // 12
                + "pointy, "            // 13
                + "direction, "         // 14
                + "gold, "              // 15
                + "gamepoints, "        // 16
                + "hairstyle, "         // 17
                + "haircolor, "         // 18
                + "ac, "                // 19
                + "mr, "                // 20
                + "s_str, "             // 21
                + "s_int, "             // 22
                + "s_wis, "             // 23
                + "s_con, "             // 24
                + "s_dex, "             // 25
                + "dmg, "               // 26
                + "hit, "               // 27
                + "curhp, "             // 28
                + "curmp, "             // 29
                + "maxhp, "             // 30
                + "maxmp, "             // 31
                + "level, "             // 32
                + "exp, "               // 33
                + "tnl, "               // 34
                + "ability, "           // 35
                + "abexp, "             // 36
                + "tna, "               // 37
                + "statpoints, "        // 38
                + "dead, "              // 39
                + "deathmapname, "      // 40
                + "deathmapid, "        // 41
                + "deathpointx, "       // 42
                + "deathpointy, "       // 43
                + "displaymask, "       // 44
                + "nation, "            // 45
                + "title, "             // 46
                + "bagslots, "          // 47
                + "occupation, "        // 48
                + "apcounter, "         // 49
                + "apmultiply "         // 50
                + "FROM characters "
                + "WHERE (name = @name)";
            com.Parameters.AddWithValue("@name", r.Name);
            reader = com.ExecuteReader();
            if (reader.Read())
            {
                p.GUID = reader.GetInt32(0);
                p.AccountID = reader.GetInt32(3);
                p.LastSessionID = reader.GetInt64(5);
                p.AdminRights = (AdminRights)Enum.Parse(typeof(AdminRights), reader.GetString(6));
                p.Sex = (Gender)Enum.Parse(typeof(Gender), reader.GetString(7));
                p.Class = (Profession)Enum.Parse(typeof(Profession), reader.GetString(8));
                p.Specialization = (Specialization)Enum.Parse(typeof(Specialization), reader.GetString(9));
                p.Map = MapDatabase[reader.GetString(10)];
                if (p.LastSessionID == Program.SessionID)
                {
                    var newMap = GameObject<Map>(reader.GetUInt32(11));
                    if (newMap != null)
                        p.Map = newMap;
                }
                p.Point.X = reader.GetInt32(12);
                p.Point.Y = reader.GetInt32(13);
                p.Direction = (Direction)Enum.Parse(typeof(Direction), reader.GetString(14));
                p.Gold = gold;
                p.GamePoints = reader.GetInt64(16);
                p.HairStyle = reader.GetInt32(17);
                p.HairColor = reader.GetInt32(18);
                p.BaseArmorClass = reader.GetInt64(19);
                p.BaseMagicResistance = reader.GetInt64(20);
                p.BaseStr = reader.GetInt64(21);
                p.BaseInt = reader.GetInt64(22);
                p.BaseWis = reader.GetInt64(23);
                p.BaseCon = reader.GetInt64(24);
                p.BaseDex = reader.GetInt64(25);
                p.BaseDmg = reader.GetInt64(26);
                p.BaseHit = reader.GetInt64(27);
                p.CurrentHP = reader.GetInt64(28);
                p.CurrentMP = reader.GetInt64(29);
                p.BaseMaximumHP = reader.GetInt64(30);
                p.BaseMaximumMP = reader.GetInt64(31);
                p.Level = reader.GetInt64(32);
                p.Experience = reader.GetInt64(33);
                p.Ability = reader.GetInt64(35);
                p.AbilityExp = reader.GetInt64(36);
                p.AvailableStatPoints = reader.GetInt32(38);
                if (reader.GetBoolean(39))
                    p.LifeStatus = LifeStatus.Dead;
                p.DeathMap = MapDatabase[reader.GetString(40)];
                if (p.LastSessionID == Program.SessionID)
                {
                    var newMap = GameObject<Map>(reader.GetUInt32(41));
                    if (newMap != null)
                        p.DeathMap = newMap;
                }
                p.DeathPoint.X = reader.GetInt32(42);
                p.DeathPoint.Y = reader.GetInt32(43);
                p.DisplayBitmask = reader.GetUInt16(44);
                p.Nation = reader.GetString(45);
                p.Title = reader.GetInt32(46);
                p.AvailableBagSlots = reader.GetInt32(47);
                p.Occupation = (Occupation)Enum.Parse(typeof(Occupation), reader.GetString(48));
                var apcounter = reader.GetString(49).Split(',');
                var apmultiply = reader.GetString(50).Split(',');

                foreach (var str in apcounter)
                {
                    var kvp = str.Split(':');
                    if (kvp.Length == 2)
                    {
                        long guid, value;
                        if (long.TryParse(kvp[0], out guid) && long.TryParse(kvp[1], out value))
                            p.AbilityPointCounter.Add(guid, value);
                    }
                }

                foreach (var str in apmultiply)
                {
                    var kvp = str.Split(':');
                    if (kvp.Length == 2)
                    {
                        long guid, value;
                        if (long.TryParse(kvp[0], out guid) && long.TryParse(kvp[1], out value))
                            p.AbilityPointMultiplier.Add(guid, value);
                    }
                }
            }
            reader.Close();
            #endregion

            client.SendStatistics(StatUpdateFlags.Full);
            client.SendPlayerID();

            #region Cookies
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT c_key, c_value FROM cookies WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var c_key = reader.GetString(0);
                var c_value = reader.GetString(1);
                if (p.Cookies.ContainsKey(c_key))
                    p.Cookies[c_key] = c_value;
                else
                    p.Cookies.Add(c_key, c_value);
            }
            reader.Close();
            #endregion

            #region Legends
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT legendname, date, dateupdated, arguments FROM legends WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    var l = CreateLegendMark(reader.GetString(0));
                    if (l != null)
                    {
                        l.DateCreated = DateTime.Parse(reader.GetString(1));
                        l.DateUpdated = DateTime.Parse(reader.GetString(2));
                        l.Arguments = reader.GetString(3).Split('\n');
                        p.Legend.Add(l.Key, l);
                    }
                }
                catch (Exception e)
                {
                    Program.WriteLine(e);
                    //                            Console.ReadKey(true);
                }
            }
            reader.Close();
            #endregion

            #region Skillbooks
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT skillname, rank, level, cooldown, i FROM skillbooks WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                int index = reader.GetInt32(4);
                if (p.SkillBook[index] == null)
                {
                    var s = CreateSkill(reader.GetString(0));
                    if (s != null && !s.RequiresAdmin && (s.RequiredClass == Profession.Peasant || s.RequiredClass == p.Class || p.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions)))
                    {
                        s.Rank = reader.GetInt32(1);
                        s.Level = reader.GetInt32(2);
                        p.AddSkill(s, index);
                        p.Cooldown(s, reader.GetInt64(3));
                    }
                }
            }
            reader.Close();

            if (p.AdminRights.HasFlag(AdminRights.GameMaster))
            {
                var s1 = new Skills.DeleteObject();
                InsertGameObject(s1);
                p.AddSkill(s1);

                var s2 = new Skills.GameMasterHelper();
                InsertGameObject(s2);
                p.AddSkill(s2);
            }
            if (p.AdminRights.HasFlag(AdminRights.ArenaHost) ||
                p.AdminRights.HasFlag(AdminRights.GameMaster))
            {
                var s = new Skills.ToggleBlock();
                InsertGameObject(s);
                p.AddSkill(s);
            }
            #endregion

            #region Spellbooks
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT spellname, rank, level, cooldown, i FROM spellbooks WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                int index = reader.GetInt32(4);
                if (p.SpellBook[index] == null)
                {
                    var s = CreateSpell(reader.GetString(0));
                    if (s != null && !s.RequiresAdmin && (s.RequiredClass == Profession.Peasant || s.RequiredClass == p.Class || p.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions)))
                    {
                        s.Rank = reader.GetInt32(1);
                        s.Level = reader.GetInt32(2);
                        p.AddSpell(s, index);
                        p.Cooldown(s, reader.GetInt64(3));
                    }
                }
            }
            reader.Close();

            if (p.AdminRights.HasFlag(AdminRights.ArenaHost) ||
                p.AdminRights.HasFlag(AdminRights.CanCreateItems) ||
                p.AdminRights.HasFlag(AdminRights.CanCreateSkills) ||
                p.AdminRights.HasFlag(AdminRights.CanCreateSpells) ||
                p.AdminRights.HasFlag(AdminRights.CanCreateMonsters) ||
                p.AdminRights.HasFlag(AdminRights.CanCreateMerchants))
            {
                var s1 = new Spells.Create();
                InsertGameObject(s1);
                p.AddSpell(s1);

                var s2 = new Spells.ClassLookup();
                InsertGameObject(s2);
                p.AddSpell(s2);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanKickUser))
            {
                var s = new Spells.KickUser();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.ArenaHost) ||
                p.AdminRights.HasFlag(AdminRights.CanLocateUser))
            {
                var s = new Spells.LocateUser();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanMonsterForm))
            {
                var s = new Spells.MonsterSprite();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanMoveUser))
            {
                var s = new Spells.MoveUser();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanSetMap))
            {
                var s = new Spells.SetMap();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanChangeStat))
            {
                var s = new Spells.Stat();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanStealth))
            {
                var s = new Spells.Stealth();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.ArenaHost) ||
                p.AdminRights.HasFlag(AdminRights.CanSummonUser))
            {
                var s = new Spells.SummonUser();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            if (p.AdminRights.HasFlag(AdminRights.CanTeleport))
            {
                var s = new Spells.Teleport();
                InsertGameObject(s);
                p.AddSpell(s);
            }
            #endregion

            #region Spellbars
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT spellname, rank, timeleft, args FROM spellbars WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var dict = new Dictionary<string, string>();
                var s = reader.GetString(0);
                var args = reader.GetString(3);
                foreach (var arg in args.Split('\n'))
                {
                    var kvp = arg.Split(';');
                    if (kvp.Length == 2)
                        dict.Add(kvp[0], kvp[1]);
                }
                if (SpellTypes.ContainsKey(s))
                {
                    p.AddStatus(s, rank: reader.GetInt32(1), timeLeft: reader.GetInt32(2), args: dict);
                }
            }
            reader.Close();
            #endregion

            #region Inventories
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT itemname, color, amount, durability, soulbound, i, data, maxhpmod, maxmpmod, strmod, intmod, wismod, conmod, dexmod, hitmod, dmgmod, acmod, mrmod, minattackmod, maxattackmod, minmagicmod, maxmagicmod FROM inventories WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                int index = reader.GetInt32(5);
                if (p.Inventory[index] == null)
                {
                    var i = CreateItem(reader.GetString(0));
                    if (i != null)
                    {
                        i.Color = reader.GetInt32(1);
                        i.Amount = reader.GetInt32(2);
                        i.CurrentDurability = reader.GetInt32(3);
                        i.Soulbound = reader.GetBoolean(4);
                        i.MiscData = reader.GetString(6);
                        i.DynamicMaximumHpMod = reader.GetInt64(7);
                        i.DynamicMaximumMpMod = reader.GetInt64(8);
                        i.DynamicStrMod = reader.GetInt64(9);
                        i.DynamicIntMod = reader.GetInt64(10);
                        i.DynamicWisMod = reader.GetInt64(11);
                        i.DynamicConMod = reader.GetInt64(12);
                        i.DynamicDexMod = reader.GetInt64(13);
                        i.DynamicHitMod = reader.GetInt64(14);
                        i.DynamicDmgMod = reader.GetInt64(15);
                        i.DynamicArmorClassMod = reader.GetInt64(16);
                        i.DynamicMagicResistanceMod = reader.GetInt64(17);
                        i.DynamicMinimumAttackPowerMod = reader.GetInt64(18);
                        i.DynamicMaximumAttackPowerMod = reader.GetInt64(19);
                        i.DynamicMinimumMagicPowerMod = reader.GetInt64(20);
                        i.DynamicMaximumMagicPowerMod = reader.GetInt64(21);
                        p.AddItem(i, index);
                    }
                }
            }
            reader.Close();
            #endregion

            #region Bags
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT itemname, color, amount, durability, soulbound, i, data, maxhpmod, maxmpmod, strmod, intmod, wismod, conmod, dexmod, hitmod, dmgmod, acmod, mrmod, minattackmod, maxattackmod, minmagicmod, maxmagicmod FROM bags WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                int index = reader.GetInt32(5);
                if (p.BagItems[index] == null)
                {
                    var i = CreateItem(reader.GetString(0));
                    if (i != null)
                    {
                        i.Slot = index + 1;
                        i.Color = reader.GetInt32(1);
                        i.Amount = reader.GetInt32(2);
                        i.CurrentDurability = reader.GetInt32(3);
                        i.Soulbound = reader.GetBoolean(4);
                        i.MiscData = reader.GetString(6);
                        i.DynamicMaximumHpMod = reader.GetInt64(7);
                        i.DynamicMaximumMpMod = reader.GetInt64(8);
                        i.DynamicStrMod = reader.GetInt64(9);
                        i.DynamicIntMod = reader.GetInt64(10);
                        i.DynamicWisMod = reader.GetInt64(11);
                        i.DynamicConMod = reader.GetInt64(12);
                        i.DynamicDexMod = reader.GetInt64(13);
                        i.DynamicHitMod = reader.GetInt64(14);
                        i.DynamicDmgMod = reader.GetInt64(15);
                        i.DynamicArmorClassMod = reader.GetInt64(16);
                        i.DynamicMagicResistanceMod = reader.GetInt64(17);
                        i.DynamicMinimumAttackPowerMod = reader.GetInt64(18);
                        i.DynamicMaximumAttackPowerMod = reader.GetInt64(19);
                        i.DynamicMinimumMagicPowerMod = reader.GetInt64(20);
                        i.DynamicMaximumMagicPowerMod = reader.GetInt64(21);
                        p.BagItems[index] = i;
                    }
                }
            }
            reader.Close();
            #endregion

            #region Bank Items
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT itemname, color, amount, durability, data FROM bankitems WHERE (acct_id = @acct_id)";
            com.Parameters.AddWithValue("@acct_id", p.AccountID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                if (p.AccountID != 0)
                {
                    var i = CreateItem(reader.GetString(0));
                    if (i != null)
                    {
                        int index = p.BankItems.FindIndex(item => item != null && item.GetType() == i.GetType());
                        if (index < 0)
                        {
                            i.Color = reader.GetInt32(1);
                            i.Amount = reader.GetInt32(2);
                            i.CurrentDurability = reader.GetInt32(3);
                            i.MiscData = reader.GetString(4);
                            p.BankItems.Add(i);
                        }
                        else
                        {
                            p.BankItems[index].Amount += i.Amount;
                        }
                    }
                }
            }
            reader.Close();
            #endregion

            #region Parcels
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT sender, itemname, color, amount, durability, gold FROM parcels WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var s = reader.GetString(0);
                var i = CreateItem(reader.GetString(1));
                if (i != null)
                {
                    i.Color = reader.GetInt32(2);
                    i.Amount = reader.GetInt32(3);
                    i.CurrentDurability = reader.GetInt32(4);
                }
                var g = reader.GetInt64(5);
                p.Parcels.Add(new Parcel(s, i, g));
            }
            reader.Close();
            #endregion

            #region Equipments
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT itemname, color, durability, soulbound, i, data, maxhpmod, maxmpmod, strmod, intmod, wismod, conmod, dexmod, hitmod, dmgmod, acmod, mrmod, minattackmod, maxattackmod, minmagicmod, maxmagicmod FROM equipments WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                int index = reader.GetInt32(4);
                if (p.Equipment[index] == null)
                {
                    var i = CreateItem(reader.GetString(0));
                    if (i != null)
                    {
                        i.Color = reader.GetInt32(1);
                        i.CurrentDurability = reader.GetInt32(2);
                        i.Soulbound = reader.GetBoolean(3);
                        i.MiscData = reader.GetString(5);
                        i.DynamicMaximumHpMod = reader.GetInt64(6);
                        i.DynamicMaximumMpMod = reader.GetInt64(7);
                        i.DynamicStrMod = reader.GetInt64(8);
                        i.DynamicIntMod = reader.GetInt64(9);
                        i.DynamicWisMod = reader.GetInt64(10);
                        i.DynamicConMod = reader.GetInt64(11);
                        i.DynamicDexMod = reader.GetInt64(12);
                        i.DynamicHitMod = reader.GetInt64(13);
                        i.DynamicDmgMod = reader.GetInt64(14);
                        i.DynamicArmorClassMod = reader.GetInt64(15);
                        i.DynamicMagicResistanceMod = reader.GetInt64(16);
                        i.DynamicMinimumAttackPowerMod = reader.GetInt64(17);
                        i.DynamicMaximumAttackPowerMod = reader.GetInt64(18);
                        i.DynamicMinimumMagicPowerMod = reader.GetInt64(19);
                        i.DynamicMaximumMagicPowerMod = reader.GetInt64(20);
                        p.AddEquipment((Equipment)i, index);
                    }
                }
            }
            reader.Close();
            #endregion

            #region Guild
            foreach (var guild in Guilds.Values)
            {
                if (guild.Members.Contains(name))
                {
                    foreach (var c in Clients)
                    {
                        if (c.Player != null && c.Player.Guild == guild)
                        {
                            c.SendMessage(string.Format("{0} member {1} has logged in", guild.Name, name));
                        }
                    }
                    p.Guild = guild;
                    if (guild.Leader.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        p.GuildRank = GuildRank.Leader;
                    else if (guild.Council.Contains(name))
                        p.GuildRank = GuildRank.Council;
                    else
                        p.GuildRank = GuildRank.Member;
                }
            }
            #endregion

            #region Quests

            foreach (var questKey in QuestTypes.Keys)
            {
                var quest = CreateQuest(questKey);
                p.Quests.Add(questKey, quest);
            }

            var activeQuests = new List<Quest>();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT quest_name, current_step, progress FROM quests WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var questName = reader.GetString(0);
                var questStep = reader.GetInt32(1);
                var progress = (QuestProgress)Enum.Parse(typeof(QuestProgress), reader.GetString(2));

                if (p.Quests.ContainsKey(questName) && progress != QuestProgress.Unstarted)
                {
                    var quest = p.Quests[questName];
                    if (questStep > 0 && questStep <= quest.Steps.Count)
                    {
                        quest.Progress = progress;
                        quest.CurrentStep = questStep;
                        if (progress == QuestProgress.InProgress)
                            activeQuests.Add(quest);
                    }
                }
            }
            reader.Close();
            #endregion

            #region Quest Steps
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT quest_name, step, progress FROM quest_steps WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var questName = reader.GetString(0);
                var questStep = reader.GetInt32(1);
                var progress = (QuestProgress)Enum.Parse(typeof(QuestProgress), reader.GetString(2));

                if (p.Quests.ContainsKey(questName) && progress != QuestProgress.Unstarted)
                {
                    var quest = p.Quests[questName];
                    if (questStep > 0 && questStep <= quest.Steps.Count && quest.Progress != QuestProgress.Unstarted)
                    {
                        quest[questStep].Progress = progress;
                    }
                }
            }
            reader.Close();
            #endregion

            #region Quest Objectives
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT questname, queststep, objectivename, count, miscdata FROM quest_objectives WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var questName = reader.GetString(0);
                var questStep = reader.GetInt32(1);
                var objName = reader.GetString(2);
                var count = reader.GetInt32(3);
                var misc = reader.GetString(4);

                if (QuestTypes.ContainsKey(questName))
                {
                    var quest = p.Quests[questName];
                    if (questStep > 0 && questStep <= quest.Steps.Count && quest.Progress != QuestProgress.Unstarted)
                    {
                        var subQuest = quest[questStep];
                        if (subQuest.Objectives.ContainsKey(objName) && subQuest.Progress != QuestProgress.Unstarted)
                        {
                            var obj = subQuest[objName];
                            obj.Count = Math.Min(count, obj.Requirement);
                            obj.MiscData = misc ?? string.Empty;
                        }
                    }
                }
            }
            reader.Close();

            p.SendQuestInfo(activeQuests.ToArray());
            #endregion

            #region Professions
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT list FROM professions WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var list = reader.GetString(0);
                foreach (var item in list.Split(','))
                {
                    if (ManufactureTypes.ContainsKey(item))
                        p.AvailableManufactures.Add(item);
                }
            }
            reader.Close();
            #endregion

            #region Professions
            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT list FROM visited_maps WHERE (character_id = @character_id)";
            com.Parameters.AddWithValue("@character_id", p.GUID);
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                var list = reader.GetString(0);
                foreach (var item in list.Split(','))
                {
                    if (MapDatabase.ContainsKey(item))
                        p.VisitedMaps.Add(item);
                }
            }
            reader.Close();
            #endregion

            if (p.AdminRights.HasFlag(AdminRights.CanStealth))
                p.Stealth = true;

            var packet = new ServerPacket(0x54);
            packet.WriteByte(0x00); // ??
            packet.WriteUInt32(0x00); // ??
            packet.WriteByte(60); // 60 slots
            for (int i = 0; i < 60; i++)
            {
                packet.WriteByte(0x00);
                packet.WriteByte(0x00);
            }
            client.Enqueue(packet);

            var map = p.Map;
            p.Map = null;
            map.InsertCharacter(client.Player, p.Point.X, p.Point.Y);

            map.OnLogin(client.Player);

            if (p.LastSessionID != Program.SessionID)
                map.OnSessionMismatch(client.Player);

            ExpectedRedirects.Remove(id);
            r = null;

            if (GlobalScript != null)
            {
                var dialog = GlobalScript.OnLogin(p);
                p.GiveDialog(p, dialog);
            }

            var characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            do
            {
                var sb = new StringBuilder();
                for (int i = 0; i < 32; i++)
                {
                    sb.Append(characters[Program.Random(characters.Length)]);
                }
                p.SecureKey = sb.ToString();
            } while (secureKeys.Contains(p.SecureKey));
            secureKeys.Add(p.SecureKey);

            p.Loaded = true;

            Program.WriteLine("Character logged in: {0} [{1}]", name, ((IPEndPoint)client.Socket.RemoteEndPoint).Address);
        }
        private void MsgHandler_RequestMap(Client client, ClientPacket msg)
        {
            if (client.Player == null)
                return;

            client.SendMap(client.Player.Map);
        }
        private void MsgHandler_UseSpell(Client client, ClientPacket msg)
        {
            var player = client.Player;
            
            if (player.Polymorphed)
            {
                client.SendMessage("You are polymorphed.");
                return;
            }

            if (player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            int slot = msg.ReadByte();

            var spell = player.SpellBook[slot - 1];

            if (player.SpellPoints < spell.SpellPointCost)
                return;

            if (spell.CastLines > 0)
            {
                var timeElapsed = DateTime.UtcNow.Subtract(player.StartCastTime).TotalSeconds;
                if (timeElapsed < spell.CastLines - 0.5 || timeElapsed > spell.CastLines + 0.5 || spell.CastLines != player.NextSpellLines || !player.IsCasting)
                    return;
            }

            switch (spell.CastType)
            {
                case SpellCastType.NoTarget:
                    {
                        player.UseSpell(spell, null, string.Empty);
                    } break;
                case SpellCastType.Target:
                    {
                        var c = GameObject<Character>(msg.ReadUInt32());
                        if (player.WithinRange(c, 12))
                        {
                            player.UseSpell(spell, c, string.Empty);
                        }
                    } break;
                case SpellCastType.TextInput:
                case SpellCastType.DigitInputOne:
                case SpellCastType.DigitInputTwo:
                case SpellCastType.DigitInputThree:
                case SpellCastType.DigitInputFour:
                    {
                        string text = msg.ReadString();
                        player.UseSpell(spell, null, text);
                    } break;
            }

            player.SpellPoints -= spell.SpellPointCost;
        }
        private void MsgHandler_Walk(Client client, ClientPacket msg)
        {
            if (client.Player == null)
                return;

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            Direction direction = (Direction)msg.ReadByte();
            if (client.Player.WalkCounter > 5 || !client.Player.Walk(direction))
            {
                client.SendMapInfo();
                client.SendLocation();
                client.Player.Display();
                foreach (var obj in client.Player.Map.Objects)
                {
                    obj.DisplayTo(client.Player);
                }
                var p = new ServerPacket(0x22);
                p.WriteByte(0x00);
                client.Enqueue(p);
                return;
            }

            if (!client.Player.AdminRights.HasFlag(AdminRights.CanMonsterForm))
                client.Player.WalkCounter += 2;
        }
        private void MsgHandler_Paper(Client client, ClientPacket msg)
        {
            var slot = msg.ReadByte();
            var text = msg.ReadString(msg.ReadUInt16());
            if (client.Player.Paper != null)
            {
                client.Player.Paper.MiscData = text;
                client.Player.Paper = null;
            }
        }
        private void MsgHandler_PickupItem(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            int slot = msg.ReadByte();
            if ((slot > 0) && (slot <= client.Player.Inventory.Length) && (client.Player.Inventory[slot - 1] == null))
            {
                int x = msg.ReadUInt16();
                int y = msg.ReadUInt16();

                if (client.Player.Map.Width <= x)
                    return;
                if (client.Player.Map.Height <= y)
                    return;
                if (Math.Abs(client.Player.Point.X - x) > 2)
                    return;
                if (Math.Abs(client.Player.Point.Y - y) > 2)
                    return;

                var tile = client.Player.Map[x, y];

                bool success = false;
                bool isProtected = false;

                for (int i = 0; i < tile.Objects.Count && !success; i++)
                {
                    var item = tile.Objects[i] as Item;

                    if (item == null)
                        continue;

                    if (client.Player.Quests.ContainsKey(item.QuestName))
                    {
                        bool continueQuest = true;

                        var quest = client.Player.Quests[item.QuestName];
                        if (item.QuestStep == quest.CurrentStep)
                        {
                            var subQuest = quest.QuestStep;
                            if (subQuest.Progress == QuestProgress.InProgress)
                                continueQuest = false;
                        }

                        if (continueQuest)
                            continue;
                    }

                    if (item.ProtectionOwners.Count > 0)
                    {
                        bool isOwner = item.ProtectionOwners.Contains(client.Player.Name);
                        if (!isOwner && (DateTime.UtcNow < item.ProtectionExpireTime))
                        {
                            isProtected = true;
                            continue;
                        }
                    }

                    if (item is Gold)
                    {
                        client.Player.Gold += item.Value;
                        client.SendStatistics(StatUpdateFlags.Experience);
                        RemoveGameObject(item);
                        success = true;
                    }
                    else
                    {
                        if (item.LootRoll)
                        {
                            if (!item.LootRollers.Contains(client.Player))
                            {
                                item.LootRollers.Add(client.Player);
                                client.SendMessage(string.Format("You have been added to roll for {0}.", item.Name));
                            }
                        }
                        else
                        {
                            if (item.EquipOnPickup && item is Equipment)
                            {
                                item.Map.RemoveCharacter(item);
                                var equip = (item as Equipment);
                                if (client.Player.Equipment[equip.EquipmentSlot - 1] == null)
                                {
                                    client.Player.AddEquipment(equip);
                                    client.Player.Display();
                                    item.OnPickup(client.Player);
                                }
                            }
                            else
                            {
                                item.Map.RemoveCharacter(item);
                                if (client.Player.AddItem(item, slot - 1))
                                    item.OnPickup(client.Player);
                            }
                        }
                        success = true;
                    }
                }

                if (isProtected && !success)
                    client.SendMessage("That item is protected.");
            }
        }
        private void MsgHandler_DropItem(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            int slot = msg.ReadByte();
            if ((slot > 0) && (slot <= client.Player.Inventory.Length))
            {
                int x = msg.ReadUInt16();
                int y = msg.ReadUInt16();
                int amount = msg.ReadInt32();

                if (client.Player.Map.Width <= x)
                    return;
                if (client.Player.Map.Height <= y)
                    return;
                if (Math.Abs(client.Player.Point.X - x) > 2)
                    return;
                if (Math.Abs(client.Player.Point.Y - y) > 2)
                    return;
                if (client.Player.Map.Walls[x, y])
                    return;

                var item = client.Player.Inventory[slot - 1];
                if (item != null)
                {
                    if ((item.Soulbound || item.BindType == BindType.BindToAccount) && !client.Player.AdminRights.HasFlag(AdminRights.CanDropSoulboundItems))
                    {
                        client.SendMessage("You can't drop that item.");
                        return;
                    }
                    item = client.Player.RemoveItem(slot - 1, amount);
                    client.Player.Map.InsertCharacter(item, x, y);
                }
            }
        }
        private void MsgHandler_Turn(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled && !status.CanTurnWithChannel)
                    client.Player.RemoveStatus(status.StatusName);
            }

            Direction direction = (Direction)msg.ReadByte();
            if (client.Player.Direction != direction)
            {
                client.Player.Turn(direction);
            }
        }
        private void MsgHandler_RequestDisplay(Client client, ClientPacket msg)
        {

        }
        private void MsgHandler_Spacebar(Client client, ClientPacket msg)
        {
            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            foreach (Skill s in client.Player.SkillBook)
            {
                if ((s != null) && s.IsAssail)
                    client.Player.UseSkill(s);
            }
        }
        private void MsgHandler_RequestBag(Client client, ClientPacket msg)
        {
            client.Player.UpdateBag();
        }
        private void MsgHandler_Userlist(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var userlist = from c in Clients
                           where (c != null) && (c.Player != null) && (client.Player.AdminRights >= c.Player.AdminRights || !c.Player.Stealth)
                           orderby c.Player.Level descending, c.Player.Name ascending
                           select c.Player;

            var p = new ServerPacket(0x36);
            p.WriteUInt16((ushort)userlist.Count());
            p.WriteUInt16((ushort)userlist.Count());
            foreach (Player player in userlist)
            {
                p.WriteByte((byte)player.Class);
                p.WriteByte(255);
                p.WriteByte((byte)player.Status);
                p.WriteByte((byte)player.Title); // title
                p.WriteByte(0x00); // master check
                p.WriteByte(0x01); // emblem icon
                p.WriteUInt16(0x00); // icon 1
                p.WriteUInt16(0x00); // icon 2
                p.WriteString8(player.Name);
            }
            client.Enqueue(p);
        }
        private void MsgHandler_PrivateMessage(Client client, ClientPacket msg)
        {
            string name = msg.ReadString(msg.ReadByte());
            string message = msg.ReadString(msg.ReadByte());

            if (DateTime.UtcNow.Subtract(client.Player.LastMessage).TotalSeconds < 0.30)
                return;

            client.Player.LastMessage = DateTime.UtcNow;

            if (name == "!")
            {
                if (client.Player.Guild == null)
                {
                    client.SendMessage("You have no guild.", 12);
                    return;
                }

                foreach (var c in Clients)
                {
                    if ((c.Player != null) && (c.Player.Guild == client.Player.Guild))
                    {
                        c.SendMessage(string.Format("<!{0}> {1}", client.Player.Name, message), 12);
                    }
                }

                return;
            }
            else if (name == "!!")
            {
                if (!client.Player.Group.HasMembers)
                {
                    client.SendMessage("You have no group.", 11);
                    return;
                }

                foreach (var p in client.Player.Group.Members)
                {
                    p.SendMessage(string.Format("[!{0}] {1}", client.Player.Name, message), 11);
                }
                return;
            }

            foreach (var c in Clients)
            {
                if ((c.Player != null) && !c.Player.Stealth && c.Player.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    c.SendMessage(string.Format("{0}\" {1}", client.Player.Name, message), 0);
                    client.SendMessage(string.Format("{0}> {1}", c.Player.Name, message), 0);
                    return;
                }
            }

            client.SendMessage(String.Format("{0} is nowhere to be found.", name), 0);
        }
        private void MsgHandler_UseItem(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }

            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }

            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }

            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }

            if (client.Player.Polymorphed)
            {
                client.SendMessage("You are polymorphed.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            if (client.Player.IsCasting)
            {
                client.SendMessage("You cannot do that while casting.");
                return;
            }

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            int slot = msg.ReadByte();

            if ((slot > 0) && (slot <= client.Player.Inventory.Length))
            {
                var item = client.Player.Inventory[slot - 1];

                if (item != null)
                {
                    if (item.Class != Profession.Peasant && item.Class != client.Player.Class && !client.Player.AdminRights.HasFlag(AdminRights.IgnoreClassRestrictions))
                    {
                        client.SendMessage("Your class cannot use this item.");
                        return;
                    }

                    if (client.Player.Level < item.Level && !client.Player.AdminRights.HasFlag(AdminRights.IgnoreLevelRestrictions))
                    {
                        client.SendMessage("Your level is too low to use this item.");
                        return;
                    }

                    if (client.Player.InCombat && item is Equipment)
                    {
                        client.SendMessage("You are in combat.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(item.RequiredSkillName))
                    {
                        var index = client.Player.SkillBook.IndexOf(item.RequiredSkillName);
                        if (index < 0)
                        {
                            client.SendMessage("You do not have the required skill for this item.");
                            return;
                        }
                        var skill = client.Player.SkillBook[index];
                        if (skill.Level < item.RequiredSkillLevel)
                        {
                            client.SendMessage("{0} must be level {1} to use this item.", skill.Name, item.RequiredSkillLevel);
                            return;
                        }
                    }

                    if (!string.IsNullOrEmpty(item.CooldownFamily))
                    {
                        var cooldowns = client.Player.ItemCooldowns;
                        if (cooldowns.ContainsKey(item.CooldownFamily))
                        {
                            var timeLeft = cooldowns[item.CooldownFamily].Subtract(DateTime.UtcNow).TotalSeconds;
                            if (timeLeft > 0)
                            {
                                client.SendMessage("You cannot use that item yet.");
                                return;
                            }
                            cooldowns[item.CooldownFamily] = DateTime.UtcNow.AddSeconds(item.CooldownLength);
                        }
                        else
                        {
                            cooldowns.Add(item.CooldownFamily, DateTime.UtcNow.AddSeconds(item.CooldownLength));
                        }
                    }

                    if (CompiledMethods.ContainsKey(item.InvokeMethod))
                    {
                        CompiledMethods[item.InvokeMethod].Invoke(null, new object[] { item, client.Player });
                    }

                    var dialog = item.Invoke(client.Player);
                    if (dialog != null)
                    {
                        dialog.GameObject = item;
                        if (dialog is DialogB)
                        {
                            client.Player.DialogSession.GameObject = item;
                            client.Player.DialogSession.Dialog = (DialogB)dialog;
                            client.Player.DialogSession.IsOpen = true;
                            client.Player.DialogSession.Map = client.Player.Map;
                        }
                        client.Enqueue(dialog.ToPacket());
                    }
                }
            }
        }
        private void MsgHandler_Emotion(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }
            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }
            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }
            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }

            if (client.Player.Polymorphed)
            {
                client.SendMessage("You are polymorphed.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            client.Player.CancelSpellCast();

            int id = msg.ReadByte();
            if (id <= 35)
            {
                foreach (var c in client.Player.Map.Objects)
                {
                    if (client.Player.WithinRange(c, 12) && (c is Player))
                        (c as Player).Client.BodyAnimation(client.Player.ID, (id + 9), 120);
                }
                client.BodyAnimation(client.Player.ID, (id + 9), 120);
            }
        }
        private void MsgHandler_DropGold(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            long value = msg.ReadUInt32();
            if ((value > 0) && (value <= client.Player.Gold))
            {
                int x = msg.ReadUInt16();
                int y = msg.ReadUInt16();

                if (client.Player.Map.Width <= x)
                    return;
                if (client.Player.Map.Height <= y)
                    return;
                if (Math.Abs(client.Player.Point.X - x) > 2)
                    return;
                if (Math.Abs(client.Player.Point.Y - y) > 2)
                    return;
                if (client.Player.Map.Walls[x, y])
                    return;

                Gold g = new Gold(value);
                client.Player.Gold -= value;
                this.InsertGameObject(g);
                client.Player.Map.InsertCharacter(g, x, y);
                client.SendStatistics(StatUpdateFlags.Experience);
            }
        }
        private void MsgHandler_ExchangeItem(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var slot = msg.ReadByte();
            var id = msg.ReadUInt32();

            var packet = new ServerPacket(0x4B);
            packet.WriteUInt16(0x0006);
            packet.WriteByte(0x4A);
            packet.WriteByte(0x00);
            packet.WriteUInt32(id);
            packet.WriteByte(0x00);
            client.Enqueue(packet);

            packet = new ServerPacket(0x4B);
            packet.WriteUInt16(0x0007);
            packet.WriteByte(0x4A);
            packet.WriteByte(0x01);
            packet.WriteUInt32(id);
            packet.WriteByte(slot);
            packet.WriteByte(0x00);
            client.Enqueue(packet);
        }
        private void MsgHandler_ExchangeGold(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var gold = msg.ReadUInt32();
            var id = msg.ReadUInt32();

            var packet = new ServerPacket(0x4B);
            packet.WriteUInt16(0x0006);
            packet.WriteByte(0x4A);
            packet.WriteByte(0x00);
            packet.WriteUInt32(id);
            packet.WriteByte(0x00);
            client.Enqueue(packet);

            packet = new ServerPacket(0x4B);
            packet.WriteUInt16(0x000A);
            packet.WriteByte(0x4A);
            packet.WriteByte(0x03);
            packet.WriteUInt32(id);
            packet.WriteUInt32(gold);
            packet.WriteByte(0x00);
            client.Enqueue(packet);
        }
        private void MsgHandler_RequestProfile(Client client, ClientPacket msg)
        {
            client.SendProfile();
        }
        private void MsgHandler_Group(Client client, ClientPacket msg)
        {
            switch (msg.ReadByte())
            {
                case 0x02:
                    {
                        string name = msg.ReadString(msg.ReadByte());
                        foreach (var c in client.Player.Map.Objects)
                        {
                            if (c is Player && c.Name == name && c.WithinRange(client.Player, 12))
                            {
                                var p = (c as Player);
                                if (!p.GroupToggle)
                                {
                                    client.SendMessage(string.Format("{0} does not wish to join your group.", c.Name));
                                }
                                else if (p.Group.HasMembers)
                                {
                                    if ((p.Group == client.Player.Group) && (p.Group.Leader == client.Player))
                                    {
                                        p.Group.RemoveMember(p);
                                    }
                                    else
                                    {
                                        client.SendMessage(string.Format("{0} is in another group.", p.Name));
                                    }
                                }
                                else if (client.Player.Group.HasMembers)
                                {
                                    if (client.Player.Group.Members.Count > 12)
                                    {
                                        client.SendMessage("Your group cannot accept anymore members.");
                                    }
                                    else
                                    {
                                        p.Group.Disband(false);
                                        client.Player.Group.AddMember(p);
                                    }
                                }
                                else
                                {
                                    p.Group.Disband(false);
                                    client.Player.Group.Disband(false);

                                    client.Player.GroupToggle = true;

                                    var group = new Group(this, string.Empty, client.Player);
                                    client.Player.Group = group;
                                    group.AddMember(p);
                                }
                                break;
                            }
                        }
                    } break;
                case 0x08: // map
                    {
                        string name = msg.ReadString(msg.ReadByte());
                        if (name != client.Player.Name)
                            client.Connected = false;
                        client.Player.MapOpen = msg.ReadBoolean();
                    } break;
            }
        }
        private void MsgHandler_ToggleGroup(Client client, ClientPacket msg)
        {
            client.Player.GroupToggle = !client.Player.GroupToggle;
            if (!client.Player.GroupToggle && (client.Player.Group.HasMembers))
                client.Player.Group.RemoveMember(client.Player);
            client.SendProfile();
        }
        private void MsgHandler_MoveItemSkillSpell(Client client, ClientPacket msg)
        {
            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            var pane = msg.ReadByte();
            var slotA = msg.ReadByte();
            var slotB = msg.ReadByte();

            if (slotA < 1 || slotB < 1)
                return;

            switch (pane)
            {
                case 0x00: // inventory
                    {
                        if (slotA > client.Player.Inventory.Length)
                            return;

                        if (slotB > client.Player.Inventory.Length)
                            return;

                        var currentItemA = client.Player.Inventory[slotA - 1];
                        var currentItemB = client.Player.Inventory[slotB - 1];

                        var itemA = client.Player.RemoveItem(slotA - 1, currentItemA != null ? currentItemA.Amount : 0);
                        var itemB = client.Player.RemoveItem(slotB - 1, currentItemB != null ? currentItemB.Amount : 0);

                        if (itemA != null)
                        {
                            InsertGameObject(itemA);
                            client.Player.AddItem(itemA, slotB - 1);
                        }

                        if (itemB != null)
                        {
                            InsertGameObject(itemB);
                            client.Player.AddItem(itemB, slotA - 1);
                        }
                    } break;
                case 0x01: // spellbook
                    {
                        if (slotA > client.Player.SpellBook.Length)
                            return;
                        if (slotB > client.Player.SpellBook.Length)
                            return;
                        Spell spellA = client.Player.RemoveSpell(slotA - 1);
                        Spell spellB = client.Player.RemoveSpell(slotB - 1);
                        if (spellA != null)
                            client.Player.AddSpell(spellA, slotB - 1);
                        if (spellB != null)
                            client.Player.AddSpell(spellB, slotA - 1);
                    } break;
                case 0x02: // skillbook
                    {
                        if (slotA > client.Player.SkillBook.Length)
                            return;
                        if (slotB > client.Player.SkillBook.Length)
                            return;
                        Skill skillA = client.Player.RemoveSkill(slotA - 1);
                        Skill skillB = client.Player.RemoveSkill(slotB - 1);
                        if (skillA != null)
                            client.Player.AddSkill(skillA, slotB - 1);
                        if (skillB != null)
                            client.Player.AddSkill(skillB, slotA - 1);
                    } break;
            }
        }
        private void MsgHandler_Refresh(Client client, ClientPacket msg)
        {
            if (DateTime.UtcNow.Subtract(client.Player.LastRefresh).TotalSeconds > 1)
            {
                client.Refresh();
            }
        }
        private void MsgHandler_DialogResponse(Client client, ClientPacket msg)
        {
            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }

            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }

            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var nds = client.Player.DialogSession;

            msg.ReadByte(); // umok!

            var goId = msg.ReadUInt32();
            var dialogId = msg.ReadUInt16(); // unused
            var dialogNumber = msg.ReadUInt16();

            var c = nds.GameObject;

            if ((nds.Map != null && nds.Map != client.Player.Map) || !nds.IsOpen)
                return;

            if (nds.Dialog is OptionDialog && dialogNumber == 2)
            {
                var od = (nds.Dialog as OptionDialog);
                msg.ReadByte();
                int option = msg.ReadByte();
                if (option < 1 || option > od.Options.Count)
                {
                    client.Connected = false;
                    return;
                }
                msg.Position -= 2;
            }

            switch (dialogNumber)
            {
                case 0: // back
                    {
                        if (nds.Dialog.CanGoBack)
                            nds.Dialog = nds.Dialog.Back(client.Player, msg);
                        else
                            nds.Dialog = nds.Dialog.Exit(client.Player, msg);
                    } break;
                case 1: // exit
                    {
                        if (nds.Dialog.CanClose)
                            nds.Dialog = nds.Dialog.Exit(client.Player, msg);
                    } break;
                case 2: // next
                    {
                        if (nds.Dialog.CanGoNext)
                            nds.Dialog = nds.Dialog.Next(client.Player, msg);
                        else
                            nds.Dialog = nds.Dialog.Exit(client.Player, msg);
                    } break;
            }

            if (nds.Dialog != null)
            {
                nds.IsOpen = true;
                nds.Dialog.GameObject = c;
                nds.Map = client.Player.Map;
                client.Enqueue(nds.Dialog.ToPacket());
            }
            else
            {
                nds.IsOpen = false;
                nds.GameObject = null;
                nds.Map = null;
                client.Enqueue(Dialog.ExitPacket());
            }
        }
        private void MsgHandler_CharacterClicked(Client client, ClientPacket msg)
        {
            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }

            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }

            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            switch (msg.ReadByte())
            {
                case 1:
                    {
                        var id = msg.ReadUInt32();
                        if (GameObjectsID.ContainsKey(id))
                        {
                            var c = GameObject<Character>(id);
                            if (c.WithinRange(client.Player, 12))
                                c.OnClick(client);
                        }
                    } break;
                case 3:
                    {
                        var x = msg.ReadInt16();
                        var y = msg.ReadInt16();
                        var map = client.Player.Map;
                        if ((map.Width > x) && (map.Height > y))
                        {
                            //if (map.Doors[x, y] != null)
                            //{
                            //    map.Doors[x, y].IsOpen = !map.Doors[x, y].IsOpen;
                            //    map.Walls[x, y] = !map.Doors[x, y].IsOpen;
                            //    foreach (var c in map.Characters)
                            //    {
                            //        if (c is Player)
                            //        {
                            //            var p = (c as Player);
                            //            if (p.Point.DistanceFrom(x, y) <= 12)
                            //            {
                            //                p.Client.ToggleDoor(map.Doors[x, y]);
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    } break;
            }
        }
        private void MsgHandler_RemoveEquipment(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }
            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }
            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }
            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            int slot = msg.ReadByte();
            if (slot > 0 && slot <= client.Player.Equipment.Length)
            {
                if (client.Player.Equipment[slot - 1] != null)
                {
                    int index = client.Player.FindEmptyInventoryIndex();
                    if (index > -1)
                    {
                        Item item = client.Player.RemoveEquipment(slot - 1);
                        client.Player.AddItem(item, index);
                        client.Player.Display();
                    }
                }
            }
        }
        private void MsgHandler_UseStatPoint(Client client, ClientPacket msg)
        {
            if (client.Player.Dead)
            {
                client.SendMessage("You can't do that while dead.");
                return;
            }

            if (client.Player.AvailableStatPoints > 0)
            {
                switch (msg.ReadByte())
                {
                    case 0x01:
                        client.Player.BaseStr++;
                        break;
                    case 0x04:
                        client.Player.BaseInt++;
                        break;
                    case 0x08:
                        client.Player.BaseWis++;
                        break;
                    case 0x10:
                        client.Player.BaseCon++;
                        break;
                    case 0x02:
                        client.Player.BaseDex++;
                        break;
                    default: return;
                }
                client.Player.AvailableStatPoints--;
                client.SendStatistics(StatUpdateFlags.Primary);
            }
        }
        private void MsgHandler_Exchange(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var type = msg.ReadByte();
            var id = msg.ReadUInt32();

            if (GameObjectsID.ContainsKey(id) && (GameObjectsID[id] is Player))
            {
                var player = client.Player;
                var trader = GameObjectsID[id] as Player;

                switch (type)
                {
                    case 0x00: // open exchange
                        {
                            if (player.ExchangeInfo != null)
                            {
                                client.SendMessage("You are already in the middle of an exchange.");
                                return;
                            }

                            if (trader.ExchangeInfo != null)
                            {
                                client.SendMessage("That person is busy right now.");
                                return;
                            }

                            if (trader.DialogSession.IsOpen)
                            {
                                client.SendMessage("That person is busy right now.");
                                return;
                            }

                            if (player.Map != trader.Map)
                            {
                                client.SendMessage("That person is too far away");
                                return;
                            }

                            if (Math.Abs(player.Point.X - trader.Point.X) > 2)
                            {
                                client.SendMessage("That person is too far away");
                                return;
                            }

                            if (Math.Abs(player.Point.Y - trader.Point.Y) > 2)
                            {
                                client.SendMessage("That person is too far away");
                                return;
                            }

                            player.ExchangeInfo = new Exchange(trader);
                            trader.ExchangeInfo = new Exchange(player);

                            var packet = new ServerPacket(0x42);
                            packet.WriteByte(0x00);
                            packet.WriteUInt32(trader.ID);
                            packet.WriteString8(trader.Name);
                            client.Enqueue(packet);

                            packet = new ServerPacket(0x42);
                            packet.WriteByte(0x00);
                            packet.WriteUInt32(player.ID);
                            packet.WriteString8(player.Name);
                            trader.Client.Enqueue(packet);
                        } break;
                    case 0x01: // add item
                        {
                            if (player.ExchangeInfo == null)
                                return;

                            if (trader.ExchangeInfo == null)
                                return;

                            if (player.ExchangeInfo.Trader != trader)
                                return;

                            if (trader.ExchangeInfo.Trader != player)
                                return;

                            if (player.ExchangeInfo.Confirmed)
                                return;

                            var index = (msg.ReadByte() - 1);
                            if (index < 0 || index > player.Inventory.Length)
                                return;

                            var item = player.Inventory[index];
                            if (item == null)
                                return;

                            if (item.Soulbound || item.BindType == BindType.BindToAccount)
                            {
                                client.SendMessage("You can't exchange that item.");
                                return;
                            }

                            if (item.Weight + player.ExchangeInfo.Weight > trader.AvailableWeight)
                            {
                                client.SendMessage("That item is too heavy.");
                                return;
                            }

                            var newItem = player.RemoveItem(index);
                            player.ExchangeInfo.Items.Add(newItem);
                            player.ExchangeInfo.Weight += item.Weight;

                            var packet = new ServerPacket(0x42);
                            packet.WriteByte(0x02);
                            packet.WriteByte(0x00);
                            packet.WriteByte((byte)player.ExchangeInfo.Items.Count);
                            packet.WriteUInt16((ushort)(item.Sprite + 0x8000));
                            packet.WriteUInt16((ushort)item.Color);
                            packet.WriteByte(0x00);
                            packet.WriteString8(item.Name);
                            client.Enqueue(packet);

                            packet = new ServerPacket(0x42);
                            packet.WriteByte(0x02);
                            packet.WriteByte(0x01);
                            packet.WriteByte((byte)player.ExchangeInfo.Items.Count);
                            packet.WriteUInt16((ushort)(item.Sprite + 0x8000));
                            packet.WriteUInt16((ushort)item.Color);
                            packet.WriteByte(0x00);
                            packet.WriteString8(item.Name);
                            trader.Client.Enqueue(packet);
                        } break;
                    case 0x03: // add gold
                        {
                            if (player.ExchangeInfo == null)
                                return;

                            if (trader.ExchangeInfo == null)
                                return;

                            if (player.ExchangeInfo.Trader != trader)
                                return;

                            if (trader.ExchangeInfo.Trader != player)
                                return;

                            if (player.ExchangeInfo.Confirmed)
                                return;

                            var gold = msg.ReadUInt32();
                            if (gold > player.Gold)
                                return;

                            if (player.ExchangeInfo.Gold != 0)
                                return;

                            player.Gold -= gold;
                            player.Client.SendStatistics(StatUpdateFlags.Experience);
                            player.ExchangeInfo.Gold = gold;

                            var packet = new ServerPacket(0x42);
                            packet.WriteByte(0x03);
                            packet.WriteByte(0x00);
                            packet.WriteUInt32(gold);
                            client.Enqueue(packet);

                            packet = new ServerPacket(0x42);
                            packet.WriteByte(0x03);
                            packet.WriteByte(0x01);
                            packet.WriteUInt32(gold);
                            trader.Client.Enqueue(packet);
                        } break;
                    case 0x04: // cancel
                        {
                            if (player.ExchangeInfo == null)
                                return;

                            if (trader.ExchangeInfo == null)
                                return;

                            if (player.ExchangeInfo.Trader != trader)
                                return;

                            if (trader.ExchangeInfo.Trader != player)
                                return;

                            player.CancelExchange();
                        } break;
                    case 0x05: // confirm
                        {
                            if (player.ExchangeInfo == null)
                                return;

                            if (trader.ExchangeInfo == null)
                                return;

                            if (player.ExchangeInfo.Trader != trader)
                                return;

                            if (trader.ExchangeInfo.Trader != player)
                                return;

                            if (player.ExchangeInfo.Confirmed)
                                return;

                            player.ExchangeInfo.Confirmed = true;

                            if (trader.ExchangeInfo.Confirmed)
                                player.FinishExchange();

                            var packet = new ServerPacket(0x42);
                            packet.WriteByte(0x05);
                            packet.WriteByte(0x00);
                            packet.WriteString8("You exchanged");
                            client.Enqueue(packet);

                            packet = new ServerPacket(0x42);
                            packet.WriteByte(0x05);
                            packet.WriteByte(0x01);
                            packet.WriteString8("You exchanged");
                            trader.Client.Enqueue(packet);
                        } break;
                }
            }
        }
        private void MsgHandler_StartSpellCast(Client client, ClientPacket msg)
        {
            if (client.Player.Polymorphed)
            {
                client.SendMessage("You are polymorphed.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            client.Player.IsCasting = true;
            client.Player.StartCastTime = DateTime.UtcNow;
            client.Player.NextSpellLines = msg.ReadByte();
        }
        private void MsgHandler_Chant(Client client, ClientPacket msg)
        {
            string message = msg.ReadString(msg.ReadByte());
            client.Player.Say(message, 2);
        }
        private void MsgHandler_Manufacture(Client client, ClientPacket msg)
        {
            msg.ReadUInt16();
            var type = msg.ReadByte();
            switch (type)
            {
                case 0x00: // request item
                    {
                        var index = msg.ReadByte();

                        var items = new string[client.Player.CurrentManufactures.Count];
                        client.Player.CurrentManufactures.CopyTo(items);

                        var name = items[index];

                        if (ManufactureDatabase.ContainsKey(name))
                        {
                            var m = ManufactureDatabase[name];
                            var item = ItemDatabase[m.Item];
                            var ingredients = new string[m.Ingredients.Count];
                            int i = 0;
                            foreach (var ingredient in m.Ingredients)
                            {
                                var ingitem = ItemDatabase[ingredient.Key];
                                ingredients[i] = string.Format("{0} ({1})", ingitem.Name, ingredient.Value);
                                i++;
                            }

                            var packet = new ServerPacket(0x50);
                            packet.WriteByte(0x01);
                            packet.WriteByte(0x3C);
                            packet.WriteByte(0x01);
                            packet.WriteByte(index);
                            packet.WriteUInt16((ushort)(0x8000 + item.Sprite));
                            packet.WriteString8(item.Name);
                            packet.WriteString16(m.Description);
                            packet.WriteString16("Ingredients: " + string.Join(", ", ingredients));
                            packet.WriteByte(0x01);
                            packet.WriteByte(byte.MinValue);
                            client.Enqueue(packet);
                        }
                        else
                        {
                            client.Player.CurrentManufactures.Remove(name);
                            client.Player.AvailableManufactures.Remove(name);
                        }
                    } break;
                case 0x01:
                    {
                        if (client.Player.CurrentManufacture != null)
                            return;

                        var name = msg.ReadString(msg.ReadByte());

                        var items = new string[client.Player.CurrentManufactures.Count];
                        client.Player.CurrentManufactures.CopyTo(items);

                        foreach (var i in items)
                        {
                            if (ManufactureDatabase.ContainsKey(i))
                            {
                                var m = ManufactureDatabase[i];
                                var item = ItemDatabase[m.Item];
                                if (item.Name == name && m.CanManufacture(client.Player))
                                {
                                    client.Player.CurrentManufacture = m;
                                    client.Player.ManufactureStart = DateTime.UtcNow;
                                    var packet = new ServerPacket(0x51);
                                    packet.WriteByte(0x00);
                                    packet.WriteByte(0x0A);
                                    packet.WriteByte(0x00);
                                    client.Enqueue(packet);
                                    break;
                                }
                            }
                            else
                            {
                                client.Player.CurrentManufactures.Remove(i);
                                client.Player.AvailableManufactures.Remove(i);
                            }
                        }
                    } break;
            }
        }
        private void MsgHandler_Title(Client client, ClientPacket msg)
        {
            var title = msg.ReadByte();
            if (TitleDatabase[title].Available(client.Player))
            {
                client.Player.Title = title;
                client.SendProfile();
            }
        }
        private void MsgHandler_RequestDialog(Client client, ClientPacket msg)
        {
            if (client.Player.Sleeping)
            {
                client.SendMessage("You can't do that while asleep.");
                return;
            }

            if (client.Player.Coma)
            {
                client.SendMessage("You can't do that while in a coma.");
                return;
            }

            if (client.Player.Frozen)
            {
                client.SendMessage("You can't do that while frozen.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var nds = client.Player.DialogSession;

            if (nds.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            msg.ReadByte(); // umok!

            var npc = GameObject<Merchant>(msg.ReadUInt32());
            var dialogId = msg.ReadUInt16();

            if (npc.Map != client.Player.Map || nds.IsOpen)
                return;

            DialogMenuOption dmo = null;

            if (dialogId < Dialog.DIALOG_GLOBAL_MAX)
            {
                switch (dialogId)
                {
                    #region Buy
                    case Dialog.DIALOG_BUY_01:
                        {
                            dmo = new Buy_1();
                        } break;
                    case Dialog.DIALOG_BUY_02:
                        {
                            dmo = new Buy_2();
                        } break;
                    #endregion
                    #region Sell
                    case Dialog.DIALOG_SELL_01:
                        {
                            dmo = new Sell_1();
                        } break;
                    case Dialog.DIALOG_SELL_02:
                        {
                            dmo = new Sell_2();
                        } break;
                    case Dialog.DIALOG_SELL_03:
                        {
                            dmo = new Sell_3();
                        } break;
                    case Dialog.DIALOG_SELL_04:
                        {
                            dmo = new Sell_4();
                        } break;
                    #endregion
                    #region Deposit
                    case Dialog.DIALOG_DEPOSIT_01:
                        {
                            dmo = new Deposit_1();
                        } break;
                    case Dialog.DIALOG_DEPOSIT_02:
                        {
                            dmo = new Deposit_2();
                        } break;
                    #endregion
                    #region Withdraw
                    case Dialog.DIALOG_WITHDRAW_01:
                        {
                            dmo = new Withdraw_1();
                        } break;
                    case Dialog.DIALOG_WITHDRAW_02:
                        {
                            dmo = new Withdraw_2();
                        } break;
                    #endregion
                    #region Learn Skill
                    case Dialog.DIALOG_LEARNSKILL_01:
                        {
                            dmo = new LearnSkill_1();
                        } break;
                    case Dialog.DIALOG_LEARNSKILL_02:
                        {
                            dmo = new LearnSkill_2();
                        } break;
                    case Dialog.DIALOG_LEARNSKILL_03:
                        {
                            dmo = new LearnSkill_3();
                        } break;
                    #endregion
                    #region Learn Spell
                    case Dialog.DIALOG_LEARNSPELL_01:
                        {
                            dmo = new LearnSpell_1();
                        } break;
                    case Dialog.DIALOG_LEARNSPELL_02:
                        {
                            dmo = new LearnSpell_2();
                        } break;
                    case Dialog.DIALOG_LEARNSPELL_03:
                        {
                            dmo = new LearnSpell_3();
                        } break;
                    #endregion
                    #region Forget Skill
                    case Dialog.DIALOG_FORGETSKILL_01:
                        {
                            dmo = new ForgetSkill_1();
                        } break;
                    case Dialog.DIALOG_FORGETSKILL_02:
                        {
                            dmo = new ForgetSkill_2();
                        } break;
                    #endregion
                    #region Forget Spell
                    case Dialog.DIALOG_FORGETSPELL_01:
                        {
                            dmo = new ForgetSpell_1();
                        } break;
                    case Dialog.DIALOG_FORGETSPELL_02:
                        {
                            dmo = new ForgetSpell_2();
                        } break;
                    #endregion
                    #region Upgrade Skill
                    case Dialog.DIALOG_UPGRADESKILL_01:
                        {
                            dmo = new UpgradeSkill_1();
                        } break;
                    case Dialog.DIALOG_UPGRADESKILL_02:
                        {
                            dmo = new UpgradeSkill_2();
                        } break;
                    case Dialog.DIALOG_UPGRADESKILL_03:
                        {
                            dmo = new UpgradeSkill_3();
                        } break;
                    #endregion
                    #region Upgrade Spell
                    case Dialog.DIALOG_UPGRADESPELL_01:
                        {
                            dmo = new UpgradeSpell_1();
                        } break;
                    case Dialog.DIALOG_UPGRADESPELL_02:
                        {
                            dmo = new UpgradeSpell_2();
                        } break;
                    case Dialog.DIALOG_UPGRADESPELL_03:
                        {
                            dmo = new UpgradeSpell_3();
                        } break;
                    #endregion
                }
            }
            else
            {
                dialogId -= Dialog.DIALOG_GLOBAL_MAX;
                var key = npc.DialogMenuOptions[dialogId];
                dmo = DialogMenuOptionDatabase[key];
            }

            if (dmo.CanOpen(client.Player))
            {
                if (dmo is QuestMenuOption)
                {
                    var qmo = dmo as QuestMenuOption;
                    var quest = client.Player.Quests[qmo.QuestType];
                    if (quest.Progress == QuestProgress.Finished ||
                        quest.QuestStep.Progress < qmo.MinimumProgress ||
                        quest.QuestStep.Progress > qmo.MaximumProgress ||
                        quest.CurrentStep != qmo.QuestStep ||
                        client.Player.Level < quest.MinimumLevel ||
                        client.Player.Level > quest.MaximumLevel ||
                        !quest.Prerequisites.TrueForAll(req => client.Player.Quests[req].Progress == QuestProgress.Finished))
                        return;
                }

                var dialog = dmo.Open(client.Player, npc, msg);
                if (dialog != null)
                {
                    dialog.GameObject = npc;
                    if (dialog is DialogB)
                    {
                        nds.GameObject = npc;
                        nds.Dialog = (DialogB)dialog;
                        nds.IsOpen = true;
                        nds.Map = client.Player.Map;
                    }
                }
                client.Enqueue((dialog != null) ? dialog.ToPacket() : Dialog.ExitPacket());
            }
        }
        private void MsgHandler_MessageBoards(Client client, ClientPacket msg)
        {
            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }

            var p = new ServerPacket(0x31);
            p.WriteByte(0x01);
            p.WriteUInt16(0x00);
            p.WriteByte(0x0);
            client.Enqueue(p);
        }
        private void MsgHandler_UseSkill(Client client, ClientPacket msg)
        {
            if (client.Player.Polymorphed)
            {
                client.SendMessage("You are polymorphed.");
                return;
            }

            if (client.Player.ExchangeInfo != null)
            {
                client.SendMessage("You can't do that in the middle of an exchange.");
                return;
            }

            if (client.Player.DialogSession.IsOpen)
            {
                client.SendMessage("You are busy right now.");
                return;
            }

            var slot = msg.ReadByte();

            var skill = client.Player.SkillBook[slot - 1];

            if (client.Player.SkillPoints < skill.SkillPointCost)
                return;

            client.Player.CancelSpellCast();

            var statuses = new Spell[client.Player.Statuses.Count];
            client.Player.Statuses.Values.CopyTo(statuses, 0);
            foreach (var status in statuses)
            {
                if (status.Channeled)
                    client.Player.RemoveStatus(status.StatusName);
            }
            
            client.Player.UseSkill(skill);

            client.Player.SkillPoints -= skill.SkillPointCost;
        }
        private void MsgHandler_Logoff(Client client, ClientPacket msg)
        {
            if (client.Player != null)
            {
                var player = client.Player;

                if (player.ExchangeInfo != null)
                    player.CancelExchange();

                if (player.Group.HasMembers)
                    player.Group.RemoveMember(player);

                player.CancelSpellCast();

                var equipment = new Equipment[player.Equipment.Length];
                player.Equipment.CopyTo(equipment, 0);
                foreach (var item in equipment)
                {
                    if (item != null && item.DropOnLogoff)
                    {
                        client.Player.RemoveEquipment(item);
                        client.Player.Map.InsertCharacter(item, client.Player.Point);
                    }
                }

                var statuses = new Spell[player.Statuses.Count];
                player.Statuses.Values.CopyTo(statuses, 0);
                foreach (var status in statuses)
                {
                    if (status.Channeled || status.RequiresCaster || status.SingleTarget)
                        player.RemoveStatus(status.StatusName);
                }

                var singleStatuses = new string[player.SingleTargetSpells.Count];
                player.SingleTargetSpells.Keys.CopyTo(singleStatuses, 0);
                foreach (var status in singleStatuses)
                {
                    var character = player.SingleTargetSpells[status];
                    character.RemoveStatus(status);
                }

                player.Save();

                statuses = new Spell[player.Statuses.Count];
                player.Statuses.Values.CopyTo(statuses, 0);
                foreach (var status in statuses)
                {
                    player.RemoveStatus(status.StatusName);
                }

                RemoveGameObject(player);
                client.Player = null;

                var p = new ServerPacket(0x4C);
                p.WriteByte(0x01);
                p.WriteUInt16(0x00);
                client.Enqueue(p);

                Redirection r = new Redirection();
                r.DestinationServer = LoginServer;
                r.EncryptionParameters = client.EncryptionParams;
                r.Name = player.Name;
                r.SourceServer = this;
                client.Redirect(r);
            }
        }
        private void MsgHandler_Chat(Client client, ClientPacket msg)
        {
            bool shout = msg.ReadBoolean();
            string message = msg.ReadString(msg.ReadByte());
            if (message.StartsWith("/") && client.Player.AdminRights.HasFlag(AdminRights.CanUseCommands))
            {
                #region Commands
                string[] parameters = message.Split(' ');
                switch (parameters[0].ToLower())
                {
                    case "/cursor":
                        {
                            int mode;
                            if (int.TryParse(parameters[1], out mode))
                                client.Player.Cursor(mode);
                        } break;
                    case "/tileinfo":
                        {
                            var map = client.Player.Map;
                            int x = client.Player.Point.X;
                            int y = client.Player.Point.Y;
                            client.SendMessage(string.Format("BG: {0},  LFG: {1},  RFG: {2}",
                                map.GetBackground(x, y), map.GetLeftForeground(x, y), map.GetRightForeground(x, y)
                                ));
                        } break;
                    case "/setbg":
                        {
                            var map = client.Player.Map;
                            int x = client.Player.Point.X;
                            int y = client.Player.Point.Y;
                            int value;
                            if (int.TryParse(parameters[1], out value))
                                map.SetBackground(x, y, value);
                        } break;
                    case "/setlfg":
                        {
                            var map = client.Player.Map;
                            int x = client.Player.Point.X;
                            int y = client.Player.Point.Y;
                            int value;
                            if (int.TryParse(parameters[1], out value))
                                map.SetLeftForeground(x, y, value);
                        } break;
                    case "/setrfg":
                        {
                            var map = client.Player.Map;
                            int x = client.Player.Point.X;
                            int y = client.Player.Point.Y;
                            int value;
                            if (int.TryParse(parameters[1], out value))
                                map.SetRightForeground(x, y, value);
                        } break;
                    case "/title":
                        {
                            if (parameters.Length > 1)
                            {
                                byte title;
                                if (byte.TryParse(parameters[1], out title) && (title < 3))
                                    client.Player.Title = title;
                            }
                        } break;
                    case "/class":
                        {
                            if (parameters.Length > 1)
                            {
                                byte profession;
                                if (byte.TryParse(parameters[1], out profession) && (profession < 6))
                                    client.Player.Class = (Profession)profession;
                            }
                        } break;
                    case "/spec":
                        {
                            if (parameters.Length > 1)
                            {
                                byte spec;
                                if (byte.TryParse(parameters[1], out spec) && (spec < 5))
                                    client.Player.Specialization = (Specialization)spec;
                            }
                        } break;
                    case "/loadscript":
                        {
                            LoadScripts();
                        } break;
                    case "/sound":
                        {
                            if (parameters.Length == 2)
                            {
                                byte sound;
                                if (byte.TryParse(parameters[1], out sound))
                                    client.Player.SoundEffect(sound);
                            }
                        } break;
                    case "/effect":
                        {
                            if (parameters.Length == 2)
                            {
                                int effect;
                                if (int.TryParse(parameters[1], out effect))
                                {
                                    client.Player.SpellAnimation(effect, 100);
                                }
                            }
                        } break;
                    case "/animation":
                        {
                            if (parameters.Length == 2)
                            {
                                int animation;
                                if (int.TryParse(parameters[1], out animation))
                                {
                                    client.Player.BodyAnimation(animation, 20);
                                }
                            }
                        } break;
                    case "/minigame":
                        {
                            if (parameters.Length == 3)
                            {
                                int value1;
                                int value2;
                                if (int.TryParse(parameters[1], out value1) && int.TryParse(parameters[2], out value2))
                                {
                                    client.SendMiniGame(value1, value2);
                                }
                            }
                        } break;
                    case "/emblem1":
                        {
                            if (parameters.Length == 2)
                            {
                                int value;
                                if (int.TryParse(parameters[1], out value))
                                {
                                    client.Player.EmblemA = value;
                                    client.Player.Display();
                                }
                            }
                        } break;
                    case "/emblem2":
                        {
                            if (parameters.Length == 2)
                            {
                                int value;
                                if (int.TryParse(parameters[1], out value))
                                {
                                    client.Player.EmblemB = value;
                                    client.Player.Display();
                                }
                            }
                        } break;
                    default:
                        {
                            client.SendMessage("Unknown command!");
                        } break;
                }
                #endregion
            }
            else
            {
                if (DateTime.UtcNow.Subtract(client.Player.LastMessage).TotalSeconds < 0.25)
                    return;

                var match = Regex.Match(message, "^([A-Za-z0-9 ]+)( add dialog )([A-Za-z_][A-Za-z0-9_]*)$");
                if (match.Success && client.Player.AdminRights.HasFlag(AdminRights.CanUseCommands))
                {
                    var name = match.Groups[1].ToString();
                    var type = match.Groups[3].ToString().Replace(' ', '_');
                    if (DialogMenuOptionTypes.ContainsKey(type))
                    {
                        foreach (var obj in client.Player.Map.Objects)
                        {
                            var npc = obj as Merchant;
                            if (npc != null && npc.Name == name)
                            {
                                if (!npc.DialogMenuOptions.Contains(type))
                                {
                                    npc.DialogMenuOptions.Add(type);
                                    npc.Say(string.Format("I am now managing {0}.", type), 0);
                                }
                            }
                        }
                    }
                }

                if (shout)
                {
                    if (client.Player.AdminRights.HasFlag(AdminRights.CanGlobalShout))
                    {
                        BroadcastMessage("{0}! {1}", client.Player.Name, message);
                    }
                    else if (client.Player.AdminRights.HasFlag(AdminRights.ArenaHost))
                    {
                        client.Player.Say(message, 1);
                        client.Player.Map.BroadcastMessage("{0}! {1}", client.Player.Name, message);
                    }
                    else
                    {
                        client.Player.Say(message, 1);
                    }
                }
                else
                {
                    client.Player.Say(message, 0);
                }

                var nds = client.Player.DialogSession;

                var objects = new VisibleObject[client.Player.Map.Objects.Count];
                client.Player.Map.Objects.CopyTo(objects);

                foreach (var obj in objects)
                {
                    if (obj is Merchant && client.Player.Point.DistanceFrom(obj.Point) <= 12)
                    {
                        var npc = obj as Merchant;
                        foreach (var key in npc.DialogMenuOptions)
                        {
                            var dmo = DialogMenuOptionDatabase[key];
                            if (dmo.CanOpen(client.Player) && !(dmo is QuestMenuOption) && !nds.IsOpen && message.ToLower().Contains(dmo.ChatTrigger.ToLower()) && !string.IsNullOrEmpty(dmo.ChatTrigger))
                            {
                                var dialog = dmo.Open(client.Player, npc, null);
                                if (dialog != null)
                                {
                                    dialog.GameObject = npc;
                                    if (dialog is DialogB)
                                    {
                                        nds.GameObject = npc;
                                        nds.Dialog = (DialogB)dialog;
                                        nds.IsOpen = true;
                                        nds.Map = client.Player.Map;
                                    }
                                }
                                client.Enqueue((dialog != null) ? dialog.ToPacket() : Dialog.ExitPacket());
                            }
                        }
                    }
                }

                client.Player.LastMessage = DateTime.UtcNow;
            }
        }
        private void MsgHandler_Bag(Client client, ClientPacket msg)
        {
            var player = client.Player;

            var what = msg.ReadByte();
            var type = msg.ReadByte();
            if (what == 0x05 && type == 0x00)
            {
                var dir = msg.ReadByte();
                var slotA = msg.ReadByte();
                var slotB = msg.ReadByte();
                switch (dir)
                {
                    case 0x00: // inv -> bag
                        {
                            if (slotA > client.Player.Inventory.Length)
                                return;

                            if (slotB > client.Player.BagItems.Length)
                                return;

                            var currentItemA = client.Player.Inventory[slotA - 1];
                            var currentItemB = client.Player.BagItems[slotB - 1];

                            var itemA = client.Player.RemoveItem(slotA - 1, currentItemA != null ? currentItemA.Amount : 0);
                            var itemB = client.Player.RemoveBagItem(slotB - 1, currentItemB != null ? currentItemB.Amount : 0);

                            if (itemA != null)
                            {
                                InsertGameObject(itemA);
                                client.Player.AddBagItem(itemA, slotB - 1);
                            }

                            if (itemB != null)
                            {
                                InsertGameObject(itemB);
                                client.Player.AddItem(itemB, slotA - 1);
                            }
                        } break;
                    case 0x01: // bag -> inv
                        {
                            if (slotA > client.Player.BagItems.Length)
                                return;

                            if (slotB > client.Player.Inventory.Length)
                                return;

                            var currentItemA = client.Player.BagItems[slotA - 1];
                            var currentItemB = client.Player.Inventory[slotB - 1];

                            var itemA = client.Player.RemoveBagItem(slotA - 1, currentItemA != null ? currentItemA.Amount : 0);
                            var itemB = client.Player.RemoveItem(slotB - 1, currentItemB != null ? currentItemB.Amount : 0);

                            if (itemA != null)
                            {
                                InsertGameObject(itemA);
                                client.Player.AddItem(itemA, slotB - 1);
                            }

                            if (itemB != null)
                            {
                                InsertGameObject(itemB);
                                client.Player.AddBagItem(itemB, slotA - 1);
                            }
                        } break;
                    case 0x02: // bag -> bag
                        {
                            if (slotA > client.Player.BagItems.Length)
                                return;

                            if (slotB > client.Player.BagItems.Length)
                                return;

                            var currentItemA = client.Player.BagItems[slotA - 1];
                            var currentItemB = client.Player.BagItems[slotB - 1];

                            var itemA = client.Player.RemoveBagItem(slotA - 1, currentItemA != null ? currentItemA.Amount : 0);
                            var itemB = client.Player.RemoveBagItem(slotB - 1, currentItemB != null ? currentItemB.Amount : 0);

                            if (itemA != null)
                            {
                                InsertGameObject(itemA);
                                client.Player.AddBagItem(itemA, slotB - 1);
                            }

                            if (itemB != null)
                            {
                                InsertGameObject(itemB);
                                client.Player.AddBagItem(itemB, slotA - 1);
                            }
                        } break;
                }
            }
        }
        private void MsgHandler_RequestWebsite(Client client, ClientPacket msg)
        {
            var packet = new ServerPacket(0x62);
            //packet.WriteString16(string.Format("http://game.wewladh.com:2600/index.html?gs={0}&id={1}&sk={2}",
            packet.WriteString16(string.Format("http://www.wewladh.com/bbs/",
                Index, client.Player.ID, client.Player.SecureKey
                ));
            packet.WriteUInt16(0x00);
            client.Enqueue(packet);
        }
        private void MsgHandler_PlayerStatus(Client client, ClientPacket msg)
        {
            int status = msg.ReadByte();
            client.Player.Status = (status < 8) ? status : 0;
        }
        private void MsgHandler_MiniGame(Client client, ClientPacket msg)
        {
            int gameId = msg.ReadByte();
            int gameState = msg.ReadByte();
            bool result = msg.ReadBoolean();

            if (MiniGameDatabase.ContainsKey(gameId))
            {
                var mg = MiniGameDatabase[gameId];

                var p = client.Player;

                if (mg.RequiredItem.Count > 0 && (!p.Inventory.Contains(p.MiniGameItem) || !mg.RequiredItem.Contains(p.MiniGameItem)))
                {
                    client.SendMessage("You do not have the correct item.");
                    return;
                }

                if (mg.RequiredWeapon.Count > 0 && (p.Weapon == null || !mg.RequiredWeapon.Contains(p.Weapon.GetType().Name)))
                {
                    client.SendMessage("You do not have the correct weapon.");
                    return;
                }

                int index = client.Player.Inventory.IndexOf(client.Player.MiniGameItem);

                switch (gameState)
                {
                    case 2: // game complete
                        {
                            if (result)
                                mg.OnWin(client.Player);
                            else
                                mg.OnLose(client.Player);
                            client.Player.RemoveItem(index, 1);
                        } break;
                    case 3: // bad location
                        {
                            client.SendMessage("You cannot use that here.");
                        } break;
                }

                p.MiniGameItem = null;
            }
        }
        private void MsgHandler_WorldMap(Client client, ClientPacket msg)
        {
            if (client.Player.WorldMap.IsOpen)
            {
                int index = msg.ReadInt32();
                var node = client.Player.WorldMap.Nodes[index];
                var map = MapDatabase[node.MapTypeName];
                client.Player.Map.RemoveCharacter(client.Player);
                map.InsertCharacter(client.Player, node.MapX, node.MapY);
                client.Player.WorldMap.IsOpen = false;
            }
        }
        private void MsgHandler_DisplayBitmask(Client client, ClientPacket msg)
        {
            client.Player.DisplayBitmask = msg.ReadUInt16();
            client.Player.Display();
        }
        private void MsgHandler_Quest(Client client, ClientPacket msg)
        {
            var type = msg.ReadByte();
            switch (type)
            {
                case 0x02: // abandon
                    {
                        var questId = msg.ReadUInt16();
                        foreach (var quest in client.Player.Quests)
                        {
                            if (quest.Value.ID == questId)
                                client.Player.AbandonQuest(quest.Key);
                        }
                    } break;
            }
        }
        #endregion

        #region Instance Creating
        public Reactor CreateReactor(string name)
        {
            name = name.Replace(' ', '_');
            if (ReactorTypes.ContainsKey(name))
            {
                Reactor npc = (Reactor)Activator.CreateInstance(ReactorTypes[name]);
                return npc;
            }
            return null;
        }
        public Monster CreateMonster(string name)
        {
            name = name.Replace(' ', '_');
            if (NpcTypes.ContainsKey(name))
            {
                Monster npc = (Monster)Activator.CreateInstance(NpcTypes[name]);
                npc.CurrentHP = npc.BaseMaximumHP;
                npc.CurrentMP = npc.BaseMaximumMP;
                return npc;
            }
            return null;
        }
        public Merchant CreateMerchant(string name)
        {
            name = name.Replace(' ', '_');
            if (NpcTypes.ContainsKey(name))
            {
                Merchant npc = (Merchant)Activator.CreateInstance(NpcTypes[name]);
                npc.CurrentHP = npc.BaseMaximumHP;
                npc.CurrentMP = npc.BaseMaximumMP;
                return npc;
            }
            return null;
        }
        public Item CreateItem(string name)
        {
            name = name.Replace(' ', '_');
            if (ItemTypes.ContainsKey(name))
            {
                Item item = (Item)Activator.CreateInstance(ItemTypes[name]);
                item.CurrentDurability = item.MaximumDurability;
                item.Amount = 1;
                InsertGameObject(item);
                return item;
            }
            return null;
        }
        public Skill CreateSkill(string name)
        {
            name = name.Replace(' ', '_');
            if (SkillTypes.ContainsKey(name))
            {
                var skill = (Skill)Activator.CreateInstance(SkillTypes[name]);
                InsertGameObject(skill);
                return skill;
            }
            return null;
        }
        public Spell CreateSpell(string name)
        {
            name = name.Replace(' ', '_');
            if (SpellTypes.ContainsKey(name))
            {
                Spell spell = (Spell)Activator.CreateInstance(SpellTypes[name]);
                spell.TimeLeft = spell.Ranks[0].Duration;
                spell.NextTick = DateTime.UtcNow.AddSeconds(1);
                InsertGameObject(spell);
                return spell;
            }
            return null;
        }
        public Quest CreateQuest(string name)
        {
            name = name.Replace(' ', '_');
            if (QuestTypes.ContainsKey(name))
            {
                Quest quest = (Quest)Activator.CreateInstance(QuestTypes[name]);
                quest.ID = QuestDatabase[name].ID;
                return quest;
            }
            return null;
        }
        public LegendMark CreateLegendMark(string name)
        {
            if (LegendMarkTypes.ContainsKey(name))
            {
                return (LegendMark)Activator.CreateInstance(LegendMarkTypes[name]);
            }
            return null;
        }
        public Map CreateMap(string name)
        {
            name = name.Replace(' ', '_');
            if (MapDatabase.ContainsKey(name))
            {
                var type = MapDatabase[name].GetType();
                var map = (Map)Activator.CreateInstance(type);
                map.Initialize(this);
                return map;
            }
            return null;
        }
        public T CreateMap<T>() where T : Map
        {
            T map = (T)Activator.CreateInstance(typeof(T));
            map.Initialize(this);
            return map;
        }
        public T CreateNpc<T>() where T : Monster
        {
            T npc = (T)Activator.CreateInstance<T>();
            npc.CurrentHP = npc.BaseMaximumHP;
            npc.CurrentMP = npc.BaseMaximumMP;
            return npc;
        }
        #endregion
    }
}