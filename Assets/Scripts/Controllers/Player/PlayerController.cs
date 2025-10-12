using UnityEngine;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField]
    private float playerSpeed=3.5f;
    private Rigidbody2D rb;
    private Vector2 movementInput;  // InputManager 로부터 전달받은 현재 이동 입력 값을 저장

    // 체력 관련 변수들
    [Header("Health System")]
    [SerializeField] private float playerMaxHP = 100f; //플레이어 최대체력 변수
    [SerializeField] private float playerCurHP;         //플레이어 현재체력 변수
    [SerializeField] private bool bIsInvulnerable = false; //충돌 후 무적 상태 여부 확인
    [SerializeField] private float invulnerabilityDuration = 1f; //충돌 후 무적 상태 지속 시간
    
    // 체력 변화 이벤트
    public event Action<float> OnHealthChanged; 
    //플레이어 사망 이벤트
    public event Action OnPlayerDied;
    

    [Header("Attack System")]
    [SerializeField] private AttackManager attackManager;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

     void Start()
    {
        playerCurHP = playerMaxHP;  // 게임 시작 시 체력을 최대치로 설정
        OnHealthChanged?.Invoke(playerCurHP);  // 초기 체력 이벤트 발생
        if (attackManager != null)
            attackManager.StartAutoAttack();
    }

     void OnEnable()
    {
        if (InputManager.instance != null)
            InputManager.instance.OnMove += HandleMove;
    }

     void OnDisable()
    {
        if (InputManager.instance != null)
            InputManager.instance.OnMove -= HandleMove;
    }

     void HandleMove(Vector2 move)
    {
        movementInput = move;
    }

    void FixedUpdate()
    {
        //대각선 가속화 해결
        if (movementInput.sqrMagnitude > 0)
        {
            Vector2 moveDirection = movementInput.normalized;
            rb.linearVelocity = moveDirection * playerSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void TakeDamage(float damage) // 데미지 받는 함수
    {
        if (bIsInvulnerable || playerCurHP <= 0) return;
    
        playerCurHP -= damage;
        playerCurHP = Mathf.Max(0, playerCurHP);
    
        OnHealthChanged?.Invoke(playerCurHP);
    
        if (playerCurHP <= 0)
        {
            OnPlayerDied?.Invoke();
            Destroy(gameObject);
        // 플레이어 사망 처리
        }
        else
        {
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }


    private IEnumerator InvulnerabilityCoroutine()
    {
        bIsInvulnerable = true;                                     // 무적 상태로 설정
        yield return new WaitForSeconds(invulnerabilityDuration); // 무적 시간만큼 대기
        bIsInvulnerable = false;                                    // 무적 상태 해제
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Enemy 스크립트에서 데미지 값을 가져오거나 기본값 사용
            float damage = other.GetComponent<MonsterController>()?.normalDamage ?? 10f;
            TakeDamage(damage);
        }
    }


}
