// See https://aka.ms/new-console-template for more information

//필요 모듈
//NetworkManager
//RankingManager
//PlayerManager

RankingManager[] rankingTest = new RankingManager[3];
for (int i = 0; i < 3; i++)
{
    rankingTest[i] = new RankingManager();
}

Task t1 = Task.Run(rankingTest[0].DoRankingTest);
Task t2 = Task.Run(rankingTest[1].DoRankingTest);
Task t3 = Task.Run(rankingTest[2].DoRankingTest);

Console.WriteLine("Task 모두 호출 완료");

Task.WhenAll(t1, t2, t3).Wait();


Console.WriteLine("Task Wait 완료");


return 0;