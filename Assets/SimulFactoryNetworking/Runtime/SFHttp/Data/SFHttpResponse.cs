namespace SimulFactoryNetworking.Runtime.SFHttp.Data
{
    public class SFHttpResponse
    {
        private int statusCode;
        private string body;
        private string contentType;

        public SFHttpResponse()
        {
            body = string.Empty;
            contentType = string.Empty;
        }

        public void SetStatus(int statusCode)
        {
            this.statusCode = statusCode;
        }

        public int GetStatusCode()
        {
            return statusCode;
        }

        public void SetContentType(string contentType)
        {
            this.contentType = contentType;
        }

        public string GetContextType()
        {
            return this.contentType;
        }

        public void SetBody(string body)
        {
            this.body = body;
        }

        public string GetBody()
        {
            return this.body;
        }
    }
}
