using SimulFactoryNetworking.Unity6.Runtime.Common;
using SimulFactoryNetworking.Unity6.Runtime.Core;
using SimulFactoryNetworking.Unity6.Runtime.SFHttp.Data;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SimulFactoryNetworking.Unity6.Runtime.SFHttp
{
    public class SFHttpClient<T> : SFClient
    {
        private Func<SFHttpResponse<T>, Awaitable> callback;
        private SFHttpRequest request;
        private SocketAsyncEventArgs receiveArgs;
        private byte[] buffer;
        private SFHttpResponse<T> httpResponse;
        private Func<float, Awaitable> progress;
        private SFHttpClient(SFHttpRequest request, Func<SFHttpResponse<T>, Awaitable> callback, Func<float, Awaitable> progress) : base()
        {
            this.request = request;
            this.callback = callback;
            this.progress = progress;
        }

        public static void Send(SFHttpRequest request, Func<SFHttpResponse<T>, Awaitable> callback, Func<float, Awaitable> progress)
        {
            SFHttpClient<T> httpClient = new SFHttpClient<T>(request, callback, progress);
            httpClient.SetConnectTimeOut(request.GetTimeOut());
            httpClient.Connect(request.GetHost(), request.GetPort());
        }

        protected override void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                cancellationTokenSource = new CancellationTokenSource();
                base.Send(request.GetHttpRequest());
                buffer = new byte[socket.ReceiveBufferSize];
                receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.SetBuffer(buffer);
                receiveArgs.Completed += SocketReceiveEvent;
                base.OnConnected(sender, connectEventArgs);
            }
        }

        protected override void SetSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override void Receive()
        {
            // Header Data
            socket.ReceiveAsync(receiveArgs);
        }

        protected override void SocketReceiveEvent(object sender, SocketAsyncEventArgs args)
        {
            int receiveBytes = args.BytesTransferred;
            if (httpResponse == null)
            {
                string result = Encoding.ASCII.GetString(buffer, 0, receiveBytes);
                httpResponse = new SFHttpResponse<T>(result);

                if (httpResponse.GetContentLength() > 0)
                {
                    Receive();
                    return;
                }
            }
            else
            {

                if (receiveBytes > 0)
                {
                    httpResponse.AddBody(Encoding.ASCII.GetString(buffer, 0, receiveBytes));
                }

                if (progress != null)
                {
                    _ = progress((float)httpResponse.GetBodyLength() / httpResponse.GetContentLength());
                }

                if (httpResponse.GetBodyLength() < httpResponse.GetContentLength())
                {
                    Receive();
                    return;
                }
            }

            if (httpResponse.GetStatusCode() == 200 && httpResponse.TryGetHeader("Content-Type", out string contentType) && contentType == HttpContentType.ApplicationJson)
            {
                httpResponse.ConvertToJson();
            }

            socket.Close();
            Dispose();

            _ = callback(httpResponse);
        }
    }
}
