using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CSharp;
using MySql.Data.MySqlClient;

namespace Wewladh
{
    public static class Program
    {
        public static bool RunningSlowly { get; private set; }
        public static int Port { get; private set; }
        public static string HostName { get; private set; }
        private static string frameworkPath;
        private static string connectorPath;

        public static bool ShouldReset = true;
        public static bool Winter = (DateTime.UtcNow.Month < 3 || DateTime.UtcNow.Month > 11);
        public static readonly long SessionID = DateTime.UtcNow.ToFileTimeUtc();

        private static DateTime baseResetTime = DateTime.UtcNow.AddHours(24);
        private static ScheduledReset scheduledReset = new ScheduledReset(baseResetTime);
        private static DateTime lastDatabaseConnection;
        public static MySqlConnection MySqlConnection { get; private set; }
        private static string mySqlConnectionString = string.Empty;

        public static int IsLoaded { get; private set; }

        public static readonly string StartupPath = Directory.GetCurrentDirectory();

        public static uint Checksum { get; private set; }
        public static byte[] RawData { get; private set; }
        public static LobbyServer LobbyServer { get; private set; }
        public static List<GameServer> GameServers { get; private set; }
        public static readonly object SyncObj = new object();
        public static Thread ServerThread;
        private static Random _random = new Random();

        public static HashSet<string> IPBanList { get; private set; }

        public static int Random()
        {
            return _random.Next();
        }
        public static int Random(int max)
        {
            return _random.Next(max);
        }
        public static int Random(int min, int max)
        {
            return _random.Next(min, max);
        }

        public static uint RandomUInt32()
        {
            return (uint)_random.Next();
        }
        public static uint RandomUInt32(int max)
        {
            return (uint)_random.Next(max);
        }
        public static uint RandomUInt32(int min, int max)
        {
            return (uint)_random.Next(min, max);
        }

        private static void Main(string[] args)
        {
            XDocument doc = XDocument.Load("config.xml");
            HostName = doc.Element("config").Element("hostname").Value;
            Port = (int)doc.Element("config").Element("port");
            //frameworkPath = doc.Element("config").Element("framework").Value;
            //connectorPath = doc.Element("config").Element("connector").Value;

            var mysqlserver = doc.Element("config").Element("mysql").Attribute("server").Value;
            var mysqldatabase = doc.Element("config").Element("mysql").Attribute("database").Value;
            var mysqlusername = doc.Element("config").Element("mysql").Attribute("username").Value;
            var mysqlpassword = doc.Element("config").Element("mysql").Attribute("password").Value;
            mySqlConnectionString = string.Format("SERVER={0}; DATABASE={1}; UID={2}; PASSWORD={3}",
                mysqlserver, mysqldatabase, mysqlusername, mysqlpassword);

            IPBanList = new HashSet<string>();
            foreach (var line in File.ReadAllLines("ipban.txt"))
            {
                var regex = new Regex("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                if (regex.IsMatch(line))
                    IPBanList.Add(line);
            }

            IsLoaded = -1;

            try
            {
                ConnectToDatabase();

                Map.LoadCollisionData("shared\\sotp.dat");

                LoadServers();

                IsLoaded = 1;
            }
            catch (Exception e)
            {
                IsLoaded = 0;
                Program.WriteLine(e);
                Console.ReadKey(true);
            }

            if (IsLoaded == 1)
            {
                while (DateTime.UtcNow < scheduledReset.ResetTime)
                {
                    lock (SyncObj)
                    {
                        DateTime dt1 = DateTime.UtcNow;
                        LobbyServer.AcceptConnection();
                        foreach (var client in LobbyServer.Clients)
                        {
                            if (DateTime.UtcNow.Subtract(client.LastPacket).TotalMinutes > 1)
                                client.Connected = false;
                        }
                        foreach (GameServer gs in GameServers)
                        {
                            try
                            {
                                gs.UpdateGame();
                                gs.AcceptConnection();
                                gs.LoginServer.AcceptConnection();
                            }
                            catch (Exception e)
                            {
                                if (!Directory.Exists("CrashLogs"))
                                    Directory.CreateDirectory("CrashLogs");
                                string filename = ("CrashLogs\\crashlog-" + DateTime.Now.ToFileTime() + ".txt");
                                var sw = new StreamWriter(File.Create(filename));
                                sw.WriteLine(e.ToString());
                                sw.Close();
                                Program.WriteLine(e);
                                Console.ReadKey(true);
                                Process.Start(Process.GetCurrentProcess().ProcessName + ".exe");
                                Process.GetCurrentProcess().Kill();
                                return;
                            }

                            foreach (ScheduledReset.Warning sw in scheduledReset.Warnings.ToArray())
                            {
                                if (DateTime.UtcNow > sw.WarningTime)
                                {
                                    foreach (Client c in gs.Clients)
                                    {
                                        c.SendMessage(sw.Message);
                                        c.SendMessage(sw.Message);
                                        c.SendMessage(sw.Message);
                                    }
                                    scheduledReset.Warnings.Remove(sw);
                                }
                            }

                            if (scheduledReset.Cancelled)
                            {
                                foreach (Client c in gs.Clients)
                                {
                                    c.SendMessage("Server reset cancelled.");
                                    c.SendMessage("Server reset cancelled.");
                                    c.SendMessage("Server reset cancelled.");
                                }
                                scheduledReset.Cancelled = false;
                            }
                        }
                        if (DateTime.UtcNow.Subtract(lastDatabaseConnection).TotalHours > 1)
                        {
                            if (MySqlConnection != null)
                            {
                                MySqlConnection.Close();
                                ConnectToDatabase();
                            }
                        }
                        DateTime dt2 = DateTime.UtcNow;
                        if (dt2.Subtract(dt1).TotalSeconds > 0.150)
                        {
                            RunningSlowly = true;
                            //WriteLine("[{0}]: the server is running slowly!", DateTime.UtcNow);
                        }
                        else
                        {
                            RunningSlowly = false;
                        }
                    }

                    Thread.Sleep(50);

                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.Q: ScheduleReset(0); break;
                        }
                    }
                }

                ShutdownGameServers();

                if (MySqlConnection != null)
                    MySqlConnection.Close();

                if (ShouldReset)
                    Process.Start(Process.GetCurrentProcess().ProcessName + ".exe");

                Process.GetCurrentProcess().Kill();
            }
        }

