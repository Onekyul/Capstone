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


}
