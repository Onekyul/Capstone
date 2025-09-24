using UnityEngine;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{


    [SerializeField] private float contactDamage = 5.0f;

    public float normalDamage => contactDamage;

    [SerializeField] private float damageInterval = 0.25f;
    private readonly Dictionary<PlayerController, float> lastDamageTime = new();

    protected Transform player;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetClosestPlayer();
    }

    protected virtual void Update()
    {
        // 1) 접촉 데미지는 플레이어 유무와 관계없이 매 프레임 처리
        ProcessContactDamage();

        // 2) 추적/이동 로직
        player = GetClosestPlayer();
        if (player == null) return;

        FlipSpriteTowardsPlayer();
    }

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

        foreach (var p in players)
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

    // 접촉 중인 플레이어들에게 주기적 데미지
    private void ProcessContactDamage()
    {
        if (lastDamageTime.Count == 0) return;

        var snapshot = new List<PlayerController>(lastDamageTime.Keys);
        foreach (var pc in snapshot)
        {
            if (pc == null || !pc.gameObject.activeInHierarchy)
            {
                lastDamageTime.Remove(pc);
                continue;
            }

            float last = lastDamageTime[pc];
            if (Time.time - last >= damageInterval)
            {
                pc.TakeDamage(contactDamage);
                lastDamageTime[pc] = Time.time;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        // 즉시 1틱 (원치 않으면 아래 두 줄 제거)
        pc.TakeDamage(contactDamage);
        lastDamageTime[pc] = Time.time;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        lastDamageTime.Remove(pc);
    }

    private void OnDisable()
    {
        lastDamageTime.Clear();
    }
    
    
}