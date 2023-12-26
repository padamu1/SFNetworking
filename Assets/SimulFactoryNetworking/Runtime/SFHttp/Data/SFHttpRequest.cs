using SimulFactoryNetworking.Runtime.Common;
using System.Net;

namespace SimulFactoryNetworking.Runtime.SFHttp.Data
{
    public class SFHttpRequest
    {
        private string host;
        private HTTP_METHOD method;
        private string contentType;
        private string content;
        private int port;
        private string path;

        public SFHttpRequest(string host, string path, int port, HTTP_METHOD method)
        {
            this.host = host;
            this.path = path;
            this.port = port;
            this.method = method;
            contentType = string.Empty;
            content = string.Empty;
        }

        public string GetHost()
        {
            return host;
        }

        public string GetPath()
        {
            return path;
        }

        public int GetPort()
        {
            return port;
        }

        public string GetMethod()
        {
            return HttpMethodString.GetHttpMethodString(method);
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
    }
}
