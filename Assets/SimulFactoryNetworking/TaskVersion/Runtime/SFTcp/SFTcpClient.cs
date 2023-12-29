using SimulFactoryNetworking.TaskVersion.Runtime.Common;
using SimulFactoryNetworking.TaskVersion.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace SimulFactoryNetworking.TaskVersion.Runtime.SFTcp
{
    public class SFTcpClient<T> : SFClient
    {
        private ISerializer<T> serializer;
        private Queue<T> receivePacketQueue;
        private byte[] receiveBuffer = new byte[2147483647];

        public SFTcpClient(ISerializer<T> serializer) : base()
        {
            this.serializer = serializer;
            socket.ReceiveBufferSize = 2147483647;
            receivePacketQueue = new Queue<T>();
        }

        protected override void SetSocket()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async Task Receive()
        {
            while(socket.Connected)
            {
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
                                object packetData = serializer.Deserialize(incommingData, out incommingData);
                                if (packetData != null && packetData.GetType() == typeof(T))
                                {
                                    receivePacketQueue.Enqueue((T)packetData);
                                    Debug.LogFormat("packet enqueue");
                                }
                                else
                                {
                                    Debug.LogFormat("packet not enqueue");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                await Task.Delay(10);
            }
        }

        public void Send(T packet)
        {
            byte[] bytes = serializer.Serialize(packet);

            Task.Run(async () => await base.Send(bytes));
        }

        public void Send(byte[] bytes)
        {
            Task.Run(async () => await base.Send(bytes));
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
