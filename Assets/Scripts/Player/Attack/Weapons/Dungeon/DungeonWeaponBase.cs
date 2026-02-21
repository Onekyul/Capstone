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

        if (DataManager.instance != null && !string.IsNullOrEmpty(weaponId))
        {
            WeaponData data = DataManager.instance.GetWeaponData(weaponId);
            if (data != null)
            {
                int level = DataManager.instance.GetItemLevel(weaponId);
                calcDamage = data.baseAtk + (data.atkPerLevel * level);
            }

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

    // DungeonAttackManager에서 매 프레임 호출
    public void Attack(Vector2 direction)
    {
        if (playerStats != null && playerStats.IsDead) return;

        // 쿨타임이 끝났다면 공격 실행
        if (AttackCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            // 1. 공격 시각 효과는 모든 클라이언트가 각자 그림 (RPC 호출)
            RPC_PlayAttackVisuals(direction);

            // 2. ★ 실제 데미지 판정은 서버만 실행!
            if (HasStateAuthority)
            {
                ExecuteServerAttack(direction);
            }

            // 3. 쿨타임 재설정
            float speedMult = playerStats != null ? playerStats.AttackSpeedMultiplier : 1.0f;
            float finalCooldown = NetAttackCooldown / speedMult;
            AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, finalCooldown);
        }
    }

    // [RPC] 나 공격했으니까 너희들 화면에도 이펙트 띄워줘!
    [Rpc(RpcSources.StateAuthority | RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayAttackVisuals(Vector2 direction)
    {
        CreateAttackEffect(direction);
    }

    // 자식 클래스에서 구현할 내용들
    protected abstract void ExecuteServerAttack(Vector2 direction); // 서버 타격 판정
    protected abstract void CreateAttackEffect(Vector2 direction); // 시각 효과

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