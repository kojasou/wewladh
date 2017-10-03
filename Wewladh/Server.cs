using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Wewladh
{
    public abstract class Server
    {
        public delegate void MessageHandler(Client client, ClientPacket msg);

        protected TcpListener listener;
        public List<Client> Clients { get; private set; }
        public MessageHandler[] MessageHandlers { get; private set; }
        protected abstract void LoadPacketHandlers();
        public Dictionary<uint, Redirection> ExpectedRedirects { get; private set; }
        public IPEndPoint EndPoint { get; protected set; }

        public Server()
        {
            Clients = new List<Client>();
            MessageHandlers = new MessageHandler[256];
            for (int i = 0; i < MessageHandlers.Length; i++)
            {
                MessageHandlers[i] = MsgHandler_UnhandledPacket;
            }

            LoadPacketHandlers();
            ExpectedRedirects = new Dictionary<uint, Redirection>();
        }

        public void Start()
        {
            this.listener = new TcpListener(IPAddress.Any, EndPoint.Port);
            this.listener.Start();
        }

        public void AcceptConnection()
        {
            if (listener.Pending())
            {
                var socket = listener.AcceptSocket();
                var greetMsg = new ServerPacket(0x7E);
                var client = new Client(socket, this);

                var ip = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();

                if (Program.IPBanList.Contains(ip))
                {
                    client.Connected = false;
                    return;
                }

                Clients.Add(client);
                greetMsg.WriteByte(0x1B);
                greetMsg.WriteString("CONNECTED SERVER\n");
                client.Enqueue(greetMsg);
            }
        }

        public virtual void Shutdown()
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

        private void MsgHandler_UnhandledPacket(Client client, ClientPacket msg)
        {

        }

        public class Redirection
        {
            public Server SourceServer { get; set; }
            public Server DestinationServer { get; set; }
            public Encryption.Parameters EncryptionParameters { get; set; }
            public string Name { get; set; }
            public uint ID { get; private set; }
            private static uint nextRedirect = 1;
            public Redirection()
            {
                this.ID = nextRedirect++;
            }
        }
    }
}