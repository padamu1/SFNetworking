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

        public SFTcpClient(int headerBufferSize, IReceiveFilter receiveFilter, ISerializer<T> serializer) : base()
        {
            this.receiveFilter = receiveFilter;
            this.serializer = serializer;
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

            tcpPacketData.lastDataCheckedTime = DateTime.Now;

            ReceiveData(token).Forget();
        }

        protected async UniTask ReceiveData(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            tcpPacketData.receiveLength = socket.Receive(tcpPacketData.receiveBuffer, 0, tcpPacketData.receiveBuffer.Length, SocketFlags.None, out tcpPacketData.socketError);

            // Success => 수신에 이상 없음
            // WouldBlock => 수신에 이상은 없으나, 뒤에 추가 데이터가 남아있음
            if (tcpPacketData.socketError == SocketError.Success || tcpPacketData.socketError == SocketError.WouldBlock)
            {
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
                else
                {
                    if (tcpPacketData.headerIndex != 0)
                    {
                        receiveFilter.CheckUnknownPacket(tcpPacketData.headerBuffer, out tcpPacketData.socketError);
                        Disconnect(tcpPacketData.socketError);
                        return;
                    }
                }
            }
            else
            {
                Disconnect(tcpPacketData.socketError);
                return;
            }

            await UniTask.NextFrame();
            ReceiveData(token).Forget();
        }

        public void Send(T packet)
        {
            byte[] bytes = serializer.Serialize(packet);

            UniTask.Create(() => base.Send(bytes));
        }

        public void Send(byte[] bytes)
        {
            UniTask.Create(() => base.Send(bytes));
        }

        public int CheckData()
        {
            if ((DateTime.Now - tcpPacketData.lastDataCheckedTime).TotalMilliseconds > socket.ReceiveTimeout)
            {
                tcpPacketData.socketError = SocketError.TimedOut;
                Disconnect(tcpPacketData.socketError);
                return 0;
            }
            tcpPacketData.lastDataCheckedTime = DateTime.Now;
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
