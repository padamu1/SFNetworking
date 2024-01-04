namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public byte[] Filter(byte[] incomingData , int startIndex, out int nextIndex);
    }
}
