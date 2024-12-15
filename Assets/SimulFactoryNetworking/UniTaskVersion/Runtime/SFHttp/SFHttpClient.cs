using Cysharp.Threading.Tasks;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Core;
using SimulFactoryNetworking.UniTaskVersion.Runtime.SFHttp.Data;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFHttp
{
    public class SFHttpClient<T> : SFClient
    {
        private Func<SFHttpResponse<T>, UniTask> callback;
        private SFHttpRequest request;
        private Func<float, UniTask> progress;

        private SFHttpClient(SFHttpRequest request, Func<SFHttpResponse<T>, UniTask> callback, Func<float, UniTask> progress) : base()
        {
            this.request = request;
            this.callback = callback;
            this.progress = progress;
        }

        public static void Send(SFHttpRequest request, Func<SFHttpResponse<T>, UniTask> callback, Func<float, UniTask> progress)
        {
            SFHttpClient<T> httpClient = new SFHttpClient<T>(request, callback, progress);
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
                        base.Send(request.GetHttpRequest());
                        await Receive(cancelTokenSource.Token);
                    });
            }
        }

        protected override void SetSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 30000;
            socket.SendTimeout = 30000;
        }

        protected override async UniTask Receive(CancellationToken cancellationToken)
        {
            await UniTask.SwitchToThreadPool();
            byte[] recvBuff = new byte[1000000];

            // Header Data
            int nCount = socket.Receive(recvBuff, 0, recvBuff.Length, SocketFlags.None, out SocketError socketError);

            if (socketError == SocketError.TimedOut)
            {
                Receive(cancellationToken).Forget();
                return;
            }

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

                    if(progress != null)
                    {
                        UniTask.Create(async () =>
                        {
                            await UniTask.SwitchToMainThread();
                            await progress(httpResponse.GetBody().Length / (float)length);
                        }).Forget();
                    }
                    await UniTask.NextFrame();
                }
            }

            socket.Close();

            if (httpResponse.GetStatusCode() == 200 && httpResponse.TryGetHeader("Content-Type", out string contentType) && contentType == HttpContentType.ApplicationJson)
            {
                await httpResponse.ConvertToJson();
            }

            await UniTask.SwitchToMainThread();

            if(httpResponse.GetStatusCode() == 200)
            {
                await callback(httpResponse);
            }
        }

        protected virtual SFHttpResponse<T> ParseData(string result)
        {
            SFHttpResponse<T> response = new SFHttpResponse<T>(result);
            return response;
        }

        public override void Dispose()
        {
            // Do not need dispose
        }
    }
}
