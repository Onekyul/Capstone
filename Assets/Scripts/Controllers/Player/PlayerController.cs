using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance; // 싱글톤
    
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
        // 싱글톤 패턴: 중복 생성 방지
        if (instance != null && instance != this)
        {
            Debug.Log("PlayerController: 중복된 플레이어 오브젝트 파괴");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 플레이어 유지
        Debug.Log("PlayerController: DontDestroyOnLoad 적용 완료");
        
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
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // 씬 로드 시 플레이어 위치 재설정
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"PlayerController: 씬 로드됨 - {scene.name}");
        
        // 플레이어 스폰 지점 찾기
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
            Debug.Log($"PlayerController: 스폰 지점으로 이동 - {spawnPoint.transform.position}");
        }
        else
        {
            Debug.LogWarning($"PlayerController: PlayerSpawnPoint를 찾을 수 없습니다. (씬: {scene.name})");
        }
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
