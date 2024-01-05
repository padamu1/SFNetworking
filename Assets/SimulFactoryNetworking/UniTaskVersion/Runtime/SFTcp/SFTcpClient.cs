﻿using Cysharp.Threading.Tasks;
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

        public SFTcpClient(IReceiveFilter receiveFilter, ISerializer<T> serializer) : base()
        {
            this.receiveFilter = receiveFilter;
            this.serializer = serializer;
        }

        protected override void SetSocket()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Blocking = false;
            socket.NoDelay = true;
            socket.ReceiveTimeout = 30000;
            socket.SendTimeout = 30000;
            receivePacketQueue = new Queue<T>();
            tcpPacketData = new TcpPacketData(8096);
        }

        protected override async UniTask Receive(CancellationToken token)
        {
            while (socket.Connected)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                tcpPacketData.receiveLength = socket.Receive(tcpPacketData.receiveBuffer, 0, tcpPacketData.bufferSize, SocketFlags.None, out tcpPacketData.socketError);
                if (tcpPacketData.socketError == SocketError.Success || tcpPacketData.socketError == SocketError.WouldBlock)
                {
                    if (tcpPacketData.receiveLength > 0)
                    {
                        tcpPacketData.currentIndex = 0;
                        while (tcpPacketData.receiveLength > tcpPacketData.currentIndex)
                        {
                            receiveFilter.Filter(tcpPacketData);

                            if (tcpPacketData.currentPacketLength == tcpPacketData.totalPacketLength)
                            {
                                T packetData = serializer.Deserialize(tcpPacketData.packet);
                                if (packetData != null)
                                {
                                    receivePacketQueue.Enqueue(packetData);
                                }
                            }
                        }
                    }
                }
                else
                {
                    break;
                }

                await UniTask.NextFrame();
            }

            Disconnect(tcpPacketData.socketError);
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
            return receivePacketQueue.Count;
        }

        public T GetData()
        {
            return receivePacketQueue.Dequeue();
        }

        public override void Disconnect(SocketError socketError = SocketError.Success)
        {
            Disconnected?.Invoke(this, new DisconnectEventArgs() 
            { 
                socketError = socketError
            });
            base.Disconnect();
        }
    }
}
