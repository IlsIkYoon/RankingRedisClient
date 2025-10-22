

using System.Numerics;
using System.Runtime.InteropServices;

static class MessageHandler
{

    public const int IncompleteData = 10;
    public const int Success = 0;
    static public int HandleMessage(CSession mySession, CPacket packet)
    {
        //메세지 꺼내면서 헤더랑 값 확인 
        if (packet.GetSize() < Marshal.SizeOf<NetworkHeader>()) //네트워크 헤더만큼도 없으면
        {
            return IncompleteData;
        }
        //네트워크 헤더 추출
        NetworkHeader ntHeader;
        byte[] tempNtHeader = new byte[Marshal.SizeOf<NetworkHeader>()];
        packet.DequeueData(tempNtHeader, Marshal.SizeOf<NetworkHeader>());
        ntHeader = MemoryMarshal.Read<NetworkHeader>(tempNtHeader);

        if (packet.GetSize() < ntHeader.len)
        {
            packet.RestorePacket(tempNtHeader, Marshal.SizeOf<NetworkHeader>());
            return IncompleteData;
        }
        //컨텐츠 메세지 추출
        ContentsMessage ctMessage;
        byte[] tempCtMessage = new byte[Marshal.SizeOf<ContentsMessage>()];
        packet.DequeueData(tempCtMessage, Marshal.SizeOf<ContentsMessage>());
        ctMessage = MemoryMarshal.Read<ContentsMessage>(tempCtMessage);

        //컨텐츠 핸들러 함수 호출
        ContentsManager.OnMessage(mySession, ctMessage);


        return Success;
    }


}