namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public byte[] Filter(byte[] incomingData , out byte[] outgoingData);
    }
}
