using SimulFactoryNetworking.Unity6.Runtime.Common;
using SimulFactoryNetworking.Unity6.Runtime.Core;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
            tcpPacketData = new TcpPacketData(8192 * 2, headerBufferSize);
        }

        /// <summary>
        /// Set Socket Option for tcp socket networking
        /// </summary>
        protected override void SetSocket()
        {
            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = true;

            socket.Blocking = false;
            socket.ReceiveTimeout = 300000;
            socket.SendTimeout = 120000;

            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, 255);
            }
            catch (SocketException ex)
            {
                Debug.LogError($"Warning: TTL setting failed - {ex.Message}");
            }

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 24 * 1024);

            try
            {
                LingerOption lingerOption = new LingerOption(false, 0);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);
            }
            catch (SocketException ex)
            {
                Debug.LogError($"Warning: LingerOption setting failed - {ex.Message}");
            }

            byte[] keepAlive = new byte[12];
            BitConverter.GetBytes(1).CopyTo(keepAlive, 0);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                BitConverter.GetBytes(60000).CopyTo(keepAlive, 4);
                BitConverter.GetBytes(5000).CopyTo(keepAlive, 8);
            }
            else
            {
                BitConverter.GetBytes(60).CopyTo(keepAlive, 4);
                BitConverter.GetBytes(5).CopyTo(keepAlive, 8);
            }

            socket.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            receivePacketQueue = new ConcurrentQueue<T>();

            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.DisconnectReuseSocket = false;
            receiveArgs.Completed += SocketReceiveEvent;
        }

        /// <summary>
        /// Set Socket keep alive option
        /// </summary>
        protected override void RunReceiveBackGround()
        {
            // set buffer
            receiveArgs.SetBuffer(tcpPacketData.receiveBuffer);
            base.RunReceiveBackGround();
        }

        protected override async Awaitable Receive()
        {
            // receive data on backgroudnthread
            await Awaitable.BackgroundThreadAsync();

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

            if (args.SocketError != SocketError.WouldBlock && args.SocketError != SocketError.Success)
            {
                _ = Receive();
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

            _ = Receive();
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
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            base.Disconnect();

            _ = DisconnectEvent(socketError);
        }

        public override void Dispose()
        {
            if (receiveArgs != null)
            {
                receiveArgs.Dispose();
            }

            base.Dispose();
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
