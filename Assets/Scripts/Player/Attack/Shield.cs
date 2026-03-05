using UnityEngine;

public class Shield : MonoBehaviour
{
    private ShieldOrbit orbitRoot;

    void Start()
    {
        orbitRoot = GetComponentInParent<ShieldOrbit>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[방패 충돌 감지] {collision.name}, 태그: {collision.tag}, 레이어: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        // 적 레이어 체크 (설명서의 Enemy 레이어 7번 또는 태그 활용)
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log($"[방패] Enemy 태그 확인됨!");
            
            float damage = orbitRoot.GetShieldDamage();
            
            // 몬스터에게 데미지 전달
            var monster = collision.GetComponent<MonsterController>();
            if (monster != null)
            {
                // 엘리트 킬러 능력 적용
                if (PlayerStats.Instance != null)
                {
                    MonsterType monsterType = monster.GetMonsterType();
                    float monsterTypeMultiplier = PlayerStats.Instance.GetMonsterTypeDamageMultiplier(monsterType);
                    damage *= monsterTypeMultiplier;
                    
                    if (monsterTypeMultiplier != 1.0f)
                    {
                        Debug.Log($"[엘리트 킬러] {monsterType} 몬스터에게 배율 {monsterTypeMultiplier * 100}% 적용! 최종 데미지: {damage:F1}");
                    }
                }
                
                monster.TakeDamage(damage);
                Debug.Log($"[방패 타격] {collision.name}에게 {damage:F1} 데미지");
                
                // TODO : 필요하다면 여기서 타격 이펙트 생성
            }
            else
            {
                Debug.LogWarning($"[방패] {collision.name}에 MonsterController가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[방패] {collision.name}은 Enemy 태그가 아닙니다. 현재 태그: {collision.tag}");
        }
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}