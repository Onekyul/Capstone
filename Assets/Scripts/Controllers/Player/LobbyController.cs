using Fusion;
using UnityEngine;

public class LobbyController : NetworkBehaviour
{
    [SerializeField] private Transform unitRoot;
    private Animator _animator;

    [Networked, OnChangedRender(nameof(ApplyFlip))]
    private NetworkBool IsFacingLeft { get; set; }

    [Header("Settings")]
    public float moveSpeed = 5f;

    public override void Spawned()
    {
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

    public override void Render()
    {
        ApplyFlip();
    }

    private void ApplyFlip()
    {
        Transform target = unitRoot != null ? unitRoot : transform;
        Vector3 scale = target.localScale;
        scale.x = IsFacingLeft ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        target.localScale = scale;
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

            // 좌우 반전 (networked)
            if (inputDir.x != 0)
            {
                IsFacingLeft = inputDir.x < 0;
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
