

using System.Security.Cryptography;

public class RankingManager
{
    public SessionManager sessionManager;
    public NetworkManager networkManager;

    //todo//100위 전체 랭킹이 들어오는 자료구조가 하나 있어야 함 

    private static EventWaitHandle _stopEvent = new ManualResetEvent(false);
    public RankingManager()
    {
        sessionManager = new SessionManager(1000);
        networkManager = new NetworkManager();
    }

    public bool ConenctRedis()
    {
        networkManager.ConnectRankingServer();
        return true;
    }
    bool GetRanking()
    {
        //내 서버 랭킹 자료구조에 넣어줌 

        return true;
    }

    bool UpdateRanking(UInt64 ID, uint level)
    {
        //랭킹 업데이트 작업
        //todo//Redis를 이용한 쿼리 원자적 작업 필요


        return true;
    }
    public static void ExitRankingTestThread()
    {
        Console.WriteLine("RakingThread 종료 시그널");
        _stopEvent.Set();   
    }

    public void DoRankingTest() //쓰레드 함수
    {
        Console.WriteLine("DoRankingTest 함수 호출");

        ConenctRedis();

        UInt64 resultID = 0;
        uint resultLevel = 0;
        bool bLevelUp = false;

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

            Thread.Sleep(100);
        }

        Console.WriteLine($"Ranking thread 종료 : {Thread.CurrentThread.ManagedThreadId}");
    }

}