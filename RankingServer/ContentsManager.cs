


using System.Diagnostics;
using System.Net.Http.Headers;

class ContentsManager
{

    public static void OnMessage(CSession currentSession, ContentsMessage currentMessage)
    {

        switch ((enContentsType)currentMessage.type)
        {
            case enContentsType.CS_GetRanking:
                GetRanking(currentSession);
                break;

            case enContentsType.CS_UpdateRanking:
                UpdateRanking(currentSession, currentMessage.ID, currentMessage.level);
                break;

            default:
                Debugger.Break();
                break;
        }


    }

    public static void GetRanking(CSession currentSession)
    {
        List<RankItem> rankingData = CRankingManager.Instance.GetRanking();

        CPacket newPacket = new CPacket();
        ContentsMessage contentsHeader = new ContentsMessage();
        contentsHeader.type = (byte)enContentsType.SC_GetRankingReturn;
        contentsHeader.ID = 0;
        contentsHeader.level = 0;

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms)) //컨텐츠 헤더 붙이기
        {
            bw.Write(contentsHeader.type);
            bw.Write(contentsHeader.ID);
            bw.Write(contentsHeader.level);

            byte[] rankItemBytes = ms.ToArray();
            long length = ms.Length;

            newPacket.EnqueueData(rankItemBytes, (int)length);
        }
        
        foreach (var item in rankingData) //RankData 패킷에 넣기
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(item.UserID);
                bw.Write(item.Level);
                bw.Write(item.Rank);

                byte[] rankItemBytes = ms.ToArray();
                long length = ms.Length;

                newPacket.EnqueueData(rankItemBytes, (int)length);
            }
        }

        newPacket.MakeNetworkHeader();
        currentSession.SendPacket(newPacket);        
    }
    public static void UpdateRanking(CSession currentSession, Int64 ID, uint level)
    {
        CRankingManager.Instance.UpdateRanking((UInt64)ID, level);
    }

}