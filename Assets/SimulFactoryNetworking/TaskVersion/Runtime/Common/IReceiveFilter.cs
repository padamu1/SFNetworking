using SimulFactoryNetworking.TaskVersion.Runtime.SFTcp;

namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void HeaderFilter(byte[] receiveBuffer, int currentIndex, out int nextIndex, out int totalPacketLength);
    }
}
