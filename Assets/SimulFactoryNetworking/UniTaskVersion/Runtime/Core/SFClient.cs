using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Core
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
        protected CancellationTokenSource cancelTokenSource;

        public bool IsConnected => socket.Connected;

        public SFClient()
        {
            cancelTokenSource = new CancellationTokenSource();
            SetSocket();
            socket.ReceiveTimeout = 1000;
            socket.SendTimeout = 1000;
            Conneted += OnConnected;
        }

        protected abstract void SetSocket();

        protected virtual void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if(connectEventArgs.isConnected)
            {
                UniTask.Create(() => Receive(cancelTokenSource.Token));
            }
        }
        public void Connect(string uri, int port)
        {
            UniTask t = UniTask.Create(() => ToConnect(uri, port));
        }
        private async UniTask ToConnect(string uri, int port)
        {
            float connectStartTime = Time.realtimeSinceStartup;
            while(socket.Connected == false)
            {
                try
                {
                    await socket.ConnectAsync(uri, port);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if(Time.realtimeSinceStartup - connectStartTime > connectTimeout)
                {
                    break;
                }
            }

            var handle = Conneted;
            if (handle != null)
            {
                handle.Invoke(this, new ConnectEventArgs() 
                { 
                    isConnected = socket.Connected 
                });
            }
        }

        public void Disconnect()
        {
            Dispose();
            socket.Close();
        }

        public virtual void Dispose() => cancelTokenSource.Cancel();

        protected async UniTask Send(byte[] bytes) 
        {
            socket.Send(bytes);
        }

        protected abstract UniTask Receive(CancellationToken cancellationToken);

        public void SetConnectTimeOut(int miliseconds) => connectTimeout = miliseconds;

        public void SetReciveTimeOut(int miliseconds) => receiveTimeOut = miliseconds;

        public void SetSendTimeOut(int miliseconds) => sendTimeOut = miliseconds;
    }
}