using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulFactoryNetworking.Runtime.Common
{
    public interface ISerializer<T>
    {
        public object Deserialize(byte[] bytes, out byte[] nextData);
        public byte[] Serialize(T t);
    }
}
