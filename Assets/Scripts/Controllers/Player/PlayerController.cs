using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance; // 싱글톤
    
    [SerializeField]
    private float playerSpeed = 2.5f;
    private Rigidbody2D rb;
    public Vector2 movementInput;  // InputManager 로부터 전달받은 현재 이동 입력 값을 저장
    
    [Header("Attack System")]
    [SerializeField] private AttackManager attackManager;
    
    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("UI")]
    [SerializeField] private Transform healthBarTransform;
    
    [Header("Animation")]
    [SerializeField] private Animator animator; // 애니메이터 참조 추가
    // 만약 특정 씬(거점)에서만 작동하게 하려면 씬 이름을 지정할 수 있습니다.
    [SerializeField] private string baseSceneName = "BaseArea 1"; 

    private PlayerStats playerStats; 

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("PlayerController: 중복된 플레이어 오브젝트 파괴");
            Destroy(gameObject);
            return;
        }

        instance = this;
        Debug.Log("PlayerController: 초기화 완료");

        rb = GetComponent<Rigidbody2D>();
        playerStats = GetComponent<PlayerStats>(); 
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Animator 자동 할당
        if (animator == null)
        {
            animator = GetComponent<Animator>();
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
            InputManager.instance.OnLook += HandleLook; 
        }
    }

    void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnMove -= HandleMove;
            InputManager.instance.OnLook -= HandleLook; 
        }
    }

    void HandleMove(Vector2 move)
    {
        movementInput = move;
    }
    
    void HandleLook(Vector2 lookDirection)
    {
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 currentScale = transform.localScale;

            if (lookDirection.x > 0)
            {
                currentScale.x = -Mathf.Abs(currentScale.x);
            }
            else if (lookDirection.x < 0)
            {
                currentScale.x = Mathf.Abs(currentScale.x);
            }

            // 플레이어 좌우 반전
            transform.localScale = currentScale;

            // ★ 추가된 부분: 체력바가 같이 뒤집히는 것을 방지
            if (healthBarTransform != null)
            {
                Vector3 hpScale = healthBarTransform.localScale;
                hpScale.x = (currentScale.x < 0) ? -Mathf.Abs(hpScale.x) : Mathf.Abs(hpScale.x);
                healthBarTransform.localScale = hpScale;
            }
        }
    }

    // 애니메이션 처리는 보통 Update에서 수행하여 프레임 지연을 막습니다.
    void Update()
    {
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (movementInput.sqrMagnitude > 0)
        {
            Vector2 moveDirection = movementInput.normalized;
            
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

    // 애니메이션 상태를 업데이트하는 함수
    private void UpdateAnimation()
    {
        if (animator == null) return;

        // 현재 씬이 거점(baseSceneName)일 때만 이동/대기 애니메이션 작동
        if (SceneManager.GetActiveScene().name == baseSceneName)
        {
            bool isMoving = movementInput.sqrMagnitude > 0;
            animator.SetBool("1_Move", isMoving);
        }
        else
        {
            // 전투 지역(던전)에서는 이동 애니메이션 파라미터를 강제로 끕니다.
            // 이렇게 하면 이동 키를 눌러도 걷는 모션으로 강제 전환되지 않습니다.
            animator.SetBool("1_Move", false);
        }
    }
}