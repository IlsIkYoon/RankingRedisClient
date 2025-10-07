

using System.Security.Cryptography;

public class RankingManager
{
    public SessionManager sessionManager;
    public NetworkManager networkManager;
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

    bool UpdateRanking(UInt64 ID, uint level)
    {
        //랭킹 업데이트 작업
        //todo//Redis를 이용한 쿼리 원자적 작업 필요


        return true;
    }


    public void DoRankingTest() //쓰레드 함수
    {
        Console.WriteLine("DoRankingTest 함수 호출");

        ConenctRedis();

        int count = 0;

        UInt64 resultID = 0;
        uint resultLevel = 0;
        bool bLevelUp = false;

        while (true)
        {
            count++;
            //종료에 대한 건 이벤트 시그널 확인
            bLevelUp = sessionManager.RandomPlayerLevelUp(out resultID, out resultLevel);
            if (bLevelUp == true)
            {
                UpdateRanking(resultID, resultLevel);
            }
            
            //Frame Sleep


            if (count == 20)
            {
                break;
            }
            Thread.Sleep(10000);
        }


    }

}