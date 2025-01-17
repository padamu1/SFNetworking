namespace SimulFactoryNetworking.Unity6.Runtime.Common
{
    public interface ISerializer<T>
    {
        public T Deserialize(byte[] bytes);
        public byte[] Serialize(in T t);
    }
}
