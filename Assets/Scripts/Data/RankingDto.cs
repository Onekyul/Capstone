using System;
using System.Collections.Generic;

/// <summary>
/// 보스던전 랭킹 조회 DTO.
/// GET /api/Ranking/boss?top=N 응답 형식.
/// </summary>
[Serializable]
public class RankingResponseDto
{
    public List<RankingEntryDto> rankings = new List<RankingEntryDto>();
}

[Serializable]
public class RankingEntryDto
{
    public int rank;
    public string nickname;
    public float clearTime;
}
