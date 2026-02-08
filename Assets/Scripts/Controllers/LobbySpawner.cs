using UnityEngine;
using Fusion;
public class LobbySpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("프리팹 연결")]
    public NetworkObject playerPrefab; 
    
    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Vector2 randomCircle = Random.insideUnitCircle * 3f; 
            Vector3 spawnPos = new Vector3(randomCircle.x, randomCircle.y, 0); 

            // 캐릭터 생성 (Runner.Spawn 사용)
            NetworkObject myChar = Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            
            Debug.Log($"✅ 캐릭터 생성 완료 (ID: {DataManager.instance.MyUserId})");
        }
    }
}
