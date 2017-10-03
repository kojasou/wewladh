using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Wewladh
{
    public class Client
    {
        public Server Server { get; set; }

        public Player Player { get; set; }
        public string NewPlayerName { get; set; }
        public string NewPlayerPassword { get; set; }

        public Socket Socket { get; private set; }
        public bool Connected { get; set; }

        private byte[] clientBuffer = new byte[65535];
        private List<byte> fullClientBuffer = new List<byte>();
        private Queue<ServerPacket> sendQueue = new Queue<ServerPacket>();
        private DateTime lastDequeue = DateTime.MinValue;

        private byte ordinal = 0;
        public Encryption.Parameters EncryptionParams { get; private set; }

        public Thread _clientLoopThread;
        public DateTime LastPacket { get; private set; }

        public IPAddress IPAddress
        {
            get { return (Socket.RemoteEndPoint as IPEndPoint).Address; }
        }

        public Client(Socket socket, Server server)
        {
            this.Server = server;
            this.Socket = socket;
            this.Connected = true;
            this.EncryptionParams = new Encryption.Parameters("NexonInc.", 0);
            this._clientLoopThread = new Thread(new ThreadStart(AsyncClientLoop));
            this._clientLoopThread.Start();
            this.LastPacket = DateTime.UtcNow;
        }

        private static void EndReceive(IAsyncResult ar)
        {
            var client = (Client)ar.AsyncState;

            try
            {
                int count = client.Socket.EndReceive(ar);

                for (int i = 0; i < count; i++)
                    client.fullClientBuffer.Add(client.clientBuffer[i]);

                if ((count == 0) || (client.fullClientBuffer[0] != 0xAA))
                {
                    client.Connected = false;
                    return;
                }

                while (client.fullClientBuffer.Count > 3)
                {
                    int length = ((client.fullClientBuffer[1] << 8) + client.fullClientBuffer[2] + 3);
                    if (length > client.fullClientBuffer.Count)
                        break;
                    List<byte> data = client.fullClientBuffer.GetRange(0, length);
                    client.fullClientBuffer.RemoveRange(0, length);
                    ClientPacket msg = new ClientPacket(data.ToArray());
                    if (msg.ShouldEncrypt())
                    {
                        Encryption.Transform(client.EncryptionParams, msg);
                    }

                    lock (Program.SyncObj)
                    {
                        if (client.Connected)
                            client.Server.MessageHandlers[msg.Opcode].Invoke(client, msg);
                    }
                }

                client.LastPacket = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                if (client.Server is GameServer)
                    Program.WriteLine(e);
                client.Connected = false;
            }
        }
        public void AsyncClientLoop()
        {
            while (Connected)
            {
                try
                {
                    var result = Socket.BeginReceive(clientBuffer, 0, clientBuffer.Length, SocketFlags.None,
                        new AsyncCallback(EndReceive), this);
                    while (Connected && !result.IsCompleted)
                    {
                        var fullbuffer = new List<byte>();

                        lock (Program.SyncObj)
                        {
                            if (DateTime.UtcNow.Subtract(lastDequeue).TotalMilliseconds > 250)
                            {
                                while (sendQueue.Count > 0)
                                {
                                    var msg = sendQueue.Dequeue();

                                    if (msg.ShouldEncrypt())
                                    {
                                        msg.Ordinal = ordinal++;
                                        Encryption.Transform(EncryptionParams, msg);
                                    }
                                    msg.Length = (ushort)(msg.BodyData.Length + (msg.Header.Length - 3));
                                    byte[] buffer = new byte[msg.Header.Length + msg.BodyData.Length];
                                    Array.Copy(msg.Header, 0, buffer, 0, msg.Header.Length);
                                    Array.Copy(msg.BodyData, 0, buffer, msg.Header.Length, msg.BodyData.Length);
                                    fullbuffer.AddRange(buffer);
                                }
                                lastDequeue = DateTime.UtcNow;
                            }
                        }

                        if (fullbuffer.Count > 0)
                        {
                            byte[] sendbuffer = fullbuffer.ToArray();
                            try
                            {
                                Socket.BeginSend(sendbuffer, 0, sendbuffer.Length, SocketFlags.None,
                                    new AsyncCallback(EndSend), Socket);
                            }
                            catch
                            {

                            }
                        }

                        Thread.Sleep(75);
                    }
                }
                catch (Exception e)
                {
                    Program.WriteLine(e);
                    Connected = false;
                }
            }

            lock (Program.SyncObj)
            {
                if (Player != null)
                {
                    if (Player.ExchangeInfo != null)
                        Player.CancelExchange();

                    Player.CancelSpellCast();

                    var equipment = new Equipment[Player.Equipment.Length];
                    Player.Equipment.CopyTo(equipment, 0);
                    foreach (var item in equipment)
                    {
                        if (item != null && item.DropOnLogoff)
                        {
                            Player.RemoveEquipment(item);
                            Player.Map.InsertCharacter(item, Player.Point);
                        }
                    }

                    var statuses = new Spell[Player.Statuses.Count];
                    Player.Statuses.Values.CopyTo(statuses, 0);
                    foreach (var status in statuses)
                    {
                        if (status.Channeled || status.RequiresCaster || status.SingleTarget)
                            Player.RemoveStatus(status.StatusName);
                    }

                    var singleStatuses = new string[Player.SingleTargetSpells.Count];
                    Player.SingleTargetSpells.Keys.CopyTo(singleStatuses, 0);
                    foreach (var status in singleStatuses)
                    {
                        var character = Player.SingleTargetSpells[status];
                        character.RemoveStatus(status);
                    }

                    if (Player.Group.HasMembers)
                        Player.Group.RemoveMember(Player);

                    this.Player.Save();

                    statuses = new Spell[Player.Statuses.Count];
                    Player.Statuses.Values.CopyTo(statuses, 0);
                    foreach (var status in statuses)
                    {
                        Player.RemoveStatus(status.StatusName);
                    }

                    this.Player.GameServer.RemoveGameObject(Player);
                    this.Player = null;
                }
                this.Socket.Close();
                this.Server.Clients.Remove(this);
            }
        }

        public void Redirect(Server.Redirection r)
        {
            r.DestinationServer.ExpectedRedirects.Add(r.ID, r);

            var ipEndPoint = r.DestinationServer.EndPoint;

            byte[] addressBytes = ipEndPoint.Address.GetAddressBytes();

            if (IPAddress.IsLoopback(IPAddress) || IPAddress.ToString().StartsWith("192.168"))
                addressBytes = IPAddress.Parse("192.168.0.42").GetAddressBytes();
            
            Array.Reverse(addressBytes);

            var p = new ServerPacket(0x03);
            p.Write(addressBytes);
            p.WriteUInt16((ushort)ipEndPoint.Port);
            p.WriteByte((byte)(r.EncryptionParameters.PrivateKey.Length + Encoding.GetEncoding(949).GetBytes(r.Name).Length + 7));
            p.WriteByte(r.EncryptionParameters.Seed);
            p.WriteByte((byte)r.EncryptionParameters.PrivateKey.Length);
            p.Write(r.EncryptionParameters.PrivateKey);
            p.WriteString8(r.Name);
            p.WriteUInt32(r.ID);
            Enqueue(p);
        }

        public void Enqueue(ServerPacket msg)
        {
            sendQueue.Enqueue(msg);
        }
        private static void EndSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch
            {

            }
        }

        public void SendMiniGame(int value1, int value2)
        {
            var packet = new ServerPacket(0x64);
            packet.WriteByte((byte)value1);
            packet.WriteByte((byte)value2);
            packet.WriteByte(0x00);
            Enqueue(packet);
        }
        public void Refresh()
        {
            SendMapInfo();
            SendLocation();
            Player.Display();
            foreach (var c in Player.Map.Objects)
            {
                if (c.WithinRange(Player, 12))
                {
                    c.DisplayTo(Player);
                }
            }

            //for (int x = 0; x < Player.Map.Width; x++)
            //{
            //    for (int y = 0; y < Player.Map.Height; y++)
            //    {
            //        if (Player.Map.Doors[x, y] != null)
            //            ToggleDoor(Player.Map.Doors[x, y]);
            //    }
            //}

            var p = new ServerPacket(0x22);
            p.WriteByte(0x00);
            Enqueue(p);
            Player.LastRefresh = DateTime.UtcNow;
        }
        public void SendProfile()
        {
            var p = new ServerPacket(0x39);
            p.WriteByte(Player.GameServer.NationDatabase[Player.Nation].Flag);
            p.WriteString8((Player.Guild != null) ? Player.GuildRank.ToString() : string.Empty);

            var masks = new List<byte>();

            for (int i = 0; i < Player.GameServer.TitleDatabase.Count; i++)
            {
                var title = Player.GameServer.TitleDatabase[i];

                var value = i % 8;

                if (value == 0)
                {
                    masks.Add(0);
                }

                var index = masks.Count - 1;

                if (title.Available(Player))
                {
                    switch (value)
                    {
                        case 0: masks[index] += 0x80; break;
                        case 1: masks[index] += 0x40; break;
                        case 2: masks[index] += 0x20; break;
                        case 3: masks[index] += 0x10; break;
                        case 4: masks[index] += 0x08; break;
                        case 5: masks[index] += 0x04; break;
                        case 6: masks[index] += 0x02; break;
                        case 7: masks[index] += 0x01; break;
                    }
                }
            }

            p.WriteByte((byte)masks.Count);
            foreach (var mask in masks)
                p.WriteByte(mask);

            p.WriteByte((byte)Player.Title);
            if (!Player.Group.HasMembers)
            {
                p.WriteString8("그룹 없음");
            }
            else
            {
                StringBuilder sb = new StringBuilder("그룹구성원\n");
                foreach (var player in Player.Group.Members)
                {
                    sb.AppendFormat("{0} {1}\n", (player == Player.Group.Leader) ? "*" : " ", player.Name);
                }
                sb.AppendFormat("총 {0}명", Player.Group.Members.Count);
                p.WriteString8(sb.ToString());
            }
            p.WriteByte(Player.GroupToggle);
            p.WriteByte(0x00); // ??
            p.WriteByte((byte)Player.Class);
            p.WriteByte(0x01); // ??
            p.WriteByte(0x00); // ??
            p.WriteString8(string.Format("{0}{1}{2}",
                Player.Master ? "Master " : string.Empty,
                (Player.Specialization != Specialization.None) ? Player.Specialization + " " : string.Empty,
                Player.Class));
            p.WriteString8((Player.Guild != null) ? Player.Guild.Name : string.Empty);
            p.WriteByte((byte)Player.Legend.Count);
            foreach (var kvp in Player.Legend.OrderBy(l => l.Value.DateUpdated))
            {
                p.WriteByte((byte)kvp.Value.Icon);
                p.WriteByte((byte)kvp.Value.Color);
                p.WriteString8(kvp.Value.Key);
                p.WriteString8(kvp.Value.ToString());
            }
            p.WriteByte(0x00); // ??
            p.WriteUInt16(Player.DisplayBitmask);
            p.WriteByte(0x02); // ??
            p.WriteUInt32(0x00); // ??
            p.WriteByte(0x00); // ??
            Enqueue(p);
        }
        public void RemoveCharacter(uint id)
        {
            var p = new ServerPacket(0x0E);
            p.WriteUInt32(id);
            Enqueue(p);
        }
        public void SendLocation()
        {
            SendLocation(Player.Point.X, Player.Point.Y);
        }
        public void SendLocation(int x, int y)
        {
            var p = new ServerPacket(0x04);
            p.WriteUInt16((ushort)x);
            p.WriteUInt16((ushort)y);
            p.WriteUInt16(11);
            p.WriteUInt16(11);
            Enqueue(p);
        }
        public void SendPlayerID()
        {
            SendPlayerID(Player.ID);
        }
        public void SendPlayerID(uint id)
        {
            var p = new ServerPacket(0x05);
            p.WriteUInt32(id);
            p.WriteByte(1);
            p.WriteByte(213);
            p.WriteByte((byte)Player.Class);
            p.WriteUInt16(0);
            Enqueue(p);
        }
        public void SendStatistics(StatUpdateFlags flags)
        {
            if (Player.CanWalkThroughWalls || Player.CanWalkThroughUnits)
            {
                flags |= StatUpdateFlags.GameMasterA;
            }
            else
            {
                flags |= StatUpdateFlags.Swimming;
            }

            var p = new ServerPacket(0x08);
            p.WriteByte((byte)flags);
            if ((flags & StatUpdateFlags.Primary) == StatUpdateFlags.Primary)
            {
                p.Write(new byte[] { 1, 0, 0 });
                p.WriteByte((byte)Player.Level);
                p.WriteByte(0x00);
                p.WriteByte((byte)Player.Ability);
                p.WriteUInt32(Player.MaximumHP);
                p.WriteUInt32(Player.MaximumMP);
                p.WriteUInt16(Player.Str);
                p.WriteUInt16(Player.Int);
                p.WriteUInt16(Player.Wis);
                p.WriteUInt16(Player.Con);
                p.WriteUInt16(Player.Dex);
                p.WriteByte(Player.AvailableStatPoints > 0);
                p.WriteByte((byte)Player.AvailableStatPoints);
                p.WriteUInt16((ushort)Player.MaximumWeight);
                p.WriteUInt16((ushort)Player.CurrentWeight);
                p.WriteUInt32(uint.MinValue);
            }
            if ((flags & StatUpdateFlags.Current) == StatUpdateFlags.Current)
            {
                p.WriteUInt32((uint)Player.CurrentHP);
                p.WriteUInt32((uint)Player.CurrentMP);
            }
            if ((flags & StatUpdateFlags.Experience) == StatUpdateFlags.Experience)
            {
                p.WriteUInt32((uint)Player.Experience);
                p.WriteUInt32((uint)(Player.ToNextLevel - Player.Experience));
                p.WriteUInt32((uint)(Player.ToNextLevel - Player.ToThisLevel));
                p.WriteUInt32((uint)Player.AbilityExp);
                p.WriteUInt32((uint)(Player.ToNextAbility - Player.AbilityExp));
                p.WriteUInt32((uint)(Player.ToNextAbility - Player.ToThisAbility));
                p.WriteUInt32((uint)Player.GamePoints);
                p.WriteUInt32((uint)Player.Gold);
            }
            if ((flags & StatUpdateFlags.Secondary) == StatUpdateFlags.Secondary)
            {
                p.WriteUInt32(uint.MinValue);
                p.WriteUInt16(ushort.MinValue);
                p.WriteByte((byte)Player.OffenseElement);
                p.WriteByte((byte)Player.DefenseElement);
                p.WriteByte((byte)(Player.MagicResistance / 10));
                p.WriteByte(byte.MinValue);
                p.WriteSByte(Player.ArmorClass);
                p.WriteSByte(Player.Dmg);
                p.WriteSByte(Player.Hit);
            }
            Enqueue(p);
        }
        public void SendMapInfo()
        {
            var p = new ServerPacket(0x15);
            p.WriteUInt16((ushort)Player.Map.Number);
            p.WriteByte((byte)(Player.Map.Width % 256));
            p.WriteByte((byte)(Player.Map.Height % 256));
            byte flags = 0;
            if ((Player.Map.Flags & MapFlags.Snow) == MapFlags.Snow)
                flags |= 1;
            if ((Player.Map.Flags & MapFlags.Rain) == MapFlags.Rain)
                flags |= 2;
            if ((Player.Map.Flags & MapFlags.NoMap) == MapFlags.NoMap)
                flags |= 64;
            if ((Player.Map.Flags & MapFlags.Winter) == MapFlags.Winter)
                flags |= 128;
            p.WriteByte(flags);
            p.WriteByte((byte)(Player.Map.Width / 256));
            p.WriteByte((byte)(Player.Map.Height / 256));
            p.WriteByte((byte)(Player.Map.Checksum % 256));
            p.WriteByte((byte)(Player.Map.Checksum / 256));
            p.WriteString8(Player.Map.Name);
            Enqueue(p);

            if (Player.CurrentMusic != Player.Map.Music)
            {
                Player.CurrentMusic = Player.Map.Music;
                SoundEffect(0x8000 + Player.Map.Music);
            }

            Player.WorldMap.IsOpen = false;
        }
        public void SendMap(Map map)
        {
            int tile = 0;
            for (int row = 0; row < map.Height; row++)
            {
                var p = new ServerPacket(0x3C);
                p.WriteUInt16((ushort)row);
                for (int column = 0; column < map.Width * 6; column += 2)
                {
                    p.WriteByte(map.RawData[tile + 1]);
                    p.WriteByte(map.RawData[tile]);
                    tile += 2;
                }
                Enqueue(p);
            }
        }
        public void SendLoginMessage(int type, string msg)
        {
            var p = new ServerPacket(0x02);
            p.WriteByte((byte)type);
            p.WriteString8(msg);
            Enqueue(p);
        }
        public void SendMessage(string message)
        {
            SendMessage(message, 3);
        }
        public void SendMessage(string message, int type)
        {
            var p = new ServerPacket(0x0A);
            p.WriteByte((byte)type);
            p.WriteString16(message);
            p.WriteByte(0x00);
            Enqueue(p);
        }
        public void SendMessage(string format, params object[] args)
        {
            var p = new ServerPacket(0x0A);
            p.WriteByte(0x03);
            p.WriteString16(format, args);
            p.WriteByte(0x00);
            Enqueue(p);
        }
        public void BodyAnimation(uint id, int animation, int speed)
        {
            var p = new ServerPacket(0x1A);
            p.WriteUInt32((uint)id);
            p.WriteByte((byte)animation);
            p.WriteUInt16((ushort)speed);
            p.WriteUInt16(0x00);
            Enqueue(p);
        }
        public void SpellAnimation(uint id, int animation, int speed)
        {
            var p = new ServerPacket(0x29);
            p.WriteByte(0x00); // ??
            p.WriteUInt32((uint)id);
            p.WriteUInt32((uint)id);
            p.WriteUInt16((ushort)animation);
            p.WriteUInt16(ushort.MinValue);
            p.WriteUInt16((ushort)speed);
            p.WriteByte(0x00);
            Enqueue(p);
        }
        public void SpellAnimation(uint id, int animation, int speed, uint fromId, int fromAnimation)
        {
            var p = new ServerPacket(0x29);
            p.WriteByte(0x00); // ??
            p.WriteUInt32((uint)id);
            p.WriteUInt32((uint)fromId);
            p.WriteUInt16((ushort)animation);
            p.WriteUInt16((ushort)fromAnimation);
            p.WriteUInt16((ushort)speed);
            p.WriteByte(0x00);
            Enqueue(p);
        }
        public void SpellAnimation(int animation, int x, int y, int speed)
        {
            var p = new ServerPacket(0x29);
            p.WriteByte(0x00); // ??
            p.WriteUInt32(uint.MinValue);
            p.WriteUInt16((ushort)animation);
            p.WriteUInt16((ushort)speed);
            p.WriteUInt16((ushort)x);
            p.WriteUInt16((ushort)y);
            Enqueue(p);
        }
        public void DisplaySpellBar(Spell s)
        {
            var p = new ServerPacket(0x3A);
            p.WriteUInt16((ushort)s.Icon);
            p.WriteString8(s.Name);
            p.WriteUInt16((ushort)(s.TimeLeft * s.Speed / 1000));
            Enqueue(p);
        }
        public void SendCooldown(int slot, long length, int pane)
        {
            var p = new ServerPacket(0x3F);
            p.WriteByte((byte)pane);
            p.WriteByte((byte)slot);
            p.WriteUInt32((uint)length);
            Enqueue(p);
        }
        public void ToggleDoor(Door door)
        {
            foreach (var point in door.Points)
            {
                var packet = new ServerPacket(0x32);
                packet.WriteByte(0x01);
                packet.WriteByte((byte)point.X);
                packet.WriteByte((byte)point.Y);
                packet.WriteByte(!door.IsOpen);
                packet.WriteByte((byte)door.Direction);
                packet.WriteByte(0x00);
                Enqueue(packet);
            }
        }
        public void SendWorldMap()
        {
            Player.WorldMap.IsOpen = true;
            var packet = new ServerPacket(0x2E);
            packet.WriteString8(Player.WorldMap.FileName);
            packet.WriteByte((byte)Player.WorldMap.Nodes.Count);
            packet.WriteByte(0x09);
            for (int i = 0; i < Player.WorldMap.Nodes.Count; i++)
            {
                var node = Player.WorldMap.Nodes[i];
                packet.WriteUInt16((ushort)node.X);
                packet.WriteUInt16((ushort)node.Y);
                packet.WriteString8(node.MapName);
                packet.WriteInt32(i);
                packet.WriteUInt16((ushort)node.MapX);
                packet.WriteUInt16((ushort)node.MapY);
            }
            Enqueue(packet);
        }
        public void SendPaper(Item item, byte color)
        {
            Player.Paper = item;
            var packet = new ServerPacket(0x1B);
            packet.WriteByte((byte)item.Slot);
            packet.WriteByte(color);
            packet.WriteByte(0x0F);
            packet.WriteByte(0x14);
            packet.WriteString16(item.MiscData);
            Enqueue(packet);
        }

        public void RemoveSkill(int slot)
        {
            var p = new ServerPacket(0x2D);
            p.WriteByte((byte)slot);
            p.WriteByte(0);
            Enqueue(p);
        }
        public void RemoveSpell(int slot)
        {
            var p = new ServerPacket(0x18);
            p.WriteByte((byte)slot);
            p.WriteByte(0);
            Enqueue(p);
        }

        public void SoundEffect(int sound)
        {
            var p = new ServerPacket(0x19);
            p.WriteUInt16((ushort)sound);
            Enqueue(p);
        }
    }
}