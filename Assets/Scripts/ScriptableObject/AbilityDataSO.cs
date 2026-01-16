using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class AbilityDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public int abilityID;           // 능력 고유 ID
    public string abilityName;      // 능력 이름
    public string description;      // 능력 설명
    public Sprite icon;             // 능력 아이콘

    [Header("Ability Properties")]
    public AbilityType abilityType; // 능력 타입
    public bool canStack;           // 중복 습득 가능 여부 (질주 등)
    public bool hasLevels;          // 레벨업 가능 여부 (흡혈, 빠른 손놀림 등)
    public int maxLevel = 5;        // 최대 레벨 (레벨업 가능한 경우)

    [Header("Stat Modifiers")]
    public StatModifier[] statModifiers; // 스탯 변화 배열
}

// 능력 타입
public enum AbilityType
{
    Combat,         // 전투 (2연격, 응축된 공격 등)
    Survival,       // 생존 (흡혈, 회피 등)
    Mobility,       // 이동 (질주)
    Utility,        // 유틸 (그림자 은신)
    Passive         // 패시브 (분노, 복수심)
}

// 스탯 변화 데이터
[System.Serializable]
public class StatModifier
{
    public StatType statType;           // 변경할 스탯 종류
    public ModifierOperation operation; // 연산 타입 (더하기/곱하기/설정)
    public float[] valuePerLevel;       // 레벨별 값 (레벨 1~5)
    
    public float GetValue(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, valuePerLevel.Length - 1);
        return valuePerLevel[index];
    }
}

// 스탯 종류
public enum StatType
{
    AttackDamageMultiplier,     // 공격력 배율
    AttackSpeedMultiplier,      // 공격속도 배율
    MoveSpeedMultiplier,        // 이동속도 배율
    AttackCount,                // 공격 횟수
    MaxHP,                      // 최대 체력
    Defense,                    // 방어력
    VampireChance,              // 흡혈 확률
    DodgeChance,                // 회피 확률
    ShadowCooldown,             // 그림자 은신 쿨타임
    HasRage,                    // 분노 보유
    HasRevenge,                 // 복수심 보유
    HasCriticalStrike,          // 급소 공격 보유
    HasLastStand,               // 불굴의 의지 보유
    HasStealth,                 // 잠입의 달인 보유
    ExpMultiplier               // 경험치 획득률 배율
}

// 연산 타입
public enum ModifierOperation
{
    Add,            // 더하기 (+=)
    Multiply,       // 곱하기 (*=)
    Set             // 설정 (=)
}
