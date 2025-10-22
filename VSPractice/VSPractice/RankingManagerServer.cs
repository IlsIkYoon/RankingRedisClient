
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct messageHeader
{
    public char type;
    public UInt64 userID;
    public uint userLevel;
}

enum enMessageType
{
    UpdateMessage = 0, 
    GetMessage
}

class CRankingManagerServer
{
    SessionManager sessionManager = new SessionManager(100);

//네트워크 클래스도 하나 필요

    public List<RankItem> CurrentRankingData = new List<RankItem>();
    private static EventWaitHandle _stopEvent = new ManualResetEvent(false);

    public AutoResetEvent _syncEvent = new AutoResetEvent(false);


    public void DoRankingTest()
    {

        UInt64 resultID = 0;
        uint resultLevel = 0;
        bool bLevelUp = false;
        long readIntervalTS = 20;
        long lastTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        while (true)
        {
            bLevelUp = sessionManager.RandomPlayerLevelUp(out resultID, out resultLevel);
            if (bLevelUp == true)
            {
                UpdateRanking(resultID, resultLevel);
            }

            if (_stopEvent.WaitOne(0)) //종료 시그널 확인
            {
                break;
            }

            long currentTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (currentTS - lastTS > readIntervalTS)
            {
                bool getResult;
                getResult = GetRanking();
                if (getResult == true)
                {
                    _syncEvent.Set();
                    lastTS = currentTS;
                }
            }

            Thread.Sleep(1);
        }

    }

    bool UpdateRanking(UInt64 ID, uint level)
    {
        messageHeader msgHeader = new messageHeader();
        msgHeader.type = (char)enMessageType.UpdateMessage;
        msgHeader.userID = ID;
        msgHeader.userLevel = level;


        return true;
    }

    public bool GetRanking()
    {
        messageHeader msgHeader = new messageHeader();
        msgHeader.type = (char)enMessageType.GetMessage;
        msgHeader.userID = 0;
        msgHeader.userLevel = 0;



        return true;
    }



}