using Cysharp.Threading.Tasks;
using SimulFactoryNetworking.Runtime.Core;
using SimulFactoryNetworking.Runtime.SFHttp.Data;
using System;
using System.Net.Sockets;
using System.Text;

namespace SimulFactoryNetworking.Runtime.SFHttp
{
    public class SFHttpClient : SFClient
    {
        private Action<SFHttpResponse> callback;
        private SFHttpRequest request;

        private SFHttpClient(SFHttpRequest request, Action<SFHttpResponse> callback) : base()
        {
            this.request = request;
            this.callback = callback;
        }

        public static void Send(SFHttpRequest request, Action<SFHttpResponse> callback)
        {
            SFHttpClient httpClient = new SFHttpClient(request, callback);
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
            int nCount = socket.Receive(recvBuff);

            string result = Encoding.ASCII.GetString(recvBuff, 0, nCount);

            callback.Invoke(ParseData(result));            

            socket.Close();
        }

        protected virtual SFHttpResponse ParseData(string result)
        {
            SFHttpResponse response = new SFHttpResponse(result);
            return response;
        }
    }
}
