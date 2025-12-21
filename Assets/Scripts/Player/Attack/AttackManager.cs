using UnityEngine;
using System.Collections;

public class AttackManager : MonoBehaviour
{
    // currentWeapon은 런타임에 자동 설정되므로 Inspector 노출 불필요
    private WeaponBase currentWeapon;
    
    
    [Header("Weapon Objects")]
    [SerializeField] private GameObject swordObject;
    [SerializeField] private GameObject spearObject;
    [SerializeField] private GameObject bowObject;

    private float bowOrbitDistance = 0.5f; //활이 플레이어 주위를 도는 거리 설정 변수


    private bool bIsAutoAttacking = false; //공격 코루틴이 실행 중인지 확인 여부
    private Coroutine autoAttackCoroutine; // 실행 중인 코루틴 자체를 저장하는 변수
    private Vector2 curLookDir = Vector2.right; // 현재 플레이어가 바라보는 방향 저장
    
    void Start()
    {
        // 기본 무기 설정
        SwitchWeapon(WeaponType.Sword);

        // 자동 공격 시작
        StartAutoAttack();
    }

    void Update()
    {
        // 무기 교체 키 입력 (1, 2, 3 키)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(WeaponType.Sword);
            Debug.Log("검으로 교체!");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(WeaponType.Spear);
            Debug.Log("창으로 교체!");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchWeapon(WeaponType.Bow);
            Debug.Log("활로 교체!");
        }

        // 활을 장착하고 있을 때만 회전하도록 처리
        if (bowObject != null && currentWeapon is BowWeapon)
        {
            // 1. 위치 설정: curLookDir 방향으로 bowOrbitDistance 만큼 떨어진 위치로 이동
            bowObject.transform.position = (Vector2)transform.position + (curLookDir * bowOrbitDistance);
        
            // 2. 회전 설정: curLookDir 방향을 바라보도록 회전
            bowObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, curLookDir);
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
        // 1. 모든 무기 오브젝트를 비활성화 (벨트에 넣기)
        if (swordObject != null) swordObject.SetActive(false);
        if (spearObject != null) spearObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(false);

        // 2. 선택한 무기만 활성화하고 currentWeapon으로 설정 (벨트에서 꺼내기)
        switch (weaponType)
        {
            case WeaponType.Sword:
                if (swordObject != null)
                {
                    swordObject.SetActive(true);
                    currentWeapon = swordObject.GetComponent<SwordWeapon>();
                }
                break;
            case WeaponType.Spear:
                if (spearObject != null)
                {
                    spearObject.SetActive(true);
                    currentWeapon = spearObject.GetComponent<SpearWeapon>();
                }
                break;
            case WeaponType.Bow:
                if (bowObject != null)
                {
                    bowObject.SetActive(true);
                    currentWeapon = bowObject.GetComponent<BowWeapon>();
                }
                break;
        }
    }

    public enum WeaponType
    {
        Sword,
        Spear,
        Bow
    }
}


