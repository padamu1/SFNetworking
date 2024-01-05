namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface ISerializer<T>
    {
        public T Deserialize(byte[] bytes);
        public byte[] Serialize(T t);
    }
}
