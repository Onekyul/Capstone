using UnityEngine;
using Fusion;

public class DungeonAttackManager : NetworkBehaviour
{
    private WeaponBase currentWeapon;
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
        stats = GetComponent<DungeonPlayerStats>(); // 세팅

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

        if (bowObject != null && currentWeapon is BowWeapon)
        {
            bowObject.transform.position = (Vector2)transform.position + (NetLookDir * bowOrbitDistance);
            bowObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, NetLookDir);
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
                if (swordObject != null) { swordObject.SetActive(true); currentWeapon = swordObject.GetComponent<SwordWeapon>(); }
                break;
            case 1: 
                if (spearObject != null) { spearObject.SetActive(true); currentWeapon = spearObject.GetComponent<SpearWeapon>(); }
                break;
            case 2: 
                if (bowObject != null) { bowObject.SetActive(true); currentWeapon = bowObject.GetComponent<BowWeapon>(); }
                break;
        }
    }
}