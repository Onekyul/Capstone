using System;
using System.Collections.Generic;

// ============================
//  목록 조회
// ============================

[Serializable]
public class PartyDto
{
    public int partyId;
    public string title;
    public string leaderName;
    public int currentCount;
    public int maxCount;
    public int dungeonId;
}

[Serializable]
public class PartyListWrapper
{
    public PartyDto[] parties;
}

// ============================
//  상세 조회 (대기실 폴링)
// ============================

[Serializable]
public class PartyMemberDto
{
    public int userId;
    public string nickname;
    public bool isReady;
}

[Serializable]
public class PartyDetailRes
{
    public int partyId;
    public int leaderId;
    public int dungeonId;
    public int maxCount;
    public List<PartyMemberDto> members = new List<PartyMemberDto>();
}

// ============================
//  파티 생성
// ============================

[Serializable]
public class PartyCreateReq
{
    public string title;
    public int leaderId;
    public string leaderNickname;
    public int maxCount;
    public int dungeonId;
}

[Serializable]
public class PartyCreateRes
{
    public int partyId;
    public string message;
    public int leaderId;
}

// ============================
//  파티 가입
// ============================

[Serializable]
public class PartyJoinReq
{
    public int partyId;
    public int userId;
    public string nickname;
}

[Serializable]
public class PartyJoinRes
{
    public int partyId;
    public string message;
    public int currentCount;
    public int leaderId;
}

// ============================
//  준비
// ============================

[Serializable]
public class PartyReadyReq
{
    public int partyId;
    public int userId;
    public bool isReady;
}

[Serializable]
public class PartyReadyRes
{
    public string message;
}

// ============================
//  탈퇴
// ============================

[Serializable]
public class PartyLeaveReq
{
    public int partyId;
    public int userId;
}

[Serializable]
public class PartyLeaveRes
{
    public string message;
}

// ============================
//  던전 입장
// ============================

[Serializable]
public class PartyEnterReq
{
    public int partyId;
    public int userId;
}

[Serializable]
public class PartyEnterRes
{
    public string sessionName;
    public string message;
}
