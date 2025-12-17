using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New UpgradeTable", menuName = "Data/Upgrade Table")]
public class UpgradeTable : ScriptableObject
{
    public List<UpgradeStep> steps;

    public UpgradeStep GetNextStep(int currentLevel)
    {
        if(currentLevel<steps.Count) return steps[currentLevel];
        return null;
    }

    [System.Serializable]
    public class UpgradeStep
    {
        [Header("비용 및 확률")]
        [Range(0, 100)] 
        public int successRate; // 성공 확률
        public ItemData requiredMaterial; // 필요 재료
        public int materialCount;         // 필요 개수

        [Header("상위 장비")]
        // 나무검->철검 같은 강화
        public WeaponData nextTierWeapon;
        public ArmorData nextTierArmor;
        
    }

}
