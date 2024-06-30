using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimulFactoryNetworking.TaskVersion.Runtime.Core
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

        public bool IsConnected => socket.Connected;

        public SFClient()
        {
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
                Task.Run(async () => await Receive(cancellationTokenSource.Token));
            }
        }
        public void Connect(string uri, int port)
        {
            Task.Run(async() => await ToConnect(uri, port));
        }
        private async Task ToConnect(string uri, int port)
        {
            DateTime connectStartTime = DateTime.Now;
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

                if(socket.Connected == false)
                {
                    socket.Close();
                    SetSocket();
                }

                if ((DateTime.Now - connectStartTime).TotalMilliseconds > connectTimeout)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            if(IsConnected)
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
            if(cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            cancellationTokenSource.Cancel();
        }

        protected async Task Send(byte[] bytes) 
        {
            socket.Send(bytes);
        }

        protected abstract Task Receive(CancellationToken cancelToken);

        public void SetConnectTimeOut(int miliseconds) => connectTimeout = miliseconds;

        public void SetReciveTimeOut(int miliseconds) => receiveTimeOut = miliseconds;

        public void SetSendTimeOut(int miliseconds) => sendTimeOut = miliseconds;
    }
}