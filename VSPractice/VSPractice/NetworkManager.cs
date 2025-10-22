
using System.Net.Sockets;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

// Redis 연결 및 작업을 위한 클래스
public class RedisClient
{
    private readonly ConnectionMultiplexer _redis;
    public readonly IDatabase _db;

    // 생성자: Redis 서버에 연결합니다.
    public RedisClient(string connectionString)
    {
        Console.WriteLine($"Redis Client 생성자 호출 // Thread ID : {Thread.CurrentThread.ManagedThreadId}");

        try
        {
            // ConnectionMultiplexer는 Redis 연결을 관리하는 핵심 객체입니다.
            // 비용이 많이 드는 작업이므로 앱 생명주기 동안 단 하나의 인스턴스를 유지해야 합니다.
            _redis = ConnectionMultiplexer.Connect(connectionString);

            // 데이터베이스 인스턴스를 가져옵니다. (기본 DB는 0번)
            _db = _redis.GetDatabase();

            Console.WriteLine("Redis 서버에 성공적으로 연결되었습니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Redis 연결 오류 발생: {ex.Message}");
            throw; // 연결 실패 시 앱을 종료하거나 오류를 던집니다.
        }
    }

    // 간단한 데이터 쓰기 (SET)
    public async Task SetValueAsync(string key, string value)
    {
        // Redis 명령어는 비동기(Async)로 실행하는 것이 성능에 좋습니다.
        bool isSet = await _db.StringSetAsync(key, value);
        if (isSet)
        {
            Console.WriteLine($"[SET 성공] 키: {key}, 값: {value}");
        }
    }

    // 간단한 데이터 읽기 (GET)
    public async Task<string> GetValueAsync(string key)
    {
        RedisValue value = await _db.StringGetAsync(key);
        if (value.IsNull)
        {
            return $"[GET 실패] 키: {key}에 해당하는 값이 없습니다.";
        }
        Console.WriteLine($"[GET 성공] 키: {key}, 값: {value}");
        return value.ToString();
    }

    // 종료 시 연결을 해제합니다.
    public void CloseConnection()
    {
        _redis?.Dispose();
        Console.WriteLine("Redis 연결을 해제했습니다.");
    }
}


public class NetworkManager
{
    public RedisClient? redisConnector;
    public bool ConnectRankingServer()
    {
        //RedisConnect작업
        const string RedisHost = "localhost:6379";

        redisConnector = new RedisClient(RedisHost);

        return true;
    }

}


    
