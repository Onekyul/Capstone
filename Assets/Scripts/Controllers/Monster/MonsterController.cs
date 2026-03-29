using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI; // ★ UI 기능을 쓰려면 이게 꼭 필요합니다!

// 몬스터 타입 enum
public enum MonsterType
{
    Normal,     // 일반 몬스터
    Elite,      // 엘리트 몬스터 (은상자 드랍)
    Element ,   // 속성 몬스터 (금상자 드랍)
    Boss    // 보스 몬스터
}

public class MonsterController : MonoBehaviour
{
    [Header("Monster Type")]
    [SerializeField] public MonsterType monsterType = MonsterType.Normal; // 몬스터 타입
    public MonsterType GetMonsterType() => monsterType; // 외부에서 타입 확인용

    [Header("Basic Stats")]
    [SerializeField] protected float contactDamage = 5.0f;
    public float normalDamage => contactDamage;

    [SerializeField] protected float MaxHP = 100;
    [SerializeField] protected float CurHP;

    [SerializeField] protected float moveSpeed = 3.0f; // 기본 이동 속도 (0이면 움직이지 않음)
    protected float baseMoveSpeed; // 기본/엘리트 보너스가 적용된 실제 기본 속도
    protected float currentMoveSpeed; // 상태 이상 효과가 적용된 현재 이동 속도

    [Header("Pool & Drop")]
    [SerializeField] private string poolTag;
    [SerializeField] private GameObject expJewelPrefab;

    [Header("Pool Settings")]
    [Tooltip("체크하면 풀링 매니저로 반납하고, 체크 해제하면 그냥 Destroy 됩니다.")]
    [SerializeField] protected bool usePooling = true; // 기본값은 true 

    [Header("UI")]
    [SerializeField] protected Slider hpSlider; // 체력바 슬라이더 연결용 변수


    [Header("Lightning Enchantment")]
    [SerializeField] private GameObject lightningFieldPrefab; // 번개 필드 프리팹

    [Header("Visual Effects")]
    [SerializeField] private GameObject burnEffectObject;   // 화상 이펙트 (자식 오브젝트 연결)
    [SerializeField] private GameObject poisonEffectObject; // 독 이펙트 (자식 오브젝트 연결)
    // 얼음 이펙트는 색깔로 처리하므로 필요 없음 (또는 얼음 조각 이펙트 추가 가능)

    private float lightningRadius = 5.0f; // 번개 범위


    // 몬스터가 죽을 때 발동할 이벤트
    public event Action OnDeath;

    protected Transform player;
    protected SpriteRenderer spriteRenderer;

    // --- 상태이상 코루틴 ---
    private Coroutine burnCoroutine;
    private Coroutine iceCoroutine;
    private Coroutine poisonCoroutine;

    // --- 상태 플래그 ---
    private bool isSlowed = false;
    protected bool isFrozen = false;
    private float damageMultiplier = 1.0f; // 썩음 배율

    // 최근에 맞은 데미지 (상태이상 데미지 계산용)
    public float storedLastDamage = 0f; // 전염을 위해 public으로 변경

    // 1. 원본 스탯을 저장할 변수 추가
    protected float defaultMaxHP;
    protected float defaultContactDamage;
    protected float defaultMoveSpeed;
    protected Vector3 defaultScale; // 1. 원본 크기 저장 변수 추가

