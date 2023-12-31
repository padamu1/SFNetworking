using System;

namespace SimulFactoryNetworking.TaskVersion.Runtime.Common
{
    public enum HTTP_METHOD
    {
        GET = 0,
        POST = 1,
        PUT = 2,
        DELETE = 3,
    }

    public class HttpMethodString
    {
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";

        public static string GetHttpMethodString(HTTP_METHOD method)
        {
            switch (method)
            {
                case HTTP_METHOD.GET:
                    return GET;
                case HTTP_METHOD.POST: 
                    return POST;
                case HTTP_METHOD.PUT: 
                    return PUT;
                case HTTP_METHOD.DELETE: 
                    return DELETE;
                default:
                    throw new ArgumentException();
            }
        }
    }

    public class HttpContentType
    {
        public const string ApplicationJson = "application/json";
    }
}
