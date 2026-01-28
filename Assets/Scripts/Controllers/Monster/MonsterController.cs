using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI; // ★ UI 기능을 쓰려면 이게 꼭 필요합니다!

public class MonsterController : MonoBehaviour
{
    [Header("Basic Stats")]
    [SerializeField] protected float contactDamage = 5.0f;
    public float normalDamage => contactDamage;

    [SerializeField] protected float MaxHP = 100;
    [SerializeField] protected float CurHP;

    [SerializeField] protected float defaultMoveSpeed = 3.0f;
    protected float currentMoveSpeed;

    [Header("Pool & Drop")]
    [SerializeField] private string poolTag;
    [SerializeField] private ExpJewelController expJewelPrefab;

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
    private bool isFrozen = false;
    private float damageMultiplier = 1.0f; // 썩음 배율

    // 최근에 맞은 데미지 (상태이상 데미지 계산용)
    private float storedLastDamage = 0f;

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetClosestPlayer();
        ResetStatus();
    }

    protected virtual void OnEnable()
    {
        ResetStatus();
    }

    private void ResetStatus()
    {
        CurHP = MaxHP;
        // 체력바 초기화 
        if (hpSlider != null)
        {
            hpSlider.value = 1.0f;
            hpSlider.gameObject.SetActive(true);
        }
        currentMoveSpeed = defaultMoveSpeed;
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
        if (isFrozen) return;

        player = GetClosestPlayer();
        if (player == null) return;

        FlipSpriteTowardsPlayer();
        transform.position = Vector2.MoveTowards(transform.position, player.position, currentMoveSpeed * Time.deltaTime);

        if (IsDead()) ReturnToPool();
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
    public void TakeDamage(float damage)
    {
        storedLastDamage = damage; // 데미지 저장

        float finalDamage = damage * damageMultiplier; // 썩음 적용
        CurHP -= finalDamage;
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
        if (hpSlider != null)
        {
            // 현재 체력 비율 계산 (0.0 ~ 1.0)
            hpSlider.value = CurHP / MaxHP;
        }
        //StartCoroutine(FlashColor(Color.yellow, 0.1f));
    }


    public void TakeElement(int[] enchants)
    {
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
        currentMoveSpeed = defaultMoveSpeed * (1.0f - slowPercent);
        UpdateColor(); // ★ 색상 갱신 (Cyan)

        yield return new WaitForSeconds(3.0f);

        currentMoveSpeed = defaultMoveSpeed;
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
        currentMoveSpeed = defaultMoveSpeed;
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


    // --- 유틸리티 ---
    protected bool IsDead() => CurHP <= 0;

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
            Instantiate(expJewelPrefab, transform.position, Quaternion.identity);
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

    public bool isGetFrozen()
    {
        return isFrozen;
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