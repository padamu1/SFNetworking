## 🌐 SFNetworking

Socket Networking package for Unity6

---

## 📌 How to Import

Window - Pacakge Manager - Install Package from git URL - "https://github.com/padamu1/SFNetworking"

---

## 📌 How to Use

### Make Custom Packet class (sample)

```csharp
public struct PacketData
{
    public byte EvCode { get; set; }
    public Dictionary<byte, object> Data { get; set; }


    public PacketData(byte evCode, Dictionary<byte, object> data)
    {
        EvCode = evCode;
        Data = data;
    }
}
```


### Receive Filter (sample)

```csharp
public class ReceiveFilter : IReceiveFilter
{
    public const int PACKET_HEADER_SIZE = 4;
    private byte[] lengthBytes = new byte[4];

    public void HeaderFilter(byte[] headerBuffer, out int totalPacketLength)
    {
        Array.Copy(headerBuffer, 0, lengthBytes, 0, lengthBytes.Length);
        totalPacketLength = BitConverter.ToInt32(lengthBytes);
    }

    public void CheckUnknownPacket(byte[] packet, out SocketError socketError)
    {
        if (packet.Length == 0)
        {
            socketError = SocketError.NoData;
            return;
        }

        socketError = SocketError.Success;
    }
}

```

### Serailzier (sample)

```csharp
public class Serializer : ISerializer<PacketData>
{
  PacketData ISerializer<PacketData>.Deserialize(byte[] value)
  {
    return (PacketData)Deserialize(Decompress(value));
  }
  PacketData ISerializer<PacketData>.Deserialize(byte[] value)
  {
    return (PacketData)Deserialize(Decompress(value));
  }
  ...
}
```

### Connect to server

```csharp
SFTcpClient<PacketData> sfClient = new SFTcpClient<PacketData>(ReceiveFilter.PACKET_HEADER_SIZE, 1, new ReceiveFilter(), new Serializer());
sfClient.SetReciveTimeOut(300000);

sfClient.Conneted -= OnConnected;
sfClient.Conneted += OnConnected;
sfClient.Disconnected -= OnDisconnected;
sfClient.Disconnected += OnDisconnected;
sfClient.Connect(gameServerUri, port, 10);
```

---

## 🖥️ Supported Operating Systems  

✅ **Android**  
✅ **Window**  
✅ **IOS**    
✅ **Mac**  

## 🎯 Only support unity 6

|Client|Protocol|Version|Minimum Unity Version|
|------|----------------|---|---|
|SFTcpClient|Tcp|1.0.0|6000.0.26f1|
|SFTcpClient|Http|1.0.0|6000.0.26f1|

### Support unity 5 and 6 ( ~ version 0.0.3) - Legacy

|Client|Protocol|Version|MinimumUnity Version|
|------|----------------|---|---|
|SFTcpClient|Tcp (UniTask)|0.0.3|2022.3.8f1|
|SFTcpClient|Tcp (Task)|0.0.3|2022.3.8f1|
|SFTcpClient|Http (UniTask)|0.0.3|2022.3.8f1|
|SFTcpClient|Http (Task)|0.0.2|2022.3.8f1|

---

## 🐟 License  
This project is licensed under the [MIT License](LICENSE). Feel free to use and modify! 😊  


