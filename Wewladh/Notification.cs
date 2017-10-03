using System;
using System.IO;
using System.Text;

namespace Wewladh
{
    public class Notification
    {
        public string Text { get; set; }
        public uint Checksum { get; private set; }
        public byte[] RawData { get; private set; }

        public Notification()
        {
            this.Text = String.Empty;
        }
        public Notification(string text)
        {
            this.Text = text;
        }

        public void Write(string dataPath)
        {
            string decPath = (dataPath + "\\notification_dec");
            string encPath = (dataPath + "\\notification_enc");

            using (Stream stream = File.Create(decPath))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.GetEncoding(949)))
                {
                    writer.Write(Encoding.GetEncoding(949).GetBytes(this.Text));
                }
            }

            this.Checksum = ~CRC32.Calculate(File.ReadAllBytes(decPath));
            ZLIB.Compress(decPath, encPath);
            this.RawData = File.ReadAllBytes(encPath);

            File.Delete(decPath);
            File.Delete(encPath);
        }
    }
}