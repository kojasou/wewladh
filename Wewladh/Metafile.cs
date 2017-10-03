using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Wewladh
{
    public class Metafile
    {
        public string Name { get; set; }
        public List<Element> Elements { get; set; }
        public uint Checksum { get; private set; }
        public byte[] RawData { get; private set; }

        public Metafile()
        {
            this.Name = String.Empty;
            this.Elements = new List<Element>();
        }
        public Metafile(string name)
        {
            this.Name = name;
            this.Elements = new List<Element>();
        }
        public Metafile(string name, params Element[] elements)
        {
            this.Name = name;
            this.Elements = new List<Element>(elements);
        }

        public class Element
        {
            public string Text { get; set; }
            public List<string> Properties { get; set; }

            public Element()
            {
                this.Text = String.Empty;
                this.Properties = new List<string>();
            }
            public Element(string text)
            {
                this.Text = text;
                this.Properties = new List<string>();
            }
            public Element(string text, params string[] properties)
            {
                this.Text = text;
                this.Properties = new List<string>(properties);
            }
        }

        public static Metafile Read(string path)
        {
            if (File.Exists(path))
            {
                Metafile mf = new Metafile(Path.GetFileName(path));

                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.GetEncoding(949)))
                    {
                        int ElementCount = (reader.ReadByte() << 8 | reader.ReadByte());
                        for (int i = 0; i < ElementCount; i++)
                        {
                            Element el = new Element(reader.ReadString());
                            int PropertyCount = (reader.ReadByte() << 8 | reader.ReadByte());
                            for (int p = 0; p < PropertyCount; p++)
                            {
                                int length = (reader.ReadByte() << 8 | reader.ReadByte());
                                byte[] line = reader.ReadBytes(length);
                                el.Properties.Add(Encoding.GetEncoding(949).GetString(line));
                            }
                            mf.Elements.Add(el);
                        }
                    }
                }

                return mf;
            }

            return null;
        }
        public void Write(string dataPath)
        {
            string decPath = (dataPath + "\\metafile_" + Name + "_dec");
            string encPath = (dataPath + "\\metafile_" + Name + "_enc");

            using (Stream stream = File.Create(decPath))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.GetEncoding(949)))
                {
                    writer.Write((byte)(Elements.Count / 256));
                    writer.Write((byte)(Elements.Count % 256));
                    foreach (Element el in Elements)
                    {
                        writer.Write(el.Text);
                        writer.Write((byte)(el.Properties.Count / 256));
                        writer.Write((byte)(el.Properties.Count % 256));
                        foreach (string p in el.Properties)
                        {
                            byte[] data = Encoding.GetEncoding(949).GetBytes(p);
                            writer.Write((byte)(data.Length / 256));
                            writer.Write((byte)(data.Length % 256));
                            writer.Write(data);
                        }
                    }
                    writer.Flush();
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