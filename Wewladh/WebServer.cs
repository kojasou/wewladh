using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Wewladh
{
    public static class WebServer
    {
        private delegate WebResponse WebPage(WebClient wc);

        private static Socket listener;
        private static Dictionary<string, WebPage> pages;

        public static void Start()
        {
            pages = new Dictionary<string, WebPage>(StringComparer.CurrentCultureIgnoreCase);
            pages.Add("/", new WebPage(WebPage_Index_HTML));
            pages.Add("/style.css", new WebPage(WebPage_Style_CSS));
            pages.Add("/index.html", new WebPage(WebPage_Index_HTML));

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, 2600));
            listener.Listen(10);
            listener.BeginAccept(new AsyncCallback(EndAccept), null);
        }

        private static void EndAccept(IAsyncResult ar)
        {
            try
            {
                var socket = listener.EndAccept(ar);
                var client = new WebClient(socket);
                socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None,
                    new AsyncCallback(EndReceive), client);
            }
            catch
            {

            }
            finally
            {
                listener.BeginAccept(new AsyncCallback(EndAccept), null);
            }
        }
        private static void EndReceive(IAsyncResult ar)
        {
            try
            {
                var client = (WebClient)ar.AsyncState;
                int count = client.Socket.EndReceive(ar);
                client.StringBuilder.Append(Encoding.UTF8.GetString(client.Buffer, 0, count));
                ProcessRequest(client);
            }
            catch
            {

            }
        }
        private static void EndSend(IAsyncResult ar)
        {
            try
            {
                var client = (WebClient)ar.AsyncState;
                client.Socket.EndSend(ar);
                client.Socket.Close();
            }
            catch
            {

            }
        }

        private static void ProcessRequest(WebClient wc)
        {
            wc.Process();

            if (pages.ContainsKey(wc.RequestedPage))
            {
                var response = pages[wc.RequestedPage].Invoke(wc);
                byte[] buffer = Encoding.UTF8.GetBytes(response.ToString());
                wc.Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(EndSend), wc);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("HTTP/1.1 200 OK");
                sb.AppendLine("Content-Length: 5");
                sb.AppendLine("Connection: close");
                sb.AppendLine("Content-Type: text/html; charset=UTF-8");
                sb.AppendLine("");
                sb.AppendLine("error");
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                wc.Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(EndSend), wc);
            }
        }

        private static WebResponse WebPage_Style_CSS(WebClient wc)
        {
            var webResponse = new WebResponse("text/css");
            webResponse.WriteLine("body {");
            webResponse.WriteLine("  font-family: Verdana;");
            webResponse.WriteLine("  font-size: 12px;");
            webResponse.WriteLine("}");
            return webResponse;
        }
        private static WebResponse WebPage_Index_HTML(WebClient wc)
        {
            var webResponse = new WebResponse("text/html");

            webResponse.WriteLine("<!DOCTYPE HTML>");
            webResponse.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
            webResponse.WriteLine("<head>");
            webResponse.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />");
            webResponse.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\" />");
            webResponse.WriteLine("<title>Wewladh</title>");
            webResponse.WriteLine("</head>");
            webResponse.WriteLine("<body style=\"margin:0\">");
            lock (Program.SyncObj)
            {
                var player = GetPlayerFromQueryString(wc);
                if (player != null)
                {
                    webResponse.WriteLine("<object width=\"610\" height=\"3-2\"><param name=\"movie\" value=\"http://www.youtube.com/e/s0ujF8D6-5k?version=3&autoplay=1&rel=0&fs=0&controls=0&disablekb=1&modestbranding=1&showinfo=0\"></param><param name=\"allowFullScreen\" value=\"false\"></param><param name=\"allowscriptaccess\" value=\"always\"></param><embed src=\"http://www.youtube.com/e/s0ujF8D6-5k?version=3&autoplay=1&rel=0&fs=0&controls=0&disablekb=1&modestbranding=1&showinfo=0\" type=\"application/x-shockwave-flash\" width=\"610\" height=\"302\" allowscriptaccess=\"always\" allowfullscreen=\"false\"></embed></object>");
                }
                else
                {
                    webResponse.Write("<h3>ERROR</h3>");
                }
            }
            webResponse.WriteLine("</body>");
            webResponse.WriteLine("</html>");
            return webResponse;
        }

        private static Player GetPlayerFromQueryString(WebClient wc)
        {
            if (!wc.GetData.ContainsKey("gs"))
                return null;

            if (!wc.GetData.ContainsKey("id"))
                return null;

            if (!wc.GetData.ContainsKey("sk"))
                return null;

            int index = 0;
            if (int.TryParse(wc.GetData["gs"], out index) && index >= 0 && index < Program.GameServers.Count)
            {
                var gs = Program.GameServers[index];
                uint id = 0;
                if (uint.TryParse(wc.GetData["id"], out id))
                {
                    var player = gs.GameObject<Player>(id);
                    if (player.SecureKey == wc.GetData["sk"])
                        return player;
                }
            }
            return null;
        }
    }

    public class WebClient
    {
        public Socket Socket { get; private set; }
        public byte[] Buffer { get; private set; }
        public string FullRequest { get; private set; }
        public string RequestedPage { get; private set; }
        public Dictionary<string, string> GetData { get; private set; }
        public Dictionary<string, string> PostData { get; private set; }
        public StringBuilder StringBuilder { get; private set; }
        public string Method { get; private set; }
        public WebClient(Socket socket)
        {
            this.Socket = socket;
            this.Buffer = new byte[65535];
            this.StringBuilder = new StringBuilder();
            this.GetData = new Dictionary<string, string>();
            this.PostData = new Dictionary<string, string>();
            this.Method = string.Empty;
            this.FullRequest = string.Empty;
            this.RequestedPage = string.Empty;
        }
        public void Process()
        {
            FullRequest = StringBuilder.ToString();
            var lines = new List<string>(Regex.Split(FullRequest, "\r\n"));
            if (lines != null && lines.Count > 2)
            {
                var requestLine = lines[0].Split(' ');
                if (requestLine.Length == 3)
                {
                    Method = requestLine[0];
                    var requestedPage = requestLine[1].Split('?');
                    if (requestedPage.Length == 2)
                    {
                        var getData = requestedPage[1].Split('&');
                        foreach (var kvp in getData)
                        {
                            var _kvp = kvp.Split('=');
                            if (_kvp.Length == 2 && !GetData.ContainsKey(_kvp[0]))
                                GetData.Add(_kvp[0], _kvp[1]);
                        }
                    }
                    RequestedPage = requestedPage[0];
                }

                int contentIndex = lines.IndexOf(string.Empty) + 1;
                if (contentIndex > 0 && contentIndex < lines.Count)
                {
                    for (int i = contentIndex; i < lines.Count; i++)
                    {
                        var line = lines[i].Split('=');
                        if (line.Length == 2 && !PostData.ContainsKey(line[0]))
                            PostData.Add(line[0], line[1]);
                    }
                }
            }
        }
    }

    public class WebResponse
    {
        private StringBuilder stringBuilder;
        public string Content
        {
            get { return stringBuilder.ToString(); }
        }
        public string ContentType { get; set; }
        public WebResponse(string contentType)
        {
            this.ContentType = contentType;
            stringBuilder = new StringBuilder();
        }
        public void Write(string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
        }
        public void WriteLine()
        {
            stringBuilder.AppendLine();
        }
        public void WriteLine(string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }
        public override string ToString()
        {
            var response = stringBuilder.ToString();
            var sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("Content-Length: " + Encoding.UTF8.GetByteCount(response));
            sb.AppendLine("Connection: close");
            sb.AppendLine("Content-Type: " + ContentType + "; charset=UTF-8");
            sb.AppendLine("");
            sb.AppendLine(response);
            return sb.ToString();
        }
    }
}