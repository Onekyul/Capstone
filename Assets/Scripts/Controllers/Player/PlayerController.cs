using UnityEngine;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField]
    private float playerSpeed=3.5f;
    private Rigidbody2D rb;
    private Vector2 movementInput;  // InputManager 로부터 전달받은 현재 이동 입력 값을 저장
    
    [Header("Attack System")]
    [SerializeField] private AttackManager attackManager;
    
    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer spriteRenderer; // 스프라이트 렌더러 참조
    
    private PlayerStats playerStats; // PlayerStats 참조
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerStats = GetComponent<PlayerStats>(); // PlayerStats 참조 가져오기
        
        // SpriteRenderer 자동 할당
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning("PlayerController: SpriteRenderer를 찾을 수 없습니다!");
            }
        }
    }

     void Start()
    {
        if (attackManager != null)
            attackManager.StartAutoAttack();
        
        
    }

     void OnEnable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnMove += HandleMove;
            InputManager.instance.OnLook += HandleLook; // 마우스 방향 이벤트 구독
        }
            
    }

     void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnMove -= HandleMove;
            InputManager.instance.OnLook -= HandleLook; // 마우스 방향 이벤트 구독 해제
        }
    }

     void HandleMove(Vector2 move)
    {
        movementInput = move;
    }
    
    void HandleLook(Vector2 lookDirection)
    {
        // 마우스 방향에 따라 스프라이트 좌우 반전
        if (spriteRenderer != null && lookDirection.sqrMagnitude > 0.0001f)
        {
            // 마우스가 왼쪽에 있으면 flipX = true, 오른쪽에 있으면 flipX = false
            spriteRenderer.flipX = lookDirection.x < 0;
        }
    }
     

    void FixedUpdate()
    {
        //대각선 가속화 해결
        if (movementInput.sqrMagnitude > 0)
        {
            Vector2 moveDirection = movementInput.normalized;
            
            // PlayerStats의 이동속도 배율 적용
            float finalSpeed = playerSpeed;
            if (playerStats != null)
            {
                finalSpeed *= playerStats.GetMoveSpeedMultiplier();
            }
            
            rb.linearVelocity = moveDirection * finalSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

   


}
