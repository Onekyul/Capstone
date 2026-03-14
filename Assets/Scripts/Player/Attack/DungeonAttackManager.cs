using UnityEngine;
using Fusion;

public class DungeonAttackManager : NetworkBehaviour
{
    private DungeonWeaponBase currentWeapon;
    private DungeonPlayerStats stats; // ★ 스탯 스크립트 참조 추가
    
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

#if !UNITY_SERVER
        if (HasInputAuthority)
        {
            if (DataManager.instance != null)
            {
                string equippedWeaponId = DataManager.instance.currentPlayer.equippedWeaponId;
                int weaponType = DataManager.instance.GetWeaponTypeFromId(equippedWeaponId);
                RPC_SetWeapon(weaponType, equippedWeaponId);
            }
            RPC_SetAutoAttack(true);
        }

        // 데디서버가 NetWeaponType을 이미 0으로 설정한 경우 OnChangedRender가 안 불리므로
        // 클라이언트에서 현재 값으로 직접 초기화
        SwitchWeaponVisuals(NetWeaponType, "");
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

        NetWeaponType = weaponType;
        IsAutoAttacking = true;
        SwitchWeaponVisuals(weaponType, weaponId);

        Debug.Log($"[Server] 무기 초기화: Type={weaponType}, Id={weaponId}");
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
                SwitchWeaponVisuals(NetWeaponType, "");
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

        if (Runner.Tick % 60 == 0) // 1초마다 한 번 로그
        {
            Debug.Log($"[AttackManager] IsAutoAttacking={IsAutoAttacking}, currentWeapon={currentWeapon != null}, NetLookDir={NetLookDir}, HasStateAuth={HasStateAuthority}, HasInputAuth={HasInputAuthority}");
        }

        if (IsAutoAttacking && currentWeapon != null && NetLookDir.sqrMagnitude > 0)
        {
            currentWeapon.Attack(NetLookDir);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetWeapon(int weaponType, string weaponId)
    {
        NetWeaponType = weaponType;
        SwitchWeaponVisuals(weaponType, weaponId); 
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetAutoAttack(NetworkBool isAttacking)
    {
        IsAutoAttacking = isAttacking;
    }

    public void OnWeaponChanged()
    {
      
        if (stats != null && stats.IsDead) return;
        
        SwitchWeaponVisuals(NetWeaponType, ""); 
    }

    private void HideAllWeapons()
    {
        if (swordObject != null) swordObject.SetActive(false);
        if (spearObject != null) spearObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(false);
    }

    private void SwitchWeaponVisuals(int weaponType, string weaponId)
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

        Debug.Log($"[DungeonAttackManager] SwitchWeaponVisuals: type={weaponType}, currentWeapon={currentWeapon}");
    }
}