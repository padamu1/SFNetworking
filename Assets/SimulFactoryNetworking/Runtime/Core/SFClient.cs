using System;
using System.Collections.Concurrent;
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
        private ConcurrentQueue<ArraySegment<byte>> sendQueue = new ConcurrentQueue<ArraySegment<byte>>();
        private int sendTaskRun;

        public bool IsConnected => socket == null || socket.Connected;

        public SFClient()
        {
            SetSocket();
            socket.ReceiveTimeout = 1000;
            socket.SendTimeout = 1000;
            Conneted -= OnConnected;
            Conneted += OnConnected;

            sendAsyncArgs = new SocketAsyncEventArgs();
            sendAsyncArgs.Completed += OnSend;
        }

        protected abstract void SetSocket();

        protected virtual void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                cancellationTokenSource = new CancellationTokenSource();
                _ = RunReceiveBackGround();
            }
        }

        public void Connect(string uri, int port)
        {
            _ = ToConnect(uri, port);
        }

        private async Awaitable ToConnect(string uri, int port)
        {
            // Switch to background thread
            await Awaitable.BackgroundThreadAsync();

            DateTime connectStartTime = DateTime.Now;
            while (socket.Connected == false)
            {
                try
                {
                    await socket.ConnectAsync(uri, port);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                if (socket.Connected == false)
                {
                    socket.Close();
                    SetSocket();
                }
                else
                {
                    break;
                }
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
            socket.Close();
        }

        public virtual void Dispose()
        {
            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            cancellationTokenSource.Cancel();
        }

        public void Send(ArraySegment<byte> bytes)
        {
            if (IsConnected)
            {
                sendQueue.Enqueue(bytes);
            }

            if (Interlocked.Exchange(ref sendTaskRun, 1) == 1)
            {
                return;
            }

            Awaitable.BackgroundThreadAsync().OnCompleted(SendProcess);
        }

        private void SendProcess()
        {
            if (sendQueue.TryDequeue(out ArraySegment<byte> packet))
            {
                sendAsyncArgs.SetBuffer(packet);
                if (socket.SendAsync(sendAsyncArgs) == false)
                {
                    OnSend(this, sendAsyncArgs);
                }

                return;
            }

            Interlocked.Exchange(ref sendTaskRun, 0);
        }

        private void OnSend(object? sender, SocketAsyncEventArgs e)
        {
            if (CheckExceptionSocketError(e.SocketError))
            {
                Disconnect();
                return;
            }

            SendProcess();
        }

        protected virtual async Awaitable RunReceiveBackGround()
        {
            await Awaitable.BackgroundThreadAsync();

            if (IsConnected)
            {
                Receive();
            }
        }

        protected abstract void SocketReceiveEvent(object sender, SocketAsyncEventArgs args);

        protected abstract void Receive();

        public void SetConnectTimeOut(int miliseconds) => connectTimeout = miliseconds;

        public void SetReciveTimeOut(int miliseconds) => receiveTimeOut = miliseconds;

        public void SetSendTimeOut(int miliseconds) => sendTimeOut = miliseconds;

        protected bool CheckExceptionSocketError(SocketError socketError)
        {
            if (socketError == SocketError.OperationAborted || socketError == SocketError.ConnectionAborted || socketError == SocketError.ConnectionReset || socketError == SocketError.NotConnected || socketError == SocketError.ConnectionRefused)
            {
                return true;
            }

            return false;
        }
    }
}