using UnityEngine;
using Fusion;

public class DungeonAttackManager : NetworkBehaviour
{
    private DungeonWeaponBase currentWeapon;
    private DungeonPlayerStats stats;
    private Animator animator;

    // 로컬 클라이언트 전용 쿨타임 추적 (즉각 이펙트/애니메이션용)
    private float localNextAttackTime = 0f;
    
    [Header("Weapon Objects")]
    [SerializeField] private GameObject swordObject;
    [SerializeField] private GameObject spearObject;
    [SerializeField] private GameObject bowObject;

    private float bowOrbitDistance = 0.5f;

    [Networked, OnChangedRender(nameof(OnWeaponChanged))]
    public int NetWeaponType { get; set; } 
    
    [Networked] public NetworkBool IsAutoAttacking { get; set; } 
    [Networked] public Vector2 NetLookDir { get; set; } 

    public override void Spawned()
    {
        stats = GetComponent<DungeonPlayerStats>();
        animator = GetComponentInChildren<Animator>();

#if !UNITY_SERVER
        if (HasInputAuthority)
        {
            if (DataManager.instance != null)
            {
                string equippedWeaponId = DataManager.instance.currentPlayer.equippedWeaponId;
                int weaponType = DataManager.instance.GetWeaponTypeFromId(equippedWeaponId);
                RPC_SetWeapon(weaponType);
            }
            else
            {
                Debug.LogWarning("[Client] DataManager.instance가 null — RPC_SetWeapon 미전송!");
            }
            RPC_SetAutoAttack(true);
        }

        // 데디서버가 NetWeaponType을 이미 0으로 설정한 경우 OnChangedRender가 안 불리므로
        // 클라이언트에서 현재 값으로 직접 초기화
        SwitchWeaponVisuals(NetWeaponType);
#endif
        // 데디서버에서는 BossDungeonServer가 InitWeaponFromServer()를 호출함
    }

    /// <summary>
    /// 데디서버 전용: 서버가 직접 무기를 설정.
    /// StateAuthority(서버)에서만 호출해야 함.
    /// </summary>
    public void InitWeaponFromServer(int weaponType, string weaponId)
    {
        if (!HasStateAuthority) return;

        IsAutoAttacking = true;

        if (currentWeapon == null)
        {
            // RPC_SetWeapon이 아직 미도착 → 서버 계산값으로 임시 설정 (RPC 도착 시 덮어씀)
            SwitchWeaponVisuals(weaponType);
            Debug.Log($"[Server] 무기 초기화(서버 계산값 임시): Type={weaponType}, Id={weaponId}");
        }
        else
        {
            // RPC_SetWeapon이 이미 도착해 currentWeapon이 설정됨 → 건드리지 않음
            Debug.Log($"[Server] 무기 초기화: RPC 이미 처리됨, currentWeapon={currentWeapon.GetType().Name}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // ★ [추가] 죽었으면 공격 불가! 무기도 숨김
        if (stats != null && stats.IsDead)
        {
            HideAllWeapons();
            return;
        }
        else
        {
            // 살았는데 무기가 안 보이면 다시 보이게 세팅
            if (currentWeapon != null && !currentWeapon.gameObject.activeSelf)
            {
                SwitchWeaponVisuals(NetWeaponType);
            }
        }

        if (GetInput(out NetworkInputData data))
        {
            NetLookDir = data.lookDirection; 
        }

        if (bowObject != null && currentWeapon is DungeonBow)
        {
            bowObject.transform.position = (Vector2)transform.position + (NetLookDir * bowOrbitDistance);
            bowObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, NetLookDir);
        }

        if (IsAutoAttacking && currentWeapon != null && NetLookDir.sqrMagnitude > 0)
        {
            // 서버: 데미지 처리 + 다른 클라이언트들에게 이펙트 RPC
            if (HasStateAuthority)
            {
                if (currentWeapon.TryServerAttack(NetLookDir))
                {
                    RPC_PlayAttackEffect(NetWeaponType, NetLookDir);
                }
            }

#if !UNITY_SERVER
            // 로컬 클라이언트: 즉시 이펙트 + 애니메이션 (서버 왕복 없음)
            if (HasInputAuthority)
            {
                float speedMult = stats != null ? stats.AttackSpeedMultiplier : 1f;
                float cooldown = currentWeapon.NetAttackCooldown > 0f
                    ? currentWeapon.NetAttackCooldown / speedMult
                    : 1f; // 초기화 전 기본값
                float now = Runner.SimulationTime;

                if (now >= localNextAttackTime)
                {
                    localNextAttackTime = now + cooldown;
                    PlayAttackVisuals(NetWeaponType, NetLookDir);
                }
            }
#endif
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetWeapon(int weaponType)
    {
        Debug.Log($"[Server] RPC_SetWeapon 수신: weaponType={weaponType}, 이전NetWeaponType={NetWeaponType}");
        NetWeaponType = weaponType;
        SwitchWeaponVisuals(weaponType);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetAutoAttack(NetworkBool isAttacking)
    {
        IsAutoAttacking = isAttacking;
    }

    // 다른 플레이어들에게만 전송 (Proxies = InputAuthority도 StateAuthority도 아닌 클라이언트)
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    public void RPC_PlayAttackEffect(int weaponType, Vector2 direction)
    {
        PlayAttackVisuals(weaponType, direction);
    }

    private void PlayAttackVisuals(int weaponType, Vector2 direction)
    {
        if (animator != null)
            animator.SetTrigger("2_Attack");

        switch (weaponType)
        {
            case 0: swordObject?.GetComponent<DungeonSword>()?.CreateAttackEffect(direction); break;
            case 1: spearObject?.GetComponent<DungeonSpear>()?.CreateAttackEffect(direction); break;
            case 2: bowObject?.GetComponent<DungeonBow>()?.CreateAttackEffect(direction); break;
        }
    }

    public void OnWeaponChanged()
    {
        if (stats != null && stats.IsDead) return;
        SwitchWeaponVisuals(NetWeaponType);
    }

    private void HideAllWeapons()
    {
        if (swordObject != null) swordObject.SetActive(false);
        if (spearObject != null) spearObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(false);
    }

    public DungeonWeaponBase GetCurrentWeapon() => currentWeapon;

    private void SwitchWeaponVisuals(int weaponType)
    {
        HideAllWeapons();

        switch (weaponType)
        {
            case 0:
                if (swordObject != null)
                {
                    swordObject.SetActive(true);
                    currentWeapon = swordObject.GetComponent<DungeonSword>();
                    if (currentWeapon == null)
                        Debug.LogError("[DungeonAttackManager] swordObject에 DungeonSword 컴포넌트가 없습니다!");
                }
                break;
            case 1:
                if (spearObject != null)
                {
                    spearObject.SetActive(true);
                    currentWeapon = spearObject.GetComponent<DungeonSpear>();
                    if (currentWeapon == null)
                        Debug.LogError("[DungeonAttackManager] spearObject에 DungeonSpear 컴포넌트가 없습니다!");
                }
                break;
            case 2:
                if (bowObject != null)
                {
                    bowObject.SetActive(true);
                    currentWeapon = bowObject.GetComponent<DungeonBow>();
                    if (currentWeapon == null)
                        Debug.LogError("[DungeonAttackManager] bowObject에 DungeonBow 컴포넌트가 없습니다!");
                }
                break;
        }

        if (animator != null)
            animator.SetInteger("WeaponType", weaponType);

        Debug.Log($"[DungeonAttackManager] SwitchWeaponVisuals: type={weaponType}, currentWeapon={currentWeapon}");
    }
}