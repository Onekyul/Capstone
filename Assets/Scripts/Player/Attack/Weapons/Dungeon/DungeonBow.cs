using UnityEngine;
using Fusion;

public class DungeonBow : DungeonWeaponBase
{
    [Header("Bow Specific")]
    [SerializeField] private DungeonArrow arrowPrefab; 

    protected override void ExecuteServerAttack(Vector2 direction)
    {
        if (HasStateAuthority)
        {
            Quaternion spawnRot = Quaternion.LookRotation(Vector3.forward, direction);

            Runner.Spawn(
                arrowPrefab,
                attackPoint.position,
                spawnRot,
                Object.InputAuthority,
                (runner, spawnedObj) =>  
                {
                    DungeonArrow arrow = spawnedObj.GetComponent<DungeonArrow>();
                    
                    if (arrow != null)
                    {
                        arrow.InitNetworkData(GetTotalDamage(), CalculateAppliedEnchants());
                    }
                }
            );
        }
    }

    public override void CreateAttackEffect(Vector2 direction)
    {
        Debug.Log("[DungeonBow] CreateAttackEffect 실행!");
        // 활은 화살 자체가 시각 효과이므로 별도 이펙트 없음
    }
}