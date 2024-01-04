namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface ISerializer<T>
    {
        public object Deserialize(byte[] bytes);
        public byte[] Serialize(T t);
    }
}
