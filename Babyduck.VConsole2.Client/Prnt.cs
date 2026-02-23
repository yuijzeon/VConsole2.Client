using System;
using System.Buffers.Binary;
using System.Text;

namespace Babyduck.VConsole2.Client
{
    public class Prnt : IPackage
    {
        public string TypeName => "PRNT";
        public uint ChannelId { get; set; }
        public ulong Timestamp { get; set; }
        public uint Rgba { get; set; }
        public string Message { get; set; }

        public void FromBytes(byte[] payload)
        {
            ChannelId = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(0, 4));
            Timestamp = BitConverter.ToUInt64(payload, 4);
            Rgba = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(12, 4));
            Message = Encoding.UTF8.GetString(payload, 28, payload.Length - 28);
        }
    }
}