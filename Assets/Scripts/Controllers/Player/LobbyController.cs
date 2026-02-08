using Fusion;
using UnityEngine;

public class LobbyController : NetworkBehaviour
{
    private SpriteRenderer _renderer;
    private Animator _animator;

    [Header("Settings")]
    public float moveSpeed = 5f;

    public override void Spawned()
    {
        _renderer = GetComponent<SpriteRenderer>();
        //_animator = GetComponent<Animator>();
        
        if (HasStateAuthority)
        {
            if (InputManager.instance != null)
            {
                InputManager.instance.lookOrigin = this.transform;
            }
            
            // 2. 카메라도 나를 따라오게 설정 (카메라 스크립트가 있다면)
            // Camera.main.GetComponent<CameraFollow>().target = this.transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        
        if (InputManager.instance == null) return;
        
        if (ChatManager.IsChatting == true) return;
        Vector2 inputDir = InputManager.instance.playerMoveInput; 
        
        // 이동 로직
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // 이동
            transform.Translate(inputDir * moveSpeed * Runner.DeltaTime);

            // 좌우 반전 (Sprite Flip)
            if (inputDir.x != 0)
            {
                _renderer.flipX = (inputDir.x < 0);
            }

            // 애니메이션
            //if (_animator != null) _animator.SetBool("IsRun", true);
        }
        else
        {
          //  if (_animator != null) _animator.SetBool("IsRun", false);
        }
    }
}
