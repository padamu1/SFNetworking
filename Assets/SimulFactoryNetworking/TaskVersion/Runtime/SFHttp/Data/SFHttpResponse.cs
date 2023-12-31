using System.Collections.Generic;

namespace SimulFactoryNetworking.TaskVersion.Runtime.SFHttp.Data
{
    public class SFHttpResponse
    {
        private int statusCode;
        private string body;
        private Dictionary<string, string> headerDic;

        public SFHttpResponse(string response)
        {
            string[] dataArray = response.Split("\r\n\r\n");

            string[] headers = dataArray[0].Split("\r\n");

            string[] result = headers[0].Split(' ');

            statusCode = int.Parse(result[1]);

            headerDic = new Dictionary<string, string>();

            for(int index = 1;  index < headers.Length; index++)
            {
                string[] headerInfo = headers[index].Split(":");

                headerDic.Add(headerInfo[0], headerInfo[1].Trim());
            }

            body = response.Remove(0, dataArray[0].Length);
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

        public string GetBody()
        {
            return this.body;
        }
    }
}
