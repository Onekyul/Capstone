using Fusion;
using TMPro;
using UnityEngine;
using System;

public class DungeonPlayerStats : NetworkBehaviour, IDamageable
{
    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public float NetCurHP { get; set; }

    [Networked, OnChangedRender(nameof(OnDeadStateChanged))]
    public NetworkBool IsDead { get; set; }

    [Networked]
    public NetworkBool IsFrozen { get; set; }

    [Header("Base Stats")]
    public float baseMaxHP = 100f;
    public float baseDefense = 0f;

    [Networked] public float MaxHP { get; set; }
    [Networked] public float TotalDefense { get; set; }
    [Networked] public float MoveSpeedMultiplier { get; set; } = 1.0f;
    [Networked] public float AttackSpeedMultiplier { get; set; } = 1.0f;

    [Header("닉네임 UI")]
    [SerializeField] private TMP_Text nicknameText;

    [Networked, OnChangedRender(nameof(UpdateNicknameUI))]
    public NetworkString<_32> Nickname { get; set; }

    public event Action<float> OnHealthChangedLocal;
    public event Action OnPlayerDiedLocal;

    public override void Spawned()
    {
#if !UNITY_SERVER
        if (HasInputAuthority)
        {
            CalculateAndSendMyStats();

            string myNickname = "Unknown";
            if (SessionManager.Instance != null)
                myNickname = SessionManager.Instance.Nickname;
            RPC_SetNickname(myNickname);
        }

        // HP바 초기화: 모든 플레이어 오브젝트에서 실행 (원격 플레이어 HP바도 표시)
        DungeonHpBarSlider hpBar = GetComponentInChildren<DungeonHpBarSlider>(true);
        if (hpBar != null)
            hpBar.Init(this);
#endif
        UpdateNicknameUI();
        // 데디서버에서는 BossDungeonServer.OnPlayerJoined()가 InitFromServerData()를 호출함
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(NetworkString<_32> newNickname)
    {
        Nickname = newNickname;
    }

    private string _cachedNickname;

    private void UpdateNicknameUI()
    {
        if (nicknameText == null) return;
        nicknameText.text = Nickname.ToString();
        _cachedNickname = nicknameText.text;
    }

    public override void Render()
    {
#if !UNITY_SERVER
        // OnChangedRender 누락 방지: 값이 달라졌으면 직접 갱신
        string current = Nickname.ToString();
        if (current != _cachedNickname)
            UpdateNicknameUI();
#endif
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

    // 피격 체크: Fusion FixedUpdateNetwork에서 수동 충돌 체크
    private int _lastDamageTick;
    private const int DamageTickInterval = 60; // 60틱 = 약 1초 쿨다운
    private const float ContactRadius = 0.6f;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (IsDead || NetCurHP <= 0) return;

        // 보석 획득 체크
        if (JewelSyncManager.Instance != null)
        {
            for (int i = 0; i < JewelSyncManager.MaxJewels; i++)
            {
                if (!JewelSyncManager.Instance.JewelActive[i]) continue;
                if (Vector2.Distance(transform.position, JewelSyncManager.Instance.JewelPositions[i]) < JewelSyncManager.PickupRadius)
                {
                    JewelSyncManager.Instance.PickupJewel(i);
                    RPC_GainExp(JewelSyncManager.JewelExpAmount);
                    break;
                }
            }
        }

        if (Runner.Tick - _lastDamageTick < DamageTickInterval) return;

        // 주변 Enemy 태그 오브젝트 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, ContactRadius);
        foreach (var col in colliders)
        {
            if (!col.CompareTag("Enemy")) continue;
            MonsterController monster = col.GetComponent<MonsterController>();
            if (monster == null) continue;
            if (monster.monsterType == MonsterType.Boss) continue; // 보스 충돌 데미지 없음

            _lastDamageTick = Runner.Tick;
            TakeDamage(monster.normalDamage);
            Debug.Log($"[서버] 플레이어 피격: {col.name}, 데미지={monster.normalDamage}, 남은HP={NetCurHP}");
            break;
        }
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
            IsDead = true;
            Debug.Log("[서버] 플레이어 사망 판정!");

            // 서버에서도 Collider 비활성화 (몬스터 타겟팅 차단)
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

#if UNITY_SERVER
            BossDungeonServer.Instance?.OnPlayerDied();
#endif
        }
    }

    /// <summary>
    /// 서버 → 모든 클라이언트: 잡몹 처치 exp 지급.
    /// BossDungeonServer.GrantExpToAllPlayers()에서 호출.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GainExp(float amount)
    {
        if (BossDungeonLevelManager.instance != null)
            BossDungeonLevelManager.instance.GainExperience(amount);
    }

    /// <summary>
    /// 서버 → 클라이언트: 페이즈 타이머 시작 (120초 카운트다운 동기화).
    /// BossDungeonServer.CoInitBossSession() 및 그로기 종료 시 호출.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_StartPhaseTimer()
    {
        if (HasInputAuthority && BossDungeonUIManager.instance != null)
            BossDungeonUIManager.instance.StartTimer();
    }

    /// <summary>
    /// 서버 → 클라이언트: 보스 사망 시 결과 알림.
    /// BossDungeonServer.OnBossDefeated()에서 호출.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_ShowBossResult(NetworkBool cleared, float clearTime)
    {
        if (HasInputAuthority && BossDungeonUIManager.instance != null)
        {
            BossDungeonUIManager.instance.ShowResultPanel(cleared, clearTime);
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