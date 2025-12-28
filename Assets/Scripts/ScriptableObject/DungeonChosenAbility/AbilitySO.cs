using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAbilityData", menuName = "Game/AbilitySO")]
public class AbilityData : ScriptableObject
{
    [Header("능력 식별 정보")]
    [Range(0, 39)]
    public int abilityID;
    public int maxLevel;

    [Header("UI 표시 정보")]
    public string abilityName;
    public Sprite icon;

    [Tooltip("설명에 {0}을 넣으면 그 자리에 아래 'Values'의 숫자가 들어갑니다.\n예: 적 처치 시 {0}% 확률로 회복")]
    [TextArea]
    public string description;

    [Header("레벨별 수치")]
    [Tooltip("레벨 1, 2, 3... 순서대로 적용될 수치를 입력하세요.")]
    public List<float> values; 
}
