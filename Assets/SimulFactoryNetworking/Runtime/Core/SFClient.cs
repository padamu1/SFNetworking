using Codice.Client.Common;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
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
        public event EventHandler<ConnectEventArgs> Conneted;
        protected CancellationTokenSource cancellationTokenSource;

        private SocketAsyncEventArgs sendAsyncArgs;
        private ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
        private byte[] sendBuffer;
        private int bytesSent;

        private int sendDelay;

        public bool IsConnected => socket == null || socket.Connected;

        private int isSent;

        public SFClient()
        {
            SetSocket();
            Conneted -= OnConnected;
            Conneted += OnConnected;

            connectTimeout = 60000;
            receiveTimeOut = 60000;
            sendTimeOut = 60000;

            cancellationTokenSource = new CancellationTokenSource();

            sendAsyncArgs = new SocketAsyncEventArgs();
            sendAsyncArgs.DisconnectReuseSocket = false;
            sendAsyncArgs.Completed += OnSend;
        }

        protected abstract void SetSocket();

        protected virtual void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                RunReceiveBackGround();
            }
        }

        public void Connect(string uri, int port, int sendDelay)
        {
            this.sendDelay = sendDelay;
            _ = ToConnect(uri, port);
        }

        private async Awaitable ToConnect(string uri, int port)
        {
            // Switch to background thread
            await Awaitable.BackgroundThreadAsync();

            IPAddress ipAddress;
            if (!IPAddress.TryParse(uri, out ipAddress))
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(uri);
                ipAddress = addresses[0];
            }
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, port);

            DateTime connectStartTime = DateTime.Now;
            while (socket.Connected == false && cancellationTokenSource.IsCancellationRequested == false)
            {
                try
                {
                    await socket.ConnectAsync(iPEndPoint);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                if (socket.Connected == false)
                {
                    socket.Close();

                    if ((DateTime.Now - connectStartTime).TotalMilliseconds > connectTimeout)
                    {
                        break;
                    }


                    SetSocket();
                }
                else
                {
                    break;
                }

                await Awaitable.WaitForSecondsAsync(100);
            }

            // Switch to main thread
            await Awaitable.MainThreadAsync();

            if (IsConnected)
            {
                Conneted?.Invoke(this, new ConnectEventArgs()
                {
                    isConnected = socket.Connected
                });
            }
            else
            {
                Disconnect(SocketError.TimedOut);
            }
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

            if (IsConnected == false || cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            bytesSent = 0;

            if (sendQueue.TryDequeue(out sendBuffer))
            {
                TrySend();
                return;
            }

            Interlocked.Exchange(ref isSent, 0);
        }

        /// <summary>
        /// try send before send all data
        /// </summary>
        private void TrySend()
        {
            int remaining = sendBuffer.Length - bytesSent;
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
            if (CheckExceptionSocketError(e.SocketError))
            {
                Disconnect();
                return;
            }

            if (e.BytesTransferred > 0)
            {
                bytesSent += e.BytesTransferred;

                if (bytesSent < sendBuffer.Length)
                {
                    TrySend();
                }
                else
                {
                    _ = SendProcess();
                }
            }
            else
            {
                TrySend();
            }
        }

        /// <summary>
        /// Start Receive Method
        /// </summary>
        protected virtual void RunReceiveBackGround()
        {
            if (IsConnected)
            {
                _ = Receive();
            }
        }

        protected abstract void SocketReceiveEvent(object sender, SocketAsyncEventArgs args);

        /// <summary>
        /// Receive data with awaitable method
        /// </summary>
        /// <returns></returns>
        protected abstract Awaitable Receive();

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