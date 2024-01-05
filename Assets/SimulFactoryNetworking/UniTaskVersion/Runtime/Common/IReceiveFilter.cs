using SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void Filter(TcpPacketData tcpPacketData);
    }
}
