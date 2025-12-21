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
    
    private PlayerStats playerStats; // PlayerStats 참조
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerStats = GetComponent<PlayerStats>(); // PlayerStats 참조 가져오기
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
        }
            
    }

     void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnMove -= HandleMove;
        }
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