    protected virtual void Awake()
    {
        // 2. 게임 시작 시 Inspector에 설정된 초기 값을 저장
        // 오타 수정: defaultContactDamage = contactDamage;
        defaultMaxHP = MaxHP;
        defaultContactDamage = contactDamage;
        defaultMoveSpeed = moveSpeed;
        defaultScale = transform.localScale; // 2. 에디터에 설정한 원래 크기를 기억함
    }

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetClosestPlayer();
    }

    protected virtual void OnEnable()
    {
        OnDeath = null; // 풀 재사용 시 이전 구독자 누적 방지
        ResetStatus();
    }

    private void ResetStatus()
    {
        // 3. 누적된 스탯을 원본 값으로 리셋
        MaxHP = defaultMaxHP;
        contactDamage = defaultContactDamage;
        moveSpeed = defaultMoveSpeed;
        monsterType = MonsterType.Normal; // 타입 리셋 추가
        // 4. Vector3.one 대신 기억해둔 원본 크기로 복구
        transform.localScale = defaultScale;

        CurHP = MaxHP;

        if (hpSlider != null)
        {
            hpSlider.value = 1.0f;
            hpSlider.gameObject.SetActive(true);
        }

        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = moveSpeed;
        damageMultiplier = 1.0f;
        storedLastDamage = 0f;
        isSlowed = false;
        isFrozen = false;

        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        if (iceCoroutine != null) StopCoroutine(iceCoroutine);
        if (poisonCoroutine != null) StopCoroutine(poisonCoroutine);

        // ★ 이펙트 끄기 (초기화)
        if (burnEffectObject != null) burnEffectObject.SetActive(false);
        if (poisonEffectObject != null) poisonEffectObject.SetActive(false);

        UpdateColor(); // 색상 초기화
    }

    protected virtual void Update()
    {
        // 죽음 체크는 빙결 상태와 관계없이 항상 수행

        if (IsDead())
        {
            ReturnToPool();
            return;
        }

        // 빙결 상태면 이동하지 않음
        if (isFrozen) return;

        player = GetClosestPlayer();
        if (player == null) return;

        FlipSpriteTowardsPlayer();
        Move();
    }

    protected virtual void Move()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.position, currentMoveSpeed * Time.deltaTime);
    }


    // ★ 상태에 따라 색상을 결정하는 함수 (우선순위 로직)
    private void UpdateColor()
    {
        if (spriteRenderer == null) return;

        // 1순위: 빙결 (진한 파랑)
        if (isFrozen)
        {
            spriteRenderer.color = new Color(0.3f, 0.3f, 1f);
        }
        // 2순위: 감속 (하늘색)
        else if (isSlowed)
        {
            spriteRenderer.color = Color.cyan;
        }
        // 3순위: 일반 (흰색)
        else
        {
            spriteRenderer.color = Color.white;
        }

        // *참고: 독(보라색)이나 화상(빨간색)은 이제 색깔을 바꾸지 않습니다. 이펙트로 보여줍니다.
    }

    // ====================================================================
    // 1. 기본 피격 (공격력 스냅샷 저장)
    // ====================================================================
    public virtual void TakeDamage(float damage)
    {
        storedLastDamage = damage; // ★ 데미지를 먼저 저장 (상태이상 계산 기준값)

        float finalDamage = damage * damageMultiplier; // 썩음 적용
        CurHP -= finalDamage;

        // ★ [추가] 데미지 텍스트 띄우기
        // 몬스터 머리 위(Y축 + 0.5 ~ 1.0)에 띄우면 보기 좋습니다.
        Vector3 popupPos = transform.position + new Vector3(0, 0.5f, 0);

        // 매니저가 있는지 확인하고 호출
        if (DamageTextManager.Instance != null)
        {
            // 크리티컬 여부는 일단 false로, 나중에 확률 로직 넣으시면 true로 바꾸세요
            DamageTextManager.Instance.CreatePopup(popupPos, finalDamage, false);
        }
        // 체력바 갱신 로직
        if (hpSlider != null)
        {
            // 현재 체력 비율 계산 (0.0 ~ 1.0)
            hpSlider.value = CurHP / MaxHP;
        }

        //StartCoroutine(FlashColor(Color.red, 0.1f));
    }

    public void TakeDirectDamage(float damage)
    {
        float finalDamage = damage * damageMultiplier;
        CurHP -= finalDamage;

        // ★ [추가] 여기도 똑같이 추가 (화상, 독 데미지 등)
        if (DamageTextManager.Instance != null)
        {
            Vector3 popupPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.5f, 0); // 위치 약간 랜덤하게
            DamageTextManager.Instance.CreatePopup(popupPos, finalDamage, false);
        }
        if (hpSlider != null)
        {
            // 현재 체력 비율 계산 (0.0 ~ 1.0)
            hpSlider.value = CurHP / MaxHP;
        }
        //StartCoroutine(FlashColor(Color.yellow, 0.1f));
    }


    public void TakeElement(int[] enchants)
    {
        // ★ 주의: TakeElement는 반드시 TakeDamage 이후에 호출되어야 합니다!
        // ★ storedLastDamage가 설정되어 있어야 화상/번개 계산이 정상 작동합니다.
        
        if (storedLastDamage <= 0)
        {
            Debug.LogWarning($"[{gameObject.name}] TakeElement가 TakeDamage 전에 호출되었거나 storedLastDamage가 0입니다!");
        }

        // [0] 화염 
        if (enchants.Length > 0 && enchants[0] > 0)
        {
            ApplyBurn(enchants[0]);
        }

        // [1] 얼음 
        if (enchants.Length > 1 && enchants[1] > 0)
        {
            ApplyIce(enchants[1]);
        }

        // [2] 번개 
        if (enchants.Length > 2 && enchants[2] > 0)
        {
            ApplyLightning(enchants[2]);
        }

        // [3] 독 
        if (enchants.Length > 3 && enchants[3] > 0)
        {
            ApplyPoison(enchants[3]);
        }

        // [4] 물 (피격 방어막) - 플레이어에게 방어막 부여
        if (enchants.Length > 4 && enchants[4] > 0)
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TriggerWaterEnchantShield(enchants[4]);
            }
        }
        
        // 전염 능력 체크 (5% 확률)
        if (PlayerStats.Instance != null && 
            PlayerStats.Instance.HasContagion() && 
            UnityEngine.Random.value <= PlayerStats.Instance.GetContagionChance())
        {
            SpreadEnchantment(enchants);
        }
    }


    // --- [0] 화염 ---
    private void ApplyBurn(int level)
    {
        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        burnCoroutine = StartCoroutine(BurnRoutine(level));
    }

    IEnumerator BurnRoutine(int level)
    {
        // ★ 화상 이펙트 켜기
        if (burnEffectObject != null) burnEffectObject.SetActive(true);

        int ticks = 6;
        float interval = 0.5f;
        // 화상 데미지: 본체 데미지의 10% * 레벨
        float tickDamage = storedLastDamage * (level * 0.1f);

        for (int i = 0; i < ticks; i++)
        {
            //화상 데미지 적용 확인 디버그 
            Debug.Log($"Burn Tick {i + 1}/{ticks}: Dealing {tickDamage} damage.");
            yield return new WaitForSeconds(interval);
            TakeDirectDamage(tickDamage);
        }

        // ★ 화상 이펙트 끄기
        if (burnEffectObject != null) burnEffectObject.SetActive(false);
        burnCoroutine = null;
    }

    // --- [1] 얼음 (감속 -> 빙결 로직) ---
    private void ApplyIce(int level)
    {
        // 이미 얼어있거나 느려져 있으면 -> 빙결(Freeze)
        if (isFrozen || isSlowed)
        {
            if (iceCoroutine != null) StopCoroutine(iceCoroutine);
            iceCoroutine = StartCoroutine(FreezeRoutine(level));
        }
        // 멀쩡하면 -> 감속(Slow)
        else
        {
            if (iceCoroutine != null) StopCoroutine(iceCoroutine);
            iceCoroutine = StartCoroutine(SlowRoutine(level));
        }
    }

    IEnumerator SlowRoutine(int level)
    {
        isSlowed = true;
        isFrozen = false;

        float slowPercent = Mathf.Clamp(level * 0.15f, 0.1f, 0.9f);
        currentMoveSpeed = baseMoveSpeed * (1.0f - slowPercent);
        UpdateColor(); // ★ 색상 갱신 (Cyan)

        yield return new WaitForSeconds(3.0f);

        currentMoveSpeed = baseMoveSpeed;
        isSlowed = false;
        UpdateColor(); // ★ 색상 복구 (White or Frozen)
        iceCoroutine = null;
    }

    IEnumerator FreezeRoutine(int level)
    {
        isFrozen = true;
        isSlowed = false;
        currentMoveSpeed = 0f;
        UpdateColor(); // ★ 색상 갱신 (Blue)

        float duration = 1.5f + (level * 0.2f);
        yield return new WaitForSeconds(duration);

        isFrozen = false;
        currentMoveSpeed = baseMoveSpeed;
        UpdateColor(); // ★ 색상 복구 (White)
        iceCoroutine = null;
    }

    // --- [2] 번개 (Lightning) ---
    private void ApplyLightning(int level)
    {
        // 1. 즉발 데미지 계산 (본체 데미지의 50% * 레벨)
        float lightningDamage = storedLastDamage * (level * 0.5f);

        // 2. 주변 몬스터에게 즉발 데미지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, lightningRadius);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            // 몬스터 태그 확인
            if (hit.CompareTag("Enemy"))
            {
                // 자기 자신은 제외 
                if (hit.gameObject == this.gameObject) continue;

                MonsterController otherMonster = hit.GetComponent<MonsterController>();
                if (otherMonster != null)
                {
                    otherMonster.TakeDirectDamage(lightningDamage);
                    hitCount++;
                }
            }
        }

        Debug.Log($"[Lightning Enchant] 즉발 데미지: {lightningDamage}, {hitCount}마리 타격");

        // 3. 번개 필드 생성 (프리팹이 있는 경우)
        if (lightningFieldPrefab != null)
        {
            GameObject fieldObj = Instantiate(lightningFieldPrefab, transform.position, Quaternion.identity);
            LightningFieldController fieldController = fieldObj.GetComponent<LightningFieldController>();

            if (fieldController != null)
            {
                // LightningFieldController가 자동으로 플레이어 공격력 × 30%를 계산합니다
                Debug.Log($"[Lightning Enchant] 번개 필드 생성 - 플레이어 현재 공격력의 30%로 지속 데미지");
            }
        }
        else
        {
            Debug.LogWarning("[Lightning Enchant] lightningFieldPrefab이 할당되지 않았습니다!");
        }
    }

    // --- [3] 독 (Poison/Rot) ---
    private void ApplyPoison(int level)
    {
        if (poisonCoroutine != null) StopCoroutine(poisonCoroutine);
        poisonCoroutine = StartCoroutine(RotRoutine(level));
    }

    IEnumerator RotRoutine(int level)
    {
        // ★ 독 이펙트 켜기
        if (poisonEffectObject != null) poisonEffectObject.SetActive(true);

        // 받는 피해량 증폭
        damageMultiplier = 1.0f + (level * 0.2f);
        // 독은 색깔을 바꾸지 않으므로 UpdateColor 호출 안 함 (또는 약간의 틴트만 추가 가능)

        yield return new WaitForSeconds(3.0f);

        damageMultiplier = 1.0f;

        // ★ 독 이펙트 끄기
        if (poisonEffectObject != null) poisonEffectObject.SetActive(false);
        poisonCoroutine = null;
    }
    
    // --- [전염] 인챈트 효과 전염 ---
    private void SpreadEnchantment(int[] enchants)
    {
        // 1. 주변 몬스터 탐지 (반경 6 유닛)
        float spreadRadius = 6.0f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, spreadRadius);
        
        // 2. 유효한 몬스터만 필터링 (자신 제외, Enemy 태그)
        System.Collections.Generic.List<MonsterController> validTargets = new System.Collections.Generic.List<MonsterController>();
        foreach (var hit in hits)
        {
            // 자기 자신은 제외
            if (hit.gameObject == this.gameObject) continue;
            // Enemy 태그 확인
            if (!hit.CompareTag("Enemy")) continue;
            
            MonsterController monster = hit.GetComponent<MonsterController>();
            if (monster != null && !monster.IsDead())
            {
                validTargets.Add(monster);
            }
        }
        
        // 3. 랜덤으로 1명 선택
        if (validTargets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
            MonsterController target = validTargets[randomIndex];
            
            // 4. 같은 인챈트 효과 전염 (재귀 방지를 위해 직접 적용)
            ApplyEnchantmentDirectly(target, enchants);
            
            Debug.Log($"[전염] {gameObject.name}의 인챈트가 {target.gameObject.name}에게 전염됨! (화염:{enchants[0]}, 얼음:{enchants[1]}, 번개:{enchants[2]}, 독:{enchants[3]})");
        }
    }
    
    // 인챈트를 직접 적용 (전염 시 재귀 방지)
    private void ApplyEnchantmentDirectly(MonsterController target, int[] enchants)
    {
        if (target == null) return;
        
        // storedLastDamage를 타겟에게 전달해야 하므로 타겟의 storedLastDamage 설정
        target.storedLastDamage = this.storedLastDamage;
        
        // [0] 화염
        if (enchants.Length > 0 && enchants[0] > 0)
        {
            target.ApplyBurn(enchants[0]);
        }

        // [1] 얼음
        if (enchants.Length > 1 && enchants[1] > 0)
        {
            target.ApplyIce(enchants[1]);
        }

        // [2] 번개
        if (enchants.Length > 2 && enchants[2] > 0)
        {
            target.ApplyLightning(enchants[2]);
        }

        // [3] 독
        if (enchants.Length > 3 && enchants[3] > 0)
        {
            target.ApplyPoison(enchants[3]);
        }
    }


    // --- 유틸리티 ---
    protected bool IsDead() => CurHP <= 0;

    // 네트워크 래퍼에서 접근할 수 있도록 public getter 제공
    public float GetCurrentHP() => CurHP;
    public float GetMaxHP() => MaxHP;

    protected Transform GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return null;
        Transform closest = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;
        foreach (GameObject p in players)
        {
            if (!p.activeInHierarchy) continue;
            // 사망한 플레이어는 추격/공격 대상에서 제외
            var dungeonStats = p.GetComponent<DungeonPlayerStats>();
            if (dungeonStats != null && dungeonStats.Object != null && dungeonStats.Object.IsValid && dungeonStats.IsDead) continue;
            float sqr = (p.transform.position - myPos).sqrMagnitude;
            if (sqr < minSqrDist) { minSqrDist = sqr; closest = p.transform; }
        }
        return closest;
    }

    protected void FlipSpriteTowardsPlayer()
    {
        if (spriteRenderer != null && player != null)
            spriteRenderer.flipX = player.position.x > transform.position.x;
    }

    protected virtual void ReturnToPool()
    {
        // ★ 0-1. 모든 상태이상 코루틴 즉시 정지 (빙결 상태 유지를 위해)
        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        if (iceCoroutine != null) StopCoroutine(iceCoroutine);
        if (poisonCoroutine != null) StopCoroutine(poisonCoroutine);
        
        // 0-2. 빙결폭발 체크 (죽음 이벤트보다 먼저! isFrozen 상태가 초기화되기 전에)
        Debug.Log($"[ReturnToPool] {gameObject.name} 사망! isFrozen: {isFrozen}, PlayerStats: {(PlayerStats.Instance != null ? "존재" : "null")}");
        
        if (isFrozen && PlayerStats.Instance != null)
        {
            Debug.Log($"[ReturnToPool] 빙결 상태 확인! CheckFrozenExplosion 호출 시작");
            CheckFrozenExplosion();
        }
        else
        {
            Debug.Log($"[ReturnToPool] 빙결폭발 조건 불만족 - isFrozen: {isFrozen}, PlayerStats: {(PlayerStats.Instance != null)}");
        }
        
        // 1. 죽음 이벤트 알림 
        OnDeath?.Invoke();

        // 2. 흡혈 처리
        if (PlayerStats.Instance != null)
        {
            Debug.Log($"[몬스터 처치] {gameObject.name} 죽음, 마지막 데미지: {storedLastDamage:F1} → 플레이어 흡혈 체크 호출");
            PlayerStats.Instance.OnEnemyKilled(storedLastDamage);
        }
        else
        {
            Debug.LogWarning("[몬스터 처치] PlayerStats 인스턴스를 찾을 수 없음!");
        }

        // 3. 아이템 드랍
        if (expJewelPrefab != null)
        {
#if UNITY_SERVER
            // 보스 던전 데디서버: JewelSyncManager에 위치 등록 → 클라이언트에 보석 표시
            if (JewelSyncManager.Instance != null)
                JewelSyncManager.Instance.AddJewel(transform.position);
            else
                Instantiate(expJewelPrefab, transform.position, Quaternion.identity);
#else
            Instantiate(expJewelPrefab, transform.position, Quaternion.identity);
#endif
        }

        if (usePooling)
        {
            // 풀링을 사용하는 몬스터라면 -> 매니저에게 반납
            if (MonsterPool.Instance != null)
                MonsterPool.Instance.ReturnToPool(poolTag, this);
            else
                Destroy(gameObject); // 매니저 없으면 파괴
        }
        else
        {
            // 풀링을 안 쓰는 몬스터(속성 몬스터 등)라면 -> 그냥 파괴
            Destroy(gameObject);
        }
    }
    
    // 빙결폭발 체크 및 처리 (빙결 상태로 죽으면 주변에 폭발 데미지)
    private void CheckFrozenExplosion()
    {
        // PlayerStats에서 빙결폭발 능력 보유 여부 확인
        if (!PlayerStats.Instance.HasFrozenExplosion())
        {
            Debug.Log("[빙결폭발] 능력 미보유 - 폭발 발동 안함");
            return;
        }

        Debug.Log($"★★★ [빙결폭발] 빙결 상태 몬스터 사망! 폭발 발동! 위치: {transform.position} ★★★");

        // 폭발 범위 (5유닛)
        float explosionRadius = 5.0f;
        
        // 폭발 데미지 = 플레이어 현재 공격력 × 200%
        float explosionDamage = PlayerStats.Instance.GetTotalDamage() * 2.0f;
        
        Debug.Log($"[빙결폭발] 플레이어 공격력: {PlayerStats.Instance.GetTotalDamage():F1}, 폭발 데미지: {explosionDamage:F1}");

        // 주변 몬스터 탐지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        Debug.Log($"[빙결폭발] 폭발 범위 내 콜라이더 {hits.Length}개 감지");

        int hitCount = 0;
        foreach (var hit in hits)
        {
            Debug.Log($"[빙결폭발] 감지된 오브젝트: {hit.gameObject.name}, 태그: {hit.tag}");
            
            // 몬스터 태그 확인
            if (hit.CompareTag("Enemy"))
            {
                // 자기 자신은 제외 (이미 죽음)
                if (hit.gameObject == this.gameObject)
                {
                    Debug.Log($"[빙결폭발] 자기 자신 제외: {hit.gameObject.name}");
                    continue;
                }

                MonsterController otherMonster = hit.GetComponent<MonsterController>();
                if (otherMonster != null)
                {
                    float beforeHP = otherMonster.CurHP;
                    
                    // TakeDirectDamage로 데미지 적용
                    otherMonster.TakeDirectDamage(explosionDamage);
                    
                    float afterHP = otherMonster.CurHP;
                    Debug.Log($"[빙결폭발] {otherMonster.gameObject.name}에게 {explosionDamage:F1} 데미지! HP: {beforeHP:F1} → {afterHP:F1}");
                    
                    hitCount++;
                }
                else
                {
                    Debug.LogWarning($"[빙결폭발] MonsterController가 없음: {hit.gameObject.name}");
                }
            }
        }

        Debug.Log($"[빙결폭발] 최종 결과 - 폭발 데미지: {explosionDamage:F1}, {hitCount}마리 타격");
        
        // 폭발 범위 시각화 (Scene 뷰에서 확인 가능)
        StartCoroutine(DrawExplosionRadius(explosionRadius));
    }
    
    /// <summary>
    /// 폭발 범위 시각화 코루틴 (디버그용)
    /// </summary>
    private IEnumerator DrawExplosionRadius(float radius)
    {
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Gizmos는 OnDrawGizmos에서만 그릴 수 있으므로 여기서는 Debug.DrawLine 사용
            int segments = 32;
            float angle = 0f;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float currentAngle = angle * Mathf.Deg2Rad;
                float nextAngle = (angle + angleStep) * Mathf.Deg2Rad;
                
                Vector3 start = transform.position + new Vector3(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius, 0);
                Vector3 end = transform.position + new Vector3(Mathf.Cos(nextAngle) * radius, Mathf.Sin(nextAngle) * radius, 0);
                
                Debug.DrawLine(start, end, Color.cyan, duration);
                
                angle += angleStep;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public bool isGetFrozen()
    {
        return isFrozen;
    }

    // Scene 뷰에서 빙결폭발 범위 시각화
    private void OnDrawGizmos()
    {
        // 빙결 상태이고 빙결폭발 능력이 있을 때만 범위 표시
        if (isFrozen && PlayerStats.Instance != null && PlayerStats.Instance.HasFrozenExplosion())
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // 반투명 cyan
            Gizmos.DrawSphere(transform.position, 5.0f); // 폭발 범위
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 5.0f); // 폭발 범위 외곽선
        }
    }

    // IEnumerator FlashColor(Color color, float time)
    // {
    //     spriteRenderer.color = color;
    //     yield return new WaitForSeconds(time);

    //     // 상태이상 색상 복구 로직
    //     if (isFrozen) spriteRenderer.color = new Color(0.3f, 0.3f, 1f);
    //     else if (isSlowed) spriteRenderer.color = Color.cyan;
    //     else if (damageMultiplier > 1.0f) spriteRenderer.color = new Color(0.7f, 0f, 1f);
    //     else spriteRenderer.color = Color.white;
    // }
}