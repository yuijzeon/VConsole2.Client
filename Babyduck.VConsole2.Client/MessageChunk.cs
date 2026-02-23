using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Babyduck.VConsole2.Client
{
    public class MessageChunk
    {
        public ChunkHeader Header { get; set; }
        public byte[] Payload { get; set; }

        public IPackage? ParsePayload<T>() where T : IPackage, new()
        {
            var package = new T();
            if (Header.GetTypeName() != package.TypeName)
            {
                return null;
            }
            
            package.FromBytes(Payload);
            return package;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChunkHeader
    {
        public uint Type;
        public ushort Version;
        public uint Length;
        public ushort Handle;

        public string GetTypeName()
        {
            var bytes = BitConverter.GetBytes(Type);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}