using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Auto Attack Settings")]
    [SerializeField] private string[] safeZoneSceneNames = { "Base", "거점", "Town", "Village" }; // 자동 공격이 비활성화될 씬 이름들

    private bool bIsAutoAttacking; //공격 코루틴이 실행 중인지 확인 여부
    private Coroutine autoAttackCoroutine; // 실행 중인 코루틴 자체를 저장하는 변수
    private Vector2 curLookDir = Vector2.right; // 현재 플레이어가 바라보는 방향 저장
    
    void Start()
    {
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

        // ===== 무기별 인챈트 테스트 치트키 =====
        // F1-F4: 현재 무기에 인챈트 추가
        if (Input.GetKeyDown(KeyCode.F1))
        {
            currentWeapon?.SetEnchantmentLevel(0, 5); // 불 인챈트 5레벨
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            currentWeapon?.SetEnchantmentLevel(1, 5); // 얼음 인챈트 5레벨
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            currentWeapon?.SetEnchantmentLevel(2, 5); // 번개 인챈트 5레벨
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            currentWeapon?.SetEnchantmentLevel(3, 5); // 독 인챈트 5레벨
        }

        // F5: 현재 무기의 모든 인챈트 레벨 확인
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (currentWeapon != null)
            {
                int[] levels = currentWeapon.GetEnchantmentLevels();
                Debug.Log($"현재 무기 인챈트 레벨 - 불:{levels[0]}, 얼음:{levels[1]}, 번개:{levels[2]}, 독:{levels[3]}");
            }
        }

        // F6: 모든 무기에 동일한 인챈트 적용 (테스트용)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            int[] allEnchants = new int[] { 3, 3, 3, 3 }; // 모든 인챈트 레벨 3
            if (swordObject != null) swordObject.GetComponent<SwordWeapon>()?.SetAllEnchantmentLevels(allEnchants);
            if (spearObject != null) spearObject.GetComponent<SpearWeapon>()?.SetAllEnchantmentLevels(allEnchants);
            if (bowObject != null) bowObject.GetComponent<BowWeapon>()?.SetAllEnchantmentLevels(allEnchants);
            Debug.Log("모든 무기에 인챈트 레벨 3 적용!");
        }

        // F7: 각 무기에 다른 인챈트 적용 (테스트용)
        if (Input.GetKeyDown(KeyCode.F7))
        {
            // 검: 불 인챈트만
            if (swordObject != null) swordObject.GetComponent<SwordWeapon>()?.SetAllEnchantmentLevels(new int[] { 5, 0, 0, 0 });
            // 창: 얼음 인챈트만
            if (spearObject != null) spearObject.GetComponent<SpearWeapon>()?.SetAllEnchantmentLevels(new int[] { 0, 5, 0, 0 });
            // 활: 번개 인챈트만
            if (bowObject != null) bowObject.GetComponent<BowWeapon>()?.SetAllEnchantmentLevels(new int[] { 0, 0, 5, 0 });
            Debug.Log("무기별 고유 인챈트 적용! (검:불, 창:얼음, 활:번개)");
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
        string equippedWeaponId = "";
        
        switch (weaponType)
        {
            case WeaponType.Sword:
                if (swordObject != null)
                {
                    swordObject.SetActive(true);
                    currentWeapon = swordObject.GetComponent<SwordWeapon>();
                    // 검의 weaponId 가져오기
                    if (currentWeapon != null)
                    {
                        equippedWeaponId = currentWeapon.GetWeaponId();
                    }
                }
                break;
            case WeaponType.Spear:
                if (spearObject != null)
                {
                    spearObject.SetActive(true);
                    currentWeapon = spearObject.GetComponent<SpearWeapon>();
                    // 창의 weaponId 가져오기
                    if (currentWeapon != null)
                    {
                        equippedWeaponId = currentWeapon.GetWeaponId();
                    }
                }
                break;
            case WeaponType.Bow:
                if (bowObject != null)
                {
                    bowObject.SetActive(true);
                    currentWeapon = bowObject.GetComponent<BowWeapon>();
                    // 활의 weaponId 가져오기
                    if (currentWeapon != null)
                    {
                        equippedWeaponId = currentWeapon.GetWeaponId();
                    }
                }
                break;
        }
        
        // 3. DataManager에 현재 무기 저장
        if (DataManager.instance != null && !string.IsNullOrEmpty(equippedWeaponId))
        {
            DataManager.instance.EquipWeapon(equippedWeaponId);
            Debug.Log($"AttackManager: 무기 변경 저장 - {equippedWeaponId}");
        }
    }

    // ===== 무기 접근 메서드 (AbilitySystem 등에서 사용) =====
    
    /// <summary>
    /// 현재 활성화된 무기를 반환합니다.
    /// </summary>
    public WeaponBase GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// 모든 무기 오브젝트를 배열로 반환합니다.
    /// </summary>
    public GameObject[] GetAllWeaponObjects()
    {
        return new GameObject[] { swordObject, spearObject, bowObject };
    }

    /// <summary>
    /// 모든 무기의 WeaponBase 컴포넌트를 배열로 반환합니다.
    /// </summary>
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


