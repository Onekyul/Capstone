using UnityEngine;

public class DiagonalMoveMonsterController : MonsterController
{
    private Vector2 moveDirection;
    
    protected override void Start()
    {
        base.Start();
    }
    
    // 스포너가 이 몬스터의 이동 방향을 지정해주는 함수
    public void SetDirection(Vector2 direction)
    {
        this.moveDirection = direction;
    }

    // 부모의 Update를 완전히 오버라이드 (플레이어 추적 대신 지정된 방향으로 이동)
    protected override void Update()
    {
        if (getIsFrozen()) return;

        // 지정된 방향으로 currentMoveSpeed에 맞춰 이동 (슬로우/프리즈 효과 적용)
        transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime);

        if (IsDead()) ReturnToPool();
    }
}