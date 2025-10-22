
using System.Diagnostics;

class CProfiler : IDisposable
{
    Stopwatch stopwatch = new Stopwatch();
    string funcName;
    public CProfiler(string funcName)
    {
        stopwatch.Start();
        this.funcName = funcName;
    }


    public void Dispose()
    {
        stopwatch.Stop();
        long resultTick = stopwatch.ElapsedTicks;
        CProfilerManager.SaveProfileData(funcName, resultTick);
        GC.SuppressFinalize(this); 
    }
}

class ProfileData
{
    public string funcName;
    public long tickAmount;
    public int count;
}

class CProfilerManager
{
    private static Dictionary<string, ProfileData> ProfileDataTable = new Dictionary<string, ProfileData>();

    public static void WriteProfileData()
    {
        string filePath = "Profiler_Output.txt";
        
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("======================================================================");
                writer.WriteLine($"프로파일러 데이터 출력 ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")})");
                writer.WriteLine("======================================================================");
                writer.WriteLine("{0,-30} {1,-10} {2,-15} {3,-15}", 
                                 "함수 이름 (FuncName)", 
                                 "호출 횟수 (Count)", 
                                 "누적 시간 (us)", 
                                 "평균 시간 (us/Call)");
                writer.WriteLine("----------------------------------------------------------------------");

                foreach (KeyValuePair<string, ProfileData> entry in ProfileDataTable)
                {
                    ProfileData data = entry.Value;
                    
                    // 평균 시간 계산 (호출 횟수가 0이 아닐 때만)
                    long avgTicks = (data.count > 0) ? (data.tickAmount / data.count) : 0;
                    double microSecond = (double)data.tickAmount * 1_000_000 / Stopwatch.Frequency;
                    double avgSecond = microSecond / data.count;

                    // 데이터를 포맷에 맞춰 파일에 작성
                    writer.WriteLine("{0,-30} {1,-10} {2,-15:N2} {3,-15:N2}",
                                     data.funcName,
                                     data.count,
                                     microSecond,
                                     avgSecond);
                }

                writer.WriteLine("======================================================================");
                Console.WriteLine($"프로파일러 데이터가 '{filePath}'에 성공적으로 저장되었습니다.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"프로파일러 파일 출력 중 오류 발생: {ex.Message}");
        }
    
    }
    public static void ResetProfileDate()
    {
        foreach (KeyValuePair<string, ProfileData> item in ProfileDataTable) // KeyValuePair 타입 사용
        {
            item.Value.count = 0;
            item.Value.tickAmount = 0;
        }
    }
    public static void SaveProfileData(string funcName, long tickTime)
    {
        bool bContain = ProfileDataTable.ContainsKey(funcName);
        if (bContain == true)
        {
            ProfileDataTable[funcName].count++;
            ProfileDataTable[funcName].tickAmount += tickTime;
        }
        else
        {
            ProfileData profileData = new ProfileData();
            profileData.count = 1;
            profileData.funcName = funcName;
            profileData.tickAmount = tickTime;
            ProfileDataTable.Add(funcName, profileData);
        }
        return;
    }

}
