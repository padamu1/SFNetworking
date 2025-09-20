using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace SimulFactoryNetworking.Unity6.Runtime.Core
{
    public class ConnectEventArgs : EventArgs
    {
        public bool isConnected;
    }
    public abstract class SFClient : IDisposable
    {
        protected Socket socket;
        protected int connectTimeout;
        protected int receiveTimeOut;
        protected int sendTimeOut;
        public event EventHandler<ConnectEventArgs> Connected;
        protected CancellationTokenSource cancellationTokenSource;

        private SocketAsyncEventArgs sendAsyncArgs;
        private ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();

        private int totalSendBytes;
        private int bytesSent;

        private int sendDelay;

        private DateTime connectStartTime;

        public bool IsConnected => socket != null && socket.Connected;

        private int isSent;

        private byte[] sendBuffer;


        private string uri;
        private int port;
        protected IPEndPoint endPoint;

        public SFClient()
        {
            SetSocket();
            Connected -= OnConnected;
            Connected += OnConnected;

            connectTimeout = 60000;
            receiveTimeOut = 60000;
            sendTimeOut = 60000;

            cancellationTokenSource = new CancellationTokenSource();

            sendAsyncArgs = new SocketAsyncEventArgs();
            sendAsyncArgs.DisconnectReuseSocket = false;
            sendAsyncArgs.SocketFlags = SocketFlags.None;
            sendAsyncArgs.Completed += OnSend;
        }

        protected abstract void SetSocket();

        protected virtual void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                sendAsyncArgs.RemoteEndPoint = endPoint;
                sendAsyncArgs.UserToken = socket;
                sendBuffer = new byte[socket.SendBufferSize > 1024 * 16 ? 1024 * 16 : socket.SendBufferSize];

                Awaitable.BackgroundThreadAsync().OnCompleted(Receive);
            }
        }

        public void Connect(string uri, int port, int sendDelay)
        {
            this.sendDelay = sendDelay;

            this.uri = uri;
            this.port = port;

            connectStartTime = DateTime.Now;

            Awaitable.BackgroundThreadAsync().OnCompleted(() => _ = SetEndPoint(SetConnection));
        }

        private async Awaitable SetEndPoint(Action onComplete)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(uri);
            IPAddress ipAddress = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                                  ?? addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetworkV6);

            endPoint = new IPEndPoint(ipAddress, port);

            onComplete();
        }

        private void SetConnection()
        {
            SocketAsyncEventArgs connectionArgs = new SocketAsyncEventArgs();
            connectionArgs.RemoteEndPoint = endPoint;
            connectionArgs.UserToken = socket;
            connectionArgs.Completed += OnSocketConnected;

            bool pending = socket.ConnectAsync(connectionArgs);
            if (pending == false)
            {
                OnSocketConnected(this, connectionArgs);
            }
        }

        private void OnSocketConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                this.socket = e.UserToken as Socket;
                _ = EndConnection(true);
                return;
            }
            else
            {
                if ((DateTime.Now - connectStartTime).TotalMilliseconds > connectTimeout)
                {
                    _ = EndConnection(false);
                    return;
                }
                else
                {
                    SetSocket();
                    _ = SetEndPoint(SetConnection);
                }
            }
        }

        private async Awaitable EndConnection(bool connect)
        {
            await Awaitable.MainThreadAsync();

            Connected?.Invoke(this, new ConnectEventArgs()
            {
                isConnected = connect
            });

            Connected = null;
        }

        public virtual void Disconnect(SocketError socketError = SocketError.Success)
        {
            Dispose();
        }

        public void Send(byte[] bytes)
        {
            if (IsConnected)
            {
                sendQueue.Enqueue(bytes);
            }

            if (Interlocked.Exchange(ref isSent, 1) == 0)
            {
                _ = SendProcess();
            }
        }

        private async Awaitable SendProcess()
        {
            await Awaitable.BackgroundThreadAsync();

            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            sendAsyncArgs.BufferList = null;

            bytesSent = 0;
            totalSendBytes = 0;

            while (sendQueue.TryDequeue(out var sendBuffer))
            {
                if (totalSendBytes + sendBuffer.Length > this.sendBuffer.Length)
                {
                    break;
                }

                Buffer.BlockCopy(sendBuffer, 0, this.sendBuffer, totalSendBytes, sendBuffer.Length);

                totalSendBytes += sendBuffer.Length;
            }

            if (totalSendBytes > 0)
            {
                TrySend();
                return;
            }

            Interlocked.Exchange(ref isSent, 0);

            if (sendQueue.IsEmpty == false)
            {
                if (Interlocked.Exchange(ref isSent, 1) == 0)
                {
                    _ = SendProcess();
                }
            }
        }

        /// <summary>
        /// try send before send all data
        /// </summary>
        private void TrySend()
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            int remaining = totalSendBytes - bytesSent;
            if (remaining <= 0)
            {
                _ = SendProcess();
                return;
            }

            sendAsyncArgs.SetBuffer(sendBuffer, bytesSent, remaining);

            bool willRaiseEvent = socket.SendAsync(sendAsyncArgs);
            if (!willRaiseEvent)
            {
                OnSend(socket, sendAsyncArgs);
            }
        }

        /// <summary>
        /// On SendAsyncEventArgs complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSend(object? sender, SocketAsyncEventArgs e)
        {
            if (CheckKnownException(e.SocketError) == false)
            {
                Disconnect(e.SocketError);
                return;
            }

            if (e.SocketError == SocketError.Success)
            {
                bytesSent += e.BytesTransferred;
            }

            TrySend();
        }

        protected abstract void SocketReceiveEvent(object sender, SocketAsyncEventArgs args);

        /// <summary>
        /// Receive data method
        /// </summary>
        /// <returns></returns>
        protected abstract void Receive();

        /// <summary>
        /// Set connnect timeout <br />
        /// Will reconnect to server when connect failed before connectTimeout <br />
        /// default value is 60000
        /// </summary>
        /// <param name="miliseconds"></param>
        public void SetConnectTimeOut(int miliseconds) => connectTimeout = miliseconds;

        /// <summary>
        /// Set receive Timeout <br />
        /// default value is 60000
        /// </summary>
        public void SetReciveTimeOut(int miliseconds)
        {
            receiveTimeOut = miliseconds;
            socket.ReceiveTimeout = receiveTimeOut;
        }

        /// <summary>
        /// Set send Timeout <br />
        /// default value is 60000
        /// </summary>
        public void SetSendTimeOut(int miliseconds)
        {
            sendTimeOut = miliseconds;
            socket.SendTimeout = sendTimeOut;
        }

        /// <summary>
        /// Check socket error
        /// </summary>
        /// <param name="socketError"></param>
        /// <returns></returns>
        protected bool CheckExceptionSocketError(SocketError socketError)
        {
            if (socketError == SocketError.OperationAborted || socketError == SocketError.ConnectionAborted || socketError == SocketError.ConnectionReset || socketError == SocketError.NotConnected || socketError == SocketError.ConnectionRefused)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check socket error
        /// </summary>
        /// <param name="socketError"></param>
        /// <returns></returns>
        protected bool CheckKnownException(SocketError socketError)
        {
            if (socketError == SocketError.WouldBlock || socketError == SocketError.Success)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dispose socket
        /// </summary>
        public virtual void Dispose()
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (sendQueue != null)
            {
                sendQueue.Clear();
            }

            if (sendAsyncArgs != null)
            {
                sendAsyncArgs.Dispose();
            }

            cancellationTokenSource.Cancel();

            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}