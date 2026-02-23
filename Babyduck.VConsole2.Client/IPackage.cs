namespace Babyduck.VConsole2.Client
{
    public interface IPackage
    {
        public string TypeName { get; }
        public void FromBytes(byte[] payload);
    }
}