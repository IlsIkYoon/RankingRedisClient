


using System.Diagnostics;

public class RankItem : IEquatable<RankItem>
{
    public UInt64 UserID { get; set; }
    public uint Level { get; set; }
    public int Rank { get; set; }

    public bool Equals(RankItem other)
    {
        if (other == null)
        {
            return false;
        }

        return this.UserID == other.UserID &&
                    this.Level == other.Level &&
                    this.Rank == other.Rank;
    }

    public RankItem Clone()
    {
        return new RankItem
        {
            UserID = this.UserID,
            Level = this.Level,
            Rank = this.Rank
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserID, Level, Rank);
    }

}

public class RankItemComparer : IComparer<RankItem>
{
    public int Compare(RankItem x, RankItem y)
    {
        if (x == null)
        {
            Debugger.Break();
            return (y == null) ? 0 : -1;
        }
        if (y == null)
        {
            Debugger.Break();
            return 1;
        }
        // 1. Level 기준으로 비교 (내림차순 정렬: 높은 레벨이 먼저)
        // x.Level이 y.Level보다 크면(레벨이 높으면) 음수(-1)를 반환하여 x가 y보다 앞에 오도록 함.
        int levelComparison = y.Level.CompareTo(x.Level); 
        if (levelComparison != 0)
        {
            return levelComparison;
        }
        // 2. Level이 같으면 UserID 기준으로 비교 (오름차순 정렬: 낮은 ID가 먼저)
        // x.UserID가 y.UserID보다 작으면 음수(-1)를 반환하여 x가 y보다 앞에 오도록 함.
        return x.UserID.CompareTo(y.UserID);
    }
}

class CRankingManager
{
    private SortedSet<RankItem> rank_writeSet = new SortedSet<RankItem>(new RankItemComparer());
    private List<RankItem> rank_readSet = new List<RankItem>(100);
    //rankingchange에 대한 변수들
    private readonly object _rankingChangeLock = new object();
    private long prevTime;
    private const long rankingChangeIntervalTs = 10000; //10초 주기로 랭킹데이터 교체

    private Dictionary<UInt64, RankItem> ID_Map = new Dictionary<ulong, RankItem>();
    private readonly object _writeSetLock = new object();
    uint lessLevel;
    private const int _maxRankCount = 100;
    private static CRankingManager _instance = null;
    private static readonly object _singleTonlock = new object();
    public static CRankingManager Instance
    {
        get
        {
            // 스레드 안전성을 위한 Double-Check Locking
            if (_instance == null)
            {
                lock (_singleTonlock)
                {
                    if (_instance == null)
                    {
                        _instance = new CRankingManager();
                    }
                }
            }
            return _instance;
        }
    }
    private CRankingManager()
    {
        Console.WriteLine("RankingManager 생성자 호출");
        prevTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public bool UpdateRanking(UInt64 userID, uint level)
    {
        if (level < lessLevel)  //더블 체크 방식으로 lessLevel보다 낮으면 바로 리턴 시키기
        {
            return false;
        }

        //락 진입 후 업데이트
        lock (_writeSetLock)
        {
            RankItem newRankItem = new RankItem();
            newRankItem.UserID = userID;
            newRankItem.Level = level;
            newRankItem.Rank = 0;

            int currentSize = rank_writeSet.Count;

            if (currentSize < _maxRankCount) //랭킹 데이터가 꽉차지 않았다면 바로 넣기
            {
                if(ID_Map.ContainsKey(userID) == true) //이미 저장 되었던 ID면 기존걸 삭제해줌
                {
                    RankItem deleteTarget = ID_Map[userID];
                    ID_Map.Remove(userID);
                    bool retval = rank_writeSet.Remove(deleteTarget);
                    if(retval == false)
                    {
                        Debugger.Break();
                    }
                }

                rank_writeSet.Add(newRankItem);
                ID_Map.Add(userID, newRankItem);
                lessLevel = rank_writeSet.Max.Level;

                return true;
            }

            RankItem lowestRank = rank_writeSet.Max;
            if (lowestRank == null)
            {
                Debugger.Break(); //처음 넣는 것도 아니고 이미 랭킹 데이터가 꽉 찬 상황이었을테니 비정상
            }
            if (lowestRank.Level > newRankItem.Level) //레벨이 맨 아래보다 낮으면 랭킹 진입 불가
            {
                return false;
            }

            if(ID_Map.ContainsKey(userID) == true) //이미 저장 되었던 ID면 기존걸 삭제해줌
                {
                    RankItem deleteTarget = ID_Map[userID];
                    ID_Map.Remove(userID);
                    bool retval = rank_writeSet.Remove(deleteTarget);
                    if(retval == false)
                    {
                        Debugger.Break();
                    }
                }

            rank_writeSet.Add(newRankItem);
            rank_writeSet.Remove(lowestRank);
            lessLevel = rank_writeSet.Max.Level;
        }

        return true;
    }
    public List<RankItem> GetRanking()
    {
        List<RankItem> retRankData = new List<RankItem>(100);
        lock (_rankingChangeLock)
        {
            if (CanChangeRanking() == true)
            {
                lock (_writeSetLock) //writeset을 readset에 넣기 작업
                {
                    rank_readSet.Clear();
                    foreach (var item in rank_writeSet)
                    {
                        rank_readSet.Add(item);
                    }
                }

            }
            foreach (var item in rank_readSet)
            {
                retRankData.Add(item.Clone()); //아예 새로 넣어주기 (깊은 복사)
            }
        }

        return retRankData; 
    }


    private bool CanChangeRanking() //락 잡고 호출해야 함
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); ;
        if (currentTime - prevTime > rankingChangeIntervalTs)
        {
            prevTime = currentTime;
            return true;
        }

        return false;
    }

}

