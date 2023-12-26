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
            httpClient.SetConnectTimeOut(1000);
            httpClient.Connect(request.GetHost(), request.GetPort());
        }

        protected override async void OnConnected(object sender, ConnectEventArgs connectEventArgs)
        {
            if (connectEventArgs.isConnected)
            {
                // Receive 활성화
                UniTask t = UniTask.Create(
                    async () => 
                    {
                        await base.Send(ParseData());
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

            // 파일 저장
            string result = Encoding.ASCII.GetString(recvBuff, 0, nCount);

            socket.Close();
        }

        private byte[] ParseData()
        {
            string http = $"{request.GetMethod()} {request.GetPath()} HTTP/1.1\r\n" +
                            $"Host: {request.GetHost()}\r\n" +
                            $"Content-Type: {request.GetContentType()}\r\n" +
                            $"Connection: keep-alive\r\n" +
                            //$"Cache -Control: max-age=0\r\n" +
                            //$"User -Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36\r\n" +
                            $"Accept: */*\r\n" +
                            $"Accept-Encoding: gzip, deflate\r\n\r\n";// +
                            //$"Accept-Language: en-US,en;q=0.9\r\n\r\n";// +
                            //$"{request.GetContent()}";
            return Encoding.UTF8.GetBytes(http);
        }
    }
}
