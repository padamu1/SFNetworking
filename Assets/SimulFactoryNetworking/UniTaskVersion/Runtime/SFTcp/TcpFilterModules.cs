using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using System;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp
{
    public class TcpFilterModules
    {
        public static void Filter(IReceiveFilter receiveFilter, TcpPacketData tcpPacketData)
        {
            if (tcpPacketData.currentPacketLength == tcpPacketData.totalPacketLength)
            {
                receiveFilter.HeaderFilter(tcpPacketData.receiveBuffer, tcpPacketData.currentIndex, out tcpPacketData.currentIndex, out tcpPacketData.totalPacketLength);
                tcpPacketData.packet = new byte[tcpPacketData.totalPacketLength];
                tcpPacketData.currentPacketLength = 0;
            }

            ParseData(tcpPacketData);
        }

        private static void ParseData(TcpPacketData tcpPacketData)
        {
            int copyLength;
            int lackLength = tcpPacketData.totalPacketLength - tcpPacketData.currentPacketLength;
            if (tcpPacketData.receiveLength - tcpPacketData.currentIndex < lackLength)
            {
                copyLength = tcpPacketData.receiveLength - tcpPacketData.currentIndex;
            }
            else
            {
                copyLength = lackLength;
            }

            Buffer.BlockCopy(tcpPacketData.receiveBuffer, tcpPacketData.currentIndex, tcpPacketData.packet, tcpPacketData.currentPacketLength, copyLength);

            tcpPacketData.currentIndex += copyLength;
            tcpPacketData.currentPacketLength += copyLength;
        }
    }
}
