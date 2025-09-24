using UnityEngine;

public class NormalMonsterController : MonsterController
{

    [SerializeField] private float moveSpeed = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        FollowPlayer();
    }
    
    void FollowPlayer()
    {
        if (player == null) return;
        Vector2 targetPosition = new Vector2(player.position.x, player.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

}
