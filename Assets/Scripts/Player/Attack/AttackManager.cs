using UnityEngine;
using System.Collections;

public class AttackManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponBase currentWeapon;
    
    
    [Header("Weapon Types")]
    [SerializeField] private SwordWeapon swordWeapon;
    // [SerializeField] private SpearWeapon spearWeapon;
    // [SerializeField] private BowWeapon bowWeapon;

    [Header("Weapon Visuals")] // 식별하기 쉽도록 헤더 추가
    [SerializeField] private Transform bowTransform;
    private float bowOrbitDistance = 0.6f;
    
    private bool bIsAutoAttacking = false; //공격 코루틴이 실행 중인지 확인 여부
    private Coroutine autoAttackCoroutine; // 실행 중인 코루틴 자체를 저장하는 변수
    private Vector2 curLookDir;
    
    void Start()
    {
        // 기본 무기 설정
        if (currentWeapon == null && swordWeapon != null)
        {
            currentWeapon = swordWeapon;
        }
    }

    void Update()
    {
        // 활을 장착하고 있을 때만 회전하도록 처리
        if (bowTransform != null && currentWeapon is BowWeapon)
        {
           bowTransform.gameObject.SetActive(true);

            // 1. 위치 설정: curLookDir 방향으로 bowOrbitDistance 만큼 떨어진 위치로 이동
            bowTransform.position = (Vector2)transform.position + (curLookDir * bowOrbitDistance);
        
            // 2. 회전 설정: curLookDir 방향을 바라보도록 회전
            bowTransform.rotation = Quaternion.LookRotation(Vector3.forward, curLookDir);
        }
            // 다른 무기를 들었을 때는 활이 보이지 않게 처리 (선택사항)
        else if (bowTransform != null)
        {
            bowTransform.gameObject.SetActive(false);
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
                
                currentWeapon.Attack(curLookDir);
            }
            else
            {
                Debug.LogWarning("AttackManager: No current weapon assigned!");
            }

            yield return null;
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
     Spear,
     Bow
}
