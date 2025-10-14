using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance; //정적 변수 선언
    
    public Vector2 playerMoveInput { get; private set; }
    public Vector2 LookDir{get; private set;} = Vector2.right;

   // 이벤트 선언
    public event Action OnInteractPressed;
    public event Action<Vector2> OnMove; //
    public event Action<Vector2> OnLook; //플레이어가 바라보는 방향 저장


    [Header("Aim Origin")]
    public Transform lookOrigin;
    

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        Vector2 newMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // WASD 이동 감지

        if (newMove != playerMoveInput)    // 입력 값이 달라질 때만 OnMove 이벤트 발생 시켜서 효율성 증가                    
        {
            playerMoveInput = newMove;                         
            OnMove?.Invoke(playerMoveInput);                  
        }

        if (Input.GetKeyDown(KeyCode.E)) // E키 눌렀을 때 
        {
            OnInteractPressed?.Invoke();
        }
        

        if (Camera.main != null && lookOrigin != null) // 캐릭터가 마우스 커서를 바라보게 하는 방향 계산산       
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
            mouseWorld.z = 0f;                                 

            Vector2 dir = (Vector2)mouseWorld - (Vector2)lookOrigin.position; 
            if (dir.sqrMagnitude > 0.0001f)                    
            {
                dir.Normalize();                               
                if (dir != LookDir)                            
                {
                    LookDir = dir;                             
                    OnLook?.Invoke(LookDir);                   
                }
            }
        }

    }
}
