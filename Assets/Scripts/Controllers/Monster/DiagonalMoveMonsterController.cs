using UnityEngine;

public class DiagonalMoveMonsterController : MonsterController
{
    private Vector2 moveDirection;
    // 스포너가 이 몬스터의 이동 방향을 지정해주는 함수
    public void SetDirection(Vector2 direction)
    {
        this.moveDirection = direction;
    }

    protected override void Move()
    {
        // 지정된 방향으로 currentMoveSpeed에 맞춰 이동
        transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime);
    }
}