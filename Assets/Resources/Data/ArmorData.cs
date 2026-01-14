using UnityEngine;

public enum ArmorType
{
    Helmet,
    Armor,
    Boots
}

[CreateAssetMenu(fileName = "New Armor", menuName = "Data/Item/Armor")]
public class ArmorData : ScriptableObject
{
    [Header("방어구 타입")]
    public ArmorType armorType;
    [Header("방어구 기본 정보")]
    public string armorId;     
    public string armorName; 
    [TextArea] 
    public string description;
    public Sprite icon;
    
    [Header("방어구 스펙")] 
    public int bonusDef;        // 기본 방어력 보너스
    public int bonusHp;         // 기본 체력 보너스
    public float bonusAtkSpeed;    // 기본 속도 보너스
    public GameObject modelprefab;
    
    [Header("강화당 증가량")]
    public int defPerLevel = 1;     // 강화 레벨당 방어력 증가량 (헬멧용)
    public int hpPerLevel = 1;      // 강화 레벨당 체력 증가량 (갑옷용)
    public float speedPerLevel = 1f; // 강화 레벨당 속도 증가량 (신발용)
    
    [Header("강화 레시피")]
    public UpgradeTable upgradeTable;
    
    public float GetStat(EquipmentType type,int level)
    {
        switch (type)
        {
            case EquipmentType.Helmet:
                return bonusDef + (level * 1);
            case EquipmentType.Armor:
                return bonusHp + (level * 1);
            case EquipmentType.Boots:
                // 이동속도 (소수점 계산)
                return bonusAtkSpeed + (level * 1f);
        }
        // 예: 기본방어력 + (레벨 * 3)
        return 0f;
    }


}
