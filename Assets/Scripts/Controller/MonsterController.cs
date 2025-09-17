using UnityEngine;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 2f; // 적의 이동 속도


    protected Transform player; // 현재 타겟(가장 가까운 Player)
    protected SpriteRenderer spriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetClosestPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        // 매 프레임 가장 가까운 플레이어 갱신
        player = GetClosestPlayer();
        if (player == null)
        {
            // 타겟이 없으면 상태 리셋 후 종료
            return;
        }
        //플레이어 위치에 따라 스프라이트 좌우 반전
        FlipSpriteTowardsPlayer();
        FollowPlayer();
    }


    protected void FlipSpriteTowardsPlayer()
    {
        if (spriteRenderer != null) 
        {
            spriteRenderer.flipX = player.position.x > transform.position.x;
        }
    }

    protected void FollowPlayer()
    {
        if(player == null) return;

        Vector2 targetPosition = new Vector2(player.position.x, player.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
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


}
