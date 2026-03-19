/// <summary>
/// 스킬/몬스터 데미지를 PlayerStats(싱글) 또는 DungeonPlayerStats(네트워크) 중 올바른 쪽에 전달.
/// </summary>
public static class PlayerDamageHelper
{
    public static void TakeDamage(UnityEngine.GameObject playerObj, float damage)
    {
        // 보스 던전 데디서버: DungeonPlayerStats 우선
        var dungeonStats = playerObj.GetComponent<DungeonPlayerStats>();
        if (dungeonStats != null) { dungeonStats.TakeDamage(damage); return; }

        // 일반 싱글 던전: PlayerStats
        var stats = playerObj.GetComponent<PlayerStats>();
        if (stats != null) stats.TakeDamage(damage);
    }
}
