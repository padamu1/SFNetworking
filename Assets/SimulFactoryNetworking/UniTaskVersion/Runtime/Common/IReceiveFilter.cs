using System.Net.Sockets;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void HeaderFilter(byte[] headerBuffer, out int totalPacketLength);

        public void CheckUnknownPacket(byte[] packet, out SocketError socketError);
    }
}
