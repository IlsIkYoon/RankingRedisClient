


using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

//네트워크 헤더
// 길이

//컨텐츠 메세지
//타입
//길이
//
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct NetworkHeader
{
   public ushort len;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ContentsMessage
{
    public byte type;
    public Int64 ID;
    public uint level;
}

//GetRankingReturn은 타입, ID, Level을 보내고 List를 보내면 됨

public enum enContentsType : byte
{
    CS_UpdateRanking = 0,
    CS_GetRanking,
    SC_GetRankingReturn

}