using Fusion;
using UnityEngine;

/// <summary>
/// 보스 MonsterController(MonoBehaviour)의 상태를 네트워크로 동기화하는 래퍼.
/// 보스 프리팹에 NetworkObject + 이 컴포넌트를 추가.
/// BossMonsterController 자체는 수정하지 않음.
///
/// 서버: BossMonsterController 값을 읽어 [Networked]에 저장
/// 클라이언트: [OnChangedRender]로 HP/위치 변화 감지 → UI 업데이트, BossMonsterController 비활성화
/// </summary>
public class NetworkBossWrapper : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnBossHPChanged))]
    public float NetBossHP { get; set; }

    [Networked]
    public float NetBossMaxHP { get; set; }

    [Networked, OnChangedRender(nameof(OnBossPhaseChanged))]
    public int NetBossPhase { get; set; } // 0=Normal, 1=Rage, 2=Groggy

    [Networked, OnChangedRender(nameof(OnBossPositionChanged))]
    public Vector2 NetBossPosition { get; set; }

    [Networked, OnChangedRender(nameof(OnBossDeadChanged))]
    public NetworkBool NetBossDead { get; set; }

    private BossMonsterController _boss;

    public override void Spawned()
    {
        _boss = GetComponent<BossMonsterController>();

        if (_boss == null)
        {
            Debug.LogError("[NetworkBossWrapper] BossMonsterController를 찾을 수 없습니다! 같은 오브젝트에 붙어있어야 합니다.");
            return;
        }

        if (HasStateAuthority)
        {
            // 서버: 보스 오브젝트 활성화 및 초기 스탯 동기화
            if (!_boss.gameObject.activeSelf)
                _boss.gameObject.SetActive(true);

            NetBossDead = false;
            NetBossMaxHP = _boss.GetMaxHP();
            NetBossHP = _boss.GetCurrentHP();
            NetBossPosition = transform.position;
            Debug.Log($"[NetworkBossWrapper] 보스 초기화: MaxHP={NetBossMaxHP}, HP={NetBossHP}");
        }
        else
        {
            // 클라이언트: 보스 AI/로직 비활성화 (서버에서만 처리)
            _boss.enabled = false;
            Debug.Log("[NetworkBossWrapper] 클라이언트: BossMonsterController 비활성화");

            // 클라이언트에서 BossStageManager 비활성화 (몬스터 중복 스폰 방지)
            if (BossStageManager.instance != null)
            {
                BossStageManager.instance.StopAllCoroutines();
                BossStageManager.instance.enabled = false;
                Debug.Log("[NetworkBossWrapper] 클라이언트: BossStageManager 비활성화");
            }

            // UI 초기값 설정
            if (BossDungeonUIManager.instance != null)
                BossDungeonUIManager.instance.UpdateBossHP(NetBossHP, NetBossMaxHP);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || _boss == null) return;

        // 서버: 보스 실제 상태를 [Networked]에 매 틱 반영
        NetBossHP = _boss.GetCurrentHP();
        NetBossPhase = (int)_boss.CurrentPhase;
        NetBossPosition = transform.position;

        if (_boss.GetCurrentHP() <= 0 && !NetBossDead)
        {
            NetBossDead = true;
        }
    }

    // === 클라이언트 OnChangedRender 콜백 ===

    public void OnBossHPChanged()
    {
#if !UNITY_SERVER
        if (BossDungeonUIManager.instance != null)
            BossDungeonUIManager.instance.UpdateBossHP(NetBossHP, NetBossMaxHP);
#endif
    }

    public void OnBossPhaseChanged()
    {
#if !UNITY_SERVER
        var phase = (BossMonsterController.BossPhase)NetBossPhase;
        Debug.Log($"[클라이언트] 보스 페이즈 변경: {phase}");
        // 페이즈별 클라이언트 시각 효과 추가 가능
#endif
    }

    public void OnBossPositionChanged()
    {
#if !UNITY_SERVER
        // 클라이언트: 서버 위치로 보스 오브젝트 이동
        if (!HasStateAuthority)
            transform.position = (Vector3)(Vector2)NetBossPosition;
#endif
    }

    public void OnBossDeadChanged()
    {
#if !UNITY_SERVER
        if (NetBossDead && !HasStateAuthority)
        {
            Debug.Log("[클라이언트] 보스 사망 처리");
            gameObject.SetActive(false);
        }
#endif
    }
}
