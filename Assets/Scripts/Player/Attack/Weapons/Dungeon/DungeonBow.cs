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

    protected override void CreateAttackEffect(Vector2 direction)
    {
        // 활은 시각 이펙트(검기 등)가 따로 없고 화살 자체가 날아가므로 비워두거나,
        // 필요하다면 활 쏘는 사운드 플레이, 활시위 당기는 애니메이션 등을 여기서 처리합니다.
    }
}