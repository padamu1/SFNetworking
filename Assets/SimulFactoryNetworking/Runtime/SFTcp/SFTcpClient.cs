﻿using SimulFactoryNetworking.Unity6.Runtime.Common;
using SimulFactoryNetworking.Unity6.Runtime.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace SimulFactoryNetworking.Unity6.Runtime.SFTcp
{
    public class SFTcpClient<T> : SFClient
    {
        public event EventHandler<DisconnectEventArgs> Disconnected;

        private ISerializer<T> serializer;
        private ConcurrentQueue<T> receivePacketQueue;
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
            socket.ReceiveTimeout = 30000;
            socket.SendTimeout = 30000;
            socket.ReceiveBufferSize = 8096;
            socket.NoDelay = true;
            receivePacketQueue = new ConcurrentQueue<T>();

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
            if (IsConnected == false)
            {
                Disconnect(SocketError.NotConnected);
                return;
            }

            if (cancellationTokenSource.IsCancellationRequested) return;

            if (socket.ReceiveAsync(receiveArgs) == false)
            {
                SocketReceiveEvent(this, receiveArgs);
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

            if (args.SocketError != SocketError.Success && args.SocketError != SocketError.WouldBlock)
            {
                Receive();
                return;
            }

            if (tcpPacketData.receiveLength == 0)
            {
                Disconnect(SocketError.NoData);
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

            Receive();
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

        public bool GetData(out T data)
        {
            if (receivePacketQueue.TryDequeue(out T peek))
            {
                data = peek;
                return true;
            }
            data = default;
            return false;
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
