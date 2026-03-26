using Fusion;
using UnityEngine;

/// <summary>
/// 보스 스킬 5종을 클라이언트에 비주얼 동기화.
/// 서버: Perform*Attack 이후 RPC 호출 → 클라이언트: 동일 프리팹 생성 (데미지는 HasStateAuthority 가드로 차단).
/// </summary>
public class BossSkillSyncManager : NetworkBehaviour
{
    public static BossSkillSyncManager Instance { get; private set; }

    [Header("스킬 프리팹 (클라이언트 비주얼용)")]
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject iceClusterPrefab;
    [SerializeField] private GameObject thunderPatternPrefab;
    [SerializeField] private GameObject poisonSporePrefab;
    [SerializeField] private GameObject waterWavePrefab;

    public override void Spawned()
    {
        Instance = this;
    }

    // ── Fire ──

    public void SyncFireAttack(Vector2 from, Vector2 to, float damage, float speed,
        float fieldDuration, float fieldMaxScale, float fieldExpandSpeed,
        float fieldDamageInterval, float fieldDamage)
    {
        RPC_SpawnFire(from, to, damage, speed, fieldDuration, fieldMaxScale,
            fieldExpandSpeed, fieldDamageInterval, fieldDamage);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnFire(Vector2 from, Vector2 to, float damage, float speed,
        float fieldDuration, float fieldMaxScale, float fieldExpandSpeed,
        float fieldDamageInterval, float fieldDamage)
    {
#if UNITY_SERVER
        return; // 서버는 BossMonsterController에서 이미 생성
#endif
        if (fireProjectilePrefab == null) return;
        var obj = Instantiate(fireProjectilePrefab, (Vector3)from, Quaternion.identity);
        var proj = obj.GetComponent<FireMortarProjectile>();
        if (proj != null)
            proj.Setup(to, damage, speed, fieldDuration, fieldMaxScale,
                fieldExpandSpeed, fieldDamageInterval, fieldDamage);
    }

    // ── Ice ──

    public void SyncIceAttack(Vector2 targetPos, float warningDuration, float bodyDuration,
        float contactDamage, float shardDamage, float shardSpeed, int shardCount, float shardLifetime)
    {
        RPC_SpawnIce(targetPos, warningDuration, bodyDuration, contactDamage,
            shardDamage, shardSpeed, shardCount, shardLifetime);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnIce(Vector2 targetPos, float warningDuration, float bodyDuration,
        float contactDamage, float shardDamage, float shardSpeed, int shardCount, float shardLifetime)
    {
#if UNITY_SERVER
        return;
#endif
        if (iceClusterPrefab == null) return;
        var obj = Instantiate(iceClusterPrefab, (Vector3)targetPos, Quaternion.identity);
        var cluster = obj.GetComponent<IceClusterController>();
        if (cluster != null)
            cluster.Setup(warningDuration, bodyDuration, contactDamage,
                shardDamage, shardSpeed, shardCount, shardLifetime);
    }

    // ── Thunder ──

    public void SyncThunderAttack(Vector2 targetPos, float warningDuration, float strikeDamage,
        float strikeRadius, float strikeVfxDuration, float fieldDuration, float fieldDamagePerTick)
    {
        RPC_SpawnThunder(targetPos, warningDuration, strikeDamage, strikeRadius,
            strikeVfxDuration, fieldDuration, fieldDamagePerTick);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnThunder(Vector2 targetPos, float warningDuration, float strikeDamage,
        float strikeRadius, float strikeVfxDuration, float fieldDuration, float fieldDamagePerTick)
    {
#if UNITY_SERVER
        return;
#endif
        if (thunderPatternPrefab == null) return;
        var obj = Instantiate(thunderPatternPrefab, (Vector3)targetPos, Quaternion.identity);
        var pattern = obj.GetComponent<ThunderPatternController>();
        if (pattern != null)
            pattern.Setup(warningDuration, strikeDamage, strikeRadius,
                strikeVfxDuration, fieldDuration, fieldDamagePerTick);
    }

    // ── Poison ──

    public void SyncPoisonAttack(Vector2 from, Vector2 targetPos, float speed,
        float damage, float life, float radius)
    {
        RPC_SpawnPoison(from, targetPos, speed, damage, life, radius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnPoison(Vector2 from, Vector2 targetPos, float speed,
        float damage, float life, float radius)
    {
#if UNITY_SERVER
        return;
#endif
        if (poisonSporePrefab == null) return;
        var obj = Instantiate(poisonSporePrefab, (Vector3)from, Quaternion.identity);
        var spore = obj.GetComponent<PoisonSporeController>();
        if (spore != null)
        {
            // 클라이언트에서는 플레이어 Transform 직접 추적 대신 임시 오브젝트 사용
            var tempTarget = new GameObject("_PoisonVisualTarget");
            tempTarget.transform.position = (Vector3)targetPos;
            spore.Setup(tempTarget.transform, speed, damage, life, radius);
            Destroy(tempTarget, life + 1f);
        }
    }

    // ── Water ──

    public void SyncWaterAttack(Vector2 from, Vector2 direction, float speed, float damage,
        float lifetime, float startScale, float targetScale)
    {
        RPC_SpawnWater(from, direction, speed, damage, lifetime, startScale, targetScale);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnWater(Vector2 from, Vector2 direction, float speed, float damage,
        float lifetime, float startScale, float targetScale)
    {
#if UNITY_SERVER
        return;
#endif
        if (waterWavePrefab == null) return;
        var obj = Instantiate(waterWavePrefab, (Vector3)from, Quaternion.identity);
        var wave = obj.GetComponent<WaterWaveController>();
        if (wave != null)
            wave.Setup(direction, speed, damage, lifetime, startScale, targetScale);
    }
}
