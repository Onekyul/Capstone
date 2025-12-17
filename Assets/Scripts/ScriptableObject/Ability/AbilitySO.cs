using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityData", menuName = "Game/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("능력 식별 정보")]
    [Tooltip("배열의 인덱스로 사용될 ID입니다. (0 ~ 39 사이의 고유한 값)")]
    [Range(0, 39)]
    public int abilityID;

    [Tooltip("이 능력을 몇 번까지 찍을 수 있는지 (예: 1이면 1번 나오면 끝, 5면 5번까지 등장)")]
    public int maxLevel;

    [Header("UI 표시 정보")]
    public string abilityName;
    public Sprite icon;
    [TextArea]
    public string description;
}
