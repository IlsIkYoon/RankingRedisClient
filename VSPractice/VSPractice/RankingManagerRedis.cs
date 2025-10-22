

using System.Security.Cryptography;
using StackExchange.Redis;
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

    public override int GetHashCode()
    {
         return HashCode.Combine(UserID, Level, Rank);
    }

}


public class CRankingManagerRedis
{
    public SessionManager sessionManager;
    public NetworkManager networkManager;

    public AutoResetEvent _syncEvent = new AutoResetEvent(false);

    public const string MASTER_KEY = "level_ranking_write";
    public const string CACHE_KEY = "level_ranking_read";
    public const string TIMESTAMP_KEY = "ranking_last_update_ts";
    public const string LOCK_KEY = "ranking_update_lock";
    public const int UPDATE_INTERVAL_SEC = 15; // 랭킹 갱신 주기(초)
    public const int LOCK_TTL_SEC = 5;

    public List<RankItem> CurrentRankingData = new List<RankItem>();

    private static EventWaitHandle _stopEvent = new ManualResetEvent(false);
    public CRankingManagerRedis()
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
        //KEYS[1] = ranking key
        //-- ARGV[1] = user level (score) 
        //-- ARGV[2] = max rank (예: 100)
        //-- ARGV[3] = user ID (member)
        string luaScript = @"
        
        local userLevel = ARGV[1]
        local maxRank = tonumber(ARGV[2])
        local userID = ARGV[3]
        local level_ranking_write = KEYS[1];

        local count = redis.call('ZCARD', level_ranking_write)

        if count < tonumber(maxRank) then
            redis.call('ZADD', level_ranking_write, userLevel, userID)
            return 1
        end
        
        local lowest = redis.call('ZRANGE', level_ranking_write, 0, 0, 'WITHSCORES')
        local lowest_score = tonumber(lowest[2])
        
        if tonumber(ARGV[1]) <= lowest_score then
            return 0
        end
        
        local added_count = redis.call('ZADD', level_ranking_write, userLevel, userID)
        count = redis.call('ZCARD', level_ranking_write)

        if count > maxRank then
            redis.call('ZREMRANGEBYRANK', level_ranking_write, 0, 0)
        end

        return 1";

        string rankingKey = "level_ranking_write";
        int maxRank = 100;

        var result = (int)networkManager.redisConnector._db.ScriptEvaluate(luaScript, new RedisKey[] { rankingKey },
            new RedisValue[] { level, maxRank, ID });

        if (result == 1)
            Console.WriteLine("랭킹 업데이트됨");
        else
            Console.WriteLine("랭킹 갱신 필요 없음");

        return true;
    }
    public static void ExitRankingTestThread()
    {
        Console.WriteLine("RakingThread 종료 시그널");
        _stopEvent.Set();
    }

    public void DoRankingTest() 
    {
        ConenctRedis();

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

        Console.WriteLine($"Ranking thread 종료 : {Thread.CurrentThread.ManagedThreadId}");
    }

    public bool GetRanking()
    {
        //KEYS[1]	ranking_last_update_ts	마지막 갱신 시간을 저장하는 키 (String)
        //KEYS[2]	level_ranking_write	실시간 업데이트가 발생하는 마스터 ZSET 키
        //KEYS[3]	level_ranking_read	클라이언트가 읽는 캐시 ZSET 키
        //KEYS[4]	ranking_update_lock	갱신 중복을 막는 분산 락 키 (String)
        //ARGV[1]	current_ts	현재 유닉스 타임스탬프 (초)
        //ARGV[2]	update_interval_sec	갱신 주기 (3시간 = 10800초)
        //ARGV[3]	lock_ttl_sec	락이 풀릴 때까지의 시간 (갱신 예상 시간보다 길게)

        string luaScript = @"
            local last_ts_key = KEYS[1]
            local level_ranking_write = KEYS[2]
            local level_ranking_read = KEYS[3]
            local ranking_update_lock = KEYS[4] 

            local current_ts = tonumber(ARGV[1])
            local Update_interval_ts = tonumber(ARGV[2])
            local lock_ttl = tonumber(ARGV[3])

            local last_ts_str = redis.call('GET', last_ts_key)
            local last_ts = tonumber(last_ts_str) or 0

            if (current_ts - last_ts) < Update_interval_ts then
                return {0, redis.call('ZCARD', level_ranking_read)} 
            end

            local lock_acquired = redis.call('SET', ranking_update_lock, '1', 'EX', lock_ttl, 'NX') 

            if lock_acquired then
                local master_data = redis.call('ZRANGE', level_ranking_write, 0, -1, 'WITHSCORES')
                redis.call('DEL', level_ranking_read)

                if #master_data > 0 then
                    local zadd_args = {level_ranking_read}
                    for i=1, #master_data, 2 do
                        table.insert(zadd_args, master_data[i+1])
                        table.insert(zadd_args, master_data[i])
                    end
                    redis.call('ZADD', unpack(zadd_args))
                end
                
                redis.call('SET', last_ts_key, current_ts)
                redis.call('DEL', ranking_update_lock)

                return {1, redis.call('ZCARD', level_ranking_read)} 
            else
                return {0, redis.call('ZCARD', level_ranking_read)} 
            end
        ";

        // 2. 스크립트 실행
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        RedisResult scriptResult = networkManager.redisConnector._db.ScriptEvaluate(
            luaScript,
            new RedisKey[] { TIMESTAMP_KEY, MASTER_KEY, CACHE_KEY, LOCK_KEY }, // 4개의 KEYS
            new RedisValue[] { currentTimestamp, UPDATE_INTERVAL_SEC, LOCK_TTL_SEC } // 3개의 ARGV
        );

        RedisValue[] results = (RedisValue[])scriptResult;
        int updatedFlag = (int)results[0];

        if (updatedFlag == 1)
        {
            Console.WriteLine($"[Ranking Cache] 갱신 주기가 경과하여 랭킹 캐시가 갱신되었습니다.");
        }

          SortedSetEntry[] entriesWithScores = networkManager.redisConnector._db.SortedSetRangeByRankWithScores(
              CACHE_KEY,
              0,
              99,
              Order.Descending
          );
          CurrentRankingData.Clear();
          int rank = 1;
          foreach (var redisEntry in entriesWithScores)
          {
               UInt64 userID = (UInt64)redisEntry.Element;
               uint level = (uint)redisEntry.Score;
               // 새로운 RankItem 객체를 생성하여 리스트에 추가합니다.
               CurrentRankingData.Add(new RankItem
               {
                   UserID = userID,
                   Level = level,
                   Rank = rank++ // 순위(Rank)는 1부터 시작하며, 항목을 추가할 때마다 1씩 증가
               });
           }
           Console.WriteLine($"[Ranking Read] {CurrentRankingData.Count}개 랭킹 항목을 캐시에서 읽어 리스트에 저장했습니다.");
           return entriesWithScores.Length > 0;
    }
}
