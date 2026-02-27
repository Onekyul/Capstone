using Fusion;
using UnityEngine;
using System;

public class DungeonPlayerStats : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public float NetCurHP { get; set; }
    
    [Networked, OnChangedRender(nameof(OnDeadStateChanged))]
    public NetworkBool IsDead { get; set; } 

    [Header("Base Stats")]
    public float baseMaxHP = 100f;
    public float baseDefense = 0f;

    [Networked] public float MaxHP { get; set; }
    [Networked] public float TotalDefense { get; set; }
    [Networked] public float MoveSpeedMultiplier { get; set; } = 1.0f;
    [Networked] public float AttackSpeedMultiplier { get; set; } = 1.0f;

    public event Action<float> OnHealthChangedLocal; 
    public event Action OnPlayerDiedLocal; 

    public override void Spawned()
    {
#if !UNITY_SERVER
        if (HasInputAuthority)
        {
            CalculateAndSendMyStats();
        }
#endif
        // 데디서버에서는 BossDungeonServer.OnPlayerJoined()가 InitFromServerData()를 호출함
    }

    /// <summary>
    /// 데디서버 전용: 서버가 백엔드에서 조회한 스탯을 직접 설정.
    /// StateAuthority(서버)에서만 호출해야 함.
    /// </summary>
    public void InitFromServerData(float maxHp, float defense, float moveSpeed, float attackSpeed)
    {
        if (!HasStateAuthority) return;

        MaxHP = maxHp;
        TotalDefense = defense;
        MoveSpeedMultiplier = moveSpeed;
        AttackSpeedMultiplier = attackSpeed;
        NetCurHP = MaxHP;
        IsDead = false;

        Debug.Log($"[Server] 플레이어 스탯 초기화: HP={maxHp}, Def={defense}, MoveSpd={moveSpeed}, AtkSpd={attackSpeed}");
    }

    private void CalculateAndSendMyStats()
    {
        // 스탯 계산 코드
        if (DataManager.instance == null) { RPC_InitMyStats(baseMaxHP, baseDefense, 1.0f, 1.0f); return; }

        float calcMaxHP = baseMaxHP; float calcDefense = baseDefense; float calcMoveSpeed = 1.0f; float calcAttackSpeed = 1.0f;

        string armorId = DataManager.instance.GetEquippedItemId(EquipmentType.Armor);
        if (!string.IsNullOrEmpty(armorId)) { ArmorData armor = DataManager.instance.GetArmorData(armorId); if (armor != null) calcMaxHP += armor.bonusHp + (armor.hpPerLevel * DataManager.instance.GetItemLevel(armorId)); }

        string helmetId = DataManager.instance.GetEquippedItemId(EquipmentType.Helmet);
        if (!string.IsNullOrEmpty(helmetId)) { ArmorData helmet = DataManager.instance.GetArmorData(helmetId); if (helmet != null) calcDefense += helmet.bonusDef + (helmet.defPerLevel * DataManager.instance.GetItemLevel(helmetId)); }

        string bootsId = DataManager.instance.GetEquippedItemId(EquipmentType.Boots);
        if (!string.IsNullOrEmpty(bootsId)) { ArmorData boots = DataManager.instance.GetArmorData(bootsId); if (boots != null) calcAttackSpeed += boots.bonusAttackSpeed + (boots.attackSpeedPerLevel * DataManager.instance.GetItemLevel(bootsId)); }

        RPC_InitMyStats(calcMaxHP, calcDefense, calcMoveSpeed, calcAttackSpeed);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_InitMyStats(float maxHp, float defense, float moveSpeed, float attackSpeed)
    {
        MaxHP = maxHp; TotalDefense = defense; MoveSpeedMultiplier = moveSpeed; AttackSpeedMultiplier = attackSpeed;
        NetCurHP = MaxHP;
        IsDead = false; 
    }

    public void TakeDamage(float rawDamage)
    {
        if (!HasStateAuthority) return; 
        if (IsDead || NetCurHP <= 0) return; 

        float damageReduction = TotalDefense / (TotalDefense + 100f);
        float finalDamage = rawDamage * (1f - damageReduction);

        NetCurHP -= finalDamage;

        if (NetCurHP <= 0)
        {
            NetCurHP = 0;
            IsDead = true; // ★ 체력이 0이 되면 서버가 사망 상태로 만듦
            Debug.Log("[서버] 플레이어 사망 판정!");
        }
    }

    public void OnHPChanged()
    {
        if (HasInputAuthority) OnHealthChangedLocal?.Invoke(NetCurHP);
    }
    
    public void OnDeadStateChanged()
    {
        bool isDead = IsDead; 
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !isDead;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = !isDead;
        
        if (isDead && HasInputAuthority)
        {
            OnPlayerDiedLocal?.Invoke();
            Debug.Log("사망");
        }
    }
}