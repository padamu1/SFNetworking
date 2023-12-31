using Cysharp.Threading.Tasks;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Core;
using SimulFactoryNetworking.UniTaskVersion.Runtime.SFHttp.Data;
using System;
using System.Net.Sockets;
using System.Text;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFHttp
{
    public class SFHttpClient<T> : SFClient
    {
        private Action<SFHttpResponse<T>> callback;
        private SFHttpRequest request;

        private SFHttpClient(SFHttpRequest request, Action<SFHttpResponse<T>> callback) : base()
        {
            this.request = request;
            this.callback = callback;
        }

        public static void Send(SFHttpRequest request, Action<SFHttpResponse<T>> callback)
        {
            SFHttpClient<T> httpClient = new SFHttpClient<T>(request, callback);
            httpClient.SetConnectTimeOut(request.GetTimeOut());
            httpClient.Connect(request.GetHost(), request.GetPort());
        }

        protected override void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                UniTask t = UniTask.Create(
                    async () => 
                    {
                        await base.Send(request.GetHttpRequest());
                        await Receive();
                    });
            }
        }

        protected override void SetSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async UniTask Receive()
        {
            byte[] recvBuff = new byte[socket.ReceiveBufferSize];

            // Header Data
            int nCount = socket.Receive(recvBuff);

            string result = Encoding.ASCII.GetString(recvBuff, 0, nCount);
            SFHttpResponse<T> httpResponse = ParseData(result);

            if (httpResponse.TryGetHeader("Content-Length", out string value))
            {
                int length = int.Parse(value);

                // Check Body Data
                while (length > httpResponse.GetBody().Length)
                {
                    int count = socket.Receive(recvBuff);

                    string temp = Encoding.ASCII.GetString(recvBuff, 0, count);

                    httpResponse.AddBody(temp);
                }
            }

            if (httpResponse.TryGetHeader("Content-Type", out string contentType) && contentType == HttpContentType.ApplicationJson)
            {
                httpResponse.ConvertToJson();
            }

            callback(httpResponse);

            socket.Close();
        }

        protected virtual SFHttpResponse<T> ParseData(string result)
        {
            SFHttpResponse<T> response = new SFHttpResponse<T>(result);
            return response;
        }
    }
}
