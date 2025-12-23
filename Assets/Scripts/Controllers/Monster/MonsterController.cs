using System.Collections;
using UnityEngine;
using System;

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
    [SerializeField] protected bool usePooling = true; // 기본값은 true (일반 몬스터용)

    private float lightningRadius = 5.0f; // 번개 범위

    // ★ [추가] 몬스터가 죽을 때 발동할 이벤트
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
        currentMoveSpeed = defaultMoveSpeed;
        damageMultiplier = 1.0f;
        storedLastDamage = 0f;
        isSlowed = false;
        isFrozen = false;

        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        if (iceCoroutine != null) StopCoroutine(iceCoroutine);
        if (poisonCoroutine != null) StopCoroutine(poisonCoroutine);

        if(spriteRenderer != null) spriteRenderer.color = Color.white;
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

    // ====================================================================
    // 1. 기본 피격 (공격력 스냅샷 저장)
    // ====================================================================
    public void TakeDamage(float damage)
    {
        storedLastDamage = damage; // 데미지 저장

        float finalDamage = damage * damageMultiplier; // 썩음 적용
        CurHP -= finalDamage;
        
        //StartCoroutine(FlashColor(Color.red, 0.1f));
    }

    // 상태이상이나 번개로 인한 추가 데미지 (저장된 공격력 갱신 X)
    public void TakeDirectDamage(float damage)
    {
        float finalDamage = damage * damageMultiplier; // 번개 데미지도 썩음 증폭을 받을지 선택 (일단 받게 설정)
        CurHP -= finalDamage;
        //StartCoroutine(FlashColor(Color.yellow, 0.1f));
    }


    // ====================================================================
    // 2. 상태이상 적용 (순서: 화염, 얼음, 번개, 독)
    // ====================================================================
    public void TakeElement(int[] enchants)
    {
        // [0] 화염 (Fire)
        if (enchants.Length > 0 && enchants[0] > 0)
        {
            ApplyBurn(enchants[0]);
        }

        // [1] 얼음 (Ice: 감속/빙결 통합)
        if (enchants.Length > 1 && enchants[1] > 0)
        {
            ApplyIce(enchants[1]);
        }

        // [2] 번개 (Lightning) - 새로 추가됨!
        if (enchants.Length > 2 && enchants[2] > 0)
        {
            ApplyLightning(enchants[2]);
        }

        // [3] 독 (Poison) - 인덱스 밀림
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
        int ticks = 6; 
        float interval = 0.5f;
        // 화상 데미지: 본체 데미지의 10% * 레벨
        float tickDamage = storedLastDamage * (level * 0.1f);

        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(interval);
            TakeDirectDamage(tickDamage);
        }
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
        spriteRenderer.color = Color.cyan;

        yield return new WaitForSeconds(3.0f);

        currentMoveSpeed = defaultMoveSpeed;
        isSlowed = false;
        spriteRenderer.color = Color.white;
        iceCoroutine = null;
    }

    IEnumerator FreezeRoutine(int level)
    {
        isFrozen = true;
        isSlowed = false; 
        currentMoveSpeed = 0f;
        spriteRenderer.color = new Color(0.3f, 0.3f, 1f); 

        float duration = 1.5f + (level * 0.2f);
        yield return new WaitForSeconds(duration);

        isFrozen = false;
        currentMoveSpeed = defaultMoveSpeed;
        spriteRenderer.color = Color.white;
        iceCoroutine = null;
    }

    // --- [2] 번개 (Lightning) ---
    private void ApplyLightning(int level)
    {
        // 1. 데미지 계산 (예: 본체 데미지의 50% * 레벨)
        float lightningDamage = storedLastDamage * (level * 0.5f);

        // 2. 주변 몬스터 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, lightningRadius);

        foreach (var hit in hits)
        {
            // 몬스터 태그 확인
            if (hit.CompareTag("Monster"))
            {
                // 자기 자신은 제외 (이미 맞았으니까)
                if (hit.gameObject == this.gameObject) continue;

                MonsterController otherMonster = hit.GetComponent<MonsterController>();
                if (otherMonster != null)
                {
                    // 주변 몬스터에게 '직접 데미지'만 줍니다.
                    // (TakeElement를 또 부르면 번개가 무한 전이될 수 있으므로 주의)
                    otherMonster.TakeDirectDamage(lightningDamage);
                }
            }
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
        // 받는 피해량 증폭
        damageMultiplier = 1.0f + (level * 0.2f);
        spriteRenderer.color = new Color(0.7f, 0f, 1f); 

        yield return new WaitForSeconds(3.0f);

        damageMultiplier = 1.0f;
        spriteRenderer.color = Color.white;
        poisonCoroutine = null;
    }


    // --- 유틸리티 ---
    private bool IsDead() => CurHP <= 0;

    protected Transform GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return null;
        Transform closest = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;
        foreach (GameObject p in players) {
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
        // 1. 죽음 이벤트 알림 (스테이지 매니저에게 보고)
        OnDeath?.Invoke();

        // 2. 아이템 드랍
        if (expJewelPrefab != null)
        {
            Instantiate(expJewelPrefab, transform.position, Quaternion.identity);
        }

        // 3. ★ 여기가 수정된 핵심 로직입니다 ★
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