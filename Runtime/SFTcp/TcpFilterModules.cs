using SimulFactoryNetworking.Unity6.Runtime.Common;
using System;

namespace SimulFactoryNetworking.Unity6.Runtime.SFTcp
{
    public class TcpFilterModules
    {
        public static void Filter(IReceiveFilter receiveFilter, TcpPacketData tcpPacketData)
        {
            if (tcpPacketData.currentPacketLength == tcpPacketData.totalPacketLength)
            {
                ParseHeader(receiveFilter, tcpPacketData);
            }

            if (tcpPacketData.headerIndex == 0)
            {
                ParseData(tcpPacketData);
            }
        }

        private static void ParseHeader(IReceiveFilter receiveFilter, TcpPacketData tcpPacketData)
        {
            int headerLength = tcpPacketData.receiveLength - tcpPacketData.currentIndex - tcpPacketData.headerIndex;

            if (headerLength + tcpPacketData.headerIndex > tcpPacketData.headerBufferSize)
            {
                headerLength = tcpPacketData.headerBufferSize - tcpPacketData.headerIndex;
            }

            Buffer.BlockCopy(tcpPacketData.receiveBuffer, tcpPacketData.currentIndex, tcpPacketData.headerBuffer, tcpPacketData.headerIndex, headerLength);

            if (headerLength + tcpPacketData.headerIndex != tcpPacketData.headerBufferSize)
            {
                tcpPacketData.headerIndex = headerLength;
            }
            else
            {
                tcpPacketData.headerIndex = 0;
            }

            tcpPacketData.currentIndex += headerLength;

            if (tcpPacketData.headerIndex == 0)
            {
                receiveFilter.HeaderFilter(tcpPacketData.headerBuffer, out tcpPacketData.totalPacketLength);

                tcpPacketData.packet = new byte[tcpPacketData.totalPacketLength];
                tcpPacketData.currentPacketLength = 0;
            }
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
