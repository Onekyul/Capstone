using UnityEngine;

public class AltarController : MonsterController
{
    [Header("Altar Settings")]
    [SerializeField] private float hpPerMinion = 50f;

    // BossStageManager가 재단을 생성할 때 호출
    public void Setup(float minionCount)
    {
        // 잡몹 수에 비례해 체력 설정 (예: 마리당 50)
        MaxHP = minionCount * hpPerMinion;
        if (MaxHP < 10) MaxHP = 100000f; // 최소 체력 보장
        CurHP = MaxHP;
    }

    protected override void ReturnToPool()
    {
        // 1. 보스에게 그로기 모드 전환 알림 (매니저 통해 or 직접)
        BossStageManager.instance.OnAltarDestroyed();

        // 2. 파괴 이펙트 및 제거
        Destroy(gameObject);
    }
}