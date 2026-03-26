using System;
using System.Collections.Generic;

/// <summary>
/// 보스던전 결과 전송 DTO.
/// POST /api/Dungeon/result 요청 형식.
/// nickname은 백엔드가 userId로 자체 조회.
/// </summary>
[Serializable]
public class DungeonResultDto
{
    public string sessionName;
    public int partyId;
    public List<PlayerResultDto> results = new List<PlayerResultDto>();
}

[Serializable]
public class PlayerResultDto
{
    public int userId;
    public bool cleared;
    public float clearTime;
}

[Serializable]
public class DungeonResultResponse
{
    public bool success;
}
