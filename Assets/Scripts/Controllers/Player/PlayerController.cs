using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance; // 싱글톤
    
    [SerializeField]
    private float playerSpeed=2.5f;
    private Rigidbody2D rb;
    public Vector2 movementInput;  // InputManager 로부터 전달받은 현재 이동 입력 값을 저장
    
    [Header("Attack System")]
    [SerializeField] private AttackManager attackManager;
    
    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer spriteRenderer; // 스프라이트 렌더러 참조
    
    private PlayerStats playerStats; // PlayerStats 참조
    

    void Awake()
    {
        // 싱글톤 패턴: 중복 생성 방지
        if (instance != null && instance != this)
        {
            Debug.Log("PlayerController: 중복된 플레이어 오브젝트 파괴");
            Destroy(gameObject);
            return;
        }

        instance = this;
        Debug.Log("PlayerController: 초기화 완료");

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
        Debug.Log($"PlayerController: Start 완료 (씬: {SceneManager.GetActiveScene().name})");
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
    
    // void HandleLook(Vector2 lookDirection)
    // {
    //     // 마우스 방향에 따라 스프라이트 좌우 반전
    //     if (spriteRenderer != null && lookDirection.sqrMagnitude > 0.0001f)
    //     {
    //         spriteRenderer.flipX = lookDirection.x < 0;
    //     }
    // }
    void HandleLook(Vector2 lookDirection)
{
    // 마우스 방향에 따라 캐릭터 전체를 좌우 반전
    if (lookDirection.sqrMagnitude > 0.0001f)
    {
        // 현재 스케일 값을 가져옵니다.
        Vector3 currentScale = transform.localScale;

        if (lookDirection.x > 0)
        {
            // 왼쪽을 볼 때: x 스케일을 음수로 (캐릭터가 왼쪽을 보게 됨)
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else if (lookDirection.x < 0)
        {
            // 오른쪽을 볼 때: x 스케일을 양수로 (캐릭터가 오른쪽을 보게 됨)
            currentScale.x = Mathf.Abs(currentScale.x);
        }

        // 변경된 스케일 적용
        transform.localScale = currentScale;
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
