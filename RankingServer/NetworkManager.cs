using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;


class CNetworkManager
{
    //SessionList
    public CSessionManager sessionManager = new CSessionManager();
    private const int serverPort = 8080;
    private const int BufferSize = 8192;
    public Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Thread SendThread;

    public async Task Start()
    {
        Console.WriteLine("NetworkManager Start!!");
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
        listenSocket.Listen(500);

        SendThread = new Thread(new ThreadStart(SendThreadFunc));

        try
        {
            while (true)
            {
                Socket clientSocket = await listenSocket.AcceptAsync();

                _ = HandleClientAsync(clientSocket);
                Console.WriteLine($"[INFO] New client connected: {clientSocket.RemoteEndPoint}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ERROR] Socket Error: {ex.Message}");
        }
        finally
        {
            listenSocket.Close();
        }
    }

    private async Task HandleClientAsync(Socket clientSocket)
    {
        CPacket currentPacket = new CPacket();

        CSession mySession = sessionManager.InitNewSession(clientSocket);

        while (true)
        {
            try
            {
                int bytesRead = await clientSocket.ReceiveAsync(currentPacket.GetEnqueueBuffer(), SocketFlags.None);
                if (bytesRead == 0)
                {
                    break;
                }
                while (true)
                {
                    if (MessageHandler.HandleMessage(mySession, currentPacket) != MessageHandler.Success)
                    {
                        break;
                    }
                }
                currentPacket.InitPacket(); //패킷 데이터 앞으로 땡겨주는 작업
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                Console.WriteLine($"[INFO] Client disconnected unexpectedly: {clientSocket.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Handling client failed: {ex.Message}");
                Debugger.Break();
            }
            finally
            {
                clientSocket.Close();
            }
        }
    }

    public void SendAllSession() //SendThread에서 싱글로 계속 돌아갈 예정
    {
        var sendTasks = new List<Task>();

        for (uint i = 0; i < CSessionManager.sessionMaxCount; i++)
        {
            CSession currentSession = sessionManager.GetSession(i);
            lock (currentSession.sessionLock)
            {
                if (currentSession.bSessionInit == false)
                {
                    continue;
                }

                //send작업 병렬로 실행
                sendTasks.Add(currentSession.SendAllQueuedPacketsAsync());
            }
        }
        Task.WhenAll(sendTasks);
    }

    public void SendThreadFunc()
    {
        long prevTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        while (true)
        {
            SendAllSession();
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long intervalTime = currentTime - prevTime;
            Thread.Sleep(40 - (int)intervalTime);
        }
    }

}


