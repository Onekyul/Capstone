using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 무기 타입 전환 UI.
/// 3종 무기(검/창/활) 모두 보유 상태에서 타입 전환.
/// EquipWeapon() + SaveGame()으로 서버 동기화.
/// </summary>
public class SwapWeaponUI : MonoBehaviour
{
    [Header("무기 슬롯 (검/창/활)")]
    [SerializeField] private Button swordButton;
    [SerializeField] private Button spearButton;
    [SerializeField] private Button bowButton;

    [SerializeField] private Image swordIcon;
    [SerializeField] private Image spearIcon;
    [SerializeField] private Image bowIcon;

    [SerializeField] private TextMeshProUGUI swordNameText;
    [SerializeField] private TextMeshProUGUI spearNameText;
    [SerializeField] private TextMeshProUGUI bowNameText;

    [Header("상세 스탯 텍스트 (신규)")]
    [SerializeField] private TextMeshProUGUI swordStatText;
    [SerializeField] private TextMeshProUGUI spearStatText;
    [SerializeField] private TextMeshProUGUI bowStatText;

    [Header("설명 텍스트 (신규)")]
    [SerializeField] private TextMeshProUGUI swordDescText;
    [SerializeField] private TextMeshProUGUI spearDescText;
    [SerializeField] private TextMeshProUGUI bowDescText;

    [Header("장착 색상")]
    [SerializeField] private Color equippedColor   = new Color(1f, 1f, 1f, 1f);    // 밝음 (장착 중)
    [SerializeField] private Color unequippedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 어두움 (미장착)

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (DataManager.instance == null) return;

        int currentType = DataManager.instance.GetWeaponTypeFromId(
            DataManager.instance.currentPlayer.equippedWeaponId);

        SetupSlot(0, swordButton, swordIcon, swordNameText, swordStatText, swordDescText, currentType);
        SetupSlot(1, spearButton, spearIcon, spearNameText, spearStatText, spearDescText, currentType);
        SetupSlot(2, bowButton, bowIcon, bowNameText, bowStatText, bowDescText, currentType);
    }

    private void SetupSlot(int weaponType, Button button, Image icon, TextMeshProUGUI nameText,
        TextMeshProUGUI statText, TextMeshProUGUI descText, int currentType)
    {
        bool isEquipped = (weaponType == currentType);
        string weaponId = DataManager.instance.GetEquippedWeaponId(weaponType);
        WeaponData data = null;

        if (!string.IsNullOrEmpty(weaponId))
            data = DataManager.instance.GetWeaponData(weaponId);

        if (data != null)
        {
            if (icon != null) { icon.sprite = data.icon; icon.enabled = true; }

            int level = DataManager.instance.GetItemLevel(weaponId);

            if (nameText != null)
                nameText.text = level > 0 ? $"+{level} {data.weaponName}" : data.weaponName;

            if (statText != null)
            {
                int atk = data.baseAtk + (data.atkPerLevel * level);
                var (attackSpeed, range) = GetFixedStats(weaponType);
                statText.text = $"공격력: {atk}\n공격속도: {attackSpeed}\n사거리: {range}";
            }

            if (descText != null)
                descText.text = string.IsNullOrEmpty(data.description) ? GetDefaultDesc(weaponType) : data.description;

            button.interactable = !isEquipped;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnWeaponSelected(weaponId));
        }
        else
        {
            if (icon != null) icon.enabled = false;
            if (nameText != null) nameText.text = "미보유";
            if (statText != null) statText.text = "";
            if (descText != null) descText.text = "";
            button.interactable = false;
        }

        // 버튼 배경 이미지 명도로 장착 상태 표현
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.color = isEquipped ? equippedColor : unequippedColor;
    }

    /// <summary>무기 타입별 고정 스탯 (공격속도, 사거리)</summary>
    private (float attackSpeed, float range) GetFixedStats(int weaponType)
    {
        switch (weaponType)
        {
            case 0: return (1.0f, 2.0f);   // 검
            case 1: return (1.2f, 3.0f);   // 창
            case 2: return (0.8f, 10.0f);  // 활
            default: return (1.0f, 1.0f);
        }
    }

    /// <summary>WeaponData.description이 비어있을 때 사용할 기본 설명</summary>
    private string GetDefaultDesc(int weaponType)
    {
        switch (weaponType)
        {
            case 0: return "균형 잡힌 근접 무기";
            case 1: return "긴 사거리의 관통 무기";
            case 2: return "먼 거리의 적을 공격하는 무기";
            default: return "";
        }
    }

    private void OnWeaponSelected(string weaponId)
    {
        DataManager.instance.EquipWeapon(weaponId); // 로컬 적용 + SaveGame
        RefreshUI();
    }
}
