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
                // 자기 자신은 제외 
                if (hit.gameObject == this.gameObject) continue;

                MonsterController otherMonster = hit.GetComponent<MonsterController>();
                if (otherMonster != null)
                {
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
    protected bool IsDead() => CurHP <= 0;

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