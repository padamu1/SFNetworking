using SimulFactoryNetworking.Unity6.Runtime.SFTcp;
using System.Net.Sockets;

namespace SimulFactoryNetworking.Unity6.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void HeaderFilter(byte[] headerBuffer, out int totalPacketLength);

        public void CheckUnknownPacket(byte[] packet, out SocketError socketError);
    }
}
