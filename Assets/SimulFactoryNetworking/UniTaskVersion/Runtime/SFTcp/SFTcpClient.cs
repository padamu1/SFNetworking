using Cysharp.Threading.Tasks;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Common;
using SimulFactoryNetworking.UniTaskVersion.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

namespace SimulFactoryNetworking.UniTaskVersion.Runtime.SFTcp
{
    public class SFTcpClient<T> : SFClient
    {
        private ISerializer<T> serializer;
        private Queue<T> receivePacketQueue;
        private byte[] receiveBuffer = new byte[2147483647];
        private IReceiveFilter receiveFilter;
        public SFTcpClient(IReceiveFilter receiveFilter, ISerializer<T> serializer) : base()
        {
            this.receiveFilter = receiveFilter;
            this.serializer = serializer;
            socket.ReceiveBufferSize = 2147483647;
            receivePacketQueue = new Queue<T>();
        }

        protected override void SetSocket()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async UniTask Receive(CancellationToken token)
        {
            while (socket.Connected)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (socket.Available != 0)
                {
                    try
                    {
                        int length;

                        if ((length = socket.Receive(receiveBuffer)) > 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(receiveBuffer, 0, incommingData, 0, length);

                            while (incommingData.Length > 0)
                            {
                                byte[] packetBytes = receiveFilter.Filter(incommingData, out incommingData);

                                if (packetBytes != null)
                                {
                                    object packetData = serializer.Deserialize(packetBytes);
                                    if (packetData != null && packetData.GetType() == typeof(T))
                                    {
                                        receivePacketQueue.Enqueue((T)packetData);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                await UniTask.NextFrame();
            }
        }

        public void Send(T packet)
        {
            byte[] bytes = serializer.Serialize(packet);

            UniTask.Create(() => base.Send(bytes));
        }

        public void Send(byte[] bytes)
        {
            UniTask.Create(() => base.Send(bytes));
        }

        public int CheckData()
        {
            return receivePacketQueue.Count;
        }

        public T GetData()
        {
            return receivePacketQueue.Dequeue();
        }
    }
}
