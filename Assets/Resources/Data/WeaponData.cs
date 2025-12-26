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
    public int baseAtk;         
    
    [Header("강화 레시피")]
    public UpgradeTable upgradeTable; 
    
    public int GetStat(int level)
    {
        return baseAtk + (level * 2);
    }
}