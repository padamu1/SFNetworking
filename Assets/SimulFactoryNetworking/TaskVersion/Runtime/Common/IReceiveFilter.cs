using SimulFactoryNetworking.TaskVersion.Runtime.SFTcp;

namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void Filter(TcpPacketData tcpPacketData);
    }
}
