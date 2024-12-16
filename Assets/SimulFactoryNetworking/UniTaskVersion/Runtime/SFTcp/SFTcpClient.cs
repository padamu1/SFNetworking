using Cysharp.Threading.Tasks;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp
{
    public class SFTcpClient<T> : SFClient
    {
        public event EventHandler<DisconnectEventArgs> Disconnected;

        private ISerializer<T> serializer;
        private Queue<T> receivePacketQueue;
        private IReceiveFilter receiveFilter;
        private TcpPacketData tcpPacketData;
        private int receiveDelayMilliSeconds;

        public SFTcpClient(int headerBufferSize, int receiveDelayMilliSeconds, IReceiveFilter receiveFilter, ISerializer<T> serializer) : base()
        {
            this.receiveFilter = receiveFilter;
            this.serializer = serializer;
            this.receiveDelayMilliSeconds = receiveDelayMilliSeconds;
            tcpPacketData = new TcpPacketData(8096, headerBufferSize);
        }

        protected override void SetSocket()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Blocking = false;
            socket.NoDelay = true;
            socket.ReceiveTimeout = 30000;
            socket.SendTimeout = 30000;
            receivePacketQueue = new Queue<T>();
        }

        protected override async UniTask Receive(CancellationToken token)
        {
            tcpPacketData.socketError = SocketError.Success;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    tcpPacketData.receiveLength = await socket.ReceiveAsync(tcpPacketData.receiveBuffer, SocketFlags.None);
                }
                catch (Exception e)
                {
                    if (IsConnected)
                    {
                        Disconnect(SocketError.NotConnected);
                    }
                    return;
                }

                if (tcpPacketData.receiveLength == 0)
                {
                    Disconnect(SocketError.NotConnected);
                    return;
                }

                if (CheckExceptionSocketError(tcpPacketData.socketError))
                {
                    Disconnect(tcpPacketData.socketError);
                    return;
                }

                if (tcpPacketData.receiveLength > 0)
                {
                    tcpPacketData.currentIndex = 0;
                    while (tcpPacketData.currentIndex < tcpPacketData.receiveLength)
                    {
                        TcpFilterModules.Filter(receiveFilter, tcpPacketData);

                        if (tcpPacketData.headerIndex == 0 && tcpPacketData.currentPacketLength == tcpPacketData.totalPacketLength)
                        {
                            T packetData = serializer.Deserialize(tcpPacketData.packet);
                            if (packetData != null)
                            {
                                receivePacketQueue.Enqueue(packetData);
                            }
                        }
                    }
                }

                await UniTask.Delay(receiveDelayMilliSeconds);
            }
        }

        public void Send(T packet)
        {
            byte[] bytes = serializer.Serialize(packet);

            base.Send(bytes);
        }

        public int CheckData()
        {
            return receivePacketQueue.Count;
        }

        public T GetData()
        {
            return receivePacketQueue.Dequeue();
        }

        public override void Disconnect(SocketError socketError = SocketError.Success)
        {
            if (cancelTokenSource.IsCancellationRequested)
            {
                return;
            }

            Disconnected?.Invoke(this, new DisconnectEventArgs() 
            { 
                socketError = socketError
            });

            base.Disconnect();
        }
    }
}
