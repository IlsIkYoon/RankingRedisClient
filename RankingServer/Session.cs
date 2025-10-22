

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml;

class CSession
{
    //ip
    //port
    public Socket socket;
    public object sessionLock = new object();

    public bool bSessionInit = false;

    private ConcurrentQueue<CPacket> sendBuffer = new ConcurrentQueue<CPacket>();

    public bool SendPacket(CPacket packet)
    {
        sendBuffer.Enqueue(packet);

        return true;
    }

    public async Task SendAllQueuedPacketsAsync()
    {
        // 1. 큐의 모든 패킷을 ArraySegment 리스트로 변환
        List<ArraySegment<byte>> buffers = new List<ArraySegment<byte>>();

        // 락이 없어도 ConcurrentQueue는 안전하게 Dequeue 가능
        while (sendBuffer.TryDequeue(out CPacket packet))
        {
            ArraySegment<byte> buffer = packet.GetBuffer();
            // ArraySegment<byte>를 생성하여 리스트에 추가 (데이터 복사 없음)
            buffers.Add(buffer);
        }

        if (buffers.Count == 0)
        {
            return; // 보낼 데이터 없음
        }

        // 2. Scatter/Gather I/O 실행
        // Socket.SendAsync 오버로드를 사용하여 리스트에 담긴 모든 버퍼를 한 번의 I/O 작업으로 전송합니다.
        // C++의 WSASend(WSABUF)와 동일한 고성능 I/O를 제공합니다.
        await socket.SendAsync(buffers, SocketFlags.None);
    }

}


class CSessionManager
{
    public const int sessionMaxCount = 100;
    List<CSession> sessionList = new List<CSession>(sessionMaxCount);
    public int currentSessionIndex = 0;
    public CSessionManager()
    {

    }

    public CSession InitNewSession(Socket socket)
    {
        int myIndex = Interlocked.Increment(ref currentSessionIndex);

        sessionList[myIndex].socket = socket;
        sessionList[myIndex].bSessionInit = true;

        return sessionList[myIndex];
    }
    public CSession GetSession(uint idex)
    {
        if (idex > sessionMaxCount)
        {
            Debugger.Break();
        }

        return sessionList[(int)idex];
    }




}