// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");



CNetworkManager networkManager = new CNetworkManager();
_ = networkManager.Start(); //네트워크 매니저에서 알아서 코드 돌아감

//여기서 Send작업 하자

//메인에선 종료 요청 등의 작업만 하면 됨

Thread.Sleep(1000);



return 0;