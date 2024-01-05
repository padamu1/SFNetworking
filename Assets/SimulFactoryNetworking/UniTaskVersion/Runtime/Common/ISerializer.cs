using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Common
{
    public interface ISerializer<T>
    {
        public T Deserialize(byte[] bytes);
        public byte[] Serialize(T t);
    }
}
