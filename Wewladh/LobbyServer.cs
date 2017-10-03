using System;
using System.Net;

namespace Wewladh
{
    public class LobbyServer : Server
    {
        public LobbyServer()
        {
            Program.WriteLine("[LOADING LOBBY SERVER]");
            IPHostEntry entry = Dns.GetHostEntry(Program.HostName);
            if (entry.AddressList.Length > 0)
            {
                EndPoint = new IPEndPoint(entry.AddressList[0], Program.Port);
            }
            Program.WriteLine("[LOBBY SERVER LOADED!]");
        }

        protected override void LoadPacketHandlers()
        {
            MessageHandlers[0x00] = MsgHandler_ClientVersion;
            MessageHandlers[0x57] = MsgHandler_ServerTable;
        }

        private void MsgHandler_ClientVersion(Client client, ClientPacket msg)
        {
            var p = new ServerPacket(0x00);
            p.WriteByte(0x00);
            p.WriteUInt32(Program.Checksum);
            p.WriteByte(0x00);
            p.WriteString8("NexonInc.");
            client.Enqueue(p);

            Program.WriteLine("Client connected: {0}", ((IPEndPoint)client.Socket.RemoteEndPoint).Address);
        }
        private void MsgHandler_ServerTable(Client client, ClientPacket msg)
        {
            bool mismatch = msg.ReadBoolean();
            int serverId = msg.ReadByte();

            if (mismatch)
            {
                var p = new ServerPacket(0x56);
                p.WriteUInt16((ushort)Program.RawData.Length);
                p.Write(Program.RawData);
                client.Enqueue(p);
            }
            else
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
}