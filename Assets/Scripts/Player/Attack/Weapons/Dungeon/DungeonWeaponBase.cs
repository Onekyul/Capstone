using UnityEngine;
using Fusion;

public abstract class DungeonWeaponBase : NetworkBehaviour
{
    [Header("Weapon Identity")]
    public string weaponId = ""; 

    // =======================================================
    // [동기화 변수] 서버와 모든 클라이언트가 공유하는 무기 스탯
    // =======================================================
    [Networked] public float NetBaseDamage { get; set; }
    [Networked] public float NetAttackCooldown { get; set; }

    [Networked] public int EnchantFire { get; set; }
    [Networked] public int EnchantIce { get; set; }
    [Networked] public int EnchantLightning { get; set; }
    [Networked] public int EnchantPoison { get; set; }

    [Networked] public TickTimer AttackCooldownTimer { get; set; } // 퓨전 전용 쿨타임

    [SerializeField] protected float attackDuration = 0.3f;
    [SerializeField] protected float enchantChancePerLevel = 6f; 
    [SerializeField] protected Transform attackPoint;

    protected DungeonPlayerStats playerStats;

    public override void Spawned()
    {
        playerStats = GetComponentInParent<DungeonPlayerStats>();

        if (HasInputAuthority)
        {
            LoadAndSendWeaponData();
        }
    }

    private void LoadAndSendWeaponData()
    {
        float calcDamage = 10f;
        float calcCooldown = 1f;
        int fire = 0, ice = 0, lightning = 0, poison = 0;

        if (DataManager.instance != null)
        {
            if (!string.IsNullOrEmpty(weaponId))
            {
                WeaponData data = DataManager.instance.GetWeaponData(weaponId);
                if (data != null)
                {
                    int level = DataManager.instance.GetItemLevel(weaponId);
                    calcDamage = data.baseAtk + (data.atkPerLevel * level);
                }
            }

            // 인챈트는 weaponId 유무와 무관하게 항상 읽음
            fire = DataManager.instance.GetEnchantLevel("ent_fire");
            ice = DataManager.instance.GetEnchantLevel("ent_ice");
            lightning = DataManager.instance.GetEnchantLevel("ent_lightning");
            poison = DataManager.instance.GetEnchantLevel("ent_poison");
        }

        RPC_InitWeaponStats(calcDamage, calcCooldown, fire, ice, lightning, poison);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_InitWeaponStats(float damage, float cooldown, int fire, int ice, int lightning, int poison)
    {
        NetBaseDamage = damage;
        NetAttackCooldown = cooldown;
        EnchantFire = fire;
        EnchantIce = ice;
        EnchantLightning = lightning;
        EnchantPoison = poison;
        
        AttackCooldownTimer = TickTimer.None; 
    }

    /// <summary>
    /// 서버 전용: 쿨타임 체크 → 데미지 판정. 시각 효과 없음.
    /// 공격 발동 시 true 반환.
    /// </summary>
    public bool TryServerAttack(Vector2 direction)
    {
        if (!HasStateAuthority) return false;
        if (playerStats != null && playerStats.IsDead) return false;
        if (!AttackCooldownTimer.ExpiredOrNotRunning(Runner)) return false;

        Debug.Log($"[Server] {GetType().Name}.TryServerAttack() 발동");

        ExecuteServerAttack(direction);

        float speedMult = playerStats != null ? playerStats.AttackSpeedMultiplier : 1.0f;
        AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, NetAttackCooldown / speedMult);

        return true;
    }

    // 자식 클래스에서 구현할 내용들
    protected abstract void ExecuteServerAttack(Vector2 direction); // 서버 타격 판정
    public abstract void CreateAttackEffect(Vector2 direction);     // 시각 효과 (DungeonAttackManager에서 호출)

    // 스탯 계산 유틸
    public float GetTotalDamage() => NetBaseDamage; // 엘리트 킬러 등은 여기서 추가 계산 가능

    public int[] CalculateAppliedEnchants()
    {
        int[] result = new int[4];
        if (Random.Range(0f, 100f) < EnchantFire * enchantChancePerLevel) result[0] = EnchantFire;
        if (Random.Range(0f, 100f) < EnchantIce * enchantChancePerLevel) result[1] = EnchantIce;
        if (Random.Range(0f, 100f) < EnchantLightning * enchantChancePerLevel) result[2] = EnchantLightning;
        if (Random.Range(0f, 100f) < EnchantPoison * enchantChancePerLevel) result[3] = EnchantPoison;
        return result;
    }
}