using SimulFactoryNetworking.TaskVersion.Runtime.SFTcp;
using System.Net.Sockets;

namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void HeaderFilter(byte[] headerBuffer, out int totalPacketLength);

        public void CheckUnknownPacket(byte[] packet, out SocketError socketError);
    }
}
