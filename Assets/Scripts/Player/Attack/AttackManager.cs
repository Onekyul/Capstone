using UnityEngine;
using System.Collections;

public class AttackManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponBase currentWeapon;
    [SerializeField] private float autoAttackInterval = 1f;
    
    [Header("Weapon Types")]
    [SerializeField] private SwordWeapon swordWeapon;
    // [SerializeField] private SpearWeapon spearWeapon;
    // [SerializeField] private BowWeapon bowWeapon;
    
    private bool bIsAutoAttacking = false; //공격 코루틴이 실행 중인지 확인 여부
    private Coroutine autoAttackCoroutine; // 실행 중인 코루틴 자체를 저장하는 변수
    private Vector2 curLookDir= Vector2.right;
    
    void Start()
    {
        // 기본 무기 설정
        if (currentWeapon == null && swordWeapon != null)
        {
            currentWeapon = swordWeapon;
        }
    }
    
    private void OnEnable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnLook += HandleLook;
        }
    }

    private void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnLook -= HandleLook;
        }
    }

    private void HandleLook(Vector2 direction)
    {
        curLookDir = direction;
    }

    public void StartAutoAttack()
    {
        if (!bIsAutoAttacking) // bIsAutoAttacking이 False 이면,
        {
            bIsAutoAttacking = true;
            autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
        }
    }
    
    public void StopAutoAttack()
    {
        if (bIsAutoAttacking)
        {
            bIsAutoAttacking = false;
            if (autoAttackCoroutine != null)
            {
                StopCoroutine(autoAttackCoroutine);
            }
        }
    }
    
    private IEnumerator AutoAttackCoroutine()
    {
        Debug.Log("AttackManager: Auto attack started");
        while (bIsAutoAttacking)
        {
            if (currentWeapon != null)
            {
                Vector2 attackDirection = curLookDir;
                Debug.Log($"AttackManager: Attacking in direction {attackDirection}");
                currentWeapon.Attack(attackDirection);
            }
            else
            {
                Debug.LogWarning("AttackManager: No current weapon assigned!");
            }
            
            yield return new WaitForSeconds(autoAttackInterval);
        }
    }
    
    
    public void SwitchWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                currentWeapon = swordWeapon;
                break;
            // case WeaponType.Spear:
            //     currentWeapon = spearWeapon;
            //     break;
            // case WeaponType.Bow:
            //     currentWeapon = bowWeapon;
            //     break;
        }
    }
}

public enum WeaponType
{
    Sword,
    // Spear,
    // Bow
}
