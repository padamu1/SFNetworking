using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using System.Net;
using System.Text;
using UnityEditor.PackageManager.Requests;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFHttp.Data
{
    public class SFHttpRequest
    {
        private string host;
        private HTTP_METHOD method;
        private string contentType;
        private string content;
        private int port;
        private string path;
        private int timeOut;

        public SFHttpRequest(string host, string path, int port, HTTP_METHOD method , int timeOut)
        {
            this.host = host;
            this.path = path;
            this.port = port;
            this.method = method;
            contentType = string.Empty;
            content = string.Empty;
            this.timeOut = timeOut;
        }

        public string GetHost()
        {
            return host;
        }

        public string GetPath()
        {
            return path;
        }

        public void SetPath(string path)
        {
            this.path = path;
        }

        public int GetPort()
        {
            return port;
        }

        public string GetMethod()
        {
            return HttpMethodString.GetHttpMethodString(method);
        }

        public void SetMethod(HTTP_METHOD method)
        {
            this.method=method;
        }

        public int GetTimeOut()
        {
            return timeOut;
        }

        public SFHttpRequest SetContentType(string contentType)
        {
            this.contentType = contentType;
            return this;
        }

        public string GetContentType()
        {
            return contentType;
        }

        public SFHttpRequest SetContent(string content)
        {
            this.content = content;
            return this;
        }

        public string GetContent()
        {
            return content;
        }

        public byte[] GetHttpRequest()
        {
            string http = $"{GetMethod()} {GetPath()} HTTP/1.1\r\n" +
            $"Host: {GetHost()}\r\n" +
                            $"Content-Type: {GetContentType()}\r\n" +
                            $"Connection: keep-alive\r\n" +
                            $"Accept: */*\r\n" +
                            $"Accept-Encoding: gzip, deflate\r\n\r\n";
            return Encoding.UTF8.GetBytes(http);
        }
    }
}
