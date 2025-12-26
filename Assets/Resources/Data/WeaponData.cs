using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Data/Item/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("무기 기본 정보")]
    public string weaponId;     
    public string weaponName;  
    [TextArea] 
    public string description;
    public Sprite icon;
    //public GameObject modelPrefab; 

    [Header("공격력")]
    public int baseAtk;         // 기본 공격력 (강화 전)
    public int atkPerLevel = 5; // 강화 레벨당 증가하는 공격력
    
    [Header("강화 레시피")]
    public UpgradeTable upgradeTable;
}