namespace SimulFactoryNetworking.UniTaskVersion.Runtime.Common
{
    public interface IReceiveFilter
    {
        public void HeaderFilter(byte[] receiveBuffer, int currentIndex, out int nextIndex, out int totalPacketLength);
    }
}
