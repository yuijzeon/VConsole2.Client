namespace Babyduck.VConsole2.Client
{
    public interface IPackage
    {
        public bool IsCompatible(ChunkHeader header);
        public void LoadFrom(ChunkHeader header, byte[] payload);
    }
}