using UnityEngine;

public class FireMonsterController : ElementMonsterController
{
    [Header("Fire Mortar Settings")]
    [SerializeField] private GameObject mortarPrefab;
    [SerializeField] private float mortarSpeed = 8f;
    [SerializeField] private float fireDamage = 10f;

    [Header("Fire Field Settings")]
    [SerializeField] private float fieldDuration = 3.0f;
    [SerializeField] private float fieldMaxScale = 3.0f;
    [SerializeField] private float fieldExpandSpeed = 2.0f;
    [SerializeField] private float fieldDamageInterval = 0.5f;
    [SerializeField] private float fieldDamage = 5.0f;

    protected override void PerformAttack()
    {
        if (mortarPrefab != null && player != null)
        {
            GameObject mortar = Instantiate(mortarPrefab, transform.position, Quaternion.identity);

            mortar.GetComponent<FireMortarProjectile>().Setup(
                player.position,
                fireDamage,
                mortarSpeed,
                fieldDuration,
                fieldMaxScale,
                fieldExpandSpeed,
                fieldDamageInterval,
                fieldDamage
            );
        }
    }
}