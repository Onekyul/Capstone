using System;
using System.Collections.Generic;

/// <summary>
/// 데디케이티드 서버가 백엔드 API에서 받아올 플레이어 장비 DTO.
/// GET /api/Game/player-stats?userId=X 응답 형식.
/// 스탯 계산은 서버가 ScriptableObject로 직접 수행.
/// </summary>
[Serializable]
public class PlayerStatsDto
{
    public int userId;
    public string equippedWeapon;
    public string equippedHelmet;
    public string equippedArmor;
    public string equippedBoots;
    public List<EquipItemDto> equipments = new List<EquipItemDto>();
}

[Serializable]
public class EquipItemDto
{
    public string id;
    public int level;
}
