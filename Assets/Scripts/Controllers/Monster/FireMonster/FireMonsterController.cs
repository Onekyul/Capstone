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
            
            mortar.GetComponent<FireMortarProjectile>().Setup(player.position, normalDamage);
        }
    }
}