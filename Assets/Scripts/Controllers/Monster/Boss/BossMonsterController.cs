using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossMonsterController : MonsterController
{
    public enum BossPhase { Normal, Rage, Groggy }
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Normal;

    [Header("Boss Specific")]
    [SerializeField] private float teleportInterval = 20f;
    [SerializeField] private float attackCooldownNormal = 5f;
    [SerializeField] private float attackCooldownRage = 2.5f; // 절반
    [SerializeField] private float groggyDuration = 8f;
    [SerializeField] private float attackRange = 15f;


    [Header("Water Skill")]
    [SerializeField] private GameObject waterWavePrefab; // 파도 프리팹
    [SerializeField] private float waveSpeed = 6.0f;     // 파도 속도
    [SerializeField] private float waveDamage = 15.0f;        // 파도 공격력
    [SerializeField] private float waveLifetime = 2.0f;   // 파도 유지 시간
    [SerializeField] private float startScaleMultiplier = 0.5f; // 시작 크기 배율
    [SerializeField] private float targetScaleMultiplier = 1.5f; // 종료 크기 배율

    [Header("Fire Projectile Settings")]
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private float mortarSpeed = 8f;
    [SerializeField] private float fireDamage = 10f;

    [Header("Fire Field Settings")]
    [SerializeField] private float fireFieldDuration = 3.0f;
    [SerializeField] private float fieldMaxScale = 3.0f;
    [SerializeField] private float fieldExpandSpeed = 2.0f;
    [SerializeField] private float fieldDamageInterval = 0.5f;
    [SerializeField] private float fieldDamage = 5.0f;

    [Header("Ice Skill")]
    [SerializeField] private GameObject iceClusterPrefab; // 아까 만든 얼음 덩어리 프리팹
    [SerializeField] private float iceWarningDuration = 1.5f;
    [SerializeField] private float bodyDuration = 2.0f;
    [SerializeField] private float iceContactDamage = 10f;
    [SerializeField] private float shardDamage = 5f;
    [SerializeField] private float shardSpeed = 5f;
    [SerializeField] private int shardCount = 6;
    [SerializeField] private float shardLifetime = 3.0f;

    [Header("Poison Skill")]
    [SerializeField] private GameObject poisonSporePrefab;   // 독 포자 프리팹
    [SerializeField] private float sporeSpeed = 2.0f;  // 포자 속도 (아주 느리게)
    [SerializeField] private float sporeLife = 5.0f;   // 포자 유지 시간
    [SerializeField] private float explosionRadius = 2.0f; // 폭발 범위
    [SerializeField] private float explosionDamage = 10.0f; // 폭발 데미지

    [Header("Thunder Skill")]
    [SerializeField] private GameObject thunderPatternPrefab; // 위에서 만든 패턴 프리팹
    [SerializeField] private int burstCount = 3;               // 연속 번개 횟수
    [SerializeField] private float burstInterval = 0.1f;       // 번개 간격
    [SerializeField] private float thunderWarningDuration = 1.0f;      // 경고 시간
    [SerializeField] private float strikeDamage = 10f;          // 번개 1발당 데미지
    [SerializeField] private float strikeRadius = 1.5f;         // 번개 피격 범위
    [SerializeField] private float strikeVfxDuration = 0.1f;    // 번개 이펙트 노출 시간
    [SerializeField] private float thunderFieldDuration = 3.0f;        // 장판 유지 시간
    [SerializeField] private float fieldDamagePerTick = 2.0f;   // 장판 틱 데미지

    [SerializeField] private GameObject RageShield;

    private float attackTimer;
    private bool isInvincible = false; // 무적 상태

    protected override void Start()
    {
        base.Start();
        StartCoroutine(TeleportRoutine());
        monsterType = MonsterType.Boss; // 필요하다면 MonsterType에 Boss 추가
        RageShield.SetActive(false);
    }

    protected override void Update()
    {
        // 그로기 상태면 아무것도 안 함
        if (CurrentPhase == BossPhase.Groggy) return;

        // 레이지 모드일 때만 플레이어 추격
        if (CurrentPhase == BossPhase.Rage)
        {
            moveSpeed *= 2; // 레이지 모드에서 속도 2배
            base.Update(); // MonsterController의 이동 로직 사용
        }

        if(CurrentPhase == BossPhase.Normal)
        {
            base.Update(); // 기본 이동 로직
        }

        // 공격 쿨타임 관리
        attackTimer -= Time.deltaTime;
        float currentCooldown = (CurrentPhase == BossPhase.Rage) ? attackCooldownRage : attackCooldownNormal;

        if (attackTimer <= 0)
        {
            PerformRandomAttack();
            attackTimer = currentCooldown;
        }
    }

    // 데미지 받는 함수 오버라이드 (무적 처리 & 그로기 배율)
    public override void TakeDamage(float damage) // MonsterController의 TakeDamage를 virtual로 바꿔주세요
    {
        if (isInvincible) return; // 레이지 모드(재단 존재) 시 무적

        float finalDamage = damage;

        // 그로기 상태: 플레이어 레벨 스택만큼 데미지 증폭
        if (CurrentPhase == BossPhase.Groggy)
        {
            // LevelManager에서 누적된 배율 가져오기 (3단계에서 구현)
            float multiplier = BossDungeonLevelManager.instance.GetBossDamageMultiplier();
            finalDamage *= multiplier;

            // 데미지 폰트 띄울 때 "Critical!" 처럼 크게 띄우기 가능
        }

        base.TakeDamage(finalDamage);
    }

    // --- 행동 로직 ---

    IEnumerator TeleportRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportInterval);
            if (CurrentPhase == BossPhase.Normal)
            {
                // 1. 흐려지기 (Alpha값 조정 or 쉐이더)
                // yield return StartCoroutine(FadeOut());

                // 2. 랜덤 플레이어 위치로 이동
                Transform target = GetClosestPlayer(); // 혹은 랜덤 플레이어
                if (target != null)
                {
                    transform.position = target.position + (Vector3)Random.insideUnitCircle * 3f;
                }

                // 3. 나타나기
                // yield return StartCoroutine(FadeIn());
            }
        }
    }

    private void PerformRandomAttack()
    {
        // 5가지 중 랜덤 선택
        int rand = Random.Range(0, 5);

        // 주변 플레이어 모두 찾기 (범위 15f 예시)
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Player"));

        foreach (var p in players)
        {
            // 각 플레이어에게 공격 발사
            switch (rand)
            {
                case 0:
                    PerformFireAttack(p.transform);
                    break;
                case 1:
                    PerformIceAttack(p.transform);
                    break;
                case 2:
                    PerformThunderAttack(p.transform);
                    break;
                case 3:
                    PerformPoisonAttack(p.transform);
                    break;
                case 4:
                    PerformWaterAttack(p.transform);
                    break;
            }
        }
    }

    private void PerformFireAttack(Transform target)
    {
        if (fireProjectilePrefab == null || target == null) return;

        GameObject mortar = Instantiate(fireProjectilePrefab, transform.position, Quaternion.identity);
        FireMortarProjectile projectile = mortar.GetComponent<FireMortarProjectile>();
        if (projectile != null)
        {
            projectile.Setup(
                target.position,
                fireDamage,
                mortarSpeed,
                fireFieldDuration,
                fieldMaxScale,
                fieldExpandSpeed,
                fieldDamageInterval,
                fieldDamage
            );
        }
    }

    private void PerformIceAttack(Transform target)
    {
        if (iceClusterPrefab == null || target == null) return;

        GameObject clusterObj = Instantiate(iceClusterPrefab, target.position, Quaternion.identity);
        IceClusterController cluster = clusterObj.GetComponent<IceClusterController>();
        if (cluster != null)
        {
            cluster.Setup(
                iceWarningDuration,
                bodyDuration,
                iceContactDamage,
                shardDamage,
                shardSpeed,
                shardCount,
                shardLifetime
            );
        }
    }

    private void PerformThunderAttack(Transform target)
    {
        if (thunderPatternPrefab == null || target == null) return;

        StartCoroutine(SpawnThunderBurstAtTarget(target));
    }

    private IEnumerator SpawnThunderBurstAtTarget(Transform target)
    {
        for (int i = 0; i < burstCount; i++)
        {
            Vector3 targetPos = target.position;
            GameObject patternObj = Instantiate(thunderPatternPrefab, targetPos, Quaternion.identity);
            ThunderPatternController pattern = patternObj.GetComponent<ThunderPatternController>();
            if (pattern != null)
            {
                pattern.Setup(
                    thunderWarningDuration,
                    strikeDamage,
                    strikeRadius,
                    strikeVfxDuration,
                    thunderFieldDuration,
                    fieldDamagePerTick
                );
            }

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }
    }

    private void PerformPoisonAttack(Transform target)
    {
        if (poisonSporePrefab == null || target == null) return;

        GameObject sporeObj = Instantiate(poisonSporePrefab, transform.position, Quaternion.identity);
        PoisonSporeController spore = sporeObj.GetComponent<PoisonSporeController>();
        if (spore != null)
        {
            spore.Setup(target, sporeSpeed, explosionDamage, sporeLife, explosionRadius);
        }
    }

    private void PerformWaterAttack(Transform target)
    {
        if (waterWavePrefab == null || target == null) return;

        Vector3 targetDir = (target.position - transform.position).normalized;
        GameObject waveObj = Instantiate(waterWavePrefab, transform.position, Quaternion.identity);
        WaterWaveController wave = waveObj.GetComponent<WaterWaveController>();
        if (wave != null)
        {
            wave.Setup(
                targetDir,
                waveSpeed,
                waveDamage,
                waveLifetime,
                startScaleMultiplier,
                targetScaleMultiplier
            );
        }
    }



    // --- 페이즈 전환 함수 (매니저가 호출) ---

    public void SetRageMode(bool state)
    {
        if (state)
        {
            CurrentPhase = BossPhase.Rage;
            isInvincible = true;
            RageShield.SetActive(true);
            // 스프라이트 붉게 변하기 등 연출
        }
    }

    public void SetGroggyMode()
    {
        StartCoroutine(GroggyRoutine());
    }

    IEnumerator GroggyRoutine()
    {
        CurrentPhase = BossPhase.Groggy;
        isInvincible = false; // 무적 해제

        // 보스 멈춤, 애니메이션 변경 등
        Debug.Log("보스 그로기 상태! 극딜 타이밍!");
        RageShield.SetActive(false);

        yield return new WaitForSeconds(groggyDuration);

        // 그로기 종료 -> 통상 모드 복귀
        CurrentPhase = BossPhase.Normal;
        isInvincible = false;
        Debug.Log("보스 그로기 종료, 통상 모드 복귀.");

        // 레벨 매니저 리셋 요청
        BossDungeonLevelManager.instance.ResetBossLevelStack();
    }
}