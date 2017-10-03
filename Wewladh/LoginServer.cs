using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace Wewladh
{
    public class LoginServer : Server
    {
        public static readonly string[] RESERVED_STRINGS = new string[]
        {
            "acht", "kyle", "heil", "mark", "hyru", "svaf", "jer", "umok", "mop"
        };
        public static readonly HashSet<char> ALLOWED_CHARACTERS = new HashSet<char>("abcdefghijklmnopqrstuvwxyz");

        public GameServer GameServer { get; private set; }
        public Notification Notification { get; private set; }

        protected override void LoadPacketHandlers()
        {
            MessageHandlers[0x02] = MsgHandler_CreateA;
            MessageHandlers[0x03] = MsgHandler_CreateB;
            MessageHandlers[0x04] = MsgHandler_Login;
            MessageHandlers[0x26] = MsgHandler_ChangePassword;
            MessageHandlers[0x4B] = MsgHandler_RequestNotification;
            MessageHandlers[0x57] = MsgHandler_ServerTable;
            MessageHandlers[0x68] = MsgHandler_RequestWebsite;
            MessageHandlers[0x10] = MsgHandler_ClientJoin;
            MessageHandlers[0x0B] = MsgHandler_Logoff;
            MessageHandlers[0x7B] = MsgHandler_RequestMetafile;
        }

        public LoginServer(GameServer gs, int port)
        {
            GameServer = gs;

            Notification = new Notification(String.Format("{0} : {1}", GameServer.Name, GameServer.Description));
            if (File.Exists(GameServer.DataPath + "\\notification.txt"))
            {
                Notification.Text = File.ReadAllText(GameServer.DataPath + "\\notification.txt").Replace("\r\n", "\n");
            }
            Notification.Write(GameServer.DataPath);

            IPHostEntry entry = Dns.GetHostEntry(Program.HostName);
            if (entry.AddressList.Length > 0)
            {
                EndPoint = new IPEndPoint(entry.AddressList[0], port);
            }
        }

        private void MsgHandler_RequestMetafile(Client client, ClientPacket msg)
        {
            if (!msg.ReadBoolean())
            {
                string fileName = msg.ReadString(msg.ReadByte());
                if (GameServer.MetafileDatabase.ContainsKey(fileName))
                {
                    var p = new ServerPacket(0x6F);
                    p.WriteByte(0);
                    p.WriteString8(fileName);
                    p.WriteUInt32(GameServer.MetafileDatabase[fileName].Checksum);
                    p.WriteUInt16((ushort)GameServer.MetafileDatabase[fileName].RawData.Length);
                    p.Write(GameServer.MetafileDatabase[fileName].RawData);
                    client.Enqueue(p);
                }
            }
            else
            {
                var p = new ServerPacket(0x6F);
                p.WriteByte(1);
                p.WriteUInt16((ushort)GameServer.MetafileDatabase.Count);
                foreach (var kvp in GameServer.MetafileDatabase)
                {
                    p.WriteString8(kvp.Value.Name);
                    p.WriteUInt32(kvp.Value.Checksum);
                }
                client.Enqueue(p);
            }
        }
        private void MsgHandler_CreateA(Client client, ClientPacket msg)
        {
            string name = msg.ReadString(msg.ReadByte()).ToLower();
            string password = msg.ReadString(msg.ReadByte());
            string email = msg.ReadString(msg.ReadByte());

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT COUNT(*) FROM characters WHERE (name = @name) AND (server = @server)";
            com.Parameters.AddWithValue("@name", name);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            var result = com.ExecuteScalar();

            if ((long)result > 0)
            {
                client.SendLoginMessage(3, "That name is unavailable.");
                return;
            }

            foreach (var str in RESERVED_STRINGS)
            {
                if (name.Contains(str))
                {
                    client.SendLoginMessage(3, "That name is unavailable.");
                    return;
                }
            }

            if (!GameServer.AllowCreate)
            {
                client.SendLoginMessage(3, "Character creation is currently disabled.");
                return;
            }

            for (int i = 0; i < name.Length; i++)
            {
                if (!ALLOWED_CHARACTERS.Contains(name[i]))
                {
                    client.SendLoginMessage(3, "Character names may only contain letters.");
                    return;
                }
            }

            if ((name.Length < 4) || (name.Length > 12))
            {
                client.SendLoginMessage(3, "Character names must be 4 to 12 letters long.");
                return;
            }

            if ((password.Length < 4) || (password.Length > 8))
            {
                client.SendLoginMessage(5, "Passwords must be 4 to 8 characters long.");
                return;
            }

            client.NewPlayerName = name;
            client.NewPlayerPassword = password;

            client.SendLoginMessage(0, "\0");
        }
        private void MsgHandler_CreateB(Client client, ClientPacket msg)
        {
            if (string.IsNullOrEmpty(client.NewPlayerName) || string.IsNullOrEmpty(client.NewPlayerPassword))
                return;

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT COUNT(*) FROM characters WHERE (name = @name) AND (server = @server)";
            com.Parameters.AddWithValue("@name", client.NewPlayerName);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            var result = com.ExecuteScalar();
            if ((long)result > 0)
            {
                client.SendLoginMessage(3, "That character already exists.");
                return;
            }

            Player p = Player.Create(GameServer, client.NewPlayerName);

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "INSERT INTO characters VALUES (0, @name, @password, 0, @server, @creation_date, 0, "
                + "'None', 'None', 'Peasant', 'None', 0, 0, 0, 0, 'North', 0, 0, 0, 0, 0, 0, "
                + "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '', 0, 0, 'None', '', '')"; // 50
            com.Parameters.AddWithValue("@name", client.NewPlayerName);
            com.Parameters.AddWithValue("@password", client.NewPlayerPassword);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            com.Parameters.AddWithValue("@creation_date", DateTime.UtcNow);
            com.ExecuteNonQuery();

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT character_id FROM characters WHERE (name = @name) AND (server = @server)";
            com.Parameters.AddWithValue("@name", p.Name);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            result = com.ExecuteScalar();
            p.GUID = (int)result;

            p.HairStyle = msg.ReadByte();
            p.Sex = (Gender)msg.ReadByte();
            p.HairColor = msg.ReadUInt16();

            if ((p.HairStyle > 17) || (p.HairStyle < 1))
                p.HairStyle = 1;

            if ((p.Sex != Gender.Male) && (p.Sex != Gender.Female))
                p.Sex = Gender.Male;

            if (p.HairColor > 13)
                p.HairColor = 0;
            p.Loaded = true;
            p.Save();
            client.NewPlayerName = null;
            client.NewPlayerPassword = null;

            client.SendLoginMessage(0, "\0");

            Program.WriteLine("New character created: {0}", p.Name);
        }
        private void MsgHandler_Login(Client client, ClientPacket msg)
        {
            var name = msg.ReadString(msg.ReadByte());
            var password1 = msg.ReadString(msg.ReadByte());

            var accountid = 0;
            var password2 = String.Empty;
            var gamemaster = AdminRights.None;
            var active = false;
            var exists = false;

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT characters.acct_id, characters.password, characters.gm, accounts.active "
                + "FROM characters "
                + "LEFT JOIN accounts "
                + "ON characters.acct_id = accounts.acct_id "
                + "WHERE (characters.name = @name) AND (characters.server = @server)";
            com.Parameters.AddWithValue("@name", name);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            var reader = com.ExecuteReader();
            if (reader.Read())
            {
                accountid = reader.GetInt32(0);
                password2 = reader.GetString(1);
                gamemaster = (AdminRights)Enum.Parse(typeof(AdminRights), reader.GetString(2));
                active = (!reader.IsDBNull(3) && reader.GetBoolean(3));
                exists = true;
            }
            reader.Close();

            if (!GameServer.AllowLogin)
            {
                client.SendLoginMessage(14, "The game server is currently closed.");
                return;
            }

            if (!exists)
            {
                client.SendLoginMessage(14, "That character does not exist.");
                return;
            }

            if (password1 != password2)
            {
                client.SendLoginMessage(14, "Incorrect password.");
                return;
            }

            if (accountid == 0)
            {
                client.SendLoginMessage(14, "Please register your character at game.wewladh.com");
                return;
            }

            if (!active)
            {
                client.SendLoginMessage(14, "Your account has been locked.");
                return;
            }

            foreach (Client c in GameServer.Clients)
            {
                if ((c.Player != null) && c.Player.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    c.Connected = false;
                    client.SendLoginMessage(14, "Your character is already playing.");
                    return;
                }
                if ((c.Player != null) && (accountid != 0) && (c.Player.AccountID == accountid))
                {
                    c.Connected = false;
                    client.SendLoginMessage(14, "Your account is already playing.");
                    return;
                }
            }

            if (Program.RunningSlowly)
            {
                client.SendLoginMessage(14, "The game server is busy right now. Please try again.");
                return;
            }

            client.SendLoginMessage(0, "\0");

            var p = new ServerPacket(0x22);
            p.WriteByte(0x00);
            client.Enqueue(p);

            foreach (var message in File.ReadAllLines(GameServer.DataPath + "\\login.txt"))
                client.SendMessage(message);

            var r = new Redirection();
            r.DestinationServer = GameServer;
            r.EncryptionParameters = client.EncryptionParams;
            r.Name = name;
            r.SourceServer = this;
            client.Redirect(r);
        }
        private void MsgHandler_ChangePassword(Client client, ClientPacket msg)
        {
            var name = msg.ReadString(msg.ReadByte());
            var oldPassword = msg.ReadString(msg.ReadByte());
            var newPassword = msg.ReadString(msg.ReadByte());

            var character_id = 0;
            var password = String.Empty;
            var exists = false;

            var com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "SELECT characters.character_id, characters.password "
                + "FROM characters "
                + "WHERE (characters.name = @name) AND (characters.server = @server)";
            com.Parameters.AddWithValue("@name", name);
            com.Parameters.AddWithValue("@server", GameServer.Name);
            var reader = com.ExecuteReader();
            if (reader.Read())
            {
                character_id = reader.GetInt32(0);
                password = reader.GetString(1);
                exists = true;
            }
            reader.Close();

            if (!exists)
            {
                client.SendLoginMessage(14, "That character does not exist.");
                return;
            }

            if (oldPassword != password)
            {
                client.SendLoginMessage(14, "Incorrect password.");
                return;
            }

            com = Program.MySqlConnection.CreateCommand();
            com.CommandText = "UPDATE characters SET characters.password=@password WHERE characters.character_id=@character_id";
            com.Parameters.AddWithValue("@password", newPassword);
            com.Parameters.AddWithValue("@character_id", character_id);

            if (com.ExecuteNonQuery() < 0)
            {
                client.SendLoginMessage(14, "Unknown error.");
                return;
            }
            
            client.SendLoginMessage(0, "\0");
        }
        private void MsgHandler_RequestNotification(Client client, ClientPacket msg)
        {
            var p1 = new ServerPacket(0x60);
            p1.WriteByte(0x01);
            p1.WriteUInt16((ushort)Notification.RawData.Length);
            p1.Write(Notification.RawData);
            client.Enqueue(p1);
        }
        private void MsgHandler_ServerTable(Client client, ClientPacket msg)
        {
            if (msg.ReadBoolean())
            {
                var p = new ServerPacket(0x56);
                p.WriteUInt16((ushort)Program.RawData.Length);
                p.Write(Program.RawData);
                client.Enqueue(p);
            }
            else
            {
                int serverId = msg.ReadByte();
                if (serverId < Program.GameServers.Count)
                {
                    Redirection r = new Redirection();
                    r.DestinationServer = Program.GameServers[serverId].LoginServer;
                    r.EncryptionParameters = client.EncryptionParams;
                    r.Name = String.Format(String.Empty, r.ID);
                    r.SourceServer = this;
                    client.Redirect(r);
                }
            }
        }
        private void MsgHandler_RequestWebsite(Client client, ClientPacket msg)
        {
            var p1 = new ServerPacket(0x66);
            p1.WriteByte(0x03);
            p1.WriteString8("http://www.wewladh.com");
            client.Enqueue(p1);
        }
        private void MsgHandler_ClientJoin(Client client, ClientPacket msg)
        {
            byte seed = msg.ReadByte();
            byte[] key = msg.Read(msg.ReadByte());
            string name = msg.ReadString(msg.ReadByte());
            uint id = msg.ReadUInt32();

            Encryption.Parameters encryptionParameters = new Encryption.Parameters(key, seed);

            if (ExpectedRedirects.ContainsKey(id) && (ExpectedRedirects[id] != null))
            {
                Redirection r = ExpectedRedirects[id];
                if ((r.Name == name) && r.EncryptionParameters.Matches(encryptionParameters))
                {
                    if (r.SourceServer == Program.LobbyServer || r.SourceServer is LoginServer)
                    {
                        var p = new ServerPacket(0x60);
                        p.WriteByte(0x00);
                        p.WriteUInt32(Notification.Checksum);
                        client.Enqueue(p);

                        var packet = new ServerPacket(0x6F);
                        packet.WriteByte(1);
                        packet.WriteUInt16((ushort)GameServer.MetafileDatabase.Count);
                        foreach (var kvp in GameServer.MetafileDatabase)
                        {
                            packet.WriteString8(kvp.Value.Name);
                            packet.WriteUInt32(kvp.Value.Checksum);
                        }
                        client.Enqueue(packet);
                    }
                }
            }
        }
        private void MsgHandler_Logoff(Client client, ClientPacket msg)
        {

        }
    }
}