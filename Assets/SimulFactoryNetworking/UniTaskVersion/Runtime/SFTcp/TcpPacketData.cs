using System;
using System.Net.Sockets;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp
{
    public class TcpPacketData
    {
        public byte[] headerBuffer;
        public int headerBufferSize;
        public int headerIndex;

        public int bufferSize;
        public byte[] receiveBuffer;
        public int receiveLength;
        public int currentIndex;
        public byte[] packet;
        public int currentPacketLength;
        public int totalPacketLength;
        public SocketError socketError;

        public TcpPacketData(int bufferSize, int headerBufferSize)
        {
            this.bufferSize = bufferSize;
            receiveBuffer = new byte[bufferSize];

            this.headerBuffer = new byte[headerBufferSize];
            this.headerBufferSize = headerBufferSize;
        }
    }
}
