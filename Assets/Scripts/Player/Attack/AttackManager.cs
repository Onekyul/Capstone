using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AttackManager : MonoBehaviour
{
    // currentWeapon은 런타임에 자동 설정되므로 Inspector 노출 불필요
    private WeaponBase currentWeapon;
    private Animator animator;
    
    [Header("Weapon Objects")]
    [SerializeField] private GameObject swordObject;
    [SerializeField] private GameObject spearObject;
    [SerializeField] private GameObject bowObject;

    private float bowOrbitDistance = 0.5f; //활이 플레이어 주위를 도는 거리 설정 변수

    [Header("Auto Attack Settings")]
    [SerializeField] private string[] safeZoneSceneNames = { "Base", "거점", "Town", "Village" }; // 자동 공격이 비활성화될 씬 이름들

    private bool bIsAutoAttacking; //공격 코루틴이 실행 중인지 확인 여부
    private Coroutine autoAttackCoroutine; // 실행 중인 코루틴 자체를 저장하는 변수
    private Vector2 curLookDir = Vector2.right; // 현재 플레이어가 바라보는 방향 저장
    
    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        // DataManager에서 장착된 무기 ID를 읽어와서 해당 무기로 자동 장착
        if (DataManager.instance != null)
        {
            string equippedWeaponId = DataManager.instance.currentPlayer.equippedWeaponId;
            int weaponType = DataManager.instance.GetWeaponTypeFromId(equippedWeaponId);
            SwitchWeapon((WeaponType)weaponType);
            Debug.Log($"AttackManager: 저장된 무기 자동 장착 - {equippedWeaponId} (타입: {(WeaponType)weaponType})");
        }
        else
        {
            // DataManager가 없으면 기본 무기(검)로 시작
            SwitchWeapon(WeaponType.Sword);
            Debug.LogWarning("AttackManager: DataManager가 없어 기본 무기(검)로 시작합니다.");
        }

        // 현재 씬이 안전 지역(거점)이 아니면 자동 공격 시작
        if (!IsInSafeZone())
        {
            StartAutoAttack();
            Debug.Log("AttackManager: 전투 지역 - 자동 공격 활성화");
        }
        else
        {
            Debug.Log("AttackManager: 안전 지역(거점) - 자동 공격 비활성화");
        }
    }

    void Update()
    {
        // 활을 장착하고 있을 때만 회전하도록 처리
        // if (bowObject != null && currentWeapon is BowWeapon)
        // {
        //     // 1. 위치 설정: curLookDir 방향으로 bowOrbitDistance 만큼 떨어진 위치로 이동
        //     bowObject.transform.position = (Vector2)transform.position + (curLookDir * bowOrbitDistance);
        
        //     // 2. 회전 설정: curLookDir 방향을 바라보도록 회전
        //     bowObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, curLookDir);
        // }
        
        // ===== 치트키: 무기 교체 =====
        // 1 키: 검으로 교체
        if (Input.GetKeyDown(KeyCode.Alpha1) )
        {
            SwitchWeapon(WeaponType.Sword);
            Debug.Log("===== [치트키] 검으로 무기 교체 =====");
        }
        
        // 2 키: 창으로 교체
        if (Input.GetKeyDown(KeyCode.Alpha2) )
        {
            SwitchWeapon(WeaponType.Spear);
            Debug.Log("===== [치트키] 창으로 무기 교체 =====");
        }
        
        // 3 키: 활으로 교체
        if (Input.GetKeyDown(KeyCode.Alpha3) )
        {
            SwitchWeapon(WeaponType.Bow);
            Debug.Log("===== [치트키] 활으로 무기 교체 =====");
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
            Debug.Log("AttackManager: 자동 공격 시작");
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
                Debug.Log("AttackManager: 자동 공격 정지");
            }
        }
    }
    
    // 현재 씬이 거점인지 확인합니다.
    private bool IsInSafeZone()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        foreach (string safeName in safeZoneSceneNames)
        {
            if (currentSceneName.Contains(safeName))
            {
                return true;
            }
        }
        return false;
    }
    
    // private IEnumerator AutoAttackCoroutine()
    // {
    //     Debug.Log("AttackManager: Auto attack started");
    //     while (bIsAutoAttacking)
    //     {
    //         if (currentWeapon != null)
    //         {
                
    //             currentWeapon.Attack(curLookDir);
    //         }
    //         else
    //         {
    //             Debug.LogWarning("AttackManager: No current weapon assigned!");
    //         }

    //         yield return null;
    //     }
    // }

    private IEnumerator AutoAttackCoroutine()
    {
        Debug.Log("AttackManager: Auto attack started");

        while (bIsAutoAttacking)
        {
            if (currentWeapon != null)
            {
                // 1. 쿨타임이 돌 때마다 정확히 한 번씩 공격 애니메이션 실행!
                if (animator != null)
                {
                    animator.SetTrigger("2_Attack");
                }

                // 2. 실제 데미지 판정 및 검강 발사
                currentWeapon.Attack(curLookDir);

                // 3. 무기 쿨타임만큼 대기
                float actualCooldown = currentWeapon.GetAttackCooldown();
                yield return new WaitForSeconds(actualCooldown); 
            }
            else
            {
                yield return null;
            }
        }
    }


    public void SwitchWeapon(WeaponType weaponType)
    {
        // 1. 모든 무기 오브젝트를 비활성화 (벨트에 넣기)
        if (swordObject != null) swordObject.SetActive(false);
        if (spearObject != null) spearObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(false);

        // 2. 선택한 무기만 활성화하고 currentWeapon으로 설정 (벨트에서 꺼내기)
        string equippedWeaponId = "";
        int weaponTypeIndex = (int)weaponType;
        
        //무기 타입별로 해당 타입의 마지막 사용 무기 ID 가져오기
        if (DataManager.instance != null)
        {
            equippedWeaponId = DataManager.instance.GetEquippedWeaponId(weaponTypeIndex);
        }
        
        switch (weaponType)
        {
            case WeaponType.Sword:
                if (swordObject != null)
                {
                    swordObject.SetActive(true);
                    currentWeapon = swordObject.GetComponent<SwordWeapon>();
                    
                    // 무기 ID가 비어있지 않으면 설정 (강화/인챈트 데이터 로드)
                    if (currentWeapon != null && !string.IsNullOrEmpty(equippedWeaponId))
                    {
                        currentWeapon.SetWeaponId(equippedWeaponId);
                        Debug.Log($"AttackManager: 검에 weaponId 설정 - {equippedWeaponId}");
                    }
                }
                break;
                
            case WeaponType.Spear:
                if (spearObject != null)
                {
                    spearObject.SetActive(true);
                    currentWeapon = spearObject.GetComponent<SpearWeapon>();
                    
                    // 무기 ID가 비어있지 않으면 설정 (강화/인챈트 데이터 로드)
                    if (currentWeapon != null && !string.IsNullOrEmpty(equippedWeaponId))
                    {
                        currentWeapon.SetWeaponId(equippedWeaponId);
                        Debug.Log($"AttackManager: 창에 weaponId 설정 - {equippedWeaponId}");
                    }
                }
                break;
                
            case WeaponType.Bow:
                if (bowObject != null)
                {
                    bowObject.SetActive(true);
                    currentWeapon = bowObject.GetComponent<BowWeapon>();
                    
                    // 무기 ID가 비어있지 않으면 설정 (강화/인챈트 데이터 로드)
                    if (currentWeapon != null && !string.IsNullOrEmpty(equippedWeaponId))
                    {
                        currentWeapon.SetWeaponId(equippedWeaponId);
                        Debug.Log($"AttackManager: 활에 weaponId 설정 - {equippedWeaponId}");
                    }
                }
                break;
        }
        
        //DataManager에 현재 무기 저장 (하위 호환성)
        if (DataManager.instance != null && !string.IsNullOrEmpty(equippedWeaponId))
        {
            DataManager.instance.currentPlayer.equippedWeaponId = equippedWeaponId;
            DataManager.instance.SaveGame();
            Debug.Log($"AttackManager: 무기 변경 저장 완료 - {equippedWeaponId}");
        }
        // 👈 추가: 애니메이터에게 현재 무기 타입(0:검, 1:창, 2:활)을 알려줌
        if (animator != null)
        {
            animator.SetInteger("WeaponType", (int)weaponType);
        }
    }

 
    public WeaponBase GetCurrentWeapon()
    {
        return currentWeapon;
    }


    public GameObject[] GetAllWeaponObjects()
    {
        return new GameObject[] { swordObject, spearObject, bowObject };
    }
    
    public WeaponBase[] GetAllWeapons()
    {
        WeaponBase[] weapons = new WeaponBase[3];
        if (swordObject != null) weapons[0] = swordObject.GetComponent<WeaponBase>();
        if (spearObject != null) weapons[1] = spearObject.GetComponent<WeaponBase>();
        if (bowObject != null) weapons[2] = bowObject.GetComponent<WeaponBase>();
        return weapons;
    }

    public enum WeaponType
    {
        Sword,
        Spear,
        Bow
    }
}


