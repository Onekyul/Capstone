using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class DungeonPlayerController : NetworkBehaviour
{
    [SerializeField] private float basePlayerSpeed = 5f;
    [SerializeField] private Transform unitRoot;
    private Rigidbody2D rb;
    private DungeonPlayerStats stats; // 스탯 스크립트 가져오기

    [Networked, OnChangedRender(nameof(ApplyFlip))]
    public NetworkBool IsFacingLeft { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
stats = GetComponent<DungeonPlayerStats>();

        if (HasInputAuthority)
        {
            var vcam = FindObjectOfType<CinemachineCamera>();
            if (vcam != null) vcam.Follow = this.transform;

            if (InputManager.instance != null)
                InputManager.instance.lookOrigin = this.transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (stats != null && (stats.IsDead || stats.IsFrozen))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            if (data.direction.sqrMagnitude > 0)
            {
                float finalSpeed = basePlayerSpeed * (stats != null ? stats.MoveSpeedMultiplier : 1.0f);
                rb.linearVelocity = data.direction.normalized * finalSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (data.lookDirection.sqrMagnitude > 0.0001f)
            {
                IsFacingLeft = data.lookDirection.x < 0;
            }
        }
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
}