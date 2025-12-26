using UnityEngine;

public class FireMonsterController : ElementMonsterController
{
    [Header("Fire Mortar Settings")]
    [SerializeField] private GameObject mortarPrefab;

    protected override void PerformAttack()
    {
        if (mortarPrefab != null && player != null)
        {
            GameObject mortar = Instantiate(mortarPrefab, transform.position, Quaternion.identity);

            // [수정] Setup 호출 시 공격력(normalDamage)도 같이 전달
            // normalDamage는 부모인 MonsterController가 가지고 있습니다.
            mortar.GetComponent<FireMortarProjectile>().Setup(player.position, normalDamage);
        }
    }
}