using Fusion;
using UnityEngine;

public class DungeonPlayerController : NetworkBehaviour
{
    [SerializeField] private float basePlayerSpeed = 5f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private DungeonPlayerStats stats; // 스탯 스크립트 가져오기

    [Networked] public NetworkBool IsFacingLeft { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<DungeonPlayerStats>();

        if (HasInputAuthority)
        {
            Camera.main.transform.SetParent(this.transform);
            Camera.main.transform.localPosition = new Vector3(0, 0, -10);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (stats != null && stats.IsDead)
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
        
        if (spriteRenderer != null && spriteRenderer.enabled)
        {
            spriteRenderer.flipX = IsFacingLeft;
        }
    }
}