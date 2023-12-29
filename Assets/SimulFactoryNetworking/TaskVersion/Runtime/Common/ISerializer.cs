namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface ISerializer<T>
    {
        public object Deserialize(byte[] bytes, out byte[] nextData);
        public byte[] Serialize(T t);
    }
}
