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
                buffer = new byte[socket.ReceiveBufferSize];
                receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.SetBuffer(buffer);
                receiveArgs.Completed += SocketReceiveEvent;

                base.OnConnected(sender, connectEventArgs);

                socket.Send(request.GetHttpRequest(), SocketFlags.None, out SocketError errorCode);

                Debug.Log($"socket Send status : {errorCode.ToString()}");
            }
        }

        protected override void SetSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override void Receive()
        {
            if (socket.ReceiveAsync(receiveArgs) == false)
            {
                SocketReceiveEvent(this, receiveArgs);
            }
        }

        protected override void SocketReceiveEvent(object sender, SocketAsyncEventArgs args)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (CheckExceptionSocketError(args.SocketError))
            {
                Disconnect(args.SocketError);
                return;
            }

            if (SocketError.Success != args.SocketError && SocketError.WouldBlock != args.SocketError)
            {
                Receive();
                return;
            }

            int receiveBytes = args.BytesTransferred;

            if (receiveBytes == 0)
            {
                Disconnect();
                return;
            }

            if (httpResponse == null)
            {
                string result = Encoding.UTF8.GetString(buffer, 0, receiveBytes);
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
                    httpResponse.AddBody(Encoding.UTF8.GetString(buffer, 0, receiveBytes));
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

            Disconnect();

            _ = callback(httpResponse);
        }
    }
}
