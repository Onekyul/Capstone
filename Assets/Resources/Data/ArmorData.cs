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
    public int bonusDef;
    public int bonusHp;
    public float bonusSpeed;
    public GameObject modelprefab;
    
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
                return bonusSpeed + (level * 0.1f);
        }
        // 예: 기본방어력 + (레벨 * 3)
        return 0f;
    }


}
