using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Enchant", menuName = "Data/Enchant")]
public class EnchantData : ScriptableObject
{  
    [Header("기본 정보")]
    public string enchantId; 
    public string enchantName; 
    public Sprite icon;        
    public Sprite lockedIcon; //해금 전 아이콘
    
    [TextArea(3, 5)] 
    public string descriptionTemplate;
    
    [Header("인챈트 레시피")]
    public List<EnchantLevelInfo> levels;

    //다음 레시피를 반환
    public EnchantLevelInfo GetNextLevelInfo(int currentLevel)
    {
        if (currentLevel < levels.Count) return levels[currentLevel];
        return null;
    }
    
    //현재 레벨 수치 반환 -> 데미지 계산
    public EnchantLevelInfo GetCurrentLevelInfo(int currentLevel)
    {
        if (currentLevel > 0 && currentLevel <= levels.Count) return levels[currentLevel - 1];
        return null;
    }
    
    

    
    
    [System.Serializable]
    public class EnchantLevelInfo
    {
        [Header("확률")]
        [Range(0, 100)]
        public int successRate;         
        public List<EnchantMaterialCost> requiredMaterials; // 필요 재료

        [Header("능력치 상세 설정")]
        public List<EnchantParameter> parameters; 
    }
    [System.Serializable]
    public class EnchantMaterialCost // 비용
    {
        public ItemData material;       
        public int count;               
    }

    [System.Serializable]
    public class EnchantParameter
    {
        public EnchantStatType type;    // 능력 종류 (확률, 데미지 등)
        public float value;             
    }

// 능력치 종류 정의
    public enum EnchantStatType
    {
        None,
        ChancePercent,      // 발동 확률 (n%)
        DamagePercent,      // 데미지 계수 (공격력의 n%)
        Duration,           // 지속 시간 (n초)
        Cooldown,           // 쿨타임 (n초)
        Radius,             // 범위 반지름 (n)
        ValueAmount,        // 고정 수치 (방어막 n 등)
        SlowPercent,        // 이속 감소율 (n%)
        BurnDamagePercent,  // 화상 데미지
        DotInterval,         // 도트 데미지 간격 (0.5초 등)
        FreezeChancePercent, // 빙결 발동 확률 (슬로우 상태일 때) 
        FreezeDuration      // 빙결 지속 시간
    }
}
