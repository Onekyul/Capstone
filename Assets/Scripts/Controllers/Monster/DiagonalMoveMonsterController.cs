using UnityEngine;

public class DiagonalMoveMonsterController : MonsterController
{
    private Vector2 moveDirection;

    [SerializeField] private float moveSpeed = 2f;
    // 스포너가 이 몬스터의 이동 방향을 지정해주는 함수
    public void SetDirection(Vector2 direction)
    {
        this.moveDirection = direction;
    }

    // 부모의 Update(플레이어 추적)를 덮어써서 새로운 행동을 정의
    protected override void Update()
    {
        base.Update();

        // 지정된 방향으로 moveSpeed에 맞춰 이동
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        
    }
}