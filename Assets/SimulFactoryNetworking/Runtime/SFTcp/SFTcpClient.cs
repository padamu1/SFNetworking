using SimulFactoryNetworking.Unity6.Runtime.Common;
using SimulFactoryNetworking.Unity6.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace SimulFactoryNetworking.Unity6.Runtime.SFTcp
{
    public class SFTcpClient<T> : SFClient
    {
        public event EventHandler<DisconnectEventArgs> Disconnected;

        private ISerializer<T> serializer;
        private Queue<T> receivePacketQueue;
        private IReceiveFilter receiveFilter;
        private TcpPacketData tcpPacketData;
        private int receiveDelayMilliSeconds;
        private SocketAsyncEventArgs receiveArgs;

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

            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += SocketReceiveEvent;
        }

        protected override Awaitable RunReceiveBackGround()
        {
            receiveArgs.SetBuffer(tcpPacketData.receiveBuffer);
            return base.RunReceiveBackGround();
        }

        protected override void Receive()
        {
            if (cancellationTokenSource.IsCancellationRequested) return;

            if (socket.ReceiveAsync(receiveArgs) == false)
            {
                SocketReceiveEvent(this, receiveArgs);
                return;
            }
        }

        protected override void SocketReceiveEvent(object sender, SocketAsyncEventArgs args)
        {
            tcpPacketData.receiveLength = args.BytesTransferred;

            if (CheckExceptionSocketError(args.SocketError))
            {
                Disconnect(args.SocketError);
                return;
            }

            if (tcpPacketData.receiveLength == 0)
            {
                Disconnect(SocketError.NotConnected);
                return;
            }

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

            if (IsConnected)
            {
                Receive();
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
            if (cancellationTokenSource == null || cancellationTokenSource.Token.IsCancellationRequested)
            {
                return;
            }

            base.Disconnect();

            _ = DisconnectEvent(socketError);
        }

        private async Awaitable DisconnectEvent(SocketError socketError)
        {
            await Awaitable.MainThreadAsync();

            Disconnected?.Invoke(this, new DisconnectEventArgs()
            {
                socketError = socketError
            });
        }
    }
}
