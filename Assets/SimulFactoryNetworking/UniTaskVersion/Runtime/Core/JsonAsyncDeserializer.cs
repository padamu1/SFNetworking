using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Core
{
    public class JsonAsyncDeserializer
    {
        // StringBuilder의 내용을 사용하여 비동기적으로 역직렬화를 수행하는 메서드
        public static async UniTask<T> DeserializeFromStringBuilderAsync<T>(StringBuilder body)
        {
            // StringBuilder의 내용을 string으로 변환
            string jsonString = body.ToString();

            // string을 바이트 배열로 변환
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);

            // 바이트 배열을 사용하여 MemoryStream 생성
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                return await DeserializeFromStreamAsync<T>(stream);
            }
        }

        // 스트림을 사용하여 비동기적으로 역직렬화를 수행하는 메서드
        private static async UniTask<T> DeserializeFromStreamAsync<T>(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = new JsonSerializer();
                // 스트림으로부터 비동기적으로 데이터 역직렬화
                return await Task.Run(() => serializer.Deserialize<T>(jsonReader)).ConfigureAwait(false);
            }
        }
    }
}
