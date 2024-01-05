using System.Net.Sockets;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp
{
    public class TcpPacketData
    {
        public int bufferSize;
        public byte[] receiveBuffer;
        public int receiveLength;
        public int currentIndex;
        public byte[] packet;
        public int currentPacketLength;
        public int totalPacketLength;
        public SocketError socketError;

        public TcpPacketData(int bufferSize)
        {
            this.bufferSize = bufferSize;
            receiveBuffer = new byte[bufferSize];
        }
    }
}
