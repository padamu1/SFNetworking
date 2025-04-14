using System.Collections.Generic;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;

namespace SimulFactoryNetworking.Unity6.Runtime.SFHttp.Data
{
    public class SFHttpResponse<T>
    {
        private int statusCode;
        private StringBuilder body;
        private Dictionary<string, string> headerDic;
        private T data;
        private int contentLength;
        private int bodyLength;

        public SFHttpResponse(string response)
        {
            string[] dataArray = response.Split("\r\n\r\n");

            string[] headers = dataArray[0].Split("\r\n");

            string[] result = headers[0].Split(' ');

            statusCode = int.Parse(result[1]);

            headerDic = new Dictionary<string, string>();

            for (int index = 1; index < headers.Length; index++)
            {
                string[] headerInfo = headers[index].Split(":");

                headerDic.Add(headerInfo[0], headerInfo[1].Trim());
            }

            body = new StringBuilder();
            body.Append(response.Remove(0, dataArray[0].Length + 4));

            if (TryGetHeader("Content-Length", out string value))
            {
                contentLength = int.Parse(value);
            }
            else 
            {
                contentLength = 0;
            }
        }

        public int GetStatusCode()
        {
            return statusCode;
        }

        public bool TryGetHeader(string key, out string value)
        {
            if (headerDic.ContainsKey(key))
            {
                value = headerDic[key];
                return true;
            }
            value = null;
            return false;
        }

        public void AddBody(string body)
        {
            this.body.Append(body);
            bodyLength += body.Length;
        }

        public string GetBody()
        {
            return body.ToString();
        }

        public int GetBodyLength()
        {
            return bodyLength;
        }

        public void ConvertToJson()
        {
            data = JsonConvert.DeserializeObject<T>(body.ToString());
        }

        public T GetJsonData()
        {
            return data;
        }

        public int GetContentLength()
        {
            return contentLength;
        }
    }
}
