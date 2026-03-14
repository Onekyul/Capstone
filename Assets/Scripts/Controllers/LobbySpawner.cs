using UnityEngine;
using Fusion;

public class LobbySpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("프리팹 연결")]
    public NetworkObject playerPrefab;

    /// <summary>
    /// Fusion이 새 세션에 입장할 때마다 호출.
    /// 던전 복귀 후 DungeonSessionManager가 세션을 완전 재시작하므로
    /// 이 콜백이 다시 발화하여 정상 스폰된다.
    /// </summary>
    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
            TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (Runner == null || !Runner.IsRunning) return;

        // 이미 스폰된 오브젝트가 있으면 중복 스폰 방지
        if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out _)) return;

        Vector2 randomCircle = Random.insideUnitCircle * 3f;
        Vector3 spawnPos = new Vector3(randomCircle.x, randomCircle.y, 0);

        Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer);

        Debug.Log($"[LobbySpawner] 캐릭터 생성 완료 (ID: {DataManager.instance.MyUserId})");
    }
}