        public static void ConnectToDatabase()
        {
            Program.Write("Connecting to MySQL database... ");
            DateTime dt1 = DateTime.UtcNow;
            MySqlConnection = new MySqlConnection(mySqlConnectionString);
            MySqlConnection.Open();
            DateTime dt2 = DateTime.UtcNow;
            Program.WriteLine("done in {0} milliseconds!", dt2.Subtract(dt1).TotalMilliseconds);
            lastDatabaseConnection = DateTime.UtcNow;
        }
        public static void LoadServers()
        {
            LobbyServer = new LobbyServer();
            GameServers = new List<GameServer>();

            string[] directories = Directory.GetDirectories("GameServers");
            foreach (string directory in directories)
            {
                if (File.Exists(directory + "\\config.xml"))
                {
                    GameServer gs = new GameServer(directory);
                    gs.Index = GameServers.Count;
                    GameServers.Add(gs);
                    gs.LoginServer.Start();
                    gs.Start();
                }
            }

            string decPath = (StartupPath + "\\mServer_dec");
            string encPath = (StartupPath + "\\mServer_enc");

            using (Stream stream = File.Create(decPath))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.GetEncoding(949)))
                {
                    byte i = 0;
                    writer.Write((byte)GameServers.Count);
                    foreach (GameServer gs in GameServers)
                    {
                        byte[] addressBytes = gs.LoginServer.EndPoint.Address.GetAddressBytes();
                        Array.Reverse(addressBytes);
                        writer.Write(i++);
                        writer.Write(addressBytes);
                        writer.Write((byte)(gs.LoginServer.EndPoint.Port / 256));
                        writer.Write((byte)(gs.LoginServer.EndPoint.Port % 256));
                        writer.Write(Encoding.GetEncoding(949).GetBytes(string.Format("{0};{1}\0", gs.Name, gs.Description)));
                        writer.Write((byte)0x00);
                    }
                }
            }

            Checksum = ~CRC32.Calculate(File.ReadAllBytes(decPath));
            ZLIB.Compress(decPath, encPath);
            RawData = File.ReadAllBytes(encPath);

            File.Delete(decPath);
            File.Delete(encPath);

            Program.WriteLine("{0} game servers loaded!", GameServers.Count);
            LobbyServer.Start();
        }
        public static void ShutdownGameServers()
        {
            foreach (GameServer gs in GameServers)
            {
                lock (Program.SyncObj)
                {
                    var com = Program.MySqlConnection.CreateCommand();
                    com.CommandText = "TRUNCATE TABLE guilds";
                    com.ExecuteNonQuery();
                    foreach (var guild in gs.Guilds.Values)
                    {
                        com = Program.MySqlConnection.CreateCommand();
                        com.CommandText = "INSERT INTO guilds VALUES (@name, @leader, @members, @council, @level, @experience)";
                        com.Parameters.AddWithValue("@name", guild.Name);
                        com.Parameters.AddWithValue("@leader", guild.Leader);
                        var members = new string[guild.Members.Count];
                        guild.Members.CopyTo(members);
                        com.Parameters.AddWithValue("@members", string.Join(";", members));
                        var council = new string[guild.Council.Count];
                        guild.Council.CopyTo(council);
                        com.Parameters.AddWithValue("@council", string.Join(";", council));
                        com.Parameters.AddWithValue("@level", guild.Level);
                        com.Parameters.AddWithValue("@experience", guild.Experience);
                        com.ExecuteNonQuery();
                    }
                    gs.Shutdown();
                }

                while (gs.Clients.Count != 0)
                {
                    Thread.Sleep(10);
                }
            }
        }

        public static void ScheduleReset(int time)
        {
            switch (time)
            {
                case 0:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddSeconds(1));
                        Program.WriteLine("Scheduled reset for 1 second");
                    } break;
                case 1:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddSeconds(30));
                        Program.WriteLine("Scheduled reset for 30 seconds");
                    } break;
                case 2:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddMinutes(1));
                        Program.WriteLine("Scheduled reset for 1 minute");
                    } break;
                case 3:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddMinutes(5));
                        Program.WriteLine("Scheduled reset for 5 minutes");
                    } break;
                case 4:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddMinutes(10));
                        Program.WriteLine("Scheduled reset for 10 minutes");
                    } break;
                case 5:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddMinutes(15));
                        Program.WriteLine("Scheduled reset for 15 minutes");
                    } break;
                case 6:
                    {
                        scheduledReset = new ScheduledReset(DateTime.UtcNow.AddMinutes(30));
                        Program.WriteLine("Scheduled reset for 30 minutes");
                    } break;
                case -1:
                    {
                        scheduledReset.Cancel(baseResetTime);
                        Program.WriteLine("Cancelled scheduled reset");
                    } break;
            }
        }

        public static Assembly Compile(params string[] path)
        {
            var options = new Dictionary<string, string>();
            options.Add("CompilerVersion", "v4.0");

            var cscp = new CSharpCodeProvider(options);
            var cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("MySql.Data.dll");
            //cp.ReferencedAssemblies.Add(connectorPath);
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            cp.ReferencedAssemblies.Add("System.Xml.dll");
            cp.ReferencedAssemblies.Add("System.Xml.Linq.dll");
#if DEBUG
            cp.ReferencedAssemblies.Add("Wewladh.exe");
#else
            cp.ReferencedAssemblies.Add(Process.GetCurrentProcess().ProcessName + ".exe");
#endif

            cp.WarningLevel = 3;
            cp.CompilerOptions = "/optimize /lib:\"C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.0\"";
            //cp.CompilerOptions = "/optimize /lib:\"" + frameworkPath + "\"";

            cp.GenerateExecutable = false;
            cp.GenerateInMemory = false;

            var cr = cscp.CompileAssemblyFromFile(cp, path);

            if (cr.Errors.HasErrors)
            {
                foreach (CompilerError ce in cr.Errors)
                {
                    WriteLine();
                    WriteLine("======== Compile Error ===================================");
                    WriteLine("{0} [ line {1}, column {2} ]", ce.FileName, ce.Line, ce.Column);
                    WriteLine(ce.ErrorText);
                    WriteLine("==========================================================");
                }
                return null;
            }

            return cr.CompiledAssembly;
        }

        public static void Write(string value)
        {
            Console.Write(value);
        }
        public static void Write(object value)
        {
            Console.Write(value);
        }
        public static void Write(string format, params object[] args)
        {
            Console.Write(format, args);
        }
        public static void WriteLine()
        {
            Console.WriteLine();
        }
        public static void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
        public static void WriteLine(object value)
        {
            Console.WriteLine(value);
        }
        public static void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class ScheduledReset
    {
        public DateTime ResetTime { get; set; }
        public List<Warning> Warnings { get; set; }
        public bool Cancelled { get; set; }

        public ScheduledReset(DateTime resetTime)
        {
            this.ResetTime = resetTime;
            this.Warnings = new List<Warning>();
            TimeSpan timeSpan = resetTime.Subtract(DateTime.UtcNow);
            if (timeSpan.TotalMinutes >= 60)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(1, 0, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 hour."));
            }
            if (timeSpan.TotalMinutes >= 30)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 30, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 30 minutes."));
            }
            if (timeSpan.TotalMinutes >= 15)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 15, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 15 minutes."));
            }
            if (timeSpan.TotalMinutes >= 10)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 10, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 10 minutes."));
            }
            if (timeSpan.TotalMinutes >= 9)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 9, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 9 minutes."));
            }
            if (timeSpan.TotalMinutes >= 8)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 8, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 8 minutes."));
            }
            if (timeSpan.TotalMinutes >= 7)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 7, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 7 minutes."));
            }
            if (timeSpan.TotalMinutes >= 6)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 6, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 6 minutes."));
            }
            if (timeSpan.TotalMinutes >= 5)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 5, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 5 minutes."));
            }
            if (timeSpan.TotalMinutes >= 4)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 4, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 4 minutes."));
            }
            if (timeSpan.TotalMinutes >= 3)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 3, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 3 minutes."));
            }
            if (timeSpan.TotalMinutes >= 2)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 2, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 2 minutes."));
            }
            if (timeSpan.TotalSeconds >= 60)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 1, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 minute."));
            }
            if (timeSpan.TotalSeconds >= 50)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 50));
                Warnings.Add(new Warning(warningTime, "Server reset in 50 seconds."));
            }
            if (timeSpan.TotalSeconds >= 40)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 40));
                Warnings.Add(new Warning(warningTime, "Server reset in 40 seconds."));
            }
            if (timeSpan.TotalSeconds >= 30)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 30));
                Warnings.Add(new Warning(warningTime, "Server reset in 30 seconds."));
            }
            if (timeSpan.TotalSeconds >= 20)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 20));
                Warnings.Add(new Warning(warningTime, "Server reset in 20 seconds."));
            }
            if (timeSpan.TotalSeconds >= 10)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 10));
                Warnings.Add(new Warning(warningTime, "Server reset in 10 seconds."));
            }
            if (timeSpan.TotalSeconds >= 9)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 9));
                Warnings.Add(new Warning(warningTime, "Server reset in 9 seconds."));
            }
            if (timeSpan.TotalSeconds >= 8)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 8));
                Warnings.Add(new Warning(warningTime, "Server reset in 8 seconds."));
            }
            if (timeSpan.TotalSeconds >= 7)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 7));
                Warnings.Add(new Warning(warningTime, "Server reset in 7 seconds."));
            }
            if (timeSpan.TotalSeconds >= 6)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 6));
                Warnings.Add(new Warning(warningTime, "Server reset in 6 seconds."));
            }
            if (timeSpan.TotalSeconds >= 5)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 5));
                Warnings.Add(new Warning(warningTime, "Server reset in 5 seconds."));
            }
            if (timeSpan.TotalSeconds >= 4)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 4));
                Warnings.Add(new Warning(warningTime, "Server reset in 4 seconds."));
            }
            if (timeSpan.TotalSeconds >= 3)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 3));
                Warnings.Add(new Warning(warningTime, "Server reset in 3 seconds."));
            }
            if (timeSpan.TotalSeconds >= 2)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 2));
                Warnings.Add(new Warning(warningTime, "Server reset in 2 seconds."));
            }
            if (timeSpan.TotalSeconds >= 1)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 1));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 second."));
            }
        }
        public ScheduledReset(TimeSpan timeSpan)
            : this(DateTime.UtcNow.Add(timeSpan))
        {

        }
        public void Cancel(DateTime resetTime)
        {
            this.Cancelled = true;
            this.ResetTime = resetTime;
            this.Warnings = new List<Warning>();
            TimeSpan timeSpan = resetTime.Subtract(DateTime.UtcNow);
            if (timeSpan.TotalMinutes >= 60)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(1, 0, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 hour."));
            }
            if (timeSpan.TotalMinutes >= 30)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 30, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 30 minutes."));
            }
            if (timeSpan.TotalMinutes >= 15)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 15, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 15 minutes."));
            }
            if (timeSpan.TotalMinutes >= 10)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 10, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 10 minutes."));
            }
            if (timeSpan.TotalMinutes >= 9)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 9, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 9 minutes."));
            }
            if (timeSpan.TotalMinutes >= 8)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 8, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 8 minutes."));
            }
            if (timeSpan.TotalMinutes >= 7)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 7, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 7 minutes."));
            }
            if (timeSpan.TotalMinutes >= 6)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 6, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 6 minutes."));
            }
            if (timeSpan.TotalMinutes >= 5)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 5, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 5 minutes."));
            }
            if (timeSpan.TotalMinutes >= 4)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 4, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 4 minutes."));
            }
            if (timeSpan.TotalMinutes >= 3)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 3, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 3 minutes."));
            }
            if (timeSpan.TotalMinutes >= 2)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 2, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 2 minutes."));
            }
            if (timeSpan.TotalSeconds >= 60)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 1, 0));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 minute."));
            }
            if (timeSpan.TotalSeconds >= 50)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 50));
                Warnings.Add(new Warning(warningTime, "Server reset in 50 seconds."));
            }
            if (timeSpan.TotalSeconds >= 40)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 40));
                Warnings.Add(new Warning(warningTime, "Server reset in 40 seconds."));
            }
            if (timeSpan.TotalSeconds >= 30)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 30));
                Warnings.Add(new Warning(warningTime, "Server reset in 30 seconds."));
            }
            if (timeSpan.TotalSeconds >= 20)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 20));
                Warnings.Add(new Warning(warningTime, "Server reset in 20 seconds."));
            }
            if (timeSpan.TotalSeconds >= 10)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 10));
                Warnings.Add(new Warning(warningTime, "Server reset in 10 seconds."));
            }
            if (timeSpan.TotalSeconds >= 9)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 9));
                Warnings.Add(new Warning(warningTime, "Server reset in 9 seconds."));
            }
            if (timeSpan.TotalSeconds >= 8)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 8));
                Warnings.Add(new Warning(warningTime, "Server reset in 8 seconds."));
            }
            if (timeSpan.TotalSeconds >= 7)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 7));
                Warnings.Add(new Warning(warningTime, "Server reset in 7 seconds."));
            }
            if (timeSpan.TotalSeconds >= 6)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 6));
                Warnings.Add(new Warning(warningTime, "Server reset in 6 seconds."));
            }
            if (timeSpan.TotalSeconds >= 5)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 5));
                Warnings.Add(new Warning(warningTime, "Server reset in 5 seconds."));
            }
            if (timeSpan.TotalSeconds >= 4)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 4));
                Warnings.Add(new Warning(warningTime, "Server reset in 4 seconds."));
            }
            if (timeSpan.TotalSeconds >= 3)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 3));
                Warnings.Add(new Warning(warningTime, "Server reset in 3 seconds."));
            }
            if (timeSpan.TotalSeconds >= 2)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 2));
                Warnings.Add(new Warning(warningTime, "Server reset in 2 seconds."));
            }
            if (timeSpan.TotalSeconds >= 1)
            {
                DateTime warningTime = resetTime.Subtract(new TimeSpan(0, 0, 1));
                Warnings.Add(new Warning(warningTime, "Server reset in 1 second."));
            }
        }

        public class Warning
        {
            public DateTime WarningTime { get; set; }
            public string Message { get; set; }

            public Warning(DateTime dateTime, string message)
            {
                this.WarningTime = dateTime;
                this.Message = message;
            }

            public override string ToString()
            {
                return string.Format("WarningTime: {0}, Message: {1}", WarningTime, Message);
            }
        }
    }
}