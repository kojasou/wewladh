using System;
using System.Text;

namespace Wewladh
{
    public abstract class Packet
    {
        private int position;
        protected byte[] rawData;

        protected byte signature;
        public byte Signature
        {
            get { return signature; }
            set { signature = value; }
        }

        protected ushort length;
        public ushort Length
        {
            get { return length; }
            set { length = value; }
        }

        protected byte opcode;
        public byte Opcode
        {
            get { return opcode; }
            set { opcode = value; }
        }

        protected byte ordinal;
        public byte Ordinal
        {
            get { return ordinal; }
            set { ordinal = value; }
        }

        protected byte[] bodyData;
        public byte[] BodyData
        {
            get { return bodyData; }
            set { bodyData = value; }
        }

        public byte[] Header
        {
            get
            {
                byte[] header = new byte[(ShouldEncrypt() ? 5 : 4)];
                header[0] = signature;
                header[1] = (byte)(length / 256);
                header[2] = (byte)(length % 256);
                header[3] = opcode;
                if (ShouldEncrypt())
                {
                    header[4] = ordinal;
                }
                return header;
            }
        }

        public int Position
        {
            get { return position; }
            set { position = value; }
        }

        public abstract bool ShouldEncrypt();

        public byte[] Read(int length)
        {
            if ((position + (length - 1)) < bodyData.Length)
            {
                byte[] buffer = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = ReadByte();
                }
                return buffer;
            }
            throw new IndexOutOfRangeException();
        }
        public byte ReadByte()
        {
            if (position < bodyData.Length)
            {
                return bodyData[position++];
            }
            throw new IndexOutOfRangeException();
        }
        public bool ReadBoolean()
        {
            if (position < bodyData.Length)
            {
                return (bodyData[position++] > 0);
            }
            throw new IndexOutOfRangeException();
        }
        public short ReadInt16()
        {
            if ((position + 1) < bodyData.Length)
            {
                return (short)(bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public ushort ReadUInt16()
        {
            if ((position + 1) < bodyData.Length)
            {
                return (ushort)(bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public int ReadInt32()
        {
            if ((position + 3) < bodyData.Length)
            {
                return (bodyData[position++] << 24 | bodyData[position++] << 16 | bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public uint ReadUInt32()
        {
            if ((position + 3) < bodyData.Length)
            {
                return (uint)(bodyData[position++] << 24 | bodyData[position++] << 16 | bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public long ReadInt64()
        {
            if ((position + 7) < bodyData.Length)
            {
                return (long)(bodyData[position++] << 56 | bodyData[position++] << 48 | bodyData[position++] << 40 | bodyData[position++] << 32 | bodyData[position++] << 24 | bodyData[position++] << 16 | bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public ulong ReadUInt64()
        {
            if ((position + 7) < bodyData.Length)
            {
                return (ulong)(bodyData[position++] << 56 | bodyData[position++] << 48 | bodyData[position++] << 40 | bodyData[position++] << 32 | bodyData[position++] << 24 | bodyData[position++] << 16 | bodyData[position++] << 8 | bodyData[position++]);
            }
            throw new IndexOutOfRangeException();
        }
        public string ReadString(int length)
        {
            if ((position + (length - 1)) < bodyData.Length)
            {
                return Encoding.GetEncoding(949).GetString(Read(length));
            }
            throw new IndexOutOfRangeException();
        }
        public string ReadString()
        {
            return ReadString(bodyData.Length - position).Trim('\0');
        }
        public string ReadString8()
        {
            var length = ReadByte();
            return ReadString(length);
        }
        public string ReadString16()
        {
            var length = ReadUInt16();
            return ReadString(length);
        }

        public void Write(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                WriteByte(value[i]);
            }
        }

        public void WriteSByte(sbyte value)
        {
            int length = bodyData.Length;
            Array.Resize<byte>(ref bodyData, (length + 1));
            bodyData[length] = (byte)value;
        }
        public void WriteByte(byte value)
        {
            int length = bodyData.Length;
            Array.Resize<byte>(ref bodyData, (length + 1));
            bodyData[length] = value;
        }
        public void WriteByte(bool value)
        {
            int length = bodyData.Length;
            Array.Resize<byte>(ref bodyData, (length + 1));
            bodyData[length] = (byte)(value ? 1 : 0);
        }
        
        public void WriteInt16(short value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }
        public void WriteUInt16(ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }

        public void WriteInt32(int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }
        public void WriteUInt32(uint value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }

        public void WriteInt64(long value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }
        public void WriteUInt64(ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write(data);
        }

        public void WriteString(string value)
        {
            byte[] buffer = Encoding.GetEncoding(949).GetBytes(value);
            Write(buffer);
        }
        public void WriteString8(string value)
        {
            byte[] buffer = Encoding.GetEncoding(949).GetBytes(value);
            WriteByte((byte)buffer.Length);
            Write(buffer);
        }
        public void WriteString8(string format, params object[] args)
        {
            WriteString8(String.Format(format, args));
        }
        public void WriteString16(string value)
        {
            byte[] buffer = Encoding.GetEncoding(949).GetBytes(value);
            WriteUInt16((ushort)buffer.Length);
            Write(buffer);
        }
        public void WriteString16(string format, params object[] args)
        {
            WriteString16(String.Format(format, args));
        }

        public override string ToString()
        {
            return String.Format("{0} : {1}", BitConverter.ToString(this.Header), BitConverter.ToString(this.BodyData));
        }
    }

    public class ClientPacket : Packet
    {
        public ClientPacket(byte opcode)
        {
            this.signature = 0xAA;
            this.bodyData = new byte[0];
            this.opcode = opcode;
        }
        public ClientPacket(byte[] rawData)
        {
            this.rawData = rawData;
            this.signature = rawData[0];
            this.length = (ushort)((rawData[1] * 256) + rawData[2]);
            this.opcode = rawData[3];
            if (this.ShouldEncrypt())
            {
                this.ordinal = rawData[4];
                this.bodyData = new byte[rawData.Length - 5];
                Array.Copy(rawData, 5, bodyData, 0, bodyData.Length);
            }
            else
            {
                this.bodyData = new byte[rawData.Length - 4];
                Array.Copy(rawData, 4, bodyData, 0, bodyData.Length);
            }
        }
        public override bool ShouldEncrypt()
        {
            return ((Opcode != 0x00) && (Opcode != 0x0B) && (Opcode != 0x10) && (Opcode != 0x62));
        }
    }

    public class ServerPacket : Packet
    {
        public ServerPacket(byte opcode)
        {
            this.signature = 0xAA;
            this.bodyData = new byte[0];
            this.opcode = opcode;
        }
        public ServerPacket(byte[] rawData)
        {
            this.rawData = rawData;
            this.signature = rawData[0];
            this.length = (ushort)((rawData[1] * 256) + rawData[2]);
            this.opcode = rawData[3];
            if (this.ShouldEncrypt())
            {
                this.ordinal = rawData[4];
                this.bodyData = new byte[rawData.Length - 5];
                Array.Copy(rawData, 5, bodyData, 0, bodyData.Length);
            }
            else
            {
                this.bodyData = new byte[rawData.Length - 4];
                Array.Copy(rawData, 4, bodyData, 0, bodyData.Length);
            }
        }
        public override bool ShouldEncrypt()
        {
            return ((Opcode != 0x00) && (Opcode != 0x03) && (Opcode != 0x7E));
        }
    }
}