using UnityEditor.EditorTools;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField] protected float contactDamage = 5.0f;
    public float normalDamage => contactDamage;

    protected Transform player;
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    protected float MaxHP;
    [SerializeField]
    protected float CurHP;

    // 이 몬스터 프리팹이 어떤 태그의 풀에 속하는지 인스펙터에서 설정
    [Tooltip("MonsterPool에서 사용하는 태그와 반드시 일치해야 합니다.")]
    [SerializeField] private string poolTag;

    [Tooltip("몬스터가 죽을 때 생성되는 경험치 보석 프리팹")]
    [SerializeField] private ExpJewelController expJewelPrefab;

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetClosestPlayer();
        CurHP = MaxHP;
    }

    protected virtual void Update()
    {
        // 추적/이동 로직은 그대로 유지
        player = GetClosestPlayer();
        if (player == null) return;

        FlipSpriteTowardsPlayer();
        //피가 0되었을 때
        if (IsDead())
        {
            ReturnToPool();
        }
    }

    // 접촉이 유지되는 동안 매 물리 프레임마다 호출됩니다.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            // 몬스터는 계속 데미지를 주려고 시도합니다.
            // 데미지가 실제로 들어갈지 여부는 PlayerController의 무적 시간 로직이 결정합니다.
            pc.TakeDamage(contactDamage);
        }
    }

    // OnTriggerEnter2D는 이제 아무 역할도 하지 않으므로 삭제해도 괜찮습니다.
    // private void OnTriggerEnter2D(Collider2D other) { }

    protected void FlipSpriteTowardsPlayer()
    {
        if (spriteRenderer != null && player != null)
        {
            spriteRenderer.flipX = player.position.x > transform.position.x;
        }
    }

    protected Transform GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return null;

        Transform closest = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (GameObject p in players)
        {
            if (!p.activeInHierarchy) continue;
            float sqr = (p.transform.position - myPos).sqrMagnitude;
            if (sqr < minSqrDist)
            {
                minSqrDist = sqr;
                closest = p.transform;
            }
        }
        return closest;
    }

    public void TakeDamage(float damage)
    {
        CurHP -= damage;
    }

    private bool IsDead()
    {
        return CurHP <= 0;
    }

    protected virtual void ReturnToPool()
    {
        // 여기에 죽는 애니메이션, 사운드, 아이템 드랍 등의 로직을 넣을 수 있습니다.
        // 경험치 보석 드랍
        if (expJewelPrefab != null)
        {
            Instantiate(expJewelPrefab, transform.position, Quaternion.identity);
        }
        // 모든 처리가 끝난 후, 자신을 풀에 반납
        MonsterPool.Instance.ReturnToPool(poolTag, this);
    }

}