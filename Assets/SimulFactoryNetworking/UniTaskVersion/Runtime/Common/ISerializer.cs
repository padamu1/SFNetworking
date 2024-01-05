namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Common
{
    public interface ISerializer<T>
    {
        public T Deserialize(byte[] bytes);
        public byte[] Serialize(T t);
    }
}
