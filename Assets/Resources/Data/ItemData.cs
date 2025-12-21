using UnityEngine;

[CreateAssetMenu(fileName = "New Material", menuName = "Data/Item/Material")]
public class ItemData : ScriptableObject
{
    [Header("아이템 기본 정보")] 
    public string itemId;
    public string itemName;
    [TextArea]
    public string description;
    public Sprite icon;
    public int maxStack = 99;

}
