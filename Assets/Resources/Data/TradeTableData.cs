using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Trade Table", menuName = "Data/Trade Table")]
public class TradeTableData : ScriptableObject
{
    [Header("교환 레시피 목록")]
    public List<TradeRecipe> recipes; 
}


[System.Serializable]
public class TradeRecipe
{
    public string recipeName; 
    
    [Header("Cost")]
    public ItemData costItem;  
    public int costAmount;

    [Header("Reward")]
    public ItemData rewardItem; 
    public int rewardAmount;
}