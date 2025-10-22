// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

CRankingManagerRedis[] rankingTest = new CRankingManagerRedis[3];
for (int i = 0; i < 3; i++)
{
    rankingTest[i] = new CRankingManagerRedis();
}

AutoResetEvent[] rankingReadEvent = new AutoResetEvent[3];
for (int i = 0; i < 3; i++)
{
    rankingReadEvent[i] = rankingTest[i]._syncEvent;
}

Task[] taskArr = new Task[3];

for (int i = 0; i < 3; i++)
{
    using (CProfiler profiler = new CProfiler("TaskRun"))
    {
        taskArr[i] = Task.Run(rankingTest[i].DoRankingTest);
    }
}

int checkCount = 0;

while (true)
{
    AutoResetEvent.WaitAll(rankingReadEvent);

    bool retval1 = rankingTest[0].CurrentRankingData.SequenceEqual(rankingTest[1].CurrentRankingData);
    bool retval2 = rankingTest[0].CurrentRankingData.SequenceEqual(rankingTest[2].CurrentRankingData);
    if (retval1 != true || retval2 != true)
    {
        Debugger.Break();
    }

    Console.WriteLine("저장된 랭킹 정보가 동일합니다 !!!!!");
    checkCount++;

    if (checkCount == 1000)
    {
        break;
    }
}

CRankingManagerRedis.ExitRankingTestThread(); //쓰레드 종료 시키기

CProfilerManager.WriteProfileData();

Task.WhenAll(taskArr).Wait();

Console.WriteLine("Task Wait 완료");

return 0;